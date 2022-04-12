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
                IncludeProgrammeReviews = true
            };
        }
    }

    [XmlType]
    public class ConfigXmlTv
    {
        public int DateMode; // 0 = Offset, 1 = UTC
        public bool IncludeProgrammeCredits;
        public bool IncludeProgrammeCategories;
        public bool IncludeProgrammeIcons;
        public bool IncludeProgrammeRatings;
        public bool IncldeProgrammeStarRatings;
        public bool IncludeProgrammeReviews;
        public bool IncludeProgrammeImages;
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
