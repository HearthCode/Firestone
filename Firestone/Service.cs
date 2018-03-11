using System.Reflection;

namespace Firestone
{
    /// <summary>
    /// Services are the RPC endpoints Hearthstone uses for client-server communication
    /// This class contains base functionality that all services have in common
    /// </summary>
    abstract class Service
    {
        /// <summary>
        /// Retrieves the descriptor corresponding to this service
        /// </summary>
        public ServiceDescriptor Descriptor => GetType().GetCustomAttribute<ServiceDescriptor>();
    }
}
