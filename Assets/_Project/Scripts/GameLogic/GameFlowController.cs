using System.Collections.Generic;
using UnityEngine;

public enum GamePhase
{
    RoundStart,
    UnitPlacement,
    ConflictResolution,
    PointScoring,
    RoundEnd
}

public class GameFlowController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TriangleBoard board;
    [SerializeField] private TrianglePlacementController placementController;

    [Header("Defeat Rules")]
    [SerializeField] private UnitDefeatBehavior defeatBehavior = UnitDefeatBehavior.RemoveFromBoard;

    [Header("Objective Rules")]
    [SerializeField] private bool disconnectedUnitsCanContestObjectives = true;

    [Header("Debug")]
    [SerializeField] private GamePhase currentPhase = GamePhase.UnitPlacement;

    [Header("Placement Phase")]
    [SerializeField] private int placementsBeforeConflictPhase = 2;

    private readonly List<UnitPiece> unitsPlacedThisPlacementPhase = new();

    public GamePhase CurrentPhase => currentPhase;
    public bool DisconnectedUnitsCanContestObjectives => disconnectedUnitsCanContestObjectives;

    public bool ShouldResolveConflictPhase()
    {
        return unitsPlacedThisPlacementPhase.Count >= placementsBeforeConflictPhase;
    }

    public void RegisterPlacedUnit(UnitPiece unit)
    {
        if (unit == null)
            return;

        if (!unitsPlacedThisPlacementPhase.Contains(unit))
            unitsPlacedThisPlacementPhase.Add(unit);

        Debug.Log(
            $"Registered placed unit: {unit.name}. " +
            $"Placed this phase: {unitsPlacedThisPlacementPhase.Count}/{placementsBeforeConflictPhase}"
        );
    }

    [ContextMenu("Resolve Conflict Phase")]
    public void ResolveConflictPhase()
    {
        if (board == null)
        {
            Debug.LogWarning("Cannot resolve conflicts: no TriangleBoard assigned.");
            return;
        }

        currentPhase = GamePhase.ConflictResolution;

        List<UnitConflictPair> conflicts =
            UnitConflictResolver.BuildConflicts(
                board,
                unitsPlacedThisPlacementPhase
            );

        List<UnitConflictResult> results =
            UnitConflictResolver.ResolveConflicts(
                board,
                conflicts
            );

        Debug.Log($"Conflict phase started. Conflicts: {results.Count}");

        HashSet<UnitPiece> defeatedUnits = new();

        foreach (UnitConflictResult result in results)
        {
            Debug.Log(BuildConflictLog(result));

            if (result.initiatorDefeated && result.initiator != null)
                defeatedUnits.Add(result.initiator);

            if (result.targetDefeated && result.target != null)
                defeatedUnits.Add(result.target);
        }

        ApplyDefeats(defeatedUnits);

        unitsPlacedThisPlacementPhase.Clear();

        if (placementController != null)
            placementController.RecalculateAllSupportNetworks();

        currentPhase = GamePhase.PointScoring;

        Debug.Log($"Conflict phase finished. Defeated units: {defeatedUnits.Count}");
    }

    private void ApplyDefeats(HashSet<UnitPiece> defeatedUnits)
    {
        if (defeatedUnits == null || defeatedUnits.Count == 0)
            return;

        foreach (UnitPiece unit in defeatedUnits)
        {
            if (unit == null)
                continue;

            if (defeatBehavior == UnitDefeatBehavior.RemoveFromBoard)
            {
                if (board != null)
                    board.RemoveUnit(unit);

                Destroy(unit.gameObject);
            }
            else
            {
                unit.SetDefeated(true);
            }
        }
    }

    private string BuildConflictLog(UnitConflictResult result)
    {
        if (result == null)
            return "Null conflict result.";

        string initiatorName = result.initiator != null
            ? result.initiator.name
            : "Missing Initiator";

        string targetName = result.target != null
            ? result.target.name
            : "Missing Target";

        string directText = result.directOverlap
            ? "Direct overlap"
            : "Range";

        return
            $"Conflict [{directText}] | " +
            $"{initiatorName}: {result.initiatorBasePower}" +
            $"+{result.initiatorExternalSupport} external support" +
            $"+{result.initiatorOwnSupportBonus} own overlap support" +
            $" = {result.initiatorTotalPower} | " +
            $"{targetName}: {result.targetBasePower}" +
            $"+{result.targetExternalSupport} support" +
            $" = {result.targetTotalPower} | " +
            $"Defeated: " +
            $"{(result.initiatorDefeated ? initiatorName : "")} " +
            $"{(result.targetDefeated ? targetName : "")}";
    }
}