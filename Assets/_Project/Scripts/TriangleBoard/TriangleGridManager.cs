using System.Collections.Generic;
using UnityEngine;

public class TriangleGridManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TriangleMapDefinition mapDefinition;
    [SerializeField] private TriangleLineRenderer lineRenderer;

    private readonly Dictionary<Vector2Int, TriangleCell> cells = new();
    private readonly Dictionary<string, TriangleNode> nodes = new();

    public TriangleMapDefinition MapDefinition => mapDefinition;

    public IEnumerable<TriangleCell> AllCells => cells.Values;
    public IEnumerable<TriangleNode> AllNodes => nodes.Values;

    private void Start()
    {
        GenerateGrid();
    }

    [ContextMenu("Generate Triangle Grid")]
    public void GenerateGrid()
    {
        ClearGrid();

        if (mapDefinition == null)
        {
            Debug.LogError("TriangleGridManager has no TriangleMapDefinition assigned.");
            return;
        }

        BuildTriangleCells();
        ApplyStamps();

        if (lineRenderer != null)
            lineRenderer.BuildLines(this);

        Debug.Log(
            $"Triangle grid generated. Cells: {cells.Count}, Nodes: {nodes.Count}, " +
            $"Center column: {mapDefinition.CenterColumn}, Center row: {mapDefinition.CenterRow}"
        );
    }

    void ClearGrid()
    {
        cells.Clear();
        nodes.Clear();
    }

    void BuildTriangleCells()
{
    for (int row = 0; row < mapDefinition.triangleRows; row++)
    {
        for (
            int col = mapDefinition.GeneratedMinColumn;
            col <= mapDefinition.GeneratedMaxColumn;
            col++
        )
        {
            TriangleCell cell = CreateCell(new Vector2Int(col, row));
            cells[cell.coord] = cell;
        }
    }
}

    TriangleCell CreateCell(Vector2Int coord)
    {
        TriangleOrientation orientation = GetOrientation(coord);

        Vector3[] cornerPositions = GetCornerPositions(coord, orientation);

        TriangleCell cell = new TriangleCell
        {
            coord = coord,
            orientation = orientation,
            isActive = false,
            isBlocked = false,
            region = MapRegion.None
        };

        cell.corners[0] = GetOrCreateNode(cornerPositions[0]);
        cell.corners[1] = GetOrCreateNode(cornerPositions[1]);
        cell.corners[2] = GetOrCreateNode(cornerPositions[2]);

        cell.sideMidpoints[0] = GetOrCreateNode((cornerPositions[0] + cornerPositions[1]) * 0.5f);
        cell.sideMidpoints[1] = GetOrCreateNode((cornerPositions[1] + cornerPositions[2]) * 0.5f);
        cell.sideMidpoints[2] = GetOrCreateNode((cornerPositions[2] + cornerPositions[0]) * 0.5f);

        // Centroid:
        // For an Up triangle this is 1/3 from bottom base toward top.
        // For a Down triangle this is 1/3 from top base toward bottom.
        Vector3 center = (cornerPositions[0] + cornerPositions[1] + cornerPositions[2]) / 3f;
        cell.center = GetOrCreateNode(center);

        foreach (TriangleNode node in cell.AllNodes)
            node.AddOwner(cell);

        return cell;
    }

    TriangleOrientation GetOrientation(Vector2Int coord)
{
    bool evenColumn = IsEven(coord.x);

    bool isUp = mapDefinition.firstColumnIsUp
        ? evenColumn
        : !evenColumn;

    return isUp ? TriangleOrientation.Up : TriangleOrientation.Down;
}

bool IsEven(int value)
{
    return Mathf.Abs(value % 2) == 0;
}

    Vector3[] GetCornerPositions(Vector2Int coord, TriangleOrientation orientation)
    {
        float side = mapDefinition.sideLength;
        float halfSide = side * 0.5f;
        float height = mapDefinition.TriangleHeight;

        float x = coord.x * halfSide;
        float y = coord.y * height;

        if (mapDefinition.offsetEveryOtherRow && coord.y % 2 == 1)
            x += halfSide;

        if (orientation == TriangleOrientation.Up)
        {
            return new[]
            {
                new Vector3(x, y, 0f),
                new Vector3(x + side, y, 0f),
                new Vector3(x + halfSide, y + height, 0f)
            };
        }

        return new[]
        {
            new Vector3(x, y + height, 0f),
            new Vector3(x + side, y + height, 0f),
            new Vector3(x + halfSide, y, 0f)
        };
    }

    TriangleNode GetOrCreateNode(Vector3 position)
    {
        string key = MakeNodeKey(position);

        if (nodes.TryGetValue(key, out TriangleNode existing))
            return existing;

        TriangleNode node = new TriangleNode(key, position);
        nodes[key] = node;
        return node;
    }

    string MakeNodeKey(Vector3 position)
    {
        int x = Mathf.RoundToInt(position.x * 100000f);
        int y = Mathf.RoundToInt(position.y * 100000f);
        int z = Mathf.RoundToInt(position.z * 100000f);

        return $"{x},{y},{z}";
    }

    void ApplyStamps()
    {
        foreach (TriangleCell cell in cells.Values)
        {
            cell.isActive = false;
            cell.isBlocked = false;
            cell.region = MapRegion.None;
        }

        foreach (TriangleMapStamp stamp in mapDefinition.stamps)
        {
            if (stamp == null || !stamp.enabled)
                continue;

            int matches = 0;

            foreach (TriangleCell cell in cells.Values)
            {
                if (!stamp.ContainsCell(cell, mapDefinition))
                    continue;

                matches++;
                ApplyStampToCell(stamp, cell);
            }

            Debug.Log(
                $"Triangle stamp '{stamp.name}' matched {matches} cells. " +
                $"Mode: {stamp.mode}, Region: {stamp.region}"
            );
        }
    }

    void ApplyStampToCell(TriangleMapStamp stamp, TriangleCell cell)
    {
        MapRegion appliedRegion = stamp.GetRegionForCell(cell, mapDefinition);

        switch (stamp.mode)
        {
            case MapStampMode.Add:
                cell.isActive = true;
                cell.region = appliedRegion == MapRegion.None
                    ? MapRegion.Neutral
                    : appliedRegion;
                cell.isBlocked = false;
                break;

            case MapStampMode.SetRegion:
                if (!cell.isActive)
                    return;

                cell.region = appliedRegion == MapRegion.None
                    ? MapRegion.Neutral
                    : appliedRegion;
                break;

            case MapStampMode.Remove:
                cell.isActive = false;
                cell.region = MapRegion.None;
                cell.isBlocked = false;
                break;

            case MapStampMode.BlockTerrain:
                if (!cell.isActive)
                    return;

                cell.region = MapRegion.Blocked;
                cell.isBlocked = true;
                break;
        }
    }

    public TriangleCell GetCell(Vector2Int coord)
    {
        cells.TryGetValue(coord, out TriangleCell cell);
        return cell;
    }

    public void ClearAllNodeVisualStates()
    {
        foreach (TriangleNode node in nodes.Values)
            node.ClearVisualStates();
    }
}