using System.Collections.Generic;
using UnityEngine;

public enum UnitAnchorType
{
    TriangleCenter,
    Corner,
    SideMidpoint
}

public enum UnitFootprintArea
{
    BaseSize,
    SupportRange
}

[System.Serializable]
public struct TriangleFootprintCell
{
    public int x;
    public int y;

    public TriangleFootprintCell(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public Vector2Int Coord => new Vector2Int(x, y);
}