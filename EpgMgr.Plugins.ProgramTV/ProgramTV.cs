using System.Data;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using EpgMgr.XmlTV;

namespace EpgMgr.Plugins

{
    public partial class ProgramTV : Plugin
    {
        public override Guid Id => Guid.Parse("F98C197E-821A-47FB-8AD5-75701B336F92");
        public override string Version => Assembly.GetExecutingAssembly().GetName().Version!.ToString();
        public override string Name => "Program TV (RO)";
        public override string ConsoleName => "ProgramTV";

        public override string Author => "EpgMgr Core Team";
        private readonly WebHelper m_web;
        public ProgramTV(Core mCore) : base(mCore)
        {
            var acceptHeaders = new List<MediaTypeWithQualityHeaderValue>
            {
                new("text/html")
                {
                    CharSet = Encoding.UTF8.WebName
                }
            };
            m_web = new WebHelper("https://program-tv.net/", null, 5000, null, acceptHeaders.ToArray());
            configTypes.Add(typeof(Channel));
            configTypes.Add(typeof(CustomTag));
            InitConfig();
            //InitChannels();
        }

        public override void LoadConfig(XmlElement? pluginConfig)
        {
            base.LoadConfig(pluginConfig);

            // If structures aren't setup, add them. This protects against old configs without them being loaded
            var allChannels = configRoot.GetList<Channel>("ChannelsAvailable");
            if (allChannels == null)
                ConfigEntry.NewConfigList(configRoot, "ChannelsAvailable", null, new List<Channel>());
            var channels = configRoot.GetList<Channel>("ChannelsAvailable");
            if (channels == null)
                ConfigEntry.NewConfigList(configRoot, "ChannelsSubbed", null, new List<Channel>());

            // If channels aren't loaded, get them from API
            if (allChannels == null || !allChannels.Any())
                getApiChannels();
        }

        public override EpgMgr.Channel[] GetXmlTvChannels()
        {
            var subbedChannels = configRoot.GetList<Channel>("ChannelsSubbed") ?? new List<Channel>();
            return subbedChannels.Where(row => row.Name != null).Select(channel => new EpgMgr.Channel(channel.Name!, channel.Name)).ToArray();
        }

        public override PluginErrors GenerateXmlTv(ref XmlTV.XmlTV xmltv)
        {
            var errors = new PluginErrors();
            var subChannels = configRoot.GetList<Channel>("ChannelsSubbed") ?? new List<Channel>();
            var xmltvChannelNames = GetXmlTvChannels().Select(row => row.Id);
            int programCount = 0;
            foreach (var channel in subChannels)
            {
                var programmes = getApiProgrammes(channel.Id, DateTime.Today, m_core.Config.XmlTvConfig.MaxDaysAhead).ToArray();

                // See if we can fix the null date(s)
                var nullProgrammes = programmes.Where(row => row.EndTime == null);
                foreach (var nullProgramme in nullProgrammes)
                {
                    var progChannel = xmltv.Channels.FirstOrDefault(row => row.Id.Equals(channel.Name));

                    var lastProgramme = progChannel?.Programmes.Values
                        .Where(wrow => wrow.StartTime > nullProgramme.StartTime)
                        .OrderBy(orow => orow.StartTime).FirstOrDefault();

                    nullProgramme.EndTime = lastProgramme?.StartTime.ToUniversalTime().DateTime;
                }

                foreach (var programme in programmes)
                {
                    // Delete any overlapping programs
                    if (programme.EndTime.HasValue)
                        xmltv.DeleteOverlaps(programme.StartTime, programme.EndTime.Value, m_core.GetAliasFromChannelName(programme.Channel)!);

                    // Create new program
                    var xmlProgramme = xmltv.GetNewProgramme(programme.StartTime, m_core.GetAliasFromChannelName(programme.Channel)!, programme.Title ?? string.Empty, programme.EndTime);

                    // Add subtitle/description if present
                    if (!string.IsNullOrWhiteSpace(programme.SubTitle))
                        xmlProgramme.AddSubtitle(programme.SubTitle, "ro");
                    if (!string.IsNullOrWhiteSpace(programme.Description))
                        xmlProgramme.AddDescription(programme.Description, "ro");
                    programCount++;
                }
            }
            m_core.FeedbackMgr.UpdateStatus($"ProgramTV: Loaded {programCount} programmes from API");

            return errors;
        }

        public override void RegisterConfigData(FolderEntry folderEntry)
        {
            // Custom paths
            RegisterCommands(folderEntry);
        }

        private void InitConfig()
        {
            ConfigEntry.NewConfigList(configRoot, "ChannelsSubbed", null, new List<Channel>());
            ConfigEntry.NewConfigList(configRoot, "ChannelsAvailable", null, new List<Channel>());
        }

