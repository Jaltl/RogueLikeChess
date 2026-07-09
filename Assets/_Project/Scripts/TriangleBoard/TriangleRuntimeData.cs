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
    Placement,
    Hover,
    Footprint,
    Invalid
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

    private bool placement;
    private bool hover;
    private bool footprint;
    private bool invalid;

    public TriangleNodeVisualState CurrentState
    {
        get
        {
            if (invalid) return TriangleNodeVisualState.Invalid;
            if (footprint) return TriangleNodeVisualState.Footprint;
            if (placement) return TriangleNodeVisualState.Placement;
            if (hover) return TriangleNodeVisualState.Hover;
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

    public void SetState(TriangleNodeVisualState state, bool active)
    {
        switch (state)
        {
            case TriangleNodeVisualState.Placement:
                placement = active;
                break;

            case TriangleNodeVisualState.Hover:
                hover = active;
                break;

            case TriangleNodeVisualState.Footprint:
                footprint = active;
                break;

            case TriangleNodeVisualState.Invalid:
                invalid = active;
                break;
        }
    }

    public void ClearVisualStates()
    {
        placement = false;
        hover = false;
        footprint = false;
        invalid = false;
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

    public Vector3 CenterPosition => center.worldPosition;

    public IEnumerable<TriangleNode> AllNodes
    {
        get
        {
            yield return corners[0];
            yield return corners[1];
            yield return corners[2];

            yield return sideMidpoints[0];
            yield return sideMidpoints[1];
            yield return sideMidpoints[2];

            yield return center;
        }
    }

    public void SetWholeVisualState(TriangleNodeVisualState state, bool active)
    {
        foreach (TriangleNode node in AllNodes)
            node.SetState(state, active);
    }

    public void SetSideVisualState(int sideIndex, TriangleNodeVisualState state, bool active)
    {
        if (sideIndex < 0 || sideIndex > 2)
            return;

        int aIndex = sideIndex;
        int bIndex = sideIndex == 2 ? 0 : sideIndex + 1;

        corners[aIndex].SetState(state, active);
        sideMidpoints[sideIndex].SetState(state, active);
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

        int aIndex = sideIndex;
        int bIndex = sideIndex == 2 ? 0 : sideIndex + 1;

        if (firstHalf)
        {
            corners[aIndex].SetState(state, active);
            sideMidpoints[sideIndex].SetState(state, active);
        }
        else
        {
            sideMidpoints[sideIndex].SetState(state, active);
            corners[bIndex].SetState(state, active);
        }
    }

    public void ClearVisualStates()
    {
        foreach (TriangleNode node in AllNodes)
            node.ClearVisualStates();
    }
}