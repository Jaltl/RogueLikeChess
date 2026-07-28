using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections;

public class TrianglePlacementController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TrianglePlacementVisualController placementVisuals;
    [SerializeField] private TriangleGridManager grid;
    [SerializeField] private TriangleLineRenderer lineRenderer;
    [SerializeField] private TriangleBoard board;
    [SerializeField] private GameFlowController gameFlow;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Transform unitRoot;

    [Header("Turn Flow")]
    [SerializeField] private bool switchPlayerAfterSuccessfulPlacement = true;

public PlayerSide CurrentPlayer => currentPlayer;

    [Header("Input")]
    [SerializeField] private bool enableActionsManually = true;
    [SerializeField] private InputActionReference pointerPositionAction;
    [SerializeField] private InputActionReference placeAction;
    [SerializeField] private InputActionReference cancelAction;

    [Header("Unit Rotation")]
    [SerializeField] private InputActionReference rotateFootprintAction;

    [SerializeField] private InputActionReference rotateUP;
    [SerializeField] private InputActionReference rotateUpRight;
    [SerializeField] private InputActionReference rotateBottomRight;
    [SerializeField] private InputActionReference rotateBottom;
    [SerializeField] private InputActionReference rotateBottomLeft;
    [SerializeField] private InputActionReference rotateUpLeft;

