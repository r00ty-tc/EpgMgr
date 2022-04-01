using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpgMgr.Plugins
{
    public partial class DemoPlugin
    {
        public static string CommandHandlerHELLO(Core core, ref FolderEntry context, string command, string[] args)
        {
            return "Hello World";
        }

        public string CommandHandlerLISTCHANNELS(Core core, ref FolderEntry context, string command, string[] args)
        {
            if (args.Length > 1 || (args.Length == 1 && !args.FirstOrDefault().Equals("all")))
                return "Invalid arguments. Use listchannels [all].";

            bool allMode = args.FirstOrDefault() != null &&
                           args.FirstOrDefault().Equals("all", StringComparison.InvariantCultureIgnoreCase);

            var result = string.Empty;
            if (!allMode)
            {
                result = "Subscribed Channels:" + Environment.NewLine;
                foreach (var channel in configRoot.GetList<Channel>("ChannelsSubbed") ?? new List<Channel>())
                {
                    result += $"{channel.Id,-10}{channel.Name,-25}" + Environment.NewLine;
                }
            }
            else
            {
                result = "All Channels:" + Environment.NewLine;
                foreach (var channel in configRoot.GetList<Channel>("ChannelsAvailable") ?? new List<Channel>())
                {
                    result += $"{channel.Id,-10}{channel.Name,-25}" + Environment.NewLine;
                }
            }

            return result;
        }
    }
}
