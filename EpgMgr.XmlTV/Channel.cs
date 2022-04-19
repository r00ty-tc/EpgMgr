using System.Xml.Serialization;
using EpgMgr.XmlTV;

namespace EpgMgr
{
    [XmlType(TypeName = "channel")]
    public class Channel
    {
        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }
        [XmlElement(ElementName = "display-name")]
        public List<TextWithLang> DisplayNames { get; set; }
        [XmlElement(ElementName = "icon")]
        public List<Icon> Icons { get; set; }
        [XmlElement(ElementName = "url")]
        public List<XmlTvUrl> Urls { get; set; }

        [XmlIgnore]
        public Dictionary<DateTimeOffset, Programme> Programmes { get; set; }

        // Custom attributes/elements. Plugins that need extra attribs will need to either add here or use the generic ones provided
        [XmlAttribute(AttributeName = "sky-sid")]
        public string? SkySID;

        // Generic Attributes for use by plugins
        [XmlAttribute(AttributeName = "plugin-customattr-A")]
        public string? PluginCustomAttrA;
        [XmlAttribute(AttributeName = "plugin-customattr-B")]
        public string? PluginCustomAttrB;
        [XmlAttribute(AttributeName = "plugin-customattr-C")]
        public string? PluginCustomAttrC;
        [XmlAttribute(AttributeName = "plugin-customattr-D")]
        public string? PluginCustomAttrD;
        [XmlAttribute(AttributeName = "plugin-customattr-E")]
        public string? PluginCustomAttrE;
        [XmlAttribute(AttributeName = "plugin-customattr-F")]
        public string? PluginCustomAttrF;

        // Generic Elements for use by plugins
        [XmlElement(ElementName = "PluginCustomElementA")]
        public string? PluginCustomElementA;
        [XmlElement(ElementName = "PluginCustomElementB")]
        public string? PluginCustomElementB;
        [XmlElement(ElementName = "PluginCustomElementC")]
        public string? PluginCustomElementC;
        [XmlElement(ElementName = "PluginCustomElementD")]
        public string? PluginCustomElementD;
        [XmlElement(ElementName = "PluginCustomElementE")]
        public string? PluginCustomElementE;
        [XmlElement(ElementName = "PluginCustomElementF")]
        public string? PluginCustomElementF;

        public Channel(string id, string? displayName = null, string? lang = null, string? iconSource = null, int? iconWidth = null, int? iconHeight = null, string? url = null, string? urlSystem = null)
        {
            Id = id;
            DisplayNames = new List<TextWithLang>();
            if (displayName != null)
                DisplayNames.Add(new TextWithLang(displayName, lang));
            Icons = new List<Icon>();
            if (iconSource != null)
                Icons.Add(new Icon(iconSource, iconWidth, iconHeight));
            Urls = new List<XmlTvUrl>();
            if (url != null)
                Urls.Add(new XmlTvUrl(url, urlSystem));

            SkySID = null;
            PluginCustomAttrA = null;
            PluginCustomAttrB = null;
            PluginCustomAttrC = null;
            PluginCustomAttrD = null;
            PluginCustomAttrE = null;
            PluginCustomAttrF = null;
            PluginCustomElementA = null;
            PluginCustomElementB = null;
            PluginCustomElementC = null;
            PluginCustomElementD = null;
            PluginCustomElementE = null;
            PluginCustomElementF = null;
            Programmes = new Dictionary<DateTimeOffset, Programme>();
        }

        public Channel()
        {
            Programmes = new Dictionary<DateTimeOffset, Programme>();
        }

        public void AddDisplayName(string displayName, string? lang) => DisplayNames.Add(new TextWithLang(displayName, lang));

        public void AddIcon(string source, int? width = null, int? height = null, string? value = null) =>
            Icons.Add(new Icon(source, width, height, value));

        public void AddUrl(string url, string? system) => Urls.Add(new XmlTvUrl(url, system));
    }
}
