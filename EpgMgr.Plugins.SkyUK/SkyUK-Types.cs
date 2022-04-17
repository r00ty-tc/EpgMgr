using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace EpgMgr.Plugins
{
    public class SkyChannels
    {
        [JsonPropertyName("services")]
        public SkyChannel[] Channels { get; set; }
    }
    [XmlType]
    public class SkyChannel
    {
        [JsonPropertyName("sid"), XmlAttribute(AttributeName = "sid")]
        public string Sid { get; set; }
        [JsonPropertyName("c"), XmlAttribute(AttributeName = "channelNo")]
        public string ChannelNo { get; set; }
        [JsonPropertyName("t"), XmlText]
        public string ChannelName { get; set; }
        [JsonPropertyName("sg"), XmlAttribute(AttributeName = "sg")]
        public int Sg { get; set; }
        [JsonPropertyName("xsg"), XmlAttribute(AttributeName = "xsg")]
        public int Xsg { get; set; }
        [JsonPropertyName("sf"), XmlAttribute(AttributeName = "type")]
        public string Sf { get; set; }
        [JsonPropertyName("adult"), XmlAttribute(AttributeName = "adult")]
        public bool IsAdsult { get; set; }
        [JsonPropertyName("local"), XmlAttribute(AttributeName = "local")]
        public bool IsLocal { get; set; }
        [JsonPropertyName("avail"), XmlElement(ElementName = "Availability")]
        public string[] Availability { get; set; }

        [JsonIgnore, XmlIgnore] 
        public string LogoUrl => $"{SkyUK.LOGO_PREFIX}{Sid}.png";
    }

    public class SkyEpgList
    {
        [JsonPropertyName("date")]
        public string DateJson { get; set; }
        [JsonIgnore]
        public DateTime Date
        {
            get => DateTime.ParseExact(DateJson, "yyyyMMdd", CultureInfo.InvariantCulture);
            set => DateJson = value.ToString("yyyyMMdd");
        }

        [JsonPropertyName("schedule")]
        public SkySchedule[] Schedules { get; set; }
    }

    public class SkySchedule
    {
        [JsonPropertyName("sid")]
        public string Sid { get; set; }
        [JsonPropertyName("events")]
        public SkyProgram[] Programmes { get; set; }
    }

    public class SkyProgram
    {
        [JsonPropertyName("st")]
        public long start { get; set; }
        [JsonIgnore]
        public DateTimeOffset StartTime => SkyUK.ConvertFromUnixTime(start);
        [JsonPropertyName("d")]
        public long duration { get; set; }
        [JsonIgnore]
        public DateTimeOffset EndTime => SkyUK.ConvertFromUnixTime(start + duration);

        [JsonPropertyName("eid")]
        public string EpisodeId { get; set; }
        [JsonPropertyName("cgid")]
        public int CategoryId { get; set; }
        [JsonPropertyName("programmeuuid")]
        public string ProgrammeUUID { get; set; }
        [JsonPropertyName("seasonnumber")]
        public int SeasonNo { get; set; }
        [JsonPropertyName("episodenumber")]
        public int EpisodeNo { get; set; }
        [JsonPropertyName("seasonuuid")]
        public string SeasonUUID { get; set; }
        [JsonPropertyName("seriesuuid")]
        public string SeriesUUID { get; set; }
        [JsonPropertyName("haschildren")]
        public bool HasChildren { get; set; }
        [JsonPropertyName("t")]
        public string Title { get; set; }
        [JsonPropertyName("sy")]
        public string Synopsis { get; set; }
        [JsonPropertyName("eg")]
        public int Eg { get; set; }
        [JsonPropertyName("esg")]
        public int Esg { get; set; }
        [JsonPropertyName("tso")]
        public int Tso { get; set; }
        [JsonPropertyName("r")]
        public string Rating { get; set; }
        [JsonPropertyName("at")]
        public string At { get; set; }
        [JsonPropertyName("s")]
        public bool S { get; set; }
        [JsonPropertyName("ad`")]
        public bool HasAudioDescription { get; set; }
        [JsonPropertyName("hd")]
        public bool IsHD { get; set; }
        [JsonPropertyName("new")]
        public bool IsNew { get; set; }
        [JsonPropertyName("canl")]
        public bool CanL { get; set; }
        [JsonPropertyName("canb")]
        public bool CanB { get; set; }
        [JsonPropertyName("hasAlternativeAudio")]
        public bool HasAlternativeAudio { get; set; }
        [JsonPropertyName("restartable")]
        public bool IsRestartable { get; set; }
        [JsonPropertyName("slo")]
        public bool Slo { get; set; }
        [JsonPropertyName("w")]
        public bool W { get; set; }
        [JsonPropertyName("ippv")]
        public bool Ippv { get; set; }
        [JsonPropertyName("oppv")]
        public bool Oppv { get; set; }
    }
}
