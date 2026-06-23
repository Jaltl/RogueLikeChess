using UnityEngine;

public class GridPoint : MonoBehaviour
{
    [Header("Setup")]
    public Vector2Int coordinates;
    public Vector3 WorldPosition => transform.position;

    [Header("Occupation")]
    public UnitPiece occupyingUnit;
    public bool IsOccupied => occupyingUnit != null;

    [Header("Terrain")]
    [SerializeField] private bool isBlockedTerrain;
    public bool IsBlockedTerrain => isBlockedTerrain;


    [Header("Highlights")]
    [SerializeField] private GameObject zoneHighlight;
    [SerializeField] private GameObject placementHighlight;
    [SerializeField] private GameObject hoverHighlight;
    [SerializeField] private GameObject footprintPreview;
    [SerializeField] private GameObject invalidPreview;

    private PlacementController placementController;
    
    public void Init(Vector2Int coordinates, PlacementController controller)
    {
        this.coordinates = coordinates;
        placementController = controller;

        SetZoneHighlight(false);
        SetPlacementHighlight(false);
        SetHoverHighlight(false);
        SetInvalidPreview(false);
        SetFootprintPreview(false);
    }

    private void OnMouseDown()
    {
        placementController.OnGridPointClicked(this);
    }

    private void OnMouseEnter()
    {
        if (placementController.HasSelectedUnit)
        {
            placementController.PreviewFootprint(this);
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

    public void SetZoneHighlight(bool active)
    {
        if (zoneHighlight != null)
        {
            zoneHighlight.SetActive(active);
        }
    }

    public void SetPlacementHighlight(bool active)
    {
        if (placementHighlight != null)
        {
            placementHighlight.SetActive(active);
        }
    }

    public void SetHoverHighlight(bool active)
    {
        if (hoverHighlight != null)
        {
            hoverHighlight.SetActive(active);
        }
    }

    public void SetFootprintPreview(bool active)
    {
        if (footprintPreview != null)
        {
            footprintPreview.SetActive(active);
        }
    }

    public void SetInvalidPreview(bool active)
    {
        if (invalidPreview != null)
        {
            invalidPreview.SetActive(active);
        }
    }

    public void SetOccupyingUnit(UnitPiece unit)
    {
        occupyingUnit = unit;
    }

    public void ClearOccupyingUnit(UnitPiece unit)
    {
        if (occupyingUnit == unit)
        {
            occupyingUnit = null;
        }
    }
}
