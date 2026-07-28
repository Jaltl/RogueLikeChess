using UnityEngine;
using System;

public enum UnitDefeatBehavior
{
    RemoveFromBoard,
    LeaveAsTerrainBlocker,
}

[Flags]
public enum UnitTag
{
    None = 0,

    Beast = 1 << 0,
    Monster = 1 << 1,
    Humanoid = 1 << 2,
    Undead = 1 << 3,
    Object = 1 << 4,
    Fae = 1 << 5,
    GiantKin = 1 << 6,
    PrimalKin = 1 << 7,
    Sorcerer = 1 << 8,
    Unique = 1 << 9,
    Lich = 1 << 10,
    Spy = 1 << 11,
    Violent = 1 << 12,
    Trampler = 1 << 13,
    Influence = 1 << 14,
    Knight = 1 << 15
}
