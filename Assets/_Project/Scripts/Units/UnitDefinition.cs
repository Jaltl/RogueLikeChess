using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Unit Definition")]
public class UnitDefinition : ScriptableObject
{
    [Header("Identity")]
    public string unitName;
    public Sprite unitIcon;

    [Header("Prefab")]
    public UnitPiece unitPrefab;

    [Header("Stats")]
    public int health = 10;
    public int power = 1;
    public int cost = 1;

    [Header("Placement")]
    public UnitAnchorType anchorType = UnitAnchorType.TriangleCenter;

    [Header("Triangle Footprints - Up Anchor")]
    public List<TriangleFootprintCell> baseSize = new();
    public List<TriangleFootprintCell> supportRange = new();

    [Header("Triangle Footprints - Down Anchor")]
    public List<TriangleFootprintCell> baseSizeDown = new();
    public List<TriangleFootprintCell> supportRangeDown = new();

    List<TriangleFootprintCell> CleanFootprint(List<TriangleFootprintCell> source)
    {
        HashSet<Vector2Int> unique = new();

        if (source != null)
        {
            foreach (TriangleFootprintCell cell in source)
                unique.Add(cell.Coord);
        }

        List<Vector2Int> sorted = new(unique);

        sorted.Sort((a, b) =>
        {
            int yCompare = a.y.CompareTo(b.y);
            if (yCompare != 0)
                return yCompare;

            return a.x.CompareTo(b.x);
        });

        List<TriangleFootprintCell> result = new();

        foreach (Vector2Int coord in sorted)
            result.Add(new TriangleFootprintCell(coord.x, coord.y));

        return result;
    }

    public IReadOnlyList<TriangleFootprintCell> GetBaseFootprint(
    TriangleOrientation orientation
)
    {
        if (orientation == TriangleOrientation.Down && baseSizeDown.Count > 0)
            return baseSizeDown;

        return baseSize;
    }

    public IReadOnlyList<TriangleFootprintCell> GetSupportFootprint(
        TriangleOrientation orientation
    )
    {
        if (orientation == TriangleOrientation.Down && supportRangeDown.Count > 0)
            return supportRangeDown;

        return supportRange;
    }

    public List<TriangleFootprintCell> GetEditableFootprint(
    UnitFootprintArea area,
    TriangleOrientation orientation
)
    {
        if (orientation == TriangleOrientation.Down)
        {
            return area == UnitFootprintArea.BaseSize
                ? baseSizeDown
                : supportRangeDown;
        }

        return area == UnitFootprintArea.BaseSize
            ? baseSize
            : supportRange;
    }

    public void SetFootprint(
        UnitFootprintArea area,
        TriangleOrientation orientation,
        List<TriangleFootprintCell> footprint
    )
    {
        List<TriangleFootprintCell> cleaned = CleanFootprint(footprint);

        if (orientation == TriangleOrientation.Down)
        {
            if (area == UnitFootprintArea.BaseSize)
                baseSizeDown = cleaned;
            else
                supportRangeDown = cleaned;
        }
        else
        {
            if (area == UnitFootprintArea.BaseSize)
                baseSize = cleaned;
            else
                supportRange = cleaned;
        }
    }
}