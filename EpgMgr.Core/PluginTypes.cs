using System.Xml.Serialization;

namespace EpgMgr.Plugins
{
    /// <summary>
    /// Custom Tag type used in Channels to store ad-hoc info
    /// </summary>
    public class CustomTag
    {
        /// <summary>
        /// Custom tag key
        /// </summary>
        [XmlAttribute]
        public string Key { get; set; }
        /// <summary>
        /// Custom tag value
        /// </summary>
        [XmlText]
        public string Value { get; set; }
        /// <summary>
        /// @ToDo: Flag to include in XML (NYI)
        /// </summary>
        [XmlAttribute]
        public bool IncludeInXml { get; set; }

        /// <summary>
        /// Create a new channel data tag with supplied values
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="includeInXml"></param>
        public CustomTag(string key, string value, bool includeInXml = false)
        {
            Key = key;
            Value = value;
            IncludeInXml = includeInXml;
        }

        /// <summary>
        /// Create a new channel data tah, used by serialization
        /// </summary>
        public CustomTag()
        {
            Key = string.Empty;
            Value = string.Empty;
        }
    }

    // All this configuration definition is with an aim to allow both a generic configuration possibility via console and later potentially GUI
    /// <summary>
    /// Configuration Entry type. Root is folder, and folders can be used to organise structure, List is a list of objects, Entry is an entry with fixed values.
    /// 1: Folder
    /// 2: List
    /// 3: Config entry
    /// </summary>
    public enum ConfigEntryType
    {
        /// <summary>
        /// Folder Config entry type
        /// </summary>
        ConfigEntryType_Folder = 1,
        /// <summary>
        /// List Config entry type
        /// </summary>
        ConfigEntryType_List = 2,
        /// <summary>
        /// Generic Config entry type
        /// </summary>
        ConfigEntryType_ConfigEntry = 3
    }

    /// <summary>
    /// Configuration Entry. A nested confuration entry. The root of which is the master configuration
    /// </summary>
    //[XmlType(TypeName = "PluginConfig")]
    //[Serializable]
    [XmlRoot("PluginConfig")]
    public class ConfigEntry
    {
        /// <summary>
        /// Plugin ID
        /// </summary>
        [XmlAttribute(AttributeName = "Id")]
        public string? PluginId { get; set; }
        /// <summary>
        /// Plugin Name
        /// </summary>
        [XmlAttribute(AttributeName = "Name")]
        public string? PluginName { get; set; }
        /// <summary>
        /// List of folders below this one
        /// </summary>
        [XmlElement(ElementName = "Folders")]
        public List<ConfigEntry>? ConfigFolders { get; set; }
        /// <summary>
        /// List of entries in this config folder
        /// </summary>
        [XmlElement(ElementName = "Entry")]
        public List<ConfigEntry>? ConfigEntries { get; set; }
        /// <summary>
        /// List of data items
        /// </summary>
        [XmlElement(ElementName = "Item")]
        public List<dynamic>? ObjectList { get; set; }

        /// <summary>
        /// Entry type (Folder, list or entry)
        /// </summary>
        [XmlIgnore]
        public ConfigEntryType? ConfigType { get; set; }
        /// <summary>
        /// Configuration entry key
        /// </summary>
        [XmlAttribute]
        public string? Key { get; set; }
        /// <summary>
        /// Config value type
        /// </summary>
        [XmlIgnore]
        public ValueType? ValueType { get; set; }
        /// <summary>
        /// Config value
        /// </summary>
        public dynamic? Value { get; set; }
        /// <summary>
        /// Config path
        /// </summary>
        [XmlAttribute]
        public string? Path { get; set; }
        /// <summary>
        /// Console ID
        /// </summary>
        [XmlAttribute]
        public string? ConsoleId { get; set; }
        /// <summary>
        /// @ToDo: Hidden in console? (NYI)
        /// </summary>
        [XmlAttribute]
        public bool ConsoleHidden { get; set; }
        /// <summary>
        /// Root configuration entry
        /// </summary>
        [XmlIgnore]
        public ConfigEntry? RootEntry { get; }
        /// <summary>
        /// Parent configuration entry
        /// </summary>
        [XmlIgnore]
        public ConfigEntry? ParentEntry { get; }

        /// <summary>
        /// Config Entry Type: XML String
        /// </summary>
        [XmlAttribute("ConfigEntryType")]
        public string? ConfigEntryTypeXml
        {
            get => ConfigType.HasValue ? ((int)ConfigType).ToString() : null;
            set
            {
                if (value == null)
                    ConfigType = null;
                else
                    ConfigType = (ConfigEntryType)int.Parse(value);
            }
        }

