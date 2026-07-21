using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Game/Unit Definition")]
public class UnitDefinition : ScriptableObject
{
    [Header("Identity")]
    public string unitName;

    [Header("unit icon")]
    public Sprite unitIcon;
    
    [Header("card icon")]
    public Sprite cardIcon;

    [Header("Prefab")]
    public UnitPiece unitPrefab;

    [Header("Stats")]
    public int power = 1;
    public int cost = 1;

    [Header("Placement")]
    public UnitAnchorType anchorType = UnitAnchorType.Corner;

    [Header("Triangle Footprints")]
    public List<TriangleFootprintCell> baseSize = new();
    public List<TriangleFootprintCell> supportRange = new();

    // Legacy fields for old hex scripts. Remove these after deleting the old hex placement scripts.
    [HideInInspector] public int placementExpansion = 1;
    [HideInInspector] public float footprintRadius = 1.5f;
    [HideInInspector] public float footprintRotationDegrees = 0f;

    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(unitName))
                return unitName;

            return name;
        }
    }

    public IReadOnlyList<TriangleFootprintCell> GetFootprint(UnitFootprintArea area)
    {
        return area == UnitFootprintArea.BaseSize
            ? baseSize
            : supportRange;
    }

    public List<TriangleFootprintCell> GetMutableFootprint(UnitFootprintArea area)
    {
        return area == UnitFootprintArea.BaseSize
            ? baseSize
            : supportRange;
    }

    public void SetFootprint(UnitFootprintArea area, List<TriangleFootprintCell> footprint)
    {
        if (area == UnitFootprintArea.BaseSize)
            baseSize = CleanFootprint(footprint);
        else
            supportRange = CleanFootprint(footprint);

        anchorType = UnitAnchorType.Corner;
    }

    private List<TriangleFootprintCell> CleanFootprint(List<TriangleFootprintCell> source)
    {
        List<TriangleFootprintCell> result = new();

        if (source == null)
            return result;

        HashSet<string> used = new();

        foreach (TriangleFootprintCell cell in source)
        {
            int roundedX = Mathf.RoundToInt(cell.localX * 10000f);
            int roundedY = Mathf.RoundToInt(cell.localY * 10000f);
            string key = $"{roundedX},{roundedY}";

            if (used.Add(key))
                result.Add(cell);
        }

        result.Sort((a, b) =>
        {
            int yCompare = a.localY.CompareTo(b.localY);
            if (yCompare != 0)
                return yCompare;

            return a.localX.CompareTo(b.localX);
        });

        return result;
    }

    private void OnValidate()
    {
        anchorType = UnitAnchorType.Corner;
    }
}