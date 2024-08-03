using Microsoft.Extensions.Logging;
using System;

namespace FSFV.Gameplanner.Common.Rng;
public class RngProvider(ILogger<RngProvider> logger) : IRngProvider
{

    const int SEED = IRngProvider.STARTING_SEED;

    public int CurrentSeed { get; private set; } = SEED;
    private Random Instance { get; set; } = new(SEED);
    private int debugCallCount = 0;

    public void Reset(int seed = SEED)
    {
        debugCallCount = 0;
        Instance = new(seed);
        CurrentSeed = seed;
    }

    public void Clear()
    {
        var seed = (int)(DateTimeOffset.Now.ToUnixTimeMilliseconds() % Int64.MaxValue);
        this.Reset(seed);
    }

    public long NextInt64()
    {
        logger.LogTrace("Rng Called: {cnt}", ++debugCallCount);
        return Instance.NextInt64();
    }

}
