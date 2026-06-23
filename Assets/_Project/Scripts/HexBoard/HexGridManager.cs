using System.Collections.Generic;
using UnityEngine;

public class HexGridManager : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private int width = 128;
    [SerializeField] private int height = 128;
    [SerializeField] private HexTile hexPrefab;

    [Header("References")]
    [SerializeField] private PlacementController placementController;
    [SerializeField] private VisualTriangleRenderer renderTriangles;

    [Header("Hex Shape")]
    //[SerializeField] private float baseHexRadius = 0.125f;
    [SerializeField] private float baseHexRotationDegrees = 0f;

    [Header("Grid Scale")]
    [SerializeField] private float cellRadius = 0.125f;
    [SerializeField] private float prefabRadiusAtScaleOne = 0.5f;

    [Header("Placement Footprint")]
    [SerializeField] private float footprintPadding = 0.01f;
    public float CellRadius => cellRadius;

    private Dictionary<Vector2Int, HexTile> tiles = new();

    private const float hexHeight = 0.8660254f; // sqrt(3)/2

    public int Width => width;
    public int Height => height;

    private void Start()
    {
        GenerateGrid();
    }

    void GenerateGrid()
    {
        tiles.Clear();

        for (int q = 0; q < width; q++)
        {
            for (int r = 0; r < height; r++)
            {
                Vector2Int axial = new Vector2Int(q, r);
                Vector3 worldPos = AxialToWorld(axial);

                HexTile tile = Instantiate(hexPrefab, worldPos, Quaternion.identity, transform);

                float visualScale = cellRadius / prefabRadiusAtScaleOne;
                tile.transform.localScale = Vector3.one * visualScale;

                tile.name = $"Hex {q},{r}";
                tile.Init(axial, placementController);

                tiles[axial] = tile;
            }
        }

        if (renderTriangles != null)
            renderTriangles.BuildTriangles();

        if (placementController != null)
            placementController.InitializePlacement();
    }

    public HexTile GetTile(Vector2Int axial)
    {
        tiles.TryGetValue(axial, out HexTile tile);
        return tile;
    }

    public IEnumerable<HexTile> GetAllTiles()
    {
        return tiles.Values;
    }

    public bool IsInside(Vector2Int axial)
    {
        return tiles.ContainsKey(axial);
    }

    public Vector3 AxialToWorld(Vector2Int axial)
    {
        int q = axial.x;
        int r = axial.y;

        float hexWidth = cellRadius * 2f;
        float hexHeight = Mathf.Sqrt(3f) * cellRadius;

        float x = q * hexWidth * 0.75f;
        float y = r * hexHeight + (q % 2 == 0 ? 0f : hexHeight / 2f);

        return new Vector3(x, y, 0f);
    }

    public List<HexTile> GetHexesInRange(Vector2Int center, int range)
    {
        List<HexTile> result = new();

        if (!tiles.TryGetValue(center, out HexTile start))
            return result;

        Queue<(Vector2Int coord, int distance)> queue = new();
        HashSet<Vector2Int> visited = new();

        queue.Enqueue((center, 0));
        visited.Add(center);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            HexTile currentTile = GetTile(current.coord);
            if (currentTile != null)
                result.Add(currentTile);

            if (current.distance >= range)
                continue;

            foreach (Vector2Int dir in GetNeighborDirections(current.coord))
            {
                Vector2Int next = current.coord + dir;

                if (!IsInside(next))
                    continue;

                if (visited.Contains(next))
                    continue;

                visited.Add(next);
                queue.Enqueue((next, current.distance + 1));
            }
        }

        return result;
    }

    public int HexDistance(Vector2Int a, Vector2Int b)
    {
        int aq = a.x;
        int ar = a.y;
        int as_ = -aq - ar;

        int bq = b.x;
        int br = b.y;
        int bs = -bq - br;

        return Mathf.Max(
            Mathf.Abs(aq - bq),
            Mathf.Abs(ar - br),
            Mathf.Abs(as_ - bs)
        );
    }

    public List<HexTile> GetHexesOverlappedByUnit(HexTile anchorHex, UnitDefinition unitDef)
    {
        List<HexTile> result = new();

        Vector3[] unitPolygon = GetHexPolygon(
            anchorHex.hexCenter,
            unitDef.footprintRadius + footprintPadding,
            unitDef.footprintRotationDegrees
        );

        foreach (HexTile tile in GetAllTiles())
        {
            Vector3[] tilePolygon = GetHexPolygon(
                tile.hexCenter,
                cellRadius,
                baseHexRotationDegrees
            );

            if (PolygonsOverlap(unitPolygon, tilePolygon))
                result.Add(tile);
        }

        return result;
    }

    private Vector3[] GetHexPolygon(Vector3 center, float radius, float rotationDegrees)
    {
        Vector3[] points = new Vector3[6];

        for (int i = 0; i < 6; i++)
        {
            float angle = Mathf.Deg2Rad * (rotationDegrees + 60f * i);

            points[i] = center + new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                0f
            );
        }

        return points;
    }

    private bool PolygonsOverlap(Vector3[] a, Vector3[] b)
    {
        foreach (Vector3 p in a)
        {
            if (PointInPolygon(p, b))
                return true;
        }

        foreach (Vector3 p in b)
        {
            if (PointInPolygon(p, a))
                return true;
        }

        for (int i = 0; i < a.Length; i++)
        {
            Vector3 a1 = a[i];
            Vector3 a2 = a[(i + 1) % a.Length];

            for (int j = 0; j < b.Length; j++)
            {
                Vector3 b1 = b[j];
                Vector3 b2 = b[(j + 1) % b.Length];

                if (LinesIntersect(a1, a2, b1, b2))
                    return true;
            }
        }

        return false;
    }

    private bool PointInPolygon(Vector3 point, Vector3[] polygon)
    {
        bool inside = false;

        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            bool intersects =
                ((polygon[i].y > point.y) != (polygon[j].y > point.y)) &&
                (point.x < (polygon[j].x - polygon[i].x) *
                (point.y - polygon[i].y) /
                (polygon[j].y - polygon[i].y) + polygon[i].x);

            if (intersects)
                inside = !inside;
        }

        return inside;
    }

    private bool LinesIntersect(Vector3 a1, Vector3 a2, Vector3 b1, Vector3 b2)
    {
        float d =
            (a2.x - a1.x) * (b2.y - b1.y) -
            (a2.y - a1.y) * (b2.x - b1.x);

        if (Mathf.Approximately(d, 0f))
            return false;

        float u =
            ((b1.x - a1.x) * (b2.y - b1.y) -
             (b1.y - a1.y) * (b2.x - b1.x)) / d;

        float v =
            ((b1.x - a1.x) * (a2.y - a1.y) -
             (b1.y - a1.y) * (a2.x - a1.x)) / d;

        return u >= 0f && u <= 1f && v >= 0f && v <= 1f;
    }

    private static readonly Vector2Int[] EvenColumnDirections =
    {
        new Vector2Int(1, 0),   // upper-right
        new Vector2Int(0, 1),   // up
        new Vector2Int(-1, 0),  // upper-left
        new Vector2Int(-1, -1), // lower-left
        new Vector2Int(0, -1),  // down
        new Vector2Int(1, -1),  // lower-right
    };

    private static readonly Vector2Int[] OddColumnDirections =
    {
        new Vector2Int(1, 1),   // upper-right
        new Vector2Int(0, 1),   // up
        new Vector2Int(-1, 1),  // upper-left
        new Vector2Int(-1, 0),  // lower-left
        new Vector2Int(0, -1),  // down
        new Vector2Int(1, 0),   // lower-right
    };

    public Vector2Int[] GetNeighborDirections(Vector2Int coord)
    {
        return coord.x % 2 == 0
            ? EvenColumnDirections
            : OddColumnDirections;
    }

    public List<HexTile> GetNeighbors(Vector2Int coord)
    {
        List<HexTile> result = new();

        Vector2Int[] dirs = GetNeighborDirections(coord);

        foreach (Vector2Int dir in dirs)
        {
            Vector2Int neighborCoord = coord + dir;

            if (tiles.TryGetValue(neighborCoord, out HexTile tile))
                result.Add(tile);
        }

        return result;
    }
}