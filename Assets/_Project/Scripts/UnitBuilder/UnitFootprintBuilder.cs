using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum UnitBuilderPaintMode
{
    Select,
    Deselect
}

public class UnitFootprintBuilder : MonoBehaviour
{
    [Header("Unit Asset")]
    [SerializeField] private UnitDefinition unitDefinition;

    [Header("Optional Geometry Source")]
    [Tooltip("Optional. Used for triangle side length and base orientation settings.")]
    [SerializeField] private TriangleMapDefinition geometrySource;

    [Header("Builder Grid Size")]
    [SerializeField] private int columns = 21;
    [SerializeField] private int rows = 21;
    [SerializeField] private float fallbackSideLength = 1f;
    [SerializeField] private bool firstColumnIsUp = true;
    [SerializeField] private bool offsetEveryOtherRow = true;

    [Header("Scene References")]
    [SerializeField] private Transform gridRoot;
    [SerializeField] private Material triangleMaterial;
    [SerializeField] private Material outlineMaterial;

    [Header("Grid Visuals")]
    [SerializeField] private float outlineWidth = 0.015f;
    [SerializeField] private Color outlineColor = new Color(0.05f, 0.05f, 0.05f, 1f);
    [SerializeField] private Color hoverOutlineColor = Color.yellow;

    [Header("Colors")]
    [SerializeField] private Color emptyUpColor = new Color(0.18f, 0.18f, 0.18f, 1f);
    [SerializeField] private Color emptyDownColor = new Color(0.26f, 0.26f, 0.26f, 1f);

    [SerializeField] private Color baseUpColor = new Color(0.02f, 0.02f, 0.02f, 1f);
    [SerializeField] private Color baseDownColor = new Color(0.08f, 0.08f, 0.08f, 1f);

    [SerializeField] private Color supportUpColor = new Color(0.78f, 0.78f, 0.78f, 1f);
    [SerializeField] private Color supportDownColor = new Color(0.9f, 0.9f, 0.9f, 1f);

    [SerializeField] private Color anchorPreviewColor = Color.yellow;

    [Header("Current Builder State")]
    [SerializeField] private UnitFootprintArea currentArea = UnitFootprintArea.BaseSize;
    [SerializeField] private UnitBuilderPaintMode paintMode = UnitBuilderPaintMode.Select;
    [SerializeField] private UnitAnchorType anchorType = UnitAnchorType.TriangleCenter;

    [Header("Mirror Painting")]
    [SerializeField] private bool mirrorPaintingEnabled = false;

    [Header("Input Actions")]
    [SerializeField] private bool enableActionsManually = true;

    [SerializeField] private InputActionReference baseSizeAction;
    [SerializeField] private InputActionReference supportRangeAction;
    [SerializeField] private InputActionReference finishedAction;

    [SerializeField] private InputActionReference selectAction;
    [SerializeField] private InputActionReference deselectAction;
    [SerializeField] private InputActionReference fillAction;
    [SerializeField] private InputActionReference mirrorAction;
    [SerializeField] private InputActionReference undoAction;
    [SerializeField] private InputActionReference clearAction;

    [SerializeField] private InputActionReference anchorCenterAction;
    [SerializeField] private InputActionReference anchorCornerAction;
    [SerializeField] private InputActionReference anchorSideMidpointAction;
    [SerializeField] private InputActionReference triangleFacingAction;

    [Header("Painting Input")]
    [SerializeField] private InputActionReference paintAction;

    [Header("Center Marker")]
    [SerializeField] private bool showCenterMarker = true;
    [SerializeField] private float centerMarkerRadius = 0.06f;
    [SerializeField] private int centerMarkerSegments = 24;
    [SerializeField] private Color centerMarkerColor = Color.yellow;
    [SerializeField] private Material centerMarkerMaterial;

    private GameObject centerMarkerObject;

    [Header("UI Button Visuals")]
    [SerializeField] private Color buttonInactiveColor = new Color(0.55f, 0.55f, 0.55f, 1f);
    [SerializeField] private Color buttonActiveColor = new Color(1f, 0.85f, 0.25f, 1f);
    [SerializeField] private Color mirrorActiveColor = new Color(0.25f, 0.9f, 1f, 1f);
    [SerializeField] private Color upOrientationColor = Color.darkGray;
    [SerializeField] private Color downOrientationColor = Color.lightGray;

