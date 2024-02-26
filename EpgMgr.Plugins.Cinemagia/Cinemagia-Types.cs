using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpgMgr.Plugins
{
    public class CinemagiaProgramme
    {
        public string Channel;
        public DateTime StartTime;
        public DateTime? EndTime;
        public string? Title;
        public string? Description;

        public CinemagiaProgramme(string channel, string startTime, DateTime? endTime, string? title, string? description)
        {
            Channel = channel;
            startTime = startTime.Replace("GMT", " ");
            StartTime = DateTime.ParseExact(startTime, "yyyy-MM-dd HH:mm:ss", null, DateTimeStyles.AssumeUniversal);
            EndTime = endTime;
            Title = title;
            Description = description;
        }
    }
}
