using System.Data;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;

namespace EpgMgr.Plugins
{
    public partial class SkyUK : Plugin
    {
        internal const string API_CHANNEL_PREFIX = "https://awk.epgsky.com/hawk/linear/services/";
        internal const string API_PROGRAMME_PREFIX = "https://awk.epgsky.com/hawk/linear/schedule/";

        internal const string LOGO_PREFIX =
            "https://d2n0069hmnqmmx.cloudfront.net/epgdata/1.0/newchanlogos/320/320/skychb";

        // This might change often. Must watch.
        internal const string BLOBDATAURI =
            "https://www.sky.com/watch/assets/pages-app-tv-guide-index-js.81a2d554690a593fed3d.js";

        internal const string DEFAULT_REGION = "4101-1";    // London HD
        public override Guid Id => Guid.Parse("17EC20A0-D302-4A42-BD10-23E5F08EDBAA");
        public override string Version => Assembly.GetExecutingAssembly().GetName().Version!.ToString();
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
            configTypes.Add(typeof(SkyRegion));
            configTypes.Add(typeof(SkyServiceGenre));
            InitConfig();
        }

        public override PluginErrors GenerateXmlTv(ref XmlTV.XmlTV xmltv)
        {
            var errors = new PluginErrors();
            var programmeList = new List<SkyEpgList>();
            var skyChannels = configRoot.GetList<SkyChannel>("ChannelsSubbed");
            var totalLookups = (m_core.Config.XmlTvConfig.MaxDaysBehind + m_core.Config.XmlTvConfig.MaxDaysAhead + 1) * skyChannels?.Count;
            var lookupCount = 0;
            m_core.FeedbackMgr.UpdateStatus("Loading programmes from API", 0, totalLookups);
            for (var date = DateTime.Today.AddDays(0 - m_core.Config.XmlTvConfig.MaxDaysBehind);
                 date <= DateTime.Today.AddDays(m_core.Config.XmlTvConfig.MaxDaysAhead);
                 date = date.AddDays(1))
            {
                var todayProgrammes = GetAllProgrammes(ref lookupCount, date);
                programmeList.AddRange(todayProgrammes);
            }
            m_core.FeedbackMgr.UpdateStatus("Done loading from API");

            var totalPrograms = programmeList.Sum(epg => epg.Schedules.Sum(schedule => schedule.Programmes?.Length));
            var currentProgram = 0;

            m_core.FeedbackMgr.UpdateStatus("Updating programmes", 0, totalPrograms);
            foreach (var epg in programmeList)
            {
                foreach (var schedule in epg.Schedules)
                {
                    var channel = skyChannels?.FirstOrDefault(row => row.Sid != null && row.Sid.Equals(schedule.Sid));
                    if (channel == null)
                    {
                        errors.AddError($"Channel {schedule.Sid} was not found");
                    }
                    else
                    {
                        if (schedule.Programmes == null) continue;
                        foreach (var programme in schedule.Programmes)
                        {
                            if (!programme.StartTime.HasValue)
                                continue;

                            // Remove overlapping program(s)
                            if (programme.EndTime.HasValue)
                                xmltv.DeleteOverlaps(programme.StartTime.Value, programme.EndTime.Value, m_core.GetAliasFromChannelName(channel.ChannelName)!);

                            // Generate new XMLTV programme
                            var xmltvProgramme = xmltv.GetNewProgramme(programme.StartTime.Value, m_core.GetAliasFromChannelName(channel.ChannelName)!,
                                programme.Title ?? string.Empty,
                                programme.EndTime, null, programme.Synopsis, null, null, "en", null, "en");

                            // Add episode info if present
                            if (programme.SeasonNo > 0 && programme.EpisodeNo > 0)
                                xmltvProgramme.AddEpisodeNum($"{programme.SeasonNo}{programme.EpisodeNo.Value.ToString("D2")}");
                            if (!string.IsNullOrWhiteSpace(programme.EpisodeId))
                                xmltvProgramme.AddEpisodeNum(programme.EpisodeId, "skyuk_epid");

                            // Add category if enabled and present and valid
                            if (m_core.Config.XmlTvConfig.IncludeProgrammeCategories && programme.Eg.HasValue)
                            {
                                    var genre = getGenre(programme.Eg.Value);
                                    if (genre != null)
                                        xmltvProgramme.AddCategory(genre);
                            }
                            currentProgram++;
                            if (currentProgram % 100 == 0 || currentProgram == totalPrograms)
                                m_core.FeedbackMgr.UpdateStatus(null, currentProgram);
                        }
                    }
                }
            }

            return errors;
        }

        public override void LoadConfig(XmlElement? pluginConfig)
        {
            base.LoadConfig(pluginConfig);

            // If structures aren't setup, add them. This protects against old configs without them being loaded
            var allChannels = configRoot.GetList<SkyChannel>("ChannelsAvailable");
            if (allChannels == null)
                ConfigEntry.NewConfigList(configRoot, "ChannelsAvailable", null, new List<SkyChannels>());
            var channels = configRoot.GetList<SkyChannel>("ChannelsAvailable");
            if (channels == null)
                ConfigEntry.NewConfigList(configRoot, "ChannelsSubbed", null, new List<SkyChannels>());

            var regions = configRoot.GetList<SkyRegion>("SkyRegions");
            if (regions == null)
                ConfigEntry.NewConfigList(configRoot, "SkyRegions", "regions", new List<SkyRegion>());
            var genres = configRoot.GetList<SkyServiceGenre>("SkyServiceGenres");
            if (genres == null)
                ConfigEntry.NewConfigList(configRoot, "SkyServiceGenres", "genres", new List<SkyServiceGenre>());

            // If either regions or genres aren't found, fetch them from API
            if (regions == null || !regions.Any() || genres == null || !genres.Any())
            {
                LoadBlobData();
            }

            // If channels aren't loaded, get them from API
            if (allChannels == null || !allChannels.Any())
                GetApiChannels();

            // If region not found, set default
            if (configRoot.GetValue<string>("SkyRegion") == null)
                ConfigEntry.NewConfigEntry<string>(configRoot, "SkyRegion", DEFAULT_REGION, "region");
        }

