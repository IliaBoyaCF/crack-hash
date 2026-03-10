namespace Manager.Abstractions.Services;

public interface ICrackedHashCache
{

    public int Count { get; }
    public int Capacity { get; }

    /// <summary>
    /// Attempts to retrieve precomputed answers for a given hash.
    /// </summary>
    /// <param name="hash">MD5 hash to look up</param>
    /// <param name="answers">Collection of strings that produce this hash when MD5 is applied</param>
    /// <returns>True if hash exists in cache, false otherwise</returns>
    bool TryGetCached(string hash, out IEnumerable<string>? answers);

    /// <summary>
    /// Stores precomputed answers for a hash to avoid recalculating.
    /// </summary>
    /// <param name="hash">MD5 hash to store</param>
    /// <param name="answers">Strings that hash to this value via MD5</param>
    bool TryAdd(string hash, IEnumerable<string> answers);
}
