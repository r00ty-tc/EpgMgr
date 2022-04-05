using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using EpgMgr.Plugins;

namespace EpgMgr
{
    public class PluginEntry
    {
        public Type PluginType { get; set; }
        public string PluginName { get; set; }
        public Plugin PluginObj { get; set; }

        public PluginEntry(Type type, string name, Plugin plugin)
        {
            PluginType = type;
            PluginName = name;
            PluginObj = plugin;
        }
    }

    public class PluginManager
    {

#if SIGNED
        [DllImport("mscoree.dll", CharSet = CharSet.Unicode)]
        static extern bool StrongNameSignatureVerificationEx(string wszFilePath, bool fForceVerification, ref bool pfWasVerified); 

        private static readonly string pluginPubKey =
            @"ACQAAASAAACUAAAABgIAAAAkAABSU0ExAAQAAAEAAQARxlX6t+1egIc1MJrKwtps2mo1/bTVtCIDsNRDPIUfCmqT8H8PPThLun8mt0PCETALXhM+R+g0du22vb1Usqd1HOhP8wUYxJJyF21hQoKXAh3Wl8Y/EHLyrRCJeS2QLbIredprzcOnrT0/tNX+0tWwaVwHdeQpiE17fSzzlNBfsg==";
#endif

        private readonly List<PluginEntry> m_plugins;
        private readonly Core m_core;
        private readonly string folderSeparator;
        public string[] PluginConsoleNames { get; private set; }

        public PluginManager(Core core)
        {
            m_core = core;
            m_plugins = new List<PluginEntry>();
            PluginConsoleNames = new string[] { };
            folderSeparator = Environment.OSVersion.Platform == PlatformID.Unix ? "/" : "\\";
        }

        internal IEnumerable<PluginEntry> Plugins => m_plugins;

        public void LoadPlugins(IEnumerable<PluginConfigEntry> enabledPlugins)
        {
            // Clear all current plugins (GC needs to deal with this)
            m_plugins.Clear();
            m_core.FeedbackMgr.UpdateStatus("Loading plugins");
            foreach (var entry in enabledPlugins)
            {
                var plugin = getPlugin($"Plugins{folderSeparator}{entry.DllFile}");
                if (plugin == null) continue;
                m_plugins.Add(new PluginEntry(plugin.GetType(), plugin.Name, plugin));
                m_core.FeedbackMgr.UpdateStatus($"Loaded plugin {plugin.Name} V{plugin.Version}");
            }
            m_core.FeedbackMgr.UpdateStatus($"Done loading {m_plugins.Count} plugins");

            PluginConsoleNames = m_plugins.Select(row => row.PluginObj.ConsoleName).ToArray();
            if (m_core.CommandMgr != null)
                m_core.CommandMgr.RefreshPlugins();
        }

        internal Plugin? getPlugin(string filename)
        {
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
                select (Plugin?)assembly.CreateInstance(plugin.FullName, false, BindingFlags.Default, null, 
                    new object[] { m_core }, CultureInfo.CurrentCulture, null)
                ).FirstOrDefault(thisPlugin => thisPlugin != null);
        }

        public string[] PluginNames => m_plugins.Select(row => row.PluginObj.Name).ToArray();
    }
}
