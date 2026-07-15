// using System.Collections.Generic;
// using UnityEngine;

// [System.Serializable]
// public class ProfileKey
// {
//     public int row;
//     public int width;
// }

// [System.Serializable]
// public class MapStamp
// {
//     public bool enabled = true;
//     public string name;

//     [Header("Operation")]
//     public MapStampMode mode = MapStampMode.Add;
//     public MapRegion region = MapRegion.Neutral;
//     public MapMirrorMode mirrorMode = MapMirrorMode.None;

//     [Header("Shape")]
//     public MapShapeType shapeType = MapShapeType.KeyframedProfile;

//     [Header("Keyframed Profile")]
//     public int centerColumn = 15;
//     public List<ProfileKey> profileKeys = new();

//     public bool ContainsTriangle(MapTriangle triangle, MapDefinition map)
//     {
//         if (!enabled || triangle == null || map == null)
//             return false;

//         Vector2Int coord = triangle.coord;

//         if (ContainsProfile(coord))
//             return true;

//         if (mirrorMode == MapMirrorMode.Vertical || mirrorMode == MapMirrorMode.Both)
//         {
//             Vector2Int mirrored = MirrorVertical(coord, map);

//             if (ContainsProfile(mirrored))
//                 return true;
//         }

//         if (mirrorMode == MapMirrorMode.Horizontal || mirrorMode == MapMirrorMode.Both)
//         {
//             Vector2Int mirrored = MirrorHorizontal(coord, map);

//             if (ContainsProfile(mirrored))
//                 return true;
//         }

//         if (mirrorMode == MapMirrorMode.Both)
//         {
//             Vector2Int mirrored = MirrorVertical(MirrorHorizontal(coord, map), map);

//             if (ContainsProfile(mirrored))
//                 return true;
//         }

//         return false;
//     }

// Vector2Int MirrorVerticalProfile(Vector2Int profileCoord, MapDefinition map)
// {
//     return new Vector2Int(
//         profileCoord.x,
//         map.TriangleRows - 1 - profileCoord.y
//     );
// }

// Vector2Int MirrorHorizontalProfile(Vector2Int profileCoord, MapDefinition map)
// {
//     return new Vector2Int(
//         map.ProfileColumns - 1 - profileCoord.x,
//         profileCoord.y
//     );
// }

// public bool ContainsProfile(Vector2Int profileCoord)
// {
//     int width = GetWidthAtRow(profileCoord.y);

//     if (width <= 0)
//         return false;

//     int left = centerColumn - width / 2;
//     int right = left + width - 1;

//     return profileCoord.x >= left && profileCoord.x <= right;
// }

//     bool ContainsCoord(Vector2Int coord)
//     {
//         switch (shapeType)
//         {
//             case MapShapeType.KeyframedProfile:
//                 return ContainsKeyframedProfile(coord);

//             case MapShapeType.Diamond:
//                 return ContainsDiamond(coord);

//             case MapShapeType.Ellipse:
//                 return ContainsEllipse(coord);
//         }

//         return false;
//     }

//     bool ContainsKeyframedProfile(Vector2Int coord)
//     {
//         int width = GetWidthAtRow(coord.y);

//         if (width <= 0)
//             return false;

//         int left = centerColumn - width / 2;
//         int right = left + width - 1;

//         return coord.x >= left && coord.x <= right;
//     }

//     public int GetWidthAtRow(int row)
//     {
//         if (profileKeys == null || profileKeys.Count == 0)
//             return -1;

//         profileKeys.Sort((a, b) => a.row.CompareTo(b.row));

//         if (row < profileKeys[0].row)
//             return -1;

//         if (row > profileKeys[profileKeys.Count - 1].row)
//             return -1;

//         for (int i = 0; i < profileKeys.Count - 1; i++)
//         {
//             ProfileKey a = profileKeys[i];
//             ProfileKey b = profileKeys[i + 1];

//             if (row < a.row || row > b.row)
//                 continue;

//             return InterpolateWidth(a, b, row);
//         }

//         return profileKeys[profileKeys.Count - 1].width;
//     }

//     int InterpolateWidth(ProfileKey a, ProfileKey b, int row)
//     {
//         int totalRows = b.row - a.row;

//         if (totalRows <= 0)
//             return MakeOdd(a.width);

//         int localRow = row - a.row;
//         float progress = localRow / (float)totalRows;

//         int width = Mathf.RoundToInt(Mathf.Lerp(a.width, b.width, progress));

//         return Mathf.Max(1, MakeOdd(width));
//     }

//         int MakeOdd(int value)
//     {
//         if (value % 2 == 0)
//             return value + 1;

//         return value;
//     }

//     bool ContainsDiamond(Vector2Int coord)
//     {
//         return false;
//     }

//     bool ContainsEllipse(Vector2Int coord)
//     {
//         return false;
//     }

//     Vector2Int MirrorVertical(Vector2Int coord, MapDefinition map)
//     {
//         return new Vector2Int(
//             coord.x,
//             map.TriangleRows - 1 - coord.y
//         );
//     }

//     Vector2Int MirrorHorizontal(Vector2Int coord, MapDefinition map)
//     {
//         return new Vector2Int(
//             map.TriangleColumns - 1 - coord.x,
//             coord.y
//         );
//     }

//     public void DebugProfile(MapDefinition map)
//     {
//         Debug.Log(
//             $"Profile debug for '{name}'. " +
//             $"TriangleRows={map.TriangleRows}, TriangleColumns={map.TriangleColumns}, " +
//             $"CenterColumn={centerColumn}, Keys={profileKeys.Count}"
//         );

//         for (int row = 0; row < map.TriangleRows; row++)
//         {
//             int width = GetWidthAtRow(row);

//             if (width <= 0)
//                 continue;

//             int left = centerColumn - width / 2;
//             int right = left + width - 1;

//             Debug.Log($"Row {row}: width={width}, left={left}, right={right}");
//         }
//     }

//     Vector2Int GetProfileCoord(MapTriangle triangle)
// {
//     int profileX = triangle.coord.x * 2;

//     if (triangle.direction == TriangleDirection.Down)
//         profileX += 1;

//     return new Vector2Int(profileX, triangle.coord.y);
// }
// }