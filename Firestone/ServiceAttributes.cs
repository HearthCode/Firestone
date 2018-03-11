using System;
using System.Text;

namespace Firestone
{
    /// <summary>
    /// Specifies whether a service is inbound or outbound
    /// </summary>
    internal enum ServiceType
    {
        Export,
        Import
    }

    /// <summary>
    /// Place this attribute on classes to turn them into RPC services
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    internal class ServiceDescriptor : Attribute
    {
        public string Name;
        public int Id;
        public ServiceType Type;

        /// <summary>
        /// Calculate the FNV-1a (32-bit) hash of the service name
        /// </summary>
        /// <returns></returns>
        public int GetHash() {
            uint hash = 0x811c9dc5;
            byte[] bytes = Encoding.ASCII.GetBytes(Name);
            foreach (var b in bytes)
                hash = (hash ^ b) * 0x1000193;
            return (int) hash;
        }
    }

    /// <summary>
    /// Place this attribute on methods within a service class to turn them into RPC method endpoints
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal class MethodDescriptor : Attribute
    {
        public int Id;
    }
}
