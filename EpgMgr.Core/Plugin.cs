using System.Data;
using System.Xml;
using System.Xml.Serialization;

namespace EpgMgr.Plugins
{
    public abstract class Plugin
    {
        public abstract Guid Id { get; }
        public abstract string Version { get; }
        public abstract string Name { get; }
        public abstract string ConsoleName { get; }
        public virtual string Author => string.Empty;
        protected ConfigEntry configRoot;
        protected List<Type> configTypes;
        protected Core m_core;

        protected Plugin(Core mCore)
        {
            this.m_core = mCore;
            configRoot = new ConfigEntry(null, Id.ToString(), Name);
            configTypes = new List<Type>();
            configTypes.Add(typeof(System.String));
        }

        public abstract EpgMgr.Channel[] GetXmlTvChannels();
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
            using (XmlReader xmlReader = pluginConfig.CreateNavigator().ReadSubtree())
            {
                var serializer = new XmlSerializer(typeof(ConfigEntry), configTypes.ToArray());
                var configTemp = (ConfigEntry)serializer.Deserialize(xmlReader);
                if (configTemp != null)
                    configRoot = configTemp;
            }
        }

        public virtual XmlElement? SaveConfig()
        {
            // Default will be to just flat serialize to XML
            // Anything more complex should just override this method
            var doc = new XmlDocument();

            using (XmlWriter xmlWriter = doc.CreateNavigator().AppendChild())
            {
                var serializer = new XmlSerializer(typeof(ConfigEntry), configTypes.ToArray());
                serializer.Serialize(xmlWriter, configRoot);
            }

            return doc.DocumentElement;
        }

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

        public abstract void RegisterConfigData(FolderEntry folderEntry);
    }
}
