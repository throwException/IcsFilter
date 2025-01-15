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
        public CalaendarModule()
        {
            Get("/{name}", parameters =>
            {
                string nameString = parameters.name;
                var config = Global.Config.Calendars.FirstOrDefault(c => c.Name.ToLowerInvariant() == nameString);

                if (config == null)
                    throw new FileNotFoundException();

                return Global.Cache.Get(config.Name);
            });
        }
    }
}