    [SerializeField] private Button baseSizeButton;
    [SerializeField] private Button supportRangeButton;

    [SerializeField] private Button selectButton;
    [SerializeField] private Button deselectButton;
    [SerializeField] private Button mirrorButton;

    [SerializeField] private Button anchorCenterButton;
    [SerializeField] private Button anchorCornerButton;
    [SerializeField] private Button anchorSideMidpointButton;
    [SerializeField] private Button triangleFacingButton;
    [SerializeField] private TMPro.TMP_Text anchorOrientationButtonText;

    [Header("Default Builder State")]
    [SerializeField] private UnitAnchorType fallbackAnchorType = UnitAnchorType.TriangleCenter;
    [SerializeField] private bool showCenterMarkerByDefault = true;

    [SerializeField] private TriangleOrientation editedAnchorOrientation = TriangleOrientation.Up;

    private readonly Stack<BuilderSnapshot> undoStack = new();

    private readonly Dictionary<Vector2Int, UnitBuilderTriangleView> views = new();
    private readonly HashSet<Vector2Int> baseSelection = new();
    private readonly HashSet<Vector2Int> supportSelection = new();

    private Vector2Int AnchorCoord => new Vector2Int(columns / 2, rows / 2);

    private float SideLength => geometrySource != null ? geometrySource.sideLength : fallbackSideLength;
    private float TriangleHeight => SideLength * Mathf.Sqrt(3f) * 0.5f;

    private Vector2Int? hoveredCoord;

    private struct BuilderSnapshot
    {
        public HashSet<Vector2Int> baseCells;
        public HashSet<Vector2Int> supportCells;

        public BuilderSnapshot(HashSet<Vector2Int> baseCells, HashSet<Vector2Int> supportCells)
        {
            this.baseCells = new HashSet<Vector2Int>(baseCells);
            this.supportCells = new HashSet<Vector2Int>(supportCells);
        }
    }

    private void OnEnable()
    {
        RegisterInputActions(true);
    }

    private void OnDisable()
    {
        RegisterInputActions(false);
    }
    private void Start()
    {
        ApplyDefaultToolState();
        LoadFromUnitDefinition();
    }

