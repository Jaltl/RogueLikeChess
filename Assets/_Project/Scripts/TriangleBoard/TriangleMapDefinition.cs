using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Triangle Map Definition")]
public class TriangleMapDefinition : ScriptableObject
{
    [Header("Logical Map Size")]
    public int playableColumns = 49;
    public int triangleRows = 49;

    [Header("Generated Grid Padding")]
    public int columnPadding = 3;

    [Header("Triangle Geometry")]
    public float sideLength = 0.08f;
    public bool offsetEveryOtherRow = true;
    public bool firstColumnIsUp = true;

    [Header("Stamp Rules")]
    public bool enforceOddStampWidths = true;
    public bool compensateOffsetRowsInProfiles = true;

    [Header("Calculated Info")]
    [SerializeField] private int calculatedGeneratedMinColumn;
    [SerializeField] private int calculatedGeneratedMaxColumn;
    [SerializeField] private int calculatedGeneratedColumnCount;
    [SerializeField] private int calculatedCenterColumn;
    [SerializeField] private int calculatedCenterRow;
    [SerializeField] private float calculatedTriangleHeight;

    [Header("Stamps")]
    public List<TriangleMapStamp> stamps = new();

    [Header("Region Colors")]
    public Color neutralColor = Color.black;
    public Color whiteStartColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    public Color blackStartColor = new Color(1f, 0.35f, 0.35f, 1f);
    public Color leftObjectiveColor = Color.cyan;
    public Color rightObjectiveColor = Color.cyan;
    public Color centerObjectiveColor = Color.yellow;
    public Color blockedColor = new Color(0.45f, 0.25f, 0.15f, 1f);

    public int GeneratedMinColumn => -columnPadding;
    public int GeneratedMaxColumn => playableColumns - 1 + columnPadding;
    public int GeneratedColumnCount => GeneratedMaxColumn - GeneratedMinColumn + 1;

    public int CenterColumn => playableColumns / 2;
    public int CenterRow => triangleRows / 2;

    public float TriangleHeight => sideLength * Mathf.Sqrt(3f) * 0.5f;

    public bool IsOffsetRow(int row)
    {
        return offsetEveryOtherRow && row % 2 == 1;
    }

    public int GetProfileColumn(Vector2Int coord)
    {
        if (!compensateOffsetRowsInProfiles)
            return coord.x;

        // Odd rows are visually shifted right.
        // Treat them as if their logical x is one column to the right.
        // This allows active cells to start one physical column earlier.
        if (IsOffsetRow(coord.y))
            return coord.x + 1;

        return coord.x;
    }

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
        if (playableColumns < 1)
            playableColumns = 1;

        if (playableColumns % 2 == 0)
            playableColumns += 1;

        if (triangleRows < 1)
            triangleRows = 1;

        if (triangleRows % 2 == 0)
            triangleRows += 1;

        if (columnPadding < 0)
            columnPadding = 0;

        if (sideLength <= 0f)
            sideLength = 0.08f;

        calculatedGeneratedMinColumn = GeneratedMinColumn;
        calculatedGeneratedMaxColumn = GeneratedMaxColumn;
        calculatedGeneratedColumnCount = GeneratedColumnCount;
        calculatedCenterColumn = CenterColumn;
        calculatedCenterRow = CenterRow;
        calculatedTriangleHeight = TriangleHeight;
    }
}