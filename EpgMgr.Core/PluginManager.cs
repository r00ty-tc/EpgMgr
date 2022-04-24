using System.Globalization;
using System.Reflection;
using EpgMgr.Plugins;

namespace EpgMgr
{
    /// <summary>
    /// Plugin Entry class
    /// </summary>
    public class PluginEntry
    {
        /// <summary>
        /// Plugin data type
        /// </summary>
        public Type PluginType { get; set; }
        /// <summary>
        /// Plugin Name
        /// </summary>
        public string PluginName { get; set; }
        /// <summary>
        /// Reference to Plugin object
        /// </summary>
        public Plugin PluginObj { get; set; }

        /// <summary>
        /// Create new plugin entry
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <param name="plugin"></param>
        public PluginEntry(Type type, string name, Plugin plugin)
        {
            PluginType = type;
            PluginName = name;
            PluginObj = plugin;
        }
    }

    /// <summary>
    /// The plugin manager class. Handles loading and managing plugins
    /// </summary>
    public class PluginManager
    {

#if SIGNED
        [DllImport("mscoree.dll", CharSet = CharSet.Unicode)]
        static extern bool StrongNameSignatureVerificationEx(string wszFilePath, bool fForceVerification, ref bool pfWasVerified); 

        private static readonly string pluginPubKey =
            @"ACQAAASAAACUAAAABgIAAAAkAABSU0ExAAQAAAEAAQARxlX6t+1egIc1MJrKwtps2mo1/bTVtCIDsNRDPIUfCmqT8H8PPThLun8mt0PCETALXhM+R+g0du22vb1Usqd1HOhP8wUYxJJyF21hQoKXAh3Wl8Y/EHLyrRCJeS2QLbIredprzcOnrT0/tNX+0tWwaVwHdeQpiE17fSzzlNBfsg==";
#endif

        private readonly List<PluginEntry> m_loadedPlugins;
        private List<PluginConfigEntry> m_pluginConfigs;
        private readonly Core m_core;
        private readonly string folderSeparator;
        /// <summary>
        /// Storage for a list of plugin console ids
        /// </summary>
        public string[] PluginConsoleNames { get; private set; }

        /// <summary>
        /// Creeate new Plugin Manage instance
        /// </summary>
        /// <param name="core"></param>
        public PluginManager(Core core)
        {
            m_core = core;
            m_loadedPlugins = new List<PluginEntry>();
            m_pluginConfigs = new List<PluginConfigEntry>();
            PluginConsoleNames = new string[] { };
            folderSeparator = Environment.OSVersion.Platform == PlatformID.Unix ? "/" : "\\";
        }

        internal IEnumerable<PluginEntry> LoadedPlugins => m_loadedPlugins;

        /// <summary>
        /// Load the plugins from the list of plugin config entries passed in
        /// </summary>
        /// <param name="enabledPlugins"></param>
        public void LoadPlugins(IEnumerable<PluginConfigEntry> enabledPlugins)
        {
            // Clear all current plugins (GC needs to deal with this)
            m_loadedPlugins.Clear();
            m_core.FeedbackMgr.UpdateStatus("Loading plugins");
            foreach (var entry in enabledPlugins)
            {
                var plugin = getPlugin($"{entry.DllFile}", true);
                if (plugin == null) continue;
                m_loadedPlugins.Add(new PluginEntry(plugin.GetType(), plugin.Name, plugin));
                m_core.FeedbackMgr.UpdateStatus($"Loaded plugin {ConsoleControl.SetFG(ConsoleColor.Green)}{plugin.Name}{ConsoleControl.SetFG(ConsoleColor.White)} V{plugin.Version}");
            }
            m_core.FeedbackMgr.UpdateStatus($"Done loading {m_loadedPlugins.Count} plugins");

            PluginConsoleNames = m_loadedPlugins.Select(row => row.PluginObj.ConsoleName).ToArray();
            m_core.CommandMgr.RefreshPlugins();
        }

        /// <summary>
        /// Return a list of all available plugins. Will scan folder if forced, or there's no plugin list yet.
        /// </summary>
        /// <param name="forceRefresh"></param>
        /// <returns></returns>
        public IEnumerable<PluginConfigEntry> GetAllPlugins(bool forceRefresh = false)
        {
            if (!forceRefresh && m_pluginConfigs.Any())
                return m_pluginConfigs;

            var fileList = Directory.GetFiles("Plugins", "*.dll");
            m_pluginConfigs = 
            (
                from file in fileList
                let plugin = getPlugin(file)
                where plugin != null
                select new PluginConfigEntry(plugin.Id.ToString(), plugin.Name, new FileInfo(file).Name, plugin.ConsoleName)
            ).ToList();

            return m_pluginConfigs;
        }
        /// <summary>
        /// Enable plugin specified by guid, console id or name.
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="consoleId"></param>
        /// <param name="name"></param>
        /// <exception cref="ArgumentException"></exception>
        public void EnablePlugin(string? guid, string? consoleId = null, string? name = null)
        {
            if (guid == null && consoleId == null && name == null)
                throw new ArgumentException("EnablePlugin invoked with no valid arguments");

            if (!m_pluginConfigs.Any())
                GetAllPlugins();

            var pluginConfig = m_pluginConfigs.FirstOrDefault(
                row => guid != null ? row.Id.Equals(guid, StringComparison.InvariantCultureIgnoreCase) :
                    1 == 0 ||
                    consoleId != null ? row.ConsoleId.Equals(consoleId, StringComparison.CurrentCultureIgnoreCase) :
                    (1 == 0 ||
                     name != null) && row.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));

