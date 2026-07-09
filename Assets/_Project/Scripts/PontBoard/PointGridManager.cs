using UnityEngine;
using System.Collections.Generic;

public class PointGridManager : MonoBehaviour
{
    [Header("Map")]
    [SerializeField] private MapDefinition mapDefinition;

    [Header("Prefabs")]
    [SerializeField] private GridPoint pointPrefab;

    [Header("References")]
    [SerializeField] private PlacementController placementController;
    [SerializeField] private VisualTriangleRenderer renderTriangles;
    [SerializeField] private MapGenerator mapGenerator;

    private Dictionary<Vector2Int, GridPoint> points = new();

    public MapDefinition MapDefinition => mapDefinition;

    public int Width => mapDefinition != null ? mapDefinition.PointWidth : 0;
    public int Height => mapDefinition != null ? mapDefinition.PointHeight : 0;

    public float PointSpacing => mapDefinition != null ? mapDefinition.pointSpacing : 0f;
    public float RowSpacing => mapDefinition != null ? mapDefinition.RowSpacing : 0f;

    private void Start()
    {
        GenerateGrid();
    }

    void GenerateGrid()
    {

        if (mapDefinition == null)
        {
            Debug.LogError("MapDefinition is not assigned in PointGridManager.");
            return;
        }

        ClearExistingGrid();
        points.Clear();

        for (int y = 0; y < mapDefinition.PointHeight; y++)
        {
            for (int x = 0; x < mapDefinition.PointWidth; x++)
            {
                Vector2Int coordinates = new Vector2Int(x, y);
                Vector3 worldPos = CoordinatesToWorldPosition(coordinates);

                GridPoint point = Instantiate(pointPrefab, worldPos, Quaternion.identity, transform);

                point.name = $"Point {x},{y}";
                point.Init(coordinates, placementController);

                points[coordinates] = point;
            }
        }

        if (mapGenerator != null)
        {
            mapGenerator.GenerateMap(mapDefinition);
        }

        if (renderTriangles != null)
        {
            renderTriangles.BuildTriangles();
        }

        if (placementController != null)
        {
            placementController.InitializePlacement();
        }
    }

    private void ClearExistingGrid()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }

    public Vector3 CoordinatesToWorldPosition(Vector2Int coordinates)
    {
        float worldX = coordinates.x * PointSpacing;
        float worldY = coordinates.y * RowSpacing;

        return new Vector3(worldX, worldY, 0f);
    }

    public GridPoint GetPointAtCoordinates(Vector2Int coordinates)
    {
        points.TryGetValue(coordinates, out GridPoint point);
        return point;
    }

    public GridPoint GetPoint(Vector2Int coord)
    {
        points.TryGetValue(coord, out GridPoint point);
        return point;
    }

    public IEnumerable<GridPoint> GetAllPoints()
    {
        return points.Values;
    }

    public bool IsInside(Vector2Int coordinates)
    {
        return points.ContainsKey(coordinates);
    }

    public Vector2Int[] GetNeighborDirections(Vector2Int coordinates)
    {
        return coordinates.y % 2 == 0 ? EvenRowDirections : OddRowDirections;
    }   

    private static readonly Vector2Int[] EvenRowDirections = new Vector2Int[]
    {
        new Vector2Int(1, 0),   // East
        new Vector2Int(0, 1),   // North-East
        new Vector2Int(-1, 1),  // North-West
        new Vector2Int(-1, 0),  // West
        new Vector2Int(-1, -1), // South-West
        new Vector2Int(0, -1)   // South-East
    };

    private static readonly Vector2Int[] OddRowDirections = new Vector2Int[]
    {
        new Vector2Int(1, 0),   // East
        new Vector2Int(1, 1),   // North-East
        new Vector2Int(0, 1),   // North-West
        new Vector2Int(-1, 0),  // West
        new Vector2Int(0, -1),  // South-West
        new Vector2Int(1, -1)   // South-East
    };

    public List<GridPoint> GetNeighbors(Vector2Int coordinates)
    {
        List<GridPoint> neighbors = new();

        foreach (Vector2Int dir in GetNeighborDirections(coordinates))
        {
            Vector2Int neighborCoord = coordinates + dir;
            if (points.TryGetValue(neighborCoord, out GridPoint neighborPoint))
            {
                neighbors.Add(neighborPoint);
            }
        }

        return neighbors;
    }

    public List<GridPoint> GetNeighborsInRange(Vector2Int center, int range)
    {
        List<GridPoint> neighborsInRange = new();

        foreach (GridPoint point in GetAllPoints())
        {
            if (!point.IsActive)
                continue;

            int dx = Mathf.Abs(point.coordinates.x - center.x);
            int dy = Mathf.Abs(point.coordinates.y - center.y);

            if (dx + dy <= range)
                neighborsInRange.Add(point);
        }

        return neighborsInRange;
    }

    public List<GridPoint> GetPointsInsideUnitFootprint(GridPoint centerPoint, UnitDefinition unitDef)
    {
        List<GridPoint> pointsInsideFootprint = new();

        if (centerPoint == null || unitDef == null)
            return pointsInsideFootprint;

        // Vector3[] unitPolygon = GetHexPolygon(
        //     centerPoint.WorldPosition, 
        //     unitDef.footprintRadius, 
        //     unitDef.footprintRotationDegrees
        // );

        foreach (GridPoint point in GetAllPoints())
        {
            if (!point.IsActive)
                continue;

            // if (IsPointInsidePolygon(point.WorldPosition, unitPolygon))
            //     pointsInsideFootprint.Add(point);
        }

        return pointsInsideFootprint;
    }

    private Vector3[] GetHexPolygon(Vector3 center, float radius, float rotationDegrees)
    {
        Vector3[] vertices = new Vector3[6];

        for (int i = 0; i < 6; i++)
        {
            float angle = Mathf.Deg2Rad * (rotationDegrees + 60f * i);

            vertices[i] = new Vector3(
                radius * Mathf.Cos(angle),
                radius * Mathf.Sin(angle),
                0f
            );
        }

        return vertices;
    }

    private bool IsPointInsidePolygon(Vector3 point, Vector3[] polygon)
    {
        bool inside = false;

        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {

            if (((polygon[i].y > point.y) != (polygon[j].y > point.y)) &&
                (point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x))
            {
                inside = !inside;
            }
        }

        return inside;
    }

    [ContextMenu("Regenerate Grid From Map Definition")]
    public void RegenerateGridFromMapDefinition()
    {
        GenerateGrid();
    }
}
