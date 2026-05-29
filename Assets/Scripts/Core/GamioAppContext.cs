using System;
using System.Collections.Generic;

namespace Gamio.Core
{
    public static class GamioAppContext
    {
        private static readonly Dictionary<Type, object> services = new();

        // Register a service by its concrete type.
        public static void Register<T>(T service) where T : class
        {
            services[typeof(T)] = service;
        }

        // Unregister only if the exact same instance is registered (prevents stale overrides).
        public static void Unregister(object service)
        {
            var type = service.GetType();
            if (services.TryGetValue(type, out var registered) && ReferenceEquals(registered, service))
                services.Remove(type);
        }

        // Retrieve a registered service by type. Logs error and returns null on miss.
        public static T Get<T>() where T : class
        {
            if (services.TryGetValue(typeof(T), out var service))
                return (T)service;
            UnityEngine.Debug.LogError($"[AppContext] {typeof(T).Name} not registered.");
            return null;
        }

        // Clear all registered services (used on scene unload / domain reload).
        public static void Clear()
        {
            services.Clear();
        }
    }
}
