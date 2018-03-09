using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
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

        /// <summary>
        /// Logger
        /// </summary>
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Application entry point
        /// </summary>
        public static void Main(string[] args) {
            Log.Info("Server starting up");

            // Load configuration from file
            var config = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
                .AddJsonFile("firestone-conf.json");

            try {
                Configuration = config.Build();
            }
            catch (Exception ex) {
                Log.Fatal("Could not load configuration file: " + ex.Message);
                Environment.Exit(1);
            }

            Log.Info("Configuration loaded successfully");

            // Start the server
            new ConnectionManager().Run().Wait();

            Log.Info("Server shutting down");
        }
    }

    /// <summary>
    /// This class listens for connections from lobby clients and dispatches them
    /// </summary>
    internal class ConnectionManager
    {
        /// <summary>
        /// Logger
        /// </summary>
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Server entry point
        /// </summary>
        public async Task Run() {
            // Initialize and open the TCP socket for incoming connections
            if (!IPAddress.TryParse(Firestone.Configuration["lobby_server_ip"], out IPAddress ipAddress))
            {
                Log.Fatal("Invalid lobby server IP address specified in configuration file");
            }
            if (!int.TryParse(Firestone.Configuration["lobby_server_port"], out int port))
            {
                Log.Fatal("Invalid lobby server port specified in configuration file");
                return;
            }
            var listener = new TcpListener(ipAddress, port);
            listener.Start();

            Log.Info($"Lobby server listening for connections on {ipAddress}:{port}");

            // Dispatch new connections
            while (true) {
                var client = await listener.AcceptTcpClientAsync();

                Log.Info("Accepted inbound connection from " + ((IPEndPoint) client.Client.RemoteEndPoint).Address);
                // do something with client
                client.Close();
            }
        }
    }
}
