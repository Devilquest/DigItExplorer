using System.Text;
using DigItExplorer.App.Models;
using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Knowledge.Animations;
using DigItExplorer.Core.Minigames;

namespace DigItExplorer.App.Services.ResourceTree;

/// <summary>Constructs the Screens/UI resource tree branch for cutscenes, slabs, and minigames.</summary>
internal static class ScreensBranch
{
    /// <summary>Builds the Screens/UI branch covering logos, title sequence, intro scenes, end sequence, and minigames.</summary>
    public static TreeNode? Build(HashSet<string> present, GameData data, SkinCatalog skins)
    {
        // The intro's own piece file names come from the game's invocation of each piece, so the presence
        // checks and labels below follow whatever this install actually plays.
        string FileOf(CutscenePiece piece) => data.Cutscenes.Pieces[piece].FileName!;
        string StemOf(CutscenePiece piece) => System.IO.Path.GetFileNameWithoutExtension(FileOf(piece));

        var root = new TreeNode { Label = "Screens & UI", IsExpanded = true };

        // Ahead of PPLOGO because that is the order a build carrying the prologue plays them in. A build
        // without one has no entry for the piece, and the file it would play stays a Raw one.
        if (data.Cutscenes.Pieces.TryGetValue(CutscenePiece.ManLogo, out var logo) && present.Contains(logo.FileName!))
        {
            var logoStem = StemOf(CutscenePiece.ManLogo);
            root.Children.Add(new TreeNode { Label = logoStem, Cutscene = CutscenePiece.ManLogo, InfoPath = [logoStem] });
        }

        if (present.Contains("PPLOGO.SPF"))
            root.Children.Add(new TreeNode { Label = "PPLOGO", Resource = "PPLOGO.SPF", InfoPath = ["PPLOGO"], UseScreensZoom = true });

        if (present.Contains(FileOf(CutscenePiece.DigTitle)))
        {
            var titleStem = StemOf(CutscenePiece.DigTitle);
            root.Children.Add(new TreeNode { Label = titleStem, Cutscene = CutscenePiece.DigTitle, InfoPath = [titleStem] });
        }

        if (present.Contains("LVL900F.MPF") && present.Contains("LVL900.DLF"))
            root.Children.Add(new TreeNode { Label = "Main Menu (LVL900)", MainMenu = true, InfoPath = ["Main Menu"] });

        bool hasIntrodrg = present.Contains("INTRODRG.SPF") && present.Contains("INTRODRG.TXT");
        bool hasIntro01 = present.Contains(FileOf(CutscenePiece.Intro01));
        bool hasIntro03 = present.Contains(FileOf(CutscenePiece.Intro03));
        bool hasCard = present.Contains(data.Cutscenes.CardPaletteFile);
        bool hasIntro02 = present.Contains(FileOf(CutscenePiece.Intro02));
        bool hasR00 = present.Contains("R00_SCRN.SPF") && present.Contains("R00_STRY.TXT");
        bool hasIntro00 = present.Contains(FileOf(CutscenePiece.Intro00));

        if (hasIntrodrg || hasIntro01 || hasIntro03 || hasCard || hasIntro02 || hasR00 || hasIntro00)
        {
            var intro = new TreeNode { Label = "Intro" };
            string[] Path(string piece) => ["Intro", piece];

            if (hasIntrodrg)
                intro.Children.Add(IntroSceneBranch("INTRODRG", "INTRODRG.SPF", "INTRODRG.TXT", IntroScene.Introdrg));
            if (hasIntro01)
                intro.Children.Add(PieceLeaf(CutscenePiece.Intro01));
            if (hasIntro03)
                intro.Children.Add(PieceLeaf(CutscenePiece.Intro03));
            if (hasCard)
            {
                var cardLabel = Encoding.Latin1.GetString(data.Cutscenes.CardText);
                intro.Children.Add(new TreeNode { Label = cardLabel, Cutscene = CutscenePiece.Card, InfoPath = Path(cardLabel) });
            }
            if (hasIntro02)
                intro.Children.Add(PieceLeaf(CutscenePiece.Intro02));

            TreeNode PieceLeaf(CutscenePiece piece)
            {
                var stem = StemOf(piece);
                return new TreeNode { Label = stem, Cutscene = piece, InfoPath = Path(stem) };
            }
            if (hasR00)
                intro.Children.Add(IntroSceneBranch("R00", "R00_SCRN.SPF", "R00_STRY.TXT", IntroScene.R00));

            if (hasIntro00)
            {
                var label = $"{StemOf(CutscenePiece.Intro00)} (unused)";
                intro.Children.Add(new TreeNode { Label = label, Cutscene = CutscenePiece.Intro00, InfoPath = Path(label) });
            }

            if (intro.Children.Count > 0) root.Children.Add(intro);
        }

        if (present.Contains(data.EndSequence.FileName))
            root.Children.Add(EndSequenceBranch(data));

        var gameOver = BuildGameOverBranch(present);
        if (gameOver is not null) root.Children.Add(gameOver);

        var instructions = BuildSlabBranch(present, data, SlabData.Instructions, "Instructions");
        if (instructions is not null) root.Children.Add(instructions);
        var credits = BuildSlabBranch(present, data, SlabData.Credits, "Credits");
        if (credits is not null) root.Children.Add(credits);

        var minigames = BuildMinigamesBranch(present, skins);
        if (minigames is not null) root.Children.Add(minigames);

        return root.Children.Count > 0 ? root : null;
    }

