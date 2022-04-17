using System.Diagnostics;
using System.Reflection;
using System.Xml;
using EpgMgr.XmlTV;

namespace EpgMgr.Plugins
{
    public partial class SkyUK : Plugin
    {
        internal const string API_CHANNEL_PREFIX = "https://awk.epgsky.com/hawk/linear/services/";
        internal const string API_PROGRAMME_PREFIX = "https://awk.epgsky.com/hawk/linear/schedule/";

        internal const string LOGO_PREFIX =
            "https://d2n0069hmnqmmx.cloudfront.net/epgdata/1.0/newchanlogos/320/320/skychb";
        public override Guid Id => Guid.Parse("17EC20A0-D302-4A42-BD10-23E5F08EDBAA");
        public override string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString();
        public override string Name => "Sky UK";
        public override string ConsoleName => "SkyUK";

        public override string Author => "EpgMgr Core Team";
        private readonly WebHelper m_web;

        public SkyUK(Core mCore) : base(mCore)
        {
            m_web = new WebHelper("https://awk.epgsky.com/");
            configTypes.Add(typeof(Channel));
            configTypes.Add(typeof(SkyChannel));
            configTypes.Add(typeof(CustomTag));
            InitConfig();
        }

        public override PluginErrors GenerateXmlTv(ref XmlTV.XmlTV xmltv)
        {
            var errors = new PluginErrors();
            var programmeList = new List<SkyEpgList>();
            var skyChannels = configRoot.GetList<SkyChannel>("ChannelsSubbed");
            var totalLookups = (m_core.Config.XmlTvConfig.MaxDaysBehind + m_core.Config.XmlTvConfig.MaxDaysAhead + 1) * skyChannels.Count;
            var lookupCount = 0;
            m_core.FeedbackMgr.UpdateStatus("Loading programmes from API", 0, totalLookups);
            for (DateTime date = DateTime.Today.AddDays(0 - m_core.Config.XmlTvConfig.MaxDaysBehind);
                 date <= DateTime.Today.AddDays(m_core.Config.XmlTvConfig.MaxDaysAhead);
                 date = date.AddDays(1))
            {
                var todayProgrammes = GetAllProgrammes(ref lookupCount, date);
                programmeList.AddRange(todayProgrammes);
            }
            m_core.FeedbackMgr.UpdateStatus("Done loading from API");

            var totalPrograms = programmeList.Sum(epg => epg.Schedules.Sum(schedule => schedule.Programmes.Length));
            var currentProgram = 0;

            m_core.FeedbackMgr.UpdateStatus("Updating programmes", 0, totalPrograms);
            foreach (var epg in programmeList)
            {
                foreach (var schedule in epg.Schedules)
                {
                    foreach (var programme in schedule.Programmes)
                    {
                        xmltv.DeleteOverlaps(programme.StartTime, programme.EndTime, schedule.Sid);
                        var xmltvProgramme = xmltv.GetNewProgramme(programme.StartTime, schedule.Sid, programme.Title,
                            programme.EndTime, null, programme.Synopsis, null, null, "en", null, "en");
                        currentProgram++;
                        if (currentProgram % 100 == 0 || currentProgram == totalPrograms)
                            m_core.FeedbackMgr.UpdateStatus(null, currentProgram);
                    }
                }
            }

            return errors;
        }

        public override void LoadConfig(XmlElement? pluginConfig)
        {
            base.LoadConfig(pluginConfig);
            var channels = configRoot.GetList<SkyChannel>("ChannelsAvailable");
            if (channels == null || !channels.Any())
                GetApiChannels();
        }

        private void InitConfig()
        {
            ConfigEntry.NewConfigList(configRoot, "ChannelsSubbed", null, new List<SkyChannels>());
            ConfigEntry.NewConfigList(configRoot, "ChannelsAvailable", null, new List<SkyChannels>());
        }

        public override EpgMgr.Channel[] GetXmlTvChannels()
        {
            var skyChannels = configRoot.GetList<SkyChannel>("ChannelsSubbed");
            if (skyChannels == null)
                return new List<EpgMgr.Channel>().ToArray();

            return skyChannels.Select(row => new EpgMgr.Channel(row.Sid, row.ChannelName, "en", row.LogoUrl)).ToArray();
        }

        public override void RegisterConfigData(FolderEntry folderEntry)
        {
            RegisterCommands(folderEntry);
        }

        protected IEnumerable<SkyChannel> GetApiChannels()
        {
            // @ToDO: Configurable region
            var channels = m_web.GetJSON<SkyChannels>(API_CHANNEL_PREFIX + "4101/1") ??
                           new SkyChannels();
            configRoot.SetList<SkyChannel>("ChannelsAvailable", channels.Channels.ToList());
            return channels.Channels;
        }

        protected IEnumerable<SkyChannel> ProcessRange(string[] rangeArgs, IEnumerable<SkyChannel> channels)
        {
            var newChannels = new List<SkyChannel>();
            foreach (var arg in rangeArgs)
            {
                if (arg.Contains("-"))
                {
                    // No spaces
                    var newArg = arg.Replace(" ", "");
                    var ranges = newArg.Split("-");
                    if (ranges.Length != 2)
                    {
                        m_core.FeedbackMgr.UpdateStatus($"Invalid range value {arg}");
                    }
                    newChannels.AddRange(channels.Where(row => string.Compare(row.ChannelNo, ranges[0], StringComparison.InvariantCultureIgnoreCase) >= 0 && string.Compare(row.ChannelNo, ranges[1], StringComparison.CurrentCultureIgnoreCase) <= 0));
                }
                else
                {
                    var thisChannel = channels.FirstOrDefault(row =>
                        row.ChannelNo.Equals(arg, StringComparison.InvariantCultureIgnoreCase));
                    if (thisChannel != null)
                        newChannels.Add(thisChannel);
                }
            }
            return newChannels;
        }

        protected SkyEpgList GetProgrammes(SkyChannel[] channels, DateTime? date = null)
        {
            if (channels.Length > 20)
                throw new Exception("Too many channels");

            if (date == null)
                date = DateTime.Today;
            var uri = API_PROGRAMME_PREFIX + date.Value.ToString("yyyyMMdd") + "/" + string.Join(",", channels.Select(row => row.Sid));
            var programmes = m_web.GetJSON<SkyEpgList>(uri);
            return programmes;
        }

        protected SkyEpgList[] GetAllProgrammes(ref int count, DateTime? date = null)
        {
            List<SkyChannel> toProcess = new List<SkyChannel>();
            var existingChannels = configRoot.GetList<SkyChannel>("ChannelsSubbed") ?? new List<SkyChannel>();
            var programmeList = new List<SkyEpgList>();
            foreach (var channel in existingChannels)
            {
                toProcess.Add(channel);
                if (toProcess.Count == 20)
                {
                    programmeList.Add(GetProgrammes(toProcess.ToArray(), date));
                    count += 20;
                    m_core.FeedbackMgr.UpdateStatus(null, count);
                    toProcess.Clear();
                }
            }

            if (toProcess.Count > 0)
            {
                programmeList.Add(GetProgrammes(toProcess.ToArray(), date));
                count += toProcess.Count;
                m_core.FeedbackMgr.UpdateStatus(null, count);
            }

            return programmeList.ToArray();
        }

        internal static DateTimeOffset ConvertFromUnixTime(long timeStamp) => new DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, new TimeSpan(0, 1, 0, 0)).AddSeconds(timeStamp);
    }
}