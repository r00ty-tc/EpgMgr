using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using EpgMgr.Plugins;

namespace EpgMgr
{

    public class CommandManager
    {
        private readonly Core m_core;
        private static readonly string[] primitives = { "cd", "ls", "dir", "exit", "quit", "save", "reload", "run", "?" };
        private List<CommandReference> GlobalCommands;
        private Dictionary<FolderEntry, List<CommandReference>> LocalCommands;
        public FolderEntry RootFolder { get; private set; }

        public CommandManager(Core core)
        {
            m_core = core;
            RefreshPlugins();
        }

        public void RefreshPlugins()
        {
            // Run when plugins added/removed to update console layout
            GlobalCommands = new List<CommandReference>();
            LocalCommands = new Dictionary<FolderEntry, List<CommandReference>>();

            // Register global commands
            RegisterCommand("cd", CommandHandlerCommands.CommandHandlerCD);
            RegisterCommand("ls", CommandHandlerCommands.CommandHandlerLS);
            RegisterCommand("dir", CommandHandlerCommands.CommandHandlerLS);
            RegisterCommand("exit", CommandHandlerCommands.CommandHandlerEXIT);
            RegisterCommand("quit", CommandHandlerCommands.CommandHandlerEXIT);
            RegisterCommand("save", CommandHandlerCommands.CommandHandlerSAVE);
            RegisterCommand("reload", CommandHandlerCommands.CommandHandlerRELOAD);
            RegisterCommand("run", CommandHandlerCommands.CommandHandlerRUN);
            RegisterCommand("?", CommandHandlerCommands.CommandHandlerLISTCMDS);

            // Register folder layout
            RootFolder = new FolderEntry("/", null);
            var configFolder = RootFolder.AddChild("config");
            var coreConfigFolder = configFolder.AddChild("core");
            var pluginConfigFolder = configFolder.AddChild("plugins");

            // Now the plugins
            foreach (var plugin in m_core.GetActivePlugins())
            {
                var thisPluginConfigFolder = pluginConfigFolder.AddChild(plugin.ConsoleName);
                plugin.RegisterConfigData(thisPluginConfigFolder);
            }
        }

        public void RegisterCommand(string commandString, CommandMethodHandler method, Plugin? plugin = null,
            FolderEntry? context = null, int? requiredArgs = null)
        {
            if (context != null)
            {
                // Check for existing registration
                var contextList = new List<CommandReference>();
                if (LocalCommands.TryGetValue(context, out contextList))
                {
                    var existingEntry = contextList.FirstOrDefault(row =>
                        row.CommandString.Equals(commandString, StringComparison.CurrentCultureIgnoreCase));
                    if (existingEntry != null)
                    {
                        if (existingEntry.Plugin != null)
                            throw new Exception(
                                $"Command {existingEntry.CommandString} already registered with plugin {existingEntry.Plugin.Name}");

                        throw new Exception($"Command {existingEntry.CommandString} already registered in context {existingEntry.Context}");
                    }
                }
            }
            else
            {
                if (GlobalCommands.Any(row =>
                        row.CommandString.Equals(commandString, StringComparison.CurrentCultureIgnoreCase)))
                    throw new Exception($"Global command {commandString} already registered");
            }

            var newCommand = new CommandReference(commandString, method, plugin, context, requiredArgs);
            if (newCommand.IsGlobal)
            {
                GlobalCommands.Add(newCommand);
            }
            else
            {
                if (!LocalCommands.TryGetValue(context, out var commandList))
                {
                    commandList = new List<CommandReference>();
                    LocalCommands[context] = commandList;
                }

                commandList.Add(newCommand);
            }
        }

        private static string[] ParseArguments(string args)
        {
            var quoted = false;
            var result = new List<string>();
            var currentString = string.Empty;
            foreach (var argChar in args)
            {
                switch (argChar)
                {
                    case '"':
                        quoted = !quoted;
                        break;
                    case ' ' when currentString.Any() && !quoted:
                        result.Add(currentString);
                        currentString = string.Empty;
                        break;
                    default:
                    {
                        if (quoted || argChar != ' ')
                            currentString += argChar;
                        break;
                    }
                }
            }
            if (!string.IsNullOrWhiteSpace(currentString))
                result.Add(currentString);

            if (quoted)
                throw new ArgumentException($"Unbalanced quotes in argument text {args}");

            return result.ToArray();
        }

        public string HandleCommand(ref FolderEntry context, string command)
        {
            context ??= RootFolder;

            var prompt = $"{context} :: ";
            if (string.IsNullOrWhiteSpace(command))
                return $"{context.FolderPath} :: ";

            // Split command as command line (supporting quotes etc)
            var commandLineSplit = ParseArguments(command);
            var commandString = commandLineSplit.First();
            var args = commandLineSplit.TakeLast(commandLineSplit.Length - 1).ToArray();
            var validCommand = GetValidCommands(context).FirstOrDefault(row => row.CommandString.Equals(commandString, StringComparison.InvariantCultureIgnoreCase));
            if (validCommand == null) return $"Invalid Command {commandString}" + Environment.NewLine + "{context} :: ";
            var result = validCommand.Method(m_core, ref context, commandString, args) + Environment.NewLine;
            result += $"{context.FolderPath} :: ";
            return result;

        }

        protected CommandReference[] GetValidCommands(FolderEntry context)
        {
            var commands = new List<CommandReference>(GlobalCommands);
            if (LocalCommands.TryGetValue(context, out var localCommands))
                commands.AddRange(localCommands);

            return commands.ToArray();
        }

        public bool ValidateChangeFolder(ref FolderEntry context, string newFolder)
        {
            var tempContext = context;
            foreach (var movement in newFolder.Split('/'))
            {
                var validFolder = tempContext.ChildFolders.FirstOrDefault(row => row.FolderName.Equals(movement, StringComparison.InvariantCultureIgnoreCase));
                // Handle .. specifically, it's special
                if (movement.Equals("..") && tempContext.ParentFolder != null)
                    tempContext = tempContext.ParentFolder;
                else if (validFolder != null)
                    tempContext = validFolder;
                else
                    return false;
            }
            context = tempContext;
            return true;
        }
    }
}
