using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpgMgr.Plugins
{
    public class ProgramTVProgramme
    {
        public string Channel;
        public DateTime StartTime;
        public DateTime? EndTime;
        public string Id;
        public string? Link;
        public string? Title;
        public string? SubTitle;
        public string? Description;

        public ProgramTVProgramme(string channel, string startTime, DateTime? endTime, string id, string? link, string? title, string? subTitle, string? description)
        {
            Channel = channel;
            startTime = startTime.Replace("GMT", " ");
            StartTime = DateTime.ParseExact(startTime, "yyyy-MM-dd HH:mm:ss", null, DateTimeStyles.AssumeUniversal);
            EndTime = endTime;
            Id = id;
            Link = link;
            Title = title;
            SubTitle = subTitle;
            Description = description;
        }
    }
}
