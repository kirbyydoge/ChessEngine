using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Coordinate {
    public int rank;
    public int file;

    public Coordinate(int rank, int file) {
        this.rank = rank;
        this.file = file;
    }
};

public struct Move {
    public Piece held;
    public Piece target;
    public Coordinate begin;
    public Coordinate end;
    public bool en_passant;
    public int castle;

    public Move(Piece held, Piece target, Coordinate begin, Coordinate end) {
        this.held = held;
        this.target = target;
        this.begin = begin;
        this.end = end;
        en_passant = false;
        castle = 0;
    }

    public Move(Piece held, Piece target, Coordinate begin, Coordinate end, bool en_passant, int castle) {
        this.held = held;
        this.target = target;
        this.begin = begin;
        this.end = end;
        this.en_passant = en_passant;
        this.castle = castle;
    }
};

public enum PieceColor {
    BLACK, WHITE
};

public enum PieceType {
    PAWN, KNIGHT, BISHOP, ROOK, QUEEN, KING
};

public static class ChessGame {
    public static PieceColor player_to_move;
    public static Piece[,] board;
    public static List<Piece> white_pieces;
    public static List<Piece> black_pieces;
    public static Piece white_king;
    public static Piece black_king;
    public static int turn;
    public static bool is_check_mate;

