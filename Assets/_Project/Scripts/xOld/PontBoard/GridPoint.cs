// using UnityEngine;

// public class GridPoint : MonoBehaviour
// {
//     [Header("Setup")]
//     public Vector2Int coordinates;
//     public Vector3 WorldPosition => transform.position;

//     [Header("Occupation")]
//     public UnitPiece occupyingUnit;
//     public bool IsOccupied => occupyingUnit != null;

//     private PlacementController placementController;

//     [Header("Terrain")]
//     [SerializeField] private bool isBlockedTerrain;
//     public bool IsBlockedTerrain => isBlockedTerrain;

//     [Header("Map State")]
//     public bool IsActive { get; private set; }
//     public MapRegion Region { get; private set; } = MapRegion.None;



//     private bool zoneHighlight;
//     private bool placementHighlight;
//     private bool hoverHighlight;
//     private bool footprintPreview;
//     private bool invalidPreview;

//     public bool IsHovered => hoverHighlight;

//     public GridPointVisualState CurrentVisualState
//     {
//         get
//         {
//             if(invalidPreview)  return GridPointVisualState.Invalid;

//             if (footprintPreview) return GridPointVisualState.Footprint;

//             if (placementHighlight) return GridPointVisualState.Placement;

//             return GridPointVisualState.None;
//         }
//     }

    
//     public void Init(Vector2Int coordinates, PlacementController controller)
//     {
//         this.coordinates = coordinates;
//         placementController = controller;

//         ClearAllVisualStates();
//         SetMapData(false, MapRegion.None, false);
//     }

//     public void SetMapData(bool isActive, MapRegion region, bool isBlockedTerrain)
//     {
//         IsActive = isActive;
//         Region = isActive ? region : MapRegion.None;
//         isBlockedTerrain = isActive && isBlockedTerrain;

//         //Om punkten har collider kan du stänga av denna punkten när den är utanför kartan
//         Collider2D collider = GetComponent<Collider2D>();
//         if (collider != null)
//         {
//             collider.enabled = isActive;
//         }
//     }

//     private void OnMouseDown()
//     {
//         placementController.OnGridPointClicked(this);
//     }

//     private void OnMouseEnter()
//     {
//         if (!IsActive)
//             return;

//         placementController.OnGridPointHoverEnter(this);
//     }

//     private void OnMouseExit()
//     {
//         if (!IsActive)
//             return;
            
//         placementController.OnGridPointHoverExit(this);
//     }

//     public void SetZoneHighlight(bool active)
//     {
//         zoneHighlight = active;
//     }

//     public void SetPlacementHighlight(bool active)
//     {
//         placementHighlight = active;
//     }

//     public void SetHoverHighlight(bool active)
//     {
//         hoverHighlight = active;
//     }

//     public void SetFootprintPreview(bool active)
//     {
//         footprintPreview = active;
//     }

//     public void SetInvalidPreview(bool active)
//     {
//         invalidPreview = active;
//     }

//     public void SetOccupyingUnit(UnitPiece unit)
//     {
//         occupyingUnit = unit;
//     }

//     public void ClearOccupyingUnit(UnitPiece unit)
//     {
//         if (occupyingUnit == unit)
//         {
//             occupyingUnit = null;
//         }
//     }

//     public void ClearPreview()
//     {
//         footprintPreview = false;
//         invalidPreview = false;
//     }

//     public void ClearAllVisualStates()
//     {
//         zoneHighlight = false;
//         placementHighlight = false;
//         hoverHighlight = false;
//         footprintPreview = false;
//         invalidPreview = false;
//     }
// }
