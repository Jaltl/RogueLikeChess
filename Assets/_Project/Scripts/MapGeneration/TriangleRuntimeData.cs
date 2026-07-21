using System.Collections.Generic;
using UnityEngine;

public enum TriangleOrientation
{
    Up,
    Down
}

public enum TriangleNodeVisualState
{
    None,

    DisabledSupport,
    WhiteStartArea,
    BlackStartArea,
    ActiveSupport,

    UnitBase,

    Hover,
    PreviewValid,
    PreviewInvalid
}

[System.Serializable]
public class TriangleProfileKey
{
    public int row;
    public int width;
}

public class TriangleNode
{
    public string key;
    public Vector3 worldPosition;

    public readonly List<TriangleCell> ownerTriangles = new();
    public readonly HashSet<UnitAnchorType> supportedUnitAnchors = new();

    private bool disabledSupport;
    private bool whiteStartArea;
    private bool blackStartArea;
    private bool activeSupport;
    private bool hover;
    private bool previewValid;
    private bool previewInvalid;

    private bool unitBase;

    public TriangleNodeVisualState CurrentState
    {
        get
        {
            if (previewInvalid)
                return TriangleNodeVisualState.PreviewInvalid;

            if (previewValid)
                return TriangleNodeVisualState.PreviewValid;

            if (hover)
                return TriangleNodeVisualState.Hover;

            if (unitBase)
                return TriangleNodeVisualState.UnitBase;

            if (activeSupport)
                return TriangleNodeVisualState.ActiveSupport;

            if (whiteStartArea)
                return TriangleNodeVisualState.WhiteStartArea;

            if (blackStartArea)
                return TriangleNodeVisualState.BlackStartArea;

            if (disabledSupport)
                return TriangleNodeVisualState.DisabledSupport;

            return TriangleNodeVisualState.None;
        }
    }

    public TriangleNode(string key, Vector3 worldPosition)
    {
        this.key = key;
        this.worldPosition = worldPosition;
    }

    public void AddOwner(TriangleCell triangle)
    {
        if (triangle != null && !ownerTriangles.Contains(triangle))
            ownerTriangles.Add(triangle);
    }

    public void RegisterUnitAnchorType(UnitAnchorType anchorType)
    {
        supportedUnitAnchors.Add(anchorType);
    }

    public bool SupportsUnitAnchorType(UnitAnchorType anchorType)
    {
        return supportedUnitAnchors.Contains(anchorType);
    }

    public void SetState(TriangleNodeVisualState state, bool active)
    {
        switch (state)
        {
            case TriangleNodeVisualState.DisabledSupport:
                disabledSupport = active;
                break;

            case TriangleNodeVisualState.WhiteStartArea:
                whiteStartArea = active;
                break;

            case TriangleNodeVisualState.BlackStartArea:
                blackStartArea = active;
                break;

            case TriangleNodeVisualState.ActiveSupport:
                activeSupport = active;
                break;

            case TriangleNodeVisualState.Hover:
                hover = active;
                break;

            case TriangleNodeVisualState.PreviewValid:
                previewValid = active;
                break;

            case TriangleNodeVisualState.PreviewInvalid:
                previewInvalid = active;
                break;
            
            case TriangleNodeVisualState.UnitBase:
                unitBase = active;
                break;

            case TriangleNodeVisualState.None:
            default:
                break;
        }
    }

    public void ClearVisualStates()
    {
        disabledSupport = false;
        whiteStartArea = false;
        blackStartArea = false;
        activeSupport = false;
        unitBase = false;
        hover = false;
        previewValid = false;
        previewInvalid = false;
    }
}

public class TriangleCell
{
    public Vector2Int coord;
    public TriangleOrientation orientation;

    public bool isActive;
    public bool isBlocked;
    public MapRegion region = MapRegion.None;

    // Convention:
    // corners[0] = base left
    // corners[1] = base right
    // corners[2] = tip
    public TriangleNode[] corners = new TriangleNode[3];

    // sideMidpoints[0] = between corner 0 and 1
    // sideMidpoints[1] = between corner 1 and 2
    // sideMidpoints[2] = between corner 2 and 0
    public TriangleNode[] sideMidpoints = new TriangleNode[3];

    public TriangleNode center;

    public Vector3 CenterPosition
    {
        get
        {
            if (center == null)
                return Vector3.zero;

            return center.worldPosition;
        }
    }

    public IEnumerable<TriangleNode> AllNodes
    {
        get
        {
            if (corners != null)
            {
                for (int i = 0; i < corners.Length; i++)
                    yield return corners[i];
            }

            if (sideMidpoints != null)
            {
                for (int i = 0; i < sideMidpoints.Length; i++)
                    yield return sideMidpoints[i];
            }

            yield return center;
        }
    }

    public void SetWholeVisualState(TriangleNodeVisualState state, bool active)
    {
        foreach (TriangleNode node in AllNodes)
        {
            if (node != null)
                node.SetState(state, active);
        }
    }

    public void SetSideVisualState(int sideIndex, TriangleNodeVisualState state, bool active)
    {
        if (sideIndex < 0 || sideIndex > 2)
            return;

        if (corners == null || corners.Length < 3)
            return;

        if (sideMidpoints == null || sideMidpoints.Length < 3)
            return;

        int aIndex = sideIndex;
        int bIndex = sideIndex == 2 ? 0 : sideIndex + 1;

        if (corners[aIndex] != null)
            corners[aIndex].SetState(state, active);

        if (sideMidpoints[sideIndex] != null)
            sideMidpoints[sideIndex].SetState(state, active);

        if (corners[bIndex] != null)
            corners[bIndex].SetState(state, active);
    }

    public void SetSideHalfVisualState(
        int sideIndex,
        bool firstHalf,
        TriangleNodeVisualState state,
        bool active
    )
    {
        if (sideIndex < 0 || sideIndex > 2)
            return;

        if (corners == null || corners.Length < 3)
            return;

        if (sideMidpoints == null || sideMidpoints.Length < 3)
            return;

        int aIndex = sideIndex;
        int bIndex = sideIndex == 2 ? 0 : sideIndex + 1;

        if (firstHalf)
        {
            if (corners[aIndex] != null)
                corners[aIndex].SetState(state, active);

            if (sideMidpoints[sideIndex] != null)
                sideMidpoints[sideIndex].SetState(state, active);
        }
        else
        {
            if (sideMidpoints[sideIndex] != null)
                sideMidpoints[sideIndex].SetState(state, active);

            if (corners[bIndex] != null)
                corners[bIndex].SetState(state, active);
        }
    }

    public void ClearVisualStates()
    {
        foreach (TriangleNode node in AllNodes)
        {
            if (node != null)
                node.ClearVisualStates();
        }
    }
}