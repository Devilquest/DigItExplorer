namespace DigItExplorer.Core.Ui;

/// <summary>Structural layer identifier for composed preview documents.</summary>
public enum MapLayer
{
    /// <summary>The level's terrain as the game draws it.</summary>
    Terrain,
    /// <summary>The terrain's collision plane, drawn over the art rather than in place of it.</summary>
    Collision,
    /// <summary>Labels naming where each exit leads.</summary>
    ExitInfo,
    /// <summary>Labels naming where each bonus warp leads.</summary>
    BonusInfo,
    WorldMapSky,
    /// <summary>The world map's middle scenery plane, between the sky and the plane the nodes stand on.</summary>
    WorldMapBackground,
    /// <summary>The world map's nearest plane, which carries the ground the nodes stand on.</summary>
    WorldMapFront,
    /// <summary>The route joining the world map's nodes, with its resting stops marked.</summary>
    WorldMapPath,
    WorldMapSignLevel,
    WorldMapSignCheckpoint,
    WorldMapSignGate,
    WorldMapSignTrace,
    EndSequenceBackground,
    EndSequenceText,
    MinigameBackground,
    /// <summary>The counter showing how many attempts the board has left.</summary>
    MinigameAttempts,
    SpinItPointer,
    FlipItRopes,
    FlipItCards,
    StopItPieces,
    StopItLabels,
    FindItPieces,
    FindItLabels,
    /// <summary>The parallax plane behind a slab screen.</summary>
    SlabBackground,
    /// <summary>A slab screen's own artwork, over the parallax and under the border.</summary>
    SlabForeground,
    /// <summary>The border drawn around a slab screen.</summary>
    SlabFrame,
    SlabText,
    MainMenuBackground,
    /// <summary>The menu's Dig It! title sign.</summary>
    MainMenuMainSign,
    MainMenuSetup,
    MainMenuPlay,
    MainMenuIntro,
    SpriteGraphic,
    /// <summary>The collision footprint of a composed mechanism.</summary>
    SpriteCollision,
}
