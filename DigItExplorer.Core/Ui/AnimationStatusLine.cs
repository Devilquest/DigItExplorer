using System.Globalization;
using DigItExplorer.Core.Knowledge.Animations;

namespace DigItExplorer.Core.Ui;

/// <summary>The line naming what the animation preview is showing.</summary>
public static class AnimationStatusLine
{
    private const string Gap = "    ·    ";

    /// <summary>Whether the animation is one held image rather than something that plays.</summary>
    /// <param name="mode">Playback mode the animation is defined with.</param>
    /// <param name="stepCount">How many steps the animation runs through.</param>
    public static bool IsStill(AnimMode mode, int stepCount) => mode == AnimMode.Pose || stepCount <= 1;

    /// <summary>Names the character, skin, and animation for the preview header.</summary>
    /// <param name="character">Character the animation belongs to.</param>
    /// <param name="skinLabel">Skin or location the sheet was read from.</param>
    /// <param name="animLabel">Name of the animation.</param>
    public static string Identity(string character, string skinLabel, string animLabel)
        => $"{character}{Gap}{skinLabel}{Gap}{animLabel}";

    /// <summary>Describes the active playback step index, sheet frame, and pace.</summary>
    /// <param name="mode">Playback mode the animation is defined with.</param>
    /// <param name="frames">Sheet frame each step draws.</param>
    /// <param name="stepIndex">Step being shown, counted from zero.</param>
    /// <param name="stepMs">Milliseconds a step lasts at the chosen speed.</param>
    /// <param name="stepsAreState">Whether the steps are meter readings rather than motion.</param>
    public static string StepProgress(AnimMode mode, IReadOnlyList<int> frames, int stepIndex, double stepMs, bool stepsAreState)
    {
        if (IsStill(mode, frames.Count))
            return $"single pose (frame {frames[stepIndex]})";

        int digits = Math.Max(frames.Count.ToString().Length, frames.Max().ToString().Length);
        string pace = stepsAreState ? "" : $"{Gap}{stepMs.ToString("0.#", CultureInfo.InvariantCulture)} ms/step";
        return $"step {(stepIndex + 1).ToString("D" + digits)}/{frames.Count.ToString("D" + digits)} "
            + $"(frame {frames[stepIndex].ToString("D" + digits)}){pace}";
    }

    /// <summary>Describes the step being shown, which for a still is the pose itself.</summary>
    /// <param name="character">Character the animation belongs to.</param>
    /// <param name="skinLabel">Skin or location the sheet was read from.</param>
    /// <param name="animLabel">Name of the animation.</param>
    /// <param name="mode">Playback mode the animation is defined with.</param>
    /// <param name="frames">Sheet frame each step draws.</param>
    /// <param name="stepIndex">Step being shown, counted from zero.</param>
    /// <param name="stepMs">Milliseconds a step lasts at the chosen speed.</param>
    /// <param name="stepsAreState">Whether the steps are meter readings rather than motion.</param>
    public static string Describe(string character, string skinLabel, string animLabel, AnimMode mode,
        IReadOnlyList<int> frames, int stepIndex, double stepMs, bool stepsAreState)
    {
        string head = Identity(character, skinLabel, animLabel);
        if (IsStill(mode, frames.Count))
            return $"{head}{Gap}single pose (frame {frames[stepIndex]})";

        return $"{head} ({mode.ToString().ToLowerInvariant()}){Gap}"
            + StepProgress(mode, frames, stepIndex, stepMs, stepsAreState);
    }
}
