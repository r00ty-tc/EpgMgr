using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpgMgr.Plugins
{
    public partial class DemoPlugin
    {
        public void RegisterCommands(FolderEntry folderEntry)
        {
            // Custom global commands
            core.CommandMgr.RegisterCommand("progresstest", CommandHandlerPROGRESSTEST, this);
            core.CommandMgr.RegisterCommand("xmltvtest", CommandHandlerXMLTVTEST, this);

            // Custom local commands
            core.CommandMgr.RegisterCommand("listchannels", CommandHandlerLISTCHANNELS, this, folderEntry);
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

        public static string CommandHandlerXMLTVTEST(Core core, ref FolderEntry context, string command,
            string[] args)
        {
            var fileName = args.FirstOrDefault() ?? "xmltvtest.xml";
            var xmltv = new XmlTV.XmlTV(DateTime.Now, "Demo test", null, "EpgMgr/Demo", null);
            var channel = xmltv.GetNewChannel("BBC1", "BBC One", "en", "http://icon.com", null, null, "http://example.com");
            channel.AddDisplayName("Test german name", "de");
            channel.AddDisplayName("Test french name", "fr");
            channel.AddIcon("http://logo.com/logo1.png", 100, 120);
            channel.AddUrl("http://nowhere.com", "test");
            var programme = xmltv.GetNewProgramme(DateTime.Now, channel.Id, "The test programme", DateTime.Now.AddHours(1),
                "The revenge", null, "English", "Drama", "en", "en", null, "en");
            xmltv.Save(fileName);
            var test = XmlTV.XmlTV.Load(fileName);
            return File.Exists(fileName) ? 
                $"Created file {fileName} with {new FileInfo(fileName).Length}" : 
                $"Failed to write file {fileName}";

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
