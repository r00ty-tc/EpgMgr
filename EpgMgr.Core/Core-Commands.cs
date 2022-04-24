using EpgMgr.Plugins;

namespace EpgMgr
{
    internal class CoreCommands
    {
        internal static void RegisterCommands(CommandManager mgr, FolderEntry context)
        {
            // Register global commands
            mgr.RegisterCommand("cd", CommandHandlerCD, $"cd: Changes folder in within the console{Environment.NewLine}Usage: cd <folder>", null, null, 1);
            mgr.RegisterCommand("ls", CommandHandlerLS, $"ls: list folders and configuration values in the current or specified context/folder{Environment.NewLine}Usage: ls [folder]");
            mgr.RegisterCommand("dir", CommandHandlerLS, $"dir: list folders and configuration values in the current or specified context/folder{Environment.NewLine}Usage: dir [folder]");
            mgr.RegisterCommand("set", CommandHandlerSET, $"set: sets a specific value in the current or specified context/folder{Environment.NewLine}Usage: set [folder/]<variable> <value>", null, null, 2);
            mgr.RegisterCommand("exit", CommandHandlerEXIT, "exit: Closes the application");
            mgr.RegisterCommand("quit", CommandHandlerEXIT, "quit: Closes the application");
            mgr.RegisterCommand("save", CommandHandlerSAVE, "save: Saves the configuration to the Config.xml file");
            mgr.RegisterCommand("reload", CommandHandlerRELOAD, "reload: Reloads all plugins and configurations");
            mgr.RegisterCommand("run", CommandHandlerRUN, "run: Will run the XMLTV extract from all enabled plugins according to the current configuration");
            mgr.RegisterCommand("alias", CommandHandlerALIAS, $"alias: set/remove/show channel aliases. Note there is no validation here{Environment.NewLine}Usage: alias set <channelname> <aliasname> / remove <channelname> / show <channelname>/ list");
            mgr.RegisterCommand("?", CommandHandlerHELP, $"?: Shows either the list of commands, or if a command is provided as an argument, the usage info for the command{Environment.NewLine}Usage: ? [command]");
            mgr.RegisterCommand("help", CommandHandlerHELP, $"help: Shows either the list of commands, or if a command is provided as an argument, the usage info for the command{Environment.NewLine}Usage: help [command]");

            // Register local commands

            // plugin config
            var coreConfigContext = mgr.RootFolder.FindEntryByPath("config/core");
            mgr.RegisterCommand("plugin", CommandHandlerConfigPLUGIN, "", null, coreConfigContext);

            // xmltv config
            var xmlTvContext = mgr.RootFolder.FindEntryByPath("config/core/xmltv");
        }

        private static string CommandHandlerConfigPLUGIN(Core core, ref FolderEntry context, string command, string[] args)
        {
            if (args.Length < 1)
                return "Invalid Arguments";

            switch (args[0].ToLower())
            {
                case "list":
                    var allMode = args.Length == 2 && args[1].Equals("all", StringComparison.InvariantCultureIgnoreCase);
                    List<Plugin> plugins;
                    if (allMode)
                    {
                        plugins = new List<Plugin>();
                        var pluginConfigs = core.GetAllPlugins();
                        core.FeedbackMgr.UpdateStatus("Reading plugin data", 0, pluginConfigs.Count());
                        var pluginCounter = 0;
                        foreach (var pluginConfig in pluginConfigs)
                        {
                            var plugin = core.PluginMgr.getPlugin(pluginConfig.DllFile, true);
                            if (plugin != null)
                                plugins.Add(plugin);
                            pluginCounter++;
                            core.FeedbackMgr.UpdateStatus("Reading plugin data", pluginCounter);
                        }
                        core.FeedbackMgr.UpdateStatus("");
                    }
                    else
                    {
                        plugins = core.PluginMgr.LoadedPlugins.Select(row => row.PluginObj).ToList();
                    }

                    return "Plugin Name              Version   Author                   Guid" + Environment.NewLine +
                        string.Join(Environment.NewLine, plugins.Select(row => $"{row.ConsoleName,-25}{row.Version,-10}{row.Author,-25}{row.Id}"));
                case "enable":
                    if (args.Length != 2)
                        return $"Invalid Arguments. Usage plugin enable <plugin guid or name>";
                    if (Guid.TryParse(args[1], out var enableGuid))
                        core.PluginMgr.EnablePlugin(enableGuid.ToString());
                    else
                        core.PluginMgr.EnablePlugin(null, args[1]);
                    core.CommandMgr.RefreshPlugins();
                    return $"{ConsoleControl.SetFG(ConsoleColor.DarkCyan)}After enabling a plugin it's advised to save configuration to store the new plugin state and any default plugin configuration.";
                case "disable":
                    if (args.Length != 2)
                        return $"Invalid Arguments. Usage plugin enable <plugin guid or name>";
                    if (Guid.TryParse(args[1], out var disableGuid))
                        core.PluginMgr.DisablePlugin(disableGuid.ToString());
                    else
                        core.PluginMgr.DisablePlugin(null, args[1]);
                    core.CommandMgr.RefreshPlugins();
                    return $"{ConsoleControl.SetFG(ConsoleColor.DarkCyan)}After disabling a plugin it's advised to save configuration to store the new plugin state and remove plugin configuration.";
                default:
                    return $"{ConsoleControl.SetFG(ConsoleColor.Red)}Invalid sub-command. Valid are list, enable, disable";
            }
        }

