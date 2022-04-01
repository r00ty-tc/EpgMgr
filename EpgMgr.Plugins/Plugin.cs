using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace EpgMgr.Plugins
{
    public class CustomTag
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public bool IncludeInXml { get; set; }

        public CustomTag(string key, string value, bool includeInXml = false)
        {
            Key = key;
            Value = value;
            IncludeInXml = includeInXml;
        }
    }
    public class Channel
    {
        public string Id { get; set; }
        public string LookupKey { get; set; }
        public string? LineupId { get; set; }
        public string? Name { get; set; }
        public string? Callsign { get; set; }
        public string? Affiliate { get; set; }
        public int? ChannelNo { get; set; }
        public int? SubChannelNo { get; set; }
        public List<CustomTag> CustomTags { get; set; }

        public Channel(string id, string? name = null, string? lineupId = null, string? callsign = null,
            string? affiliate = null, int? channelNo = null, int? subChannelNo = null, string? lookupKey = null, List<CustomTag>? customTags = null)
        {
            Id = id;
            LineupId = lineupId;
            Name = name;
            Callsign = callsign;
            Affiliate = affiliate;
            ChannelNo = channelNo;
            SubChannelNo = subChannelNo;
            CustomTags = customTags ?? new List<CustomTag>();
            LookupKey = lookupKey ?? id;
        }

        public void AddTag(string key, string value, bool includeInXml = false) => CustomTags.Add(new CustomTag(key, value, includeInXml));
        public CustomTag? GetTag(string key) => CustomTags.FirstOrDefault(row => row.Key.Equals(key));
        public void RemoveTag(string key) => CustomTags.Remove(GetTag(key));
        public bool HasTag(string key) => CustomTags.Any(row => row.Key.Equals(key));
    }

    public class PluginErrors
    {
        public bool HasErrors => Errors.Any();
        public bool HasWarnings => Warnings.Any();
        public List<string> Errors { get; set; }
        public List<string> Warnings { get; set; }
        public List<string> InformationMessages { get; set; }
        public List<string> DebugMessages { get; set; }

        public PluginErrors()
        {
            Errors = new List<string>();
            Warnings = new List<string>();
            InformationMessages = new List<string>();
            DebugMessages = new List<string>();
        }

        public void AddError(string message) => Errors.Add(message);
        public void AddWarning(string message) => Warnings.Add(message);
        public void AddInfoMessage(string message) => InformationMessages.Add(message);
        public void AddDebugMessage(string message) => DebugMessages.Add(message);
    }

    public abstract class Plugin
    {
        public abstract string Version { get; }
        public abstract string Name { get; }
        public virtual string Author => string.Empty;
        private Core core;

        protected Plugin(Core core)
        {
            this.core = core;
        }

        public abstract Channel[] GetAllChannels();
        public abstract PluginErrors GenerateXmlTv(ref XmlDocument doc);
    }
}
