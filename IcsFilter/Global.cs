using ThrowException.CSharpLibs.ConfigParserLib;
using ThrowException.CSharpLibs.LogLib;

namespace IcsFilter
{
    public static class Global
    {
        private static IcsFilterConfig _config;
        private static Logger _logger;
        private static OutputCache _cache;

        public static void LoadConfig(string filename)
        {
            var configParser = new XmlConfig<IcsFilterConfig>();
            _config = configParser.ParseFile(filename);
        }

        public static IcsFilterConfig Config
        {
            get
            {
                if (_config == null)
                {
                    _config = new IcsFilterConfig();
                }

                return _config;
            }
        }

        public static OutputCache Cache
        {
            get
            {
                if (_cache == null)
                {
                    _cache = new OutputCache(Log);

                    foreach (var calendar in Config.Calendars)
                    {
                        _cache.Add(calendar);
                    }
                }

                return _cache;
            }
        }

        public static Logger Log
        {
            get
            {
                if (_logger == null)
                {
                    _logger = new Logger();
                    _logger.ConsoleSeverity = Config.LogConsoleSeverity;
                    _logger.EnableLogFile(Config.LogFileSeverity, Config.LogFilePrefix);
                }

                return _logger;
            }
        }
    }
}