        /// <summary>
        /// Config Value Type: XML String
        /// </summary>
        [XmlAttribute("ValueType")]
        public string? ConfigValueTypeXml
        {
            get => ValueType.HasValue ? ((int)ValueType).ToString() : null;
            set
            {
                if (value == null)
                    ValueType = null;
                else
                    ValueType = (ValueType)int.Parse(value);
            }
        }

        /// <summary>
        /// Crate new Config Entry (mainly for serialization use)
        /// </summary>
        public ConfigEntry()
        {
            ConfigFolders = new List<ConfigEntry>();
            ConfigEntries = new List<ConfigEntry>();
        }

        /// <summary>
        /// Create new Configuration entry. Used to create root node
        /// </summary>
        /// <param name="parentEntry"></param>
        /// <param name="pluginId"></param>
        /// <param name="pluginName"></param>
        /// <param name="consoleHidden"></param>
        public ConfigEntry(ConfigEntry? parentEntry = null, string? pluginId = null, string? pluginName = null, bool consoleHidden = false)
        {
            PluginId = pluginId;
            PluginName = pluginName;
            ParentEntry = parentEntry;
            RootEntry = parentEntry != null ? parentEntry.FindRootRecursive() : this;
            ConsoleHidden = consoleHidden;
            ConfigType = ConfigEntryType.ConfigEntryType_Folder;
            ConfigFolders = new List<ConfigEntry>();
            ConfigEntries = new List<ConfigEntry>();
        }

        /// <summary>
        /// Create new Folder Config entry and either add it to the specified folder, or otherwise just return it
        /// </summary>
        /// <param name="key"></param>
        /// <param name="consoleId"></param>
        /// <param name="folder"></param>
        /// <returns></returns>
        public static ConfigEntry NewConfigFolder(string key, string? consoleId = null, ConfigEntry? folder = null)
        {
            var entry = new ConfigEntry(folder)
            {
                ConfigFolders = new List<ConfigEntry>(),
                ConfigEntries = new List<ConfigEntry>(),
                ObjectList = null,
                ConfigType = ConfigEntryType.ConfigEntryType_Folder,
                Key = key,
                ConsoleId = consoleId ?? key,
                Value = null,
                ValueType = null,
            };

            folder?.ConfigFolders?.Add(entry);

            return entry;
        }

        /// <summary>
        /// Create new Generic Config entry and either add it to the specified folder, or otherwise just return it
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="consoleId"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static ConfigEntry NewConfigEntry<T>(ConfigEntry folder, string key, T? value = default(T),
            string? consoleId = null)
        {
            ValueType? thisType = null;
            if (typeof(T) == typeof(string))
                thisType = EpgMgr.ValueType.ConfigValueType_String;
            else if (typeof(T) == typeof(int))
                thisType = EpgMgr.ValueType.ConfigValueType_Int32;
            else if (typeof(T) == typeof(bool))
                thisType = EpgMgr.ValueType.ConfigValueType_Bool;
            else if (typeof(T) == typeof(decimal))
                thisType = EpgMgr.ValueType.ConfigValueType_Decimal;
            else if (typeof(T) == typeof(double))
                thisType = EpgMgr.ValueType.ConfigValueType_Double;
            else if (typeof(T) == typeof(long))
                thisType = EpgMgr.ValueType.ConfigValueType_Int64;
            else
                throw new Exception("Invalid config type");

            var entry = new ConfigEntry(folder)
            {
                ConfigFolders = null,
                ConfigEntries = null,
                ObjectList = null,
                ConfigType = ConfigEntryType.ConfigEntryType_ConfigEntry,
                Key = key,
                ConsoleId = consoleId ?? key,
                Value = value,
                ValueType = thisType,
            };

            folder.ConfigEntries?.Add(entry);
            return entry;
        }

        /// <summary>
        /// Create new List Config entry and either add it to the specified folder, or otherwise just return it
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="key"></param>
        /// <param name="consoleId"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        public static ConfigEntry NewConfigList(ConfigEntry folder, string key, string? consoleId = null,
            IEnumerable<dynamic>? list = null)
        {
            var entry = new ConfigEntry(folder)
            {
                ConfigFolders = null,
                ConfigEntries = null,
                ObjectList = list?.ToList(),
                ConfigType = ConfigEntryType.ConfigEntryType_List,
                Key = key,
                ConsoleId = consoleId ?? key,
                Value = null,
                ValueType = null
            };

            folder.ConfigEntries?.Add(entry);
            return entry;
        }

        /// <summary>
        /// Search the root node for this config entry
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        protected ConfigEntry FindRootRecursive(ConfigEntry? folder = null)
        {
            while (true)
            {
                folder = folder ?? this;
                if (folder.ParentEntry == null) return folder;
                folder = folder.ParentEntry;
            }
        }

