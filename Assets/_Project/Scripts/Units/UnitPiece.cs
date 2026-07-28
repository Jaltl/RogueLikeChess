using System.Collections.Generic;
using UnityEngine;

public enum PlayerSide
{
    White,
    Black
}

public class UnitPiece : MonoBehaviour
{
    [Header("Runtime Definition")]
    [SerializeField] private UnitDefinition definition;

    [Header("Child Sprite")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    private bool supportActive;
    private bool isDefeated;

    private PlayerSide owner;
    private TriangleNode anchorNode;
    private TriangleCell anchorCell;
    private readonly List<TriangleCell> occupiedCells = new();
    private readonly List<TriangleCell> supportCells = new();

    public UnitDefinition Definition => definition;
    public PlayerSide Owner => owner;
    public TriangleNode AnchorNode => anchorNode;
    public TriangleCell AnchorCell => anchorCell;
    public IReadOnlyList<TriangleCell> OccupiedCells => occupiedCells;
    public IReadOnlyList<TriangleCell> SupportCells => supportCells;
    public bool IsDefeated => isDefeated;
    public bool SupportActive => supportActive;

    public void SetSupportActive(bool active)
    {
        supportActive = active;
    }

    public void SetDefeated(bool defeated)
    {
        isDefeated = defeated;

        if(isDefeated)
        {
            SetSupportActive(false);
        }
    }

    private void Reset()
    {
        AutoWire();
    }

    private void OnValidate()
    {
        AutoWire();
        ApplyDefinition();
    }

    private void Awake()
    {
        AutoWire();
        ApplyDefinition();
    }

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
        {
            Vector3 position = anchorNode.worldPosition;
            position.z = transform.position.z;
            transform.position = position;
        }

        ApplyDefinition();
    }

    public void ClearRuntimeCells()
    {
        anchorNode = null;
        anchorCell = null;
        occupiedCells.Clear();
        supportCells.Clear();
    }

    private void ApplyDefinition()
    {
        if (definition == null)
            return;

        gameObject.name = $"Unit - {definition.DisplayName}";

        if (spriteRenderer != null)
            spriteRenderer.sprite = definition.unitIcon;
    }

    private void AutoWire()
    {
        if (spriteRenderer != null)
            return;

        spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
    }
}