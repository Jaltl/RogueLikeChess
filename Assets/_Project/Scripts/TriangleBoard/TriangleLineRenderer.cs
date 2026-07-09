using System.Collections.Generic;
using UnityEngine;

public class TriangleLineRenderer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TriangleGridManager grid;

    [Header("Profile Edge Fixes")]
    [SerializeField] private bool addShrinkingProfileClosureEdges = true;
    [SerializeField] private float connectorTolerance = 0.35f;

    [Header("Line Visuals")]
    [SerializeField] private Material lineMaterial;
    [SerializeField] private float lineWidth = 0.01f;
    [SerializeField] private int sortingOrder = 50;
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private float zOffset = -0.1f;

    [Header("Dynamic Colors")]
    [SerializeField] private Color placementColor = Color.yellow;
    [SerializeField] private Color hoverColor = Color.white;
    [SerializeField] private Color footprintColor = new Color(0.2f, 0.5f, 1f, 1f);
    [SerializeField] private Color invalidColor = Color.red;

    private Material runtimeLineMaterial;

    private class RenderedSegment
    {
        public TriangleNode a;
        public TriangleNode b;
        public LineRenderer line;
        public readonly List<TriangleCell> ownerTriangles = new();
    }

    private class RegionRowBounds
{
    public bool hasCells;

    public int leftProfileX = int.MaxValue;
    public int rightProfileX = int.MinValue;

    public TriangleCell leftCell;
    public TriangleCell rightCell;
}

private class RowContour
{
    public bool hasCells;

    public TriangleCell leftCell;
    public TriangleCell rightCell;

    public TriangleNode leftLower;
    public TriangleNode leftUpper;

