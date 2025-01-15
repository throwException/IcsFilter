using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using ThrowException.CSharpLibs.LogLib;

namespace IcsFilter
{
    public class InputCacheEntry
    {
        public string Url { get; private set; }
        public string Text { get; set; }
        public DateTime Updated { get; set; }

        public InputCacheEntry(string url)
        {
            Url = url;
            Text = Text;
            Updated = DateTime.MinValue;
        }
    }

    public class InputCache
    {
        private readonly ILogger _logger;
        private readonly Dictionary<string, InputCacheEntry> _entries;

        public InputCache(ILogger logger)
        {
            _logger = logger;
            _entries = new Dictionary<string, InputCacheEntry>();
        }

        public void Add(string url)
        {
            lock (_entries)
            {
                if (_entries.ContainsKey(url))
                {
                    _entries.Add(url, new InputCacheEntry(url));
                }
            }
        }

        public void Update()
        {
            IEnumerable<InputCacheEntry> entries = null;

            lock (_entries)
            {
                entries = _entries.Values.ToList();
            }

            foreach (var entry in entries)
            {
                lock (entry)
                {
                    if ((entry.Text == null) || (DateTime.UtcNow.Subtract(entry.Updated).TotalMinutes > 3d))
                    {
                        UpdateEntry(entry);
                    }
                }
            }
        }

        private void UpdateEntry(InputCacheEntry entry)
        {
            var client = new WebClient();
            entry.Text = client.DownloadString(entry.Url);
            entry.Updated = DateTime.UtcNow;
            _logger.Info("Data from {0} updated", entry.Url);
        }

        private InputCacheEntry GetEntry(string url)
        {
            lock (_entries)
            {
                if (!_entries.ContainsKey(url))
                {
                    _entries.Add(url, new InputCacheEntry(url));
                }

                return _entries[url];
            }
        }

        public DateTime LastUpdated(string url)
        {
            var entry = GetEntry(url);

            lock (entry)
            {
                return entry.Updated;
            }
        }

        public string Get(string url)
        {
            var entry = GetEntry(url);

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
