using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Service Locator pattern for efficient object finding
/// Reduces FindFirstObjectByType calls and improves performance
/// </summary>
public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> services = new Dictionary<Type, object>();

    /// <summary>
    /// Register a service instance
    /// </summary>
    public static void Register<T>(T service) where T : UnityEngine.Object
    {
        services[typeof(T)] = service;
    }

    /// <summary>
    /// Register a service instance (alternative method name)
    /// </summary>
    public static void RegisterService<T>(T service) where T : UnityEngine.Object
    {
        Register(service);
    }

    /// <summary>
    /// Get a service instance
    /// </summary>
    public static T Get<T>() where T : UnityEngine.Object
    {
        if (services.TryGetValue(typeof(T), out object service))
        {
            return service as T;
        }
        return null;
    }

    /// <summary>
    /// Get a service instance (alternative method name)
    /// </summary>
    public static T GetService<T>() where T : UnityEngine.Object
    {
        return Get<T>();
    }

    /// <summary>
    /// Check if a service is registered (alternative method name)
    /// </summary>
    public static bool HasService<T>() where T : UnityEngine.Object
    {
        return Has<T>();
    }

    /// <summary>
    /// Initialize all services (placeholder for future implementation)
    /// </summary>
    public static void InitializeServices()
    {
        // This method can be expanded to initialize services in a specific order
        PerformanceUtils.Log("🔧 ServiceLocator initialized");
    }

    /// <summary>
    /// Check if all required services are registered
    /// </summary>
    public static bool AreAllServicesRegistered()
    {
        // Define required service types here
        Type[] requiredServices = new Type[]
        {
            typeof(InputManager),
            typeof(UnifiedSaveLoad),
            typeof(UnifiedUI),
            typeof(UnifiedAudio),
            typeof(UnifiedCamera),
            typeof(UnifiedParticles),
            typeof(UnifiedQuest),
            typeof(UnifiedInventory),
            typeof(UnifiedDialogue),
            typeof(UnifiedSceneManager)
        };

        foreach (Type serviceType in requiredServices)
        {
            if (!services.ContainsKey(serviceType))
            {
                PerformanceUtils.LogWarning(PerformanceUtils.FormatString("⚠️ Required service not registered: {0}", serviceType.Name));
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Check if a service is registered
    /// </summary>
    public static bool Has<T>() where T : UnityEngine.Object
    {
        return services.ContainsKey(typeof(T));
    }

    /// <summary>
    /// Unregister a service
    /// </summary>
    public static void Unregister<T>() where T : UnityEngine.Object
    {
        services.Remove(typeof(T));
    }

    /// <summary>
    /// Clear all services (useful for scene changes)
    /// </summary>
    public static void Clear()
    {
        services.Clear();
    }
}
