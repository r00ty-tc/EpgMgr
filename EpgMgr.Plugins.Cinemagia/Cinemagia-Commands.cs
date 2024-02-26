﻿
namespace EpgMgr.Plugins
{
    public partial class Cinemagia
    {
        public void RegisterCommands(FolderEntry folderEntry)
        {
            // Custom global commands

            // Custom local commands
            m_core.CommandMgr.RegisterCommand("refresh", CommandHandlerREFRESH, 
                $"Reload channels from API{Environment.NewLine}" +
                $"Usage refresh channels",
                this, folderEntry, 1);
            m_core.CommandMgr.RegisterCommand("channel", CommandHandlerCHANNEL, 
                $"Perform channel operations (list, add, remove){Environment.NewLine}" +
                $"Usage: {Environment.NewLine}" +
                $"  channel list [active] [filter]{Environment.NewLine}" +
                $"  channel add <ID>{Environment.NewLine}" +
                $"  channel remove <ID>"
                ,this, folderEntry);
        }

        public string CommandHandlerREFRESH(Core core, ref FolderEntry context, string command, string[] args)
        {
            switch (args[0].ToLower())
            {
                case "channels":
                    var channels = getApiChannels();
                    return $"Loaded {channels.Count()} channels from API";
                default:
                    return "Invalid argument. Try reload channels";
            }
        }

        public string? CommandHandlerCHANNEL(Core core, ref FolderEntry context, string command, string[] args)
        {
            if (args.Length < 1)
                return null;

            switch (args[0])
            {
                case "list":
                    {
                        if (args.Length > 3 || (args.Length >= 2 && !args[1]!.Equals("active")))
                            return null;

                        var subscribedOnly = args.Length >= 2 && args[1].Equals("active", StringComparison.InvariantCultureIgnoreCase);
                        string? filter = null;
                        if (args.Length == (subscribedOnly ? 3 : 2))
                            filter = args[subscribedOnly ? 2 : 1];

                        string result;
                        if (subscribedOnly)
                        {
                            result = "Subscribed Channels:" + Environment.NewLine;
                            var channels = configRoot.GetList<Channel>("ChannelsSubbed") ?? new List<Channel>();
                            if (filter != null)
                                channels = channels.Where(row => row.Name != null &&
                                    row.Name.Contains(filter, StringComparison.InvariantCultureIgnoreCase)).ToList();

                            result = ConsoleControl.SetFG(ConsoleColor.Green) + channels.Aggregate(result, (current, channel) => current + ($"{channel.Name}" + Environment.NewLine)) + ConsoleControl.SetFG(ConsoleColor.White);
                        }
                        else
                        {
                            result = "All Channels:" + Environment.NewLine;
                            var channels = configRoot.GetList<Channel>("ChannelsAvailable") ?? new List<Channel>();
                            if (filter != null)
                                channels = channels.Where(row => row.Name != null &&
                                                                 row.Name.Contains(filter, StringComparison.InvariantCultureIgnoreCase)).ToList();

                            var subbedChannels = configRoot.GetList<Channel>("ChannelsSubbed") ?? new List<Channel>();
                            result = channels.Aggregate(result, (current, channel) => current + ($"{(subbedChannels.Select(row => row.Name).Contains(channel.Name) ? ConsoleControl.SetFG(ConsoleColor.Green) : ConsoleControl.SetFG(ConsoleColor.White))} {channel.Name}" + Environment.NewLine));
                            result += ConsoleControl.SetFG(ConsoleColor.White);
                        }

                        return result;
                    }
                case "add":
                    {
                        if (args.Length < 2)
                            return "Invalid arguments. Try channel add <channel/range>";
                        var rangeArgs = args.TakeLast(args.Length - 1).ToArray();
                        var allChannels = configRoot.GetList<Channel>("ChannelsAvailable");
                        var existingChannels = configRoot.GetList<Channel>("ChannelsSubbed") ?? new List<Channel>();
                        if (allChannels == null || !allChannels.Any())
                            allChannels = getApiChannels().ToList();
                        var channelsToAdd = ProcessRange(rangeArgs, allChannels).Distinct();
                        var addedChans = 0;
                        var existingChans = 0;
                        foreach (var channel in channelsToAdd.Distinct())
                        {
                            if (existingChannels.Select(row => row.Name).Contains(channel.Name))
                                existingChans++;
                            else
                            {
                                existingChannels.Add(channel);
                                addedChans++;
                            }
                        }

                        if (addedChans > 0)
                            configRoot.SetList("ChannelsSubbed", existingChannels);
                        return $"Added {addedChans} channel(s), ignored {existingChans} already present";
                    }
                case "remove":
                    {
                        if (args.Length != 2)
                            return $"{ConsoleControl.ErrorColour}Invalid arguments, try channel remove <channelId>";

                        // Get channel (and lists for subbed/available channels)
                        var channelsSubbed = configRoot.GetList<Channel>("ChannelsSubbed") ?? new List<Channel>();
                        var channel = channelsSubbed.FirstOrDefault(row => row.Name != null &&
                            row.Name.Equals(args[1], StringComparison.InvariantCultureIgnoreCase));

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

        protected IEnumerable<Channel> ProcessRange(string[] rangeArgs, IEnumerable<Channel> channels)
        {
            var newChannels = new List<Channel>();
            foreach (var arg in rangeArgs)
            {
                var channelList = channels.ToArray(); // channels as SkyChannel[] ?? channels.ToArray();
                var thisChannel = channelList.FirstOrDefault(row =>
                    row.Name != null && row.Name.Equals(arg, StringComparison.InvariantCultureIgnoreCase));
                if (thisChannel != null)
                    newChannels.Add(thisChannel);
            }
            return newChannels;
        }

    }
}
