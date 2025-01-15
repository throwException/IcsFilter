using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThrowException.CSharpLibs.LogLib;

namespace IcsFilter
{
    public class OutputCacheEntry
    { 
        public CalenderConfig Config { get; private set; }
        public DateTime Updated { get; set; }
        public string Text { get; set; }

        public OutputCacheEntry(CalenderConfig config)
        {
            Config = config;
        }
    }

    public class OutputCache
    {
        private readonly ILogger _logger;
        private readonly InputCache _inputCache;
        private readonly Dictionary<string, OutputCacheEntry> _entries;

        public OutputCache(ILogger logger)
        {
            _logger = logger;
            _entries = new Dictionary<string, OutputCacheEntry>();
            _inputCache = new InputCache(_logger);
        }

        public void Add(CalenderConfig config)
        {
            lock (_entries)
            {
                if (!_entries.ContainsKey(config.Name))
                {
                    _entries.Add(config.Name, new OutputCacheEntry(config));

                    foreach (var privateUrl in config.PrivateUrls)
                    {
                        _inputCache.Add(privateUrl);
                    }
                }
            }
        }

        private OutputCacheEntry GetEntry(string name)
        {
            lock (_entries)
            {
                return _entries[name];
            }
        }

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


        private void UpdateEntry(OutputCacheEntry entry)
        {
            var cals = new List<string>();

            foreach (var privateUrl in entry.Config.PrivateUrls)
            {
                var text = _inputCache.Get(privateUrl);
                var filtered = Filter(text, entry.Config.Name);
                cals.Add(filtered);
            }

            entry.Text = Merge(cals);
            entry.Updated = DateTime.UtcNow;
            _logger.Info("Calendar {0} updated", entry.Config.Name);
        }

        public void Update()
        {
            _inputCache.Update();

            IEnumerable<OutputCacheEntry> entries = null;

            lock (_entries)
            {
                entries = _entries.Values.ToList();
            }

            foreach (var entry in entries)
            { 
                lock (entry)
                { 
                    if (RequiresUpdate(entry))
                    {
                        UpdateEntry(entry);
                    }
                }
            }
        }

        private bool RequiresUpdate(OutputCacheEntry entry)
        {
            foreach (var privateUrl in entry.Config.PrivateUrls)
            {
                if (_inputCache.LastUpdated(privateUrl) > entry.Updated)
                {
                    _logger.Info("Calendar {0} requires updated base on {1}", entry.Config.Name, privateUrl);
                    return true;
                }
            }
            return false;
        }

        public string Get(string name)
        {
            var entry = GetEntry(name);

            lock (entry)
            {
                if (entry.Text == null)
                {
                    UpdateEntry(entry);
                }
                return entry.Text;
            }
        }
    }
}