        /// <summary>
        /// Attempt to retrieve value back to specified type, only if it was the stored type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T? GetValue<T>()
        {
            if (typeof(T) == typeof(string) && ValueType == EpgMgr.ValueType.ConfigValueType_String) return (T?)Value;
            if (typeof(T) == typeof(int) && ValueType == EpgMgr.ValueType.ConfigValueType_Int32) return (T?)Value;
            if (typeof(T) == typeof(long) && ValueType == EpgMgr.ValueType.ConfigValueType_Int64) return (T?)Value;
            if (typeof(T) == typeof(bool) && ValueType == EpgMgr.ValueType.ConfigValueType_Bool) return (T?)Value;
            if (typeof(T) == typeof(decimal) && ValueType == EpgMgr.ValueType.ConfigValueType_Decimal) return (T?)Value;
            if (typeof(T) == typeof(double) && ValueType == EpgMgr.ValueType.ConfigValueType_Double) return (T?)Value;
            return default(T);
        }

        /// <summary>
        /// Set the value of a generic config entry to a value of the specified type
        /// </summary>
        /// <param name="value"></param>
        /// <typeparam name="T"></typeparam>
        /// <exception cref="ArgumentException"></exception>
        public void SetValue<T>(T value)
        {
            if (typeof(T) == typeof(string) && ValueType == EpgMgr.ValueType.ConfigValueType_String) Value = value;
            else if (typeof(T) == typeof(int) && ValueType == EpgMgr.ValueType.ConfigValueType_Int32) Value = value;
            else if (typeof(T) == typeof(long) && ValueType == EpgMgr.ValueType.ConfigValueType_Int64) Value = value;
            else if (typeof(T) == typeof(bool) && ValueType == EpgMgr.ValueType.ConfigValueType_Bool) Value = value;
            else if (typeof(T) == typeof(decimal) && ValueType == EpgMgr.ValueType.ConfigValueType_Decimal) Value = value;
            else if (typeof(T) == typeof(double) && ValueType == EpgMgr.ValueType.ConfigValueType_Double) Value = value;
            else throw new ArgumentException($"Invalid type {typeof(T)} passed as argument when expecting {ValueType}");
        }

        /// <summary>
        /// Return the value of the specifie value ID
        /// </summary>
        /// <param name="valueId"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T? GetValue<T>(string valueId)
        {
            if (ConfigEntries == null) return default(T);
            var valueObj = this.ConfigEntries.FirstOrDefault(row =>
                row.ConfigType.Equals(ConfigEntryType.ConfigEntryType_ConfigEntry) && row.Key != null && row.Key.Equals(valueId));

            return valueObj != null ? valueObj.GetValue<T>() : default(T);
        }

        /// <summary>
        /// Set the value for the specified ID with the specified value
        /// </summary>
        /// <param name="valueId"></param>
        /// <param name="value"></param>
        /// <typeparam name="T"></typeparam>
        public void SetValue<T>(string valueId, T value)
        {
            if (ConfigEntries == null) return;
            var valueObj = this.ConfigEntries.FirstOrDefault(row =>
                row.ConfigType.Equals(ConfigEntryType.ConfigEntryType_ConfigEntry) && row.Key != null && row.Key.Equals(valueId));

            valueObj?.SetValue<T>(value);
        }

        /// <summary>
        /// Retrieve the list from a list config entry, converted back to the specified type (provided it was the original type)
        /// </summary>
        /// <param name="key"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public List<T>? GetList<T>(string key)
        {
            var objList = ConfigEntries?.FirstOrDefault(row => row.Key!.Equals(key))?.ObjectList?.Cast<T>();
            return objList?.ToList();
        }

