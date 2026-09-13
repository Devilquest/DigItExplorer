using DigItExplorer.Core.Knowledge.Animations;
using DigItExplorer.Core.Ui;

namespace DigItExplorer.Tests;

/// <summary>Verifies <see cref="AnimationStatusLine"/> keeps a still's wording whatever the preview does to it.</summary>
public class AnimationStatusLineTests
{
    private static string Line(AnimMode mode, int[] frames, int step, bool stepsAreState = false)
        => AnimationStatusLine.Describe("Slugger", "Dry Lands", "Fall away", mode, frames, step, 42.86,
            stepsAreState);

    [Fact]
    public void A_pose_is_named_by_its_frame_rather_than_counted_in_steps()
    {
        Assert.Equal("Slugger    ·    Dry Lands    ·    Fall away    ·    single pose (frame 27)",
            Line(AnimMode.Pose, [27], 0));
    }

    [Fact]
    public void Selecting_the_only_step_of_a_pose_leaves_the_wording_alone()
    {
        // Clicking the lone thumbnail asks for the step already shown, which must not turn a pose into a run.
        string line = Line(AnimMode.Pose, [27], 0);
        Assert.DoesNotContain("ms/step", line);
        Assert.DoesNotContain("step 01/01", line);
    }

    [Fact]
    public void A_lone_step_is_a_still_whatever_mode_it_was_defined_with()
    {
        Assert.True(AnimationStatusLine.IsStill(AnimMode.Once, 1));
        Assert.True(AnimationStatusLine.IsStill(AnimMode.Loop, 1));
        Assert.True(AnimationStatusLine.IsStill(AnimMode.Pose, 1));
        Assert.False(AnimationStatusLine.IsStill(AnimMode.Loop, 2));
    }

    [Fact]
    public void A_sequence_counts_its_steps_and_names_its_pace()
    {
        Assert.Equal("Slugger    ·    Dry Lands    ·    Fall away (loop)    ·    "
            + "step 1/4 (frame 8)    ·    42.9 ms/step", Line(AnimMode.Loop, [8, 9, 8, 9], 0));
    }

    [Fact]
    public void The_step_and_frame_numbers_are_padded_to_the_wider_of_the_two()
    {
        Assert.Contains("step 01/04 (frame 27)", Line(AnimMode.Loop, [27, 28, 29, 30], 0));
    }

    [Fact]
    public void Steps_that_are_meter_readings_carry_no_pace()
    {
        Assert.DoesNotContain("ms/step", Line(AnimMode.Once, [6, 7, 8], 1, stepsAreState: true));
    }

    [Fact]
    public void Identity_and_step_progress_split_the_description_cleanly()
    {
        string identity = AnimationStatusLine.Identity("Slugger", "Dry Lands", "Fall away");
        string progress = AnimationStatusLine.StepProgress(AnimMode.Loop, [8, 9, 8, 9], 0, 42.86, false);

        Assert.Equal("Slugger    ·    Dry Lands    ·    Fall away", identity);
        Assert.Equal("step 1/4 (frame 8)    ·    42.9 ms/step", progress);
        Assert.Equal($"{identity} (loop)    ·    {progress}", Line(AnimMode.Loop, [8, 9, 8, 9], 0));
    }
}
