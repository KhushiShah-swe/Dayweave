using PersonalTimeline.API.Services;
namespace PersonalTimeline.Tests;
public class OAuthStateTests
{
    [Fact]
    public void StateIsUnpredictableUserBoundAndSingleUse()
    {
        var states = new OAuthStateStore();
        var first = states.Create(42, "Spotify"); var second = states.Create(42, "Spotify");
        Assert.NotEqual(first, second); Assert.Equal(64, first.Length);
        Assert.False(states.TryConsume("42", "Spotify", out _));
        Assert.True(states.TryConsume(first, "Spotify", out var user)); Assert.Equal(42, user);
        Assert.False(states.TryConsume(first, "Spotify", out _));
    }
    [Fact]
    public void RejectsWrongProviderAndExpiredState()
    {
        var clock = new MutableClock(); var states = new OAuthStateStore(clock);
        var wrongProvider = states.Create(1, "Spotify");
        Assert.False(states.TryConsume(wrongProvider, "YouTube", out _));
        var expired = states.Create(1, "Spotify"); clock.Now = clock.Now.AddMinutes(11);
        Assert.False(states.TryConsume(expired, "Spotify", out _));
    }
    private sealed class MutableClock : TimeProvider
    {
        public DateTimeOffset Now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
