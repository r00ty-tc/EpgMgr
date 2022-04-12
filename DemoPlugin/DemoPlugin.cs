using System.Data;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
using EpgMgr;

namespace EpgMgr.Plugins
{
    public partial class DemoPlugin : Plugin
    {
        public override Guid Id => Guid.Parse("b2e441ae-ac93-467e-b6a9-19782bf7c011");
        public override string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString();
        public override string Name => "Demo Plugin";
        public override string ConsoleName => "Demo";

        public override string Author => "EpgMgr Core Team";
        public override Channel[] GetAllChannels()
        {
            var allChannels = configRoot.GetList<Channel>("ChannelsAvailable") ?? new List<Channel>();
            return allChannels.ToArray();
        }

        public override PluginErrors GenerateXmlTv(ref XmlDocument doc)
        {
            throw new NotImplementedException();
        }

        public override string[] GetValidFolders(string context)
        {
            throw new NotImplementedException();
        }

        public override void RegisterConfigData(FolderEntry folderEntry)
        {
            // Custom paths
            folderEntry.AddChildFolder("TestFolder");
            folderEntry.AddChildValue("TestInt", setGetConfigValue, ValueType.ConfigValueType_Int32);
            folderEntry.AddChildValue("TestString", setGetConfigValue, ValueType.ConfigValueType_String);
            RegisterCommands(folderEntry);
        }

        public DemoPlugin(Core core) : base(core)
        {
            var thisCore = core;
            configTypes.Add(typeof(Channel));
            configTypes.Add(typeof(CustomTag));
            InitConfig();
            InitChannels();
        }

        private void InitConfig()
        {
            ConfigEntry.NewConfigList(configRoot, "ChannelsSubbed", null, new List<Channel>());
            ConfigEntry.NewConfigList(configRoot, "ChannelsAvailable", null, new List<Channel>());
            ConfigEntry.NewConfigEntry<string>(configRoot, "TestString", "Hello world");
            ConfigEntry.NewConfigEntry<int>(configRoot, "TestInt", 12);
        }

        private void InitChannels()
        {
            var subChannels = configRoot.GetList<Channel>("ChannelsSubbed") ?? new List<Channel>();
            var allChannels = configRoot.GetList<Channel>("ChannelsAvailable") ?? new List<Channel>();
            allChannels.Add(new Channel("BBC1", "BBC One"));
            allChannels.Add(new Channel("BBC2", "BBC Two"));
            allChannels.Add(new Channel("ITV", "ITV"));
            configRoot.SetList<Channel>("ChannelsAvailable", allChannels);
            subChannels.Add(allChannels.FirstOrDefault(row => row.Id.Equals("BBC2")));
            configRoot.SetList<Channel>("ChannelsSubbed", subChannels);
        }
        private void setGetConfigValue(FolderEntry context, string valuename, ValueType type, ref dynamic? value)
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
    }
}