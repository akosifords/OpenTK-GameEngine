using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using Makina.Engine.Core.Logging;
using System.Numerics;
using System.Threading;
using BepuUtilities;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Makina.Engine.Physics
{
    /// <summary>
    /// Manages the BEPUphysics simulation and associated resources.
    /// </summary>
    public class PhysicsWorld : IDisposable
    {
        public Simulation Simulation { get; private set; }
        public BufferPool BufferPool { get; private set; }
        public ThreadDispatcher ThreadDispatcher { get; private set; }

        // Mappings from physics handles back to RigidbodyComponents
        private readonly Dictionary<BodyHandle, RigidbodyComponent> _bodyHandleToComponent = new();
        private readonly Dictionary<StaticHandle, RigidbodyComponent> _staticHandleToComponent = new();

        // TODO: Expose NarrowPhase and BroadPhase callbacks if needed

        public PhysicsWorld(int targetThreadCount = 0) // 0 = auto
        {
            Log.Info("Initializing Physics World...");

            BufferPool = new BufferPool();

            // Determine thread count
            int threadCount = targetThreadCount > 0 
                ? targetThreadCount 
                : Math.Max(1, Environment.ProcessorCount > 4 ? Environment.ProcessorCount - 2 : Environment.ProcessorCount - 1);
            ThreadDispatcher = new ThreadDispatcher(threadCount);
            Log.Info($"Using {threadCount} threads for physics.");

            // TODO: Add NarrowPhase.Callbacks and PoseIntegrator.Callbacks implementations if needed
            // For now, using defaults.
            var narrowPhaseCallbacks = new NarrowPhaseCallbacks();
            var poseIntegratorCallbacks = new PoseIntegratorCallbacks(new Vector3(0, -9.81f, 0));
            Simulation = Simulation.Create(BufferPool, narrowPhaseCallbacks, poseIntegratorCallbacks, new SolveDescription(8, 1));

            Log.Info("Physics World initialized.");
        }

        // --- Component Management --- 

        internal void RegisterComponent(RigidbodyComponent component)
        {
            if (component.BodyHandle.Value != default && !_bodyHandleToComponent.ContainsKey(component.BodyHandle))
            {
                _bodyHandleToComponent.Add(component.BodyHandle, component);
            }
            else if (component.StaticHandle.Value != default && !_staticHandleToComponent.ContainsKey(component.StaticHandle))
            {
                _staticHandleToComponent.Add(component.StaticHandle, component);
            }
            else
            {
                // This might happen if called before RigidbodyComponent.InitializeBody sets the handle,
                // or if called on a component that failed initialization.
                 if (component.IsInitialized) 
                    Log.Warn($"Attempted to register RigidbodyComponent for GameObject '{component.GameObject?.Name ?? "(No GameObject)"}' but it has an invalid or already registered handle.");
            }
        }

        internal void UnregisterComponent(RigidbodyComponent component)
        {
            if (component.BodyHandle.Value != default)
            {
                _bodyHandleToComponent.Remove(component.BodyHandle);
            }
            if (component.StaticHandle.Value != default)
            {
                _staticHandleToComponent.Remove(component.StaticHandle);
            }
        }

        public bool TryGetComponent(BodyHandle handle, [NotNullWhen(true)] out RigidbodyComponent? component)
        {
            return _bodyHandleToComponent.TryGetValue(handle, out component);
        }

        public bool TryGetComponent(StaticHandle handle, [NotNullWhen(true)] out RigidbodyComponent? component)
        {
            return _staticHandleToComponent.TryGetValue(handle, out component);
        }

        // --- Simulation Update --- (Moved from below Dispose)

        /// <summary>
        /// Advances the physics simulation by the specified time step.
        /// </summary>
        /// <param name="deltaTime">The time elapsed since the last update.</param>
        public void Update(float deltaTime)
        {
            // TODO: Consider fixed time step logic if needed
            Simulation.Timestep(deltaTime, ThreadDispatcher);
        }

        // --- Dispose --- 

        public void Dispose()
        {
            Log.Info("Disposing Physics World...");
            // Clear mappings before disposing simulation to avoid issues if callbacks access them?
            _bodyHandleToComponent.Clear();
            _staticHandleToComponent.Clear();

            Simulation.Dispose();
            ThreadDispatcher.Dispose();
            BufferPool.Clear(); // BufferPool itself doesn't implement IDisposable
            Log.Info("Physics World disposed.");
            GC.SuppressFinalize(this);
        }

        // --- Placeholder Callback Implementations ---
        // These can be customized later for specific collision filtering, material properties, etc.

        private struct NarrowPhaseCallbacks : INarrowPhaseCallbacks
        {
            public void Initialize(Simulation simulation) { }

            public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b, ref float speculativeMargin) => true;
            public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB) => true;
            public bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties materialProperties) where TManifold : unmanaged, IContactManifold<TManifold>
            {
                materialProperties = new PairMaterialProperties { FrictionCoefficient = 1f, MaximumRecoveryVelocity = 2f, SpringSettings = new SpringSettings(30, 1) };
                return true;
            }
            public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold) => true;

            public void Dispose() { }
        }

        private struct PoseIntegratorCallbacks : IPoseIntegratorCallbacks
        {
            public Vector3 Gravity;
            private Vector3Wide gravityDt;

            public AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving;
            public bool AllowSubstepsForUnconstrainedBodies => false;
            public bool IntegrateVelocityForKinematics => false;

            public void Initialize(Simulation simulation) { }

            public PoseIntegratorCallbacks(Vector3 gravity) : this()
            {
                Gravity = gravity;
            }

            public void PrepareForIntegration(float dt)
            {
                // Ensure gravityDt is initialized correctly for the wide types.
                Vector3Wide.Broadcast(Gravity * dt, out gravityDt);
            }

            public void IntegrateVelocity(Vector<int> bodyIndices, Vector3Wide position, QuaternionWide orientation, BodyInertiaWide localInertia, Vector<int> integrationMask, int workerIndex, Vector<float> dt, ref BodyVelocityWide velocity)
            {
                // Apply gravity influence using wide types.
                velocity.Linear += gravityDt;
            }
        }
    }
} 