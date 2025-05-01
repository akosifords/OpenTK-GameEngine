using Makina.Engine.Rendering;
using System;
using Makina.Engine.Scene.Components;
using System.Collections.Generic;
using System.Linq;
using Makina.Engine.Core.Logging;

namespace Makina.Engine.Scene;

/// <summary>
/// Basic container for entities in the scene.
/// Manages a collection of Components that define its behavior and data.
/// </summary>
public class GameObject
{
    public Transform Transform { get; private set; }
    public string Name { get; set; }
    public bool IsActive { get; set; } = true;

    private readonly Dictionary<Type, Component> _components = new Dictionary<Type, Component>();

    public GameObject(string name = "GameObject")
    {
        Name = name;
        Transform = new Transform();
        AddComponentInternal(Transform);
    }

    /// <summary>
    /// Adds a component to the GameObject.
    /// Only one component of each type is allowed.
    /// </summary>
    public T AddComponent<T>(T component) where T : Component
    {
        Type type = typeof(T);
        if (_components.ContainsKey(type))
        {
            Log.Warn($"GameObject '{Name}' already has a component of type '{type.Name}'. Replacing is not allowed.");
            return (T)_components[type];
        }
        return AddComponentInternal(component);
    }
    
    /// <summary>
    /// Internal method to add component without type checking, used for Transform.
    /// </summary>
    private T AddComponentInternal<T>(T component) where T: Component
    {
        component.GameObject = this;
        _components.Add(typeof(T), component);
        Log.Trace($"Added component '{typeof(T).Name}' to GameObject '{Name}'");
        return component;
    }

    /// <summary>
    /// Gets the component of the specified type.
    /// </summary>
    /// <returns>The component instance, or null if not found.</returns>
    public T? GetComponent<T>() where T : Component
    {
        if (_components.TryGetValue(typeof(T), out Component? component))
        {
            return (T)component;
        }
        return null;
    }
    
    /// <summary>
    /// Tries to get the component of the specified type.
    /// </summary>
    /// <returns>True if the component was found, false otherwise.</returns>
    public bool TryGetComponent<T>(out T? component) where T : Component
    {
        if (_components.TryGetValue(typeof(T), out Component? comp))
        {
            component = (T)comp;
            return true;
        }
        component = null;
        return false;
    }

    /// <summary>
    /// Removes a component of the specified type.
    /// </summary>
    /// <returns>True if the component was found and removed, false otherwise.</returns>
    public bool RemoveComponent<T>() where T : Component
    {
        Type type = typeof(T);
        if (type == typeof(Transform)) 
        { 
             Log.Warn("Cannot remove the mandatory Transform component.");
             return false; 
        }

        if (_components.TryGetValue(type, out Component? component))
        {
            _components.Remove(type);
            Log.Trace($"Removed component '{type.Name}' from GameObject '{Name}'");
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Gets all components attached to this GameObject.
    /// </summary>
    public IEnumerable<Component> GetAllComponents()
    {
        return _components.Values;
    }
} 