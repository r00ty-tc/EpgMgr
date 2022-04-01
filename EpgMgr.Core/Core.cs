using System.Xml;
using System.Xml.Serialization;
using EpgMgr.Plugins;

namespace EpgMgr
{
    public class Core
    {
        public static readonly string CONFIG_FILE = "Config.xml";
        public PluginManager PluginMgr;
        public UserFeedbackManager FeedbackMgr { get; set; }
        private Config m_config;
        private List<Type> m_configTypes;
        public CommandManager CommandMgr;
        public Core()
        {
            m_config = new Config();
            m_configTypes = new List<Type>();
            FeedbackMgr = new UserFeedbackManager();
            PluginMgr = new PluginManager(this);
            
            // Load core config only
            LoadConfig(true);
            CommandMgr = new CommandManager(this);
            LoadConfig();
            CommandMgr.RefreshPlugins();
            //SaveConfig();
        }

        public long SaveConfig()
        {
            // Main config save
            var configXml = new XmlDocument();
            var declaration = configXml.CreateXmlDeclaration("1.0", "UTF-8", null);
            var rootNode = configXml.DocumentElement;
            configXml.InsertBefore(declaration, rootNode);
            using (XmlWriter xmlWriter = configXml.CreateNavigator().AppendChild())
            {
                var serializer = new XmlSerializer(typeof(Config), m_configTypes.ToArray());

                serializer.Serialize(xmlWriter, m_config);
            }

            // We add plugin configs direct to XML just in case a plugin writer needs some custom XML code to store their config
            var pluginConfigsElement = configXml.CreateElement("PluginConfigs");
            // Gather all unique extra types for each plugin
            foreach (var plugin in PluginMgr.Plugins)
            {
                var configNode = plugin.PluginObj.SaveConfig();
                var node = (XmlElement) configXml.ImportNode(configNode, true);
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

            if (!coreOnly)
            {
                var plugins = PluginMgr.PluginNames;
                if (m_config.EnabledPlugins == null || !plugins.Any())
                {
                    var allPlugins = GetAllPlugins();
                    m_config.EnabledPlugins = allPlugins.ToList();
                }
                PluginMgr.LoadPlugins(m_config.EnabledPlugins);

                // Now load plugin configs
                var pluginConfigs = configXml.DocumentElement.GetElementsByTagName("PluginConfigs").Item(0);
                foreach (XmlElement pluginElement in pluginConfigs)
                {
                    var pluginId = pluginElement.GetAttribute("Id");
                    var plugin = PluginMgr.Plugins.FirstOrDefault(row => row.PluginObj.Id.ToString().Equals(pluginId));
                    if (plugin != null)
                        plugin.PluginObj.LoadConfig(pluginElement);
                }
            }
        }

        public IEnumerable<PluginConfigEntry> GetAllPlugins()
        {
            var fileList = Directory.GetFiles("Plugins", "*.dll");
            List<PluginConfigEntry> pluginList = new List<PluginConfigEntry>();

            foreach (var file in fileList)
            {
                var plugin = PluginMgr.getPlugin(file);
                if (plugin != null)
                {
                    pluginList.Add(new PluginConfigEntry(plugin.Id.ToString(), plugin.Name, new FileInfo(file).Name));
                }
            }

            return pluginList;
        }

        public IEnumerable<Plugin> GetActivePlugins() => PluginMgr.Plugins.Select(row => row.PluginObj);

        public string HandleCommand(ref FolderEntry context, string command) => CommandMgr.HandleCommand(ref context, command);
    }
}