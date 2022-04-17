﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace EpgMgr.Plugins
{
    public partial class SkyUK
    {
        public void RegisterCommands(FolderEntry folderEntry)
        {
            // Custom global commands

            // Custom local commands
            m_core.CommandMgr.RegisterCommand("skytest", CommandHandlerSKYTEST, $"List sky channels using API", this,
                folderEntry);
            m_core.CommandMgr.RegisterCommand("reloadchannels", CommandHandlerRELOADCHANNELS, $"Reload channels from API",
                this, folderEntry);
            m_core.CommandMgr.RegisterCommand("channel", CommandHandlerCHANNEL, $"channel: Channel operations. add: add channel(s), remove: remove channel(s), " +
                $"list: list channels{Environment.NewLine}Usage: channel <add> <channel/range> / <remove> <channel/range> / <list> [all]", this, folderEntry);
        }


        public string CommandHandlerSKYTEST(Core core, ref FolderEntry context, string command, string[] args)
        {
            List<SkyChannel> toProcess = new List<SkyChannel>();
            var existingChannels = configRoot.GetList<SkyChannel>("ChannelsSubbed") ?? new List<SkyChannel>();
            var programmeList = new List<SkyEpgList>();
            foreach (var channel in existingChannels)
            {
                toProcess.Add(channel);
                if (toProcess.Count == 20)
                {
                    programmeList.Add(GetProgrammes(toProcess.ToArray()));
                    toProcess.Clear();
                }
            }
            if (toProcess.Count > 0)
                programmeList.Add(GetProgrammes(toProcess.ToArray()));
            return "";
        }

        public string CommandHandlerRELOADCHANNELS(Core core, ref FolderEntry context, string command, string[] args)
        {
            var channels = GetApiChannels();
            return $"Refreshed {channels.Count()} channels from API";
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
                    string searchString = string.Empty;
                    if (args.Length >= 2 && args[1].Equals("all", StringComparison.InvariantCultureIgnoreCase))
                    {
                        channels = configRoot.GetList<SkyChannel>("ChannelsAvailable");
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
                                row.ChannelName.Contains(searchString, StringComparison.InvariantCultureIgnoreCase))
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
                    int addedChans = 0;
                    int existingChans = 0;
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
                    int removedChans = 0;
                    int missingChans = 0;
                    foreach (var channel in channelsToRemove)
                    {
                        if (existingChannels.Select(row => row.Sid).Contains(channel.Sid))
                        {
                            existingChannels.Remove(channel);
                            removedChans++;
                        }
                    }

                    if (removedChans > 0)
                        configRoot.SetList("ChannelsSubbed", existingChannels);
                    return $"Removed {removedChans} channel(s)";
                }
                default:
                    return "Invalid sub-command";
            }
            return "";
        }
    }
}