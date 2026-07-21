using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class UnitFootprintBuilder : MonoBehaviour
{
    [Header("Unit Asset")]
    [SerializeField] private UnitDefinition unitDefinition;

    [Header("Optional Geometry Source")]
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

    [Header("Current Builder State")]
    [SerializeField] private UnitFootprintArea currentArea = UnitFootprintArea.BaseSize;

    [Header("Mirror Painting")]
    [SerializeField] private bool mirrorPaintingEnabled;

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

    [Header("Anchor Marker")]
    [SerializeField] private bool showAnchorMarker = true;
    [SerializeField] private bool showAnchorMarkerByDefault = true;
    [SerializeField] private float anchorMarkerRadius = 0.06f;
    [SerializeField] private int anchorMarkerSegments = 24;
    [SerializeField] private Color anchorMarkerColor = Color.yellow;
    [SerializeField] private Material anchorMarkerMaterial;

    [Header("UI Button Visuals")]
    [SerializeField] private Color buttonInactiveColor = new Color(0.55f, 0.55f, 0.55f, 1f);
    [SerializeField] private Color buttonActiveColor = new Color(1f, 0.85f, 0.25f, 1f);
    [SerializeField] private Color mirrorActiveColor = new Color(0.25f, 0.9f, 1f, 1f);
    [SerializeField] private Button baseSizeButton;
    [SerializeField] private Button supportRangeButton;
    [SerializeField] private Button mirrorButton;

    private readonly Stack<BuilderSnapshot> undoStack = new();
    private readonly Dictionary<Vector2Int, UnitBuilderTriangleView> views = new();
    private readonly Dictionary<Vector2Int, Vector2> localCenterOffsetsByCoord = new();
    private readonly HashSet<Vector2Int> baseSelection = new();
    private readonly HashSet<Vector2Int> supportSelection = new();

    private GameObject anchorMarkerObject;
    private Vector2Int? hoveredCoord;

    private Vector2Int AnchorCoord => new Vector2Int(columns / 2, rows / 2);
    private float SideLength => geometrySource != null ? geometrySource.sideLength : fallbackSideLength;
    private float TriangleHeight => SideLength * Mathf.Sqrt(3f) * 0.5f;

    private bool isPainting = false;

    private struct BuilderSnapshot
    {
        public readonly HashSet<Vector2Int> baseCells;
        public readonly HashSet<Vector2Int> supportCells;

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
    private void BuildGrid()
    {
        if (gridRoot == null)
            gridRoot = transform;

        ClearGridObjects();
        views.Clear();
        localCenterOffsetsByCoord.Clear();

        Vector3 anchorWorldOffset = GetAnchorCornerWorldPosition(AnchorCoord);

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                Vector2Int coord = new Vector2Int(col, row);
                CreateTriangleView(coord, anchorWorldOffset);
            }
        }

        CreateOrRefreshAnchorMarker();
        RefreshAllColors();
        RefreshUiState();
    }

    private void CreateTriangleView(Vector2Int coord, Vector3 anchorWorldOffset)
    {
        TriangleOrientation orientation = GetOrientation(coord);
        Vector3[] corners = GetCornerPositions(coord, orientation);

        for (int i = 0; i < corners.Length; i++)
            corners[i] -= anchorWorldOffset;

        Vector3 localCenter = (corners[0] + corners[1] + corners[2]) / 3f;
        localCenterOffsetsByCoord[coord] = new Vector2(localCenter.x, localCenter.y);

        GameObject triangleObject = new GameObject($"BuilderTriangle_{coord.x}_{coord.y}");
        triangleObject.transform.SetParent(gridRoot, false);

        MeshFilter meshFilter = triangleObject.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = CreateTriangleMesh(corners);

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
        view.Initialize(this, coord, corners, outlineMaterial, outlineWidth);
        views[coord] = view;
    }

    private Mesh CreateTriangleMesh(Vector3[] corners)
    {
        Mesh mesh = new Mesh
        {
            name = "Builder Triangle Mesh",
            vertices = corners
        };

        float signedArea =
            corners[0].x * (corners[1].y - corners[2].y) +
            corners[1].x * (corners[2].y - corners[0].y) +
            corners[2].x * (corners[0].y - corners[1].y);

        mesh.triangles = signedArea >= 0f
            ? new[] { 0, 1, 2 }
            : new[] { 0, 2, 1 };

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void ClearGridObjects()
    {
        if (gridRoot == null)
            return;

        for (int i = gridRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = gridRoot.GetChild(i);

            if (anchorMarkerObject != null && child.gameObject == anchorMarkerObject)
                continue;

            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
    }

    private TriangleOrientation GetOrientation(Vector2Int coord)
    {
        bool useFirstColumnIsUp = geometrySource != null
            ? geometrySource.firstColumnIsUp
            : firstColumnIsUp;

        bool evenColumn = Mathf.Abs(coord.x % 2) == 0;
        bool isUp = useFirstColumnIsUp ? evenColumn : !evenColumn;
        return isUp ? TriangleOrientation.Up : TriangleOrientation.Down;
    }

    private bool IsOffsetRow(int row)
    {
        if (geometrySource != null)
            return geometrySource.IsOffsetRow(row);

        if (!offsetEveryOtherRow)
            return false;

        return Mathf.Abs(row % 2) == 1;
    }

    private Vector3[] GetCornerPositions(Vector2Int coord, TriangleOrientation orientation)
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

    private Vector3 GetAnchorCornerWorldPosition(Vector2Int anchorCoord)
    {
        TriangleOrientation orientation = GetOrientation(anchorCoord);
        Vector3[] corners = GetCornerPositions(anchorCoord, orientation);
        return corners[0];
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

    private bool WouldPaintChangeAnything(List<Vector2Int> coords)
    {
        foreach (Vector2Int coord in coords)
        {
            if (WouldPaintChangeTriangle(coord))
                return true;
        }

        return false;
    }

    private bool WouldPaintChangeTriangle(Vector2Int coord)
    {
        if (!views.ContainsKey(coord))
            return false;

        bool isEditingBase = currentArea == UnitFootprintArea.BaseSize;
        bool isEditingSupport = currentArea == UnitFootprintArea.SupportRange;

        if (isPainting)
        {
            if (isEditingBase)
                return !baseSelection.Contains(coord);

            if (isEditingSupport)
                return !baseSelection.Contains(coord) && !supportSelection.Contains(coord);

            return false;
        }
        else
        {
            if (isEditingBase)
                return baseSelection.Contains(coord);

            if (isEditingSupport)
                return supportSelection.Contains(coord);

            return false;
        }
    }
    private void ApplyPaintToTriangle(Vector2Int coord)
    {
        HashSet<Vector2Int> target = currentArea == UnitFootprintArea.BaseSize ? baseSelection : supportSelection;

        bool wouldChange;

        if(isPainting)
            if(currentArea == UnitFootprintArea.BaseSize)
                wouldChange = !baseSelection.Contains(coord);
            else
                wouldChange = !baseSelection.Contains(coord) && !supportSelection.Contains(coord);
        else
            wouldChange = target.Contains(coord);

        if (!wouldChange)
            return;

        if (isPainting)
        {
            Debug.Log($"Adding coord {coord} to current selection");
            if(currentArea == UnitFootprintArea.BaseSize)
            {
                baseSelection.Add(coord);
                supportSelection.Remove(coord);
            }
            else
            {
                if(!baseSelection.Contains(coord))
                    supportSelection.Add(coord);
            }

        }
        else
        {
            Debug.Log($"Removing coord {coord} from current selection");
            target.Remove(coord);
        }

        RefreshTriangleColor(coord);
        RefreshUiState();
    }

    private HashSet<Vector2Int> GetCurrentSelection()
    {
        return currentArea == UnitFootprintArea.BaseSize
            ? baseSelection
            : supportSelection;
    }

    private void PushUndo()
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

        bool isEditingBase = currentArea == UnitFootprintArea.BaseSize;
        bool isEditingSupport = currentArea == UnitFootprintArea.SupportRange;
        Dictionary<int, List<int>> columnsByRow = new();

        foreach (Vector2Int coord in target)
        {
            if (!columnsByRow.TryGetValue(coord.y, out List<int> columnsInRow))
            {
                columnsInRow = new List<int>();
                columnsByRow[coord.y] = columnsInRow;
            }

            columnsInRow.Add(coord.x);
        }

        foreach (KeyValuePair<int, List<int>> rowPair in columnsByRow)
        {
            int row = rowPair.Key;
            List<int> columnsInRow = rowPair.Value;

            int min = columnsInRow[0];
            int max = columnsInRow[0];

            foreach (int col in columnsInRow)
            {
                if (col < min) min = col;
                if (col > max) max = col;
            }

            for (int col = min; col <= max; col++)
                TryAddToCurrentArea(new Vector2Int(col, row), isEditingBase, isEditingSupport);
        }

        if (mirrorPaintingEnabled)
            MirrorCurrentAreaIntoItself();

        RefreshAllColors();
    }

    private void TryAddToCurrentArea(Vector2Int coord, bool isEditingBase, bool isEditingSupport)
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

        bool isEditingBase = currentArea == UnitFootprintArea.BaseSize;
        bool isEditingSupport = currentArea == UnitFootprintArea.SupportRange;
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
        unitDefinition.anchorType = UnitAnchorType.Corner;

        unitDefinition.SetFootprint(UnitFootprintArea.BaseSize, ConvertSelectionToOffsets(baseSelection));
        unitDefinition.SetFootprint(UnitFootprintArea.SupportRange, ConvertSelectionToOffsets(supportSelection));

#if UNITY_EDITOR
        EditorUtility.SetDirty(unitDefinition);
        AssetDatabase.SaveAssets();
#endif

        Debug.Log(
            $"Saved unit footprint to {unitDefinition.name}. " +
            $"Base cells: {unitDefinition.baseSize.Count}, " +
            $"Support cells: {unitDefinition.supportRange.Count}, " +
            $"Anchor: {unitDefinition.anchorType}"
        );
    }

    List<TriangleFootprintCell> ConvertSelectionToOffsets(HashSet<Vector2Int> selection)
    {
        List<TriangleFootprintCell> result = new();

        float sideLength = SideLength;

        if (sideLength <= 0f)
            sideLength = 1f;

        foreach (Vector2Int coord in selection)
        {
            if (!localCenterOffsetsByCoord.TryGetValue(coord, out Vector2 localOffset))
                continue;

            result.Add(new TriangleFootprintCell(
                localOffset.x / sideLength,
                localOffset.y / sideLength
            ));
        }

        return result;
    }

    public void LoadFromUnitDefinition()
    {
        baseSelection.Clear();
        supportSelection.Clear();
        undoStack.Clear();

        showAnchorMarker = showAnchorMarkerByDefault;
        BuildGrid();

        if (unitDefinition != null)
        {
            unitDefinition.anchorType = UnitAnchorType.Corner;
            LoadFootprintIntoSelection(unitDefinition.GetMutableFootprint(UnitFootprintArea.BaseSize), baseSelection);
            LoadFootprintIntoSelection(unitDefinition.GetMutableFootprint(UnitFootprintArea.SupportRange), supportSelection);
        }

        supportSelection.RemoveWhere(coord => baseSelection.Contains(coord));
        CreateOrRefreshAnchorMarker();
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

        float sideLength = SideLength;

        if (sideLength <= 0f)
            sideLength = 1f;

        foreach (TriangleFootprintCell cell in footprint)
        {
            Vector2 localOffset = new Vector2(
                cell.localX * sideLength,
                cell.localY * sideLength
            );

            if (TryFindClosestBuilderCoord(localOffset, out Vector2Int coord))
                target.Add(coord);
        }
    }

    private bool TryFindClosestBuilderCoord(Vector2 localOffset, out Vector2Int closestCoord)
    {
        closestCoord = default;
        bool found = false;
        float bestDistanceSqr = float.MaxValue;

        foreach (KeyValuePair<Vector2Int, Vector2> pair in localCenterOffsetsByCoord)
        {
            float distanceSqr = (pair.Value - localOffset).sqrMagnitude;

            if (distanceSqr < bestDistanceSqr)
            {
                bestDistanceSqr = distanceSqr;
                closestCoord = pair.Key;
                found = true;
            }
        }

        float tolerance = SideLength * 0.35f;
        return found && bestDistanceSqr <= tolerance * tolerance;
    }

    private void RefreshAllColors()
    {
        foreach (Vector2Int coord in views.Keys)
            RefreshTriangleColor(coord);
    }

    private void RefreshTriangleColor(Vector2Int coord)
    {
        if (!views.TryGetValue(coord, out UnitBuilderTriangleView view))
            return;

        bool isBase = baseSelection.Contains(coord);
        bool isSupport = supportSelection.Contains(coord);
        bool isHovered = hoveredCoord.HasValue && hoveredCoord.Value == coord;
        bool isUp = GetOrientation(coord) == TriangleOrientation.Up;

        Color fillColor;

        if (isBase)
            fillColor = isUp ? baseUpColor : baseDownColor;
        else if (isSupport)
            fillColor = isUp ? supportUpColor : supportDownColor;
        else
            fillColor = isUp ? emptyUpColor : emptyDownColor;

        view.SetFillColor(fillColor);
        view.SetOutlineColor(isHovered ? hoverOutlineColor : outlineColor);
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
        isPainting = true;
    }

    public void SetPaintDeselect()
    {
        isPainting = false;
    }

    public void ToggleAnchorMarker()
    {
        showAnchorMarker = !showAnchorMarker;
        CreateOrRefreshAnchorMarker();
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
        if (!hoveredCoord.HasValue || hoveredCoord.Value != coord)
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

    private Vector2Int GetMirroredCoord(Vector2Int coord)
    {
        Vector2Int local = coord - AnchorCoord;
        Vector2Int mirroredLocal = new Vector2Int(-local.x, local.y);
        return AnchorCoord + mirroredLocal;
    }

    private List<Vector2Int> GetPaintCoords(Vector2Int coord)
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

    private void RegisterInputActions(bool register)
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
    }

    private void RegisterAction(
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

    private void OnBaseSizeInput(InputAction.CallbackContext context) => SetTargetBaseSize();
    private void OnSupportRangeInput(InputAction.CallbackContext context) => SetTargetSupportRange();
    private void OnFinishedInput(InputAction.CallbackContext context) => SaveToUnitDefinition();
    private void OnSelectInput(InputAction.CallbackContext context) => SetPaintSelect();
    private void OnDeselectInput(InputAction.CallbackContext context) => SetPaintDeselect();
    private void OnFillInput(InputAction.CallbackContext context) => FillCurrentRows();
    private void OnMirrorInput(InputAction.CallbackContext context) => ToggleMirrorPainting();
    private void OnUndoInput(InputAction.CallbackContext context) => Undo();
    private void OnClearInput(InputAction.CallbackContext context) => ClearCurrentArea();

    public bool IsPaintHeld()
    {
        bool selectHeld =
            selectAction != null &&
            selectAction.action != null &&
            selectAction.action.IsPressed();

        bool deselectHeld =
            deselectAction != null &&
            deselectAction.action != null &&
            deselectAction.action.IsPressed();

        return selectHeld || deselectHeld;
    }

    private void CreateOrRefreshAnchorMarker()
    {
        if (gridRoot == null)
            gridRoot = transform;

        if (anchorMarkerObject == null)
        {
            anchorMarkerObject = new GameObject("Builder Corner Anchor Marker");
            anchorMarkerObject.transform.SetParent(gridRoot, false);

            MeshFilter meshFilter = anchorMarkerObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = anchorMarkerObject.AddComponent<MeshRenderer>();

            meshFilter.sharedMesh = CreateCircleMesh(anchorMarkerRadius, anchorMarkerSegments);
            meshRenderer.sharedMaterial = anchorMarkerMaterial != null ? anchorMarkerMaterial : triangleMaterial;
            meshRenderer.sortingOrder = 100;
        }

        anchorMarkerObject.SetActive(showAnchorMarker);
        anchorMarkerObject.transform.SetParent(gridRoot, false);
        anchorMarkerObject.transform.localPosition = new Vector3(0f, 0f, -0.05f);

        MeshRenderer renderer = anchorMarkerObject.GetComponent<MeshRenderer>();

        if (renderer != null)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor("_Color", anchorMarkerColor);
            block.SetColor("_BaseColor", anchorMarkerColor);
            renderer.SetPropertyBlock(block);
        }
    }

    private Mesh CreateCircleMesh(float radius, int segments)
    {
        segments = Mathf.Max(8, segments);

        Vector3[] vertices = new Vector3[segments + 1];
        int[] triangles = new int[segments * 3];
        vertices[0] = Vector3.zero;

        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
        }

        for (int i = 0; i < segments; i++)
        {
            int triangleIndex = i * 3;
            triangles[triangleIndex] = 0;
            triangles[triangleIndex + 1] = i + 1;
            triangles[triangleIndex + 2] = i == segments - 1 ? 1 : i + 2;
        }

        Mesh mesh = new Mesh
        {
            name = "Builder Corner Anchor Marker Mesh",
            vertices = vertices,
            triangles = triangles
        };

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void RefreshUiState()
    {
        SetButtonColor(baseSizeButton, currentArea == UnitFootprintArea.BaseSize ? buttonActiveColor : buttonInactiveColor);
        SetButtonColor(supportRangeButton, currentArea == UnitFootprintArea.SupportRange ? buttonActiveColor : buttonInactiveColor);
        SetButtonColor(mirrorButton, mirrorPaintingEnabled ? mirrorActiveColor : buttonInactiveColor);
    }

    private void SetButtonColor(Button button, Color color)
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
        showAnchorMarker = true;
    }

    private void ApplyDefaultToolState()
    {
        currentArea = UnitFootprintArea.BaseSize;
        mirrorPaintingEnabled = false;
        showAnchorMarker = showAnchorMarkerByDefault;
    }
}