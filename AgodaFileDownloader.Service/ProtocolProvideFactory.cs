using System;
using System.Collections;
using AgodaFileDownloader.Service.ServiceInterface;

namespace AgodaFileDownloader.Service
{
    public static class ProtocolProviderFactory
    {
        private static readonly Hashtable ProtocolHandlers = new Hashtable();
        public static void RegisterProtocolHandler(string prefix, Type protocolProvider)
        {
            ProtocolHandlers[prefix] = protocolProvider;
        }

        public static IProtocolDownloader ResolveProvider(Type providerType)
        {
            IProtocolDownloader provider = (IProtocolDownloader)Activator.CreateInstance(providerType);
            return provider;
        }
        
        public static Type GetProtocolType(string uri)
        {
            var index = uri.IndexOf("://", StringComparison.Ordinal);
            if (index <= 0) return null;
            string prefix = uri.Substring(0, index);
            Type type = ProtocolHandlers[prefix] as Type;
            return type;
        }

        
    }
}
