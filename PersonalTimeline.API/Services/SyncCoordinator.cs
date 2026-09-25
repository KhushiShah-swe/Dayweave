namespace PersonalTimeline.API.Services;

/// <summary>Serializes provider sync and disconnect operations for this SQLite process.</summary>
public sealed class SyncCoordinator : IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    public async Task<IDisposable> EnterAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        return new Lease(_gate);
    }
    private sealed class Lease(SemaphoreSlim gate) : IDisposable
    {
        private int _disposed;
        public void Dispose() { if (Interlocked.Exchange(ref _disposed, 1) == 0) gate.Release(); }
    }
    public void Dispose() => _gate.Dispose();
}
