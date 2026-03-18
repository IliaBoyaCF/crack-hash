namespace Manager.Abstractions.Model;

/// <summary>
/// Represents an entity that can expire after a specified timeout interval.
/// When timeout is enabled, the entity tracks the elapsed time since it was started
/// and raises the <see cref="Timeout"/> event when the interval is exceeded.
/// </summary>
/// <remarks>
/// The timeout mechanism works as follows:
/// <list type="bullet">
/// <item><description>Enable timeout tracking by calling <see cref="ResetTimeout"/>, which sets <see cref="StartedAt"/> to the current time.</description></item>
/// <item><description>While <see cref="IsTimeoutEnabled"/> is true, the entity is monitored for timeout expiration.</description></item>
/// <item><description>When the time elapsed since <see cref="StartedAt"/> exceeds <see cref="TimeoutInterval"/>, <see cref="OnTimeout"/> should be called.</description></item>
/// <item><description><see cref="OnTimeout"/> raises the <see cref="Timeout"/> event and automatically disables further timeout tracking.</description></item>
/// <item><description>Timeout tracking can be manually disabled at any time by calling <see cref="IgnoreTimeout"/>.</description></item>
/// </list>
/// </remarks>
public interface ITimeoutable
{
    /// <summary>
    /// Occurs when the timeout interval has been exceeded while timeout tracking is enabled.
    /// </summary>
    event EventHandler? Timeout;

    /// <summary>
    /// Gets a value indicating whether timeout tracking is currently active.
    /// </summary>
    /// <value><c>true</c> if the entity is being monitored for timeout; otherwise, <c>false</c>.</value>
    bool IsTimeoutEnabled { get; }

    /// <summary>
    /// Gets or sets the time interval after which the entity should timeout.
    /// </summary>
    /// <value>The timeout duration.</value>
    TimeSpan TimeoutInterval { get; set; }

    /// <summary>
    /// Gets the timestamp when timeout tracking was last started or reset.
    /// </summary>
    /// <value>The UTC date and time when <see cref="ResetTimeout"/> was last called.</value>
    /// <remarks>This value is only meaningful when <see cref="IsTimeoutEnabled"/> is <c>true</c>.</remarks>
    DateTime StartedAt { get; }

    /// <summary>
    /// Starts or restarts timeout tracking.
    /// </summary>
    /// <remarks>
    /// This method sets <see cref="IsTimeoutEnabled"/> to <c>true</c> and updates 
    /// <see cref="StartedAt"/> to the current UTC time. Any previously started timeout
    /// tracking is reset, starting a new timeout period from this moment.
    /// </remarks>
    void ResetTimeout();

    /// <summary>
    /// Stops timeout tracking without triggering the timeout.
    /// </summary>
    /// <remarks>
    /// After calling this method, <see cref="IsTimeoutEnabled"/> becomes <c>false</c>,
    /// and the entity will no longer be monitored for timeout expiration until
    /// <see cref="ResetTimeout"/> is called again.
    /// </remarks>
    void IgnoreTimeout();

    /// <summary>
    /// Triggers the timeout process manually or in response to detecting that the
    /// timeout interval has been exceeded.
    /// </summary>
    /// <remarks>
    /// This method:
    /// <list type="bullet">
    /// <item><description>Raises the <see cref="Timeout"/> event.</description></item>
    /// <item><description>Sets <see cref="IsTimeoutEnabled"/> to <c>false</c> to stop further timeout tracking.</description></item>
    /// <item><description>Should be called by the monitoring component when it detects that
    /// <c>DateTime.UtcNow - StartedAt >= TimeoutInterval</c> while <see cref="IsTimeoutEnabled"/> is <c>true</c>.</description></item>
    /// </list>
    /// </remarks>
    void OnTimeout();
}