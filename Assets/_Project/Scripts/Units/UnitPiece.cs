using System.Collections.Generic;
using UnityEngine;

public enum PlayerSide
{
    White,
    Black
}

public class UnitPiece : MonoBehaviour
{
    public UnitDefinition definition;
    public PlayerSide owner;

    public HexTile anchorHex { get; private set; }
    public List<HexTile> occupiedHexes { get; private set; } = new();

    public void Init(UnitDefinition definition, PlayerSide owner, HexTile anchor)
    {
        this.definition = definition;
        this.owner = owner;
        anchorHex = anchor;
    }

    public void SetOccupiedHexes(List<HexTile> hexes)
    {
        occupiedHexes = hexes;
    }

    public void ClearOccupiedHexes()
    {
        occupiedHexes.Clear();
    }

    public void SetAnchorHex(HexTile hex)
    {
        anchorHex = hex;
    }
}