using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public enum TriangleFillLayer
{
    MapBase,

    WhiteStartArea,
    BlackStartArea,

    ActiveSupport,
    DisabledSupport,

    FriendlyUnitBase,
    SupportedFriendlyUnitBase,
    EnemyUnitBase,
    DefeatedUnitBase,

    PreviewSupport,
    PreviewBaseValid,
    PreviewBaseInvalid
}

public class TriangleCellFillRenderer : MonoBehaviour
{
    [Header("Material")]
    [SerializeField] private Material fillMaterial;

    [Header("Sorting")]
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int mapSortingOrder = 0;
    [SerializeField] private int startAreaSortingOrder = 1;
    [SerializeField] private int supportSortingOrder = 2;
    [SerializeField] private int unitBaseSortingOrder = 3;
    [SerializeField] private int previewSortingOrder = 4;

    [Header("Z Offsets")]
    [SerializeField] private float mapZOffset = 0.06f;
    [SerializeField] private float startAreaZOffset = 0.05f;
    [SerializeField] private float supportZOffset = 0.04f;
    [SerializeField] private float unitBaseZOffset = 0.03f;
    [SerializeField] private float previewZOffset = 0.02f;

private readonly Dictionary<TriangleFillLayer, MeshFilter> filters = new();
private readonly Dictionary<TriangleFillLayer, MeshRenderer> renderers = new();

private MaterialPropertyBlock propertyBlock;

private void Awake()
{
    propertyBlock = new MaterialPropertyBlock();
}

    public void ClearAll()
    {
        foreach (MeshFilter filter in filters.Values)
        {
            if (filter != null)
                filter.sharedMesh = null;
        }
    }

    public void SetLayerCells(
        TriangleFillLayer layer,
        IEnumerable<TriangleCell> cells,
        Color color
    )
    {
        EnsureLayer(layer, out MeshFilter filter, out MeshRenderer meshRenderer);

        List<Vector3> vertices = new();
        List<int> triangles = new();

        if (cells != null)
        {
            foreach (TriangleCell cell in cells)
            {
                if (cell == null)
                    continue;

                if (cell.corners == null || cell.corners.Length < 3)
                    continue;

                if (cell.corners[0] == null ||
                    cell.corners[1] == null ||
                    cell.corners[2] == null)
                    continue;

                int startIndex = vertices.Count;
                float zOffset = GetZOffset(layer);

                Vector3 a = transform.InverseTransformPoint(
                    cell.corners[0].worldPosition + new Vector3(0f, 0f, zOffset)
                );

                Vector3 b = transform.InverseTransformPoint(
                    cell.corners[1].worldPosition + new Vector3(0f, 0f, zOffset)
                );

                Vector3 c = transform.InverseTransformPoint(
                    cell.corners[2].worldPosition + new Vector3(0f, 0f, zOffset)
                );

                vertices.Add(a);
                vertices.Add(b);
                vertices.Add(c);

                float signedArea =
                    a.x * (b.y - c.y) +
                    b.x * (c.y - a.y) +
                    c.x * (a.y - b.y);

                if (signedArea >= 0f)
                {
                    triangles.Add(startIndex);
                    triangles.Add(startIndex + 1);
                    triangles.Add(startIndex + 2);
                }
                else
                {
                    triangles.Add(startIndex);
                    triangles.Add(startIndex + 2);
                    triangles.Add(startIndex + 1);
                }
            }
        }

        Mesh mesh = new Mesh
        {
            name = $"{layer} Fill Mesh"
        };

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();

        filter.sharedMesh = mesh;

        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock();


        meshRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_Color", color);
        propertyBlock.SetColor("_BaseColor", color);
        meshRenderer.SetPropertyBlock(propertyBlock);
    }

    private void EnsureLayer(
        TriangleFillLayer layer,
        out MeshFilter filter,
        out MeshRenderer meshRenderer
    )
    {
        if (filters.TryGetValue(layer, out filter) &&
            renderers.TryGetValue(layer, out meshRenderer) &&
            filter != null &&
            meshRenderer != null)
        {
            return;
        }

        GameObject layerObject = new GameObject($"{layer} Fill Layer");
        layerObject.transform.SetParent(transform, false);

        filter = layerObject.AddComponent<MeshFilter>();
        meshRenderer = layerObject.AddComponent<MeshRenderer>();

        if (fillMaterial != null)
            meshRenderer.sharedMaterial = fillMaterial;
        else
            meshRenderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));

        meshRenderer.sortingLayerName = sortingLayerName;
        meshRenderer.sortingOrder = GetSortingOrder(layer);
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        filters[layer] = filter;
        renderers[layer] = meshRenderer;
    }

    private int GetSortingOrder(TriangleFillLayer layer)
    {
        switch (layer)
        {
            case TriangleFillLayer.MapBase:
                return mapSortingOrder;

            case TriangleFillLayer.WhiteStartArea:
            case TriangleFillLayer.BlackStartArea:
                return startAreaSortingOrder;

            case TriangleFillLayer.ActiveSupport:
            case TriangleFillLayer.DisabledSupport:
                return supportSortingOrder;

            case TriangleFillLayer.FriendlyUnitBase:
            case TriangleFillLayer.SupportedFriendlyUnitBase:   
            case TriangleFillLayer.EnemyUnitBase:
            case TriangleFillLayer.DefeatedUnitBase:
                return unitBaseSortingOrder;

            case TriangleFillLayer.PreviewSupport:
            case TriangleFillLayer.PreviewBaseValid:
            case TriangleFillLayer.PreviewBaseInvalid:
                return previewSortingOrder;

            default:
                return 0;
        }
    }

    private float GetZOffset(TriangleFillLayer layer)
    {
        switch (layer)
        {
            case TriangleFillLayer.MapBase:
                return mapZOffset;

            case TriangleFillLayer.WhiteStartArea:
            case TriangleFillLayer.BlackStartArea:
                return startAreaZOffset;

            case TriangleFillLayer.ActiveSupport:
            case TriangleFillLayer.DisabledSupport:
                return supportZOffset;

            case TriangleFillLayer.FriendlyUnitBase:
            case TriangleFillLayer.SupportedFriendlyUnitBase:
            case TriangleFillLayer.EnemyUnitBase:
            case TriangleFillLayer.DefeatedUnitBase:
                return unitBaseZOffset;

            case TriangleFillLayer.PreviewSupport:
            case TriangleFillLayer.PreviewBaseValid:
            case TriangleFillLayer.PreviewBaseInvalid:
                return previewZOffset;

            default:
                return 0f;
        }
    }
}