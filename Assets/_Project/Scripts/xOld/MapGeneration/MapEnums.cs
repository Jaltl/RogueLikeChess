using System.Collections.Generic;
using UnityEngine;

// Temporary state from player input
public enum GridPointVisualState
{
    None,
    Placement,
    Hover,
    Footprint,
    Invalid
}

// Static / natural map states
public enum MapRegion
{
    None,
    Neutral,
    WhiteStart,
    BlackStart,
    LeftObjective,
    RightObjective,
    CenterObjective,
    Blocked
}

public enum MapStampMode
{
    Add,
    Remove,
    SetRegion,
    BlockTerrain
}

public enum MapShapeType
{
    Rectangle,
    SymmetricTrapezoidBand,
    RowProfile,
    KeyframedProfile,
    Diamond,
    Ellipse
}

public enum TriangleDirection
{
    Up,
    Down
}

public enum MapMirrorMode
{
    None,
    Vertical,
    Horizontal,
    Both
}

public enum PlayerSide
{
    None,
    White,
    Black
}

//Map generation
// public class MapTriangle
// {
//     public Vector2Int coord;
//     public TriangleDirection direction;
//     public MapRegion region;
//     public bool isActive;
//     public bool isBlockedTerrain;

//     public Vector2Int[] cornerCoords;
//     public List<GridPoint> points = new();
// }

// //Map line VFX
// public class VisualEdge
// {
//     public GridPoint a;
//     public GridPoint b;
//     public List<MapTriangle> ownerTriangles = new();
//     public LineRenderer line;
//}