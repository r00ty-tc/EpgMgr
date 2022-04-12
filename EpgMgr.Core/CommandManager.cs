using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            // Register folder layout
            RootFolder = new FolderEntry("/", null);
            var configFolder = RootFolder.AddChildFolder("config");
            var coreConfigFolder = configFolder.AddChildFolder("core");
            var coreXmlTvFolder = coreConfigFolder.AddChildFolder("xmltv");
            var pluginConfigFolder = configFolder.AddChildFolder("plugins");

            // Register commands
            CommandHandlerCommands.RegisterCommands(this, RootFolder);

            // Now the plugins
            foreach (var plugin in m_core.GetActivePlugins())
            {
                var thisPluginConfigFolder = pluginConfigFolder.AddChildFolder(plugin.ConsoleName);
                plugin.RegisterConfigData(thisPluginConfigFolder);
            }
        }

        public void RegisterCommand(string commandString, CommandMethodHandler method, string? usage = null,
            Plugin? plugin = null, FolderEntry? context = null, int? requiredArgs = null)
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

            var newCommand = new CommandReference(commandString, method, usage, plugin, context, requiredArgs);
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

            if (string.IsNullOrWhiteSpace(command))
                return $"{context.FolderPath} :: ";

            // Split command as command line (supporting quotes etc)
            var commandLineSplit = ParseArguments(command);
            var commandString = commandLineSplit.First();
            var args = commandLineSplit.TakeLast(commandLineSplit.Length - 1).ToArray();

            // Get command and check validity
            var validCommand = GetValidCommands(context).FirstOrDefault(row => row.CommandString.Equals(commandString, StringComparison.InvariantCultureIgnoreCase));
            if (validCommand == null) return $"Invalid Command {commandString}" + Environment.NewLine;

            // Validate required arguments
            if (validCommand.RequiredArgs != null && args.Length < validCommand.RequiredArgs)
                return $"Invalid Arguments{Environment.NewLine}{validCommand.UsageText ?? string.Empty}{Environment.NewLine}";

            // Run command method and return result
            var result = validCommand.Method(m_core, ref context, commandString, args) + Environment.NewLine;

            // Check context is still valid (mainly for when manipulating plugins). Should be a better way
            var tempContext = RootFolder;
            context = !ValidateChangeFolder(ref tempContext, context.FolderPath) ? RootFolder : tempContext;
            return result;
        }

        internal CommandReference[] GetValidCommands(FolderEntry context)
        {
            var commands = new List<CommandReference>(GlobalCommands);
            if (LocalCommands.TryGetValue(context, out var localCommands))
                commands.AddRange(localCommands);

            return commands.ToArray();
        }

        public bool ValidateChangeFolder(ref FolderEntry context, string newFolder)
        {
            var tempContext = context;
            var first = true;
            foreach (var movement in newFolder.Split('/'))
            {
                // This is just to handle an initial / meaning absolute path
                if (first && movement.Equals(String.Empty))
                {
                    GetRootConfigFolder(ref tempContext);
                    first = false;
                    continue;
                }
                first = false;

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

        internal static void GetRootConfigFolder(ref FolderEntry context)
        {
            while (context.ParentFolder != null)
                context = context.ParentFolder;
        }
    }
}
