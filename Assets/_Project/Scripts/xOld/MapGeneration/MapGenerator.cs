// using System.Collections.Generic;
// using UnityEngine;

// public class MapGenerator : MonoBehaviour
// {
//     [SerializeField] private PointGridManager grid;

//     private readonly List<MapTriangle> triangles = new();

//     public IReadOnlyList<MapTriangle> Triangles => triangles;

//     public void GenerateMap(MapDefinition mapDefinition)
//     {
//         if (grid == null)
//         {
//             Debug.LogError("MapGenerator has no DotGridManager assigned.");
//             return;
//         }

//         if (mapDefinition == null)
//         {
//             Debug.LogError("MapGenerator has no MapDefinition.");
//             return;
//         }

//         ResetPoints();
//         BuildTriangleLayer(mapDefinition);
//         ApplyStamps(mapDefinition);
//         ActivatePointsFromTriangles();

//         Debug.Log($"Generated map triangles: {triangles.Count}");
//         //DebugTriangleCounts();
//     }

//     void DebugTriangleCounts()
// {
//     int total = 0;
//     int active = 0;
//     int activeUp = 0;
//     int activeDown = 0;

//     int neutral = 0;
//     int white = 0;
//     int black = 0;
//     int left = 0;
//     int right = 0;
//     int center = 0;
//     int blocked = 0;

//     foreach (MapTriangle triangle in triangles)
//     {
//         total++;

//         if (!triangle.isActive)
//             continue;

//         active++;

//         if (triangle.direction == TriangleDirection.Up)
//             activeUp++;
//         else
//             activeDown++;

//         switch (triangle.region)
//         {
//             case MapRegion.Neutral:
//                 neutral++;
//                 break;

//             case MapRegion.WhiteStart:
//                 white++;
//                 break;

//             case MapRegion.BlackStart:
//                 black++;
//                 break;

//             case MapRegion.LeftObjective:
//                 left++;
//                 break;

//             case MapRegion.RightObjective:
//                 right++;
//                 break;

//             case MapRegion.CenterObjective:
//                 center++;
//                 break;

//             case MapRegion.Blocked:
//                 blocked++;
//                 break;
//         }
//     }

//     // Debug.Log(
//     //     $"Map triangles total: {total}, active: {active}, " +
//     //     $"active up: {activeUp}, active down: {activeDown}, " +
//     //     $"neutral: {neutral}, white: {white}, black: {black}, " +
//     //     $"left: {left}, right: {right}, center: {center}, blocked: {blocked}"
//     // );
// }

//     void ResetPoints()
//     {
//         foreach (GridPoint point in grid.GetAllPoints())
//         {
//             point.SetMapData(false, MapRegion.None, false);
//         }
//     }

// void BuildTriangleLayer(MapDefinition map)
// {
//     triangles.Clear();

//     for (int row = 0; row < map.TriangleRows; row++)
//     {
//         for (int col = 0; col < map.TriangleColumns; col++)
//         {
//             Vector2Int triangleCoord = new Vector2Int(col, row);

//             TriangleDirection direction = GetDirectionForColumn(col, map);

//             TryCreateTriangle(triangleCoord, direction, map);
//         }
//     }

//     DebugFirstTriangles();
// }

// TriangleDirection GetDirectionForColumn(int col, MapDefinition map)
// {
//     // Start and end rows with Down triangles.
//     // col 0 = Down, col 1 = Up, col 2 = Down...
//     if (map.startRowsWithDownTriangle)
//         return col % 2 == 0 ? TriangleDirection.Down : TriangleDirection.Up;

//     return col % 2 == 0 ? TriangleDirection.Up : TriangleDirection.Down;
// }

//     void DebugFirstTriangles()
//     {
//         int count = Mathf.Min(20, triangles.Count);

//         for (int i = 0; i < count; i++)
//         {
//             MapTriangle t = triangles[i];

//             string corners = "null";

