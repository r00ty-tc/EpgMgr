using System.Data;
using System.Xml;
using System.Xml.Serialization;
using EpgMgr.Plugins;
using NodaTime;
using NodaTime.TimeZones;

namespace EpgMgr
{
    /// <summary>
    /// This is the core class. It contains logic for the overall functionality and contain references to the managers for specific functionality.
    /// </summary>
    public class Core : IDisposable
    {
        // ReSharper disable once CollectionNeverUpdated.Local
        private readonly List<Type> m_configTypes;
        /// <summary>
        /// Contains the filename for the configuration file
        /// </summary>
        public const string CONFIG_FILE = "Config.xml";
        /// <summary>
        /// Reference to the plugin manager
        /// </summary>
        public PluginManager PluginMgr;
        /// <summary>
        /// Reference to the user feedback manager.
        /// </summary>
        public UserFeedbackManager FeedbackMgr { get; set; }
        /// <summary>
        /// Reference to the console command manager
        /// </summary>
        public CommandManager CommandMgr;
        /// <summary>
        /// Reference to the configuration
        /// </summary>
        public Config Config { get; private set; }

        /// <summary>
        /// Create new instance of the core object. Optional feedback event handler, required to get initial loading feedback
        /// </summary>
        /// <param name="feedback"></param>
        public Core(EventHandler<FeedbackEventArgs>? feedback = null)
        {
            Config = new Config();
            m_configTypes = new List<Type>();
            FeedbackMgr = new UserFeedbackManager(feedback);
            PluginMgr = new PluginManager(this);
            
            // Load core config only
            LoadConfig(true);
            CommandMgr = new CommandManager(this);
            LoadConfig();
            CommandMgr.RefreshPlugins();
        }

        /// <summary>
        /// Save the configuration (core and plugins)
        /// </summary>
        /// <returns></returns>
        public long SaveConfig()
        {
            Config.PreSaveConfig();
            // Main config save
            var configXml = new XmlDocument();
            var declaration = configXml.CreateXmlDeclaration("1.0", "UTF-8", null);
            var rootNode = configXml.DocumentElement;
            configXml.InsertBefore(declaration, rootNode);
            using (var xmlWriter = configXml.CreateNavigator()?.AppendChild())
            {
                if (xmlWriter != null)
                {
                    var serializer = new XmlSerializer(typeof(Config), m_configTypes.ToArray());
                    serializer.Serialize(xmlWriter, Config);
                }
            }

            // We add plugin configs direct to XML just in case a plugin writer needs some custom XML code to store their config
            var pluginConfigsElement = configXml.CreateElement("PluginConfigs");
            // Gather all unique extra types for each plugin
            foreach (var plugin in PluginMgr.LoadedPlugins)
            {
                var configNode = plugin.PluginObj.SaveConfig();
                if (configNode == null)
                    continue;

                var node = (XmlElement) configXml.ImportNode(configNode, true);

                // This removes the xmlns attributes that appear on every plugin config, without breaking dynamic type conversion
                node.Attributes.RemoveNamedItem("xmlns:xsi");
                node.Attributes.RemoveNamedItem("xmlns:xsd");
                pluginConfigsElement.AppendChild(node);
            }
            configXml.DocumentElement?.AppendChild(pluginConfigsElement);

            // Actually save file
            configXml.Save(CONFIG_FILE);
            return new FileInfo(CONFIG_FILE).Length;
        }

