using bnet.protocol.connection;

namespace Firestone.Services
{
    /// <summary>
    /// This service is called by the client when the connection is first established (after TLS authentication)
    /// to create service endpoint bindings between the client and server
    /// </summary>
    [ServiceDescriptor(Name="bnet.protocol.connection.ConnectionService", Id=0, Type=ServiceType.Export)]
    internal class ConnectionService : Service
    {
        /// <summary>
        /// ConnectRequest is always the first method called after a connection has been established.
        /// It exports a set of service endpoints to the server and sends a list of hashes of services it wishes to import
        /// </summary>
        [MethodDescriptor(Id=1)]
        public void ConnectRequest(ConnectRequest req) {
        }
    }
}
