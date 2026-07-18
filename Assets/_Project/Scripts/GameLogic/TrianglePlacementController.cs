using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections;

public class TrianglePlacementController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TriangleGridManager grid;
    [SerializeField] private TriangleLineRenderer lineRenderer;
    [SerializeField] private TriangleBoard board;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Transform unitRoot;

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

    private UnitPlacementResult currentPreview;
    private bool currentPreviewIsValid;

    public bool HasSelectedUnit => selectedUnit != null;
    public PlayerSide CurrentPlayer => currentPlayer;

    private bool placeRequested;
    private bool cancelRequested;

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

        if (placeRequested)
        {
            placeRequested = false;
            TryPlaceSelectedUnit();
        }

        if (cancelRequested)
        {
            cancelRequested = false;
            ClearSelectedUnit();
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
        currentFacing = UnitFootprintFacing.Up;

        if (selectedUnit != null)
            Debug.Log($"Selected unit: {selectedUnit.unitName}");

        Debug.Log(
        $"Selected unit: {selectedUnit.unitName} | " +
        $"Anchor: {selectedUnit.anchorType} | " +
        $"Base cells: {(selectedUnit.baseSize == null ? -1 : selectedUnit.baseSize.Count)} | " +
        $"Support cells: {(selectedUnit.supportRange == null ? -1 : selectedUnit.supportRange.Count)} | " +
        $"Current player: {currentPlayer} | " +
        $"Current zone cells: {GetCurrentPlacementZone().Count}"
    );

        ShowCurrentPlacementZone();
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

            ClearPreview();
            ShowCurrentPlacementZone();

            DebugPreview("Pointer is over UI, preview blocked.");
            return;
        }

        Vector3 pointerWorld = GetPointerWorldPosition();

        ClearPreview();

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
            currentPreviewIsValid = false;

            ShowCurrentPlacementZone();
            RefreshVisuals();

            DebugPreview(
                $"No geometry preview. Mouse world: {pointerWorld}, " +
                $"Anchor: {selectedUnit.anchorType}, MaxSnapDistance: {MaxSnapDistance}"
            );

            return;
        }

        currentPreviewIsValid = CanPlaceUnit(selectedUnit, currentPreview);

        if (currentPreviewIsValid)
        {
            ShowValidPreview(currentPreview);
            UpdateAnchorMarker(currentPreview, true);
        }
        else
        {
            ShowInvalidPreview(currentPreview);
            UpdateAnchorMarker(currentPreview, false);
        }

        if (!hasGeometryPreview || currentPreview == null)
        {
            currentPreviewIsValid = false;

            HideAnchorMarker();
            ShowCurrentPlacementZone();
            RefreshVisuals();

            return;
        }

        RefreshVisuals();
    }

    void DebugPreview(string message)
    {
        if (Time.time < nextPreviewDebugTime)
            return;

        nextPreviewDebugTime = Time.time + 0.5f;
        Debug.Log($"[Placement Preview] {message}");
    }

    void ShowInvalidPreview(UnitPlacementResult placement)
    {
        if (placement == null)
            return;

        foreach (TriangleCell supportCell in placement.supportCells)
        {
            if (supportCell == null)
                continue;

            supportCell.SetWholeVisualState(TriangleNodeVisualState.Hover, true);
        }

        foreach (TriangleCell baseCell in placement.baseCells)
        {
            if (baseCell == null)
                continue;

            baseCell.SetWholeVisualState(TriangleNodeVisualState.Invalid, true);
        }

        if (placement.anchorNode != null)
            placement.anchorNode.SetState(TriangleNodeVisualState.Invalid, true);
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

    public bool CanPlaceUnit(UnitDefinition unitDef, UnitPlacementResult placement)
    {
        if (unitDef == null || placement == null)
            return false;

        foreach (TriangleCell cell in placement.baseCells)
        {
            if (!IsBaseCellValidForCurrentPlayer(cell))
                return false;
        }

        return true;
    }

    public void TryPlaceSelectedUnit()
    {
        Debug.Log(
        $"TryPlaceSelectedUnit | " +
        $"selectedUnit: {(selectedUnit == null ? "null" : selectedUnit.unitName)} | " +
        $"preview valid: {currentPreviewIsValid} | " +
        $"preview null: {currentPreview == null}");
        if (selectedUnit == null)
            return;

        if (ignorePointerOverUi && IsPointerOverUi())
            return;

        if (!currentPreviewIsValid || currentPreview == null)
            return;

        if (!CanPlaceUnit(selectedUnit, currentPreview))
            return;

        UnitDefinition unitToPlace = selectedUnit;

        UnitPiece prefab = unitToPlace.unitPrefab;

        if (prefab == null)
        {
            Debug.LogError($"{unitToPlace.name} has no UnitPiece prefab assigned.");
            return;
        }

        UnitPiece unit = Instantiate(
            prefab,
            currentPreview.AnchorWorldPosition,
            Quaternion.identity,
            unitRoot
        );

        unit.Init(unitToPlace, currentPlayer, currentPreview);

        if (board != null)
            board.PlaceUnit(unit, currentPreview);

        ExpandPlacementZoneFromSupportCells(
            currentPreview.supportCells,
            currentPlayer
        );

        Debug.Log($"{currentPlayer} placed {unitToPlace.unitName} at {currentPreview.anchorCell.coord}");

        selectedUnit = null;
        currentPreview = null;
        currentPreviewIsValid = false;

        ClearPreview();
        ShowCurrentPlacementZone();
    }

    void ExpandPlacementZoneFromSupportCells(
    List<TriangleCell> supportCells,
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

    void ShowCurrentPlacementZone()
    {
        ClearPreview();

        foreach (TriangleCell cell in GetCurrentPlacementZone())
        {
            if (cell == null || !cell.isActive)
                continue;

            cell.SetWholeVisualState(TriangleNodeVisualState.Placement, true);
        }

        RefreshVisuals();
    }

    void ShowValidPreview(UnitPlacementResult placement)
    {
        foreach (TriangleCell cell in placement.supportCells)
        {
            if (cell == null)
                continue;

            cell.SetWholeVisualState(TriangleNodeVisualState.Hover, true);
        }

        foreach (TriangleCell cell in placement.baseCells)
        {
            if (cell == null)
                continue;

            cell.SetWholeVisualState(TriangleNodeVisualState.Footprint, true);
        }

        if (placement.anchorNode != null)
            placement.anchorNode.SetState(TriangleNodeVisualState.Placement, true);
    }

    void ClearPreview()
    {
        if (grid != null)
            grid.ClearAllNodeVisualStates();
    }

    void RefreshVisuals()
    {
        if (lineRenderer != null)
            lineRenderer.RefreshLineColors();
    }

    HashSet<TriangleCell> GetCurrentPlacementZone()
    {
        return BuildPlacementZoneForSide(currentPlayer);
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

    public void EndTurn()
    {
        currentPlayer = currentPlayer == PlayerSide.White
            ? PlayerSide.Black
            : PlayerSide.White;

        selectedUnit = null;
        currentPreview = null;
        currentPreviewIsValid = false;

        ClearPreview();
        ShowCurrentPlacementZone();

        Debug.Log($"Turn switched to {currentPlayer}");
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
}