    /// <summary>Builds the bonus minigames sub-branch covering boards, titles, and mechanics.</summary>
    private static TreeNode? BuildMinigamesBranch(HashSet<string> present, SkinCatalog skins)
    {
        var root = new TreeNode { Label = "Minigames" };

        TreeNode? BuildGame(string label, string titleFile, CharacterAnimSet? set,
            MinigameBoard? board = null, string? boardBackgroundFile = null, CharacterAnimSet? pieces = null)
        {
            var mechanics = new TreeNode { Label = "Mechanics" };
            if (set is not null) CharacterNodes.Add(mechanics, skins, [set], collapseCharacter: true);

            var piecesNode = new TreeNode { Label = "Pieces" };
            if (pieces is not null) CharacterNodes.Add(piecesNode, skins, [pieces], collapseCharacter: true);

            bool hasTitle = present.Contains(titleFile);
            bool hasBoard = board is not null && boardBackgroundFile is not null && present.Contains(boardBackgroundFile);
            if (!hasTitle && !hasBoard && mechanics.Children.Count == 0 && piecesNode.Children.Count == 0) return null;

            var game = new TreeNode { Label = label };
            if (hasTitle)
                game.Children.Add(new TreeNode
                {
                    Label = "Title Screen", Resource = titleFile,
                    InfoPath = ["Minigames", label, "Title Screen"], UseScreensZoom = true,
                });
            if (hasBoard)
                game.Children.Add(new TreeNode
                {
                    Label = "Board", Board = board, UseScreensZoom = true,
                    InfoPath = ["Minigames", label, "Board"],
                });
            if (piecesNode.Children.Count > 0) game.Children.Add(piecesNode);
            if (mechanics.Children.Count > 0) game.Children.Add(mechanics);
            return game;
        }

        var flipIt = BuildGame(skins.CharacterLabel(AnimationTables.FlipIt), "GM0_TTL.SPF", AnimationTables.FlipIt,
            MinigameBoard.FlipIt, "GM0_PARA.SPF");
        if (flipIt is not null) root.Children.Add(flipIt);
        var stopIt = BuildGame("Stop It!", "GM1_TTL.SPF", null, MinigameBoard.StopIt, "GM1_SCRN.SPF",
            AnimationTables.StopItPieces);
        if (stopIt is not null) root.Children.Add(stopIt);
        var spinIt = BuildGame(skins.CharacterLabel(AnimationTables.SpinIt), "GM2_TTL.SPF", AnimationTables.SpinIt,
            MinigameBoard.SpinIt, "GM2_SCRN.SPF");
        if (spinIt is not null) root.Children.Add(spinIt);
        var findIt = BuildGame("Find It!", "GM3_TTL.SPF", null, MinigameBoard.FindIt, "GM3_SCRN.SPF",
            AnimationTables.FindItPieces);
        if (findIt is not null) root.Children.Add(findIt);

        var prizeIcons = new TreeNode { Label = "Prize Icons" };
        CharacterNodes.Add(prizeIcons, skins,
            [AnimationTables.FlipItPrizeIcons, AnimationTables.StopItPrizeIcons, AnimationTables.FindItPrizeIcons],
            collapseCharacter: false);
        if (prizeIcons.Children.Count > 0) root.Children.Add(prizeIcons);

        return root.Children.Count > 0 ? root : null;
    }

    /// <summary>Builds the ending sequence credit screen leaves.</summary>
    private static TreeNode EndSequenceBranch(GameData data)
    {
        var root = new TreeNode { Label = "Ending Sequence" };
        for (int i = 0; i < data.EndSequence.Frames.Count; i++)
        {
            var label = $"Screen {i + 1}";
            root.Children.Add(new TreeNode { Label = label, EndSequenceScreen = i, InfoPath = ["Ending Sequence", label] });
        }
        return root;
    }

    /// <summary>Builds the Game Over screen leaves.</summary>
    private static TreeNode? BuildGameOverBranch(HashSet<string> present)
    {
        var root = new TreeNode { Label = "Game Over" };
        for (int i = 0; i < 4; i++)
        {
            var file = $"GOVER{i}.SPF";
            if (present.Contains(file))
                root.Children.Add(new TreeNode { Label = $"GOVER{i}", Resource = file, InfoPath = ["Game Over", $"GOVER{i}"], UseScreensZoom = true });
        }
        return root.Children.Count > 0 ? root : null;
    }

    /// <summary>Builds the Instructions or Credits slab screen leaves.</summary>
    private static TreeNode? BuildSlabBranch(HashSet<string> present, GameData data, int screen, string label)
    {
        string prefix = $"SLB{screen:00}";
        if (!present.Contains($"{prefix}F.MPF") || !present.Contains($"{prefix}FRM.SPF")) return null;

        var root = new TreeNode { Label = label };
        int count = data.Slab.SlabCount(screen);
        for (int i = 0; i < count; i++)
        {
            var stopLabel = $"Slab {i + 1}";
            root.Children.Add(new TreeNode
            {
                Label = stopLabel, Slab = new SlabRef(screen, i), InfoPath = [label, stopLabel],
            });
        }
        return root;
    }

    /// <summary>Builds an intro story scene branch containing background, text, and reconstruction leaves.</summary>
    private static TreeNode IntroSceneBranch(string label, string scrn, string stry, IntroScene scene)
    {
        var node = new TreeNode { Label = label };
        string[] Path(string piece) => ["Intro", label, piece];
        node.Children.Add(new TreeNode { Label = "Background", Resource = scrn, InfoPath = Path("Background"), UseScreensZoom = true });
        node.Children.Add(new TreeNode { Label = "Story Text", Resource = stry, InfoPath = Path("Story Text") });
        node.Children.Add(new TreeNode { Label = "Reconstruction", IntroScene = scene, InfoPath = Path("Reconstruction") });
        return node;
    }
}
