using System.Xml;
using System.Xml.Serialization;
using EpgMgr.Plugins;

namespace EpgMgr
{
    public class Core
    {
        private Config m_config;
        private List<Type> m_configTypes;
        public static readonly string CONFIG_FILE = "Config.xml";
        public PluginManager PluginMgr;
        public UserFeedbackManager FeedbackMgr { get; set; }
        public CommandManager CommandMgr;
        public Config Config => m_config;

        public Core(EventHandler<FeedbackEventArgs>? feedback = null)
        {
            m_config = new Config();
            m_configTypes = new List<Type>();
            FeedbackMgr = new UserFeedbackManager(feedback);
            PluginMgr = new PluginManager(this);
            
            // Load core config only
            LoadConfig(true);
            CommandMgr = new CommandManager(this);
            LoadConfig();
            CommandMgr.RefreshPlugins();
        }

        public long SaveConfig()
        {
            // Main config save
            var configXml = new XmlDocument();
            var declaration = configXml.CreateXmlDeclaration("1.0", "UTF-8", null);
            var rootNode = configXml.DocumentElement;
            configXml.InsertBefore(declaration, rootNode);
            using (var xmlWriter = configXml.CreateNavigator().AppendChild())
            {
                var serializer = new XmlSerializer(typeof(Config), m_configTypes.ToArray());
                serializer.Serialize(xmlWriter, m_config);
            }

            // We add plugin configs direct to XML just in case a plugin writer needs some custom XML code to store their config
            var pluginConfigsElement = configXml.CreateElement("PluginConfigs");
            // Gather all unique extra types for each plugin
            foreach (var plugin in PluginMgr.LoadedPlugins)
            {
                var configNode = plugin.PluginObj.SaveConfig();
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

        public void LoadConfig(bool coreOnly = false)
        {
            var configXml = new XmlDocument();
            if (!File.Exists(CONFIG_FILE))
                SaveConfig();
            configXml.Load(CONFIG_FILE);

            // Main config load
            var serializer = new XmlSerializer(typeof(Config), m_configTypes.ToArray());
            var configTemp = (Config?)serializer.Deserialize(new XmlNodeReader(configXml.DocumentElement));
            if (configTemp != null)
                m_config = configTemp;

            if (coreOnly) return;
            var plugins = PluginMgr.PluginNames;
            /*if (m_config.EnabledPlugins == null || !plugins.Any())
            {
                var allPlugins = GetAllPlugins();
                m_config.EnabledPlugins = allPlugins.ToList();
            }*/
            PluginMgr.LoadPlugins(m_config.EnabledPlugins);

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
        }

        public IEnumerable<PluginConfigEntry> GetAllPlugins() => PluginMgr.GetAllPlugins();

        public IEnumerable<Plugin> GetActivePlugins() => PluginMgr.LoadedPlugins.Select(row => row.PluginObj);

        public string HandleCommand(ref FolderEntry context, string command) => CommandMgr.HandleCommand(ref context, command);
    }
}