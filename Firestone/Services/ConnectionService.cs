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
        [MethodDescriptor(Id=1)]
        public void ConnectRequest(ConnectRequest req) {
        }
    }
}
