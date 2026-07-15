using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TriangleMapStamp
{
    public bool enabled = true;
    public string name;

    [Header("Operation")]
    public MapStampMode mode = MapStampMode.Add;
    public MapRegion region = MapRegion.Neutral;

    [Header("Mirroring")]
    public MapMirrorMode mirrorMode = MapMirrorMode.None;

    public MapRegion verticalMirrorRegionOverride = MapRegion.None;
    public MapRegion horizontalMirrorRegionOverride = MapRegion.None;

    [Header("Profile")]
    public int centerColumn = 18;
    public List<TriangleProfileKey> profileKeys = new();

    public class ResolvedProfileRow
{
    public int row;
    public int left;
    public int right;

    public int Width => right - left + 1;
}

    public bool ContainsCell(TriangleCell cell, TriangleMapDefinition map)
    {
        if (!enabled || cell == null || map == null)
            return false;

        Vector2Int coord = cell.coord;

        if (ContainsProfile(coord, map))
            return true;

        if (mirrorMode == MapMirrorMode.Vertical || mirrorMode == MapMirrorMode.Both)
        {
            Vector2Int mirrored = MirrorVertical(coord, map);

            if (ContainsProfile(mirrored, map))
                return true;
        }

        if (mirrorMode == MapMirrorMode.Horizontal || mirrorMode == MapMirrorMode.Both)
        {
            Vector2Int mirrored = MirrorHorizontal(coord, map);

            if (ContainsProfile(mirrored, map))
                return true;
        }

        if (mirrorMode == MapMirrorMode.Both)
        {
            Vector2Int mirrored = MirrorVertical(MirrorHorizontal(coord, map), map);

            if (ContainsProfile(mirrored, map))
                return true;
        }

        return false;
    }

    public MapRegion GetRegionForCell(TriangleCell cell, TriangleMapDefinition map)
    {
        if (cell == null || map == null)
            return region;

        Vector2Int coord = cell.coord;

        if (ContainsProfile(coord, map))
            return region;

        if (mirrorMode == MapMirrorMode.Vertical || mirrorMode == MapMirrorMode.Both)
        {
            Vector2Int mirrored = MirrorVertical(coord, map);

            if (ContainsProfile(mirrored, map))
            {
                return verticalMirrorRegionOverride == MapRegion.None
                    ? region
                    : verticalMirrorRegionOverride;
            }
        }

        if (mirrorMode == MapMirrorMode.Horizontal || mirrorMode == MapMirrorMode.Both)
        {
            Vector2Int mirrored = MirrorHorizontal(coord, map);

            if (ContainsProfile(mirrored, map))
            {
                return horizontalMirrorRegionOverride == MapRegion.None
                    ? region
                    : horizontalMirrorRegionOverride;
            }
        }

        return region;
    }

    bool ContainsProfile(Vector2Int coord, TriangleMapDefinition map)
{
    int width = GetWidthAtRow(coord.y, map);

    if (width <= 0)
        return false;

    int profileX = map.GetProfileColumn(coord);

    int left = centerColumn - width / 2;
    int right = left + width - 1;

    return profileX >= left && profileX <= right;
}

    int GetWidthAtRow(int row, TriangleMapDefinition map)
    {
        if (profileKeys == null || profileKeys.Count == 0)
            return -1;

        profileKeys.Sort((a, b) => a.row.CompareTo(b.row));

        if (row < profileKeys[0].row)
            return -1;

        if (row > profileKeys[profileKeys.Count - 1].row)
            return -1;

        for (int i = 0; i < profileKeys.Count - 1; i++)
        {
            TriangleProfileKey a = profileKeys[i];
            TriangleProfileKey b = profileKeys[i + 1];

            if (row < a.row || row > b.row)
                continue;

            return InterpolateWidth(a, b, row, map);
        }

        return CleanWidth(profileKeys[profileKeys.Count - 1].width, map);
    }

    int InterpolateWidth(TriangleProfileKey a, TriangleProfileKey b, int row, TriangleMapDefinition map)
    {
        int totalRows = b.row - a.row;

        if (totalRows <= 0)
            return CleanWidth(a.width, map);

        int localRow = row - a.row;
        float progress = localRow / (float)totalRows;

        int width = Mathf.RoundToInt(Mathf.Lerp(a.width, b.width, progress));

        return CleanWidth(width, map);
    }

    int CleanWidth(int width, TriangleMapDefinition map)
    {
        width = Mathf.Max(1, width);

        if (map != null && map.enforceOddStampWidths && width % 2 == 0)
            width += 1;

        return width;
    }

    Vector2Int MirrorVertical(Vector2Int coord, TriangleMapDefinition map)
    {
        return new Vector2Int(
            coord.x,
            map.triangleRows - 1 - coord.y
        );
    }

    Vector2Int MirrorHorizontal(Vector2Int coord, TriangleMapDefinition map)
{
    int profileX = map.GetProfileColumn(coord);

    int mirroredProfileX = map.playableColumns - 1 - profileX;

    // Convert back approximately into physical coordinate.
    int physicalX = mirroredProfileX;

    if (map.compensateOffsetRowsInProfiles && map.IsOffsetRow(coord.y))
        physicalX -= 1;

    return new Vector2Int(physicalX, coord.y);
}
}