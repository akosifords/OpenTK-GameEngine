using Makina.Engine.Rendering;
using System;

namespace Makina.Engine.Scene;

/// <summary>
/// Basic container for entities in the scene.
/// Holds a Transform and, for now, direct references to rendering components.
/// </summary>
public class GameObject
{
    public Transform Transform { get; private set; }
    public string Name { get; set; }

    // --- TEMPORARY: Direct references to rendering components ---
    // TODO: Replace these with a Component system (e.g., MeshRendererComponent)
    public Mesh? Mesh { get; set; }
    public Texture? Texture { get; set; }
    public Shader? Shader { get; set; } // Maybe associated via Material later
    // --- END TEMPORARY ---

    // TODO: Add List<Component> Components later

    public GameObject(string name = "GameObject")
    {
        Name = name;
        Transform = new Transform();
        // TODO: Initialize component list
    }

    // TODO: Add methods like AddComponent, GetComponent, etc.
} 