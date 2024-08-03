using System;

namespace FSFV.Gameplanner.Common.Rng;
public interface IRngProvider
{

    public const int STARTING_SEED = 628097725; // starting point

    int CurrentSeed { get; }
    void Clear();
    void Reset(int seed = STARTING_SEED);
    long NextInt64();

}