    static ChessGame() {
        board = new Piece[8, 8];
        white_pieces = new List<Piece>();
        black_pieces = new List<Piece>();
        turn = 0;
        for (int i = 0; i < board.Rank; i++) {
            for (int j = 0; j < board.GetLength(i); j++) {
                board[i, j] = null;
            }
        }
        load_fen("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
    }

    public static void load_fen(string FEN) {
        int row = 7;
        int col = 0;
        string[] parts = FEN.Split(' ');
        string position = parts[0];
        string to_play = parts[1];
        for (int i = 0; i < position.Length; i++) {
            char cmd = position[i];
            Piece piece = null;
            if (cmd == 'p') { piece = new Piece(PieceColor.BLACK, PieceType.PAWN); }
            else if (cmd == 'P') { piece = new Piece(PieceColor.WHITE, PieceType.PAWN); }
            else if (cmd == 'n') { piece = new Piece(PieceColor.BLACK, PieceType.KNIGHT); }
            else if (cmd == 'N') { piece = new Piece(PieceColor.WHITE, PieceType.KNIGHT); }
            else if (cmd == 'b') { piece = new Piece(PieceColor.BLACK, PieceType.BISHOP); }
            else if (cmd == 'B') { piece = new Piece(PieceColor.WHITE, PieceType.BISHOP); }
            else if (cmd == 'k') { piece = new Piece(PieceColor.BLACK, PieceType.KING); black_king = piece; }
            else if (cmd == 'K') { piece = new Piece(PieceColor.WHITE, PieceType.KING); white_king = piece; }
            else if (cmd == 'q') { piece = new Piece(PieceColor.BLACK, PieceType.QUEEN); }
            else if (cmd == 'Q') { piece = new Piece(PieceColor.WHITE, PieceType.QUEEN); }
            else if (cmd == 'r') { piece = new Piece(PieceColor.BLACK, PieceType.ROOK); }
            else if (cmd == 'R') { piece = new Piece(PieceColor.WHITE, PieceType.ROOK); }
            else if (cmd == '/') { col = 0; row--; }
            else if (char.IsDigit(cmd)) {
                col += cmd - '0';
            }

            if (piece != null) {
                piece.location = new Coordinate(row, col);
                board[row, col++] = piece;
                if (piece.color == PieceColor.WHITE) {
                    piece.lookup_index = white_pieces.Count;
                    white_pieces.Add(piece);
                }
                else {
                    piece.lookup_index = black_pieces.Count;
                    black_pieces.Add(piece);
                }
            }
        }
        if (to_play[0] == 'w') player_to_move = PieceColor.WHITE;
        else player_to_move = PieceColor.BLACK;
    }

    public static bool is_valid_cell(Coordinate cell) {
        return !(cell.rank < 0 || cell.rank > 7 || cell.file < 0 || cell.file > 7);
    }

    public static bool is_pickable_cell(Coordinate cell) {
        return !(cell.rank < 0 || cell.rank > 7 || cell.file < 0 || cell.file > 7) && board[cell.rank, cell.file] != null;
    }

    public static Piece get_piece(Coordinate cell) {
        if (!is_valid_cell(cell)) return null;
        return board[cell.rank, cell.file];
    }

    // Removed bound check. Move generation algorithms avoid duplicate calls to increase speed.
    public static Piece get_piece_unsafe(Coordinate cell) {
        return board[cell.rank, cell.file];
    }

    public static Piece pick_up(Coordinate cell) {
        if (!is_pickable_cell(cell) || board[cell.rank, cell.file].color != player_to_move) return null;
        Piece piece = board[cell.rank, cell.file];
        return piece;
    }

    public static void make_move(Move move) {
        if (move.en_passant) {
            board[move.begin.rank, move.begin.file] = null;
            board[move.begin.rank, move.end.file] = null;
            board[move.end.rank, move.end.file] = move.held;
            move.target.active = false;
        }
        else if (move.castle != 0) {
            board[move.begin.rank, move.begin.file] = null;
            board[move.end.rank, move.castle < 0 ? 0 : 7] = null;
            board[move.end.rank, move.end.file] = move.held;
            board[move.end.rank, move.end.file - move.castle] = move.target;
            move.target.location.file = move.end.file - move.castle;
        }
        else {
            board[move.begin.rank, move.begin.file] = null;
            board[move.end.rank, move.end.file] = move.held;
            if (move.target != null) {
                move.target.active = false;
            }
        }
        
        player_to_move = player_to_move == PieceColor.WHITE ? PieceColor.BLACK : PieceColor.WHITE;

        if (move.held.move_count == 0) { 
            move.held.first_move = turn;
        }
        move.held.location = move.end;
        move.held.move_count++;
        turn++;
    }

    public static void unmake_move(Move move) {
        if (move.en_passant) {
            board[move.begin.rank, move.begin.file] = move.held;
            board[move.begin.rank, move.end.file] = move.target;
            board[move.end.rank, move.end.file] = null;
            move.target.active = true;
        }
        else if (move.castle != 0) {
            board[move.begin.rank, move.begin.file] = move.held;
            board[move.end.rank, move.castle < 0 ? 0 : 7] = move.target;
            board[move.end.rank, move.end.file] = null;
            board[move.end.rank, move.end.file - move.castle] = null;
            move.target.location.file = move.castle < 0 ? 0 : 7;
        }
        else {
            board[move.begin.rank, move.begin.file] = move.held;
            board[move.end.rank, move.end.file] = move.target;
            if (move.target != null) {
                move.target.active = true;
            }
        }

        player_to_move = player_to_move == PieceColor.WHITE ? PieceColor.BLACK : PieceColor.WHITE;

        if (move.held.move_count == 1) {
            move.held.first_move = -1;
        }
        move.held.location = move.begin;
        move.held.move_count--;
        turn--;
    }

    // We are intentionally avoiding polymorphism here. Readability comes second for this section.
    // Since the engine needs to generate moves millions of times, it needs to be as fast as possible.
    public static List<Move> generate_moves(Coordinate begin) {
        List<Move> moves = null;
        Piece piece = pick_up(begin);
        Piece king = player_to_move == PieceColor.WHITE ? white_king : black_king;
        switch (piece.type) {
            case PieceType.PAWN: moves = Piece.generate_pawn_moves(piece, begin); break;
            case PieceType.KNIGHT: moves = Piece.generate_knight_moves(piece, begin); break;
            case PieceType.BISHOP: moves = Piece.generate_bishop_moves(piece, begin); break;
            case PieceType.ROOK: moves = Piece.generate_rook_moves(piece, begin); break;
            case PieceType.QUEEN: moves = Piece.generate_queen_moves(piece, begin); break;
            case PieceType.KING: moves = Piece.generate_king_moves(piece, begin); break;
        }
        for (int i = moves.Count - 1; i >= 0; i--) {
            Move cur_move = moves[i];
            make_move(cur_move);
            if (Piece.cell_under_attack(king.location, king.color)) {
                moves.RemoveAt(i);
            }
            unmake_move(cur_move);
        }
        return moves;
    }

    public static List<List<Move>> generate_all_moves_auto() {
        return generate_all_moves(player_to_move == PieceColor.WHITE ? white_pieces : black_pieces);
    }

    public static List<List<Move>> generate_all_moves(List<Piece> pieces) {
        List<List<Move>> moves = new List<List<Move>>();
        is_check_mate = true;
        foreach (Piece p in pieces) {
            List<Move> piece_moves = null;
            if (p.active) {
                piece_moves = generate_moves(p.location);
                if (piece_moves.Count > 0) {
                    is_check_mate = false;
                }
            }
            moves.Add(piece_moves);
        }
        return moves;
    }
}
