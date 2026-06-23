using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class PointGridManager : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private int width = 128;
    [SerializeField] private int height = 128;
    [SerializeField] private GridPoint pointPrefab;
    [SerializeField] private GameObject pointContainerPrefab;

    [Header("Spacing")]
    [SerializeField] private float pointSpacing = 0.25f;
    [Tooltip("Spacing between rows, should be point spacing * sqrt(3)/2")]
    [SerializeField] private float rowSpacing = 0.2165f; // point spacing * sqrt(3)/2

    [Header("References")]
    [SerializeField] private PlacementController placementController;
    [SerializeField] private VisualTriangleRenderer renderTriangles;

    private Dictionary<Vector2Int, GridPoint> points = new();

    public int Width => width;
    public int Height => height;
    public float PointSpacing => pointSpacing;
    public float RowSpacing => rowSpacing;

    private void Start()
    {
        GenerateGrid();
    }

    void GenerateGrid()
    {
        points.Clear();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int coordinates = new Vector2Int(x, y);
                Vector3 worldPos = new Vector3(x * pointSpacing, y * rowSpacing, 0f);

                GridPoint point = Instantiate(pointPrefab, worldPos, Quaternion.identity, transform);

                point.transform.SetParent(pointContainerPrefab.transform, true);
                point.name = $"Point {x},{y}";
                point.Init(coordinates, placementController);

                points[coordinates] = point;
            }
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

    public Vector3 CoordinatesToWorldPosition(Vector2Int coordinates)
    {
        int x = coordinates.x;
        int y = coordinates.y;

        // Calculate the world position based on the point spacing and row spacing
        float worldX = x * pointSpacing + (y % 2 == 0 ? 0f : pointSpacing / 2f);
        float worldY = y * rowSpacing;

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

        if (!points.ContainsKey(center))
        {
            return neighborsInRange; // Return empty list if center point is not valid
        }   

        Queue<(Vector2Int coordinates, int distance)> queue = new();
        HashSet<Vector2Int> visited = new();

        queue.Enqueue((center, 0));
        visited.Add(center);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            GridPoint currentPoint = GetPointAtCoordinates(current.coordinates);
            if (currentPoint != null)// && current.distance > 0) // Exclude the center point itself
            {
                neighborsInRange.Add(currentPoint);
            }

            if (current.distance > range)
            {
                continue; // Skip further processing if the distance exceeds the range
            }   

            foreach (Vector2Int dir in GetNeighborDirections(current.coordinates))
            {
                Vector2Int neighborCoord = current.coordinates + dir;

                if (!IsInside(neighborCoord))
                {
                    continue; // Skip if the neighbor is outside the grid
                }

                if (visited.Contains(neighborCoord))
                {
                    continue; // Skip if the neighbor has already been visited
                }

                visited.Add(neighborCoord);
                queue.Enqueue((neighborCoord, current.distance + 1));
            }
        }
        return neighborsInRange;
    }

    public List<GridPoint> GetPointsInsideUnitFootprint(GridPoint centerPoint, UnitDefinition unitDef)
    {
        List<GridPoint> pointsInsideFootprint = new();

        Vector3[] unitPolygon = GetHexPolygon(
        centerPoint.WorldPosition, 
        unitDef.footprintRadius, 
        unitDef.footprintRotationDegrees
        );

        foreach (GridPoint point in GetAllPoints())
        {
            if (IsPointInsidePolygon(point.WorldPosition, unitPolygon))
            {
                pointsInsideFootprint.Add(point);
            }
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
}
