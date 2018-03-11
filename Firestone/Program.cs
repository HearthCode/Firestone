using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
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
                var c = connectionTask(client);
            }
        }

        /// <summary>
        /// Set up a new inbound connection
        /// </summary>
        private async Task connectionTask(TcpClient client) {
            var clientIp = ((IPEndPoint)client.Client.RemoteEndPoint).Address;

            Log.Info("Accepted inbound connection from " + clientIp);

            var sslStream = new SslStream(client.GetStream(), false);

            // TLS server authentication
            try {
                // Don't require client authentication; don't check for certificate revocation; accept any TLS version
                await sslStream.AuthenticateAsServerAsync(certificate, false, SslProtocols.Tls, false);

                Log.Info("TLS authentication succeeded for " + clientIp);

                // Configure timeouts
                sslStream.ReadTimeout = timeoutSeconds * 1000;
                sslStream.WriteTimeout = timeoutSeconds * 1000;

                // Start connection message pump
                await connectionHandler(sslStream);
            }

            // Something is probably wrong with our SSL certificate or key
            catch (AuthenticationException ex) {
                Log.Error("TLS authentication failed for " + clientIp + ": " + ex.Message);
                if (ex.InnerException != null)
                    Log.Error("TLS authentication failure details: " + ex.InnerException.Message);
            }

            // The client closed the connection. Most likely it rejected our certificate
            catch (IOException) {
                Log.Error("TLS authentication failed for " + clientIp +
                          " - server certificate validation must be disabled in the client in order to connect");
            }

            // Clean up
            finally {
                Log.Info("Closing connection from " + clientIp);
                sslStream.Close();
                client.Close();
            }
        }

        /// <summary>
        /// Handle the connection for an individual client asynchronously (relinquishing control to the wait pump)
        /// </summary>
        private async Task connectionHandler(SslStream stream) {
            // Incoming RPC message pump
            // The Hearthstone protocol works as follows:
            // 1. The first message received is of type bnet.protocol.RpcHeader which specifies the service and method
            // (and therefore the message type) of the next message
            // 2. The next message received is of the message type specified by RpcHeader
            // 3. Go to step 1
            try {
                var rpcHeader = bnet.protocol.Header.Parser.ParseInt16DelimitedFrom(stream);
                var connect = bnet.protocol.connection.ConnectRequest.Parser.ParseFrom(stream, (int) rpcHeader.Size);
            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