        /// <summary>
        /// Load the configuration (core and plugins)
        /// </summary>
        /// <param name="coreOnly"></param>
        /// <exception cref="DataException"></exception>
        /// <exception cref="Exception"></exception>
        public void LoadConfig(bool coreOnly = false)
        {
            var configXml = new XmlDocument();
            if (!File.Exists(CONFIG_FILE))
                SaveConfig();
            configXml.Load(CONFIG_FILE);

            // Main config load
            var serializer = new XmlSerializer(typeof(Config), m_configTypes.ToArray());
            if (configXml.DocumentElement == null)
                throw new DataException("Configuration document element is null.");

            var configTemp = (Config?)serializer.Deserialize(new XmlNodeReader(configXml.DocumentElement));
            if (configTemp != null)
                Config = configTemp;

            if (coreOnly) return;
            var plugins = PluginMgr.PluginNames;

            PluginMgr.LoadPlugins(Config.EnabledPlugins);

            // Now load plugin configs
            var pluginConfigs = configXml.DocumentElement.GetElementsByTagName("PluginConfigs").Item(0);

            if (pluginConfigs == null)
                throw new Exception($"Configuration file {CONFIG_FILE} doesn't contain a plugin configuration section");

            foreach (XmlElement pluginElement in pluginConfigs)
            {
                var pluginId = pluginElement.GetAttribute("Id");
                var plugin = PluginMgr.LoadedPlugins.FirstOrDefault(row => row.PluginObj.Id.ToString().Equals(pluginId));
                plugin?.PluginObj.LoadConfig(pluginElement);
            }
            Config.PostLoadConfig();
        }

        internal void LoadPluginConfig(Plugin plugin)
        {
            var configXml = new XmlDocument();
            if (!File.Exists(CONFIG_FILE))
                SaveConfig();
            configXml.Load(CONFIG_FILE);

            // Now load plugin configs
            var pluginConfigs = (XmlElement?)configXml.DocumentElement?.GetElementsByTagName("PluginConfigs").Item(0);

            if (pluginConfigs == null)
                throw new Exception($"Configuration file {CONFIG_FILE} doesn't contain a plugin configuration section");

            var pluginElement = (XmlElement?)pluginConfigs.GetElementsByTagName(plugin.Id.ToString()).Item(0);
            plugin.LoadConfig(pluginElement);
        }

        /// <summary>
        /// Create the XMLTV file (from all enabled plugins)
        /// </summary>
        public void MakeXmlTV()
        {
            // If file exists open it. If not, make a new xmltv
            XmlTV.XmlTV? xmltvFile = null;
            if (File.Exists(Config.XmlTvConfig.Filename))
            {
                FeedbackMgr.UpdateStatus("Loading existing XMLTV file");
                xmltvFile = XmlTV.XmlTV.Load(Config.XmlTvConfig.Filename);
            }

            xmltvFile ??= new XmlTV.XmlTV(DateTime.Today);

            // Trim days out of date
            var oldPrograms = xmltvFile.Programmes.Where(row =>
                row.StartTime < DateTime.Today.AddDays(0 - Config.XmlTvConfig.MaxDaysBehind)).ToArray();
            var newProgrammes = xmltvFile.Programmes.Where(row =>
                row.StartTime > DateTime.Today.AddDays(Config.XmlTvConfig.MaxDaysAhead)).ToArray();

            foreach (var programme in oldPrograms)
                xmltvFile.DeleteProgramme(programme.StartTime, programme.Channel);
            foreach (var programme in newProgrammes)
                xmltvFile.DeleteProgramme(programme.StartTime, programme.Channel);

            // Update channels from plugins
            xmltvFile.Channels.Clear();
            foreach (var plugin in PluginMgr.LoadedPlugins)
            {
                var channels = plugin.PluginObj.GetXmlTvChannels();
                xmltvFile.Channels.AddRange(channels);
            }

            // Update links (mainly we want to remove data for channels that no longer exist)
            xmltvFile.UpdateData();

            // Update programs from plugins
            foreach (var plugin in PluginMgr.LoadedPlugins)
                plugin.PluginObj.GenerateXmlTv(ref xmltvFile);

            FeedbackMgr.UpdateStatus("Saving XMLTV");
            xmltvFile.Save(Config.XmlTvConfig.Filename);
            var info = new FileInfo(Config.XmlTvConfig.Filename);
            FeedbackMgr.UpdateStatus($"Wrote {info.Length} bytes to {Config.XmlTvConfig.Filename}");
        }

        /// <summary>
        /// Add channel alias, will add to both way lookup tables
        /// </summary>
        /// <param name="channelName"></param>
        /// <param name="alias"></param>
        public void AddAlias(string channelName, string alias)
        {
            if (Config.ChannelNameToAlias.ContainsKey(channelName)) return;
            Config.ChannelNameToAlias.Add(channelName, alias);
            Config.ChannelAliasToName.Add(alias, channelName);
        }

