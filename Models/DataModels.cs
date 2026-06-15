using System;
using System.Collections.Generic;

namespace ConfigEditor.Models
{
    public class ConfigItem
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public string Section { get; set; }
        public string Comment { get; set; }

        public ConfigItem()
        {
        }

        public ConfigItem(string key, string value, string section = "", string comment = "")
        {
            Key = key;
            Value = value;
            Section = section ?? "";
            Comment = comment ?? "";
        }

        public ConfigItem Clone()
        {
            return new ConfigItem(Key, Value, Section, Comment);
        }
    }

    public class SectionModel
    {
        public string Name { get; set; }
        public List<ConfigItem> Items { get; set; }

        public SectionModel()
        {
            Items = new List<ConfigItem>();
        }

        public SectionModel(string name) : this()
        {
            Name = name ?? "";
        }
    }

    public class IniFileModel
    {
        public string Id { get; set; }
        public string FileName { get; set; }
        public string OutputPath { get; set; }
        public string ImportPath { get; set; }
        public List<SectionModel> Sections { get; set; }

        public IniFileModel()
        {
            Id = Guid.NewGuid().ToString("N");
            Sections = new List<SectionModel>();
        }

        public IniFileModel(string fileName, string outputPath = null) : this()
        {
            FileName = fileName;
            OutputPath = outputPath ?? fileName;
        }
    }

    public class ConfigGroup
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<ConfigItem> Settings { get; set; }
        public List<string> MemberFileIds { get; set; }

        public ConfigGroup()
        {
            Id = Guid.NewGuid().ToString("N");
            Settings = new List<ConfigItem>();
            MemberFileIds = new List<string>();
        }

        public ConfigGroup(string name) : this()
        {
            Name = name;
        }
    }

    public class ProjectData
    {
        public string Name { get; set; }
        public List<ConfigItem> GlobalConfig { get; set; }
        public List<ConfigGroup> Groups { get; set; }
        public List<IniFileModel> Files { get; set; }

        public ProjectData()
        {
            Name = "New Project";
            GlobalConfig = new List<ConfigItem>();
            Groups = new List<ConfigGroup>();
            Files = new List<IniFileModel>();
        }
    }

    public enum NodeType
    {
        Project,
        GlobalConfig,
        GroupsFolder,
        Group,
        GroupSettings,
        GroupMembers,
        FilesFolder,
        File,
        Section
    }

    public class TreeNodeData
    {
        public NodeType Type { get; set; }
        public string GroupId { get; set; }
        public string FileId { get; set; }
        public string SectionName { get; set; }
    }
}
