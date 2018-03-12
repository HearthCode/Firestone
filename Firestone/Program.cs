using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using log4net;

[assembly: log4net.Config.XmlConfigurator(ConfigFile="log4net.config.xml", Watch=true)]

namespace Firestone
{
    /// <summary>
    /// The main server application
    /// </summary>
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
        /// Service endpoints provided by the server (keyed by hash)
        /// </summary>
        public static Dictionary<int, Type> ExportedServices;

        /// <summary>
        /// Service endpoints provided by the client (keyed by hash)
        /// </summary>
        public static Dictionary<int, Type> ImportedServices;

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

            // Find all the available RPC services
            var serviceTypes =
            (from t in Assembly.GetExecutingAssembly().GetTypes()
                where t.IsDefined(typeof(ServiceDescriptor), false)
                select new {
                    Service = t,
                    Descriptor = t.GetCustomAttribute<ServiceDescriptor>()
                }
            ).ToDictionary(x => x.Descriptor, x => x.Service);

            ExportedServices = (from s in serviceTypes
                where s.Key.Type == ServiceType.Export
                select new {
                    Key = s.Key.GetHash(),
                    s.Value
                }).ToDictionary(x => x.Key, x => x.Value);

            ImportedServices = (from s in serviceTypes
                where s.Key.Type == ServiceType.Import
                select new {
                    Key = s.Key.GetHash(),
                    s.Value
                }).ToDictionary(x => x.Key, x => x.Value);

            // Start the TCP server
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
        /// The TCP socket for inbound connections
        /// </summary>
        private TcpListener tcpListener;

        /// <summary>
        /// The SSL certificate of the server
        /// </summary>
        private X509Certificate certificate;

        /// <summary>
        /// The number of seconds of inactivity before a client connection is closed
        /// </summary>
        private int timeoutSeconds;

        /// <summary>
        /// Server entry point
        /// </summary>
        public async Task Run() {
            // Fetch server configuration parameters
            if (!IPAddress.TryParse(Firestone.Configuration["lobby_server_ip"], out IPAddress ipAddress)) {
                Log.Fatal("Invalid lobby server IP address specified in configuration file");
            }
            if (!int.TryParse(Firestone.Configuration["lobby_server_port"], out int port)) {
                Log.Fatal("Invalid lobby server port specified in configuration file");
                return;
            }

            var certFile = Firestone.Configuration["lobby_server_certificate_file"];
            try {
                certificate = new X509Certificate2(certFile, "firestone");
            }
            catch (Exception ex) {
                Log.Fatal("Could not load lobby server SSL certificate from " + certFile + ": " + ex.Message);
                return;
            }
            Log.Info("Successfully loaded lobby server SSL certificate from " + certFile);

            if (!int.TryParse(Firestone.Configuration["client_connection_timeout"], out timeoutSeconds)) {
                Log.Fatal("Invalid client connection timeout specified in configuration file");
            }

            // Initialize and open the TCP socket for incoming connections
            tcpListener = new TcpListener(ipAddress, port);
            tcpListener.Start();

            Log.Info($"Lobby server listening for connections on {ipAddress}:{port}");

            // Inbound connection wait loop
            while (true) {
                Log.Debug("Ready to accept next connection");
                var client = await tcpListener.AcceptTcpClientAsync();

                // Dispatch connection to a new task
                var c = ConnectionTask(client);
            }
        }

        /// <summary>
        /// Set up a new inbound connection
        /// </summary>
        private async Task ConnectionTask(TcpClient client) {
            // Set logging context as client IP address
            log4net.LogicalThreadContext.Properties["Session"] = ((IPEndPoint)client.Client.RemoteEndPoint).Address;

            Log.Info("Accepted inbound connection");

            var sslStream = new SslStream(client.GetStream(), false);

            // TLS server authentication
            try {
                // Don't require client authentication; don't check for certificate revocation; accept any TLS version
                await sslStream.AuthenticateAsServerAsync(certificate, false, SslProtocols.Tls, false);

                Log.Info("TLS authentication succeeded");

                // Configure timeouts
                sslStream.ReadTimeout = timeoutSeconds * 1000;
                sslStream.WriteTimeout = timeoutSeconds * 1000;

                // Create session and start the connection's message pump
                await new Session(sslStream).RunMessageLoop();
            }

            // Something is probably wrong with our SSL certificate or key
            catch (AuthenticationException ex) {
                Log.Error("TLS authentication failed for: " + ex.Message);
                if (ex.InnerException != null)
                    Log.Error("TLS authentication failure details: " + ex.InnerException.Message);
            }

            // The client closed the connection. Most likely it rejected our certificate
            catch (IOException) {
                Log.Error("TLS authentication failed - server certificate validation must be disabled in the client in order to connect");
            }

            // Clean up
            finally {
                Log.Info("Closing connection");
                sslStream.Close();
                client.Close();
            }
        }
    }
}
