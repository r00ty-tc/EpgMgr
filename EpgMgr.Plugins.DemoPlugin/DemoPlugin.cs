using System.Reflection;

namespace EpgMgr.Plugins

{
    public partial class DemoPlugin : Plugin
    {
        public override Guid Id => Guid.Parse("b2e441ae-ac93-467e-b6a9-19782bf7c011");
        public override string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString();
        public override string Name => "Demo Plugin";
        public override string ConsoleName => "Demo";

        public override string Author => "EpgMgr Core Team";
        public override EpgMgr.Channel[] GetXmlTvChannels()
        {
            var subbedChannels = configRoot.GetList<Channel>("ChannelsSubbed") ?? new List<Channel>();
            var returnChannels = new List<EpgMgr.Channel>();
            foreach (var channel in subbedChannels)
            {
                returnChannels.Add(new EpgMgr.Channel(channel.Id, channel.Name, null, channel.LogoUrl));
            }
            return returnChannels.ToArray();
        }

        public override PluginErrors GenerateXmlTv(ref XmlTV.XmlTV xmltv)
        {
            return new PluginErrors();
        }

        public override void RegisterConfigData(FolderEntry folderEntry)
        {
            // Custom paths
            folderEntry.AddChildFolder("TestFolder");
            folderEntry.AddChildValue("TestInt", setGetConfigValue, ValueType.ConfigValueType_Int32);
            folderEntry.AddChildValue("TestString", setGetConfigValue, ValueType.ConfigValueType_String);
            RegisterCommands(folderEntry);
        }

        public DemoPlugin(Core mCore) : base(mCore)
        {
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
    }
}