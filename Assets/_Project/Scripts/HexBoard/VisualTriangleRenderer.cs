using System.Collections.Generic;
using UnityEngine;

public class VisualTriangleRenderer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PointGridManager grid;

    [Header("Line Visuals")]
    [SerializeField] private Material lineMaterial;
    [SerializeField] private float lineWidth = 0.01f;
    [SerializeField] private int sortingOrder = 50;
    [SerializeField] private float zOffset = -0.1f;

    [Header("Triangle Size in Points")]
    [SerializeField] private int trianglePointWidth = 5;
    [SerializeField] private int trianglePointHeight = 7;

    private readonly List<GameObject> spawnedLines = new();

    public void BuildTriangles()
    {
        ClearLines();

        if (grid == null)
        {
            Debug.LogWarning("VisualTriangleRenderer has no DotGridManager assigned.");
            return;
        }

        if (lineMaterial == null)
        {
            Debug.LogWarning("VisualTriangleRenderer has no line material assigned.");
            return;
        }

        int widthSteps = trianglePointWidth - 1;      // 5 points wide = 4 spaces
        int heightSteps = trianglePointHeight - 1;    // 7 points tall = 6 spaces

        if (widthSteps <= 0 || heightSteps <= 0)
        {
            Debug.LogWarning("Triangle size must be at least 2 points wide and 2 points tall.");
            return;
        }

        HashSet<string> createdEdges = new();

        int bandIndex = 0;
        int halfWidth = widthSteps / 2;

        int lastTopY = 0;
        int lastXOffset = 0;

        for (int baseY = 0; baseY + heightSteps < grid.Height; baseY += heightSteps)
        {
            int xOffset = bandIndex % 2 == 0 ? 0 : halfWidth;
            int topY = baseY + heightSteps;

            BuildUpTriangleBand(baseY, xOffset, widthSteps, heightSteps, createdEdges);

            // Draw the horizontal row line for the base.
            DrawHorizontalBandLine(baseY, xOffset, widthSteps, createdEdges);
            
            //add the one missing outer diagonal for this band
            DrawBandEdgeCap(baseY, topY, xOffset, widthSteps, createdEdges);

            lastTopY = topY;
            lastXOffset = xOffset;

            bandIndex++;
        }

        // Close the very last row of triangle tops horizontally.
        DrawHorizontalBandLine(lastTopY, lastXOffset + halfWidth, widthSteps, createdEdges);

        Debug.Log($"Built visual triangle grid. Lines: {spawnedLines.Count}");
    }

    void BuildUpTriangleBand(int baseY, int xOffset, int widthSteps, int heightSteps, HashSet<string> createdEdges)
    {
        int topY = baseY + heightSteps;
        int halfWidth = widthSteps / 2;

        for (int x = xOffset; x + widthSteps < grid.Width; x += widthSteps)
        {
            Vector2Int bottomLeft = new Vector2Int(x, baseY);
            Vector2Int bottomRight = new Vector2Int(x + widthSteps, baseY);
            Vector2Int top = new Vector2Int(x + halfWidth, topY);

            TryAddEdge(bottomLeft, bottomRight, createdEdges);
            TryAddEdge(bottomLeft, top, createdEdges);
            TryAddEdge(bottomRight, top, createdEdges);
        }
    }

    void DrawHorizontalBandLine(int y, int xOffset, int widthSteps, HashSet<string> createdEdges)
    {
        for (int x = xOffset; x + widthSteps < grid.Width; x += widthSteps)
        {
            Vector2Int left = new Vector2Int(x, y);
            Vector2Int right = new Vector2Int(x + widthSteps, y);

            TryAddEdge(left, right, createdEdges);
        }
    }

    void DrawBandEdgeCap(
    int baseY,
    int topY,
    int xOffset,
    int widthSteps,
    HashSet<string> createdEdges)
{
    int halfWidth = widthSteps / 2;

    // Non-offset row:
    // the last downward triangle on the RIGHT is missing one side.
    if (xOffset == 0)
    {
        int lastTopStart = -1;

        for (int x = xOffset + halfWidth; x + widthSteps < grid.Width; x += widthSteps)
        {
            lastTopStart = x;
        }

        if (lastTopStart != -1)
        {
            Vector2Int topOuterRight = new Vector2Int(lastTopStart + widthSteps, topY);
            Vector2Int bottomApex = new Vector2Int(lastTopStart + halfWidth, baseY);

            TryAddEdge(topOuterRight, bottomApex, createdEdges);
        }
    }
    // Offset row:
    // the first downward triangle on the LEFT is missing one side.
    else
    {
        int firstTopStart = xOffset - halfWidth;

        Vector2Int topOuterLeft = new Vector2Int(firstTopStart, topY);
        Vector2Int bottomApex = new Vector2Int(xOffset, baseY);

        TryAddEdge(topOuterLeft, bottomApex, createdEdges);
    }
}

    private void TryAddTriangle(
        Vector2Int aCoord,
        Vector2Int bCoord,
        Vector2Int cCoord,
        HashSet<string> createdEdges)
    {
        GridPoint a = grid.GetPoint(aCoord);
        GridPoint b = grid.GetPoint(bCoord);
        GridPoint c = grid.GetPoint(cCoord);

        if (a == null || b == null || c == null)
            return;

        AddEdge(a, b, createdEdges);
        AddEdge(b, c, createdEdges);
        AddEdge(c, a, createdEdges);
    }

    void TryAddEdge(Vector2Int aCoord, Vector2Int bCoord, HashSet<string> createdEdges)
    {
        GridPoint a = grid.GetPoint(aCoord);
        GridPoint b = grid.GetPoint(bCoord);

        if (a == null || b == null)
            return;

        AddEdge(a, b, createdEdges);
    }

    private void AddEdge(GridPoint gridPointA, GridPoint gridPointB, HashSet<string> createdEdges)
    {
        string edgeKey = MakeEdgeKey(gridPointA.coordinates, gridPointB.coordinates);

        if (createdEdges.Contains(edgeKey))
            return;

        createdEdges.Add(edgeKey);

        CreateLine(gridPointA.WorldPosition + new Vector3(0f, 0f, zOffset), 
        gridPointB.WorldPosition + new Vector3(0f, 0f, zOffset));
        
    }

    void CreateLine(Vector3 start, Vector3 end)
    {
        GameObject lineObj = new GameObject("VisualTriangleLine");
        lineObj.transform.SetParent(transform, false);

        LineRenderer line = lineObj.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        
        line.SetPosition(0, start);
        line.SetPosition(1, end);

        line.startWidth = lineWidth;
        line.endWidth = lineWidth;

        line.material = lineMaterial;
        line.sortingOrder = sortingOrder;

        line.numCornerVertices = 0;
        line.numCapVertices = 0;
        line.alignment = LineAlignment.TransformZ;

        spawnedLines.Add(lineObj);
    }

    void ClearLines()
    {
        foreach (GameObject lineObj in spawnedLines)
        {
            if (lineObj != null)
                Destroy(lineObj);
        }

        spawnedLines.Clear();
    }

    string MakeEdgeKey(Vector2Int a, Vector2Int b)
    {
        if (a.x < b.x) return $"{a.x},{a.y}|{b.x},{b.y}";
        if (a.x > b.x) return $"{b.x},{b.y}|{a.x},{a.y}";
        if (a.y < b.y) return $"{a.x},{a.y}|{b.x},{b.y}";
        return $"{b.x},{b.y}|{a.x},{a.y}";
    }
}