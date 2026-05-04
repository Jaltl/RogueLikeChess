using System;
using System.Collections.Generic;
using Unity.Multiplayer.PlayMode;
using UnityEngine;
using static GameController;
using static UnityEngine.Audio.ProcessorInstance;
using static UnityEngine.GraphicsBuffer;

public class Rules : MonoBehaviour
{

    [SerializeField] private Board board;
    [SerializeField] private GameController gameController;

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

    public List<Move> GetLegalMoves(Piece piece)
    {
        var pseudo = GetPseudoMoves(piece);
        var legal = new List<Move>();

        foreach (var move in pseudo)
        {
            if (SimulateMoveSafe(piece, move))
                legal.Add(move);
        }

        //Debug.Log($"{piece.type} at {piece.x},{piece.y} pseudo:{pseudo.Count}");
        return legal;
    }

    // CORE Functions

    List<Move> GetPseudoMoves(Piece piece)
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
                return GetKingMoves(piece);

            case PieceType.Pawn:
                return GetPawnMoves(piece);
        }

        return new List<Move>();
    }

    List<Move> GetStepMoves(Piece piece, Vector2Int[] dirs)
    {
        List<Move> moves = new();

        foreach (var d in dirs)
        {
            int x = piece.x + d.x;
            int y = piece.y + d.y;

            if (!IsInside(x, y)) continue;

            var target = board.GetPiece(x, y);
            if (target == null || target.isWhite != piece.isWhite)
                moves.Add(new Move(new Vector2Int(piece.x, piece.y), new Vector2Int(x, y), target == null ? MoveType.Normal : MoveType.Capture));
        }

        return moves;
    }

    List<Move> GetKingMoves(Piece king)
    {
        List<Move> moves = new();

        foreach (var d in Moves[PieceType.King])
        {
            int x = king.x + d.x;
            int y = king.y + d.y;

            if (!IsInside(x, y)) continue;

            var target = board.GetPiece(x, y);

            if (target != null && target.isWhite == king.isWhite)
                continue;

            // KEY LINE
            if (!IsSquareAttacked(x, y, !king.isWhite))
                moves.Add(new Move(new Vector2Int(king.x, king.y), new Vector2Int(x, y), target == null ? MoveType.Normal : MoveType.Capture));

            if (CanCastleKingside(king))
            {
                moves.Add(new Move(
                    new Vector2Int(king.x, king.y),
                    new Vector2Int(king.x + 2, king.y),
                    MoveType.CastleKingSide
                ));
            }

            if (CanCastleQueenside(king))
            {
                moves.Add(new Move(
                    new Vector2Int(king.x, king.y),
                    new Vector2Int(king.x - 2, king.y),
                    MoveType.CastleQueenSide
                ));
            }
        }

        return moves;
    }

    bool CanCastleKingside(Piece king)
    {
        int y = king.y;

        Piece rook = board.GetPiece(7, y);
        if (rook == null || rook.hasMoved || king.hasMoved) return false;

        // squares between must be empty
        if (board.GetPiece(5, y) != null || board.GetPiece(6, y) != null)
            return false;

        // must not be in check or pass through check
        if (IsSquareAttacked(king.x, y, !king.isWhite)) return false;
        if (IsSquareAttacked(5, y, !king.isWhite)) return false;
        if (IsSquareAttacked(6, y, !king.isWhite)) return false;

        return true;
    }

    bool CanCastleQueenside(Piece king)
    {
        int y = king.y;

        Piece rook = board.GetPiece(0, y);
        if (rook == null || rook.hasMoved || king.hasMoved) return false;

        // squares between must be empty
        if (board.GetPiece(1, y) != null || board.GetPiece(2, y) != null)
            return false;

        // must not be in check or pass through check
        if (IsSquareAttacked(king.x, y, !king.isWhite)) return false;
        if (IsSquareAttacked(1, y, !king.isWhite)) return false;
        if (IsSquareAttacked(2, y, !king.isWhite)) return false;

        return true;
    }

    List<Move> GetSlidingMoves(Piece piece, Vector2Int[] dirs)
    {
        List<Move> moves = new();

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

                var m = new Move(new Vector2Int(piece.x,piece.y), new Vector2Int(x,y), target == null ? MoveType.Normal : MoveType.Capture);

                if (target == null)
                {
                    moves.Add(m);
                }
                else
                {
                    if (target.isWhite != piece.isWhite)
                        moves.Add(m);
                    break;
                }
            }
        }

        return moves;
    }

    //List<Vector2Int> GetPawnMoves(Piece piece)
    //{
    //    List<Vector2Int> moves = new();
    //    int dir = piece.isWhite ? 1 : -1;

    //    int forward = piece.y + dir;

    //    if (IsInside(piece.x, forward) && board.GetPiece(piece.x, forward) == null)
    //    {
    //        moves.Add(new Vector2Int(piece.x, forward));

    //        int doubleY = piece.y + 2 * dir;
    //        if (!piece.hasMoved && board.GetPiece(piece.x, doubleY) == null)
    //            moves.Add(new Vector2Int(piece.x, doubleY));
    //    }

    //    int[] dx = { -1, 1 };

    //    foreach (var d in dx)
    //    {
    //        int x = piece.x + d;
    //        int y = piece.y + dir;

    //        // EN PASSANT
    //        if (gameController.enPassant.HasValue)
    //        {
    //            var ep = gameController.enPassant.Value;

    //            if (ep.targetSquare.x == piece.x + d && ep.targetSquare.y == piece.y + dir)
    //            {
    //                moves.Add(ep.targetSquare);
    //            }
    //        }

    //        if (!IsInside(x, y)) continue;

    //        var target = board.GetPiece(x, y);
    //        if (target != null && target.isWhite != piece.isWhite)
    //            moves.Add(new Vector2Int(x, y));
    //    }

    //    return moves;
    //}
    List<Move> GetPawnMoves(Piece piece)
    {
        List<Move> moves = new();
        int dir = piece.isWhite ? 1 : -1;

        int forward = piece.y + dir;
        int doubleForward = piece.y + 2 * dir;

        // forward move
        if (IsInside(piece.x, forward) && board.GetPiece(piece.x, forward) == null)
        {
            var m = new Move(new Vector2Int(piece.x, piece.y), new Vector2Int(piece.x, forward), MoveType.Normal);

            // promotion check
            if (forward == 0 || forward == 7)
                m.isPromotion = true;

            moves.Add(m);

            // double move
            bool isStartingRank =(piece.isWhite && piece.y == 1) || (!piece.isWhite && piece.y == 6);

            if (isStartingRank && IsInside(piece.x, doubleForward) && board.GetPiece(piece.x, doubleForward) == null)
            {
                moves.Add(new Move(new Vector2Int(piece.x, piece.y), new Vector2Int(piece.x, doubleForward), MoveType.DoublePawn));
            }
        }

        // diagonal captures
        int[] dx = { -1, 1 };

        foreach (var d in dx)
        {
            int x = piece.x + d;
            int y = piece.y + dir;

            if (!IsInside(x, y)) continue;

            var target = board.GetPiece(x, y);

            if (target != null && target.isWhite != piece.isWhite)
            {
                moves.Add(new Move(
                    new Vector2Int(piece.x, piece.y),
                    new Vector2Int(x, y),
                    MoveType.Capture
                ));
            }
        }

        // EN PASSANT
        if (gameController.enPassantTarget.HasValue)
        {
            Vector2Int ep = gameController.enPassantTarget.Value;

            // check both diagonal directions
            foreach (int d in dx)
            {
                int x = piece.x + d;
                int y = piece.y + dir;

                // does this pawn move land on the EP square?
                if (ep.x == x && ep.y == y)
                {
                    Move m = new Move(
                        new Vector2Int(piece.x, piece.y),
                        ep,
                        MoveType.EnPassant
                    );

                    // the pawn being captured is behind the EP square
                    m.captureSquare = new Vector2Int(ep.x, piece.y);

                    moves.Add(m);
                }
            }
        }

        return moves;
    }

    List<Move> GetPawnAttackMoves(Piece piece)
    {
        List<Move> moves = new();
        int dir = piece.isWhite ? 1 : -1;

        int[] dx = { -1, 1 };

        foreach (var d in dx)
        {
            int x = piece.x + d;
            int y = piece.y + dir;

            if (IsInside(x, y))
            {
                var target = board.GetPiece(x, y);
                moves.Add(new Move(new Vector2Int(piece.x, piece.y), new Vector2Int(x, y), target == null ? MoveType.Normal : MoveType.Capture));
            }
        }
        return moves;
    }

    // Check SYSTEM 

    public bool IsInCheck(bool isWhite)
{
    var king = FindKing(isWhite);

    return IsSquareAttacked(king.x, king.y, !isWhite);

        //foreach (var p in board.GetAllPieces())
        //{
        //    if (p.isWhite == isWhite) continue;

        //    var moves = GetPseudoMoves(p); // OK because board is stable here

        //    foreach (var m in moves)
        //    {
        //        if (m.x == king.x && m.y == king.y)
        //            return true;
        //    }
        //}

        //return false;
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

    bool SimulateMoveSafe(Piece piece, Move move)
    {
        var captured = gameController.ResolveTarget(move);

        int oldX = piece.x;
        int oldY = piece.y;

        // apply
        board.SetPiece(oldX, oldY, null);
        board.SetPiece(move.to.x, move.to.y, piece);
        piece.SetPosition(move.to.x, move.to.y);

        bool inCheck = IsInCheck(piece.isWhite);

        // revert
        board.SetPiece(oldX, oldY, piece);
        board.SetPiece(move.to.x, move.to.y, captured);
        piece.SetPosition(oldX, oldY);

        return !inCheck;
    }

    public Piece FindKing(bool isWhite)
    {
        foreach (var p in board.GetAllPieces())
        {
            if (p.type == PieceType.King && p.isWhite == isWhite)
                return p;
        }
        return null;
    }

    bool IsInside(int x, int y) => x >= 0 && x < 8 && y >= 0 && y < 8;

    public bool IsSquareAttacked(int x, int y, bool byWhite)
    {
        foreach (var piece in board.GetAllPieces())
        {
            if (piece.isWhite != byWhite) continue;

            var attacks = GetAttackMoves(piece);

            foreach (var move in attacks)
            {
                if (move.to.x == x && move.to.y == y)
                    return true;
            }
        }

        return false;
    }

    List<Move> GetAttackMoves(Piece piece)
    {
        switch (piece.type)
        {
            case PieceType.Pawn:
                return GetPawnAttackMoves(piece); // ONLY diagonals

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
        }

        return new List<Move>();
    }
}