        /// <summary>
        /// Remove alias from both way lookup tables
        /// </summary>
        /// <param name="channelName"></param>
        public void RemoveAlias(string channelName)
        {
            if (Config.ChannelNameToAlias.TryGetValue(channelName, out var alias))
            {
                Config.ChannelAliasToName.Remove(alias);
                Config.ChannelNameToAlias.Remove(channelName);
            }
        }

        /// <summary>
        /// Return channel name based on provided alias. If nullIfNotFound is not set, the alias will be returned if the alias is not found.
        /// </summary>
        /// <param name="alias"></param>
        /// <param name="nullIfNotFound"></param>
        /// <returns></returns>
        public string? GetChannelNameFromAlias(string alias, bool nullIfNotFound = false)
        {
            if (Config.ChannelAliasToName.TryGetValue(alias, out var channelName))
                return channelName;

            return nullIfNotFound ? null : alias;
        }

        /// <summary>
        /// Return alias based on provided channel name. If nottIfNotFound is not set, the channel name will be returned if no alias has been set for the provided channel.
        /// </summary>
        /// <param name="channelName"></param>
        /// <param name="nullIfNotFound"></param>
        /// <returns></returns>
        public string? GetAliasFromChannelName(string? channelName, bool nullIfNotFound = false)
        {
            if (channelName == null) return null;
            if (Config.ChannelNameToAlias.TryGetValue(channelName, out var alias))
                return alias;

            return nullIfNotFound ? null : channelName;

        }

        /// <summary>
        /// Returns all plugins (enabled or not). Will scan plugins folder.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<PluginConfigEntry> GetAllPlugins() => PluginMgr.GetAllPlugins();

        /// <summary>
        /// Returns enabled plugins.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Plugin> GetActivePlugins() => PluginMgr.LoadedPlugins.Select(row => row.PluginObj);

        /// <summary>
        /// Uses command manager to handle the specified command text
        /// </summary>
        /// <param name="context"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        public string HandleCommand(ref FolderEntry context, string command) => CommandMgr.HandleCommand(ref context, command);

        /// <summary>
        /// Disposal of other IDisposables
        /// </summary>
        public void Dispose()
        {
            FeedbackMgr.Dispose();
        }

        /// <summary>
        /// Return a list of valid timezones
        /// </summary>
        /// <returns></returns>
        public static string[] GetTimezones() => DateTimeZoneProviders.Tzdb.Ids.ToArray();

        /// <summary>
        /// Return local timezone if possible, otherwise UTC
        /// </summary>
        /// <returns></returns>
        public static string GetLocalTimezone()
        {
            try
            {
                return DateTimeZoneProviders.Tzdb.GetSystemDefault().Id;
            }
            catch
            {
                return "Etc/UTC";
            }
        }
        /// <summary>
        /// Convert unix time (seconds since 01/01/1970) to UTC DateTime
        /// </summary>
        /// <param name="timeStamp"></param>
        /// <returns></returns>
        public static DateTime ConvertFromUnixTimeUTC(long timeStamp) => new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(timeStamp);

        /// <summary>
        /// Static method to convert a UTC time to a specified timezone
        /// </summary>
        /// <param name="utcDateTime"></param>
        /// <param name="zoneId"></param>
        /// <returns></returns>
        /// <exception cref="DateTimeZoneNotFoundException"></exception>
        public static DateTimeOffset ConvertFromUTCToTimezone(DateTime utcDateTime, string zoneId)
        {
            var zone = DateTimeZoneProviders.Tzdb.GetZoneOrNull(zoneId);
            if (zone == null)
                throw new DateTimeZoneNotFoundException($"Invalid time zone {zoneId}");
            var zoneTime = new ZonedDateTime(Instant.FromDateTimeUtc(utcDateTime), zone);
            return zoneTime.ToDateTimeOffset();
        }

        /// <summary>
        /// Convert a specified UTC time to the timezone configured for XmlTV files
        /// </summary>
        /// <param name="utcDateTime"></param>
        /// <returns></returns>
        public DateTimeOffset ConvertFromUTCToTimezone(DateTime utcDateTime) => ConvertFromUTCToTimezone(utcDateTime, Config.XmlTvConfig.TimeZone);

    }
}