        /// <summary>
        /// Set the list value on a list config entry to supplied list of specified type
        /// </summary>
        /// <param name="key"></param>
        /// <param name="list"></param>
        /// <typeparam name="T"></typeparam>
        public void SetList<T>(string key, List<T> list)
        {
            var entry = ConfigEntries?.FirstOrDefault(row => row.Key!.Equals(key));
            if (entry != null)
                entry.ObjectList = list.Cast<dynamic>().ToList();
        }
    }

    /// <summary>
    /// Channel Class
    /// </summary>
    [Serializable]
    public class Channel
    {
        /// <summary>
        /// Channel ID
        /// </summary>
        [XmlAttribute]
        public string Id { get; set; }
        /// <summary>
        /// Channel Lookup Key
        /// </summary>
        [XmlAttribute(AttributeName = "Key")]
        public string LookupKey { get; set; }
        /// <summary>
        /// Channel Lineup ID
        /// </summary>
        [XmlAttribute]
        public string? LineupId { get; set; }
        /// <summary>
        /// Channel Name
        /// </summary>
        [XmlText]
        public string? Name { get; set; }
        /// <summary>
        /// Channel Callsign
        /// </summary>
        [XmlAttribute]
        public string? Callsign { get; set; }
        /// <summary>
        /// Channel Affiliate
        /// </summary>
        [XmlAttribute]
        public string? Affiliate { get; set; }
        /// <summary>
        /// Channel Logo URL
        /// </summary>
        [XmlAttribute]
        public string? LogoUrl { get; set; }
        /// <summary>
        /// Channel Number
        /// </summary>
        [XmlIgnore]
        public int? ChannelNo { get; set; }
        /// <summary>
        /// Sub channel number
        /// </summary>
        [XmlIgnore]
        public int? SubChannelNo { get; set; }
        /// <summary>
        /// Channel Number (XML String)
        /// </summary>
        [XmlAttribute("ChannelNo")]

        public string? ChannelNoXml
        {
            get => ChannelNo?.ToString();
            set
            {
                if (value == null)
                    ChannelNo = null;
                else
                    ChannelNo = int.Parse(value);
            }
        }
        /// <summary>
        /// List of custom tags
        /// </summary>
        public List<CustomTag>? CustomTags { get; set; }

        /// <summary>
        /// Create new instance of a channel.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="lineupId"></param>
        /// <param name="callsign"></param>
        /// <param name="affiliate"></param>
        /// <param name="channelNo"></param>
        /// <param name="subChannelNo"></param>
        /// <param name="logoUrl"></param>
        /// <param name="lookupKey"></param>
        /// <param name="customTags"></param>
        public Channel(string id, string? name = null, string? lineupId = null, string? callsign = null,
            string? affiliate = null, int? channelNo = null, int? subChannelNo = null, string? logoUrl = null, string? lookupKey = null, List<CustomTag>? customTags = null)
        {
            Id = id;
            LineupId = lineupId;
            Name = name;
            Callsign = callsign;
            Affiliate = affiliate;
            ChannelNo = channelNo;
            SubChannelNo = subChannelNo;
            LogoUrl = logoUrl;
            CustomTags = customTags;
            LookupKey = lookupKey ?? id;
        }

        /// <summary>
        /// Create new channel (for serialization)
        /// </summary>
        public Channel()
        {
            Id = string.Empty;
            LookupKey = string.Empty;
        }

        /// <summary>
        /// Add tag to the channel
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="includeInXml"></param>
        public void AddTag(string key, string value, bool includeInXml = false)
        {
            CustomTags ??= new List<CustomTag>();
            CustomTags.Add(new CustomTag(key, value, includeInXml));
        }

        /// <summary>
        /// Get specified tag from this channel
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public CustomTag? GetTag(string key) => CustomTags?.FirstOrDefault(row => row.Key.Equals(key));

        /// <summary>
        /// Remove specified tag from this channel
        /// </summary>
        /// <param name="key"></param>
        public void RemoveTag(string key)
        {
            var tag = GetTag(key);
            if (tag != null)
                CustomTags?.Remove(tag);
        } 
        /// <summary>
        /// Return whether there are any tags present on this channel?
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool HasTag(string key) => CustomTags?.Any(row => row.Key.Equals(key)) ?? false;
    }

    /// <summary>
    /// Class to store plugin errors
    /// </summary>
    public class PluginErrors
    {
        /// <summary>
        /// Return true if there's any errors logged
        /// </summary>
        public bool HasErrors => Errors.Any();
        /// <summary>
        /// Return true if there are any warnings logged
        /// </summary>
        public bool HasWarnings => Warnings.Any();
        /// <summary>
        /// List of error messages
        /// </summary>
        public List<string> Errors { get; set; }
        /// <summary>
        /// List of warning messages
        /// </summary>
        public List<string> Warnings { get; set; }
        /// <summary>
        /// List of information messages
        /// </summary>
        public List<string> InformationMessages { get; set; }
        /// <summary>
        /// List of debug messages
        /// </summary>
        public List<string> DebugMessages { get; set; }

        /// <summary>
        /// Create new plugin errors object
        /// </summary>
        public PluginErrors()
        {
            Errors = new List<string>();
            Warnings = new List<string>();
            InformationMessages = new List<string>();
            DebugMessages = new List<string>();
        }

        /// <summary>
        /// Adds an error message
        /// </summary>
        /// <param name="message"></param>
        public void AddError(string message) => Errors.Add(message);
        /// <summary>
        /// Adds a warning message
        /// </summary>
        /// <param name="message"></param>
        public void AddWarning(string message) => Warnings.Add(message);
        /// <summary>
        /// Adds an information message
        /// </summary>
        /// <param name="message"></param>
        public void AddInfoMessage(string message) => InformationMessages.Add(message);
        /// <summary>
        /// Adds a debug message
        /// </summary>
        /// <param name="message"></param>
        public void AddDebugMessage(string message) => DebugMessages.Add(message);
    }
}
