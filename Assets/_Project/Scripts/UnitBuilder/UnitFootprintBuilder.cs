using System.Collections.Generic;
using UnityEngine;

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

    private readonly Dictionary<Vector2Int, UnitBuilderTriangleView> views = new();
    private readonly HashSet<Vector2Int> baseSelection = new();
    private readonly HashSet<Vector2Int> supportSelection = new();
    private readonly Stack<BuilderSnapshot> undoStack = new();

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

    private void Start()
    {
        BuildGrid();
        LoadFromUnitDefinition();
        RefreshAllColors();
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

        RefreshAllColors();
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
            GameObject child = gridRoot.GetChild(i).gameObject;

            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
    }

    TriangleOrientation GetOrientation(Vector2Int coord)
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
        if (!views.ContainsKey(coord))
            return;

        bool isEditingBase = currentArea == UnitFootprintArea.BaseSize;
        bool isEditingSupport = currentArea == UnitFootprintArea.SupportRange;

        if (isEditingSupport && paintMode == UnitBuilderPaintMode.Select)
        {
            // Support range is not allowed inside the base.
            if (baseSelection.Contains(coord))
                return;
        }

        HashSet<Vector2Int> target = GetCurrentSelection();

        bool alreadySelected = target.Contains(coord);

        if (paintMode == UnitBuilderPaintMode.Select && alreadySelected)
            return;

        if (paintMode == UnitBuilderPaintMode.Deselect && !alreadySelected)
            return;

        PushUndo();

        if (paintMode == UnitBuilderPaintMode.Select)
        {
            target.Add(coord);

            // Base always wins over support.
            if (isEditingBase)
                supportSelection.Remove(coord);
        }
        else
        {
            target.Remove(coord);
        }

        RefreshTriangleColor(coord);
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

                if (!views.ContainsKey(coord))
                    continue;

                if (isEditingSupport && baseSelection.Contains(coord))
                    continue;

                target.Add(coord);

                if (isEditingBase)
                    supportSelection.Remove(coord);
            }
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
            ConvertSelectionToOffsets(baseSelection)
        );

        unitDefinition.SetFootprint(
            UnitFootprintArea.SupportRange,
            ConvertSelectionToOffsets(supportSelection)
        );

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

        foreach (Vector2Int coord in selection)
        {
            Vector2Int offset = coord - AnchorCoord;
            result.Add(new TriangleFootprintCell(offset.x, offset.y));
        }

        return result;
    }

    public void LoadFromUnitDefinition()
    {
        baseSelection.Clear();
        supportSelection.Clear();
        undoStack.Clear();

        if (unitDefinition == null)
        {
            RefreshAllColors();
            return;
        }

        anchorType = unitDefinition.anchorType;

        LoadFootprintIntoSelection(unitDefinition.baseSize, baseSelection);
        LoadFootprintIntoSelection(unitDefinition.supportRange, supportSelection);

        supportSelection.RemoveWhere(coord => baseSelection.Contains(coord));

        RefreshAllColors();
    }

    void LoadFootprintIntoSelection(List<TriangleFootprintCell> footprint, HashSet<Vector2Int> target)
    {
        if (footprint == null)
            return;

        foreach (TriangleFootprintCell cell in footprint)
        {
            Vector2Int coord = AnchorCoord + cell.Coord;

            if (coord.x < 0 || coord.x >= columns)
                continue;

            if (coord.y < 0 || coord.y >= rows)
                continue;

            target.Add(coord);
        }
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
        bool isAnchor = coord == AnchorCoord;
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
        else if (isAnchor)
        {
            fillColor = anchorPreviewColor;
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
    }

    public void SetTargetSupportRange()
    {
        currentArea = UnitFootprintArea.SupportRange;
    }

    public void SetPaintSelect()
    {
        paintMode = UnitBuilderPaintMode.Select;
    }

    public void SetPaintDeselect()
    {
        paintMode = UnitBuilderPaintMode.Deselect;
    }

    public void SetAnchorTriangleCenter()
    {
        anchorType = UnitAnchorType.TriangleCenter;
        BuildGrid();
    }

    public void SetAnchorCorner()
    {
        anchorType = UnitAnchorType.Corner;
        BuildGrid();
    }

    public void SetAnchorSideMidpoint()
    {
        anchorType = UnitAnchorType.SideMidpoint;
        BuildGrid();
    }

    public void SetUnitDefinition(UnitDefinition definition)
    {
        unitDefinition = definition;
        LoadFromUnitDefinition();
        BuildGrid();
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
}