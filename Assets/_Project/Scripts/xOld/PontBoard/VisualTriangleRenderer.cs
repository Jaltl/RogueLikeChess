// using System.Collections.Generic;
// using UnityEngine;

// public class VisualTriangleRenderer : MonoBehaviour
// {
//     [Header("References")]
//     [SerializeField] private PointGridManager grid;
//     [SerializeField] private MapGenerator mapGenerator;

//     [Header("Line Visuals")]
//     [SerializeField] private Material lineMaterial;
//     [SerializeField] private float lineWidth = 0.01f;
//     [SerializeField] private int sortingOrder = 50;
//     [SerializeField] private string sortingLayerName = "Default";
//     [SerializeField] private float zOffset = -0.1f;

//     [Header("Temporary Gameplay Colors")]
//     [SerializeField] private Color defaultColor = Color.black;
//     [SerializeField] private Color placementColor = Color.yellow;
//     [SerializeField] private Color hoverColor = Color.white;
//     [SerializeField] private Color footprintColor = new Color(0.2f, 0.5f, 1f, 1f);
//     [SerializeField] private Color invalidColor = Color.red;

//     [Header("Border Bridge Fix")]
//     [SerializeField] private bool addBorderBridgeEdges = false;
//     [SerializeField] private bool addLeftBorderBridgeEdges = true;
//     [SerializeField] private bool addRightBorderBridgeEdges = true;

//     private Material runtimeLineMaterial;

//     private class RenderedEdge
//     {
//         public GridPoint a;
//         public GridPoint b;
//         public LineRenderer line;
//         public List<MapTriangle> ownerTriangles = new();

//         public bool isBridgeEdge;
//     }

//     private readonly Dictionary<string, RenderedEdge> edgesByKey = new();

//     [ContextMenu("Rebuild Triangle Visuals")]
//     public void BuildTriangles()
//     {
//         ClearLines();

//         if (grid == null)
//         {
//             Debug.LogError("VisualTriangleRenderer is missing DotGridManager.");
//             return;
//         }

//         if (mapGenerator == null)
//         {
//             Debug.LogError("VisualTriangleRenderer is missing MapGenerator.");
//             return;
//         }

//         if (lineMaterial == null)
//         {
//             Debug.LogError("VisualTriangleRenderer is missing line material.");
//             return;
//         }

//         PrepareRuntimeMaterial();

//         int activeUp = 0;
//         int activeDown = 0;

//         foreach (MapTriangle triangle in mapGenerator.Triangles)
//         {
//             if (triangle == null)
//                 continue;

//             if (!triangle.isActive)
//                 continue;

//             if (triangle.direction == TriangleDirection.Up)
//                 activeUp++;
//             else
//                 activeDown++;

//             AddTriangleEdges(triangle);
//         // }
//         // if (addBorderBridgeEdges)
//         // {
//         //     AddBorderBridgeEdges();
//         }

//         RefreshLineColors();

//         Debug.Log(
//             $"Built triangle visuals. Edges: {edgesByKey.Count}. " +
//             $"Active Up: {activeUp}, Active Down: {activeDown}"
//         );
//     }

//     void AddTriangleEdges(MapTriangle triangle)
//     {
//         if (triangle.cornerCoords == null || triangle.cornerCoords.Length < 3)
//         {
//             Debug.LogWarning($"Triangle {triangle.coord} has no valid cornerCoords.");
//             return;
//         }

//         Vector2Int a = triangle.cornerCoords[0];
//         Vector2Int b = triangle.cornerCoords[1];
//         Vector2Int c = triangle.cornerCoords[2];

//         TryAddEdge(a, b, triangle);
//         TryAddEdge(b, c, triangle);
//         TryAddEdge(c, a, triangle);
//     }

//     void TryAddEdge(Vector2Int aCoord, Vector2Int bCoord, MapTriangle ownerTriangle)
//     {
//         GridPoint a = grid.GetPoint(aCoord);
//         GridPoint b = grid.GetPoint(bCoord);

