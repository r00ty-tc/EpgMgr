using System.Xml.Serialization;

namespace EpgMgr
{
    [XmlType(TypeName = "Plugin")]
    public class PluginConfigEntry
    {
        [XmlAttribute]
        public string Id { get; set; }
        [XmlText]
        public string Name { get; set; }
        [XmlAttribute]
        public string DllFile { get; set; }
        [XmlAttribute]
        public string ConsoleId { get; set; }

        public PluginConfigEntry()
        {
            Id = string.Empty;
            Name = string.Empty;
            DllFile = string.Empty;
            ConsoleId = string.Empty;
        }

        public PluginConfigEntry(string id, string name, string dllFile, string? consoleId = null)
        {
            Id = id;
            Name = name;
            DllFile = dllFile;
            ConsoleId = consoleId ?? name;
        }
    }

    public class ChannelAlias
    {
        [XmlAttribute(AttributeName = "channel-name")]
        public string ChannelName { get; set; }
        [XmlText]
        public string Alias { get; set; }

        public ChannelAlias(string channelName, string alias)
        {
            ChannelName = channelName;
            Alias = alias;
        }

        public ChannelAlias()
        {
            ChannelName = string.Empty;
            Alias = string.Empty;

        }
    }

    [XmlType]
    public class Config
    {
        public List<PluginConfigEntry> EnabledPlugins { get; set; }
        public ConfigXmlTv XmlTvConfig { get; set; }
        [XmlIgnore]
        public Dictionary<string, string> ChannelNameToAlias { get; set; }
        [XmlIgnore]
        public Dictionary<string, string> ChannelAliasToName { get; set; }

        public List<ChannelAlias> ChannelAliases { get; set; }

        public Config()
        {
            EnabledPlugins = new List<PluginConfigEntry>();
            XmlTvConfig = new ConfigXmlTv
            {
                IncldeProgrammeStarRatings = true,
                IncludeProgrammeCategories = true,
                IncludeProgrammeCredits = true,
                IncludeProgrammeIcons = true,
                IncludeProgrammeImages = true,
                IncludeProgrammeRatings = true, 
                IncludeProgrammeReviews = true,
                Filename = "Default-Guide.xml",
                MaxDaysBehind = 1,
                MaxDaysAhead = 5
            };
            ChannelNameToAlias = new Dictionary<string, string>();
            ChannelAliasToName = new Dictionary<string, string>();
            ChannelAliases = new List<ChannelAlias>();
        }

        public void PostLoadConfig()
        {
            ChannelNameToAlias = ChannelAliases.ToDictionary(row => row.ChannelName, row => row.Alias);
            ChannelAliasToName = ChannelAliases.ToDictionary(row => row.Alias, row => row.ChannelName);
        }

        public void PreSaveConfig()
        {
            // So dumb that I can't do this as a property setter
            ChannelAliases = ChannelNameToAlias.Select(row => new ChannelAlias(row.Key, row.Value)).ToList();
        }
    }

    [XmlType(TypeName = "XmlTv")]
    public class ConfigXmlTv
    {
        public ConfigXmlTv()
        {
            Filename = string.Empty;
            IncludeProgrammeCredits = false;
            IncludeProgrammeCategories = false;
            IncludeProgrammeIcons = false;
            IncludeProgrammeRatings = false;
            IncldeProgrammeStarRatings = false;
            IncludeProgrammeReviews = false;
            IncludeProgrammeImages = false;
            MaxDaysAhead = 5;
            MaxDaysBehind = 1;
        }

        [XmlElement]
        public string Filename { get; set; }
        [XmlAttribute]
        public bool IncludeProgrammeCredits { get; set; }
        [XmlAttribute]
        public bool IncludeProgrammeCategories { get; set; }
        [XmlAttribute]
        public bool IncludeProgrammeIcons { get; set; }
        [XmlAttribute]
        public bool IncludeProgrammeRatings { get; set; }
        [XmlAttribute]
        public bool IncldeProgrammeStarRatings { get; set; }
        [XmlAttribute]
        public bool IncludeProgrammeReviews { get; set; }
        [XmlAttribute]
        public bool IncludeProgrammeImages { get; set; }

        [XmlAttribute] 
        public int MaxDaysAhead { get; set; }
        [XmlAttribute] 
        public int MaxDaysBehind { get; set; }
    }
    /// <summary>
    /// Configuration Value type. Specifies the standard type for the value in this field
    /// </summary>
    public enum ValueType
    {
        /// <summary>
        /// string
        /// </summary>
        ConfigValueType_String = 1,
        /// <summary>
        /// bool
        /// </summary>
        ConfigValueType_Bool = 2,
        /// <summary>
        /// int
        /// </summary>
        ConfigValueType_Int32 = 3,
        /// <summary>
        /// long
        /// </summary>
        ConfigValueType_Int64 = 4,
        /// <summary>
        /// decimal
        /// </summary>
        ConfigValueType_Decimal = 5,
        /// <summary>
        /// double
        /// </summary>
        ConfigValueType_Double = 6
    }
}
