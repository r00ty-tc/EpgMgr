﻿using System.Xml.Serialization;

namespace EpgMgr.XmlTV
{
    [XmlType]
    public class EmptyElement
    {
        [XmlText]
        public string? Empty;

        public EmptyElement() { }
    }

    [XmlType(TypeName = "url")]
    public class XmlTvUrl
    {
        [XmlText] 
        public string Url { get; set; }

        [XmlAttribute(AttributeName = "system")]
        public string? System { get; set; }

        public XmlTvUrl()
        {
            Url = string.Empty;
        }

        public XmlTvUrl(string url, string? system = null)
        {
            Url = url;
            System = system;
        }
    }

    [XmlType]
    public class TextWithLang
    {
        [XmlAttribute(AttributeName = "lang")] public string? Lang { get; set; }
        [XmlText] public string DisplayName { get; set; }

        public TextWithLang(string name, string? lang)
        {
            Lang = lang;
            DisplayName = name;
        }

        public TextWithLang()
        {
            DisplayName = string.Empty;
        }
    }

    [XmlType(TypeName = "icon")]
    public class Icon
    {
        [XmlAttribute(AttributeName = "src")] public string Source { get; set; }
        [XmlIgnore] public int? Width { get; set; }
        [XmlIgnore] public int? Height { get; set; }
        [XmlText] public string? Value { get; set; }

        [XmlAttribute(AttributeName = "width")]
        public string? WidthXml
        {
            get => Width?.ToString();
            set
            {
                if (value == null)
                    Width = null;
                else
                    Width = int.Parse(value);
            }
        }

        [XmlAttribute(AttributeName = "height")]
        public string? HeightXml
        {
            get => Height?.ToString();
            set
            {
                if (value == null)
                    Height = null;
                else
                    Height = int.Parse(value);
            }
        }

        public Icon(string source, int? width = null, int? height = null, string? value = null)
        {
            Source = source;
            Width = width;
            Height = height;
            Value = value;
        }

        public Icon()
        {
            Source = string.Empty;
        }

    }

    [XmlType(TypeName = "image")]
    public class Image
    {
        [XmlText] 
        public string Url { get; set; }
        [XmlAttribute(AttributeName = "type")] 
        public string? Type { get; set; }

        [XmlAttribute(AttributeName = "orient")]
        public string? Orientation { get; set; }

        [XmlAttribute(AttributeName = "system")]
        public string? System { get; set; }

        [XmlAttribute(AttributeName = "size")]
        public string? SizeXml { get; set; }

        [XmlIgnore]
        public int? Size
        {
            get => Size = SizeXml == null ? null : int.Parse(SizeXml);
            set => SizeXml = value?.ToString();
        }

        public Image()
        {
            Url = string.Empty;
        }

        public Image(string url, string? type = null, int? size = null, string? orientation = null,
            string? system = null)
        {
            Url = url;
            Type = type;
            Size = size;
            Orientation = orientation;
            System = system;
        }
    }

    [XmlType]
    public class ValueIcon
    {
        [XmlElement(ElementName = "value")]
        public string Value { get; set; }
        [XmlElement(ElementName = "icon")]
        public Icon Icon { get; set; }

        public ValueIcon()
        {
            Value = string.Empty;
            Icon = new Icon();
        }
        public ValueIcon(Icon icon, string value)
        {
            Icon = icon;
            Value = value;
        }
    }
}