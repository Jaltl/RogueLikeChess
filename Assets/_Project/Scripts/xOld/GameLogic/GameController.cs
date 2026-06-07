using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class GameController : MonoBehaviour
{
    [Header("Game Logic Scripts")]
    [SerializeField] Board board;
    [SerializeField] Rules rules;
    [SerializeField] Gridmanager grid;
    [SerializeField] AnimationController anim;
    [SerializeField] PromotionUI promotionUI;

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
    //public struct EnPassantState
    //{
    //    public Vector2Int targetSquare;
    //    public Vector2Int capturingPawnSquare;
    //}
    //public EnPassantState? enPassant;

    //public Piece enPassantVictim;
    public Vector2Int? enPassantTarget;

    public enum MoveType
    {
        Normal,
        Capture,
        DoublePawn,
        EnPassant,
        CastleKingSide,
        CastleQueenSide,
        Promotion
    }

    public struct Move
    {
        public Vector2Int from;
        public Vector2Int to;

        public MoveType type;

        // optional extra data
        public Vector2Int? captureSquare;
        public bool isPromotion;

        public Move(Vector2Int from, Vector2Int to, MoveType type)
        {
            this.from = from;
            this.to = to;
            this.type = type;

            captureSquare = null;
            isPromotion = false;
        }
    }

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
        if(!isWhite)
        {
            piece.BlackTeam();
        }
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

        // TRY MOVE
        TryMove(x, y);
    }


    void TryMove(int x, int y)
    {
        var moves = rules.GetLegalMoves(selected);

        foreach (var m in moves)
        {
            if (m.to.x == x && m.to.y == y)
            {
                ExecuteMove(selected, m);
                selected = null;
                return;
            }
        }

        selected = null;
        ClearHighlights();
    }

    void ExecuteMove(Piece piece, Move move)
    {
        state = GameState.Animating;

        ClearHighlights();
        ClearLastMoveHighlight();

        Piece target = ResolveTarget(move);
        
        enPassantTarget = null;

        if (target != null)
        {
            board.RemovePiece(target);
        }

        board.MovePiece(piece, move.to.x, move.to.y);

        anim.Play(anim.Move(piece, GetWorldPosition(move.to.x, move.to.y)));

        HandleSpecialMove(move, piece);

        piece.hasMoved = true;

        if (move.type == MoveType.DoublePawn)
        {
            int dir = piece.isWhite ? 1 : -1;

            enPassantTarget = new Vector2Int(move.to.x, move.to.y - dir);
            Vector2Int pawnSquare = move.to;
        }
        else
        {
            enPassantTarget = null;
        }

        if (target != null)
        {
            anim.Play(anim.Capture(target));
        }

        lastFromTile = grid.GetTileAtPosition(new Vector2Int(move.from.x, move.from.y));
        ShowLastMove(move.from, move.to);

        selected = null;
        state = GameState.Selecting;
        SwitchTurn();
        //enPassantTarget = null;
        CheckGameState();
    }

    void HandleSpecialMove(Move move, Piece king)
    {
        switch (move.type)
        {
            case MoveType.CastleKingSide:
                MoveRook(7, 5, king.y);
                break;

            case MoveType.CastleQueenSide:
                MoveRook(0, 3, king.y);
                break;

            case MoveType.Promotion:
                state = GameState.Promotion;
                StartCoroutine(PromotionRoutine(king));
                break;
        }
    }

    public Piece ResolveTarget(Move move)
    {
        switch (move.type)
        {
            case MoveType.Capture:
                return board.GetPiece(move.to.x, move.to.y);

            case MoveType.EnPassant:
                if (move.captureSquare.HasValue)
                    return board.GetPiece(move.captureSquare.Value.x, move.captureSquare.Value.y);
                break;
        }

        return null;
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

        HighlightCheck(white);
    }

    void Highlight(Piece piece)
    {
        var moves = rules.GetLegalMoves(piece);

        foreach (var move in moves)
        {
            var tile = grid.GetTileAtPosition(move.to);
            if (tile == null) continue;

            if (move.type == MoveType.Capture || move.type == MoveType.EnPassant)
            {
                tile.ShowCapture(true);
            }
            else
            {
                tile.ShowMove(true);
            }
        }
    }

    void ClearLastMoveHighlight()
    {
        //Debug.Log("Tried to clear blue");
        if (lastFromTile != null) lastFromTile.ShowLastMove(false);
        //if (lastToTile != null) lastToTile.SetLatestMoveHighlight(false);
    }

    private void ClearHighlights()
    {
        foreach (var tile in grid.GetAllTiles())
        {
            tile.ClearAllHighlight();
        }
    }

    Vector3 GetWorldPosition(int x, int y)
    {
        return new Vector3(x, y, 0);
    }

    void ShowLastMove(Vector2Int from, Vector2Int to)
    {
        ClearLastMoveHighlight();

        grid.GetTileAtPosition(from)?.ShowLastMove(true);
        //grid.GetTileAtPosition(to)?.ShowLastMove();
    }

    void HighlightCheck(bool isWhite)
    {
        // CLEAR ALL check states first
        foreach (var tile in grid.GetAllTiles())
        {
            tile.ClearCheck();
        }

        //Find king
        Piece king = rules.FindKing(isWhite);
        if (king == null) return;


        //Apply if in check
        if (rules.IsInCheck(isWhite))
        {
            Tile kingTile = grid.GetTileAtPosition(new Vector2Int(king.x, king.y));

            if (kingTile != null)
                kingTile.KingInCheck();
        }
    }

    void HandleCastling(Piece king, Move move)
    {
        int dir = (move.to.x > move.from.x) ? 1 : -1;

        int rookX = (dir == 1) ? 7 : 0;
        int rookTargetX = king.x - dir;

        Piece rook = board.GetPiece(rookX, king.y);
        if (rook == null) return;

        board.SetPiece(rookX, king.y, null);
        board.SetPiece(rookTargetX, king.y, rook);

        rook.SetPosition(rookTargetX, king.y);
        anim.Play(anim.Move(rook, GetWorldPosition(rookTargetX, king.y)));
    }

    void MoveRook(int fromX, int toX, int y)
    {
        Piece rook = board.GetPiece(fromX, y);
        if (rook == null) return;

        board.SetPiece(fromX, y, null);
        board.SetPiece(toX, y, rook);

        rook.SetPosition(toX, y);

        anim.Play(anim.Move(rook, GetWorldPosition(toX, y)));
    }

    IEnumerator PromotionRoutine(Piece pawn)
    {
        state = GameState.Promotion;

        bool chosen = false;
        PieceType result = PieceType.Queen;

        promotionUI.Show(choice =>
        {
            result = choice;
            chosen = true;
        });

        yield return new WaitUntil(() => chosen);

        //ReplacePawn(pawn, result);

        state = GameState.Selecting;
    }
}
