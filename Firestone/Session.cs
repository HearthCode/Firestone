using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Threading.Tasks;
using log4net;

namespace Firestone
{
    /// <summary>
    /// This class represents a self-contained session between one client and the server
    /// It is created after TLS authentication but before any application layer data is exchanged
    /// </summary>
    internal class Session
    {
        /// <summary>
        /// Logger
        /// </summary>
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// The underlying network stream for this session
        /// </summary>
        private SslStream stream;

        /// <summary>
        /// Bound RPC service endpoints imported from the client
        /// </summary>
        private Dictionary<int, Service> importedServices = new Dictionary<int, Service>();

        /// <summary>
        /// Bound RPC service endpoints exported from the server
        /// </summary>
        private Dictionary<int, Service> exportedServices = new Dictionary<int, Service>();

        /// <summary>
        /// Create a new session
        /// </summary>
        /// <param name="stream">Authenticated SslStream of the connection</param>
        public Session(SslStream stream) {
            // Store the underlying network connection and IP address
            this.stream = stream;

            // Create a binding to the connection service (bnet.protocol.connection.ConnectionService)
            BindExportedService(0x65446991, 0);
        }

        /// <summary>
        /// Binds an exported service from the server to the session
        /// </summary>
        /// <param name="hash">The FNV-1a hash of the fully-qualified service name</param>
        /// <param name="index">The desired service ID to bind as</param>
        /// <returns>True if the binding succeeded, false if no matching service could be found</returns>
        public bool BindExportedService(int hash, int index) {
            // Don't overwrite existing binding
            if (exportedServices.ContainsKey(index)) {
                Log.Error($"Attempted to bind service with hash {hash} to already-used service ID {index}");
                return false;
            }

            // Look up and instantiate the service for this session
            try {
                var serviceType = Firestone.ExportedServices[hash];
                var service = Activator.CreateInstance(serviceType) as Service;
                service.Session = this;

                exportedServices.Add(index, service);
                Log.Debug($"Bound {service.Descriptor.Name} (Id {service.Descriptor.Id}) to session service Id {index}");
                return true;
            }
            catch (KeyNotFoundException) {
                Log.Error($"Attempted to import service with unknown hash {hash} on ID {index}");
                return false;
            }
        }

        /// <summary>
        /// Incoming RPC message pump
        /// This is the main entry point for the session
        /// </summary>
        public async Task RunMessageLoop() {
            // The Hearthstone protocol works as follows:
            // 1. The first message received is of type bnet.protocol.RpcHeader which specifies the service and method
            // (and therefore the message type) of the next message
            // 2. The next message received is of the message type specified by RpcHeader
            // 3. Go to step 1
            try {
                while (true) {
                    // Get the RPC Header message
                    var rpcHeader = bnet.protocol.Header.Parser.ParseInt16DelimitedFrom(stream);
                    var serviceEndpoint = exportedServices[(int) rpcHeader.ServiceId];
                    var (methodEndpointInfo, messageParser) = serviceEndpoint.Methods[(int) rpcHeader.MethodId];

                    // Get and parse the message specified in the RPC Header
                    var message = messageParser.ParseFrom(stream, (int) rpcHeader.Size);

                    // Dispatch message to handler method and yield while it is being processed
                    await Task.Run(() => methodEndpointInfo.Invoke(serviceEndpoint, new object[] {message}));
                }
            }
            catch (Exception ex) {
                Log.Error($"Session terminated: {ex.Message}");
            }
        }
    }
}
