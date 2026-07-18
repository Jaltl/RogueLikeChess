using UnityEngine;

public enum UnitAnchorType
{
    Corner
}

public enum UnitFootprintArea
{
    BaseSize,
    SupportRange
}

public enum UnitFootprintFacing
{
    Up = 0,
    UpRight = 1,
    DownRight = 2,
    Down = 3,
    DownLeft = 4,
    UpLeft = 5
}

[System.Serializable]
public struct TriangleFootprintCell
{
    public float localX;
    public float localY;

    public TriangleFootprintCell(float localX, float localY)
    {
        this.localX = localX;
        this.localY = localY;
    }

    public Vector2 LocalOffset => new Vector2(localX, localY);
}