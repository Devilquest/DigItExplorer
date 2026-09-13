namespace DigItExplorer.Core.Knowledge.Animations;

/// <summary>Simulates the player's random idle wait sequence by walking the state graph (seg3:0x6D40).</summary>
public static class DugWaitSimulator
{
    /// <summary>Reachable frame transitions keyed by base frame index.</summary>
    public static readonly IReadOnlyDictionary<int, int[]> Graph = new Dictionary<int, int[]>
    {
        [64] = [66, 66, 72, 72],
        [66] = [68, 68, 66, 64],
        [68] = [70, 66, 66, 68],
        [70] = [68, 68, 70, 70],
        [72] = [64, 74, 74, 72],
        [74] = [72, 72, 74, 74],
    };

    /// <summary>Walks the transition graph using a random 2-bit selector delegate.</summary>
    /// <param name="pick4">Supplies random values in the range 0..3.</param>
    /// <param name="pairs">How many pair-transitions to walk.</param>
    /// <returns>Array of frame indices in playback order.</returns>
    public static int[] Walk(Func<int> pick4, int pairs)
    {
        int cur = 64;
        var frames = new int[pairs * 16];
        for (int p = 0; p < pairs; p++)
        {
            cur = Graph[cur & ~1][pick4()];
            for (int tick = 0; tick < 16; tick++)
            {
                cur ^= 1;
                frames[p * 16 + tick] = cur;
            }
        }
        return frames;
    }

    /// <summary>Walks the transition graph using a Random instance.</summary>
    public static int[] Walk(Random rng, int pairs) => Walk(() => rng.Next(4), pairs);
}