//         if (a == null || b == null)
//             return;

//         // Important:
//         // Do NOT check a.IsActive or b.IsActive here.
//         // In the triangle-first system, the active triangle is the source of truth for visuals.

//         string key = MakeEdgeKey(aCoord, bCoord);

//         if (edgesByKey.TryGetValue(key, out RenderedEdge existingEdge))
//         {
//             if (!existingEdge.ownerTriangles.Contains(ownerTriangle))
//                 existingEdge.ownerTriangles.Add(ownerTriangle);

//             return;
//         }

//         LineRenderer line = CreateLine(
//             a.WorldPosition + new Vector3(0f, 0f, zOffset),
//             b.WorldPosition + new Vector3(0f, 0f, zOffset)
//         );

//         RenderedEdge edge = new RenderedEdge
//         {
//             a = a,
//             b = b,
//             line = line
//         };

//         edge.ownerTriangles.Add(ownerTriangle);

//         edgesByKey[key] = edge;
//     }

//     LineRenderer CreateLine(Vector3 start, Vector3 end)
//     {
//         GameObject lineObject = new GameObject("VisualTriangleLine");
//         lineObject.transform.SetParent(transform, false);

//         LineRenderer line = lineObject.AddComponent<LineRenderer>();

//         line.useWorldSpace = true;
//         line.positionCount = 2;

//         line.SetPosition(0, start);
//         line.SetPosition(1, end);

//         line.startWidth = lineWidth;
//         line.endWidth = lineWidth;

//         line.sharedMaterial = runtimeLineMaterial != null
//             ? runtimeLineMaterial
//             : lineMaterial;

//         line.sortingOrder = sortingOrder;

//         if (!string.IsNullOrEmpty(sortingLayerName))
//             line.sortingLayerName = sortingLayerName;

//         line.numCapVertices = 0;
//         line.numCornerVertices = 0;
//         line.textureMode = LineTextureMode.Stretch;
//         line.alignment = LineAlignment.TransformZ;

//         return line;
//     }
    

//     public void RefreshLineColors()
//     {
//         foreach (RenderedEdge edge in edgesByKey.Values)
//         {
//             if (edge == null || edge.line == null)
//                 continue;

//             Color color = GetEdgeColor(edge);

//             edge.line.startColor = color;
//             edge.line.endColor = color;
//         }
//     }

//     Color GetEdgeColor(RenderedEdge edge)
//     {
//         GridPointVisualState aState = edge.a.CurrentVisualState;
//         GridPointVisualState bState = edge.b.CurrentVisualState;

//         // Temporary gameplay states override map colors.
//         // These require both endpoints, except Hover.
//         if (aState == GridPointVisualState.Invalid && bState == GridPointVisualState.Invalid)
//             return invalidColor;

//         if (aState == GridPointVisualState.Footprint && bState == GridPointVisualState.Footprint)
//             return footprintColor;

//         if (aState == GridPointVisualState.Placement && bState == GridPointVisualState.Placement)
//             return placementColor;

//         if (aState == GridPointVisualState.Hover || bState == GridPointVisualState.Hover)
//             return hoverColor;

//         MapRegion region = GetRegionForEdge(edge);

//         if (region != MapRegion.None && grid.MapDefinition != null)
//             return grid.MapDefinition.GetRegionColor(region);

//         return defaultColor;
//     }

//     MapRegion GetRegionForEdge(RenderedEdge edge)
//     {
//         if (edge == null || edge.ownerTriangles == null || edge.ownerTriangles.Count == 0)
//             return MapRegion.None;

//         if (edge.isBridgeEdge)
//             return GetStrictBridgeRegion(edge);

//         MapRegion bestRegion = MapRegion.None;
//         int bestPriority = -1;

//         foreach (MapTriangle triangle in edge.ownerTriangles)
//         {
//             if (triangle == null)
//                 continue;

//             if (!triangle.isActive)
//                 continue;

//             MapRegion region = triangle.region;

//             if (region == MapRegion.None)
//                 continue;

