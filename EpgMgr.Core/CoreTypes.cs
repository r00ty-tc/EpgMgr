using System.Xml.Serialization;
using NodaTime;
using NodaTime.TimeZones;

namespace EpgMgr
{
    /// <summary>
    /// Generic plugin configuration entry. This contains the information needed to load the plugin
    /// </summary>
    [XmlType(TypeName = "Plugin")]
    public class PluginConfigEntry
    {
        /// <summary>
        /// Plugin ID (guid)
        /// </summary>
        [XmlAttribute]
        public string Id { get; set; }
        /// <summary>
        /// Plugin name
        /// </summary>
        [XmlText]
        public string Name { get; set; }
        /// <summary>
        /// DLL File for plugin
        /// </summary>
        [XmlAttribute]
        public string DllFile { get; set; }
        /// <summary>
        /// The name used in the console for this plugin. If not specified Name will be used.
        /// </summary>
        [XmlAttribute]
        public string ConsoleId { get; set; }

        /// <summary>
        /// Create new plugin entry. Only really used by deserializer
        /// </summary>
        public PluginConfigEntry()
        {
            Id = string.Empty;
            Name = string.Empty;
            DllFile = string.Empty;
            ConsoleId = string.Empty;
        }

        /// <summary>
        /// Create new plugin entry with specified values. consoleId will be the same as name if not specified.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="dllFile"></param>
        /// <param name="consoleId"></param>
        public PluginConfigEntry(string id, string name, string dllFile, string? consoleId = null)
        {
            Id = id;
            Name = name;
            DllFile = dllFile;
            ConsoleId = consoleId ?? name;
        }
    }

    /// <summary>
    /// Channel alias class. Describes a channel alias entry
    /// </summary>
    public class ChannelAlias
    {
        /// <summary>
        /// The channel name for this item
        /// </summary>
        [XmlAttribute(AttributeName = "channel-name")]
        public string ChannelName { get; set; }
        /// <summary>
        /// The channel alias for this item
        /// </summary>
        [XmlText]
        public string Alias { get; set; }

        /// <summary>
        /// Create a new alias entry with the specified values
        /// </summary>
        /// <param name="channelName"></param>
        /// <param name="alias"></param>
        public ChannelAlias(string channelName, string alias)
        {
            ChannelName = channelName;
            Alias = alias;
        }

        /// <summary>
        /// Create a new alias entry. Used when de-serializing
        /// </summary>
        public ChannelAlias()
        {
            ChannelName = string.Empty;
            Alias = string.Empty;

        }
    }

    /// <summary>
    /// Master configuration. Serialized/Deserialized to Config.xml
    /// </summary>
    [XmlType]
    public class Config
    {
        /// <summary>
        /// List of enabled plugin entries.
        /// </summary>
        public List<PluginConfigEntry> EnabledPlugins { get; set; }
        /// <summary>
        /// XMLTV Configuration options
        /// </summary>
        public ConfigXmlTv XmlTvConfig { get; set; }
        /// <summary>
        /// Lookup table for channel name to alias. Not serialized
        /// </summary>
        [XmlIgnore]
        public Dictionary<string, string> ChannelNameToAlias { get; set; }
        /// <summary>
        /// Lookup table for alias to channel name. Not serialized
        /// </summary>
        [XmlIgnore]
        public Dictionary<string, string> ChannelAliasToName { get; set; }

        /// <summary>
        /// Serialized version of the aliases, created from the lookup tables above and when loading config will generate the lookup tables
        /// </summary>
        public List<ChannelAlias> ChannelAliases { get; set; }

        /// <summary>
        /// Get new configuration with default values
        /// </summary>
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
                MaxDaysAhead = 5,
                TimeZone = Core.GetLocalTimezone()
            };
            ChannelNameToAlias = new Dictionary<string, string>();
            ChannelAliasToName = new Dictionary<string, string>();
            ChannelAliases = new List<ChannelAlias>();
        }

        /// <summary>
        /// Operations run after the configuration is loaded and de-serialized
        /// </summary>
        public void PostLoadConfig()
        {
            ChannelNameToAlias = ChannelAliases.ToDictionary(row => row.ChannelName, row => row.Alias);
            ChannelAliasToName = ChannelAliases.ToDictionary(row => row.Alias, row => row.ChannelName);
        }

        /// <summary>
        /// Operations run before the configuration is serialized and saved.
        /// </summary>
        public void PreSaveConfig()
        {
            // So dumb that I can't do this as a property setter
            ChannelAliases = ChannelNameToAlias.Select(row => new ChannelAlias(row.Key, row.Value)).ToList();
        }
    }

    /// <summary>
    /// XMLTV Configuration items
    /// </summary>
    [XmlType(TypeName = "XmlTv")]
    public class ConfigXmlTv
    {
        /// <summary>
        /// Get new XMLTV condfiguration items
        /// </summary>
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
            // Local timezone ID by default
            TimeZone = Core.GetLocalTimezone();
        }

        /// <summary>
        /// The XMLTV filename that will be created
        /// </summary>
        [XmlElement]
        public string Filename { get; set; }
        /// <summary>
        /// Include programme credits. The plguins need to honour this.
        /// </summary>
        [XmlAttribute]
        public bool IncludeProgrammeCredits { get; set; }
        /// <summary>
        /// Include programme categories. The plguins need to honour this.
        /// </summary>
        [XmlAttribute]
        public bool IncludeProgrammeCategories { get; set; }
        /// <summary>
        /// Include programme icons. The plguins need to honour this.
        /// </summary>
        [XmlAttribute]
        public bool IncludeProgrammeIcons { get; set; }
        /// <summary>
        /// Include programme ratings. The plguins need to honour this.
        /// </summary>
        [XmlAttribute]
        public bool IncludeProgrammeRatings { get; set; }
        /// <summary>
        /// Include star ratings. The plguins need to honour this.
        /// </summary>
        [XmlAttribute]
        public bool IncldeProgrammeStarRatings { get; set; }
        /// <summary>
        /// Include programme reviews. The plguins need to honour this.
        /// </summary>
        [XmlAttribute]
        public bool IncludeProgrammeReviews { get; set; }
        /// <summary>
        /// Include programme images. The plguins need to honour this.
        /// </summary>
        [XmlAttribute]
        public bool IncludeProgrammeImages { get; set; }

        /// <summary>
        /// The maximum number of days ahead to attempt to fetch programmes for.
        /// </summary>
        [XmlAttribute] 
        public int MaxDaysAhead { get; set; }
        /// <summary>
        /// The maximum number of days behind for which programmes will be kept in the xmltv file for.
        /// </summary>
        [XmlAttribute] 
        public int MaxDaysBehind { get; set; }
        /// <summary>
        /// Time zone to be used when creating the XMLTV file. Uses Noda (IANA) timezone name
        /// </summary>
        [XmlAttribute]
        public string TimeZone { get; set; }
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