        internal static string CommandHandlerCD(Core core, ref FolderEntry context, string command, string[] args)
        {
            if (args.Length != 1)
                return "Invalid arguments. Use cd <folder>";

            // Handle / specially for root folder element
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

            var result = string.Empty;
            var tempContext = context;
            if (args.Length == 1 && !core.CommandMgr.ValidateChangeFolder(ref context, args.First()))
                return $"Invalid folder {args.First()}";

            var maxFolder = context.ChildFolders.Any() ? context.ChildFolders.Max(row => row.FolderName.Length) : 0;
            var maxValue = context.ChildValues.Any() ? context.ChildValues.Max(row => row.ValueId.Length) : 0;
            var maxLength = Math.Max(maxFolder, maxValue);
            result = string.Join(Environment.NewLine, context.ChildFolders.Select(row => row.FolderName.PadRight(maxLength + 2) + ConsoleControl.SetFG(ConsoleColor.Magenta) + "<Folder>" + ConsoleControl.SetFG(ConsoleColor.White)));
            if (context.ChildValues.Any()) result += Environment.NewLine;
            result += string.Join(Environment.NewLine,
                context.ChildValues.Select(row => $"{ConsoleControl.SetFG(ConsoleColor.Yellow)}{row.ValueId.PadRight(maxLength + 1)}{ConsoleControl.SetFG(ConsoleColor.White)} = " +
                                                  $"{ConsoleControl.SetFG(ConsoleColor.Green)}{(row.Type.Equals(ValueType.ConfigValueType_String) ? "\"" : "")}" +
                                                  $"{row.Value}{(row.Type.Equals(ValueType.ConfigValueType_String) ? "\"" : "")}{ConsoleControl.SetFG(ConsoleColor.White)}"));

            if (args.Length == 1)
                context = tempContext;