//             int priority = GetRegionPriority(region);

//             if (priority > bestPriority)
//             {
//                 bestPriority = priority;
//                 bestRegion = region;
//             }
//         }

//         return bestRegion;
//     }

//     MapRegion GetStrictBridgeRegion(RenderedEdge edge)
//     {
//         MapRegion result = MapRegion.None;

//         foreach (MapTriangle triangle in edge.ownerTriangles)
//         {
//             if (triangle == null)
//                 continue;

//             if (!triangle.isActive)
//                 continue;

//             MapRegion region = triangle.region;

//             if (region == MapRegion.None)
//                 continue;

//             // If any connected triangle is Neutral, keep the bridge neutral.
//             // This prevents a border line from accidentally taking an objective/start color.
//             if (region == MapRegion.Neutral)
//                 return MapRegion.Neutral;

//             if (result == MapRegion.None)
//             {
//                 result = region;
//                 continue;
//             }

//             // If the bridge touches mixed non-neutral regions, fall back to Neutral
//             // so the line does not look incorrectly owned by one region.
//             if (result != region)
//                 return MapRegion.Neutral;
//         }

//         return result;
//     }

//     int GetRegionPriority(MapRegion region)
//     {
//         switch (region)
//         {
//             case MapRegion.Blocked:
//                 return 100;

//             case MapRegion.CenterObjective:
//                 return 90;

//             case MapRegion.WhiteStart:
//             case MapRegion.BlackStart:
//                 return 80;

//             case MapRegion.LeftObjective:
//             case MapRegion.RightObjective:
//                 return 70;

//             case MapRegion.Neutral:
//                 return 10;

//             default:
//                 return 0;
//         }
//     }

//     void PrepareRuntimeMaterial()
//     {
//         if (runtimeLineMaterial == null)
//         {
//             runtimeLineMaterial = new Material(lineMaterial);
//             runtimeLineMaterial.name = $"{lineMaterial.name}_Runtime_LineRenderer";
//         }

//         // Important:
//         // The material color should be white so LineRenderer start/end colors are visible.
//         runtimeLineMaterial.color = Color.white;
//     }

//     void ClearLines()
//     {
//         edgesByKey.Clear();

//         for (int i = transform.childCount - 1; i >= 0; i--)
//         {
//             GameObject child = transform.GetChild(i).gameObject;

//             if (Application.isPlaying)
//                 Destroy(child);
//             else
//                 DestroyImmediate(child);
//         }
//     }

//     string MakeEdgeKey(Vector2Int a, Vector2Int b)
//     {
//         if (a.x < b.x)
//             return $"{a.x},{a.y}|{b.x},{b.y}";

//         if (a.x > b.x)
//             return $"{b.x},{b.y}|{a.x},{a.y}";

//         if (a.y < b.y)
//             return $"{a.x},{a.y}|{b.x},{b.y}";

//         return $"{b.x},{b.y}|{a.x},{a.y}";
//     }

//     void AddBorderBridgeEdges()
//     {
//         Dictionary<int, RowBoundaryTriangles> rows = BuildRowBoundaryTriangleLookup();

//         for (int row = 0; row < grid.MapDefinition.TriangleRows - 1; row++)
//         {
//             int nextRow = row + 1;

//             if (!rows.TryGetValue(row, out RowBoundaryTriangles current))
//                 continue;

//             if (!rows.TryGetValue(nextRow, out RowBoundaryTriangles next))
//                 continue;

//             if (addLeftBorderBridgeEdges)
//                 AddLeftBorderBridgeEdge(current.firstUp, next.firstUp);

//             if (addRightBorderBridgeEdges)
//                 AddRightBorderBridgeEdge(current.lastUp, next.lastUp);
//         }
//     }

//     Dictionary<int, RowBoundaryTriangles> BuildRowBoundaryTriangleLookup()
//     {
//         Dictionary<int, RowBoundaryTriangles> rows = new();

//         foreach (MapTriangle triangle in mapGenerator.Triangles)
//         {
//             if (triangle == null)
//                 continue;

