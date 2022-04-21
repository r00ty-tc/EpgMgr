using System.Data;
using System.Globalization;
using System.Xml;
using System.Xml.Serialization;

namespace EpgMgr.XmlTV
{
    [XmlRoot("tv")]
    public class XmlTV
    {
        [XmlAttribute(AttributeName = "date")]
        public string? DateXml { get; set; }
        [XmlAttribute(AttributeName = "source-info-url")]
        public string? SourceInfoUrl { get; set; }
        [XmlAttribute(AttributeName = "source-info-name")]
        public string? SourceInfoName { get; set; }
        [XmlAttribute(AttributeName = "source-data-url")]
        public string? SourceDataUrl { get; set; }
        [XmlAttribute(AttributeName = "generator-info-name")]
        public string? GeneratorInfoName { get; set; }
        [XmlAttribute(AttributeName = "generator-info-url")]
        public string? GeneratorInfoUrl { get; set; }
        [XmlElement(ElementName = "channel")]
        public List<Channel> Channels { get; set; }
        [XmlElement(ElementName = "programme")]
        public List<Programme> Programmes { get; set; }

        [XmlIgnore]
        public DateTime? Date
        {
            get => DateXml != null ? DateTime.ParseExact(DateXml, "yyyyMMdd", CultureInfo.InvariantCulture) : null;
            set => DateXml = value?.ToString("yyyyMMdd");
        }

        [XmlIgnore] private Dictionary<string, Channel> channelLookup;
        [XmlIgnore] private Dictionary<Tuple<string, string>, Programme> programmeLookup;

        public XmlTV(DateTime? date, string? sourceInfoName = null, string? sourceInfoUrl = null, string? sourceDataUrl = null, string? generatorInfoName = null, string? generatorInfoUrl = null)
        {
            SourceInfoName = sourceInfoName;
            SourceInfoUrl = sourceInfoUrl;
            SourceDataUrl = sourceDataUrl;
            GeneratorInfoName = generatorInfoName;
            GeneratorInfoUrl = generatorInfoUrl;
            Channels = new List<Channel>();
            Programmes = new List<Programme>();
            channelLookup = new Dictionary<string, Channel>();
            programmeLookup = new Dictionary<Tuple<string, string>, Programme>();

            Date = date;
        }

        public XmlTV()
        {
            Channels = new List<Channel>();
            Programmes = new List<Programme>();
            channelLookup = new Dictionary<string, Channel>();
            programmeLookup = new Dictionary<Tuple<string, string>, Programme>();
        }

        public void UpdateData()
        {
            // Update any data not included in XML file that needs to be regenerated after deserialization
            channelLookup = new Dictionary<string, Channel>(Channels.Select(row => new KeyValuePair<string, Channel>(row.Id, row)));
            programmeLookup = new Dictionary<Tuple<string, string>, Programme>(Programmes.Select(row =>
                new KeyValuePair<Tuple<string, string>, Programme>(
                    new Tuple<string, string>(row.Channel, ParseDateTime(row.StartTime)), row)));

            // Update links programme -> channel
            Programmes.ForEach(row => row.ChannelRef = GetChannel(row.Channel));

            // Update links channel -> programme
            Programmes.ForEach(row => GetChannel(row.Channel)?.Programmes.Add(row.StartTime, row));

            // Remove invalid programmes
            var toDelete = Programmes.Where(row => row.ChannelRef == null).ToArray();
            foreach (var programme in toDelete)
                DeleteProgramme(programme.StartTime, programme.Channel);
        }

        public Channel GetNewChannel(string id, string? displayName = null, string? lang = null, string? iconSource = null, int? iconWidth = null, int? iconHeight = null, string? url = null, string? urlSystem = null)
        {
            var channel = new Channel(id, displayName, lang, iconSource, iconWidth, iconHeight, url, urlSystem);
            Channels.Add(channel);
            channelLookup.Add(id, channel);
            return channel;
        }

        public Channel? GetChannel(string id) => channelLookup.TryGetValue(id, out var channel) ? channel : null;

        public void DeleteChannel(string id)
        {
            if (!channelLookup.TryGetValue(id, out var channel)) return;
            Channels.Remove(channel);
            channelLookup.Remove(id);
        }

