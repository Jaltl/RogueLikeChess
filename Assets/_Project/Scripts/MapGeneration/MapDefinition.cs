using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Map Definition")]
public class MapDefinition : ScriptableObject
{
[Header("Triangle Map Size")]
public int triangleColumns = 31;
public int triangleRows = 49;

[Header("Triangle Point Size")]
public int trianglePointWidth = 5;
public int trianglePointHeight = 7;

[Header("Triangle Pattern")]
public bool startRowsWithDownTriangle = true;

public int Width => triangleColumns * (trianglePointWidth - 1) + 1;
public int Height => triangleRows * (trianglePointHeight - 1) + 1;
public int ProfileColumns => TriangleColumns * 2 - 1;
public int ProfileCenterColumn => ProfileColumns / 2;

[Header("Spacing")]
public float pointSpacing = 0.025f;

[Header("Calculated - Do Not Edit")]
[SerializeField] private int calculatedPointWidth;
[SerializeField] private int calculatedPointHeight;
[SerializeField] private int calculatedTriangleColumns;
[SerializeField] private int calculatedTriangleRows;
[SerializeField] private int calculatedCenterColumn;
[SerializeField] private int calculatedCenterRow;

[Header("Calculated Profile Info")]
[SerializeField] private int calculatedProfileColumns;
[SerializeField] private int calculatedProfileCenterColumn;

public float RowSpacing => pointSpacing * Mathf.Sqrt(3f) / 2f;
public int WidthSteps => trianglePointWidth - 1;
public int HeightSteps => trianglePointHeight - 1;

public int HalfWidthSteps => WidthSteps / 2;

public int PointWidth => (triangleColumns - 1) * HalfWidthSteps + WidthSteps + 1;

public int PointHeight => triangleRows * HeightSteps + 1;

public int TriangleColumns => triangleColumns;
public int TriangleRows => triangleRows;

public int CenterColumn => triangleColumns / 2;
public int CenterRow => triangleRows / 2;

    [Header("Map Shape Stamps")]
    public List<MapStamp> stamps = new();

    [Header("Region Colors")]
    public Color neutralColor = Color.black;
    public Color whiteStartColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    public Color blackStartColor = new Color(1f, 0.35f, 0.35f, 1f);
    public Color leftObjectiveColor = Color.cyan;
    public Color rightObjectiveColor = Color.cyan;
    public Color centerObjectiveColor = Color.yellow;
    public Color blockedColor = new Color(0.45f, 0.25f, 0.15f, 1f);

    public Color GetRegionColor(MapRegion region)
    {
        switch (region)
        {
            case MapRegion.WhiteStart:
                return whiteStartColor;

            case MapRegion.BlackStart:
                return blackStartColor;

            case MapRegion.LeftObjective:
                return leftObjectiveColor;

            case MapRegion.RightObjective:
                return rightObjectiveColor;

            case MapRegion.CenterObjective:
                return centerObjectiveColor;

            case MapRegion.Blocked:
                return blockedColor;

            case MapRegion.Neutral:
            default:
                return neutralColor;
        }
    }

    private void OnValidate()
{
    if (trianglePointWidth < 3)
        trianglePointWidth = 3;

    if (trianglePointHeight < 2)
        trianglePointHeight = 2;

    // This system assumes a clean half-width.
    if ((trianglePointWidth - 1) % 2 != 0)
        trianglePointWidth += 1;

    calculatedPointWidth = PointWidth;
    calculatedPointHeight = PointHeight;
    calculatedTriangleColumns = TriangleColumns;
    calculatedTriangleRows = TriangleRows;
    calculatedCenterColumn = CenterColumn;
    calculatedCenterRow = CenterRow;
}
}