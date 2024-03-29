﻿using System.Data;
using EpgMgr.Plugins;

namespace EpgMgr
{

    /// <summary>
    /// CommandManager: Helper object to help handle console commands
    /// </summary>
    public class CommandManager
    {
        private readonly Core m_core;
        private static readonly string[] primitives = { "cd", "ls", "dir", "exit", "quit", "save", "reload", "run", "?" };
        private List<CommandReference> GlobalCommands = null!;
        private Dictionary<FolderEntry, List<CommandReference>> LocalCommands = null!;
        /// <summary>
        /// Defines the context object that represents the absolute root / folder.
        /// </summary>
        public FolderEntry RootFolder { get; private set; } = null!;

        /// <summary>
        /// Initialize Command Manager. Requires reference to core object
        /// </summary>
        /// <param name="core"></param>
        public CommandManager(Core core)
        {
            m_core = core;
            RefreshPlugins();
        }

        /// <summary>
        /// Refresh local data with latest information. Mainly used to handle loading/unloading of plugins to refresh folder and command structure
        /// </summary>
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
            CoreCommands.RegisterCommands(this, RootFolder);

            // Register variables
            coreXmlTvFolder.AddChildValue("IncludeProgrammeCredits", SetgetXmlTvValue, ValueType.ConfigValueType_Bool);
            coreXmlTvFolder.AddChildValue("IncludeProgrammeCategories", SetgetXmlTvValue, ValueType.ConfigValueType_Bool);
            coreXmlTvFolder.AddChildValue("IncludeProgrammeIcons", SetgetXmlTvValue, ValueType.ConfigValueType_Bool);
            coreXmlTvFolder.AddChildValue("IncludeProgrammeRatings", SetgetXmlTvValue, ValueType.ConfigValueType_Bool);
            coreXmlTvFolder.AddChildValue("IncldeProgrammeStarRatings", SetgetXmlTvValue, ValueType.ConfigValueType_Bool);
            coreXmlTvFolder.AddChildValue("IncludeProgrammeReviews", SetgetXmlTvValue, ValueType.ConfigValueType_Bool);
            coreXmlTvFolder.AddChildValue("IncludeProgrammeImages", SetgetXmlTvValue, ValueType.ConfigValueType_Bool);
            coreXmlTvFolder.AddChildValue("Filename", SetgetXmlTvValue, ValueType.ConfigValueType_String);
            coreXmlTvFolder.AddChildValue("MaxDaysBehind", SetgetXmlTvValue, ValueType.ConfigValueType_Int32);
            coreXmlTvFolder.AddChildValue("MaxDaysAhead", SetgetXmlTvValue, ValueType.ConfigValueType_Int32);

            // Now the plugins
            foreach (var plugin in m_core.GetActivePlugins())
            {
                var thisPluginConfigFolder = pluginConfigFolder.AddChildFolder(plugin.ConsoleName);
                plugin.RegisterConfigData(thisPluginConfigFolder);
            }
        }

        /// <summary>
        /// Registers a command with the associated callback. If context is omitted it will be a global command, if context is provided it will only be valid from that folder in console
        /// </summary>
        /// <param name="commandString"></param>
        /// <param name="method"></param>
        /// <param name="usage"></param>
        /// <param name="plugin"></param>
        /// <param name="context"></param>
        /// <param name="requiredArgs"></param>
        /// <exception cref="Exception"></exception>
        /// <exception cref="DataException"></exception>
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
                if (context == null)
                    throw new DataException("SANITY CHECK. This should never happen! Attempting to add local command without context");

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

