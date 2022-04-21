using System.Data;

namespace EpgMgr.Plugins
{
    public partial class SkyUK
    {
        public void RegisterCommands(FolderEntry folderEntry)
        {
            // Custom global commands

            // Custom local commands
            m_core.CommandMgr.RegisterCommand("refresh", CommandHandlerREFRESH, $"Reload channels or static data from API{Environment.NewLine}Usage refresh channels / refresh data",
                this, folderEntry, 1);
            m_core.CommandMgr.RegisterCommand("channel", CommandHandlerCHANNEL, $"channel: Channel operations. add/remove/adjust alias for channel(s){Environment.NewLine}" +
                $"Usage: channel add <channel/range> / remove <channel/range> / list [all] / alias set <channelNo> <newName> / alias remove <channelNo> / alias list", this, folderEntry);
            m_core.CommandMgr.RegisterCommand("region", CommandHandlerREGION, $"region: Region operations. list/show/set region for API operations{Environment.NewLine}Usage: region list / show / set <regionid>", this, folderEntry);
        }

        public string CommandHandlerREFRESH(Core core, ref FolderEntry context, string command, string[] args)
        {
            switch (args[0].ToLower())
            {
                case "channels":
                    var channels = GetApiChannels();
                    return $"Refreshed {channels.Count()} channels from API";
                case "data":
                    LoadBlobData();
                    return "Reloaded static data";
                default:
                    return "Invalid argument. Try reload channels or reload data";
            }
        }

        public string CommandHandlerREGION(Core core, ref FolderEntry context, string command, string[] args)
        {
            if (args.Length < 1)
                return $"Invalid arguments. Need at least one argument. Use help region for details";

            var regions = configRoot.GetList<SkyRegion>("SkyRegions");
            switch (args[0].ToLower())
            {
                case "list":
                {
                    if (args.Length != 1)
                        return $"Invalid arguments. Usage: region list";

                    var result = "ID         Region" + Environment.NewLine;
                    if (regions == null || !regions.Any())
                        return $"{ConsoleControl.ErrorColour}No regions found!";

                    foreach (var region in regions)
                        result += $"{region.RegionId,-10} {region.RegionName}{Environment.NewLine}";

                    return result;
                }
                case "show":
                {
                    if (args.Length != 1)
                        return $"Invalid arguments. Usage: region show";

                    var region = configRoot.GetValue<string>("SkyRegion");
                    if (string.IsNullOrWhiteSpace(region))
                    {
                        configRoot.SetValue<string>("SkyRegion", DEFAULT_REGION);
                        region = DEFAULT_REGION;
                    }

                    var regionData = regions?.FirstOrDefault(row => row.RegionId.Equals(region));
                    if (regionData == null)
                        throw new DataException($"Region {region} not found in region data!");

                    return $"Current region is {regionData.RegionId}: {regionData.RegionName}";
                }
                case "set":
                {
                    if (args.Length != 2)
                        return $"Invalid arguments. Usage: region set <regionid>";

                    var region = configRoot.GetValue<string>("SkyRegion");
                    var regionData = regions?.FirstOrDefault(row => row.RegionId.Equals(args[1], StringComparison.InvariantCultureIgnoreCase));
                    if (regionData == null)
                        return $"{ConsoleControl.ErrorColour}Invalid region {args[1]}";

                    if (regionData.RegionId.Equals(region))
                        return "No change to region. Nothing to do";

                    configRoot.SetValue("SkyRegion", regionData.RegionId);
                    GetApiChannels();
                    return
                        $"{ConsoleControl.SetFG(ConsoleColor.Green)}Region set to {regionData.RegionId} ({regionData.RegionName})";
                }
            }
            return "";
        }

