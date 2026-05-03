using UnityEngine;

public class GameController : MonoBehaviour
{
    [Header("Game Logic Scripts")]
    [SerializeField] Board board;
    [SerializeField] Rules rules;
    [SerializeField] Gridmanager grid;
    [SerializeField] AnimationController anim;

    private Piece selected;

    [Header("Piece Prefabs")]
    [SerializeField] private Piece pawnPrefab;
    [SerializeField] private Piece knightPrefab;
    [SerializeField] private Piece bishopPrefab;
    [SerializeField] private Piece rookPrefab;
    [SerializeField] private Piece queenPrefab;
    [SerializeField] private Piece kingPrefab;

    public enum GameState { WhiteTurn, BlackTurn, End }
    public GameState state;
    private Tile lastFromTile;
    private Tile lastToTile;

    private void Start()
    {
        SetupBoard();
        state = GameState.WhiteTurn;
    }

    private void SetupBoard()
    {
        // Pawns
        for (int i = 0; i < 8; i++)
        {
            SpawnPiece(pawnPrefab, i, 1, true);
            SpawnPiece(pawnPrefab, i, 6, false);
        }

        // Rooks
        SpawnPiece(rookPrefab, 0, 0, true);
        SpawnPiece(rookPrefab, 7, 0, true);
        SpawnPiece(rookPrefab, 0, 7, false);
        SpawnPiece(rookPrefab, 7, 7, false);

        // Knights
        SpawnPiece(knightPrefab, 1, 0, true);
        SpawnPiece(knightPrefab, 6, 0, true);
        SpawnPiece(knightPrefab, 1, 7, false);
        SpawnPiece(knightPrefab, 6, 7, false);

        // Bishops
        SpawnPiece(bishopPrefab, 2, 0, true);
        SpawnPiece(bishopPrefab, 5, 0, true);
        SpawnPiece(bishopPrefab, 2, 7, false);
        SpawnPiece(bishopPrefab, 5, 7, false);

        // Queens
        SpawnPiece(queenPrefab, 3, 0, true);
        SpawnPiece(queenPrefab, 3, 7, false);

        // Kings
        SpawnPiece(kingPrefab, 4, 0, true);
        SpawnPiece(kingPrefab, 4, 7, false);
    }

    private void SpawnPiece(Piece prefab, int x, int y, bool isWhite)
    {
        Piece piece = Instantiate(prefab, new Vector3(x, y, 0), Quaternion.identity);

        piece.isWhite = isWhite;
        piece.type = prefab.type;
        piece.SetPosition(x, y);

        board.SetPiece(x, y, piece);
    }

    public void OnTileClicked(Tile tile)
    {
        if (anim.IsAnimating()) return;

        int x = tile.x;
        int y = tile.y;

        var clicked = board.GetPiece(x, y);
        bool whiteTurn = state == GameState.WhiteTurn;

        if (selected == null)
        {
            if (clicked != null && clicked.isWhite == whiteTurn)
            {
                selected = clicked;
                Highlight(selected);

                if (!anim.IsAnimating()) 
                {
                    anim.Play(anim.SelectPulse(selected));
                    anim.PlaySoundSelect();
                }
            }
            return;
        }

        if (clicked != null && clicked.isWhite == whiteTurn)
        {
            selected = clicked;
            Highlight(selected);
            return;
        }

        TryMove(x, y);
    }

    void TryMove(int x, int y)
    {
        foreach (var move in rules.GetLegalMoves(selected))
        {
            if (move.x == x && move.y == y)
            {
                ExecuteMove(selected, x, y);
                selected = null;
                return;
            }
        }

        selected = null;
    }

    void ExecuteMove(Piece piece, int x, int y)
    {
        Tile fromTile = grid.GetTileAtPosition(new Vector2Int(piece.x, piece.y));
        Tile toTile = grid.GetTileAtPosition(new Vector2Int(x, y));

        ClearLastMoveHighlight();

        lastFromTile = fromTile;
        lastToTile = toTile;

        lastFromTile.SetLatestMoveHighlight(true);
        lastToTile.SetLatestMoveHighlight(true);


        var target = board.GetPiece(x, y);

        if (target != null)
            anim.Play(anim.Capture(target));

        board.MovePiece(piece, x, y);

        anim.Play(anim.Move(piece, new Vector3(x, y)));

        piece.hasMoved = true;

        SwitchTurn();

        CheckGameState();
    }

    void SwitchTurn()
    {
        state = (state == GameState.WhiteTurn)
            ? GameState.BlackTurn
            : GameState.WhiteTurn;
    }

    void CheckGameState()
    {
        bool white = state == GameState.WhiteTurn;

        if (rules.IsCheckmate(white))
        {
            Debug.Log("Checkmate");
            state = GameState.End;
        }
        else if (rules.IsStalemate(white))
        {
            Debug.Log("Stalemate");
            state = GameState.End;
        }
    }

    void Highlight(Piece piece)
    {
        foreach (var move in rules.GetLegalMoves(piece))
        {
            var tile = grid.GetTileAtPosition(move);

            if (board.GetPiece(move.x, move.y) == null)
                tile.SetlegalHighlight(true);
            else
                tile.SetCaptureHighlight(true);
        }
    }

    void ClearLastMoveHighlight()
    {
        if (lastFromTile != null) lastFromTile.SetLatestMoveHighlight(false);
        if (lastToTile != null) lastToTile.SetLatestMoveHighlight(false);
    }
}
