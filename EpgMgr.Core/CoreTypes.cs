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

        public PluginConfigEntry() { }

        public PluginConfigEntry(string id, string name, string dllFile)
        {
            Id = id;
            Name = name;
            DllFile = dllFile;
        }
    }

    [Serializable]
    public class Config
    {
        public List<PluginConfigEntry> EnabledPlugins { get; set; }

        public Config()
        {
            EnabledPlugins = new List<PluginConfigEntry>();
        }
    }
}