private UnitFootprintFacing currentFacing = UnitFootprintFacing.Up;

    [Header("Placement")]
    [SerializeField] private float snapDistanceMultiplier = 1.2f;
    [SerializeField] private bool ignorePointerOverUi = true;

    [Header("Anchor Marker")]
    [SerializeField] private bool showAnchorMarker = true;
    [SerializeField] private float anchorMarkerRadius = 0.08f;
    [SerializeField] private int anchorMarkerSegments = 32;
    [SerializeField] private float anchorMarkerZ = -0.25f;
    [SerializeField] private float anchorMarkerLineWidth = 0.025f;
    [SerializeField] private Material anchorMarkerMaterial;
    [SerializeField] private Color validAnchorMarkerColor = Color.yellow;
    [SerializeField] private Color invalidAnchorMarkerColor = Color.red;

    private GameObject anchorMarkerObject;
    private LineRenderer anchorMarkerLine;

    private UnitDefinition selectedUnit;
    private PlayerSide currentPlayer = PlayerSide.White;

    private readonly HashSet<TriangleCell> whitePlacementZone = new();
    private readonly HashSet<TriangleCell> blackPlacementZone = new();
    private readonly HashSet<TriangleCell> emptyPlacementZone = new();

    private readonly HashSet<TriangleCell> whiteExpandedPlacementZone = new();
    private readonly HashSet<TriangleCell> blackExpandedPlacementZone = new();

    private readonly HashSet<TriangleCell> currentPlacementZoneCache = new();

    private readonly HashSet<TriangleNode> controlledAreaCornerNodes = new();

    private readonly HashSet<TriangleCell> controlledAreaCache = new();

    private UnitPlacementResult currentPreview;
    private bool currentPreviewIsValid;

    public bool HasSelectedUnit => selectedUnit != null;

    private bool placeRequested;
    private bool cancelRequested;

    private bool isRebuildingPlacementVisuals;

    float MaxSnapDistance
    {
        get
        {
            if (grid != null && grid.MapDefinition != null)
                return grid.MapDefinition.sideLength * snapDistanceMultiplier;

            return 0.25f;
        }
    }

    private void Awake()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;
    }

    private IEnumerator Start()
    {
        yield return null;

        InitializePlacement();
    }

    private void OnEnable()
    {
        RegisterInputActions(true);
    }

    private void OnDisable()
    {
        RegisterInputActions(false);
    }

    private void Update()
    {
        UpdatePreviewFromPointer();

        bool consumePlace = placeRequested;
        bool consumeCancel = cancelRequested;

        placeRequested = false;
        cancelRequested = false;

        // Right click / cancel has priority.
        if (consumeCancel)
        {
            if (selectedUnit != null)
            {
                ClearSelectedUnit();
            }
            else
            {
                TryKillHoveredUnitForDebug();
            }

            return;
        }

        if (consumePlace)
        {
            TryPlaceSelectedUnit();
        }
    }

    void RegisterInputActions(bool register)
    {
        RegisterAction(placeAction, OnPlaceInput, register);
        RegisterAction(cancelAction, OnCancelInput, register);
        RegisterAction(rotateFootprintAction, OnRotateFootprintInput, register);

    RegisterAction(rotateUP, OnFacingUpInput, register);
    RegisterAction(rotateUpRight, OnFacingUpRightInput, register);
    RegisterAction(rotateBottomRight, OnFacingDownRightInput, register);
    RegisterAction(rotateBottom, OnFacingDownInput, register);
    RegisterAction(rotateBottomLeft, OnFacingDownLeftInput, register);
    RegisterAction(rotateUpLeft, OnFacingUpLeftInput, register);

        if (pointerPositionAction != null && pointerPositionAction.action != null)
        {
            if (register && enableActionsManually)
                pointerPositionAction.action.Enable();

            if (!register && enableActionsManually)
                pointerPositionAction.action.Disable();
        }
    }

    void RegisterAction(
    InputActionReference actionReference,
    System.Action<InputAction.CallbackContext> callback,
    bool register
)
{
    if (actionReference == null || actionReference.action == null)
        return;

    if (register)
    {
        actionReference.action.performed += callback;
        actionReference.action.Enable();
    }
    else
    {
        actionReference.action.performed -= callback;
        actionReference.action.Disable();
    }
}
    void OnPlaceInput(InputAction.CallbackContext context)
    {
        Debug.Log("Place input received.");
        placeRequested = true;
    }

    void OnCancelInput(InputAction.CallbackContext context)
    {
        Debug.Log("Cancel input received.");
        cancelRequested = true;
    }

    public void InitializePlacement()
    {
        CreateInitialPlacementZones();
        ShowCurrentPlacementZone();
    }

    void CreateInitialPlacementZones()
    {
        int whiteCount = 0;
        int blackCount = 0;
        int activeCount = 0;

        foreach (TriangleCell cell in grid.AllCells)
        {
            if (cell == null || !cell.isActive)
                continue;

            activeCount++;

            if (cell.region == MapRegion.WhiteStart)
                whiteCount++;

            if (cell.region == MapRegion.BlackStart)
                blackCount++;
        }

        Debug.Log(
            $"Placement zone scan | Active: {activeCount} | " +
            $"WhiteStart: {whiteCount} | BlackStart: {blackCount}"
        );
    }
    public void SelectUnit(UnitDefinition unit)
    {
        selectedUnit = unit;
        currentFacing = GetDefaultFacingForPlayer(currentPlayer);

        currentPreview = null;
        currentPreviewIsValid = false;
        HideAnchorMarker();

        if (selectedUnit != null)
        {
            Debug.Log(
                $"Selected unit: {selectedUnit.DisplayName} | " +
                $"Current player: {currentPlayer} | " +
                $"Default facing: {currentFacing}"
            );
        }

        RebuildPlacementVisuals();
    }

    public void ClearSelectedUnit()
    {
        selectedUnit = null;
        currentPreview = null;
        currentPreviewIsValid = false;

        HideAnchorMarker();
        ClearPreview();
        ShowCurrentPlacementZone();
    }

    private float nextPreviewDebugTime;

    void UpdatePreviewFromPointer()
    {
        if (selectedUnit == null)
            return;

        if (grid == null)
        {
            DebugPreview("No TriangleGridManager assigned.");
            return;
        }

        if (ignorePointerOverUi && IsPointerOverUi())
        {
            currentPreview = null;
            currentPreviewIsValid = false;
            HideAnchorMarker();

            RebuildPlacementVisuals();

            DebugPreview("Pointer is over UI, preview blocked.");
            return;
        }

        Vector3 pointerWorld = GetPointerWorldPosition();

        bool hasGeometryPreview = UnitFootprintResolver.TryResolvePlacementFromWorld(
            grid,
            selectedUnit,
            pointerWorld,
            currentFacing,
            MaxSnapDistance,
            out currentPreview,
            null
        );

        if (!hasGeometryPreview || currentPreview == null)
        {
            currentPreview = null;
            currentPreviewIsValid = false;
            HideAnchorMarker();

            RebuildPlacementVisuals();

            DebugPreview(
                $"No geometry preview. Mouse world: {pointerWorld}, " +
                $"Anchor: {selectedUnit.anchorType}, MaxSnapDistance: {MaxSnapDistance}"
            );

            return;
        }

        currentPreviewIsValid = CanPlaceUnit(selectedUnit, currentPreview);

        UpdateAnchorMarker(currentPreview, currentPreviewIsValid);

        RebuildPlacementVisuals();
    }

    void DebugPreview(string message)
    {
        if (Time.time < nextPreviewDebugTime)
            return;

        nextPreviewDebugTime = Time.time + 0.5f;
        Debug.Log($"[Placement Preview] {message}");
    }

    bool IsBaseCellValidForCurrentPlayer(TriangleCell cell)
    {
        if (cell == null)
            return false;

        if (!cell.isActive)
            return false;

        if (cell.isBlocked)
            return false;

        if (!GetCurrentPlacementZone().Contains(cell))
            return false;

        if (board != null && board.IsCellOccupied(cell))
            return false;

        return true;
    }

    private bool CanPlaceUnit(UnitDefinition unitDef, UnitPlacementResult placement)
    {
        if (unitDef == null || placement == null)
            return false;

        if (placement.baseCells == null || placement.baseCells.Count == 0)
            return false;

        foreach (TriangleCell baseCell in placement.baseCells)
        {
            if (!IsBaseCellTerrainValid(baseCell))
                return false;
        }

        return BaseTouchesOrOverlapsActiveControlledArea(
            placement.baseCells,
            currentPlayer
        );
    }

    private bool IsBaseCellTerrainValid(TriangleCell cell)
    {
        if (cell == null)
            return false;

        if (!cell.isActive)
            return false;

        if (cell.isBlocked)
            return false;

        // Later, when terrain flags are active:
        // if (cell.BlocksPlacement)
        //     return false;

        // Existing unit bodies block placement.
        if (board != null && board.CellBlocksPlacementForSide(cell, currentPlayer))
            return false;

        return true;
    }

    private bool PlacementTouchesOrOverlapsControlledArea(UnitPlacementResult placement)
    {
        HashSet<TriangleCell> controlledArea = GetCurrentPlacementZone();

        if (controlledArea == null || controlledArea.Count == 0)
            return false;

        // Overlap is allowed and counts as connected.
        foreach (TriangleCell baseCell in placement.baseCells)
        {
            if (baseCell == null)
                continue;

            if (controlledArea.Contains(baseCell))
                return true;
        }

        // Touching by corner or edge also counts as connected.
        return FootprintTouchesControlledArea(placement.baseCells, controlledArea);
    }

    private bool BaseTouchesOrOverlapsActiveControlledArea(
    IReadOnlyList<TriangleCell> baseCells,
    PlayerSide side
)
    {
        HashSet<TriangleCell> controlledArea = GetActiveControlledArea(side);

        if (controlledArea == null || controlledArea.Count == 0)
            return false;

        return CellsTouchOrOverlapCellSet(baseCells, controlledArea);
    }

    private bool CellsTouchOrOverlapCellSet(
    IReadOnlyList<TriangleCell> sourceCells,
    HashSet<TriangleCell> targetCells
)
    {
        if (sourceCells == null || targetCells == null || targetCells.Count == 0)
            return false;

        // Overlap counts.
        foreach (TriangleCell sourceCell in sourceCells)
        {
            if (sourceCell == null)
                continue;

            if (targetCells.Contains(sourceCell))
                return true;
        }

        // Corner/edge touch counts.
        HashSet<TriangleNode> targetCorners = new();

        foreach (TriangleCell targetCell in targetCells)
        {
            if (targetCell == null || targetCell.corners == null)
                continue;

            foreach (TriangleNode corner in targetCell.corners)
            {
                if (corner != null)
                    targetCorners.Add(corner);
            }
        }

        foreach (TriangleCell sourceCell in sourceCells)
        {
            if (sourceCell == null || sourceCell.corners == null)
                continue;

            foreach (TriangleNode corner in sourceCell.corners)
            {
                if (corner != null && targetCorners.Contains(corner))
                    return true;
            }
        }

        return false;
    }

    private void RecalculateSupportNetwork(PlayerSide side)
    {
        if (board == null || grid == null)
            return;

        List<UnitPiece> sideUnits = new();

        foreach (UnitPiece unit in board.Units)
        {
            if (unit == null)
                continue;

            if (unit.Owner != side)
                continue;

            unit.SetSupportActive(false);

            if (unit.IsDefeated)
                continue;

            sideUnits.Add(unit);
        }

        HashSet<TriangleCell> startArea = new();
        AddStartAreaCells(side, startArea);

        HashSet<UnitPiece> connectedUnits = new();
        Queue<UnitPiece> queue = new();

        // Seed units: their BASE touches/overlaps the START AREA.
        foreach (UnitPiece unit in sideUnits)
        {
            if (CellsTouchOrOverlapCellSet(unit.OccupiedCells, startArea))
            {
                connectedUnits.Add(unit);
                queue.Enqueue(unit);
                unit.SetSupportActive(true);
            }
        }

        // Chain units: their BASE touches/overlaps CONNECTED support.
        while (queue.Count > 0)
        {
            UnitPiece connectedUnit = queue.Dequeue();

            HashSet<TriangleCell> connectedSupport = new();

            foreach (TriangleCell supportCell in connectedUnit.SupportCells)
            {
                if (supportCell == null)
                    continue;

                if (!supportCell.isActive)
                    continue;

                if (supportCell.isBlocked)
                    continue;

                connectedSupport.Add(supportCell);
            }

            foreach (UnitPiece candidate in sideUnits)
            {
                if (candidate == null)
                    continue;

                if (connectedUnits.Contains(candidate))
                    continue;

                if (CellsTouchOrOverlapCellSet(candidate.OccupiedCells, connectedSupport))
                {
                    connectedUnits.Add(candidate);
                    queue.Enqueue(candidate);
                    candidate.SetSupportActive(true);
                }
            }
        }

        Debug.Log(
            $"{side} support network: {connectedUnits.Count}/{sideUnits.Count} units connected."
        );
    }

    private bool FootprintTouchesControlledArea(
    IReadOnlyList<TriangleCell> footprintCells,
    HashSet<TriangleCell> controlledArea
)
    {
        controlledAreaCornerNodes.Clear();

        foreach (TriangleCell controlledCell in controlledArea)
        {
            if (controlledCell == null)
                continue;

            if (!controlledCell.isActive)
                continue;

            foreach (TriangleNode corner in controlledCell.corners)
            {
                if (corner != null)
                    controlledAreaCornerNodes.Add(corner);
            }
        }

        foreach (TriangleCell footprintCell in footprintCells)
        {
            if (footprintCell == null)
                continue;

            foreach (TriangleNode corner in footprintCell.corners)
            {
                if (corner != null && controlledAreaCornerNodes.Contains(corner))
                    return true;
            }
        }

        return false;
    }

    private HashSet<TriangleCell> GetActiveControlledArea(PlayerSide side)
    {
        controlledAreaCache.Clear();

        AddStartAreaCells(side, controlledAreaCache);

        if (board == null)
            return controlledAreaCache;

        foreach (UnitPiece unit in board.Units)
        {
            if (unit == null)
                continue;

            if (unit.Owner != side)
                continue;

            if (!unit.SupportActive)
                continue;

            foreach (TriangleCell supportCell in unit.SupportCells)
            {
                if (supportCell == null)
                    continue;

                if (!supportCell.isActive)
                    continue;

                if (supportCell.isBlocked)
                    continue;

                controlledAreaCache.Add(supportCell);
            }
        }

        return controlledAreaCache;
    }

    private void AddStartAreaCells(
    PlayerSide side,
    HashSet<TriangleCell> target
)
    {
        if (grid == null || target == null)
            return;

        MapRegion startRegion = side == PlayerSide.White
            ? MapRegion.WhiteStart
            : MapRegion.BlackStart;

        foreach (TriangleCell cell in grid.AllCells)
        {
            if (cell == null)
                continue;

            if (!cell.isActive)
                continue;

            if (cell.region == startRegion)
                target.Add(cell);
        }
    }

    public void TryPlaceSelectedUnit()
    {
        if (selectedUnit == null)
            return;

        if (ignorePointerOverUi && IsPointerOverUi())
            return;

        if (currentPreview == null || !currentPreviewIsValid)
        {
            Debug.Log("Placement blocked: no valid preview.");
            return;
        }

        UnitDefinition unitToPlace = selectedUnit;

        if (unitToPlace.unitPrefab == null)
        {
            Debug.LogWarning($"{unitToPlace.name} has no unit prefab assigned.");
            return;
        }

        UnitPiece unit = Instantiate(
            unitToPlace.unitPrefab,
            currentPreview.AnchorWorldPosition,
            Quaternion.identity,
            unitRoot
        );

        unit.Init(
            unitToPlace,
            currentPlayer,
            currentPreview
        );

        if (board != null)
            board.PlaceUnit(unit, currentPreview);

        if (gameFlow != null)
            gameFlow.RegisterPlacedUnit(unit);

        UnitFootprintFacing placedFacing = currentPreview.facing;

        RecalculateSupportNetwork(currentPlayer);

        currentPreview = null;
        currentPreviewIsValid = false;
        HideAnchorMarker();

        if (switchPlayerAfterSuccessfulPlacement)
            SwitchCurrentPlayer();
        
        selectedUnit = null;

        if (gameFlow != null && gameFlow.ShouldResolveConflictPhase())
        {
            gameFlow.ResolveConflictPhase();
        }
        else
        {
            RebuildPlacementVisuals();
        }

        Debug.Log(
            $"Placed {unitToPlace.DisplayName} for {currentPlayer}. Facing: {placedFacing}"
        );
    }

    private void ExpandPlacementZoneFromSupportCells(
    IReadOnlyList<TriangleCell> supportCells,
    PlayerSide side
)
    {
        HashSet<TriangleCell> expandedZone = side == PlayerSide.White
            ? whiteExpandedPlacementZone
            : blackExpandedPlacementZone;

        foreach (TriangleCell cell in supportCells)
        {
            if (cell == null)
                continue;

            if (!cell.isActive)
                continue;

            if (cell.isBlocked)
                continue;

            expandedZone.Add(cell);
        }
    }

    private void ShowCurrentPlacementZone()
    {
        RebuildPlacementVisuals();
    }

    void ClearPreview()
    {
        currentPreview = null;
        currentPreviewIsValid = false;
        HideAnchorMarker();

        RebuildPlacementVisuals();
    }

    void RefreshVisuals()
    {
        if (lineRenderer != null)
            lineRenderer.RefreshLineColors();
    }

    private HashSet<TriangleCell> GetCurrentPlacementZone()
    {
        return GetActiveControlledArea(currentPlayer);
    }

    HashSet<TriangleCell> BuildPlacementZoneForSide(PlayerSide side)
    {
        currentPlacementZoneCache.Clear();

        if (grid == null)
            return currentPlacementZoneCache;

        MapRegion startingRegion = side == PlayerSide.White
            ? MapRegion.WhiteStart
            : MapRegion.BlackStart;

        foreach (TriangleCell cell in grid.AllCells)
        {
            if (cell == null)
                continue;

            if (!cell.isActive)
                continue;

            if (cell.region == startingRegion)
                currentPlacementZoneCache.Add(cell);
        }

        HashSet<TriangleCell> expandedZone = side == PlayerSide.White
            ? whiteExpandedPlacementZone
            : blackExpandedPlacementZone;

        foreach (TriangleCell cell in expandedZone)
        {
            if (cell == null)
                continue;

            if (!cell.isActive)
                continue;

            if (cell.isBlocked)
                continue;

            currentPlacementZoneCache.Add(cell);
        }

        return currentPlacementZoneCache;
    }

    HashSet<TriangleCell> GetPlacementZone(PlayerSide side)
    {
        switch (side)
        {
            case PlayerSide.White:
                return whitePlacementZone;

            case PlayerSide.Black:
                return blackPlacementZone;

            default:
                return emptyPlacementZone;
        }
    }

    private void SwitchCurrentPlayer()
    {
        currentPlayer = currentPlayer == PlayerSide.White
            ? PlayerSide.Black
            : PlayerSide.White;

        currentFacing = GetDefaultFacingForPlayer(currentPlayer);

        Debug.Log($"Current player is now {currentPlayer}. Default facing: {currentFacing}");
    }

    Vector3 GetPointerWorldPosition()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;

        if (worldCamera == null)
            return Vector3.zero;

        Vector2 screenPosition = Vector2.zero;

        if (Mouse.current != null)
            screenPosition = Mouse.current.position.ReadValue();

        Ray ray = worldCamera.ScreenPointToRay(screenPosition);

        Plane boardPlane = new Plane(Vector3.forward, Vector3.zero);

        if (boardPlane.Raycast(ray, out float enter))
        {
            Vector3 world = ray.GetPoint(enter);
            world.z = 0f;
            return world;
        }

        return Vector3.zero;
    }

    bool IsPointerOverUi()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    void UpdateAnchorMarker(UnitPlacementResult placement, bool isValid)
    {
        if (!showAnchorMarker || placement == null || placement.anchorNode == null)
        {
            HideAnchorMarker();
            return;
        }

        CreateAnchorMarkerIfNeeded();

        Vector3 position = placement.anchorNode.worldPosition;
        position.z = anchorMarkerZ;

        anchorMarkerObject.transform.position = position;
        anchorMarkerObject.SetActive(true);

        Color color = isValid ? validAnchorMarkerColor : invalidAnchorMarkerColor;
        anchorMarkerLine.startColor = color;
        anchorMarkerLine.endColor = color;
    }

    void HideAnchorMarker()
    {
        if (anchorMarkerObject != null)
            anchorMarkerObject.SetActive(false);
    }

    void CreateAnchorMarkerIfNeeded()
    {
        if (anchorMarkerObject != null)
            return;

        anchorMarkerObject = new GameObject("Placement Anchor Marker");

        anchorMarkerLine = anchorMarkerObject.AddComponent<LineRenderer>();
        anchorMarkerLine.useWorldSpace = false;
        anchorMarkerLine.positionCount = anchorMarkerSegments + 1;

        anchorMarkerLine.startWidth = anchorMarkerLineWidth;
        anchorMarkerLine.endWidth = anchorMarkerLineWidth;

        anchorMarkerLine.sortingOrder = 1000;
        anchorMarkerLine.numCapVertices = 4;
        anchorMarkerLine.numCornerVertices = 4;

        if (anchorMarkerMaterial != null)
            anchorMarkerLine.sharedMaterial = anchorMarkerMaterial;

        for (int i = 0; i <= anchorMarkerSegments; i++)
        {
            float t = i / (float)anchorMarkerSegments;
            float angle = t * Mathf.PI * 2f;

            Vector3 point = new Vector3(
                Mathf.Cos(angle) * anchorMarkerRadius,
                Mathf.Sin(angle) * anchorMarkerRadius,
                0f
            );

            anchorMarkerLine.SetPosition(i, point);
        }

        anchorMarkerObject.SetActive(false);
    }

   void OnRotateFootprintInput(InputAction.CallbackContext context)
{
    Vector2 scroll = context.ReadValue<Vector2>();

    if (scroll.y > 0f)
        RotateFootprint(1);
    else if (scroll.y < 0f)
        RotateFootprint(-1);
}

