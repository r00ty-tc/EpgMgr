using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpgMgr
{
    internal class CommandHandlerCommands
    {
        internal static string CommandHandlerCD(Core core, ref FolderEntry context, string command, string[] args)
        {
            if (args.Length != 1)
                return "Invalid arguments. Use cd <folder>";

            // Handle / specially
            if (args.First().Trim().Equals("/"))
                context = core.CommandMgr.RootFolder;
            else if (!core.CommandMgr.ValidateChangeFolder(ref context, args.First()))
                return $"Invalid folder: {args.First()}";

            return "";
        }
        internal static string CommandHandlerLS(Core core, ref FolderEntry context, string command, string[] args)
        {
            if (args.Length > 1)
                return "Invalid arguments. Use ls [folder]";

            if (args.Length == 1)
            {
                var tempContext = context;
                if (!core.CommandMgr.ValidateChangeFolder(ref context, args.First()))
                    return $"Invalid folder {args.First()}";

                var result = string.Join(Environment.NewLine, context.ChildFolders.Select(row => row.FolderName));
                context = tempContext;
                return result;
            }
            else
            {
                return string.Join(Environment.NewLine, context.ChildFolders.Select(row => row.FolderName));
            }
        }
        internal static string CommandHandlerEXIT(Core core, ref FolderEntry context, string command, string[] args)
        {
            return "**END**";
        }
        internal static string CommandHandlerSAVE(Core core, ref FolderEntry context, string command, string[] args)
        {
            var size = core.SaveConfig();
            return $"Saved config file with {size} bytes";
        }
        internal static string CommandHandlerRELOAD(Core core, ref FolderEntry context, string command, string[] args)
        {
            core.LoadConfig();
            return "Configuration reloaded";
        }
        internal static string CommandHandlerRUN(Core core, ref FolderEntry context, string command, string[] args)
        {
            return "";
        }
        internal static string CommandHandlerLISTCMDS(Core core, ref FolderEntry context, string command, string[] args)
        {
            return "";
        }
    }
}
