using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

namespace IcsFilter
{
    public class CalenderConfig : ConfigSection
    {
        public const string NameTag = "Name";
        public const string PrivateUrlTag = "PrivateUrl";

        public string Name { get; private set; }
        public List<string> PrivateUrls { get; private set; }

        public CalenderConfig()
        {
            PrivateUrls = new List<string>();
        }

        public override IEnumerable<ConfigItem> ConfigItems
        {
            get
            {
                yield return new ConfigItemString(NameTag, v => Name = v);
            }
        }

        public override void Load(XElement root)
        {
            base.Load(root);

            foreach (var privateUrl in root.Elements(PrivateUrlTag))
            {
                PrivateUrls.Add(privateUrl.Value); 
            }
        }

        public override IEnumerable<SubConfig> SubConfigs => new SubConfig[0];
    }

    public class IcsFilterConftig : Config
    {
        public const string CalendarTag = "Calendar";

        private readonly List<CalenderConfig> _calendars;

        public IEnumerable<CalenderConfig> Calendars { get { return _calendars; } }

        public IcsFilterConftig()
        {
            _calendars = new List<CalenderConfig>();
        }

        public override IEnumerable<ConfigSection> ConfigSections
        {
            get
            {
                return new ConfigSection[0];
            }
        }

        public string LogFilePrefix { get; private set; }

        public override IEnumerable<ConfigItem> ConfigItems
        {
            get
            {
                yield return new ConfigItemString("LogFilePrefix", v => LogFilePrefix = v);
            }
        }

        public override IEnumerable<SubConfig> SubConfigs
        { 
            get
            {
                return new SubConfig[0]; 
            }
        }

        public override void Load(XElement root)
        {
            base.Load(root);

            _calendars.Clear();
            foreach (var calendarElement in root.Elements(CalendarTag))
            {
                var config = new CalenderConfig();
                config.Load(calendarElement);
                _calendars.Add(config);
            }
        }
    }
}