void OnFacingUpInput(InputAction.CallbackContext context)
{
    SetFootprintFacing(UnitFootprintFacing.Up);
}

void OnFacingUpRightInput(InputAction.CallbackContext context)
{
    SetFootprintFacing(UnitFootprintFacing.UpRight);
}

void OnFacingDownRightInput(InputAction.CallbackContext context)
{
    SetFootprintFacing(UnitFootprintFacing.DownRight);
}

void OnFacingDownInput(InputAction.CallbackContext context)
{
    SetFootprintFacing(UnitFootprintFacing.Down);
}

void OnFacingDownLeftInput(InputAction.CallbackContext context)
{
    SetFootprintFacing(UnitFootprintFacing.DownLeft);
}

void OnFacingUpLeftInput(InputAction.CallbackContext context)
{
    SetFootprintFacing(UnitFootprintFacing.UpLeft);
}

void RotateFootprint(int direction)
{
    int value = (int)currentFacing;
    value += direction;

    if (value < 0)
        value = 5;

    if (value > 5)
        value = 0;

    SetFootprintFacing((UnitFootprintFacing)value);
}

void SetFootprintFacing(UnitFootprintFacing facing)
{
    if (currentFacing == facing)
        return;

    currentFacing = facing;

    Debug.Log($"Footprint facing: {currentFacing}");

    if (selectedUnit != null)
        UpdatePreviewFromPointer();
}

    public void KillUnit(UnitPiece unit)
    {
        if (unit == null)
            return;

        PlayerSide owner = unit.Owner;

        if (board != null)
            board.RemoveUnit(unit);

        Destroy(unit.gameObject);

        RecalculateSupportNetwork(owner);

        currentPreview = null;
        currentPreviewIsValid = false;
        HideAnchorMarker();

        RebuildPlacementVisuals();
    }

    private void RebuildPlacementVisuals()
    {
        if (isRebuildingPlacementVisuals)
            return;

        isRebuildingPlacementVisuals = true;

        if (placementVisuals != null)
        {
            placementVisuals.Refresh(
                currentPlayer,
                currentPreview,
                currentPreviewIsValid
            );
        }

        isRebuildingPlacementVisuals = false;
    }

    private bool TryGetHoveredUnit(out UnitPiece hoveredUnit)
{
    hoveredUnit = null;

    if (board == null || grid == null)
        return false;

    if (ignorePointerOverUi && IsPointerOverUi())
        return false;

    Vector3 pointerWorld = GetPointerWorldPosition();

    float cellSnapDistance = 0.25f;

    if (grid.MapDefinition != null)
        cellSnapDistance = grid.MapDefinition.sideLength * 0.6f;

    TriangleCell hoveredCell = grid.FindClosestCellCenter(
        pointerWorld,
        cellSnapDistance
    );

    if (hoveredCell == null)
        return false;

    hoveredUnit = board.GetUnit(hoveredCell);

    return hoveredUnit != null;
}

private void TryKillHoveredUnitForDebug()
{
    if (!TryGetHoveredUnit(out UnitPiece hoveredUnit))
    {
        Debug.Log("No unit under pointer to kill.");
        return;
    }

    Debug.Log($"Debug killing hovered unit: {hoveredUnit.name}");

    KillUnit(hoveredUnit);
}

public void RecalculateAllSupportNetworks()
{
    RecalculateSupportNetwork(PlayerSide.White);
    RecalculateSupportNetwork(PlayerSide.Black);

    RebuildPlacementVisuals();
}

private UnitFootprintFacing GetDefaultFacingForPlayer(PlayerSide side)
{
    return side == PlayerSide.White
        ? UnitFootprintFacing.Up
        : UnitFootprintFacing.Down;
}
}