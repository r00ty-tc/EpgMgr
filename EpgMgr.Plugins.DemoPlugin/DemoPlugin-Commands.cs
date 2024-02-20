
namespace EpgMgr.Plugins
{
    public partial class DemoPlugin
    {
        public void RegisterCommands(FolderEntry folderEntry)
        {
            // Custom global commands

            // Custom local commands
            m_core.CommandMgr.RegisterCommand("channel", CommandHandlerCHANNEL, $"Perform channel operations (list, add, remove){Environment.NewLine}Usage: channel list [all] / channel add <ID> / chanel remove <ID>",this, folderEntry);
        }

        public string? CommandHandlerCHANNEL(Core core, ref FolderEntry context, string command, string[] args)
        {
            if (args.Length < 1)
                return null;

            switch (args[0])
            {
                case "list":
                    {
                        if (args.Length > 2 || (args.Length == 2 && !args[1]!.Equals("active")))
                            return $"{ConsoleControl.ErrorColour}Invalid arguments, try channel list [active].";

                        var subscribedOnly = args.Length == 2 && args[1].Equals("active", StringComparison.InvariantCultureIgnoreCase);

                        string result;
                        if (subscribedOnly)
                        {
                            result = "Subscribed Channels:" + Environment.NewLine;
                            result += ConsoleControl.SetFG(ConsoleColor.Green) + (configRoot.GetList<Channel>("ChannelsSubbed") ??
                                      new List<Channel>()).Aggregate(result, (current, channel) => current + ($"{channel.Id,-10}{channel.Name,-25}" + Environment.NewLine)) + ConsoleControl.SetFG(ConsoleColor.White);
                        }
                        else
                        {
                            var subbedChannels = configRoot.GetList<Channel>("ChannelsSubbed") ?? new List<Channel>();
                            result = "All Channels:" + Environment.NewLine;
                            result += (configRoot.GetList<Channel>("ChannelsAvailable") ??
                                      new List<Channel>()).Aggregate(result, (current, channel) => current + (
                                      $"{(subbedChannels.Select(row => row.Id).Contains(channel.Id) 
                                        ? ConsoleControl.SetFG(ConsoleColor.Green) 
                                        : ConsoleControl.SetFG(ConsoleColor.White))}" + 
                                      $"{channel.Id,-10}{channel.Name,-25}" + Environment.NewLine)) + ConsoleControl.SetFG(ConsoleColor.White);
                        }

                        return result;
                    }
                case "add":
                    {
                        if (args.Length != 2)
                            return $"{ConsoleControl.ErrorColour}Invalid arguments, try channel add <channelId>";

                        // Get channel (and lists for subbed/available channels)
                        var channelsSubbed = configRoot.GetList<Channel>("ChannelsSubbed") ?? new List<Channel>();
                        var channelsAvailable = configRoot.GetList<Channel>("ChannelsAvailable");
                        var channel = channelsAvailable?.FirstOrDefault(row =>
                            row.Id.Equals(args[1], StringComparison.InvariantCultureIgnoreCase));

                        // If not found, error
                        if (channel == null)
                            return $"{ConsoleControl.ErrorColour}Channel {args[1]} not found";

                        // Add the channel and return result to user
                        channelsSubbed.Add(channel);
                        configRoot.SetList("ChannelsSubbed", channelsSubbed);

                        return $"Added {channel.Id} ({channel.Name}) to active channels";
                    }
                case "remove":
                    {
                        if (args.Length != 2)
                            return $"{ConsoleControl.ErrorColour}Invalid arguments, try channel remove <channelId>";

                        // Get channel (and lists for subbed/available channels)
                        var channelsSubbed = configRoot.GetList<Channel>("ChannelsSubbed") ?? new List<Channel>();
                        var channel = channelsSubbed.FirstOrDefault(row =>
                            row.Id.Equals(args[1], StringComparison.InvariantCultureIgnoreCase));

                        if (channel == null)
                            return $"{ConsoleControl.ErrorColour}Channel {args[1]} not found in active channel list";

                        channelsSubbed.Remove(channel);
                        configRoot.SetList("ChannelsSubbed", channelsSubbed);
                        return $"Removed {channel.Id} ({channel.Name}) from active channels";
                    }
                default:
                    return $"{ConsoleControl.ErrorColour}Invalid arguments, try help channel";
            }
        }
    }
}
