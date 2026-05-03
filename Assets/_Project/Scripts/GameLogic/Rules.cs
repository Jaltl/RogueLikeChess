using System.Collections.Generic;
using UnityEngine;

public class Rules : MonoBehaviour
{

    [SerializeField] private Board board;

    public Dictionary<PieceType, Vector2Int[]> Moves;

    private void Awake()
    {
        Moves = new Dictionary<PieceType, Vector2Int[]>()
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
        }
    };
    }

    public List<Vector2Int> GetLegalMoves(Piece piece)
    {
        var pseudo = GetPseudoMoves(piece);
        var legal = new List<Vector2Int>();

        foreach (var move in pseudo)
        {
            if (SimulateMoveSafe(piece, move))
                legal.Add(move);
        }

        Debug.Log($"{piece.type} at {piece.x},{piece.y} pseudo:{pseudo.Count}");
        return legal;
    }

    // CORE Functions

    List<Vector2Int> GetPseudoMoves(Piece piece)
    {
        //Debug.Log($"Getting pseudo moves for {piece.type} at ({piece.x}, {piece.y}) The Piece is {piece}");
        switch (piece.type)
        {
            case PieceType.Knight:
                return GetStepMoves(piece, Moves[PieceType.Knight]);

            case PieceType.Bishop:
                return GetSlidingMoves(piece, Moves[PieceType.Bishop]);

            case PieceType.Rook:
                return GetSlidingMoves(piece, Moves[PieceType.Rook]);

            case PieceType.Queen:
                return GetSlidingMoves(piece, Moves[PieceType.Queen]);

            case PieceType.King:
                return GetStepMoves(piece, Moves[PieceType.King]);

            case PieceType.Pawn:
                return GetPawnMoves(piece);
        }

        return new List<Vector2Int>();
    }

    List<Vector2Int> GetStepMoves(Piece piece, Vector2Int[] dirs)
    {
        List<Vector2Int> moves = new();

        foreach (var d in dirs)
        {
            int x = piece.x + d.x;
            int y = piece.y + d.y;

            if (!IsInside(x, y)) continue;

            var target = board.GetPiece(x, y);
            if (target == null || target.isWhite != piece.isWhite)
                moves.Add(new Vector2Int(x, y));
        }

        return moves;
    }

    List<Vector2Int> GetSlidingMoves(Piece piece, Vector2Int[] dirs)
    {
        List<Vector2Int> moves = new();

        foreach (var d in dirs)
        {
            int x = piece.x;
            int y = piece.y;

            while (true)
            {
                x += d.x;
                y += d.y;

                if (!IsInside(x, y)) break;

                var target = board.GetPiece(x, y);

                if (target == null)
                {
                    moves.Add(new Vector2Int(x, y));
                }
                else
                {
                    if (target.isWhite != piece.isWhite)
                        moves.Add(new Vector2Int(x, y));
                    break;
                }
            }
        }

        return moves;
    }

    List<Vector2Int> GetPawnMoves(Piece piece)
    {
        List<Vector2Int> moves = new();
        int dir = piece.isWhite ? 1 : -1;

        int forward = piece.y + dir;

        if (IsInside(piece.x, forward) && board.GetPiece(piece.x, forward) == null)
        {
            moves.Add(new Vector2Int(piece.x, forward));

            int doubleY = piece.y + 2 * dir;
            if (!piece.hasMoved && board.GetPiece(piece.x, doubleY) == null)
                moves.Add(new Vector2Int(piece.x, doubleY));
        }

        int[] dx = { -1, 1 };

        foreach (var d in dx)
        {
            int x = piece.x + d;
            int y = piece.y + dir;

            if (!IsInside(x, y)) continue;

            var target = board.GetPiece(x, y);
            if (target != null && target.isWhite != piece.isWhite)
                moves.Add(new Vector2Int(x, y));
        }

        return moves;
    }

    // Check SYSTEM 

    public bool IsInCheck(bool isWhite)
{
    var king = FindKing(isWhite);

    foreach (var p in board.GetAllPieces())
    {
        if (p.isWhite == isWhite) continue;

        var moves = GetPseudoMoves(p); // OK because board is stable here

        foreach (var m in moves)
        {
            if (m.x == king.x && m.y == king.y)
                return true;
        }
    }

    return false;
}

    public bool IsCheckmate(bool isWhite)
    {
        return IsInCheck(isWhite) && !HasMoves(isWhite);
    }

    public bool IsStalemate(bool isWhite)
    {
        return !IsInCheck(isWhite) && !HasMoves(isWhite);
    }

    bool HasMoves(bool isWhite)
    {
        foreach (var p in board.GetAllPieces())
        {
            if (p.isWhite != isWhite) continue;

            if (GetLegalMoves(p).Count > 0)
                return true;
        }

        return false;
    }

    bool SimulateMoveSafe(Piece piece, Vector2Int move)
    {
        Piece captured = board.GetPiece(move.x, move.y);

        int oldX = piece.x;
        int oldY = piece.y;

        // simulate logically only
        piece.x = move.x;
        piece.y = move.y;

        board.SetPiece(oldX, oldY, null);
        board.SetPiece(move.x, move.y, piece);

        bool inCheck = IsInCheck(piece.isWhite);

        // revert
        board.SetPiece(oldX, oldY, piece);
        board.SetPiece(move.x, move.y, captured);

        piece.x = oldX;
        piece.y = oldY;

        Debug.Log($"Testing move {piece.type}: {piece.x},{piece.y} -> {move.x},{move.y}");
        return !inCheck;
    }

    Piece FindKing(bool isWhite)
    {
        foreach (var p in board.GetAllPieces())
        {
            if (p.type == PieceType.King && p.isWhite == isWhite)
                return p;
        }
        return null;
    }

    bool IsInside(int x, int y) => x >= 0 && x < 8 && y >= 0 && y < 8;
}