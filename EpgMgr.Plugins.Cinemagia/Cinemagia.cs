using System.Data;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using EpgMgr.XmlTV;

namespace EpgMgr.Plugins

{
    public partial class Cinemagia : Plugin
    {
        private static readonly Regex channelRegex = new("\\<a href=\\\"https://www.cinemagia.ro/program-tv/([a-zA-Z0-9\\-]*?)/\\\" title=\\\"(?:[^\\\"]*?)\\\" class\\=\\\"station-link\\\"\\>([^\\<]*?)\\</a\\>", RegexOptions.Compiled);
        private static readonly Regex programmeRegex = new Regex(
            "\\<td class=\\\"ora\\\"\\>\\s*\\<div\\>(\\d{2}:\\d{2})\\<\\/div\\>.*?\\<\\/td\\>\\s*\\<td class=\\\"event\\\"\\>\\s*\\<div class=\\\"title\\\">\\s*(?:\\<a href=\\\".*?\\\" title=\\\".*?\\\"\\>)?(.*?)(?:\\<\\/a\\>)?\\s*?(?:\\<span class=\\\"sub_title\\\"\\>(.*?)\\<\\/span\\>\\s*)?\\s*\\<\\/div\\>", RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex daylinkRegex = new Regex("\\<div class=\\\"navigation_container\\\"\\>\\s*\\<ul class=\\\"tab_5\\\"\\>\\s*(?:\\<li(?: class=\\\"current\\\")?\\>\\<a href=\\\"(.*?)\\\" title=\\\"(.*?)\\\"\\>\\<span\\>(.*?)\\<\\/span\\>\\<\\/a\\>\\<\\/li\\>\\s*)*\\<\\/ul\\>", RegexOptions.Singleline | RegexOptions.Compiled);
        public override Guid Id => Guid.Parse("ED14D5F6-456E-48CB-85B3-DF6442B2E457");
        public override string Version => Assembly.GetExecutingAssembly().GetName().Version!.ToString();
        public override string Name => "Cinemagia (RO)";
        public override string ConsoleName => "Cinemagia";

        public override string Author => "EpgMgr Core Team";
        private readonly WebHelper m_web;
        public Cinemagia(Core mCore) : base(mCore)
        {
            var acceptHeaders = new List<MediaTypeWithQualityHeaderValue>
            {
                new("text/html")
                {
                    CharSet = Encoding.UTF8.WebName
                }
            };
            m_web = new WebHelper("https://www.cinemagia.ro/program-tv/", null, 5000, null, acceptHeaders.ToArray());
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
            m_core.FeedbackMgr.UpdateStatus("Cinemagia: Updating programmes", 0, subChannels.Count);
            var currentChannel = 0;
            var totalPrograms = 0;
            foreach (var channel in subChannels)
            {
                var programmes = getApiProgrammes(channel.Id, DateTime.Today, m_core.Config.XmlTvConfig.MaxDaysAhead).ToArray();
                totalPrograms += programmes.Length;
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
                    if (!string.IsNullOrWhiteSpace(programme.Description))
                        xmlProgramme.AddDescription(programme.Description, "ro");
                    programCount++;
                    if (programCount % 10 == 0)
                        m_core.FeedbackMgr.UpdateStatus(null, programCount, totalPrograms);
                }
            }
            m_core.FeedbackMgr.UpdateStatus(null, programCount, totalPrograms);

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
            var webData = m_web.WebGet("https://www.cinemagia.ro/program-tv/?autodetect_stations=true");
            var matches = channelRegex.Matches(webData);
            if (matches.Count == 0)
                throw new DataException("Data returned no channel results");

            var channels = new List<Channel>();
            foreach (Match match in matches)
                channels.Add(new Channel(match.Groups[1].Value, match.Groups[2].Value));

            if (channels.Any())
                configRoot.SetList("ChannelsAvailable", channels.ToList());

            return channels;
        }

        private IEnumerable<CinemagiaProgramme> getApiProgrammes(string channel, DateTime startDate, int days)
        {
            var programmes = new SortedDictionary<DateTime, CinemagiaProgramme>();
            var channels = configRoot.GetList<Channel>("ChannelsSubbed") ?? new List<Channel>();
            var thisChannel = channels.FirstOrDefault(row => row.Id.Equals(channel));
            if (thisChannel == null) return new List<CinemagiaProgramme>();

            if (days > 14) days = 14;
            var endDate = startDate.AddDays(days);
            var currentDate = startDate;
            CinemagiaProgramme? lastProgramme = null;
            while (currentDate <= endDate)
            {
                var webData = m_web.WebGet($"https://program-tv.net/program_tv_saptamanal/{channel}{(currentDate.Equals(DateTime.Today) ? string.Empty : "/" + currentDate.ToString("dd.MM.yyyy"))}");
                var matches = programmeRegex.Matches(webData);

                foreach (Match match in matches)
                {
                    var programme = new CinemagiaProgramme(thisChannel.Name ?? thisChannel.Id, match.Groups[1].Value, null, match.Groups[2].Value, match.Groups[3].Value);
                    
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