        public string CommandHandlerCHANNEL(Core core, ref FolderEntry context, string command, string[] args)
        {
            if (args.Length < 1)
                return "Invalid Arguments, needs at least one argument";

            // Which sub command?
            switch (args[0].ToLower())
            {
                case "list":
                    List<SkyChannel> channels;
                    var searchString = string.Empty;
                    if (args.Length >= 2 && args[1].Equals("all", StringComparison.InvariantCultureIgnoreCase))
                    {
                        channels = configRoot.GetList<SkyChannel>("ChannelsAvailable") ?? new List<SkyChannel>();
                        if (channels == null || !channels.Any())
                            channels = GetApiChannels().ToList();
                        if (args.Length >= 3)
                            searchString = args[2];
                    }
                    else
                    {
                        channels = configRoot.GetList<SkyChannel>("ChannelsSubbed") ?? new List<SkyChannel>();
                        if (args.Length >= 2)
                            searchString = args[1];
                    }

                    if (!string.IsNullOrWhiteSpace(searchString))
                        channels = channels.Where(row =>
                                row.ChannelName != null && row.ChannelName.Contains(searchString, StringComparison.InvariantCultureIgnoreCase))
                            .ToList();
                    var result = "Number Name                      Type" + Environment.NewLine;
                    foreach (var channel in channels)
                        result += $"{channel.ChannelNo,-6} {channel.ChannelName,-25} {channel.Sf}{Environment.NewLine}";
                    return result;
                case "add":
                {
                    if (args.Length < 2)
                        return "Invalid arguments. Try channel add <channel/range>";
                    var rangeArgs = args.TakeLast(args.Length - 1).ToArray();
                    var allChannels = configRoot.GetList<SkyChannel>("ChannelsAvailable");
                    var existingChannels = configRoot.GetList<SkyChannel>("ChannelsSubbed") ?? new List<SkyChannel>();
                    if (allChannels == null || !allChannels.Any())
                        allChannels = GetApiChannels().ToList();
                    var channelsToAdd = ProcessRange(rangeArgs, allChannels).Distinct();
                    var addedChans = 0;
                    var existingChans = 0;
                    foreach (var channel in channelsToAdd.Distinct())
                    {
                        if (existingChannels.Select(row => row.Sid).Contains(channel.Sid))
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
                    if (args.Length < 2)
                        return "Invalid arguments. Try channel remove <channel/range>";
                    var rangeArgs = args.TakeLast(args.Length - 1).ToArray();
                    var existingChannels = configRoot.GetList<SkyChannel>("ChannelsSubbed") ?? new List<SkyChannel>();
                    var channelsToRemove = ProcessRange(rangeArgs, existingChannels).Distinct();
                    var removedChans = 0;
                    foreach (var channel in channelsToRemove)
                    {
                        if (!existingChannels.Select(row => row.Sid).Contains(channel.Sid)) continue;
                        existingChannels.Remove(channel);
                        removedChans++;
                    }

                    if (removedChans > 0)
                        configRoot.SetList("ChannelsSubbed", existingChannels);
                    return $"Removed {removedChans} channel(s)";
                }
                case "alias":
                {
                    if (args.Length < 2)
                        return
                            "Invalid Arguments. Try channel alias set <channelNo> <newName> / remove <channelNo> / list";

                    switch (args[1].ToLower())
                    {
                        case "set":
                        {
                            if (args.Length != 4)
                                return
                                    $"{ConsoleControl.ErrorColour}Invalid arguments. Try channel alias set <channelNo> <Alias>";
                            channels = configRoot.GetList<SkyChannel>("ChannelsSubbed") ?? new List<SkyChannel>();
                            var channel = channels.FirstOrDefault(row => row.ChannelNo != null && row.ChannelNo.Equals(args[2]));
                            if (channel == null)
                                return
                                    $"{ConsoleControl.ErrorColour}Channel {args[2]} not found. Check it is a valid channel number";
                            var alias = m_core.GetAliasFromChannelName(channel.ChannelName, true);
                            if (alias != null)
                                return
                                    $"{ConsoleControl.ErrorColour}Alias already set for {args[2]}. {channel.ChannelName} -> {alias}. This needs to be removed first";
                            if (channel.ChannelName != null)
                                m_core.AddAlias(channel.ChannelName, args[3]);

                            return
                                $"{ConsoleControl.SetFG(ConsoleColor.Green)}Added alias for channel {args[2]}. {channel.ChannelName} -> {args[3]}";
                        }
                        case "remove":
                        {
                            if (args.Length != 3)
                                return
                                    $"{ConsoleControl.ErrorColour}Invalid arguments. Try channel alias remove <channelNo>";
                            channels = configRoot.GetList<SkyChannel>("ChannelsSubbed") ?? new List<SkyChannel>();
                            var channel = channels.FirstOrDefault(row => row.ChannelNo != null && row.ChannelNo.Equals(args[2]));
                            if (channel == null)
                                return
                                    $"{ConsoleControl.ErrorColour}Channel {args[2]} not found. Check it is a valid channel number";
                            var alias = m_core.GetAliasFromChannelName(channel.ChannelName, true);
                            if (alias == null)
                                return
                                    $"{ConsoleControl.ErrorColour}No alias found for {args[2]}. ({channel.ChannelName}). Nothing to remove";
                            if (channel.ChannelName != null)
                                m_core.RemoveAlias(channel.ChannelName);

                            return
                                $"{ConsoleControl.SetFG(ConsoleColor.Green)}Removed alias for channel {args[2]}. {channel.ChannelName} -> {alias}";
                        }
                        case "list":
                        {
                            if (args.Length != 2)
                                return
                                    $"{ConsoleControl.ErrorColour}Invalid arguments. Try channel alias list";
                            channels = configRoot.GetList<SkyChannel>("ChannelsSubbed") ?? new List<SkyChannel>();
                            var aliasresult = "Number Name                      Alias" + Environment.NewLine;
                            foreach (var channel in channels)
                            {
                                var alias = m_core.GetAliasFromChannelName(channel.ChannelName, true);
                                if (alias != null)
                                    aliasresult += $"{channel.ChannelNo,6} {channel.ChannelName,-25} {alias}{Environment.NewLine}";
                            }

                            return aliasresult;
                        }
                        default:
                            return $"{ConsoleControl.ErrorColour}Invalid sub-command channel alias {args[1]}";
                    }
                }
                default:
                    return $"Invalid sub-command channel {args[0]}";
            }
        }
    }
}