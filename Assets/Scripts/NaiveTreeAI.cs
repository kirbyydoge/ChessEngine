using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NaiveTreeAI : IChessAI {
    private int depth;
    private const int NEGATIVE_INF = -9999999; // int.MinValue overflows
    private const int POSITIVE_INF =  9999999;
    private int evaluated_moves;

    public NaiveTreeAI(int depth) {
        this.depth = depth;
    }

    public Move play_turn() {
        evaluated_moves = 0;
        return naive_tree_search(depth);
    }

    public int get_evaluated_moves() {
        return evaluated_moves;
    }

    private Move naive_tree_search(int depth) {
        List<Move> all_moves = ChessGame.generate_flat_moves_auto();
        evaluated_moves = all_moves.Count;
        int max_score = NEGATIVE_INF - depth;
        Move max_move = all_moves[0];
        foreach (Move m in all_moves) {
            ChessGame.make_move(m);
            int cur_score = naive_tree_search_aux(depth - 1, false);
            ChessGame.unmake_move(m);
            if (cur_score > max_score) {
                max_score = cur_score;
                max_move = m;
            }
        }
        return max_move;
    }

    private int naive_tree_search_aux(int depth, bool maximizing_player) {
        if (depth == 0) {
            return evaluate_board();
        }
        List<Move> all_moves = ChessGame.generate_flat_moves_auto();
        evaluated_moves += all_moves.Count;
        int value;
        if (maximizing_player) {
            value = NEGATIVE_INF - depth;
            foreach (Move m in all_moves) {
                ChessGame.make_move(m);
                value = Mathf.Max(value, naive_tree_search_aux(depth - 1, false));
                ChessGame.unmake_move(m);
            }
        }
        else {
            value = POSITIVE_INF + depth;
            foreach (Move m in all_moves) {
                ChessGame.make_move(m);
                value = Mathf.Min(value, naive_tree_search_aux(depth - 1, true));
                ChessGame.unmake_move(m);
            }
        }
        return value;
    }

    // TODO: Need IScoringFunction for generalization as well
    public int evaluate_board() {
        int score = 0;
        int white_mul = ChessGame.player_to_move == PieceColor.WHITE ? 1 : -1;
        int black_mul = -white_mul;
        if (ChessGame.is_check_mate) {
            return black_mul * POSITIVE_INF;
        }
        foreach (Piece p in ChessGame.white_pieces) {
            if (p.active) {
                score += white_mul * piece_score(p);
            }
        }
        foreach (Piece p in ChessGame.black_pieces) {
            if (p.active) {
                score += black_mul * piece_score(p);
            }
        }
        return score;
    }

    // TODO: Convert to map? Generalize? IDK
    public int piece_score(Piece p) { 
        switch (p.type) {
            case PieceType.PAWN: return 100;
            case PieceType.KING: return 99999; // Needs to be arbitrarily large
            case PieceType.KNIGHT: return 300;
            case PieceType.BISHOP: return 350;
            case PieceType.QUEEN: return 900;
            case PieceType.ROOK: return 500;
        }
        return 0;
    }
}
