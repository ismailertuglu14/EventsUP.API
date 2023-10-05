using Topluluk.Shared.Dtos;

namespace DBHelper.Repository;

public interface IRedisRepository
{
    /// <summary>
    /// Gets a value indicating whether the Redis connection is currently established and operational.
    /// </summary>
    /// <remarks>
    /// This property provides information about the current status of the Redis connection. If it returns true, it means the connection is established and operational.
    /// If it returns false, it indicates that the connection is not currently operational or has not been established.
    /// </remarks>
    bool IsConnected { get; }

    /// <summary>
    /// Asynchronously retrieves the value associated with the specified key from the cache.
    /// </summary>
    /// <param name="key">The key associated with the desired value in the cache.</param>
    /// <returns>A Task representing the asynchronous operation that returns the value associated with the key as a string.</returns>
    public Task<string> GetValueAsync(string key);

    /// <summary>
    /// Asynchronously retrieves multiple values from the cache based on the provided list of keys.
    /// </summary>
    /// <param name="keys">A list of keys for which values should be retrieved from the cache.</param>
    /// <returns>A Task representing the asynchronous operation that returns a dictionary containing key-value pairs for the requested keys and their associated values.</returns>
    public Task<Dictionary<string, string>> GetAllAsync(List<string> keys);

    /// <summary>
    /// Serializes the 'value' parameter of type T that is received from an external source.
    /// </summary>
    /// <param name="key">The key associated with the value.</param>
    /// <param name="value">The value to be serialized.</param>
    /// <typeparam name="T">The type of the value to be serialized.</typeparam>
    /// <returns>A task that represents the asynchronous operation. The task result is a boolean indicating whether the serialization was successful.</returns>
    Task<bool> SetValueAsync<T>(string key, T value);
  
    /// <summary>
    /// Asynchronously retrieves an object from the cache using the specified key. If the object is not found in the cache, it returns null.
    /// </summary>
    /// <typeparam name="T">The type of the object to be retrieved from the cache.</typeparam>
    /// <param name="key">The key under which the object is stored in the cache.</param>
    /// <returns>A Task representing the asynchronous operation that returns the cached object of type T if found; otherwise, it returns null.</returns>
    Task<T> GetOrNullAsync<T>(string key) where T : class;
   
    Task<List<string>> GetListValueAsync(List<string> keys);

    /// <summary>
    /// Asynchronously retrieves an object from the cache using the specified key. If the object is not found in the cache, it is added using the provided asynchronous function and then returned.
    /// </summary>
    /// <typeparam name="T">The type of the object to be retrieved or added to the cache.</typeparam>
    /// <param name="key">The key under which the object is stored in the cache.</param>
    /// <param name="action">An asynchronous function that generates or retrieves the object to be added to the cache if it's not found.</param>
    /// <returns>A Task representing the asynchronous operation that returns the cached object of type T.</returns>
    Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> action) where T : class;

    /// <summary>
    /// Retrieves an object from the cache using the specified key. If the object is not found in the cache, it is added using the provided function and then returned.
    /// </summary>
    /// <typeparam name="T">The type of the object to be retrieved or added to the cache.</typeparam>
    /// <param name="key">The key under which the object is stored in the cache.</param>
    /// <param name="action">A function that generates or retrieves the object to be added to the cache if it's not found.</param>
    /// <returns>The cached object of type T.</returns>
    T GetOrAdd<T>(string key, Func<T> action) where T : class;

    /// <summary>
    /// Asynchronously removes the value associated with the specified key from the cache.
    /// </summary>
    /// <param name="key">The key associated with the value to be removed from the cache.</param>
    /// <returns>A Task representing the asynchronous operation that removes the value associated with the key from the cache.</returns>
    public Task Clear(string key);

    /// <summary>
    /// Removes all values from the cache, effectively clearing the entire cache.
    /// </summary>
    public void ClearAll();


}