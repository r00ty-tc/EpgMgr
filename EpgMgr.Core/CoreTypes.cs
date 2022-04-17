using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public PluginConfigEntry() { }

        public PluginConfigEntry(string id, string name, string dllFile, string? consoleId = null)
        {
            Id = id;
            Name = name;
            DllFile = dllFile;
            ConsoleId = consoleId ?? name;
        }
    }

    [XmlType]
    public class Config
    {
        public List<PluginConfigEntry> EnabledPlugins { get; set; }
        public string XmlTvFilename { get; set; }
        public ConfigXmlTv XmlTvConfig { get; set; }

        public Config()
        {
            EnabledPlugins = new List<PluginConfigEntry>();
            XmlTvConfig = new ConfigXmlTv
            {
                DateMode = 0,
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
        }
    }

    [XmlType(TypeName = "XmlTv")]
    public class ConfigXmlTv
    {
        [XmlElement]
        public string Filename { get; set; }
        [XmlAttribute(AttributeName = "datemode")]
        public int DateMode { get; set; } // 0 = Offset, 1 = UTC
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
