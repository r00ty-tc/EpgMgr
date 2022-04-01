using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace EpgMgr.Plugins
{
    /// <summary>
    /// Custom Tag type used in Channels to store ad-hoc info
    /// </summary>
    public class CustomTag
    {
        [XmlAttribute]
        public string Key { get; set; }
        [XmlText]
        public string Value { get; set; }
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

        public CustomTag() { }
    }

    // All this configuration definition is with an aim to allow both a generic configuration possibility via console and later potentially GUI
    /// <summary>
    /// Configuration Entry type. Root is folder, and folders can be used to organise structure, List is a list of objects, Entry is an entry with fixed values
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
    /// Configuration Value type. Specifies the standard type for the value in this field
    /// </summary>
    public enum ConfigValueType
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

    /// <summary>
    /// Configuration Entry. A nested confuration entry. The root of which is the master configuration
    /// </summary>
    //[XmlType(TypeName = "PluginConfig")]
    [Serializable]
    public class ConfigEntry
    {
        [XmlAttribute(AttributeName = "Id")]
        public string? PluginId { get; set; }
        [XmlAttribute(AttributeName = "Name")]
        public string? PluginName { get; set; }
        [XmlElement(ElementName = "Folders")]
        public List<ConfigEntry>? ConfigFolders { get; set; }
        [XmlElement(ElementName = "Entry")]
        public List<ConfigEntry>? ConfigEntries { get; set; }
        [XmlElement(ElementName = "Item")]
        public List<dynamic>? ObjectList { get; set; }

        [XmlIgnore]
        public ConfigEntryType? ConfigType { get; set; }
        [XmlAttribute]
        public string? Key { get; set; }
        [XmlIgnore]
        public ConfigValueType? ValueType { get; set; }
        [XmlElement]
        public dynamic? Value { get; set; }
        [XmlAttribute]
        public string? Path { get; set; }
        [XmlAttribute]
        public string? ConsoleId { get; set; }
        [XmlIgnore]
        public ConfigEntry? RootEntry { get; }
        [XmlIgnore]
        public ConfigEntry? ParentEntry { get; }

        /*[XmlElement(ElementName = "Value")]
        public dynamic? ValueXml
        {
            set
            {
                var xmlElement = value;
                if (xmlElement == null)
                    Value = value;
                else
                {
                    Value = xmlElement[1].Value;
                }

            }
            get => Value;
        }*/

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

        [XmlAttribute("ConfigValueType")]
        public string? ConfigValueTypeXml
        {
            get => ValueType.HasValue ? ((int)ValueType).ToString() : null;
            set
            {
                if (value == null)
                    ValueType = null;
                else
                    ValueType = (ConfigValueType)int.Parse(value);
            }
        }

        public ConfigEntry()
        {
            ConfigFolders = new List<ConfigEntry>();
            ConfigEntries = new List<ConfigEntry>();
        }

        /// <summary>
        /// Create new Configuration entry. Used to creatr root node
        /// </summary>
        /// <param name="parentEntry"></param>
        /// <param name="pluginId"></param>
        /// <param name="pluginName"></param>
        public ConfigEntry(ConfigEntry? parentEntry = null, string? pluginId = null, string? pluginName = null)
        {
            PluginId = pluginId;
            PluginName = pluginName;
            ParentEntry = parentEntry;
            RootEntry = parentEntry != null ? parentEntry.FindRootRecursive() : this;
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
            ConfigValueType? thisType = null;
            if (typeof(T) == typeof(string))
                thisType = ConfigValueType.ConfigValueType_String;
            else if (typeof(T) == typeof(int))
                thisType = ConfigValueType.ConfigValueType_Int32;
            else if (typeof(T) == typeof(bool))
                thisType = ConfigValueType.ConfigValueType_Bool;
            else if (typeof(T) == typeof(decimal))
                thisType = ConfigValueType.ConfigValueType_Decimal;
            else if (typeof(T) == typeof(double))
                thisType = ConfigValueType.ConfigValueType_Double;
            else if (typeof(T) == typeof(long))
                thisType = ConfigValueType.ConfigValueType_Int64;
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
            if (typeof(T) == typeof(string) && ValueType == ConfigValueType.ConfigValueType_String) return (T?)Value;
            if (typeof(T) == typeof(int) && ValueType == ConfigValueType.ConfigValueType_Int32) return int.Parse(Value);
            if (typeof(T) == typeof(long) && ValueType == ConfigValueType.ConfigValueType_Int64) return long.Parse(Value);
            if (typeof(T) == typeof(bool) && ValueType == ConfigValueType.ConfigValueType_Bool) return bool.Parse(Value);
            if (typeof(T) == typeof(decimal) && ValueType == ConfigValueType.ConfigValueType_Decimal) return decimal.Parse(Value);
            if (typeof(T) == typeof(double) && ValueType == ConfigValueType.ConfigValueType_Double) return double.Parse(Value);
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
            if (typeof(T) == typeof(string) && ValueType == ConfigValueType.ConfigValueType_String) Value = value;
            else if (typeof(T) == typeof(int) && ValueType == ConfigValueType.ConfigValueType_Int32) Value = value;
            else if (typeof(T) == typeof(long) && ValueType == ConfigValueType.ConfigValueType_Int64) Value = value;
            else if (typeof(T) == typeof(bool) && ValueType == ConfigValueType.ConfigValueType_Bool) Value = value;
            else if (typeof(T) == typeof(decimal) && ValueType == ConfigValueType.ConfigValueType_Decimal) Value = value;
            else if (typeof(T) == typeof(double) && ValueType == ConfigValueType.ConfigValueType_Double) Value = value;
            else throw new ArgumentException($"Invalid type {typeof(T)} passed as argument when expecting {ValueType}");
        }

        /// <summary>
        /// Retrieve the list from a list config entry, converted back to the specified type (provided it was the original type)
        /// </summary>
        /// <param name="key"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public List<T>? GetList<T>(string key)
        {
            var objList = ConfigEntries?.FirstOrDefault(row => row.Key.Equals(key))?.ObjectList?.Cast<T>();
            return (List<T>?)objList.ToList();
        }

        /// <summary>
        /// Set the list value on a list config entry to supplied list of specified type
        /// </summary>
        /// <param name="key"></param>
        /// <param name="list"></param>
        /// <typeparam name="T"></typeparam>
        public void SetList<T>(string key, List<T> list)
        {
            var entry = ConfigEntries?.FirstOrDefault(row => row.Key.Equals(key));
            entry.ObjectList = list.Cast<dynamic>().ToList();
        }
    }

    [Serializable]
    public class Channel
    {
        [XmlAttribute]
        public string Id { get; set; }
        [XmlAttribute(AttributeName = "Key")]
        public string LookupKey { get; set; }
        [XmlAttribute]
        public string? LineupId { get; set; }
        [XmlText]
        public string? Name { get; set; }
        [XmlAttribute]
        public string? Callsign { get; set; }
        [XmlAttribute]
        public string? Affiliate { get; set; }
        [XmlAttribute]
        public string? LogoUrl { get; set; }
        [XmlIgnore]
        public int? ChannelNo { get; set; }
        [XmlIgnore]
        public int? SubChannelNo { get; set; }
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
        public List<CustomTag>? CustomTags { get; set; }

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

        public Channel() { }

        public void AddTag(string key, string value, bool includeInXml = false)
        {
            CustomTags ??= new List<CustomTag>();
            CustomTags.Add(new CustomTag(key, value, includeInXml));
        }

        public CustomTag? GetTag(string key) => CustomTags.FirstOrDefault(row => row.Key.Equals(key));
        public void RemoveTag(string key)
        {
            var tag = GetTag(key);
            if (tag != null)
                CustomTags.Remove(tag);
        } 
        public bool HasTag(string key) => CustomTags.Any(row => row.Key.Equals(key));
    }

    public class PluginErrors
    {
        public bool HasErrors => Errors.Any();
        public bool HasWarnings => Warnings.Any();
        public List<string> Errors { get; set; }
        public List<string> Warnings { get; set; }
        public List<string> InformationMessages { get; set; }
        public List<string> DebugMessages { get; set; }

        public PluginErrors()
        {
            Errors = new List<string>();
            Warnings = new List<string>();
            InformationMessages = new List<string>();
            DebugMessages = new List<string>();
        }

        public void AddError(string message) => Errors.Add(message);
        public void AddWarning(string message) => Warnings.Add(message);
        public void AddInfoMessage(string message) => InformationMessages.Add(message);
        public void AddDebugMessage(string message) => DebugMessages.Add(message);
    }
}
