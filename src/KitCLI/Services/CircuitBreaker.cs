using System.Collections.Concurrent;

namespace KitCLI.Services;

/// <summary>
/// Circuit breaker pattern implementation to prevent cascading failures
/// </summary>
public sealed class CircuitBreaker
{
    private readonly int _failureThreshold;
    private readonly TimeSpan _resetTimeout;
    private readonly ConcurrentDictionary<string, CircuitState> _circuits = new();

    public CircuitBreaker(int failureThreshold = 5, TimeSpan? resetTimeout = null)
    {
        _failureThreshold = failureThreshold;
        _resetTimeout = resetTimeout ?? TimeSpan.FromMinutes(1);
    }

    /// <summary>
    /// Execute an operation with circuit breaker protection
    /// </summary>
    public async Task<T> ExecuteAsync<T>(string circuitName, Func<Task<T>> operation)
    {
        var state = _circuits.GetOrAdd(circuitName, _ => new CircuitState());

        // Check if circuit is open
        if (state.IsOpen)
        {
            if (DateTime.UtcNow - state.LastFailureTime < _resetTimeout)
            {
                throw new CircuitBreakerOpenException($"Circuit '{circuitName}' is open. Please wait before retrying.");
            }

            // Try to half-open the circuit
            state.Reset();
        }

        try
        {
            var result = await operation();
            state.RecordSuccess();
            return result;
        }
        catch (Exception)
        {
            state.RecordFailure();

            if (state.FailureCount >= _failureThreshold)
            {
                state.Open();
                Console.Error.WriteLine($"⚠️  Circuit breaker opened for '{circuitName}' after {_failureThreshold} failures");
                Console.Error.WriteLine($"   Will retry after {_resetTimeout.TotalSeconds:F0} seconds");
            }

            throw;
        }
    }

    /// <summary>
    /// Reset a specific circuit
    /// </summary>
    public void Reset(string circuitName)
    {
        if (_circuits.TryGetValue(circuitName, out var state))
        {
            state.Reset();
        }
    }

    /// <summary>
    /// Reset all circuits
    /// </summary>
    public void ResetAll()
    {
        foreach (var state in _circuits.Values)
        {
            state.Reset();
        }
    }

    /// <summary>
    /// Get the status of a circuit
    /// </summary>
    public CircuitStatus GetStatus(string circuitName)
    {
        if (!_circuits.TryGetValue(circuitName, out var state))
        {
            return CircuitStatus.Closed;
        }

        if (state.IsOpen)
        {
            if (DateTime.UtcNow - state.LastFailureTime >= _resetTimeout)
            {
                return CircuitStatus.HalfOpen;
            }
            return CircuitStatus.Open;
        }

        return CircuitStatus.Closed;
    }

    private sealed class CircuitState
    {
        private readonly object _lock = new();
        private int _failureCount;
        private DateTime _lastFailureTime;
        private bool _isOpen;

        public int FailureCount => _failureCount;
        public DateTime LastFailureTime => _lastFailureTime;
        public bool IsOpen => _isOpen;

        public void RecordSuccess()
        {
            lock (_lock)
            {
                _failureCount = 0;
                _isOpen = false;
            }
        }

        public void RecordFailure()
        {
            lock (_lock)
            {
                _failureCount++;
                _lastFailureTime = DateTime.UtcNow;
            }
        }

        public void Open()
        {
            lock (_lock)
            {
                _isOpen = true;
            }
        }

        public void Reset()
        {
            lock (_lock)
            {
                _failureCount = 0;
                _isOpen = false;
            }
        }
    }
}

/// <summary>
/// Circuit breaker states
/// </summary>
public enum CircuitStatus
{
    /// <summary>
    /// Circuit is functioning normally
    /// </summary>
    Closed,

    /// <summary>
    /// Circuit is blocking requests due to failures
    /// </summary>
    Open,

    /// <summary>
    /// Circuit is testing if the service has recovered
    /// </summary>
    HalfOpen
}

/// <summary>
/// Exception thrown when circuit breaker is open
/// </summary>
public sealed class CircuitBreakerOpenException : Exception
{
    public CircuitBreakerOpenException(string message) : base(message)
    {
    }
}