        public Programme GetNewProgramme(DateTimeOffset startTime, string channel, string title, DateTimeOffset? stopTime = null,
            string? subtitle = null, string? description = null, string? language = null, string? category = null,
            string? titleLang = null, string? subtitleLang = null, string? descriptionLang = null,
            string? languageLang = null, string? categoryLang = null)
        {
            var programme = new Programme(startTime, channel, title, stopTime, subtitle, description, language,
                category, titleLang, subtitleLang, descriptionLang, languageLang, categoryLang);
            Programmes.Add(programme);
            programmeLookup.Add(new Tuple<string, string>(channel,programme.StartTimeXml), programme);
            programme.ChannelRef = GetChannel(channel);
            programme.ChannelRef?.Programmes.Add(programme.StartTime, programme);
            return programme;
        }

        public Programme? GetProgramme(DateTimeOffset startTime, string channel) =>
            programmeLookup.TryGetValue(new Tuple<string, string>(channel, XmlTV.ParseDateTime(startTime)),
                out var programme)
                ? programme
                : null;

        public void DeleteProgramme(DateTimeOffset startTime, string channel)
        {
            if (!programmeLookup.TryGetValue(new Tuple<string, string>(channel, XmlTV.ParseDateTime(startTime)), out var programme)) return;
            Programmes.Remove(programme);
            programme.ChannelRef?.Programmes.Remove(programme.StartTime);
            programmeLookup.Remove(new Tuple<string, string>(channel, XmlTV.ParseDateTime(startTime)));
        }

        public void DeleteOverlaps(DateTimeOffset startTime, DateTimeOffset endTime, string channel)
        {
            var channelRef = GetChannel(channel);
            if (channelRef == null)
                throw new Exception($"Channel {channel} not found");

            var programmes = channelRef.Programmes.Values.Where(row =>
                ((row.StartTime >= startTime && row.StartTime < endTime) ||
                (row.StopTime > startTime && row.StopTime <= endTime))).ToArray();

            foreach (var programme in programmes)
                DeleteProgramme(programme.StartTime, programme.Channel);
        }

        public void Save(string filename)
        {
            // Main config save
            var xmltvXml = new XmlDocument();
            var declaration = xmltvXml.CreateXmlDeclaration("1.0", "ISO-8859-1", null);
            var rootNode = xmltvXml.DocumentElement;
            xmltvXml.InsertBefore(declaration, rootNode);
            using (var xmlWriter = xmltvXml.CreateNavigator()?.AppendChild())
            {
                if (xmlWriter != null)
                {
                    var serializer = new XmlSerializer(typeof(XmlTV));
                    var ns = new XmlSerializerNamespaces();
                    ns.Add("", "");
                    serializer.Serialize(xmlWriter, this, ns);
                }
            }
            xmltvXml.Save(filename);
        }

        public static XmlTV? Load(string filename)
        {
            var tvXml = new XmlDocument();
            tvXml.Load(filename);
            if (tvXml.DocumentElement == null) return null;

            // Main config load
            var serializer = new XmlSerializer(typeof(XmlTV));
            var newXmlTv = (XmlTV?)serializer.Deserialize(new XmlNodeReader(tvXml.DocumentElement));
            newXmlTv?.UpdateData();
            return newXmlTv;
        }

        public static DateTimeOffset ParseDateTime(string datetime, bool dateOnly = false)
        {
            // Handle only date
            if (dateOnly)
                return DateTimeOffset.ParseExact(datetime, "yyyyMMdd", CultureInfo.InvariantCulture);

            // Otherwise we accept either yyyymmddhhmmss zzzz as a time with timezone offset provided
            // or we accept yyyymmddhhmmss as utc time
            // Try with offset first
            if (DateTimeOffset.TryParseExact(datetime, "yyyyMMddHHmmss zzz", null, DateTimeStyles.AssumeUniversal,
                    out var result))
                return result;
            if (DateTimeOffset.TryParseExact(datetime, "yyyyMMddHHmmss", null, DateTimeStyles.AssumeUniversal,
                    out result))
                return result;
            throw new DataException("Invalid date format provided");
        }

        public static string ParseDateTime(DateTimeOffset datetime, bool dateOnly = false) => datetime.ToString(dateOnly ? "yyyyMMdd" : "yyyyMMddHHmmss zzz").Replace(":", "");
    }
}