//             if (t.cornerCoords != null && t.cornerCoords.Length >= 3)
//             {
//                 corners =
//                     $"{t.cornerCoords[0]} / {t.cornerCoords[1]} / {t.cornerCoords[2]}";
//             }

//             Debug.Log(
//                 $"Triangle {i}: coord={t.coord}, direction={t.direction}, corners={corners}"
//             );
//         }
//     }

//     void TryCreateTriangle(Vector2Int coord, TriangleDirection direction, MapDefinition map)
//     {
//         Vector2Int[] corners = GetTriangleCornerCoords(coord, direction, map);

//         foreach (Vector2Int corner in corners)
//         {
//             if (!grid.IsInside(corner))
//                 return;
//         }

//         MapTriangle triangle = new MapTriangle
//         {
//             coord = coord,
//             direction = direction,
//             region = MapRegion.None,
//             isActive = false,
//             isBlockedTerrain = false,
//             cornerCoords = corners
//         };

//         triangles.Add(triangle);
//     }

//     Vector2Int[] GetTriangleCornerCoords(Vector2Int triangleCoord, TriangleDirection direction, MapDefinition map)
// {
//     int widthSteps = map.WidthSteps;
//     int heightSteps = map.HeightSteps;
//     int halfWidthSteps = map.HalfWidthSteps;

//     int row = triangleCoord.y;
//     int col = triangleCoord.x;

//     int baseY = row * heightSteps;
//     int topY = baseY + heightSteps;

//     // Important:
//     // Each visual triangle slot moves half a triangle width.
//     int x = col * halfWidthSteps;

//     if (direction == TriangleDirection.Up)
//     {
//         return new[]
//         {
//             new Vector2Int(x, baseY),
//             new Vector2Int(x + widthSteps, baseY),
//             new Vector2Int(x + halfWidthSteps, topY)
//         };
//     }

//     // Down triangle.
//     return new[]
//     {
//         new Vector2Int(x, topY),
//         new Vector2Int(x + widthSteps, topY),
//         new Vector2Int(x + halfWidthSteps, baseY)
//     };
// }

//     void ApplyStamps(MapDefinition map)
// {
//     foreach (MapStamp stamp in map.stamps)
//     {
//         if (stamp == null)
//         {
//             Debug.LogWarning("Stamp is null.");
//             continue;
//         }

//         if (!stamp.enabled)
//         {
//             Debug.Log($"Stamp disabled: {stamp.name}");
//             continue;
//         }

//         int matches = 0;
//         int activated = 0;

//         // Debug.Log(
//         //     $"Stamp '{stamp.name}' debug: " +
//         //     $"enabled={stamp.enabled}, " +
//         //     $"mode={stamp.mode}, " +
//         //     $"region={stamp.region}, " +
//         //     $"centerColumn={stamp.centerColumn}, " +
//         //     $"profileKeys={stamp.profileKeys.Count}, " +
//         //     $"map triangle columns={map.TriangleColumns}, " +
//         //     $"map triangle rows={map.TriangleRows}, " +
//         //     $"map center column={map.CenterColumn}, " +
//         //     $"map center row={map.CenterRow}"
//         // );

//         // foreach (ProfileKey key in stamp.profileKeys)
//         // {
//         //     Debug.Log($"  Key row={key.row}, width={key.width}");
//         // }

//         foreach (MapTriangle triangle in triangles)
//         {
//             if (!stamp.ContainsTriangle(triangle, map))
//                 continue;

//             matches++;

//             bool wasActive = triangle.isActive;
//             //stamp.DebugProfile(map);
//             ApplyStampToTriangle(stamp, triangle);

//             if (!wasActive && triangle.isActive)
//                 activated++;
//         }

//         Debug.Log(
//             $"Stamp '{stamp.name}' matched {matches} triangles, activated {activated}. " +
//             $"Mode: {stamp.mode}, Region: {stamp.region}"
//         );
//     }
// }