    public TriangleNode rightLower;
    public TriangleNode rightUpper;
}

    private readonly Dictionary<string, RenderedSegment> segmentsByKey = new();

    [ContextMenu("Build Triangle Lines")]
    public void BuildLines()
    {
        BuildLines(grid);
    }

    public void BuildLines(TriangleGridManager sourceGrid)
    {
        grid = sourceGrid;

        ClearLines();

        if (grid == null)
        {
            Debug.LogError("TriangleLineRenderer has no TriangleGridManager.");
            return;
        }

        if (lineMaterial == null)
        {
            Debug.LogError("TriangleLineRenderer has no line material.");
            return;
        }

        PrepareRuntimeMaterial();

        int activeCount = 0;

        foreach (TriangleCell cell in grid.AllCells)
        {
            if (cell == null || !cell.isActive)
                continue;

            activeCount++;
            AddCellLineSegments(cell);
        }

        foreach (TriangleCell cell in grid.AllCells)
        {
            if (cell == null || !cell.isActive)
                continue;

            activeCount++;
            AddCellLineSegments(cell);
        }

        // if (addShrinkingProfileClosureEdges)
        //     AddShrinkingProfileClosureEdges();

        RefreshLineColors();

        Debug.Log($"Triangle lines built. Active cells: {activeCount}, Segments: {segmentsByKey.Count}");
    }

    void AddCellLineSegments(TriangleCell cell)
    {
        // Side 0: corner 0 -> midpoint 0 -> corner 1
        TryAddSegment(cell.corners[0], cell.sideMidpoints[0], cell);
        TryAddSegment(cell.sideMidpoints[0], cell.corners[1], cell);

        // Side 1: corner 1 -> midpoint 1 -> corner 2
        TryAddSegment(cell.corners[1], cell.sideMidpoints[1], cell);
        TryAddSegment(cell.sideMidpoints[1], cell.corners[2], cell);

        // Side 2: corner 2 -> midpoint 2 -> corner 0
        TryAddSegment(cell.corners[2], cell.sideMidpoints[2], cell);
        TryAddSegment(cell.sideMidpoints[2], cell.corners[0], cell);
    }

    void TryAddSegment(TriangleNode a, TriangleNode b, TriangleCell owner)
    {
        if (a == null || b == null || owner == null)
            return;

        string key = MakeSegmentKey(a, b);

        if (segmentsByKey.TryGetValue(key, out RenderedSegment existing))
        {
            if (!existing.ownerTriangles.Contains(owner))
                existing.ownerTriangles.Add(owner);

            return;
        }

        LineRenderer line = CreateLine(
            a.worldPosition + new Vector3(0f, 0f, zOffset),
            b.worldPosition + new Vector3(0f, 0f, zOffset)
        );

        RenderedSegment segment = new RenderedSegment
        {
            a = a,
            b = b,
            line = line
        };

        segment.ownerTriangles.Add(owner);
        segmentsByKey[key] = segment;
    }

    LineRenderer CreateLine(Vector3 start, Vector3 end)
    {
        GameObject lineObject = new GameObject("TriangleLineSegment");
        lineObject.transform.SetParent(transform, false);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();

        line.useWorldSpace = true;
        line.positionCount = 2;

        line.SetPosition(0, start);
        line.SetPosition(1, end);

        line.startWidth = lineWidth;
        line.endWidth = lineWidth;

        line.sharedMaterial = runtimeLineMaterial;
        line.sortingOrder = sortingOrder;

        if (!string.IsNullOrEmpty(sortingLayerName))
            line.sortingLayerName = sortingLayerName;

        line.numCapVertices = 0;
        line.numCornerVertices = 0;
        line.textureMode = LineTextureMode.Stretch;
        line.alignment = LineAlignment.TransformZ;

        return line;
    }

    public void RefreshLineColors()
    {
        foreach (RenderedSegment segment in segmentsByKey.Values)
        {
            if (segment == null || segment.line == null)
                continue;

            Color color = GetSegmentColor(segment);

            segment.line.startColor = color;
            segment.line.endColor = color;
        }
    }

    Color GetSegmentColor(RenderedSegment segment)
    {
        TriangleNodeVisualState dynamicState = GetSharedEndpointState(segment);

        if (dynamicState != TriangleNodeVisualState.None)
            return GetDynamicColor(dynamicState);

        MapRegion region = GetRegionForSegment(segment);

        if (grid != null && grid.MapDefinition != null)
            return grid.MapDefinition.GetRegionColor(region);

        return Color.black;
    }

    TriangleNodeVisualState GetSharedEndpointState(RenderedSegment segment)
    {
        TriangleNodeVisualState aState = segment.a.CurrentState;
        TriangleNodeVisualState bState = segment.b.CurrentState;

        // This matches your rule:
        // if both points of this line segment are the same color/state,
        // the whole line segment becomes that color.
        if (aState == bState && aState != TriangleNodeVisualState.None)
            return aState;

        return TriangleNodeVisualState.None;
    }

    Color GetDynamicColor(TriangleNodeVisualState state)
    {
        switch (state)
        {
            case TriangleNodeVisualState.Invalid:
                return invalidColor;

            case TriangleNodeVisualState.Footprint:
                return footprintColor;

            case TriangleNodeVisualState.Placement:
                return placementColor;

            case TriangleNodeVisualState.Hover:
                return hoverColor;

            default:
                return Color.black;
        }
    }

    MapRegion GetRegionForSegment(RenderedSegment segment)
    {
        if (segment == null)
            return MapRegion.Neutral;

        List<TriangleCell> owners = GetAllSegmentOwners(segment);

        MapRegion bestNonNeutral = MapRegion.None;
        int bestNonNeutralPriority = -1;

        MapRegion bestAny = MapRegion.None;
        int bestAnyPriority = -1;

        foreach (TriangleCell owner in owners)
        {
            if (owner == null || !owner.isActive)
                continue;

            MapRegion region = owner.region;

            if (region == MapRegion.None)
                continue;

            int priority = GetRegionPriority(region);

            if (priority > bestAnyPriority)
            {
                bestAnyPriority = priority;
                bestAny = region;
            }

            if (region != MapRegion.Neutral && priority > bestNonNeutralPriority)
            {
                bestNonNeutralPriority = priority;
                bestNonNeutral = region;
            }
        }

        if (bestNonNeutral != MapRegion.None)
            return bestNonNeutral;

        if (bestAny != MapRegion.None)
            return bestAny;

        return MapRegion.Neutral;
    }

    List<TriangleCell> GetAllSegmentOwners(RenderedSegment segment)
    {
        List<TriangleCell> result = new();

        if (segment.ownerTriangles != null)
        {
            foreach (TriangleCell owner in segment.ownerTriangles)
            {
                if (owner != null && !result.Contains(owner))
                    result.Add(owner);
            }
        }

        if (segment.a == null || segment.b == null)
            return result;

        foreach (TriangleCell candidate in segment.a.ownerTriangles)
        {
            if (candidate == null)
                continue;

            if (!candidate.isActive)
                continue;

            if (!segment.b.ownerTriangles.Contains(candidate))
                continue;

            if (!result.Contains(candidate))
                result.Add(candidate);
        }

        return result;
    }

    int GetRegionPriority(MapRegion region)
    {
        switch (region)
        {
            case MapRegion.Blocked:
                return 100;

            case MapRegion.CenterObjective:
                return 90;

            case MapRegion.WhiteStart:
            case MapRegion.BlackStart:
                return 80;

            case MapRegion.LeftObjective:
            case MapRegion.RightObjective:
                return 70;

            case MapRegion.Neutral:
                return 10;

            default:
                return 0;
        }
    }

    void PrepareRuntimeMaterial()
    {
        if (runtimeLineMaterial == null)
        {
            runtimeLineMaterial = new Material(lineMaterial);
            runtimeLineMaterial.name = $"{lineMaterial.name}_Runtime_TriangleLines";
        }

        runtimeLineMaterial.color = Color.white;
    }

    void ClearLines()
    {
        segmentsByKey.Clear();

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;

            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
    }

    string MakeSegmentKey(TriangleNode a, TriangleNode b)
    {
        int compare = string.CompareOrdinal(a.key, b.key);

        if (compare <= 0)
            return $"{a.key}|{b.key}";

        return $"{b.key}|{a.key}";
    }

    Dictionary<MapRegion, Dictionary<int, RowContour>> BuildRowContours()
    {
        Dictionary<MapRegion, Dictionary<int, List<TriangleCell>>> temp = new();

        foreach (TriangleCell cell in grid.AllCells)
        {
            if (cell == null || !cell.isActive)
                continue;

            if (cell.region == MapRegion.None)
                continue;

            if (!temp.TryGetValue(cell.region, out var regionRows))
            {
                regionRows = new Dictionary<int, List<TriangleCell>>();
                temp[cell.region] = regionRows;
            }

            int row = cell.coord.y;

            if (!regionRows.TryGetValue(row, out var rowCells))
            {
                rowCells = new List<TriangleCell>();
                regionRows[row] = rowCells;
            }

            rowCells.Add(cell);
        }

        Dictionary<MapRegion, Dictionary<int, RowContour>> result = new();

        foreach (var regionPair in temp)
        {
            MapRegion region = regionPair.Key;
            var regionRows = regionPair.Value;

            result[region] = new Dictionary<int, RowContour>();

            foreach (var rowPair in regionRows)
            {
                int row = rowPair.Key;
                List<TriangleCell> rowCells = rowPair.Value;

                if (rowCells.Count == 0)
                    continue;

                TriangleCell leftCell = rowCells[0];
                TriangleCell rightCell = rowCells[0];

                foreach (var cell in rowCells)
                {
                    if (cell.CenterPosition.x < leftCell.CenterPosition.x)
                        leftCell = cell;

                    if (cell.CenterPosition.x > rightCell.CenterPosition.x)
                        rightCell = cell;
                }

                RowContour contour = new RowContour();
                contour.hasCells = true;
                contour.leftCell = leftCell;
                contour.rightCell = rightCell;

                // LEFT side: take the two corners with the smallest X
                var leftCorners = new List<TriangleNode>(leftCell.corners);
                leftCorners.Sort((a, b) =>
                {
                    int xCompare = a.worldPosition.x.CompareTo(b.worldPosition.x);
                    if (xCompare != 0) return xCompare;
                    return a.worldPosition.y.CompareTo(b.worldPosition.y);
                });

                TriangleNode leftA = leftCorners[0];
                TriangleNode leftB = leftCorners[1];

                if (leftA.worldPosition.y < leftB.worldPosition.y)
                {
                    contour.leftLower = leftA;
                    contour.leftUpper = leftB;
                }
                else
                {
                    contour.leftLower = leftB;
                    contour.leftUpper = leftA;
                }

                // RIGHT side: take the two corners with the largest X
                var rightCorners = new List<TriangleNode>(rightCell.corners);
                rightCorners.Sort((a, b) =>
                {
                    int xCompare = b.worldPosition.x.CompareTo(a.worldPosition.x);
                    if (xCompare != 0) return xCompare;
                    return a.worldPosition.y.CompareTo(b.worldPosition.y);
                });

                TriangleNode rightA = rightCorners[0];
                TriangleNode rightB = rightCorners[1];

                if (rightA.worldPosition.y < rightB.worldPosition.y)
                {
                    contour.rightLower = rightA;
                    contour.rightUpper = rightB;
                }
                else
                {
                    contour.rightLower = rightB;
                    contour.rightUpper = rightA;
                }

                result[region][row] = contour;
            }
        }

        return result;
    }
}