using DigItExplorer.Core.Knowledge.Animations;

namespace DigItExplorer.Tests;

/// <summary>Guards state graph transition rules and frame toggling in <see cref="DugWaitSimulator"/>.</summary>
public class DugWaitSimulatorTests
{
    [Fact]
    public void Walk_has_the_requested_length()
    {
        int[] frames = DugWaitSimulator.Walk(() => 0, pairs: 12);
        Assert.Equal(12 * 16, frames.Length);
    }

    [Fact]
    public void Walk_toggles_pair_partner_every_tick_within_a_pair()
    {
        // Only within a pair's own 16-tick window: at each pair boundary (every 16th frame) a brand new
        // base is looked up from the graph, so it isn't generally the XOR-1 partner of the previous frame.
        int[] frames = DugWaitSimulator.Walk(() => 0, pairs: 20);
        for (int i = 1; i < frames.Length; i++)
        {
            if (i % 16 == 0) continue;
            Assert.Equal(frames[i - 1] ^ 1, frames[i]);
        }
    }

    [Fact]
    public void Walk_pair_transitions_follow_real_graph_edges()
    {
        // Cycle deterministically through every pick value (0..3) so all four graph columns get exercised.
        int call = 0;
        int[] frames = DugWaitSimulator.Walk(() => call++ % 4, pairs: 20);

        int prevBase = 64;
        for (int p = 0; p < 20; p++)
        {
            int pick = p % 4;
            int expectedBase = DugWaitSimulator.Graph[prevBase][pick];
            int actualBase = frames[p * 16] & ~1; // toggling only ever flips the low bit
            Assert.Equal(expectedBase, actualBase);
            prevBase = expectedBase;
        }
    }

    [Fact]
    public void Walk_with_Random_stays_within_the_six_known_pairs()
    {
        int[] frames = DugWaitSimulator.Walk(new Random(1), pairs: 50);
        Assert.All(frames, f => Assert.Contains(f & ~1, DugWaitSimulator.Graph.Keys));
    }
}
