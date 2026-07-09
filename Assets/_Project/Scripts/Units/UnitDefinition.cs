using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Game/Unit Definition")]
public class UnitDefinition : ScriptableObject
{
    [Header("Identity")]
    public string unitName;
    public Sprite icon;
    public UnitPiece unitPrefab;

    [Header("Stats")]
    public int Power;
    public int Health;
    public int Cost;

    [Header("Placement Anchor")]
    public UnitAnchorType anchorType = UnitAnchorType.TriangleCenter;

    [Header("Triangle Footprints")]
    public List<TriangleFootprintCell> baseSize = new();
    public List<TriangleFootprintCell> supportRange = new();

    public IReadOnlyList<TriangleFootprintCell> GetFootprint(UnitFootprintArea area)
    {
        return area == UnitFootprintArea.BaseSize ? baseSize : supportRange;
    }

    public void SetFootprint(UnitFootprintArea area, List<TriangleFootprintCell> footprint)
    {
        if (area == UnitFootprintArea.BaseSize)
            baseSize = CleanFootprint(footprint);
        else
            supportRange = CleanFootprint(footprint);
    }

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
            if (yCompare != 0) return yCompare;
            return a.x.CompareTo(b.x);
        });

        List<TriangleFootprintCell> result = new();

        foreach (Vector2Int coord in sorted)
            result.Add(new TriangleFootprintCell(coord.x, coord.y));

        return result;
    }
}