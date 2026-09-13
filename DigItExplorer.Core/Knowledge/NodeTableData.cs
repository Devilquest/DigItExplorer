using System.Text;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Core.Knowledge;

/// <summary>Container for world map node records and world names parsed from <c>MAIN.EXE</c>.</summary>
public sealed class NodeTableData
{
    // Record layout at DS:0x27A: u16 sign; pascal name; u16 dest_map @23; u16 dest_node @25.
    private const int SignField = 0, NameField = 2, DestMapField = 23, DestNodeField = 25;
    private const int EmptyNode = 0xFFFF;

    private readonly Dictionary<World, IReadOnlyList<LevelNode>> _nodes = [];
    private readonly Dictionary<World, string> _worldNames = [];

    private NodeTableData() { }

    /// <summary>The node-info records of a world's map, in node-index order.</summary>
    public IReadOnlyList<LevelNode> Nodes(World world) =>
        _nodes.TryGetValue(world, out var nodes) ? nodes : [];

    /// <summary>The node record for <paramref name="index"/> on <paramref name="world"/>'s map, or null if empty.</summary>
    public LevelNode? Node(World world, int index) =>
        _nodes.TryGetValue(world, out var nodes) ? nodes.FirstOrDefault(n => n.Index == index) : null;

    /// <summary>Returns the in-game display name of a world derived from gate labels.</summary>
    public string WorldName(World world) =>
        _worldNames.TryGetValue(world, out var name) ? name : world.ToString();

    /// <summary>Parses the 5-map x 16-node table from the executable.</summary>
    internal static bool TryRead(ExeReader reader, out NodeTableData data)
    {
        data = new NodeTableData();

        for (int map = 0; map < ExeLayout.MapCount; map++)
        {
            var world = (World)map;
            var nodes = new List<LevelNode>();

            for (int index = 0; index < ExeLayout.NodesPerMap; index++)
            {
                var record = ExeLayout.NodeTable + (map * ExeLayout.NodesPerMap + index) * ExeLayout.NodeRecordSize;
                if (!reader.Covers(record, ExeLayout.NodeRecordSize)) return false;

                if (!reader.TryReadWord(record + SignField, out int sign)) return false;
                if (sign == EmptyNode) continue;
                if (sign > (int)SignType.Gate) return false; // not a sign kind this build knows

                if (!reader.TryReadPascalString(record + NameField, out var name)) return false;
                if (!reader.TryReadWord(record + DestMapField, out int destMap)) return false;
                if (!reader.TryReadWord(record + DestNodeField, out int destNode)) return false;

                var kind = (SignType)sign;
                GateTarget? gate = null;
                if (kind == SignType.Gate)
                {
                    if (destMap >= ExeLayout.MapCount) return false;
                    gate = new GateTarget((World)destMap, destNode);
                    data._worldNames[(World)destMap] = Encoding.Latin1.GetString(name);
                }

                nodes.Add(new LevelNode(index, kind, Encoding.Latin1.GetString(name), gate));
            }

            data._nodes[world] = nodes;
        }

        return true;
    }
}
