using System.Collections.Generic;
using UnityEngine;

public class VisualTriangleRenderer : MonoBehaviour
{
    [SerializeField] private HexGridManager grid;
    [SerializeField] private Material lineMaterial;
    [SerializeField] private float lineWidth = 0.02f;
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = 50;
    [SerializeField] private float zOffset = -0.1f;

    private readonly List<GameObject> spawnedLines = new();

    public void BuildTriangles()
    {
        ClearLines();

        if (grid == null)
        {
            Debug.LogWarning("VisualTriangleRenderer: Grid is missing.");
            return;
        }

        HashSet<string> createdEdges = new();

        foreach (HexTile hex in grid.GetAllTiles())
        {
            Vector2Int from = hex.axial;

            foreach (HexTile neighbor in grid.GetNeighbors(from))
            {
                Vector2Int to = neighbor.axial;

                string edgeKey = MakeEdgeKey(from, to);
                if (createdEdges.Contains(edgeKey))
                    continue;

                createdEdges.Add(edgeKey);

                CreateLine(
                    hex.hexCenter + new Vector3(0f, 0f, zOffset),
                    neighbor.hexCenter + new Vector3(0f, 0f, zOffset)
                );
            }
        }

        Debug.Log($"Triangle line grid built. Edge count: {createdEdges.Count}");
    }

    void CreateLine(Vector3 start, Vector3 end)
    {
        GameObject lineObj = new GameObject("TriangleLine");
        lineObj.transform.SetParent(transform, false);

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);

        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;

        lr.material = lineMaterial;
        lr.sortingLayerName = sortingLayerName;
        lr.sortingOrder = sortingOrder;

        lr.numCornerVertices = 0;
        lr.numCapVertices = 0;
        lr.textureMode = LineTextureMode.Stretch;
        lr.alignment = LineAlignment.TransformZ;

        spawnedLines.Add(lineObj);
    }

    void ClearLines()
    {
        foreach (GameObject obj in spawnedLines)
        {
            if (obj != null)
                Destroy(obj);
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