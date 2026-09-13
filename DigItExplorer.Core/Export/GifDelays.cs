namespace DigItExplorer.Core.Export;

/// <summary>Calculates dithered integer centisecond delays to match precise frame durations.</summary>
internal static class GifDelays
{
    /// <summary>Generates an array of centisecond delays for an animation sequence.</summary>
    /// <param name="frameCount">Total number of frames.</param>
    /// <param name="msPerFrame">Target milliseconds per frame.</param>
    /// <returns>Array of integer delays in hundredths of a second.</returns>
    internal static int[] Centiseconds(int frameCount, double msPerFrame)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(frameCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(msPerFrame);

        var delays = new int[frameCount];
        double elapsedMs = 0;
        int spent = 0;

        for (int i = 0; i < frameCount; i++)
        {
            elapsedMs += msPerFrame;

            // At least one hundredth: zero would tell a viewer to show the frame for no time at all, and
            // most of them answer that by substituting a delay of their own choosing.
            int delay = Math.Max(1, (int)Math.Round(elapsedMs / 10.0, MidpointRounding.ToEven) - spent);
            delays[i] = delay;
            spent += delay;
        }

        return delays;
    }
}
