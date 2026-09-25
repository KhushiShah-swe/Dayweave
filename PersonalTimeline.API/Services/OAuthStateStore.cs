using System.Collections.Concurrent;
using System.Security.Cryptography;
namespace PersonalTimeline.API.Services;

/// <summary>Single-instance, expiring, one-use OAuth correlation nonces.</summary>
public sealed class OAuthStateStore(TimeProvider? clock = null)
{
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;
    private readonly ConcurrentDictionary<string, (int UserId, string Provider, DateTimeOffset Expires)> _states = new();
    public string Create(int userId, string provider)
    {
        var now = _clock.GetUtcNow();
        foreach (var pair in _states.Where(pair => pair.Value.Expires <= now)) _states.TryRemove(pair.Key, out _);
        var state = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        _states[state] = (userId, provider, now.AddMinutes(10));
        return state;
    }
    public bool TryConsume(string state, string provider, out int userId)
    {
        userId = 0;
        if (!_states.TryRemove(state, out var value) || value.Expires <= _clock.GetUtcNow() || value.Provider != provider) return false;
        userId = value.UserId; return true;
    }
}
