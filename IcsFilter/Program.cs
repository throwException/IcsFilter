using System;
using System.IO;
using Nancy.Hosting.Self;
using ThrowException.CSharpLibs.LogLib;

namespace IcsFilter
{
    public static class MainClass
    {
        public static void Main(string[] args)
        {
            if ((args.Length < 1) ||
                (!File.Exists(args[0])))
            {
                throw new FileNotFoundException("Config file not found");
            }

            Global.LoadConfig(args[0]);

            var uri = "http://localhost:8888";
            Global.Log.Notice("Starting ICS filter  on " + uri);

            var host = new NancyHost(new Uri(uri));
            host.Start();

            Global.Log.Notice("Application started");

            while (true)
            {
                Global.Cache.Update();
                System.Threading.Thread.Sleep(1000);
            }
        }
    }
}