        /// <summary>
        /// Handles an incoming command. It will call the apropriate handler and return the result
        /// </summary>
        /// <param name="context"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        public string HandleCommand(ref FolderEntry context, string command)
        {
            context ??= RootFolder;

            if (string.IsNullOrWhiteSpace(command))
                return "";

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
            var result = validCommand.Method(m_core, ref context, commandString, args);
            if (result != null)
                result += Environment.NewLine;

            // If result is null, return usage
            if (result is null)
                return (validCommand.UsageText ?? "Invalid arguments") + Environment.NewLine;

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

        /// <summary>
        /// Attempts to change folder to the specified folder string. On failure the original folder it retained. Otherwise the context reference will point to the new folder.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="newFolder"></param>
        /// <returns></returns>
        public bool ValidateChangeFolder(ref FolderEntry context, string newFolder)
        {
            var tempContext = context;
            var first = true;
            foreach (var movement in newFolder.Split('/'))
            {
                // This is just to handle an initial / meaning absolute path
                if (first && movement.Equals(string.Empty))
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
        private void SetgetXmlTvValue(FolderEntry context, string valuename, ValueType type, ref dynamic? value)
        {
            try
            {
                switch (valuename.ToLower())
                {
                    case "includeprogrammecredits":
                        if (value == null)
                            value = m_core.Config.XmlTvConfig.IncludeProgrammeCredits;
                        else
                            m_core.Config.XmlTvConfig.IncludeProgrammeCredits = (bool)value;
                        return;
                    case "includeprogrammecategories":
                        if (value == null)
                            value = m_core.Config.XmlTvConfig.IncludeProgrammeCategories;
                        else
                            m_core.Config.XmlTvConfig.IncludeProgrammeCategories = (bool)value;
                        return;
                    case "includeprogrammeicons":
                        if (value == null)
                            value = m_core.Config.XmlTvConfig.IncludeProgrammeIcons;
                        else
                            m_core.Config.XmlTvConfig.IncludeProgrammeIcons = (bool)value;
                        return;
                    case "includeprogrammeratings":
                        if (value == null)
                            value = m_core.Config.XmlTvConfig.IncludeProgrammeRatings;
                        else
                            m_core.Config.XmlTvConfig.IncludeProgrammeRatings = (bool)value;
                        return;
                    case "incldeprogrammestarratings":
                        if (value == null)
                            value = m_core.Config.XmlTvConfig.IncldeProgrammeStarRatings;
                        else
                            m_core.Config.XmlTvConfig.IncldeProgrammeStarRatings = (bool)value;
                        return;
                    case "includeprogrammereviews":
                        if (value == null)
                            value = m_core.Config.XmlTvConfig.IncludeProgrammeReviews;
                        else
                            m_core.Config.XmlTvConfig.IncludeProgrammeReviews = (bool)value;
                        return;
                    case "includeprogrammeimages":
                        if (value == null)
                            value = m_core.Config.XmlTvConfig.IncludeProgrammeImages;
                        else
                            m_core.Config.XmlTvConfig.IncludeProgrammeImages = (bool)value;
                        return;
                    case "filename":
                        if (value == null)
                            value = m_core.Config.XmlTvConfig.Filename;
                        else
                        {
                            var fullPath = new FileInfo((string)value);
                            if (!Directory.Exists(fullPath.DirectoryName))
                            {
                                m_core.FeedbackMgr.UpdateStatus(
                                    $"{ConsoleControl.SetFG(ConsoleColor.Red)}Invalid folder {fullPath.DirectoryName}");
                                return;
                            }

                            m_core.Config.XmlTvConfig.Filename = (string)value;
                        }

                        return;
                    case "maxdaysbehind":
                        if (value == null)
                            value = m_core.Config.XmlTvConfig.MaxDaysBehind;
                        else
                        {
                            if ((int)value >= 10)
                            {
                                m_core.FeedbackMgr.UpdateStatus($"{ConsoleControl.SetFG(ConsoleColor.Red)}Invalid value. Max is 10 days");
                                return;
                            }
                            m_core.Config.XmlTvConfig.MaxDaysBehind = (int)value;
                        }
                        return;
                    case "maxdaysahead":
                        if (value == null)
                            value = m_core.Config.XmlTvConfig.MaxDaysAhead;
                        else
                        {
                            if ((int)value >= 30)
                            {
                                m_core.FeedbackMgr.UpdateStatus($"{ConsoleControl.SetFG(ConsoleColor.Red)}Invalid value. Max is 30 days");
                                return;
                            }
                            m_core.Config.XmlTvConfig.MaxDaysAhead = (int)value;
                        }
                        return;
                    default:
                        break;
                }
            }
            catch
            {
                m_core.FeedbackMgr.UpdateStatus("Invalid type used");
            }
        }
    }
}
