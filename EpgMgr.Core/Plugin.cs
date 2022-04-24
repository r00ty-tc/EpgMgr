using System.Data;
using System.Xml;
using System.Xml.Serialization;

namespace EpgMgr.Plugins
{
    /// <summary>
    /// The abstract/template class for plugins. This should be inherited with required items implemented in order to create a plugin
    /// </summary>
    public abstract class Plugin
    {
        /// <summary>
        /// A unique GUID for the plugin
        /// </summary>
        public abstract Guid Id { get; }
        /// <summary>
        /// Plugin version info
        /// </summary>
        public abstract string Version { get; }
        /// <summary>
        /// Plugin name
        /// </summary>
        public abstract string Name { get; }
        /// <summary>
        /// Name for the plugin to be shown in console folder structure
        /// </summary>
        public abstract string ConsoleName { get; }
        /// <summary>
        /// Plugin author
        /// </summary>
        public virtual string Author => string.Empty;
        /// <summary>
        /// The root configuration folder for the plugin
        /// </summary>
        protected ConfigEntry configRoot;
        /// <summary>
        /// List of custom types to be used when saving/loading the configuration.
        /// </summary>
        protected List<Type> configTypes;
        /// <summary>
        /// A reference to the core object
        /// </summary>
        protected Core m_core;

        /// <summary>
        /// Create a new instance of the plugin. A public version should be made in plugins inheriting this class.
        /// </summary>
        /// <param name="mCore"></param>
        protected Plugin(Core mCore)
        {
            this.m_core = mCore;
            configRoot = new ConfigEntry(null, Id.ToString(), Name);
            configTypes = new List<Type> { typeof(string) };
        }

        /// <summary>
        /// Method to get XmlTV channels. The method needs to be implemented and return an array of XmlTv channeels that will be in the xmltv file.
        /// </summary>
        /// <returns></returns>
        public abstract EpgMgr.Channel[] GetXmlTvChannels();
        /// <summary>
        /// Method to generate the XmlTv records. The method needs to be implemented and take the referenced xmltv object and add/remove programmes. It must respect the programmes from other plugins.
        /// </summary>
        /// <param name="xmltv"></param>
        /// <returns></returns>
        public abstract PluginErrors GenerateXmlTv(ref XmlTV.XmlTV xmltv);

        /// <summary>
        /// Load configuration from XML to plugin storage
        /// </summary>
        /// <param name="pluginConfig"></param>
        public virtual void LoadConfig(XmlElement? pluginConfig)
        {
            if (pluginConfig == null) return;
            // Default will be to just flat deserialize to XML
            // Anything more complex should just override this method
            using (var xmlReader = pluginConfig.CreateNavigator()?.ReadSubtree())
            {
                if (xmlReader == null) return;
                var serializer = new XmlSerializer(typeof(ConfigEntry), configTypes.ToArray());
                var configTemp = (ConfigEntry?)serializer.Deserialize(xmlReader);
                if (configTemp != null)
                    configRoot = configTemp;
            }
        }

        /// <summary>
        /// Create configuration data for this plugin. Can be overridden
        /// </summary>
        /// <returns></returns>
        public virtual XmlElement? SaveConfig()
        {
            // Default will be to just flat serialize to XML
            // Anything more complex should just override this method
            var doc = new XmlDocument();

            using (var xmlWriter = doc.CreateNavigator()?.AppendChild())
            {
                if (xmlWriter == null) return null;
                var serializer = new XmlSerializer(typeof(ConfigEntry), configTypes.ToArray());
                serializer.Serialize(xmlWriter, configRoot);
            }

            return doc.DocumentElement;
        }

        /// <summary>
        /// A generic method to get/set values for the types currently handled. Can be overridden and new handlers written in plugins. This is just a convenience
        /// </summary>
        /// <param name="context"></param>
        /// <param name="valuename"></param>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <exception cref="DataException"></exception>
        protected virtual void setGetConfigValue(FolderEntry context, string valuename, ValueType type, ref dynamic? value)
        {
            if (value == null)
            {
                value = type switch
                {
                    ValueType.ConfigValueType_Int32 => configRoot.GetValue<int>(valuename),
                    ValueType.ConfigValueType_Int64 => configRoot.GetValue<long>(valuename),
                    ValueType.ConfigValueType_String => configRoot.GetValue<string>(valuename),
                    ValueType.ConfigValueType_Bool => configRoot.GetValue<bool>(valuename),
                    ValueType.ConfigValueType_Decimal => configRoot.GetValue<decimal>(valuename),
                    ValueType.ConfigValueType_Double => configRoot.GetValue<double>(valuename),
                    _ => throw new DataException("Invalid type")
                };
            }
            else
            {
                switch (type)
                {
                    case ValueType.ConfigValueType_Int32:
                        configRoot.SetValue<int>(valuename, value);
                        break;
                    case ValueType.ConfigValueType_Int64:
                        configRoot.SetValue<long>(valuename, value);
                        break;
                    case ValueType.ConfigValueType_String:
                        configRoot.SetValue<string>(valuename, value);
                        break;
                    case ValueType.ConfigValueType_Bool:
                        configRoot.SetValue<bool>(valuename, value);
                        break;
                    case ValueType.ConfigValueType_Decimal:
                        configRoot.SetValue<decimal>(valuename, value);
                        break;
                    case ValueType.ConfigValueType_Double:
                        configRoot.SetValue<double>(valuename, value);
                        break;
                    default:
                        throw new DataException("Invalid type");
                }
            }
        }

        /// <summary>
        /// This method must be implemented. If should register information about configuration folders and values as well as register any console commands for the plugin
        /// </summary>
        /// <param name="folderEntry"></param>
        public abstract void RegisterConfigData(FolderEntry folderEntry);
    }
}
