using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace Firestone
{
    internal class Firestone
    {
        /// <summary>
        /// Application configuration
        /// </summary>
        public static IConfiguration Configuration { get; set; }

        public static void Main(string[] args) {
            var config = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
                .AddJsonFile("firestone-conf.json");

            Configuration = config.Build();
        }
    }
}
