namespace DigItExplorer.Tests;

/// <summary>What a test needs of the copy of the game it is pointed at.</summary>
[Flags]
public enum GameNeeds
{
    /// <summary>Any folder holding the game's archives, whichever build it is.</summary>
    AnyCopy = 0,

    /// <summary>A <c>MAIN.EXE</c> this application's addresses were derived against.</summary>
    SupportedBuild = 1,

    /// <summary>A copy shipping the whole game rather than the shareware's single world.</summary>
    FullResourceSet = 2,

    /// <summary>Archives whose contents this project has measured, which no damaged copy has.</summary>
    MeasuredArchives = 4,
}

/// <summary>A fact that reports itself skipped, not passed, when the copy of the game will not do.</summary>
public sealed class RequiresGameFactAttribute : FactAttribute
{
    private readonly GameNeeds _needs;
    private string[] _intact = [];

    public RequiresGameFactAttribute(GameNeeds needs = GameNeeds.AnyCopy)
    {
        _needs = needs;
        Skip = TestPaths.SkipReasonFor(needs, _intact);
    }

    /// <summary>Entries this test reads, which a copy recorded as having lost any of them cannot run it.</summary>
    public string[] Intact
    {
        get => _intact;
        set
        {
            _intact = value;
            Skip = TestPaths.SkipReasonFor(_needs, value);
        }
    }
}

/// <summary>The <see cref="TheoryAttribute"/> counterpart of <see cref="RequiresGameFactAttribute"/>.</summary>
public sealed class RequiresGameTheoryAttribute : TheoryAttribute
{
    private readonly GameNeeds _needs;
    private string[] _intact = [];

    public RequiresGameTheoryAttribute(GameNeeds needs = GameNeeds.AnyCopy)
    {
        _needs = needs;
        Skip = TestPaths.SkipReasonFor(needs, _intact);
    }

    /// <summary>Entries this test reads, which a copy recorded as having lost any of them cannot run it.</summary>
    public string[] Intact
    {
        get => _intact;
        set
        {
            _intact = value;
            Skip = TestPaths.SkipReasonFor(_needs, value);
        }
    }
}
