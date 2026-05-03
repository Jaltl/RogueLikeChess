using UnityEditor.Search;
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

    public enum GameState
    {
        Selecting,
        PieceSelected,
        Animating,
        Promotion,
        Checkmate,
        Stalemate
    }

    public enum Player
    {
        White,
        Black
    }

    public Player currentPlayer;
    public GameState state;
    private Tile lastFromTile;
    private Tile lastToTile;
    public Vector2Int? enPassantTarget;

    private void Start()
    {
        SetupBoard();
        currentPlayer = Player.White;
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
        Piece piece = Instantiate(prefab);
        piece.isWhite = isWhite;
        piece.type = prefab.type;
        piece.SetPosition(x, y);
        board.SetPiece(x, y, piece);
        piece.transform.position = GetWorldPosition(x, y);
    }

    public void OnTileClicked(Tile tile)
    {
        if (anim.IsAnimating()) return;

        int x = tile.x;
        int y = tile.y;

        Piece clicked = board.GetPiece(x, y);
        bool whiteTurn = currentPlayer == Player.White;

        // SELECT
        if (selected == null)
        {
            if (clicked != null && clicked.isWhite == whiteTurn)
            {
                ClearHighlights();

                selected = clicked;
                Highlight(selected);
            }
            return;
        }

        // RESELECT
        if (clicked != null && clicked.isWhite == whiteTurn)
        {
            ClearHighlights();

            selected = clicked;
            Highlight(selected);
            return;
        }

        // 🔹 TRY MOVE
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
        ClearHighlights();
    }

    void ExecuteMove(Piece piece, int x, int y)
    {
        state = GameState.Animating;

        Vector2Int from = new(piece.x, piece.y);
        Vector2Int to = new(x, y);

        var target = board.GetPiece(x, y);

        ClearHighlights();

        if (target != null)
            anim.Play(anim.Capture(target));

        board.MovePiece(piece, x, y);

        anim.Play(anim.Move(piece, GetWorldPosition(x, y)));

        piece.hasMoved = true;

        ShowLastMove(from, to);

        currentPlayer = (currentPlayer == Player.White) ? Player.Black : Player.White;
        CheckGameState();
    }

    void SwitchTurn()
    {
        currentPlayer = (currentPlayer == Player.White)
            ? Player.Black
            : Player.White;
    }

    void CheckGameState()
    {
        bool white = currentPlayer == Player.White;

        if (rules.IsCheckmate(white))
        {
            Debug.Log("Checkmate");
            state = GameState.Checkmate;
        }
        else if (rules.IsStalemate(white))
        {
            Debug.Log("Stalemate");
            state = GameState.Stalemate;
        }
    }

    void Highlight(Piece piece)
    {
        foreach (var move in rules.GetLegalMoves(piece))
        {
            var tile = grid.GetTileAtPosition(move);
            if (tile == null) continue;

            var target = board.GetPiece(move.x, move.y);

            if (target == null)
                tile.ShowMove();
            else
                tile.ShowCapture();
        }
    }

    void ClearLastMoveHighlight()
    {
        if (lastFromTile != null) lastFromTile.HideGlow();
        //if (lastToTile != null) lastToTile.SetLatestMoveHighlight(false);
    }

    private void ClearHighlights()
    {
        foreach (var tile in grid.GetAllTiles())
        {
            tile.HideGlow();
        }
    }

    Vector3 GetWorldPosition(int x, int y)
    {
        return new Vector3(x, y, 0);
    }

    void ShowLastMove(Vector2Int from, Vector2Int to)
    {
        ClearLastMoveHighlight();

        grid.GetTileAtPosition(from)?.ShowLastMove();
        //grid.GetTileAtPosition(to)?.ShowLastMove();
    }
}
