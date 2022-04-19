using EpgMgr.XmlTV;

namespace EpgMgr.Plugins
{
    public partial class DemoPlugin
    {
        public void RegisterCommands(FolderEntry folderEntry)
        {
            // Custom global commands
            m_core.CommandMgr.RegisterCommand("progresstest", CommandHandlerPROGRESSTEST, null, this);
            m_core.CommandMgr.RegisterCommand("xmltv", CommandHandlerXMLTV, $"xmltv: Test command for xmltv class development{Environment.NewLine}Usage: xmltv [load] [test]",this, null, 1);

            // Custom local commands
            m_core.CommandMgr.RegisterCommand("listchannels", CommandHandlerLISTCHANNELS, $"Lists channels enabled or available{Environment.NewLine}Usage: listchannels [all]",this, folderEntry);
        }

        public static string CommandHandlerPROGRESSTEST(Core core, ref FolderEntry context, string command, string[] args)
        {
            // Progress bar test
            core.FeedbackMgr.UpdateStatus("Progress bar test",0,250);
            for (int i = 0; i <= 250; i++)
            {
                core.FeedbackMgr.UpdateStatus(null, i);
                Thread.Sleep(10);
            }
            core.FeedbackMgr.UpdateStatus("Progress complete");
            return "Hello World";
        }

        public static string CommandHandlerXMLTV(Core core, ref FolderEntry context, string command,
            string[] args)
        {
            if (args.Length < 1)
                return "Invalid arguments. Try xmltv load <file> or xmltv test";

            // TEST: Load existing config 
            if (args[0].Equals("load", StringComparison.InvariantCultureIgnoreCase))
            {
                if (args.Length != 2)
                    return "xmltv load requires a filename as argument";

                if (!File.Exists(args[1]))
                    return $"{args[1]}: File not found";

                var startTime = DateTime.UtcNow.Ticks;
                var test = XmlTV.XmlTV.Load(args[1]);
                var endTime = DateTime.UtcNow.Ticks;
                var diff = new TimeSpan(endTime - startTime);
                return $"Loaded {args[1]} in {diff.TotalMilliseconds}ms";
            }

            if (args[0].Equals("test", StringComparison.InvariantCultureIgnoreCase))
            {
                var fileName = args.Length == 2 ? args[1] : "xmltvtest.xml";
                var xmltv = new XmlTV.XmlTV(DateTime.Now, "Demo test", null, "EpgMgr/Demo", null);
                var channel = xmltv.GetNewChannel("BBC1", "BBC One", "en", "http://icon.com", null, null, "http://example.com");
                channel.AddDisplayName("Test german name", "de");
                channel.AddDisplayName("Test french name", "fr");
                channel.AddIcon("http://logo.com/logo1.png", 100, 120);
                channel.AddUrl("http://nowhere.com", "test");
                var programme = xmltv.GetNewProgramme(DateTime.Now, channel.Id, "The test programme", DateTime.Now.AddHours(1),
                    "The revenge", null, "English", "Drama", "en", "en", null, "en");
                programme.AddIcon("http://icons.com", 150, 200);
                programme.AddEpisodeNum("S01E02");
                programme.AddSubtitleInfo("teletext");
                programme.AddCredit(CreditType.CreditDirector, "Steven Spielberg", new Image("http://images.com/spielberg"), new XmlTvUrl("http://spielberg.com"));
                programme.AddActor("Harold Lloyd", "Johnny boy", "yes");
                programme.AddActor("John Smith", "Ratfield", null, null, new XmlTvUrl("http://imdb.com/johnsmith", "imdb"));
                xmltv.Save(fileName);
                var test = XmlTV.XmlTV.Load(fileName);
                return File.Exists(fileName) ?
                    $"Created file {fileName} with {new FileInfo(fileName).Length}" :
                    $"Failed to write file {fileName}";
            }

            return "Invalid command";
        }

        public string CommandHandlerLISTCHANNELS(Core core, ref FolderEntry context, string command, string[] args)
        {
            if (args.Length > 1 || (args.Length == 1 && !args.FirstOrDefault().Equals("all")))
                return "Invalid arguments. Use listchannels [all].";

            var allMode = args.FirstOrDefault() != null &&
                          args.FirstOrDefault().Equals("all", StringComparison.InvariantCultureIgnoreCase);

            var result = string.Empty;
            if (!allMode)
            {
                result = "Subscribed Channels:" + Environment.NewLine;
                result = (configRoot.GetList<Channel>("ChannelsSubbed") ?? 
                          new List<Channel>()).Aggregate(result, (current, channel) => current + ($"{channel.Id,-10}{channel.Name,-25}" + Environment.NewLine));
            }
            else
            {
                result = "All Channels:" + Environment.NewLine;
                result = (configRoot.GetList<Channel>("ChannelsAvailable") ?? 
                          new List<Channel>()).Aggregate(result, (current, channel) => current + ($"{channel.Id,-10}{channel.Name,-25}" + Environment.NewLine));
            }

            return result;
        }
    }
}