            return result;
        }
        internal static string CommandHandlerSET(Core core, ref FolderEntry context, string command, string[] args)
        {
            // Validate arguments (probably not needed with handler validation)
            if (args.Length != 2)
                return "Invalid Arguments. Use set <variable> <value>";

            // Get variable and path separate from first argument
            var variableSplit = args[0].Split("/");
            var variable = variableSplit.LastOrDefault();
            var varPath = args[0].Replace($"{variable}", "");
            if (varPath.LastOrDefault().Equals('/'))
                varPath = varPath.Remove(varPath.Length - 1, 1);

            // Store current path and try to change to specified path if a path was provided
            var tempContext = context;
            if (!string.IsNullOrWhiteSpace(varPath) && !core.CommandMgr.ValidateChangeFolder(ref context, varPath))
                return $"Invalid folder {varPath}";

            // Try to get value object
            var varObj = context.ChildValues.FirstOrDefault(row =>
                row.ValueId.Equals(variable, StringComparison.InvariantCultureIgnoreCase));

            // Error if not found
            if (varObj == null)
                return $"Invalid variable {variable}";

            var valueChanged = false;
            try
            {
                var origValue = varObj.Value;
                // Set the value we need to parse the string to the correct type
                varObj.Value = ParseValue(args[1], varObj.Type);
                valueChanged = (!origValue?.Equals(varObj.Value));
            }
            catch (FormatException /*ex*/)
            {
                return $"{ConsoleControl.SetFG(ConsoleColor.DarkRed)}Invalid format for variable";
            }

            // Restore context and return result
            context = tempContext;

            // Only show changed value if it changed
            if (valueChanged)
                return
                    $"{ConsoleControl.SetFG(ConsoleColor.Yellow)}{varObj.ValueId}{ConsoleControl.SetFG(ConsoleColor.White)} = {ConsoleControl.SetFG(ConsoleColor.Green)}{varObj.Value}{ConsoleControl.SetFG(ConsoleColor.White)}";
            return "";
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
            var startTime = DateTime.UtcNow.Ticks;
            core.MakeXmlTV();
            return
                $"Generated XMLTV file {core.Config.XmlTvConfig.Filename} in {new TimeSpan(DateTime.UtcNow.Ticks - startTime).TotalMilliseconds}ms";
        }

        internal static string CommandHandlerALIAS(Core core, ref FolderEntry context, string command, string[] args)
        {
            if (args.Length < 1)
                return $"{ConsoleControl.ErrorColour}Invalid Arguments. Check usage with help alias";

            switch (args[0].ToLower())
            {
                case "set":
                {
                    if (args.Length != 3)
                        return $"{ConsoleControl.ErrorColour}Invalid Arguments. Usage: alias set <channelname> <alias>";

                    if (core.Config.ChannelNameToAlias.TryGetValue(args[1], out var alias))
                        return
                            $"{ConsoleControl.ErrorColour}Channel alias for {args[1]} already set to {alias}. You must remove the old alias first";
                    core.AddAlias(args[1], args[2]);
                    return $"{ConsoleControl.SetFG(ConsoleColor.Green)}Added alias {args[2]} for Channel {args[1]}";
                }
                case "remove":
                {
                    if (args.Length != 2)
                        return $"{ConsoleControl.ErrorColour}Invalid Arguments. Usage: alias remove <channelName>";
                    if (!core.Config.ChannelNameToAlias.ContainsKey(args[1]))
                        return
                            $"{ConsoleControl.ErrorColour}Channel alias for {args[1]} doesn't exist. Nothing to remove";
                    core.RemoveAlias(args[1]);
                    return $"{ConsoleControl.SetFG(ConsoleColor.Green)}Removed alias for channel {args[1]}";
                }
                case "show":
                {

                    if (args.Length != 2)
                        return $"{ConsoleControl.ErrorColour}Invalid Arguments. Usage: alias show <channelName>";
                    if (!core.Config.ChannelNameToAlias.TryGetValue(args[1], out var alias))
                        return
                            $"{ConsoleControl.ErrorColour}Channel alias for {args[1]} was not found, nothing to show";
                    return $"Alias for channel {args[1]} is {alias}";
                }
                case "list":
                {
                    if (args.Length != 1)
                        return $"{ConsoleControl.ErrorColour}Invalid Arguments. Usage: alias list";
                    var result = "Channel Name              Alias" + Environment.NewLine;
                    foreach (var alias in core.Config.ChannelNameToAlias)
                        result += $"{alias.Key,-25} {alias.Value}{Environment.NewLine}";

                    return result;
                }
                default:
                    return $"{ConsoleControl.ErrorColour}Invalid sub-command {args[0]}";
            }
        }
        internal static string CommandHandlerHELP(Core core, ref FolderEntry context, string command, string[] args)
        {
            var cmds = core.CommandMgr.GetValidCommands(context);
            if (!cmds.Any())
                return "No commands available.";

            if (args.Length == 0)
            {
                var globalCmds = string.Join(Environment.NewLine,
                    cmds.Where(row => row.Context == null).Select(row => row.CommandString));
                var localCmds = string.Join(Environment.NewLine,
                    cmds.Where(row => row.Context != null).Select(row => row.CommandString));

                // If both lists have entries put a new line after global commands.
                if (globalCmds.Length > 0 && localCmds.Length > 0)
                    globalCmds += Environment.NewLine;

                return
                    $"Available commands:{Environment.NewLine}{ConsoleControl.SetFG(ConsoleColor.Green)}{globalCmds}{ConsoleControl.SetFG(ConsoleColor.DarkCyan)}{localCmds}";
            }

            if (args.Length != 1)
            {
                var helpCmd = cmds.FirstOrDefault(row =>
                    row.CommandString.Equals(command, StringComparison.InvariantCultureIgnoreCase));
                return helpCmd != null && helpCmd.UsageText != null ? helpCmd.UsageText : "Invalid Argument";
            }

            var cmdInfo = cmds.FirstOrDefault(row =>
                row.CommandString.Equals(args[0], StringComparison.InvariantCultureIgnoreCase));
            return cmdInfo != null && cmdInfo.UsageText != null ? cmdInfo.UsageText : "Invalid Argument";
        }

        internal static dynamic? ParseValue(dynamic? value, ValueType type)
        {
            switch (type)
            {
                case ValueType.ConfigValueType_Int32:
                    return int.Parse(value);
                case ValueType.ConfigValueType_Bool:
                    return bool.Parse(value);
                case ValueType.ConfigValueType_Decimal:
                    return decimal.Parse(value);
                case ValueType.ConfigValueType_Double:
                    return double.Parse(value);
                case ValueType.ConfigValueType_Int64:
                    return long.Parse(value);
                case ValueType.ConfigValueType_String:
                    return value?.ToString();
                default:
                    return null;
            }
        }
    }
}
