using System;
using System.Collections.Generic;
using NLog;
using Makina.Engine.Core.Logging;

namespace Makina.Engine.Core
{
    /// <summary>
    /// Manages the lifetime of IDisposable resources within the engine.
    /// </summary>
    public static class ResourceManager
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private static readonly HashSet<IDisposable> _trackedResources = new HashSet<IDisposable>();
        private static readonly object _lock = new object(); // For thread safety

        /// <summary>
        /// Starts tracking an IDisposable resource.
        /// </summary>
        /// <param name="resource">The resource to track.</param>
        public static void Track(IDisposable resource)
        {
            if (resource == null) return;

            bool added;
            lock (_lock)
            {
                added = _trackedResources.Add(resource);
            }
            if(added)
            {
                _logger.Trace($"Tracking resource: {resource.GetType().Name} (Instance: {resource.GetHashCode()})");
            }
        }

        /// <summary>
        /// Stops tracking and disposes of a specific resource.
        /// </summary>
        /// <param name="resource">The resource to release.</param>
        public static void Release(IDisposable resource)
        {
            if (resource == null) return;

            bool removed;
            lock (_lock)
            {
                removed = _trackedResources.Remove(resource);
            }

            if (removed)
            {
                try
                {
                    resource.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"Error disposing resource {resource.GetType().Name} (Instance: {resource.GetHashCode()})");
                }
            }
            else
            {
                 _logger.Warn($"Attempted to release non-tracked or already released resource: {resource.GetType().Name} (Instance: {resource.GetHashCode()})");
            }
        }

        /// <summary>
        /// Disposes of all currently tracked resources.
        /// Typically called during application shutdown.
        /// </summary>
        public static void ReleaseAll()
        {
            List<IDisposable> resourcesToRelease;
            lock (_lock)
            {
                // Copy to a list to avoid modification during iteration
                resourcesToRelease = new List<IDisposable>(_trackedResources);
                _trackedResources.Clear();
            }

            _logger.Info($"Releasing {resourcesToRelease.Count} tracked resources...");

            foreach (var resource in resourcesToRelease)
            {
                try
                {
                    resource.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"Error disposing resource {resource.GetType().Name} (Instance: {resource.GetHashCode()}) during ReleaseAll");
                }
            }
             _logger.Info("Finished releasing resources.");
        }
    }
} 