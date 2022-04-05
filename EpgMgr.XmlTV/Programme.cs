using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace EpgMgr.XmlTV
{
    [XmlType(TypeName = "programme")]
    public class Programme
    {
        [XmlAttribute(AttributeName = "start")]
        public string StartTimeXml { get; set; }
        [XmlAttribute(AttributeName = "stop")]
        public string? StopTimeXml { get; set; }
        [XmlAttribute(AttributeName = "pdc-start")]
        public string? PdcStartXml { get; set; }
        [XmlAttribute(AttributeName = "vps-start")]
        public string? VpsStartXml { get; set; }
        [XmlAttribute(AttributeName = "showview")]
        public string? ShowView { get; set; }
        [XmlAttribute(AttributeName = "videoplus")]
        public string? VideoPlus { get; set; }
        [XmlAttribute(AttributeName = "channel")]
        public string Channel { get; set; }
        [XmlIgnore]
        public bool? ClumpIdx { get; set; }

        [XmlAttribute(AttributeName = "clumpidx")]
        public string? ClumpIdxXml
        {
            get => ClumpIdx?.ToString();
            set => ClumpIdx = value != null ? bool.Parse(value) : null;
        }


        [XmlElement(ElementName = "title")]
        public List<TextWithLang> Titles { get; set; }
        [XmlElement(ElementName = "sub-title")]
        public List<TextWithLang>? Subtitles { get; set; }
        [XmlElement(ElementName = "desc")]
        public List<TextWithLang>? Descriptions { get; set; }
        [XmlElement(ElementName = "credits")]
        public Credits? Credits { get; set; }
        [XmlElement(ElementName = "date")]
        public string? DateXml { get; set; }
        [XmlElement(ElementName = "category")]
        public List<TextWithLang>? Categories { get; set; }
        [XmlElement(ElementName = "keyword")]
        public List<TextWithLang>? Keywords { get; set; }
        [XmlElement(ElementName = "language")]
        public TextWithLang? Language { get; set; }
        [XmlElement(ElementName = "orig-language")]
        public TextWithLang? OrigLanguage { get; set; }

        [XmlElement(ElementName = "length")]
        public Length LengthValue { get; set; }
        [XmlElement(ElementName = "icon")]
        public List<Icon>? Icons { get; set; }
        [XmlElement(ElementName = "url")]
        public List<XmlTvUrl>? Urls { get; set; }
        [XmlElement(ElementName = "country")]
        public List<TextWithLang>? Countries { get; set; }
        [XmlElement(ElementName = "episode-num")]
        public List<EpisodeNum>? EpisodeNumbers { get; set; }
        [XmlElement(ElementName = "video")]
        public Video? Video { get; set; }
        [XmlElement(ElementName = "audio")]
        public Audio? Audio { get; set; }
        [XmlElement(ElementName = "previously-shown")]
        public PreviouslyShown? PreviouslyShown { get; set; }
        [XmlElement(ElementName = "premiere")]
        public TextWithLang? Premiere { get; set; }
        [XmlElement(ElementName = "last-chance")]
        public TextWithLang? LastChance { get; set; }
        [XmlElement(ElementName = "new")]
        public EmptyElement? New { get; set; }
        [XmlElement(ElementName = "subtitles")]
        public List<SubtitleInfo>? TxtSubtitles { get; set; }
        [XmlElement(ElementName = "rating")]
        public List<ValueIcon>? Ratings { get; set; }
        [XmlElement(ElementName = "star-rating")]
        public List<ValueIcon>? StarRatings { get; set; }
        [XmlElement(ElementName = "review")]
        public List<Review>? Reviews { get; set; }
        [XmlElement(ElementName = "image")]
        public List<Image>? Images { get; set; }

        [XmlIgnore]
        public DateTime? Date
        {
            get => DateXml != null ? XmlTV.ParseDateTime(DateXml, true).DateTime : null;
            set => DateXml = value.HasValue ? XmlTV.ParseDateTime(value.Value, true) : null;
        }

        [XmlIgnore]
        public DateTimeOffset StartTime
        {
            get => XmlTV.ParseDateTime(StartTimeXml);
            set => StartTimeXml = XmlTV.ParseDateTime(value);
        }
        [XmlIgnore]
        public DateTimeOffset? StopTime
        {
            get => StopTimeXml != null ? XmlTV.ParseDateTime(StopTimeXml) : null;
            set => StopTimeXml = value.HasValue ? XmlTV.ParseDateTime(value.Value) : null;
        }
        [XmlIgnore]
        public Channel ChannelRef { get; set; }

        public Programme() { }

        public Programme(DateTimeOffset startTime, string channel, string title, DateTimeOffset? stopTime = null, 
            string? subtitle = null, string? description = null, string? language = null, string? category = null, 
            string? titleLang = null, string? subtitleLang = null, string? descriptionLang = null, 
            string? languageLang = null, string? categoryLang = null)
        {
            StartTime = startTime;
            Channel = channel;
            StopTime = stopTime;
            Titles = new List<TextWithLang> { new TextWithLang(title, titleLang) };

            if (subtitle != null)
                Subtitles = new List<TextWithLang> { new TextWithLang(subtitle, subtitleLang) };

            if (description != null)
                Descriptions = new List<TextWithLang> { new TextWithLang(description, descriptionLang) };

            if (language != null)
                Language = new TextWithLang(language, languageLang);

            if (category != null)
                Categories = new List<TextWithLang> { new TextWithLang(category, categoryLang) };
        }

        public void AddTitle(string title, string? lang = null) => Titles.Add(new TextWithLang(title, lang));

        public void AddSubtitle(string subtitle, string? lang = null)
        {
            Subtitles ??= new List<TextWithLang>();
            Subtitles.Add(new TextWithLang(subtitle, lang));
        }

        public void AddDescription(string description, string? lang = null)
        {
            Descriptions ??= new List<TextWithLang>();
            Descriptions.Add(new TextWithLang(description, lang));
        }

        public void AddCategory(string category, string? lang = null)
        {
            Categories ??= new List<TextWithLang>();
            Categories.Add(new TextWithLang(category, lang));
        }

        public void AddKeyword(string keyword, string? lang = null)
        {
            Keywords ??= new List<TextWithLang>();
            Keywords.Add(new TextWithLang(keyword, lang));
        }

        public void AddIcon(string source, int? width, int? height)
        {
            Icons ??= new List<Icon>();
            Icons.Add(new Icon(source, width, height));
        }

        public void AddUrl(string url, string? system)
        {
            Urls ??= new List<XmlTvUrl>();
            Urls.Add(new XmlTvUrl(url, system));
        }

        public void AddCountry(string country, string? lang)
        {
            Countries ??= new List<TextWithLang>();
            Countries.Add(new TextWithLang(country, lang));
        }

        public void AddEpisodeNum(string value, string system = "onscreen")
        {
            EpisodeNumbers ??= new List<EpisodeNum>();
            EpisodeNumbers.Add(new EpisodeNum(value, system));
        }

        public void AddSubtitleInfo(string? type, string? language)
        {
            TxtSubtitles ??= new List<SubtitleInfo>();
            TxtSubtitles.Add(new SubtitleInfo(type, language));
        }

        public void AddRating(Icon icon, string value)
        {
            Ratings ??= new List<ValueIcon>();
            Ratings.Add(new ValueIcon(icon, value));
        }

        public void AddStarRating(Icon icon, string value)
        {
            StarRatings ??= new List<ValueIcon>();
            StarRatings.Add(new ValueIcon(icon, value));
        }

        public void AddReview(string type, string value, string? source, string? reviewer, string? lang)
        {
            Reviews ??= new List<Review>();
            Reviews.Add(new Review(type, value, source, reviewer, lang));
        }

        public void AddImage(string url, string? type = null, int? size = null, string? orientation = null,
            string? system = null)
        {
            Images ??= new List<Image>();
            Images.Add(new Image(url, type, size, orientation, system));
        }
    }

    [XmlType(TypeName = "length")]
    public class Length
    {
        [XmlText]
        public int Value { get; set; }
        [XmlAttribute(AttributeName = "units")]
        public string Units { get; set; }

        public Length() { }

        public Length(int value, string units)
        {
            Value = value;
            Units = units;
        }
    }

    [XmlType]
    public class EpisodeNum
    {
        [XmlText]
        public string Value { get; set; }
        [XmlAttribute(AttributeName = "system")]
        public string System { get; set; }

        public EpisodeNum() { }

        public EpisodeNum(string value, string system = "onscreen")
        {
            Value = value;
            System = system;
        }
    }

    [XmlType(TypeName = "video")]
    public class Video
    {
        [XmlElement(ElementName = "present")]
        public string? Present { get; set; }
        [XmlElement(ElementName = "colour")]
        public string? Colour { get; set; }
        [XmlElement(ElementName = "aspect")]
        public string? Aspect { get; set; }
        [XmlElement(ElementName = "quality")]
        public string? Quality { get; set; }

        public Video() { }

        public Video(string? present, string? colour = null, string? aspect = null, string? quality = null)
        {
            Present = present;
            Colour = colour;
            Aspect = aspect;
            Quality = quality;
        }
    }

    [XmlType(TypeName = "Audio")]
    public class Audio
    {
        [XmlElement(ElementName = "stereo")]
        public string? Stereo;
        [XmlElement(ElementName = "present")]
        public string? Present;     // DTD is very unclear about whether this is to be available

        public Audio() { }

        public Audio(string? stereo, string? present = null)
        {
            Stereo = stereo;
            Present = present;
        }
    }

    [XmlType(TypeName = "previously-shown")]
    public class PreviouslyShown
    {
        [XmlText]
        public string? Value;

        [XmlAttribute(AttributeName = "start")] 
        public string? StartTimeXml { get; set; }
        [XmlAttribute(AttributeName = "channel")]
        public string? Channel;
        [XmlIgnore]
        public DateTimeOffset? StartTime
        {
            get => StartTimeXml != null ? XmlTV.ParseDateTime(StartTimeXml).DateTime : null;
            set => StartTimeXml = value.HasValue ? XmlTV.ParseDateTime(value.Value) : null;
        }

        public PreviouslyShown() { }

        public PreviouslyShown(string? channel, DateTimeOffset? startTime)
        {
            Channel = channel;
            StartTime = startTime;
        }
    }

    [XmlType(TypeName = "subtitle")]
    public class SubtitleInfo
    {

        [XmlAttribute(AttributeName = "type")]
        public string? Type{ get; set; }
        [XmlElement(ElementName = "language")]
        public string? Language { get; set; }

        public SubtitleInfo() { }

        public SubtitleInfo(string? type = null, string? language = null)
        {
            Type = type;
            Language = language;
        }
    }

    [XmlType(TypeName = "review")]
    public class Review
    {
        [XmlText]
        public string Value;
        [XmlAttribute(AttributeName = "type")]
        public string Type;
        [XmlAttribute(AttributeName = "source")]
        public string? Source;
        [XmlAttribute(AttributeName = "reviewer")]
        public string? Reviewer;
        [XmlAttribute(AttributeName = "lang")]
        public string? Lang;

        public Review() { }

        public Review(string type, string value, string? source, string? reviewer, string? lang)
        {
            Type = type;
            Value = value;
            Source = source;
            Reviewer = reviewer;
            Lang = lang;
        }
    }

    public enum CreditType
    {
        CreditDirector = 1,
        CreditWriter = 2,
        CreditAdapter = 3,
        CreditProducer = 4,
        CreditComposer = 5,
        CreditEditor = 6,
        CreditPresenter = 7,
        CreditCommentator = 8,
        CreditGuest = 9
    }

    [XmlType(TypeName = "credits")]
    public class Credits
    {
        [XmlElement(ElementName = "director")]
        public List<CreditItem> Directors { get; set; }
        [XmlElement(ElementName = "actor")]
        public List<Actor> Actors { get; set; }
        [XmlElement(ElementName = "writer")]
        public List<CreditItem> Writers { get; set; }
        [XmlElement(ElementName = "adapter")]
        public List<CreditItem> Adapters { get; set; }
        [XmlElement(ElementName = "producer")]
        public List<CreditItem> Producers { get; set; }
        [XmlElement(ElementName = "composer")]
        public List<CreditItem> Composers { get; set; }
        [XmlElement(ElementName = "editor")]
        public List<CreditItem> Editors { get; set; }
        [XmlElement(ElementName = "presenter")]
        public List<CreditItem> Presenters { get; set; }
        [XmlElement(ElementName = "commentator")]
        public List<CreditItem> Commentators { get; set; }
        [XmlElement(ElementName = "guest")]
        public List<CreditItem> Guests { get; set; }

        public Credits()
        {
            Directors = new List<CreditItem>();
            Actors = new List<Actor>();
            Writers = new List<CreditItem>();
            Adapters = new List<CreditItem>();
            Producers = new List<CreditItem>();
            Composers = new List<CreditItem>();
            Editors = new List<CreditItem>();
            Presenters = new List<CreditItem>();
            Commentators = new List<CreditItem>();
            Guests = new List<CreditItem>();
        }

        public CreditItem AddCredit(CreditType type, string name, Image? image = null, XmlTvUrl? url = null)
        {
            var item = new CreditItem(name, image, url);
            switch (type)
            {
                case CreditType.CreditDirector:
                    Directors.Add(item);
                    break;
                case CreditType.CreditWriter:
                    Writers.Add(item);
                    break;
                case CreditType.CreditAdapter:
                    Adapters.Add(item);
                    break;
                case CreditType.CreditProducer:
                    Producers.Add(item);
                    break;
                case CreditType.CreditComposer:
                    Composers.Add(item);
                    break;
                case CreditType.CreditEditor:
                    Editors.Add(item);
                    break;
                case CreditType.CreditPresenter:
                    Presenters.Add(item);
                    break;
                case CreditType.CreditCommentator:
                    Commentators.Add(item);
                    break;
                case CreditType.CreditGuest:
                    Guests.Add(item);
                    break;
                default:
                    throw new NotImplementedException("Invalid Credit Type");
            }

            return item;
        }

        public Actor AddActor(string name, string? role = null, string? guest = null, Image? image = null,
            XmlTvUrl url = null)
        {
            var item = new Actor(name, role, guest, image, url);
            Actors.Add(item);
            return item;
        }
    }

    [XmlType]
    public class CreditItem
    {
        [XmlText] 
        public string Name;
        [XmlElement(ElementName = "image")]
        public Image? Image;
        [XmlElement(ElementName = "url")]
        public XmlTvUrl? Url;

        public CreditItem() { }

        public CreditItem(string name, Image? image = null, XmlTvUrl url = null)
        {
            Name = name;
            Image = image;
            Url = url;
        }
    }

    [XmlType]
    public class Actor : CreditItem
    {
        [XmlAttribute(AttributeName = "role")] 
        public string? Role { get; set; }
        [XmlAttribute(AttributeName = "guest")]
        public string? Guest { get; set; }

        public Actor() : base() { }

        public Actor(string name, string? role = null, string? guest = null, Image? image = null, XmlTvUrl url = null) : base(name, image, url)
        {
            Role = role;
            Guest = guest;
        }
    }
}
