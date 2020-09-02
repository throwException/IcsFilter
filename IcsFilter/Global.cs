using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace IcsFilter
{
    public static class Global
    {
        private static IcsFilterConftig _config;
        private static Logger _logger;

        public static IcsFilterConftig Config
        {
            get
            {
                if (_config == null)
                {
                    _config = new IcsFilterConftig();
                }

                return _config;
            }
        }

        public static Logger Log
        {
            get
            {
                if (_logger == null)
                {
                    _logger = new Logger(Config.LogFilePrefix);
                }

                return _logger;
            }
        }
    }
}