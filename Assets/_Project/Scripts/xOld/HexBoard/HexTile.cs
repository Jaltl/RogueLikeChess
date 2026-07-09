using UnityEngine;

public class HexTile : MonoBehaviour
{
    [Header("Setup")]
    public Vector2Int axial;
    public Vector3 hexCenter => transform.position;

    [Header("Occupation")]
    public UnitPiece occupyingUnit;
    public bool IsOccupied => occupyingUnit != null;

    [Header("Terrain")]
    [SerializeField] private bool isBlockedTerrain;
    public bool IsBlockedTerrain => isBlockedTerrain;

    [Header("Highlights")]
    [SerializeField] private GameObject placementHighlight;
    [SerializeField] private GameObject hoverHighlight;
    [SerializeField] private GameObject footprintPreview;
    [SerializeField] private GameObject invalidPreview;
    [SerializeField] private GameObject zoneHighlight;

    private PlacementController placementController;

    public void Init(Vector2Int axial, PlacementController controller)
    {
        this.axial = axial;
        placementController = controller;

        SetPlacementHighlight(false);
        SetHoverHighlight(false);
        SetFootprintPreview(false);
        SetInvalidPreview(false);
    }

    private void OnMouseDown()
    {
        //placementController.OnHexClicked(this);
    }

    private void OnMouseEnter()
    {
        if (placementController.HasSelectedUnit)
        {
            //placementController.PreviewFootprint(this);
        }
        else
        {
            SetHoverHighlight(true);
        }
    }

    private void OnMouseExit()
    {
        SetHoverHighlight(false);
        placementController.ClearPreview();
    }

    public void SetPlacementHighlight(bool active)
    {
        if (placementHighlight != null)
            placementHighlight.SetActive(active);
    }

    public void SetFootprintPreview(bool active)
    {
        if (footprintPreview != null)
            footprintPreview.SetActive(active);
    }

    public void SetInvalidPreview(bool active)
    {
        if (invalidPreview != null)
            invalidPreview.SetActive(active);
    }

    public void SetHoverHighlight(bool active)
    {
        if (hoverHighlight != null)
            hoverHighlight.SetActive(active);
    }

    public void SetZoneHighlight(bool active)
    {
        if (zoneHighlight != null)
            zoneHighlight.SetActive(active);
    }

    public void SetOccupied(UnitPiece unit)
    {
        occupyingUnit = unit;
    }

    public void ClearOccupied(UnitPiece unit)
    {
        if (occupyingUnit == unit)
            occupyingUnit = null;
    }
}