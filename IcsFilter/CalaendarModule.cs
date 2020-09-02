using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Nancy;

namespace IcsFilter
{
    public class CalaendarModule : NancyModule
    {
        private string Filter(string text, string calendarName)
        {
            var lines = text.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            var result = new StringBuilder();

            foreach (var line in lines)
            {
                if (line.Length >= 3)
                {
                    if (line.StartsWith("DTSTART;TZID=", StringComparison.Ordinal) ||
                        line.StartsWith("DTEND;TZID=", StringComparison.Ordinal))
                    {
                        result.AppendLine(line);
                    }
                    else
                    {
                        var index = line.IndexOf(":", StringComparison.Ordinal);

                        if (index >= 1)
                        {
                            var head = line.Substring(0, index);
                            var tail = line.Substring(index + 1);

                            switch (head)
                            {
                                case "BEGIN":
                                case "END":
                                    switch (tail)
                                    {
                                        case "VCALENDAR":
                                        case "VTIMEZONE":
                                        case "DAYLIGHT":
                                        case "STANDARD":
                                        case "VEVENT":
                                            result.AppendLine(line);
                                            break;
                                    }
                                    break;
                                case "VERSION":
                                case "CALSCALE":
                                case "REFRESH-INTERVAL":
                                case "X-PUBLISHED-TTL":
                                case "TZID":
                                case "TZOFFSETFROM":
                                case "TZOFFSETTO":
                                case "TZNAME":
                                case "DTSTART":
                                case "DTEND":
                                case "RRULE":
                                case "UID":
                                case "X-MOZ-LASTACK":
                                case "X-MOZ-GENERATION":
                                    result.AppendLine(line);
                                    break;
                                case "X-WR-CALNAME":
                                    result.AppendLine("X-WR-CALNAME:" + calendarName);
                                    break;
                                case "X-APPLE-CALENDAR-COLOR":
                                    result.AppendLine("X-APPLE-CALENDAR-COLOR:#ffffff");
                                    break;
                                case "SUMMARY":
                                    result.AppendLine("SUMMARY:Busy");
                                    break;
                            }
                        }
                    }   
                }
            }

            return result.ToString().Replace("\n", "\r\n");
        }

        private enum CalendarParseMode
        {
            Default,
            TimeZone,
            Event,
        }

        private string Merge(IEnumerable<string> calendars)
        {
            var defaultLines = new List<string>();
            var timeZones = new List<List<string>>();
            var events = new List<List<string>>();

            foreach (var text in calendars)
            {
                var lines = text.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                var mode = CalendarParseMode.Default;
                var current = new List<string>();

                foreach (var line in lines)
                { 
                    switch (mode)
                    {
                        case CalendarParseMode.Default:
                            switch (line)
                            {
                                case "BEGIN:VCALENDAR":
                                case "END:VCALENDAR":
                                    break;
                                case "BEGIN:VTIMEZONE":
                                    mode = CalendarParseMode.TimeZone;
                                    current.Clear();
                                    current.Add(line);
                                    break;
                                case "BEGIN:VEVENT":
                                    mode = CalendarParseMode.Event;
                                    current.Clear();
                                    current.Add(line);
                                    break;
                                default:
                                    if (!defaultLines.Contains(line))
                                    {
                                        defaultLines.Add(line);
                                    }
                                    break;
                            }
                            break;
                        case CalendarParseMode.TimeZone:
                            switch (line)
                            {
                                case "END:VTIMEZONE":
                                    current.Add(line);
                                    timeZones.Add(current.ToList());
                                    current.Clear();
                                    mode = CalendarParseMode.Default;
                                    break;
                                default:
                                    current.Add(line);
                                    break;
                            }
                            break;
                        case CalendarParseMode.Event:
                            switch (line)
                            {
                                case "END:VEVENT":
                                    current.Add(line);
                                    events.Add(current.ToList());
                                    current.Clear();
                                    mode = CalendarParseMode.Default;
                                    break;
                                default:
                                    current.Add(line);
                                    break;
                            }
                            break;
                    }
                }
            }

            var distinctTimeZones = new List<List<string>>();

            foreach (var timeZone in timeZones)
            {
                var id = timeZone.SingleOrDefault(l => l.StartsWith("TZID:", StringComparison.Ordinal));

                if (!distinctTimeZones.Any(t => t.Contains(id)))
                {
                    distinctTimeZones.Add(timeZone);
                }
            }

            var result = new StringBuilder();

            result.AppendLine("BEGIN:VCALENDAR");

            foreach (var line in defaultLines)
            {
                result.AppendLine(line);
            }

            foreach (var timeZone in distinctTimeZones)
            {
                foreach (var line in timeZone)
                {
                    result.AppendLine(line); 
                }
            }

            foreach (var evt in events)
            {
                foreach (var line in evt)
                {
                    result.AppendLine(line);
                }
            }

            result.AppendLine("END:VCALENDAR");

            return result.ToString().Replace("\n", "\r\n");
        }

        public CalaendarModule()
        {
            Get("/{name}", parameters =>
            {
                string nameString = parameters.name;
                var config = Global.Config.Calendars.FirstOrDefault(c => c.Name.ToLowerInvariant() == nameString);

                if (config == null)
                    throw new FileNotFoundException();

                var client = new WebClient();
                var cals = new List<string>();

                foreach (var privateUrl in config.PrivateUrls)
                {
                    var text = client.DownloadString(privateUrl);
                    var filtered = Filter(text, config.Name);
                    cals.Add(filtered);
                }

                var merged = Merge(cals);

                return merged;
            });
        }
    }
}
