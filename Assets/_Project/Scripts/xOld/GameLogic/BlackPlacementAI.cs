// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// public class BlackPlacementAI : MonoBehaviour
// {
//     [SerializeField] private PlacementController placementController;
//     [SerializeField] private List<UnitDefinition> unitPool;
//     [SerializeField] private float placementDelay = 0.5f;

//     private bool isPlacing;

//     public void PlaceAfterDelay()
//     {
//         if (!isPlacing)
//             StartCoroutine(PlaceRoutine());
//     }

//     private IEnumerator PlaceRoutine()
//     {
//         isPlacing = true;

//         yield return new WaitForSeconds(placementDelay);

//         TryPlaceRandomUnit();

//         isPlacing = false;
//     }

//     private void TryPlaceRandomUnit()
//     {
//         if (unitPool == null || unitPool.Count == 0)
//         {
//             Debug.LogWarning("Black AI has no units assigned.");
//             return;
//         }

//         UnitDefinition unit = unitPool[Random.Range(0, unitPool.Count)];

//         List<GridPoint> validTiles =
//             placementController.GetValidPlacementPoints(unit, PlayerSide.Black);

//         if (validTiles.Count == 0)
//         {
//             Debug.LogWarning($"Black AI has no valid placement points for {unit.unitName}.");
//             return;
//         }

//         GridPoint tile = validTiles[Random.Range(0, validTiles.Count)];

//         placementController.TryPlaceUnit(unit, PlayerSide.Black, tile);
//     }
// }