//             if (!triangle.isActive)
//                 continue;

//             if (triangle.direction != TriangleDirection.Up)
//                 continue;

//             int row = triangle.coord.y;

//             if (!rows.TryGetValue(row, out RowBoundaryTriangles boundary))
//             {
//                 boundary = new RowBoundaryTriangles();
//                 rows[row] = boundary;
//             }

//             if (boundary.firstUp == null || triangle.coord.x < boundary.firstUp.coord.x)
//                 boundary.firstUp = triangle;

//             if (boundary.lastUp == null || triangle.coord.x > boundary.lastUp.coord.x)
//                 boundary.lastUp = triangle;
//         }

//         return rows;
//     }

//     void AddLeftBorderBridgeEdge(MapTriangle currentRowTriangle, MapTriangle nextRowTriangle)
//     {
//         if (!IsValidTriangle(currentRowTriangle) || !IsValidTriangle(nextRowTriangle))
//             return;

//         // Left border:
//         // bottom-left of current row first triangle
//         // → bottom-left of next row first triangle.
//         Vector2Int currentBottomLeft = currentRowTriangle.cornerCoords[0];
//         Vector2Int nextBottomLeft = nextRowTriangle.cornerCoords[0];

//         TryAddBridgeEdge(
//             currentBottomLeft,
//             nextBottomLeft,
//             currentRowTriangle,
//             nextRowTriangle
//         );
//     }

//     void AddRightBorderBridgeEdge(MapTriangle currentRowTriangle, MapTriangle nextRowTriangle)
//     {
//         if (!IsValidTriangle(currentRowTriangle) || !IsValidTriangle(nextRowTriangle))
//             return;

//         // Right border:
//         // bottom-right of current row last triangle
//         // → bottom-right of next row last triangle.
//         Vector2Int currentBottomRight = currentRowTriangle.cornerCoords[1];
//         Vector2Int nextBottomRight = nextRowTriangle.cornerCoords[1];

//         TryAddBridgeEdge(
//             currentBottomRight,
//             nextBottomRight,
//             currentRowTriangle,
//             nextRowTriangle
//         );
//     }

//     bool IsValidTriangle(MapTriangle triangle)
//     {
//         return triangle != null && triangle.cornerCoords != null && triangle.cornerCoords.Length >= 3;
//     }

//     void TryAddBridgeEdge(
//         Vector2Int aCoord,
//         Vector2Int bCoord,
//         MapTriangle ownerA,
//         MapTriangle ownerB
//     )
//     {
//         GridPoint a = grid.GetPoint(aCoord);
//         GridPoint b = grid.GetPoint(bCoord);

//         if (a == null || b == null)
//             return;

//         string key = MakeEdgeKey(aCoord, bCoord);

//         if (edgesByKey.TryGetValue(key, out RenderedEdge existingEdge))
//         {
//             existingEdge.isBridgeEdge = true;

//             if (ownerA != null && !existingEdge.ownerTriangles.Contains(ownerA))
//                 existingEdge.ownerTriangles.Add(ownerA);

//             if (ownerB != null && !existingEdge.ownerTriangles.Contains(ownerB))
//                 existingEdge.ownerTriangles.Add(ownerB);

//             return;
//         }

//         LineRenderer line = CreateLine(
//             a.WorldPosition + new Vector3(0f, 0f, zOffset),
//             b.WorldPosition + new Vector3(0f, 0f, zOffset)
//         );

//         RenderedEdge edge = new RenderedEdge
//         {
//             a = a,
//             b = b,
//             line = line,
//             isBridgeEdge = true
//         };

//         if (ownerA != null)
//             edge.ownerTriangles.Add(ownerA);

//         if (ownerB != null && ownerB != ownerA)
//             edge.ownerTriangles.Add(ownerB);

//         edgesByKey[key] = edge;
//     }

//     private class RowBoundaryTriangles
//     {
//         public MapTriangle firstUp;
//         public MapTriangle lastUp;
//     }
// }