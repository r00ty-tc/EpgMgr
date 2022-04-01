using System.Xml;
using System.Xml.Serialization;
using EpgMgr;

namespace EpgMgr.Plugins
{
    public partial class DemoPlugin : Plugin
    {
        public override Guid Id => Guid.Parse("b2e441ae-ac93-467e-b6a9-19782bf7c011");
        public override string Version => "1.0";
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
            folderEntry.AddChild("TestFolder");

            // Custom global commands
            core.CommandMgr.RegisterCommand("hello", CommandHandlerHELLO, this);
            core.CommandMgr.RegisterCommand("listchannels", CommandHandlerLISTCHANNELS, this, folderEntry);
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
    }
}