        private void InitConfig()
        {
            ConfigEntry.NewConfigList(configRoot, "ChannelsSubbed", null, new List<SkyChannels>());
            ConfigEntry.NewConfigList(configRoot, "ChannelsAvailable", null, new List<SkyChannels>());
            ConfigEntry.NewConfigList(configRoot, "SkyRegions", "regions", new List<SkyRegion>());
            ConfigEntry.NewConfigList(configRoot, "SkyServiceGenres", "genres", new List<SkyServiceGenre>());
            ConfigEntry.NewConfigEntry<string>(configRoot, "SkyRegion", DEFAULT_REGION, "Region");
        }

        public override EpgMgr.Channel[] GetXmlTvChannels()
        {
            var skyChannels = configRoot.GetList<SkyChannel>("ChannelsSubbed");
            if (skyChannels == null)
                return new List<EpgMgr.Channel>().ToArray();

            return skyChannels.Select(row => new EpgMgr.Channel(m_core.GetAliasFromChannelName(row.ChannelName)!, m_core.GetAliasFromChannelName(row.ChannelName), "en", row.LogoUrl)
            {
                SkySID = row.Sid
            }).ToArray();
        }

        public override void RegisterConfigData(FolderEntry folderEntry)
        {
            folderEntry.AddChildValue("SkyRegion", setGetConfigValue, ValueType.ConfigValueType_String);
            RegisterCommands(folderEntry);
        }

        protected IEnumerable<SkyChannel> GetApiChannels()
        {
            var regionId = configRoot.GetValue<string>("SkyRegion");
            if (regionId == null) regionId = DEFAULT_REGION;
            var region = configRoot.GetList<SkyRegion>("SkyRegions")?.FirstOrDefault(row => row.RegionId.Equals(regionId));
            if (region == null)
            {
                LoadBlobData();
                region = configRoot.GetList<SkyRegion>("SkyRegions")?.FirstOrDefault(row => row.RegionId.Equals(regionId));
                if (region == null)
                {
                    throw new DataException($"Unable to find region {regionId}");
                }
            }

            var channels = m_web.GetJSON<SkyChannels>(API_CHANNEL_PREFIX + $"{region.Bouquet}/{region.SubBouquet}") ?? new SkyChannels();
            if (channels.Channels != null)
                configRoot.SetList<SkyChannel>("ChannelsAvailable", channels.Channels.ToList());

            return channels.Channels ?? Array.Empty<SkyChannel>();
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
                        row.ChannelNo != null && row.ChannelNo.Equals(arg, StringComparison.InvariantCultureIgnoreCase));
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
            return programmes ?? new SkyEpgList();
        }

        protected SkyEpgList[] GetAllProgrammes(ref int count, DateTime? date = null)
        {
            var toProcess = new List<SkyChannel>();
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

        private string? getGenre(int genreId)
        {
            var genres = configRoot.GetList<SkyServiceGenre>("SkyServiceGenres");
            if (genres == null) return null;
            return genres.FirstOrDefault(row => row.GenreId == genreId)?.GenreName;
        }

        public void LoadBlobData()
        {
            // Load blob data
            var blobData = m_web.WebGet(BLOBDATAURI);

            // Read regions
            var regionRegex =
                new Regex(
                    "M.exports\\=(\\[{text\\:\\\".+?\\\"\\,bouquet\\:\\d+?\\,subBouquet\\:\\d+?,value\\:\\\".*?\\\"\\}\\])");
            var regionDataString = regionRegex.Match(blobData);
            if (regionDataString.Groups.Count > 1)
            {
                var resultString = regionDataString.Groups[1].Value;
                resultString = resultString.Replace("text:", "\"text\":").Replace("bouquet:", "\"bouquet\":").Replace("subBouquet:", "\"subBouquet\":").Replace("value:", "\"value\":");
                var regions = JsonSerializer.Deserialize<IEnumerable<SkyRegion>>(resultString);
                if (regions != null && regions.Any())
                {
                    configRoot.SetList("SkyRegions", regions.ToList());
                    m_core.FeedbackMgr.UpdateStatus($"Loaded {regions.Count()} regions");
                }
            }

            // Read genres
            var genreRegex = new Regex("serviceGenres:(\\[{text:\\\".+?\\\",value:\\d+\\}\\])");
            var genreDataString = genreRegex.Match(blobData);
            if (genreDataString.Groups.Count > 1)
            {
                var resultString = genreDataString.Groups[1].Value;
                resultString = resultString.Replace("{text:\"HD Channels\",value:\"HD\"},", "").Replace("{text:\"All Channels\",value:0},", "").Replace("text:", "\"text\":").Replace("value:", "\"value\":");
                var genres = JsonSerializer.Deserialize<IEnumerable<SkyServiceGenre>>(resultString);
                if (genres != null && genres.Any())
                {
                    configRoot.SetList("SkyServiceGenres", genres.ToList());
                    m_core.FeedbackMgr.UpdateStatus($"Loaded {genres.Count()} Service Genres");
                }
            }
        }

        internal static DateTimeOffset ConvertFromUnixTime(long timeStamp) => new DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, new TimeSpan(0, 1, 0, 0)).AddSeconds(timeStamp);
    }
}