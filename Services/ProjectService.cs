using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using ConfigEditor.Models;

namespace ConfigEditor.Services
{
    public static class ProjectService
    {
        public static void SaveToJson(ProjectData project, string filePath)
        {
            var json = new JavaScriptSerializer().Serialize(project);
            File.WriteAllText(filePath, json, Encoding.UTF8);
        }

        public static ProjectData LoadFromJson(string filePath)
        {
            var json = File.ReadAllText(filePath, Encoding.UTF8);
            return new JavaScriptSerializer().Deserialize<ProjectData>(json);
        }

        public static IniFileModel ImportIniFile(string filePath)
        {
            var model = new IniFileModel
            {
                FileName = Path.GetFileName(filePath),
                OutputPath = filePath,
                ImportPath = filePath
            };

            SectionModel currentSection = null;
            var lines = File.ReadAllLines(filePath, Encoding.UTF8);

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                if (string.IsNullOrEmpty(trimmed))
                    continue;

                if (trimmed.StartsWith(";") || trimmed.StartsWith("#"))
                    continue;

                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    var sectionName = trimmed.Substring(1, trimmed.Length - 2).Trim();
                    currentSection = new SectionModel(sectionName);
                    model.Sections.Add(currentSection);
                    continue;
                }

                var eqIndex = trimmed.IndexOf('=');
                if (eqIndex > 0)
                {
                    var key = trimmed.Substring(0, eqIndex).Trim();
                    var value = trimmed.Substring(eqIndex + 1).Trim();

                    var item = new ConfigItem(key, value);

                    if (currentSection != null)
                        currentSection.Items.Add(item);
                    else
                    {
                        if (model.Sections.Count == 0 || model.Sections[0].Name != "")
                        {
                            model.Sections.Insert(0, new SectionModel(""));
                        }
                        model.Sections[0].Items.Add(item);
                    }
                }
            }

            return model;
        }

        public static void ExportAll(ProjectData project)
        {
            foreach (var file in project.Files)
            {
                ExportSingleFile(project, file);
            }
        }

        public static void ExportSingleFile(ProjectData project, IniFileModel file)
        {
            var outputPath = file.OutputPath;
            if (string.IsNullOrEmpty(outputPath))
                return;

            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var sb = new StringBuilder();
            var allSections = GetAllSections(project, file);

            foreach (var section in allSections)
            {
                var resolvedItems = ResolveConfig(project, file, section.Name);
                if (resolvedItems.Count == 0)
                    continue;

                if (!string.IsNullOrEmpty(section.Name))
                {
                    sb.AppendLine($"[{section.Name}]");
                }

                foreach (var item in resolvedItems)
                {
                    if (!string.IsNullOrEmpty(item.Comment))
                    {
                        sb.AppendLine($"; {item.Comment}");
                    }
                    sb.AppendLine($"{item.Key}={item.Value}");
                }

                sb.AppendLine();
            }

            File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
        }

        public static List<SectionModel> GetAllSections(ProjectData project, IniFileModel file)
        {
            var sectionNames = new HashSet<string>();
            var sections = new List<SectionModel>();

            foreach (var item in project.GlobalConfig)
            {
                var sn = item.Section ?? "";
                if (sectionNames.Add(sn))
                    sections.Add(new SectionModel(sn));
            }

            foreach (var group in project.Groups)
            {
                if (!group.MemberFileIds.Contains(file.Id))
                    continue;
                foreach (var item in group.Settings)
                {
                    var sn = item.Section ?? "";
                    if (sectionNames.Add(sn))
                        sections.Add(new SectionModel(sn));
                }
            }

            foreach (var section in file.Sections)
            {
                if (sectionNames.Add(section.Name ?? ""))
                    sections.Add(new SectionModel(section.Name));
            }

            return sections;
        }

        public static List<ConfigItem> ResolveConfig(ProjectData project, IniFileModel file, string sectionName)
        {
            sectionName = sectionName ?? "";
            var result = new Dictionary<string, ConfigItem>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in project.GlobalConfig)
            {
                if ((item.Section ?? "") == sectionName)
                    result[item.Key] = new ConfigItem(item.Key, item.Value, sectionName, item.Comment);
            }

            foreach (var group in project.Groups)
            {
                if (!group.MemberFileIds.Contains(file.Id))
                    continue;
                foreach (var item in group.Settings)
                {
                    if ((item.Section ?? "") == sectionName)
                        result[item.Key] = new ConfigItem(item.Key, item.Value, sectionName, item.Comment);
                }
            }

            foreach (var section in file.Sections)
            {
                if ((section.Name ?? "") == sectionName)
                {
                    foreach (var item in section.Items)
                    {
                        result[item.Key] = new ConfigItem(item.Key, item.Value, sectionName, item.Comment);
                    }
                }
            }

            return result.Values.ToList();
        }

        public static List<ConfigItem> GetGroupEffectiveConfig(ProjectData project, ConfigGroup group)
        {
            var result = new Dictionary<string, ConfigItem>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in project.GlobalConfig)
            {
                result[$"{item.Section ?? ""}:{item.Key}"] = new ConfigItem(item.Key, item.Value, item.Section, item.Comment);
            }

            foreach (var item in group.Settings)
            {
                result[$"{item.Section ?? ""}:{item.Key}"] = new ConfigItem(item.Key, item.Value, item.Section, item.Comment);
            }

            return result.Values.ToList();
        }

        public static string GetEffectiveSourceString(ProjectData project, IniFileModel file, string sectionName)
        {
            sectionName = sectionName ?? "";
            var sources = new List<string>();

            bool hasGlobal = project.GlobalConfig.Any(i => (i.Section ?? "") == sectionName);
            if (hasGlobal) sources.Add("Global");

            var groups = project.Groups
                .Where(g => g.MemberFileIds.Contains(file.Id) && g.Settings.Any(i => (i.Section ?? "") == sectionName))
                .Select(g => $"Group:{g.Name}");
            sources.AddRange(groups);

            var section = file.Sections.FirstOrDefault(s => (s.Name ?? "") == sectionName);
            if (section != null && section.Items.Any())
                sources.Add("File");

            return string.Join(" > ", sources);
        }

        public static string GetProjectFileFilter()
        {
            return "Config Editor Project (*.cfgproj)|*.cfgproj|All Files (*.*)|*.*";
        }

        public static string GetIniFileFilter()
        {
            return "INI Files (*.ini)|*.ini|All Files (*.*)|*.*";
        }
    }
}
