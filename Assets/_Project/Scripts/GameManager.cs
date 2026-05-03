using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public enum GameState
    {
        WhiteTurn,
        BlackTurn,
        Promotion,
        Checkmate
    }

    public GameState state;

    public Piece[,] _boardState = new Piece[8, 8];

    private Piece _selectedPiece;

    [SerializeField] Gridmanager _gridManager;

    private bool isAnimating = false;
    private Queue<IEnumerator> animationQueue = new Queue<IEnumerator>();



    [SerializeField] private Piece knightPrefab;
    [SerializeField] private Piece kingPrefab;
    [SerializeField] private Piece queenPrefab;
    [SerializeField] private Piece pawnPrefab;
    [SerializeField] private Piece bishopPrefab;
    [SerializeField] private Piece rookPrefab;

    private Vector2Int? enPassantTarget;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
             DontDestroyOnLoad(gameObject);        }
    }

    private void Start()
    {
        SetupBoard();
    }

    void PlayAnimation(IEnumerator anim)
    {
        animationQueue.Enqueue(anim);

        if (!isAnimating)
            StartCoroutine(ProcessAnimations());
    }

    IEnumerator ProcessAnimations()
    {
        isAnimating = true;

        while (animationQueue.Count > 0)
        {
            yield return StartCoroutine(animationQueue.Dequeue());
        }

        isAnimating = false;
    }

    void SwitchTurn()
    {
        state = (state == GameState.WhiteTurn)
            ? GameState.BlackTurn
            : GameState.WhiteTurn;
    }

    bool isWhiteTurn()
    {
        return state == GameState.WhiteTurn;
    }

    private void SpawnPiece(Piece prefab, int x, int y, bool isWhite)
    {
        Piece piece = Instantiate(prefab);

        piece.type = prefab.type;
        piece.isWhite = isWhite;


        piece.SetPosition(x, y);
        piece.transform.position = GetWorldPosition(x, y);

        _boardState[x, y] = piece;
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

    bool IsInsideBoard(int x, int y)
    {
        return x >= 0 && x < 8 && y >= 0 && y < 8;
    }

    Vector3 GetWorldPosition(int x, int y)
    {
        return new Vector3(x, y, 0);
    }

    private List<Vector2Int> GetKnightMoves(Piece piece)
    {
        List<Vector2Int> moveList = new List<Vector2Int>();

        foreach (var dir in Moves[PieceType.Knight])
        {
            int newX = piece.x + dir.x;
            int newY = piece.y + dir.y;

            if(IsInsideBoard(newX, newY))
            {
                Piece target = _boardState[newX, newY];

                if (target == null ||target.isWhite != piece.isWhite)
                {
                    moveList.Add(new Vector2Int(newX, newY));
                }

            }
        }
        return moveList;
    }

    private List<Vector2Int> GetKingMoves(Piece piece)
    {
        List<Vector2Int> moveList = new List<Vector2Int>();

        foreach (var dir in Moves[PieceType.King])
        {
            int newX = piece.x + dir.x;
            int newY = piece.y + dir.y;

            if (IsInsideBoard(newX, newY))
            {
                Piece target = _boardState[newX, newY];

                if (target == null || target.isWhite != piece.isWhite)
                    moveList.Add(new Vector2Int(newX, newY));
            }
        }

        if (!piece.hasMoved && !IsInCheck(piece.isWhite))
        {
            TryCastling(piece, 1, moveList);
            TryCastling(piece, -1, moveList);
        }

        return moveList;
    }

    void TryCastling(Piece king, int direction, List<Vector2Int> moves)
    {
        int rookX = direction == 1 ? 7 : 0;

        Piece rook = _boardState[rookX, king.y];
        if (rook == null || rook.hasMoved) return;

        for (int x = Mathf.Min(king.x, rookX) + 1; x < Mathf.Max(king.x, rookX); x++)
        {
            if (_boardState[x, king.y] != null) return;
        }

        for (int x = king.x; x != king.x + 2 * direction; x += direction)
        {
            if (IsSquareAttacked(x, king.y, !king.isWhite))
                return;
        }

        moves.Add(new Vector2Int(king.x + 2 * direction, king.y));
    }

    List<Vector2Int> GetSlidingMoves(Piece piece, Vector2Int[] directions)
    {
        List<Vector2Int> movesList = new List<Vector2Int>();

        foreach (var dir in directions)
        {
            int x = piece.x;
            int y = piece.y;

            while (true)
            {
                x += dir.x;
                y += dir.y;

                if (!IsInsideBoard(x, y)) break;

                Piece target = GetPiece(x,y);

                if (target == null)
                {
                    movesList.Add(new Vector2Int(x, y));
                }
                else
                {
                    if (target.isWhite != piece.isWhite)
                        movesList.Add(new Vector2Int(x, y));

                    break; // blocked
                }
            }
        }

        return movesList;
    }

    List<Vector2Int> GetPawnMoves(Piece piece)
    {
        List<Vector2Int> movesList = new List<Vector2Int>();

        int dir = piece.isWhite ? 1 : -1;

        int forwardY = piece.y + dir;

        if (enPassantTarget.HasValue)
        {
            Vector2Int ep = enPassantTarget.Value;

            if (Mathf.Abs(ep.x - piece.x) == 1 && ep.y == piece.y + dir)
            {
                movesList.Add(ep);
            }
        }

        // Forward 1
        if (IsInsideBoard(piece.x, forwardY) &&
            _boardState[piece.x, forwardY] == null)
        {
            movesList.Add(new Vector2Int(piece.x, forwardY));

            // Forward 2 (only if first move)
            int doubleY = piece.y + 2 * dir;

            if (!piece.hasMoved &&
                IsInsideBoard(piece.x, doubleY) &&
                _boardState[piece.x, doubleY] == null)
            {
                movesList.Add(new Vector2Int(piece.x, doubleY));
            }
        }

        // Attacks
        int[] dx = { -1, 1 };

        foreach (int offset in dx)
        {
            int x = piece.x + offset;
            int y = piece.y + dir;

            if (!IsInsideBoard(x, y)) continue;

            Piece target = GetPiece(x,y);

            if (target != null && target.isWhite != piece.isWhite)
            {
                movesList.Add(new Vector2Int(x, y));
            }
        }

        return movesList;
    }

    List<Vector2Int> GetLegalMoves(Piece piece)
    {
        switch (piece.type)
        {
            case PieceType.Knight:
                return GetKnightMoves(piece);

            case PieceType.Bishop:
                return GetSlidingMoves(piece, Moves[PieceType.Bishop]);

            case PieceType.Rook:
                return GetSlidingMoves(piece, Moves[PieceType.Rook]);

            case PieceType.Queen:
                return GetSlidingMoves(piece, Moves[PieceType.Queen]);

            case PieceType.King:
                return GetKingMoves(piece);

            case PieceType.Pawn:
                return GetPawnMoves(piece);
        }

        return new List<Vector2Int>();
    }

    List<Vector2Int> GetLegalMovesSafe(Piece piece)
    {
        List<Vector2Int> pseudoMoves = GetLegalMoves(piece);
        List<Vector2Int> legalMoves = new List<Vector2Int>();

        foreach (var move in pseudoMoves)
        {
            if (SimulateMovesSavesKing(piece, move))
            {
                legalMoves.Add(move);
            }
        }

        return legalMoves;
    }

    public Dictionary<PieceType, Vector2Int[]> Moves = new Dictionary<PieceType, Vector2Int[]>()
    {
        {PieceType.Knight, new Vector2Int[]
            {
                new Vector2Int(1, 2), new Vector2Int(2, 1),
                new Vector2Int(-1, 2), new Vector2Int(-2, 1),
                new Vector2Int(1, -2), new Vector2Int(2, -1),
                new Vector2Int(-1, -2), new Vector2Int(-2, -1),
            }
        },

        {PieceType.Bishop, new Vector2Int[]
            {
                new Vector2Int(1, 1), new Vector2Int(-1, 1),
                new Vector2Int(1, -1), new Vector2Int(-1, -1),
            }
         },
        {PieceType.Rook, new Vector2Int[]
            {
                new Vector2Int(1, 0), new Vector2Int(-1, 0),
                new Vector2Int(0, 1), new Vector2Int(0, -1),
            }
        },
        {PieceType.Queen, new Vector2Int[]
            {
                new Vector2Int(1, 0), new Vector2Int(-1, 0),
                new Vector2Int(0, 1), new Vector2Int(0, -1),
                new Vector2Int(1, 1), new Vector2Int(-1, 1),
                new Vector2Int(1, -1), new Vector2Int(-1, -1),
            }
        },
        {PieceType.King, new Vector2Int[]
            {
                new Vector2Int(1, 0), new Vector2Int(-1, 0),
                new Vector2Int(0, 1), new Vector2Int(0, -1),
                new Vector2Int(1, 1), new Vector2Int(-1, 1),
                new Vector2Int(1, -1), new Vector2Int(-1, -1),
            }
        },

        {PieceType.Pawn, new Vector2Int[]
            {
                new Vector2Int(0, 1)
            }
        }
    };

    public void OnTileClicked(Tile tile)
    {
        if (isAnimating || state == GameState.Promotion || state == GameState.Checkmate)
        {
            Debug.Log("Animation in progress or game over, click ignored.");
            return;
        }
        int x = tile.x;
        int y = tile.y;

        Piece clickedPiece = GetPiece(x, y);

        // Nothing selected yet -> try selecting
        if (_selectedPiece == null)
        {
            if (clickedPiece != null && clickedPiece.isWhite == (state == GameState.WhiteTurn))
            {
                _selectedPiece = clickedPiece;
                HighlightMoves(_selectedPiece);
            }

            return;
        }

        // Clicking same color piece -> reselect
        if (clickedPiece != null && clickedPiece.isWhite == (state == GameState.WhiteTurn))
        {
            _selectedPiece = clickedPiece;
            HighlightMoves(_selectedPiece);
            return;
        }

        // Otherwise -> try move
        TryMove(x, y);
    
    }

    private void TryMove(int targetX, int targetY)
    {
        var legalMoves = GetLegalMovesSafe(_selectedPiece);

        foreach (var move in legalMoves)
        {
            if (move.x == targetX && move.y == targetY)
            {
                MovePiece(_selectedPiece, targetX, targetY);
                _selectedPiece = null;
                return;
            }
        }

        _selectedPiece = null; // Deselect if move is illegal
    }

    private void MovePiece(Piece piece, int newX, int newY)
    {
        _boardState[piece.x, piece.y] = null; // Clear old position

        Vector2Int? newEnPassantTarget = null;

        if (piece.type == PieceType.Pawn)
        {
            int dir = piece.isWhite ? 1 : -1;

            if (Mathf.Abs(newY - piece.y) == 2)
            {
                newEnPassantTarget = new Vector2Int(piece.x, piece.y + dir);
            }

            if ((piece.isWhite && newY == 7) || (!piece.isWhite && newY == 0))
            {
                //StartCoroutine(PromotionCoroutine(piece));
            }

            if(enPassantTarget.HasValue)
            {
                if(newX == enPassantTarget.Value.x && newY == enPassantTarget.Value.y)
                {
                    int pawnY = piece.isWhite ? newY - 1 : newY + 1;

                    Piece capturePawn = _boardState[newX, pawnY];

                    if (capturePawn != null)
                    {
                        _boardState[newX, pawnY] = null;
                        StartCoroutine(CapturePiece(capturePawn));
                    }
                }
            }
        }

        if (piece.type == PieceType.King)
        {
            int deltaX = newX - piece.x;

            if (Mathf.Abs(deltaX) == 2)
            {
                // castling
                int rookOldX = deltaX > 0 ? 7 : 0;
                int rookNewX = deltaX > 0 ? newX - 1 : newX + 1;

                Piece rook = _boardState[rookOldX, piece.y];

                _boardState[rookOldX, piece.y] = null;
                _boardState[rookNewX, piece.y] = rook;

                rook.SetPosition(rookNewX, piece.y);
                rook.transform.position = GetWorldPosition(rookNewX, piece.y);
                rook.hasMoved = true;
            }
        }

        if (_boardState[newX, newY] != null)
        {
            Piece targetPiece = _boardState[newX, newY];
            StartCoroutine(CapturePiece(targetPiece));
        }

        _boardState[newX, newY] = piece; // Update board state


        piece.SetPosition(newX, newY);
        piece.hasMoved = true;
        PlayAnimation(MovePieceSmooth(piece, GetWorldPosition(newX, newY)));

        SwitchTurn(); // Switch turn

        ClearHighlights();

        bool currentPlayer = (state == GameState.WhiteTurn);

        if (IsInCheck(currentPlayer))
        {
            Debug.Log((currentPlayer ? "White" : "Black") + " is in check!");
        }

        if (IsCheckmate(currentPlayer))
        {
            state = GameState.Checkmate;
            Debug.Log((currentPlayer ? "White" : "Black") + " is in checkmate! Game Over.");
        }

        if (IsStalemate(currentPlayer))
        {
            state = GameState.Checkmate;
            Debug.Log("Stalemate! Game Over.");
        }  

        enPassantTarget = newEnPassantTarget;
    }

    IEnumerator MovePieceSmooth(Piece piece, Vector3 targetPos)
    {
        float duration = 0.2f;
        float time = 0;

        Vector3 start = piece.transform.position;

        while (time < duration)
        {
            float t = time / duration;

            // Smoothstep easing
            t = t * t * (3f - 2f * t);

            piece.transform.position = Vector3.Lerp(start, targetPos, t);

            time += Time.deltaTime;
            yield return null;
        }

        piece.transform.position = targetPos;
    }

    IEnumerator CapturePiece(Piece piece)
    {
        float t = 0;
        Vector3 startScale = piece.transform.localScale;

        while (t < 0.2f)
        {
            piece.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t / 0.2f);
            t += Time.deltaTime;
            yield return null;
        }

        Destroy(piece.gameObject);
    }

    private void HighlightMoves(Piece piece)
    {
        ClearHighlights();

        var moves = GetLegalMovesSafe(piece);

        foreach (var move in moves)
        {
            Tile tile = _gridManager.GetTileAtPosition(move);
            if (tile == null) continue;

            Piece target = _boardState[move.x, move.y];

            if (target == null)
            {
                tile.SetlegalHighlight(true);
            }

            else if (target.isWhite != piece.isWhite)
            {
                tile.SetCaptureHighlight(true);
            }
        }
    }

    private void ClearHighlights()
    {
        foreach (var tile in _gridManager.GetAllTiles())
        {
            tile.ClearHighlight();
        }
    }

    Piece FindKing(bool iswhite)
    {
        foreach (var piece in _boardState)
        {
            if (piece == null) continue;

            if (piece.type == PieceType.King && piece.isWhite == iswhite)
            {
                return piece;
            }
        }
        return null;
    }

    bool IsSquareAttacked(int x, int y, bool byWhite)
    {
        foreach (var piece in _boardState)
        {
            if (piece == null || piece.isWhite != byWhite) continue;

            var moves = GetLegalMoves(piece);

            foreach (var move in moves)
            {
                if (move.x == x && move.y == y)
                {
                    return true;
                }
            }
        }
        return false;
    }

    bool IsInCheck(bool isWhiteKing)
    {
        Piece king = FindKing(isWhiteKing);
        if (king == null) return false; // Should never happen

        bool opponent = !isWhiteKing;

        return IsSquareAttacked(king.x, king.y, opponent);
    }

    bool HasAnyLegalMoves(bool forWhite)
    {
        foreach (var piece in _boardState)
        {
            if (piece == null || piece.isWhite != forWhite) continue;
            var moves = GetLegalMovesSafe(piece);
            if (moves.Count > 0) return true;
        }
        return false;
    }

    bool IsStalemate(bool forWhite)
    {
        return !IsInCheck(forWhite) && !HasAnyLegalMoves(forWhite);
    }

    bool SimulateMovesSavesKing(Piece piece, Vector2Int move)
    {
        int oldX = piece.x;
        int oldY = piece.y;

        Piece captured = _boardState[move.x, move.y];

        //Simulate move
        _boardState[oldX, oldY] = null;
        _boardState[move.x, move.y] = piece;

        piece.x = move.x;
        piece.y = move.y;

        bool inCheck = IsInCheck(piece.isWhite);

        // Revert move
        _boardState[oldX, oldY] = piece;
        _boardState[move.x, move.y] = captured;

        piece.x = oldX;
        piece.y = oldY;

        return !inCheck;
    }

    bool IsCheckmate(bool forWhite)
    {
        return IsInCheck(forWhite) && !HasAnyLegalMoves(forWhite);
    }

    //IEnumerator PromotionCoroutine(Piece pawn)
    //{
    //    yield return new WaitUntil(() => promotionUI.IsChoiceMade); // Wait for player to choose promotion piece

    //    PieceType newType = promotionUI.SelectedPiece; // Get selected piece type from UI

    //    PromotePawn(pawn, newType); // Replace pawn with new piece
    //}

    //void PromotePawn(Piece pawn, PieceType newType)
    //{
    //    Piece prefab = GetPrefab(newType); //fix later

    //    Piece newPiece = Instantiate(prefab, pawn.transform.position, Quaternion.identity);

    //    newPiece.type = newType;
    //    newPiece.isWhite = pawn.isWhite;
    //    newPiece.SetPosition(pawn.x, pawn.y);

    //    _boardState[pawn.x, pawn.y] = newPiece;

    //    Destroy(pawn.gameObject);
    //}

    Piece GetPiece(int x, int y)
    {
        if (!IsInsideBoard(x, y)) return null;
        return _boardState[x, y];
    }
}
