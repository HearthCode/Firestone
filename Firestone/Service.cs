using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Google.Protobuf;

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

        private Dictionary<int, (MethodInfo, MessageParser)> methods;

        /// <summary>
        /// Retrieve all the method IDs and corresponding server methods and protobuf message types for this service
        /// This is done once per service per session, then cached
        /// </summary>
        public Dictionary<int, (MethodInfo, MessageParser)> Methods {
            get {
                if (methods != null)
                    return methods;

                // Find all the available methods for this service
                methods =
                (from m in GetType().GetMethods()
                    where m.IsDefined(typeof(MethodDescriptor), false)
                    select new {
                        m.GetCustomAttribute<MethodDescriptor>().Id,
                        Method = m,
                        MessageParser = m.GetParameters()[0].ParameterType.GetProperty("Parser").GetMethod.Invoke(null, null) as MessageParser
                    }
                ).ToDictionary(x => x.Id, x => (x.Method, x.MessageParser));
                return methods;
            }
        }

        /// <summary>
        /// The connected session this service instance is bound to
        /// </summary>
        public Session Session { get; set; }
    }
}
