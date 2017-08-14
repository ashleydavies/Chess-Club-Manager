namespace Chess_Club.Enums {
    public enum Outcome {
        WHITE_WIN,
        BLACK_WIN,
        WHITE_RESIGN,
        BLACK_RESIGN,
        DRAW
    }

    public enum DrawTypes {
        AGREEMENT,
        STALEMATE,
        FIFTY_MOVE_RULE,
        REPEATED_MOVES,
        INSUFFICIENT_CHECKMATING_MATERIAL
    }
}