            if (pluginConfig == null)
            {
                m_core.FeedbackMgr.UpdateStatus("Failed to find plugin" + (guid != null ? $"Guid: {guid}" : consoleId != null ? $"ConsoleId: {consoleId}" : name != null ? $"Name: {name}" : ""));
                return;
            }

            m_core.Config.EnabledPlugins.Add(pluginConfig);
            var plugin = getPlugin(pluginConfig.DllFile, true);
            if (plugin == null)
            {
                m_core.FeedbackMgr.UpdateStatus($"Failed to load plugin {pluginConfig.Name}");
                return;
            }
            m_core.LoadPluginConfig(plugin);
            m_core.FeedbackMgr.UpdateStatus($"Loaded plugin {plugin.Name} V{plugin.Version}");
            m_loadedPlugins.Add(new PluginEntry(plugin.GetType(), plugin.Name, plugin));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="consoleId"></param>
        /// <param name="name"></param>
        /// <exception cref="ArgumentException"></exception>
        public void DisablePlugin(string? guid, string? consoleId = null, string? name = null)
        {
            if (guid == name && consoleId == null && name == null)
                throw new ArgumentException("EnablePlugin invoked with no valid arguments");

            if (!m_pluginConfigs.Any())
                GetAllPlugins();

            var pluginConfig = m_pluginConfigs.FirstOrDefault(
                row => guid != null ? row.Id.Equals(guid, StringComparison.InvariantCultureIgnoreCase) :
                    1 == 0 ||
                    consoleId != null ? row.ConsoleId.Equals(consoleId, StringComparison.CurrentCultureIgnoreCase) :
                    (1 == 0 ||
                     name != null) && row.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));

            if (pluginConfig == null)
            {
                m_core.FeedbackMgr.UpdateStatus("Failed to find plugin" + guid != null ? $"Guid: {guid}" : "" + consoleId != null ? $"ConsoleId: {consoleId}" : "" + name != null ? $"Name: {name}" : "");
                return;
            }

            m_core.Config.EnabledPlugins.Remove(pluginConfig);
            var pluginEntry =
                m_loadedPlugins.FirstOrDefault(row => row.PluginObj.Id.ToString().Equals(pluginConfig.Id));

            if (pluginEntry == null)
            {
                m_core.FeedbackMgr.UpdateStatus("Failed to unload plugin" + guid != null ? $"Guid: {guid}" : "" + consoleId != null ? $"ConsoleId: {consoleId}" : "" + name != null ? $"Name: {name}" : "");
                return;
            }

            m_loadedPlugins.Remove(pluginEntry);
            m_core.FeedbackMgr.UpdateStatus($"Unloaded plugin {pluginConfig.Name} V{pluginEntry.PluginObj.Version}");
        }

        internal Plugin? getPlugin(string filename, bool addPluginFolder = false)
        {
            if (addPluginFolder)
                filename = $"Plugins{folderSeparator}{filename}";

            if (!File.Exists(filename))
                return null;

            var fullPath = Path.GetFullPath(filename);
            var assembly =
                Assembly.LoadFrom(fullPath);

#if SIGNED
            bool wasVerified = false;
            var validDll = StrongNameSignatureVerificationEx(fullPath, true, ref wasVerified);
            var key = System.Convert.ToBase64String(assembly.GetName().GetPublicKey());
            if (!wasVerified || !validDll || !key.Equals(pluginPubKey))
                throw new FileLoadException("Plugin not signed, or key is invalid");
#endif

            // Get exported types
            return (
                from plugin in assembly.ExportedTypes 
                where plugin.FullName != null && plugin.BaseType == typeof(Plugin) 
                select (Plugin?)assembly.CreateInstance(plugin.FullName!, false, BindingFlags.Default, null, 
                    new object[] { m_core }, CultureInfo.CurrentCulture, null)
                ).FirstOrDefault(thisPlugin => thisPlugin != null);
        }

        /// <summary>
        /// Return an array of loaded plugin names
        /// </summary>
        public string[] PluginNames => m_loadedPlugins.Select(row => row.PluginObj.Name).ToArray();
    }
}
