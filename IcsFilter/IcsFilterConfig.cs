using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using ThrowException.CSharpLibs.ConfigParserLib;
using ThrowException.CSharpLibs.LogLib;

namespace IcsFilter
{
    public class CalenderConfig : IConfig
    {
        [Setting]
        public string Name { get; private set; }

        [Setting(Name = "PrivateUrl")]
        public List<string> PrivateUrls { get; private set; }
    }

    public class IcsFilterConfig : IConfig
    {
        [Setting(Name = "Calendar")]
        public IEnumerable<CalenderConfig> Calendars { get; private set; }

        [Setting]
        public string LogFilePrefix { get; private set; }

        [Setting(Required = false, DefaultValue = LogSeverity.Info)]
        public LogSeverity LogFileSeverity { get; private set; }

        [Setting(Required = false, DefaultValue = LogSeverity.Info)]
        public LogSeverity LogConsoleSeverity { get; private set; }
    }
}
