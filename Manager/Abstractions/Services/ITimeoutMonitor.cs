using Manager.Abstractions.Model;

namespace Manager.Abstractions.Services;

/// <summary>
/// Monitors objects that can timeout and raises their timeout events when due.
/// </summary>
/// <typeparam name="TKey">The type of key used to identify tracked objects.</typeparam>
public interface ITimeoutMonitor<TKey> where TKey : notnull
{
    /// <summary>
    /// Begins or updates tracking for an object.
    /// </summary>
    /// <param name="key">Unique identifier for the object.</param>
    /// <param name="timeoutable">The object to track.</param>
    /// <param name="resetStartedAt">If true, resets the object's timeout start time to now.</param>
    /// <returns>True if the object was added or updated; false if the key is invalid or object null.</returns>
    bool TryAdd(TKey key, ITimeoutable timeoutable, bool resetStartedAt = false);

    /// <summary>
    /// Stops tracking an object.
    /// </summary>
    /// <param name="key">The key of the object to stop tracking.</param>
    /// <returns>True if the object was being tracked; otherwise false.</returns>
    bool TryRemove(TKey key);

    /// <summary>
    /// Gets all currently tracked keys.
    /// </summary>
    IEnumerable<TKey> GetTrackedKeys();

}