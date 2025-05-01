using System;
using System.Collections.Concurrent;
using NLog;

namespace Makina.Engine.Core
{
    /// <summary>
    /// A generic, thread-safe object pool.
    /// </summary>
    /// <typeparam name="T">The type of objects to pool. Must be a class with a parameterless constructor.</typeparam>
    public class ObjectPool<T> where T : class, new()
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly ConcurrentBag<T> _pool = new ConcurrentBag<T>();
        private readonly Func<T>? _factory; // Optional custom factory

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectPool{T}"/> class
        /// using the default parameterless constructor for creating new objects.
        /// </summary>
        /// <param name="initialSize">The number of objects to pre-allocate.</param>
        public ObjectPool(int initialSize = 0)
        {
            InitializePool(initialSize);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectPool{T}"/> class
        /// using a custom factory method for creating new objects.
        /// </summary>
        /// <param name="factory">The function used to create new objects when the pool is empty.</param>
        /// <param name="initialSize">The number of objects to pre-allocate using the factory.</param>
        public ObjectPool(Func<T> factory, int initialSize = 0)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            InitializePool(initialSize);
        }

        private void InitializePool(int initialSize)
        {
             if (initialSize < 0) initialSize = 0;
            _logger.Debug($"Initializing ObjectPool<{typeof(T).Name}> with size {initialSize}");
            for (int i = 0; i < initialSize; i++)
            {
                _pool.Add(CreateInstance());
            }
        }

        private T CreateInstance()
        {
            return _factory?.Invoke() ?? new T();
        }

        /// <summary>
        /// Retrieves an object from the pool or creates a new one if the pool is empty.
        /// </summary>
        /// <returns>An instance of <typeparamref name="T"/>.</returns>
        public T Get()
        {
            if (_pool.TryTake(out T? item))
            {
                _logger.Trace($"Retrieved {typeof(T).Name} from pool (Instance: {item.GetHashCode()}). Pool count: {_pool.Count}");
                return item;
            }
            else
            {
                _logger.Trace($"Pool empty for {typeof(T).Name}. Creating new instance.");
                T newItem = CreateInstance();
                _logger.Trace($"Created new {typeof(T).Name} (Instance: {newItem.GetHashCode()}).");
                return newItem;
            }
        }

        /// <summary>
        /// Returns an object to the pool.
        /// </summary>
        /// <param name="item">The object to return.</param>
        public void Return(T item)
        {
             if (item == null) 
             {
                 _logger.Warn($"Attempted to return null item to ObjectPool<{typeof(T).Name}>.");
                 return;
             }
             // Optional: Add checks here to prevent returning items already in the pool
             // or objects not originally from the pool (if necessary).
             
             // Optional: Reset object state here if needed before returning to pool.
             // if (item is IPoolable poolable) { poolable.Reset(); }

            _pool.Add(item);
            _logger.Trace($"Returned {typeof(T).Name} to pool (Instance: {item.GetHashCode()}). Pool count: {_pool.Count}");
        }

        /// <summary>
        /// Clears the pool, optionally disposing of pooled items if they implement IDisposable.
        /// </summary>
        /// <param name="disposeItems">Whether to call Dispose() on pooled items that implement IDisposable.</param>
        public void Clear(bool disposeItems = false)
        {
            _logger.Info($"Clearing ObjectPool<{typeof(T).Name}>. Dispose items: {disposeItems}");
            int disposedCount = 0;
            while (_pool.TryTake(out T? item))
            {
                if (disposeItems && item is IDisposable disposable)
                {
                    try 
                    {
                        disposable.Dispose();
                        disposedCount++;
                    } 
                    catch (Exception ex)
                    {
                        _logger.Error(ex, $"Error disposing item {item.GetType().Name} (Instance: {item.GetHashCode()}) during pool clear.");
                    }
                }
                // Otherwise, the item is just removed from the bag and will be garbage collected.
            }
            _logger.Info($"ObjectPool<{typeof(T).Name}> cleared. {disposedCount} items disposed.");
        }
    }

    /* Optional: Define an interface for poolable objects
    public interface IPoolable
    {
        void Reset(); // Method to reset the object's state when returned to the pool
    }
    */
} 