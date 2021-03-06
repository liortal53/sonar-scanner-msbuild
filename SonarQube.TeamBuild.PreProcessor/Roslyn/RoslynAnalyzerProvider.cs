﻿/*
 * SonarQube Scanner for MSBuild
 * Copyright (C) 2016-2017 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */
 
using SonarQube.Common;
using SonarQube.TeamBuild.Integration;
using SonarQube.TeamBuild.PreProcessor.Roslyn.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SonarQube.TeamBuild.PreProcessor.Roslyn
{
    public class RoslynAnalyzerProvider : IAnalyzerProvider
    {
        public const string RoslynFormatNamePrefix = "roslyn-{0}";
        public const string RoslynRulesetFileName = "SonarQubeRoslyn-{0}.ruleset";

        private const string SONARANALYZER_PARTIAL_REPO_KEY = "sonaranalyzer-{0}";
        private const string ROSLYN_REPOSITORY_PREFIX = "roslyn.";

        public const string CSharpLanguage = "cs";
        public const string CSharpPluginKey = "csharp";
        public const string CSharpRepositoryKey = "csharp";

        public const string VBNetLanguage = "vbnet";
        public const string VBNetPluginKey = "vbnet";
        public const string VBNetRepositoryKey = "vbnet";

        private readonly IAnalyzerInstaller analyzerInstaller;
        private readonly ILogger logger;
        private TeamBuildSettings sqSettings;
        private IDictionary<string, string> sqServerSettings;

        #region Public methods

        public RoslynAnalyzerProvider(IAnalyzerInstaller analyzerInstaller, ILogger logger)
        {
            if (analyzerInstaller == null)
            {
                throw new ArgumentNullException(nameof(analyzerInstaller));
            }
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }
            this.analyzerInstaller = analyzerInstaller;
            this.logger = logger;
        }

        public AnalyzerSettings SetupAnalyzer(TeamBuildSettings settings, IDictionary<string, string> serverSettings,
            IEnumerable<ActiveRule> activeRules, IEnumerable<string> inactiveRules, string language)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }
            if (language == null)
            {
                throw new ArgumentNullException(nameof(language));
            }
            if (serverSettings == null)
            {
                throw new ArgumentNullException(nameof(serverSettings));
            }
            if (inactiveRules == null)
            {
                throw new ArgumentNullException(nameof(inactiveRules));
            }
            if (activeRules == null)
            {
                throw new ArgumentNullException(nameof(activeRules));
            }
            if (!activeRules.Any())
            {
                return null;
            }

            this.sqSettings = settings;
            this.sqServerSettings = serverSettings;

            AnalyzerSettings analyzer = ConfigureAnalyzer(language, activeRules, inactiveRules);
            if (analyzer == null)
            {
                logger.LogInfo(Resources.RAP_NoPluginInstalled, language);
            }

            return analyzer;
        }

        public static string GetRoslynFormatName(string language)
        {
            return string.Format(RoslynFormatNamePrefix, language);
        }

        public static string GetRoslynRulesetFileName(string language)
        {
            return string.Format(RoslynRulesetFileName, language);
        }
        #endregion

        #region Private methods

        /// <summary>
        /// Generates several files related to rulesets and roslyn analyzer assemblies.
        /// Active rules should never be empty, but depending on the server settings of repo keys, we might have no rules in the ruleset.
        /// In that case, this method returns null.
        /// </summary>
        private AnalyzerSettings ConfigureAnalyzer(string language, IEnumerable<ActiveRule> activeRules, IEnumerable<string> inactiveRules)
        {
            RoslynRuleSetGenerator ruleSetGenerator = new RoslynRuleSetGenerator(sqServerSettings);
            RuleSet ruleSet = ruleSetGenerator.Generate(activeRules, inactiveRules, language);
            string rulesetFilePath = this.WriteRuleset(ruleSet, language);
            if (rulesetFilePath == null)
            {
                // no ruleset, nothing was written in disk
                return null;
            }

            IEnumerable<string> additionalFiles = this.WriteAdditionalFiles(language, activeRules);
            IEnumerable<string> analyzersAssemblies = this.FetchAnalyzerAssemblies(activeRules, language);

            AnalyzerSettings compilerConfig = new AnalyzerSettings(language, rulesetFilePath,
                analyzersAssemblies ?? Enumerable.Empty<string>(),
                additionalFiles ?? Enumerable.Empty<string>());
            return compilerConfig;
        }

        /// <summary>
        /// Write ruleset to a file.
        /// Nothing will be written and null with be returned if the ruleset contains no rules
        /// </summary>
        public string WriteRuleset(RuleSet ruleSet, string language)
        {
            string rulesetFilePath = null;
            if (ruleSet == null || ruleSet.Rules == null || !ruleSet.Rules.Any())
            {
                this.logger.LogDebug(Resources.RAP_ProfileDoesNotContainRuleset);
            }
            else
            {
                rulesetFilePath = GetRulesetFilePath(this.sqSettings, language);
                this.logger.LogDebug(Resources.RAP_UnpackingRuleset, rulesetFilePath);
                ruleSet.Save(rulesetFilePath);
            }
            return rulesetFilePath;
        }

        private static string GetRulesetFilePath(TeamBuildSettings settings, string language)
        {
            return Path.Combine(settings.SonarConfigDirectory, GetRoslynRulesetFileName(language));
        }

        private IEnumerable<string> WriteAdditionalFiles(string language, IEnumerable<ActiveRule> activeRules)
        {
            Debug.Assert(activeRules != null, "Supplied active rules should not be null");

            List<string> additionalFiles = new List<string>();
            string filePath = WriteSonarLintXmlFile(language, activeRules);
            if (filePath != null)
            {
                Debug.Assert(File.Exists(filePath), "Expecting the additional file to exist: {0}", filePath);
                additionalFiles.Add(filePath);
            }

            return additionalFiles;
        }

        private string WriteSonarLintXmlFile(string language, IEnumerable<ActiveRule> activeRules)
        {
            if (string.IsNullOrWhiteSpace(language))
            {
                this.logger.LogDebug(Resources.RAP_AdditionalFileNameMustBeSpecified);
                return null;
            }

            string content;
            if (language.Equals(CSharpLanguage))
            {
                content = RoslynSonarLint.GenerateXml(activeRules, sqServerSettings, language, "csharpsquid");
            }
            else
            {
                content = RoslynSonarLint.GenerateXml(activeRules, sqServerSettings, language, "vbnet");
            }

            string langDir = Path.Combine(this.sqSettings.SonarConfigDirectory, language);
            Directory.CreateDirectory(langDir);

            string fullPath = Path.Combine(langDir, "SonarLint.xml");
            if (File.Exists(fullPath))
            {
                this.logger.LogDebug(Resources.RAP_AdditionalFileAlreadyExists, language, fullPath);
                return null;
            }

            this.logger.LogDebug(Resources.RAP_WritingAdditionalFile, fullPath);
            File.WriteAllText(fullPath, content);
            return fullPath;
        }

        public IEnumerable<string> FetchAnalyzerAssemblies(IEnumerable<ActiveRule> activeRules, string language)
        {
            ICollection<string> repoKeys = ActiveRulesPartialRepoKey(activeRules, language);
            IList<Plugin> plugins = new List<Plugin>();

            foreach (string repoKey in repoKeys)
            {
                string pluginkey;
                string pluginVersion;
                string staticResourceName;
                if (!sqServerSettings.TryGetValue(PluginKeyPropertyKey(repoKey), out pluginkey)
                    || !sqServerSettings.TryGetValue(PluginVersionPropertyKey(repoKey), out pluginVersion)
                    || !sqServerSettings.TryGetValue(StaticResourceNamePropertyKey(repoKey), out staticResourceName))
                {
                    this.logger.LogInfo(Resources.RAP_NoAssembliesForRepo, repoKey, language);
                    continue;
                }

                plugins.Add(new Plugin(pluginkey, pluginVersion, staticResourceName));
            }

            IEnumerable<string> analyzerAssemblyPaths = null;
            if (plugins.Count == 0)
            {
                this.logger.LogInfo(Resources.RAP_NoAnalyzerPluginsSpecified, language);
            }
            else
            {
                this.logger.LogInfo(Resources.RAP_ProvisioningAnalyzerAssemblies, language);
                analyzerAssemblyPaths = this.analyzerInstaller.InstallAssemblies(plugins);
            }
            return analyzerAssemblyPaths;
        }

        private static string PluginKeyPropertyKey(string partialRepoKey)
        {
            return partialRepoKey + ".pluginKey";
        }

        private static string PluginVersionPropertyKey(string partialRepoKey)
        {
            return partialRepoKey + ".pluginVersion";
        }

        private static string StaticResourceNamePropertyKey(string partialRepoKey)
        {
            return partialRepoKey + ".staticResourceName";
        }

        private static ICollection<string> ActiveRulesPartialRepoKey(IEnumerable<ActiveRule> activeRules, string language)
        {
            ISet<string> list = new HashSet<string>();

            foreach (ActiveRule activeRule in activeRules)
            {
                if (activeRule.RepoKey.StartsWith(ROSLYN_REPOSITORY_PREFIX))
                {
                    list.Add(activeRule.RepoKey.Substring(ROSLYN_REPOSITORY_PREFIX.Length));
                }
                else if ("csharpsquid".Equals(activeRule.RepoKey) || "vbnet".Equals(activeRule.RepoKey))
                {
                    list.Add(string.Format(SONARANALYZER_PARTIAL_REPO_KEY, language));
                }
            }

            return list;
        }

        #endregion
    }
}