//     void ApplyStampToTriangle(MapStamp stamp, MapTriangle triangle)
//     {
//         switch (stamp.mode)
//         {
//             case MapStampMode.Add:
//             {
//                 triangle.isActive = true;
//                 triangle.region = stamp.region == MapRegion.None
//                     ? MapRegion.Neutral
//                     : stamp.region;

//                 triangle.isBlockedTerrain = false;
//                 break;
//             }

//             case MapStampMode.SetRegion:
//             {
//                 if (!triangle.isActive)
//                     return;

//                 triangle.region = stamp.region == MapRegion.None
//                     ? MapRegion.Neutral
//                     : stamp.region;

//                 break;
//             }

//             case MapStampMode.Remove:
//             {
//                 triangle.isActive = false;
//                 triangle.region = MapRegion.None;
//                 triangle.isBlockedTerrain = false;
//                 break;
//             }

//             case MapStampMode.BlockTerrain:
//             {
//                 if (!triangle.isActive)
//                     return;

//                 triangle.region = MapRegion.Blocked;
//                 triangle.isBlockedTerrain = true;
//                 break;
//             }
//         }
//     }

// void ActivatePointsFromTriangles()
// {
//     foreach (MapTriangle triangle in triangles)
//     {
//         if (!triangle.isActive)
//             continue;

//         List<GridPoint> pointsInsideTriangle = GetPointsInsideOrOnTriangle(triangle);

//         triangle.points = pointsInsideTriangle;

//         foreach (GridPoint point in pointsInsideTriangle)
//         {
//             point.SetMapData(
//                 true,
//                 triangle.region,
//                 triangle.isBlockedTerrain
//             );
//         }
//     }
// }

// List<GridPoint> GetPointsInsideOrOnTriangle(MapTriangle triangle)
// {
//     List<GridPoint> result = new();

//     GridPoint a = grid.GetPoint(triangle.cornerCoords[0]);
//     GridPoint b = grid.GetPoint(triangle.cornerCoords[1]);
//     GridPoint c = grid.GetPoint(triangle.cornerCoords[2]);

//     if (a == null || b == null || c == null)
//         return result;

//     // Always include the corners.
//     AddPointIfValid(result, a);
//     AddPointIfValid(result, b);
//     AddPointIfValid(result, c);

//     Vector3 av = a.WorldPosition;
//     Vector3 bv = b.WorldPosition;
//     Vector3 cv = c.WorldPosition;

//     foreach (GridPoint point in grid.GetAllPoints())
//     {
//         if (point == null)
//             continue;

//         if (PointInTriangleInclusive(point.WorldPosition, av, bv, cv))
//         {
//             AddPointIfValid(result, point);
//         }
//     }

//     return result;
// }

// void AddPointIfValid(List<GridPoint> list, GridPoint point)
// {
//     if (point == null)
//         return;

//     if (!list.Contains(point))
//         list.Add(point);
// }  

// bool PointInTriangleInclusive(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
// {
//     const float epsilon = 0.0001f;

//     float d1 = Sign(p, a, b);
//     float d2 = Sign(p, b, c);
//     float d3 = Sign(p, c, a);

//     bool hasNegative = d1 < -epsilon || d2 < -epsilon || d3 < -epsilon;
//     bool hasPositive = d1 > epsilon || d2 > epsilon || d3 > epsilon;

//     return !(hasNegative && hasPositive);
// }

// float Sign(Vector3 p1, Vector3 p2, Vector3 p3)
// {
//     return (p1.x - p3.x) * (p2.y - p3.y) -
//            (p2.x - p3.x) * (p1.y - p3.y);
// }
//     bool PointInPolygon(Vector3 point, Vector3[] polygon)
//     {
//         bool inside = false;

//         for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
//         {
//             bool intersects =
//                 ((polygon[i].y > point.y) != (polygon[j].y > point.y)) &&
//                 (point.x < (polygon[j].x - polygon[i].x) *
//                 (point.y - polygon[i].y) /
//                 (polygon[j].y - polygon[i].y) + polygon[i].x);

//             if (intersects)
//                 inside = !inside;
//         }

//         return inside;
//     }
// }