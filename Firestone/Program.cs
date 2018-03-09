using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using log4net;

[assembly: log4net.Config.XmlConfigurator(ConfigFile="log4net.config.xml", Watch=true)]

namespace Firestone
{
    internal class Firestone
    {
        /// <summary>
        /// Application configuration
        /// </summary>
        public static IConfiguration Configuration { get; set; }

        // Logger
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static void Main(string[] args) {
            Log.Info("Server starting up");

            var config = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
                .AddJsonFile("firestone-conf.json");

            Configuration = config.Build();

            Log.Info("Configuration loaded successfully");

            Log.Info("Server shutting down");
        }
    }
}
