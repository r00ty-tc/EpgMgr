﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
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
        protected Core core;

        protected Plugin(Core core)
        {
            this.core = core;
            configRoot = new ConfigEntry(null, Id.ToString(), Name);
            configTypes = new List<Type>();
            configTypes.Add(typeof(System.String));
        }

        public abstract Channel[] GetAllChannels();
        public abstract PluginErrors GenerateXmlTv(ref XmlDocument doc);

        /// <summary>
        /// Load configuration from XML to plugin storage
        /// </summary>
        /// <param name="pluginConfig"></param>
        public virtual void LoadConfig(XmlElement pluginConfig)
        {
            // Default will be to just flat deserialize to XML
            // Anything more complex should just override this method
            var serializer = new XmlSerializer(typeof(ConfigEntry), configTypes.ToArray());
            var configTemp = (ConfigEntry?)serializer.Deserialize(new XmlNodeReader(pluginConfig));
            if (configTemp != null)
                configRoot = configTemp;
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

        public abstract string[] GetValidFolders(string context);
        public abstract void RegisterConfigData(FolderEntry folderEntry);
    }
}