        private void InitChannels()
        {
            // Get from API if not already present
            var channels = configRoot.GetList<Channel>("ChannelsAvailable");
            if (channels == null || !channels.Any())
            {
                var apiChannels = getApiChannels().ToArray();
                if (apiChannels.Any())
                    m_core.FeedbackMgr.UpdateStatus($"Loaded {apiChannels.Count()} channels from API");
            }
        }

        private IEnumerable<Channel> getApiChannels()
        {
            var webData = m_web.WebGet("https://program-tv.net/");
            var regex = new Regex("\\<a href\\=\\\"(.*?)\\\".*?class\\=\\\"channellistentry\\\"\\>(.*?)\\<\\/a\\>");
            var matches = regex.Matches(webData);
            if (matches.Count == 0)
                throw new DataException("Data returned no channel results");

            var channels = new List<Channel>();
            foreach (Match match in matches)
                channels.Add(new Channel(match.Groups[1].Value.Replace("//program-tv.net/program_tv/", ""), match.Groups[2].Value));

            if (channels.Any())
                configRoot.SetList("ChannelsAvailable", channels.ToList());

            return channels;
        }

        private IEnumerable<ProgramTVProgramme> getApiProgrammes(string channel, DateTime startDate, int days)
        {
            var programmes = new SortedDictionary<DateTime, ProgramTVProgramme>();
            var channels = configRoot.GetList<Channel>("ChannelsSubbed") ?? new List<Channel>();
            var thisChannel = channels.FirstOrDefault(row => row.Id.Equals(channel));
            if (thisChannel == null) return new List<ProgramTVProgramme>();

            if (days > 14) days = 14;
            var endDate = startDate.AddDays(days);
            var currentDate = startDate;
            ProgramTVProgramme? lastProgramme = null;
            while (currentDate <= endDate)
            {
                var webData = m_web.WebGet($"https://program-tv.net/program_tv_saptamanal/{channel}{(currentDate.Equals(DateTime.Today) ? string.Empty : "/" + currentDate.ToString("dd.MM.yyyy"))}");
                var regex = new Regex(
                    "\\<div class\\=\\\"smartpe_progentry smartpe_progentry_old\\\" itemscope itemtype\\=\\\"\\\" title\\=\\\".*?\\\"\\>\\s*?\\<div class\\=\\\"smartpe_progentryrow\\\" itemprop\\=\\\"location\\\" content\\=\\\"\\\"\\>\\s*?\\<div class\\=\\\"smartpe_progentrycell\\\"\\>\\s*?\\<div class\\=\\\"smartpe_progentry_intable top5\\\"\\>\\s*?\\<div class\\=\\\"smartpe_progentryrow\\\"\\>\\s*?\\<div class\\=\\\"smartpe_progentrycell\\\"\\>\\s*?\\<time class\\=\\\"smartpe_time smartpe_time_old\\\"\\s*?itemprop\\=\\\"startDate\\\"\\s*?content\\=\\\"(.*?)\\\"\\> .*?\\<\\/time\\>.*?\\<h3 class\\=\\\"smartpe_progtitle_common smartpe_progtitle\\\"\\s*?itemprop\\=\\\"\\\"\\>\\<a\\s*?id\\=\\\"(.*?)\\\"\\s*? href\\=\\\"(.*?)\\\"\\s*?target\\=\\\"_blank\\\"\\>(.*?)\\<\\/a\\>\\<\\/h3\\>.*?\\<div class\\=\\\"smartpe_progshortdesc\\\"\\s*?itemprop\\=\\\"\\\"\\>(.*?)\\<\\/div\\>.*?\\<div class\\=\\\"smartpe_progentrylong\\\"\\>(.*?)\\<\\/div\\>", RegexOptions.Singleline);
                var matches = regex.Matches(webData);

                foreach (Match match in matches)
                {
                    var programme = new ProgramTVProgramme(thisChannel.Name ?? thisChannel.Id, match.Groups[1].Value, null, match.Groups[2].Value,
                        match.Groups[3].Value, match.Groups[4].Value, match.Groups[5].Value, match.Groups[6].Value);
                    
                    if (lastProgramme != null)
                        lastProgramme.EndTime = programme.StartTime;

                    if (programme.StartTime.Date < startDate || programme.StartTime.Date > endDate)
                        continue;

                    lastProgramme = programme;

                    if (!programmes.ContainsKey(programme.StartTime))
                        programmes.Add(programme.StartTime, programme);
                }

                currentDate = currentDate.AddDays(5);
            }

            return programmes.Values;
        }
    }
}