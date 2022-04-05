using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
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
        public List<Programme> Programmes { get; set; }

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

            Programmes = new List<Programme>();
        }

        public Channel()
        {
            Programmes = new List<Programme>();
        }

        public void AddDisplayName(string displayName, string? lang) => DisplayNames.Add(new TextWithLang(displayName, lang));

        public void AddIcon(string source, int? width = null, int? height = null, string? value = null) =>
            Icons.Add(new Icon(source, width, height, value));

        public void AddUrl(string url, string? system) => Urls.Add(new XmlTvUrl(url, system));
    }
}