    [ContextMenu("Rebuild Builder Grid")]
    public void BuildGrid()
    {
        if (gridRoot == null)
            gridRoot = transform;

        ClearGridObjects();
        views.Clear();

        Vector3 anchorWorldOffset = GetAnchorPointWorldPosition(AnchorCoord, anchorType);

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                Vector2Int coord = new Vector2Int(col, row);
                CreateTriangleView(coord, anchorWorldOffset);
            }
        }

        CreateOrRefreshCenterMarker();
        RefreshAllColors();
        RefreshUiState();
    }

    void CreateTriangleView(Vector2Int coord, Vector3 anchorWorldOffset)
    {
        TriangleOrientation orientation = GetOrientation(coord);
        Vector3[] corners = GetCornerPositions(coord, orientation);

        for (int i = 0; i < corners.Length; i++)
            corners[i] -= anchorWorldOffset;

        GameObject triangleObject = new GameObject($"BuilderTriangle_{coord.x}_{coord.y}");
        triangleObject.transform.SetParent(gridRoot, false);

        Mesh mesh = CreateTriangleMesh(corners);

        MeshFilter meshFilter = triangleObject.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;

        MeshRenderer meshRenderer = triangleObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = triangleMaterial;

        PolygonCollider2D collider = triangleObject.AddComponent<PolygonCollider2D>();
        collider.points = new[]
        {
            new Vector2(corners[0].x, corners[0].y),
            new Vector2(corners[1].x, corners[1].y),
            new Vector2(corners[2].x, corners[2].y)
        };

        UnitBuilderTriangleView view = triangleObject.AddComponent<UnitBuilderTriangleView>();

        view.Initialize(
            this,
            coord,
            corners,
            outlineMaterial,
            outlineWidth
        );

        views[coord] = view;
    }

    Mesh CreateTriangleMesh(Vector3[] corners)
    {
        Mesh mesh = new Mesh();
        mesh.name = "Builder Triangle Mesh";

        mesh.vertices = corners;

        float signedArea =
            (corners[0].x * (corners[1].y - corners[2].y)) +
            (corners[1].x * (corners[2].y - corners[0].y)) +
            (corners[2].x * (corners[0].y - corners[1].y));

        if (signedArea >= 0f)
            mesh.triangles = new[] { 0, 1, 2 };
        else
            mesh.triangles = new[] { 0, 2, 1 };

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    void ClearGridObjects()
    {
        if (gridRoot == null)
            return;

        for (int i = gridRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = gridRoot.GetChild(i);

            if (centerMarkerObject != null && child.gameObject == centerMarkerObject)
                continue;

            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
    }

    TriangleOrientation GetOrientation(Vector2Int coord)
    {
        TriangleOrientation rawOrientation = GetRawOrientation(coord);

        if (!ShouldFlipBuilderOrientation())
            return rawOrientation;

        return rawOrientation == TriangleOrientation.Up
            ? TriangleOrientation.Down
            : TriangleOrientation.Up;
    }

    bool IsOffsetRow(int row)
    {
        if (geometrySource != null)
            return geometrySource.IsOffsetRow(row);

        if (!offsetEveryOtherRow)
            return false;

        return Mathf.Abs(row % 2) == 1;
    }

    Vector3[] GetCornerPositions(Vector2Int coord, TriangleOrientation orientation)
    {
        float side = SideLength;
        float halfSide = side * 0.5f;
        float height = TriangleHeight;

        float x = coord.x * halfSide;
        float y = coord.y * height;

        if (IsOffsetRow(coord.y))
            x += halfSide;

        if (orientation == TriangleOrientation.Up)
        {
            return new[]
            {
                new Vector3(x, y, 0f),
                new Vector3(x + side, y, 0f),
                new Vector3(x + halfSide, y + height, 0f)
            };
        }

        return new[]
        {
            new Vector3(x, y + height, 0f),
            new Vector3(x + side, y + height, 0f),
            new Vector3(x + halfSide, y, 0f)
        };
    }

    Vector3 GetAnchorPointWorldPosition(Vector2Int anchorCoord, UnitAnchorType type)
    {
        TriangleOrientation orientation = GetOrientation(anchorCoord);
        Vector3[] corners = GetCornerPositions(anchorCoord, orientation);

        switch (type)
        {
            case UnitAnchorType.Corner:
                return corners[0];

            case UnitAnchorType.SideMidpoint:
                return (corners[0] + corners[1]) * 0.5f;

            case UnitAnchorType.TriangleCenter:
            default:
                return (corners[0] + corners[1] + corners[2]) / 3f;
        }
    }

    public void PaintTriangle(Vector2Int coord)
    {
        List<Vector2Int> paintCoords = GetPaintCoords(coord);

        if (!WouldPaintChangeAnything(paintCoords))
            return;

        PushUndo();

        foreach (Vector2Int paintCoord in paintCoords)
            ApplyPaintToTriangle(paintCoord);

        foreach (Vector2Int paintCoord in paintCoords)
            RefreshTriangleColor(paintCoord);
    }

    bool WouldPaintChangeAnything(List<Vector2Int> coords)
    {
        foreach (Vector2Int coord in coords)
        {
            if (WouldPaintChangeTriangle(coord))
                return true;
        }

        return false;
    }

    bool WouldPaintChangeTriangle(Vector2Int coord)
    {
        if (!views.ContainsKey(coord))
            return false;

        bool isEditingSupport = currentArea == UnitFootprintArea.SupportRange;

        if (isEditingSupport && paintMode == UnitBuilderPaintMode.Select)
        {
            if (baseSelection.Contains(coord))
                return false;
        }

        HashSet<Vector2Int> target = GetCurrentSelection();

        bool alreadySelected = target.Contains(coord);

        if (paintMode == UnitBuilderPaintMode.Select)
            return !alreadySelected;

        if (paintMode == UnitBuilderPaintMode.Deselect)
            return alreadySelected;

        return false;
    }

    void ApplyPaintToTriangle(Vector2Int coord)
    {
        if (!WouldPaintChangeTriangle(coord))
            return;

        bool isEditingBase = currentArea == UnitFootprintArea.BaseSize;

        HashSet<Vector2Int> target = GetCurrentSelection();

        if (paintMode == UnitBuilderPaintMode.Select)
        {
            target.Add(coord);

            if (isEditingBase)
                supportSelection.Remove(coord);
        }
        else
        {
            target.Remove(coord);
        }
    }
    HashSet<Vector2Int> GetCurrentSelection()
    {
        return currentArea == UnitFootprintArea.BaseSize
            ? baseSelection
            : supportSelection;
    }

    void PushUndo()
    {
        undoStack.Push(new BuilderSnapshot(baseSelection, supportSelection));
    }

    public void Undo()
    {
        if (undoStack.Count == 0)
            return;

        BuilderSnapshot snapshot = undoStack.Pop();

        baseSelection.Clear();
        supportSelection.Clear();

        foreach (Vector2Int coord in snapshot.baseCells)
            baseSelection.Add(coord);

        foreach (Vector2Int coord in snapshot.supportCells)
            supportSelection.Add(coord);

        RefreshAllColors();
    }

    public void FillCurrentRows()
    {
        HashSet<Vector2Int> target = GetCurrentSelection();

        if (target.Count == 0)
            return;

        PushUndo();

        bool isEditingSupport = currentArea == UnitFootprintArea.SupportRange;
        bool isEditingBase = currentArea == UnitFootprintArea.BaseSize;

        Dictionary<int, List<int>> columnsByRow = new();

        foreach (Vector2Int coord in target)
        {
            if (!columnsByRow.TryGetValue(coord.y, out List<int> cols))
            {
                cols = new List<int>();
                columnsByRow[coord.y] = cols;
            }

            cols.Add(coord.x);
        }

        foreach (KeyValuePair<int, List<int>> rowPair in columnsByRow)
        {
            int row = rowPair.Key;
            List<int> cols = rowPair.Value;

            int min = cols[0];
            int max = cols[0];

            foreach (int col in cols)
            {
                if (col < min) min = col;
                if (col > max) max = col;
            }

            for (int col = min; col <= max; col++)
            {
                Vector2Int coord = new Vector2Int(col, row);

                TryAddToCurrentArea(coord, isEditingBase, isEditingSupport);
            }
        }

        if (mirrorPaintingEnabled)
            MirrorCurrentAreaIntoItself();

        RefreshAllColors();
    }

    void TryAddToCurrentArea(Vector2Int coord, bool isEditingBase, bool isEditingSupport)
{
    if (!views.ContainsKey(coord))
        return;

    if (isEditingSupport && baseSelection.Contains(coord))
        return;

    HashSet<Vector2Int> target = GetCurrentSelection();

    target.Add(coord);

    if (isEditingBase)
        supportSelection.Remove(coord);
}

public void MirrorCurrentAreaIntoItself()
{
    HashSet<Vector2Int> target = GetCurrentSelection();

    if (target.Count == 0)
        return;

    bool isEditingSupport = currentArea == UnitFootprintArea.SupportRange;
    bool isEditingBase = currentArea == UnitFootprintArea.BaseSize;

    List<Vector2Int> original = new(target);

    foreach (Vector2Int coord in original)
    {
        Vector2Int mirrored = GetMirroredCoord(coord);

        if (!views.ContainsKey(mirrored))
            continue;

        if (isEditingSupport && baseSelection.Contains(mirrored))
            continue;

        target.Add(mirrored);

        if (isEditingBase)
            supportSelection.Remove(mirrored);
    }

    RefreshAllColors();
}

    public void SaveToUnitDefinition()
    {
        if (unitDefinition == null)
        {
            Debug.LogError("No UnitDefinition assigned to UnitFootprintBuilder.");
            return;
        }

        supportSelection.RemoveWhere(coord => baseSelection.Contains(coord));

        unitDefinition.anchorType = anchorType;

        unitDefinition.SetFootprint(
            UnitFootprintArea.BaseSize,
            editedAnchorOrientation,
            ConvertSelectionToOffsets(baseSelection)
        );

        unitDefinition.SetFootprint(
            UnitFootprintArea.SupportRange,
            editedAnchorOrientation,
            ConvertSelectionToOffsets(supportSelection)
        );

#if UNITY_EDITOR
        EditorUtility.SetDirty(unitDefinition);
        AssetDatabase.SaveAssets();
#endif

        Debug.Log(
            $"Saved {editedAnchorOrientation} unit footprint to {unitDefinition.name}. " +
            $"Base cells: {GetCurrentSavedBaseCount()}, " +
            $"Support cells: {GetCurrentSavedSupportCount()}, " +
            $"Anchor: {unitDefinition.anchorType}"
        );
    }

    int GetCurrentSavedBaseCount()
    {
        if (unitDefinition == null)
            return 0;

        return unitDefinition.GetEditableFootprint(
            UnitFootprintArea.BaseSize,
            editedAnchorOrientation
        ).Count;
    }

    int GetCurrentSavedSupportCount()
    {
        if (unitDefinition == null)
            return 0;

        return unitDefinition.GetEditableFootprint(
            UnitFootprintArea.SupportRange,
            editedAnchorOrientation
        ).Count;
    }

    List<TriangleFootprintCell> ConvertSelectionToOffsets(HashSet<Vector2Int> selection)
    {
        List<TriangleFootprintCell> result = new();

        int anchorProfileX = GetBuilderProfileColumn(AnchorCoord);

        foreach (Vector2Int coord in selection)
        {
            int profileX = GetBuilderProfileColumn(coord);

            int offsetX = profileX - anchorProfileX;
            int offsetY = coord.y - AnchorCoord.y;

            result.Add(new TriangleFootprintCell(offsetX, offsetY));
        }

        return result;
    }

    int GetBuilderProfileColumn(Vector2Int coord)
    {
        if (IsOffsetRow(coord.y))
            return coord.x + 1;

        return coord.x;
    }

    public void LoadFromUnitDefinition()
    {
        baseSelection.Clear();
        supportSelection.Clear();
        undoStack.Clear();

        if (unitDefinition != null)
            anchorType = unitDefinition.anchorType;
        else
            anchorType = fallbackAnchorType;

        showCenterMarker = showCenterMarkerByDefault;

        BuildGrid();

        if (unitDefinition != null)
        {
            LoadFootprintIntoSelection(unitDefinition.GetEditableFootprint(UnitFootprintArea.BaseSize, editedAnchorOrientation), baseSelection);

            LoadFootprintIntoSelection(unitDefinition.GetEditableFootprint(UnitFootprintArea.SupportRange, editedAnchorOrientation), supportSelection);
        }

        supportSelection.RemoveWhere(coord => baseSelection.Contains(coord));

        CreateOrRefreshCenterMarker();
        RefreshAllColors();
        RefreshUiState();
    }

    void LoadFootprintIntoSelection(
    List<TriangleFootprintCell> footprint,
    HashSet<Vector2Int> target
)
    {
        if (footprint == null)
            return;

        int anchorProfileX = GetBuilderProfileColumn(AnchorCoord);

        foreach (TriangleFootprintCell cell in footprint)
        {
            int row = AnchorCoord.y + cell.y;
            int profileX = anchorProfileX + cell.x;

            Vector2Int coord = GetBuilderCoordFromProfileColumn(profileX, row);

            if (views.ContainsKey(coord))
                target.Add(coord);
        }
    }

    Vector2Int GetBuilderCoordFromProfileColumn(int profileColumn, int row)
    {
        if (IsOffsetRow(row))
            return new Vector2Int(profileColumn - 1, row);

        return new Vector2Int(profileColumn, row);
    }

    void RefreshAllColors()
    {
        foreach (Vector2Int coord in views.Keys)
            RefreshTriangleColor(coord);
    }

    void RefreshTriangleColor(Vector2Int coord)
    {
        if (!views.TryGetValue(coord, out UnitBuilderTriangleView view))
            return;

        bool isBase = baseSelection.Contains(coord);
        bool isSupport = supportSelection.Contains(coord);
        bool isHovered = hoveredCoord.HasValue && hoveredCoord.Value == coord;

        TriangleOrientation orientation = GetOrientation(coord);
        bool isUp = orientation == TriangleOrientation.Up;

        Color fillColor;

        if (isBase)
        {
            fillColor = isUp ? baseUpColor : baseDownColor;
        }
        else if (isSupport)
        {
            fillColor = isUp ? supportUpColor : supportDownColor;
        }
        else
        {
            fillColor = isUp ? emptyUpColor : emptyDownColor;
        }

        view.SetFillColor(fillColor);

        if (isHovered)
            view.SetOutlineColor(hoverOutlineColor);
        else
            view.SetOutlineColor(outlineColor);
    }

    public void SetTargetBaseSize()
    {
        currentArea = UnitFootprintArea.BaseSize;
        RefreshUiState();
    }

    public void SetTargetSupportRange()
    {
        currentArea = UnitFootprintArea.SupportRange;
        RefreshUiState();
    }

    public void SetPaintSelect()
    {
        paintMode = UnitBuilderPaintMode.Select;
        RefreshUiState();
    }

    public void SetPaintDeselect()
    {
        paintMode = UnitBuilderPaintMode.Deselect;
        RefreshUiState();
    }

    public void SetAnchorTriangleCenter()
    {
        SetAnchorType(UnitAnchorType.TriangleCenter);
    }

    public void SetAnchorCorner()
    {
        SetAnchorType(UnitAnchorType.Corner);
        BuildGrid();
        RefreshUiState();
    }

    public void ToggleTriangleFacing()
    {
        editedAnchorOrientation = editedAnchorOrientation == TriangleOrientation.Up
            ? TriangleOrientation.Down
            : TriangleOrientation.Up;

        LoadFromUnitDefinition();
    }

    public void SetAnchorSideMidpoint()
    {
        SetAnchorType(UnitAnchorType.SideMidpoint);
    }

    private void SetAnchorType(UnitAnchorType newAnchorType)
    {
        if (anchorType == newAnchorType)
        {
            ToggleCenterMarker();
            return;
        }

        anchorType = newAnchorType;
        showCenterMarker = true;

        BuildGrid();
        CreateOrRefreshCenterMarker();
        RefreshUiState();
    }

    public void ToggleCenterMarker()
    {
        showCenterMarker = !showCenterMarker;

        CreateOrRefreshCenterMarker();
        RefreshUiState();
    }

    public void SetUnitDefinition(UnitDefinition definition)
    {
        unitDefinition = definition;

        ApplyDefaultToolState();
        LoadFromUnitDefinition();
    }

    public void SetHoveredTriangle(Vector2Int coord)
    {
        if (hoveredCoord.HasValue && hoveredCoord.Value == coord)
            return;

        Vector2Int? oldHover = hoveredCoord;
        hoveredCoord = coord;

        if (oldHover.HasValue)
            RefreshTriangleColor(oldHover.Value);

        RefreshTriangleColor(coord);
    }

    public void ClearHoveredTriangle(Vector2Int coord)
    {
        if (!hoveredCoord.HasValue)
            return;

        if (hoveredCoord.Value != coord)
            return;

        hoveredCoord = null;
        RefreshTriangleColor(coord);
    }

    public void ClearCurrentArea()
    {
        HashSet<Vector2Int> target = GetCurrentSelection();

        if (target.Count == 0)
            return;

        PushUndo();

        target.Clear();

        RefreshAllColors();
    }

    public void ToggleMirrorPainting()
    {
        mirrorPaintingEnabled = !mirrorPaintingEnabled;
        RefreshUiState();

        Debug.Log($"Mirror painting: {(mirrorPaintingEnabled ? "ON" : "OFF")}");
    }

    public void SetMirrorPainting(bool enabled)
    {
        mirrorPaintingEnabled = enabled;
        RefreshUiState();
    }

    Vector2Int GetMirroredCoord(Vector2Int coord)
    {
        Vector2Int local = coord - AnchorCoord;
        Vector2Int mirroredLocal = new Vector2Int(-local.x, local.y);

        return AnchorCoord + mirroredLocal;
    }

    List<Vector2Int> GetPaintCoords(Vector2Int coord)
    {
        List<Vector2Int> result = new();

        if (views.ContainsKey(coord))
            result.Add(coord);

        if (mirrorPaintingEnabled)
        {
            Vector2Int mirrored = GetMirroredCoord(coord);

            if (mirrored != coord && views.ContainsKey(mirrored))
                result.Add(mirrored);
        }

        return result;
    }

    void RegisterInputActions(bool register)
    {
        RegisterAction(baseSizeAction, OnBaseSizeInput, register);
        RegisterAction(supportRangeAction, OnSupportRangeInput, register);
        RegisterAction(finishedAction, OnFinishedInput, register);

        RegisterAction(selectAction, OnSelectInput, register);
        RegisterAction(deselectAction, OnDeselectInput, register);
        RegisterAction(fillAction, OnFillInput, register);
        RegisterAction(mirrorAction, OnMirrorInput, register);
        RegisterAction(undoAction, OnUndoInput, register);
        RegisterAction(clearAction, OnClearInput, register);

        RegisterAction(anchorCenterAction, OnAnchorCenterInput, register);
        RegisterAction(anchorCornerAction, OnAnchorCornerInput, register);
        RegisterAction(anchorSideMidpointAction, OnAnchorSideMidpointInput, register);
        RegisterAction(triangleFacingAction, OnTriangleFacingInput, register);

        RegisterAction(paintAction, null, register);
    }

    void RegisterAction(
        InputActionReference actionReference,
        System.Action<InputAction.CallbackContext> callback,
        bool register
    )
    {
        if (actionReference == null || actionReference.action == null)
            return;

        InputAction action = actionReference.action;

        if (register)
        {
            if (callback != null)
                action.performed += callback;

            if (enableActionsManually)
                action.Enable();
        }
        else
        {
            if (callback != null)
                action.performed -= callback;

            if (enableActionsManually)
                action.Disable();
        }
    }

    void OnTriangleFacingInput(InputAction.CallbackContext context)
    {
        ToggleTriangleFacing();
    }

    void OnBaseSizeInput(InputAction.CallbackContext context)
    {
        SetTargetBaseSize();
    }

    void OnSupportRangeInput(InputAction.CallbackContext context)
    {
        SetTargetSupportRange();
    }

    void OnFinishedInput(InputAction.CallbackContext context)
    {
        SaveToUnitDefinition();
    }

    void OnSelectInput(InputAction.CallbackContext context)
    {
        SetPaintSelect();
    }

    void OnDeselectInput(InputAction.CallbackContext context)
    {
        SetPaintDeselect();
    }

    void OnFillInput(InputAction.CallbackContext context)
    {
        FillCurrentRows();
    }

    void OnMirrorInput(InputAction.CallbackContext context)
    {
        ToggleMirrorPainting();
    }

    void OnUndoInput(InputAction.CallbackContext context)
    {
        Undo();
    }

    void OnClearInput(InputAction.CallbackContext context)
    {
        ClearCurrentArea();
    }

    void OnAnchorCenterInput(InputAction.CallbackContext context)
    {
        SetAnchorTriangleCenter();
    }

    void OnAnchorCornerInput(InputAction.CallbackContext context)
    {
        SetAnchorCorner();
    }

    void OnAnchorSideMidpointInput(InputAction.CallbackContext context)
    {
        SetAnchorSideMidpoint();
    }

    public bool IsPaintHeld()
    {
        if (paintAction != null && paintAction.action != null)
            return paintAction.action.IsPressed();

        if (Mouse.current != null)
            return Mouse.current.leftButton.isPressed;

        return false;
    }

    void CreateOrRefreshCenterMarker()
    {
        if (centerMarkerObject == null)
        {
            centerMarkerObject = new GameObject("Builder Center Marker");
            centerMarkerObject.transform.SetParent(gridRoot != null ? gridRoot : transform, false);

            MeshFilter meshFilter = centerMarkerObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = centerMarkerObject.AddComponent<MeshRenderer>();

            meshFilter.sharedMesh = CreateCircleMesh(centerMarkerRadius, centerMarkerSegments);

            if (centerMarkerMaterial != null)
                meshRenderer.sharedMaterial = centerMarkerMaterial;
            else
                meshRenderer.sharedMaterial = triangleMaterial;

            meshRenderer.sortingOrder = 100;
        }

        centerMarkerObject.SetActive(showCenterMarker);
        centerMarkerObject.transform.SetParent(gridRoot != null ? gridRoot : transform, false);
        centerMarkerObject.transform.localPosition = new Vector3(0f, 0f, -0.05f);

        MeshRenderer renderer = centerMarkerObject.GetComponent<MeshRenderer>();

        if (renderer != null)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor("_Color", centerMarkerColor);
            block.SetColor("_BaseColor", centerMarkerColor);
            renderer.SetPropertyBlock(block);
        }
    }

    Mesh CreateCircleMesh(float radius, int segments)
    {
        segments = Mathf.Max(8, segments);

        Vector3[] vertices = new Vector3[segments + 1];
        int[] triangles = new int[segments * 3];

        vertices[0] = Vector3.zero;

        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            vertices[i + 1] = new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                0f
            );
        }

        for (int i = 0; i < segments; i++)
        {
            int triangleIndex = i * 3;

            triangles[triangleIndex] = 0;
            triangles[triangleIndex + 1] = i + 1;
            triangles[triangleIndex + 2] = i == segments - 1 ? 1 : i + 2;
        }

        Mesh mesh = new Mesh();
        mesh.name = "Builder Center Marker Mesh";
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    void RefreshUiState()
    {
        SetButtonColor(
            baseSizeButton,
            currentArea == UnitFootprintArea.BaseSize ? buttonActiveColor : buttonInactiveColor
        );

        SetButtonColor(
            supportRangeButton,
            currentArea == UnitFootprintArea.SupportRange ? buttonActiveColor : buttonInactiveColor
        );

        SetButtonColor(
            selectButton,
            paintMode == UnitBuilderPaintMode.Select ? buttonActiveColor : buttonInactiveColor
        );

        SetButtonColor(
            deselectButton,
            paintMode == UnitBuilderPaintMode.Deselect ? buttonActiveColor : buttonInactiveColor
        );

        SetButtonColor(
            mirrorButton,
            mirrorPaintingEnabled ? mirrorActiveColor : buttonInactiveColor
        );

        SetButtonColor(
            anchorCenterButton,
            anchorType == UnitAnchorType.TriangleCenter ? buttonActiveColor : buttonInactiveColor
        );

        SetButtonColor(
            anchorCornerButton,
            anchorType == UnitAnchorType.Corner ? buttonActiveColor : buttonInactiveColor
        );

        SetButtonColor(
            anchorSideMidpointButton,
            anchorType == UnitAnchorType.SideMidpoint ? buttonActiveColor : buttonInactiveColor
        );

        if (triangleFacingButton != null)
        {
            SetButtonColor(
                triangleFacingButton,
                editedAnchorOrientation == TriangleOrientation.Up
                    ? upOrientationColor
                    : downOrientationColor
            );
        }

        if (anchorOrientationButtonText != null)
        {
            anchorOrientationButtonText.text = editedAnchorOrientation == TriangleOrientation.Up
                ? "Up"
                : "Down";
        }
    }

    void SetButtonColor(Button button, Color color)
    {
        if (button == null)
            return;

        Image image = button.targetGraphic as Image;

        if (image == null)
            image = button.GetComponent<Image>();

        if (image != null)
            image.color = color;
    }

    private void Reset()
    {
        showCenterMarker = true;
    }

    void ApplyDefaultToolState()
    {
        currentArea = UnitFootprintArea.BaseSize;
        paintMode = UnitBuilderPaintMode.Select;
        mirrorPaintingEnabled = false;
        showCenterMarker = showCenterMarkerByDefault;
    }

    bool ShouldFlipBuilderOrientation()
{
    TriangleOrientation currentAnchorOrientation = GetRawOrientation(AnchorCoord);
    return currentAnchorOrientation != editedAnchorOrientation;
}

TriangleOrientation GetRawOrientation(Vector2Int coord)
{
    bool useFirstColumnIsUp = geometrySource != null
        ? geometrySource.firstColumnIsUp
        : firstColumnIsUp;

    bool evenColumn = Mathf.Abs(coord.x % 2) == 0;

    bool isUp = useFirstColumnIsUp
        ? evenColumn
        : !evenColumn;

    return isUp ? TriangleOrientation.Up : TriangleOrientation.Down;
}
}