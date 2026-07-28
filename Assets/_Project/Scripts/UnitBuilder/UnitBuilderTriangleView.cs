using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(PolygonCollider2D))]
public class UnitBuilderTriangleView : MonoBehaviour
{
    public UnitFootprintBuilder builder;
    private Vector2Int coord;

    private MeshRenderer meshRenderer;
    private LineRenderer outlineRenderer;
    private MaterialPropertyBlock propertyBlock;

    public Vector2Int Coord => coord;

    public void Initialize(
        UnitFootprintBuilder builder,
        Vector2Int coord,
        Vector3[] localCorners,
        Material outlineMaterial,
        float outlineWidth
    )
    {
        this.builder = builder;
        this.coord = coord;

        meshRenderer = GetComponent<MeshRenderer>();
        propertyBlock = new MaterialPropertyBlock();

        CreateOutline(localCorners, outlineMaterial, outlineWidth);
    }

    private void OnMouseEnter()
    {
        if (builder != null)
            builder.SetHoveredTriangle(coord);
    }

    private void OnMouseOver()
    {
        if (builder != null && builder.IsPaintHeld())
            builder.PaintTriangle(coord);
    }

    private void OnMouseExit()
    {
        if (builder != null)
            builder.ClearHoveredTriangle(coord);
    }

    public void SetFillColor(Color color)
    {
        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();

        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock();

        meshRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_Color", color);
        propertyBlock.SetColor("_BaseColor", color);
        meshRenderer.SetPropertyBlock(propertyBlock);
    }

    public void SetOutlineColor(Color color)
    {
        if (outlineRenderer == null)
            return;

        outlineRenderer.startColor = color;
        outlineRenderer.endColor = color;
    }

    void CreateOutline(Vector3[] corners, Material outlineMaterial, float outlineWidth)
    {
        GameObject outlineObject = new GameObject("Outline");
        outlineObject.transform.SetParent(transform, false);

        outlineRenderer = outlineObject.AddComponent<LineRenderer>();
        outlineRenderer.useWorldSpace = false;
        outlineRenderer.positionCount = 4;

        outlineRenderer.SetPosition(0, corners[0]);
        outlineRenderer.SetPosition(1, corners[1]);
        outlineRenderer.SetPosition(2, corners[2]);
        outlineRenderer.SetPosition(3, corners[0]);

        outlineRenderer.startWidth = outlineWidth;
        outlineRenderer.endWidth = outlineWidth;

        outlineRenderer.numCapVertices = 0;
        outlineRenderer.numCornerVertices = 0;

        outlineRenderer.sortingOrder = 20;

        if (outlineMaterial != null)
            outlineRenderer.sharedMaterial = outlineMaterial;
    }
}