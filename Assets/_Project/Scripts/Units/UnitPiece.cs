using System.Collections.Generic;
using UnityEngine;

public class UnitPiece : MonoBehaviour
{
    [Header("Runtime Data")]
    [SerializeField] private UnitDefinition definition;
    [SerializeField] private PlayerSide owner;

    private TriangleNode anchorNode;
    private TriangleCell anchorCell;

    private readonly List<TriangleCell> occupiedCells = new();
    private readonly List<TriangleCell> supportCells = new();

    public UnitDefinition Definition => definition;

    public TriangleNode AnchorNode => anchorNode;
    public TriangleCell AnchorCell => anchorCell;

    public IReadOnlyList<TriangleCell> OccupiedCells => occupiedCells;
    public IReadOnlyList<TriangleCell> SupportCells => supportCells;

    public void Init(UnitDefinition definition, PlayerSide owner, UnitPlacementResult placement)
    {
        this.definition = definition;
        this.owner = owner;

        anchorNode = placement.anchorNode;
        anchorCell = placement.anchorCell;

        occupiedCells.Clear();
        supportCells.Clear();

        occupiedCells.AddRange(placement.baseCells);
        supportCells.AddRange(placement.supportCells);

        if (anchorNode != null)
            transform.position = anchorNode.worldPosition;
    }

    public void SetAnchor(TriangleNode node, TriangleCell cell)
    {
        anchorNode = node;
        anchorCell = cell;

        if (anchorNode != null)
            transform.position = anchorNode.worldPosition;
    }

    public void SetOccupiedCells(List<TriangleCell> cells)
    {
        occupiedCells.Clear();

        if (cells != null)
            occupiedCells.AddRange(cells);
    }

    public void SetSupportCells(List<TriangleCell> cells)
    {
        supportCells.Clear();

        if (cells != null)
            supportCells.AddRange(cells);
    }

    public void ClearRuntimeCells()
    {
        anchorNode = null;
        anchorCell = null;
        occupiedCells.Clear();
        supportCells.Clear();
    }
}