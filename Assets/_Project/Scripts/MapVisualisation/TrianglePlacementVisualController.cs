using System.Collections.Generic;
using UnityEngine;

public class TrianglePlacementVisualController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TriangleGridManager grid;
    [SerializeField] private TriangleBoard board;
    [SerializeField] private TriangleLineRenderer lineRenderer;
    [SerializeField] private TriangleCellFillRenderer fillRenderer;
    [SerializeField] private TriangleSupportLinkRenderer supportLinkRenderer;

    [Header("Filled Cell Colors")]
    [SerializeField] private Color mapBaseFillColor = new Color(0.12f, 0.22f, 0.36f, 1f);

    [SerializeField] private Color whiteStartFillColor = new Color(1f, 0.68f, 0.08f, 0.35f);
    [SerializeField] private Color blackStartFillColor = new Color(0.52f, 0.35f, 1f, 0.35f);

    [SerializeField] private Color activeSupportFillColor = new Color(0.1f, 0.75f, 0.22f, 0.35f);
    [SerializeField] private Color disabledSupportFillColor = new Color(0.25f, 0.25f, 0.25f, 0.4f);

    [SerializeField] private Color friendlyBaseFillColor = new Color(0.1f, 0.85f, 0.25f, 0.65f);
    [SerializeField] private Color supportedUnitFillColor = new Color(1f, 1f, 0.15f, 0.35f);
    [SerializeField] private Color enemyBaseFillColor = new Color(1f, 0.18f, 0.12f, 0.65f);
    [SerializeField] private Color defeatedBaseFillColor = new Color(0.3f, 0.3f, 0.3f, 0.7f);

    [SerializeField] private Color previewSupportFillColor = new Color(0.1f, 0.8f, 0.95f, 0.28f);
    [SerializeField] private Color previewBaseValidFillColor = new Color(0.45f, 1f, 0.2f, 0.6f);
    [SerializeField] private Color previewBaseInvalidFillColor = new Color(1f, 0.12f, 0.08f, 0.6f);

    public void Refresh(
        PlayerSide currentPlayer,
        UnitPlacementResult preview,
        bool previewIsValid
    )
    {
        if (grid == null)
            return;

        grid.ClearAllNodeVisualStates();

        if (fillRenderer != null)
        {
            fillRenderer.ClearAll();

            fillRenderer.SetLayerCells(
                TriangleFillLayer.MapBase,
                GetActiveMapCells(),
                mapBaseFillColor
            );
        }

        if (supportLinkRenderer != null)
            supportLinkRenderer.Clear();

        ShowStartAreas();
        ShowSupport(currentPlayer);
        ShowUnitBases(currentPlayer);
        ShowSupportLinksAndSupportedUnits(currentPlayer);
        ShowPreview(preview, previewIsValid);

        if (lineRenderer != null)
            lineRenderer.RefreshLineColors();
    }

    private void ShowStartAreas()
    {
        List<TriangleCell> whiteCells = GetStartAreaCells(PlayerSide.White);
        List<TriangleCell> blackCells = GetStartAreaCells(PlayerSide.Black);

        foreach (TriangleCell cell in whiteCells)
            cell.SetWholeVisualState(TriangleNodeVisualState.WhiteStartArea, true);

        foreach (TriangleCell cell in blackCells)
            cell.SetWholeVisualState(TriangleNodeVisualState.BlackStartArea, true);

        if (fillRenderer != null)
        {
            fillRenderer.SetLayerCells(
                TriangleFillLayer.WhiteStartArea,
                whiteCells,
                whiteStartFillColor
            );

            fillRenderer.SetLayerCells(
                TriangleFillLayer.BlackStartArea,
                blackCells,
                blackStartFillColor
            );
        }
    }

    private void ShowSupport(PlayerSide currentPlayer)
    {
        if (board == null)
            return;

        List<TriangleCell> activeSupportCells = new();
        List<TriangleCell> disabledSupportCells = new();

        foreach (UnitPiece unit in board.Units)
        {
            if (unit == null)
                continue;

            if (unit.Owner != currentPlayer)
                continue;

            if (unit.IsDefeated)
                continue;

            foreach (TriangleCell supportCell in unit.SupportCells)
            {
                if (supportCell == null)
                    continue;

                if (unit.SupportActive)
                {
                    supportCell.SetWholeVisualState(TriangleNodeVisualState.ActiveSupport, true);
                    activeSupportCells.Add(supportCell);
                }
                else
                {
                    supportCell.SetWholeVisualState(TriangleNodeVisualState.DisabledSupport, true);
                    disabledSupportCells.Add(supportCell);
                }
            }
        }

        if (fillRenderer != null)
        {
            fillRenderer.SetLayerCells(
                TriangleFillLayer.ActiveSupport,
                activeSupportCells,
                activeSupportFillColor
            );

            fillRenderer.SetLayerCells(
                TriangleFillLayer.DisabledSupport,
                disabledSupportCells,
                disabledSupportFillColor
            );
        }
    }

    private void ShowUnitBases(PlayerSide currentPlayer)
    {
        if (board == null)
            return;

        List<TriangleCell> friendlyCells = new();
        List<TriangleCell> enemyCells = new();
        List<TriangleCell> defeatedCells = new();

        foreach (UnitPiece unit in board.Units)
        {
            if (unit == null)
                continue;

            TriangleNodeVisualState state;

            if (unit.IsDefeated)
                state = TriangleNodeVisualState.DefeatedUnitBase;
            else if (unit.Owner == currentPlayer)
                state = TriangleNodeVisualState.FriendlyUnitBase;
            else
                state = TriangleNodeVisualState.EnemyUnitBase;

            foreach (TriangleCell baseCell in unit.OccupiedCells)
            {
                if (baseCell == null)
                    continue;

                baseCell.SetWholeVisualState(state, true);

                if (unit.IsDefeated)
                    defeatedCells.Add(baseCell);
                else if (unit.Owner == currentPlayer)
                    friendlyCells.Add(baseCell);
                else
                    enemyCells.Add(baseCell);
            }
        }

        if (fillRenderer != null)
        {
            fillRenderer.SetLayerCells(
                TriangleFillLayer.FriendlyUnitBase,
                friendlyCells,
                friendlyBaseFillColor
            );

            fillRenderer.SetLayerCells(
                TriangleFillLayer.EnemyUnitBase,
                enemyCells,
                enemyBaseFillColor
            );

            fillRenderer.SetLayerCells(
                TriangleFillLayer.DefeatedUnitBase,
                defeatedCells,
                defeatedBaseFillColor
            );
        }
    }

    private void ShowSupportLinksAndSupportedUnits(PlayerSide currentPlayer)
    {
        if (board == null)
            return;

        List<UnitSupportLink> links =
            UnitSupportUtility.GetSupportLinksForSide(board, currentPlayer);

        HashSet<TriangleCell> supportedBaseCells = new();

        foreach (UnitSupportLink link in links)
        {
            if (link == null || link.receiver == null)
                continue;

            foreach (TriangleCell cell in link.receiver.OccupiedCells)
            {
                if (cell != null)
                    supportedBaseCells.Add(cell);
            }
        }

        if (fillRenderer != null)
        {
            fillRenderer.SetLayerCells(
                TriangleFillLayer.SupportedFriendlyUnitBase,
                supportedBaseCells,
                supportedUnitFillColor
            );
        }

        if (supportLinkRenderer != null)
            supportLinkRenderer.ShowLinks(links);
    }

    private void ShowPreview(UnitPlacementResult preview, bool previewIsValid)
    {
        if (preview == null)
            return;

        foreach (TriangleCell supportCell in preview.supportCells)
        {
            if (supportCell != null)
                supportCell.SetWholeVisualState(TriangleNodeVisualState.PreviewSupport, true);
        }

        TriangleNodeVisualState baseState = previewIsValid
            ? TriangleNodeVisualState.PreviewBaseValid
            : TriangleNodeVisualState.PreviewBaseInvalid;

        foreach (TriangleCell baseCell in preview.baseCells)
        {
            if (baseCell != null)
                baseCell.SetWholeVisualState(baseState, true);
        }

        if (fillRenderer != null)
        {
            fillRenderer.SetLayerCells(
                TriangleFillLayer.PreviewSupport,
                preview.supportCells,
                previewSupportFillColor
            );

            fillRenderer.SetLayerCells(
                previewIsValid
                    ? TriangleFillLayer.PreviewBaseValid
                    : TriangleFillLayer.PreviewBaseInvalid,
                preview.baseCells,
                previewIsValid
                    ? previewBaseValidFillColor
                    : previewBaseInvalidFillColor
            );
        }

        if (preview.anchorNode != null)
            preview.anchorNode.SetState(baseState, true);
    }

    private List<TriangleCell> GetActiveMapCells()
    {
        List<TriangleCell> result = new();

        if (grid == null)
            return result;

        foreach (TriangleCell cell in grid.AllCells)
        {
            if (cell == null)
                continue;

            if (!cell.isActive)
                continue;

            result.Add(cell);
        }

        return result;
    }

    private List<TriangleCell> GetStartAreaCells(PlayerSide side)
    {
        List<TriangleCell> result = new();

        if (grid == null)
            return result;

        MapRegion targetRegion = side == PlayerSide.White
            ? MapRegion.WhiteStart
            : MapRegion.BlackStart;

        foreach (TriangleCell cell in grid.AllCells)
        {
            if (cell == null)
                continue;

            if (!cell.isActive)
                continue;

            if (cell.region == targetRegion)
                result.Add(cell);
        }

        return result;
    }
}