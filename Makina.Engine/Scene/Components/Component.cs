using System;

namespace Makina.Engine.Scene.Components
{
    public abstract class Component
    {
        public GameObject GameObject { get; internal set; } // Reference to the owning GameObject

        // Optional lifecycle methods (can be added later if needed)
        // public virtual void Initialize() { }
        // public virtual void Update(float deltaTime) { }
        // public virtual void Dispose() { } 
    }
} 