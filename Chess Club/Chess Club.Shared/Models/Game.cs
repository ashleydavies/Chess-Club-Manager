using System;
using System.Collections.Generic;
using System.Text;
using Chess_Club.Enums;
using System.Linq;

namespace Chess_Club.Models {
    public class Game : Model
    {
        /// <summary>
        /// Outcome of the game
        /// </summary>
        public Outcome Outcome { get; set; }

        /// <summary>
        /// Draw Type of the game if applicable
        /// </summary>
        public DrawTypes DrawType { get; set; } 

        /// <summary>
        /// ID of the white player
        /// </summary>
        public int WhitePlayerID;

        /// <summary>
        /// ID of the black player
        /// </summary>
        public int BlackPlayerID;

        /// <summary>
        /// ID of the session the game was played in
        /// </summary>
        public int SessionID;

        /// <summary>
        /// ELO of the white player at the time of play
        /// </summary>
        public int WhitePlayerELO;

        /// <summary>
        /// ELO of the black player at the time of play
        /// </summary>
        public int BlackPlayerELO;

        /// <summary>
        /// KFactor of the white player at the time of play
        /// </summary>
        public int WhitePlayerKFactor;

        /// <summary>
        /// KFactor of the black player at the time of play
        /// </summary>
        public int BlackPlayerKFactor;

        /// <summary>
        /// Simple wrapper around WhitePlayerID to allow the program to dynamically request the Member rather than ID
        /// </summary>
        public Member WhitePlayer
        {
            get
            {
                // Grab the member from the database using a quick where lambda expression to match the ID
                return App.db.Members.Where(wp => wp.ID == WhitePlayerID).First();
            }
            set
            {
                // Set the ID for the object based on the ID given
                WhitePlayerID = value.ID;
            }
        }

        /// <summary>
        /// Simple wrapper around BlackPlayerID to allow the program to dynamically request the Member rather than ID
        /// </summary>
        public Member BlackPlayer
        {
            get
            {
                // Grab the member from the database using a quick where lambda expression to match the ID
                return App.db.Members.Where(bp => bp.ID == BlackPlayerID).First();
            }
            set
            {
                // Set the ID for the object based on the ID given
                BlackPlayerID = value.ID;
            }
        }

        /// <summary>
        /// Property which generates a string representing the winner upon request
        /// </summary>
        public string Winner
        {
            get
            {
                switch (Outcome)
                {
                    // If black won outright or white resigned
                    case Outcome.BLACK_WIN:
                    case Outcome.WHITE_RESIGN:
                        // Black won
                        return "Black";
                    // If white won outright or black resigned
                    case Outcome.WHITE_WIN:
                    case Outcome.BLACK_RESIGN:
                        // White won
                        return "White";
                    default:
                        // It must have been a draw
                        return "Draw";
                }
            }
        }

        /// <summary>
        /// Calculate the probability of the better player winning
        /// </summary>
        /// <param name="probability">Pass in a double reference which is set</param>
        /// <returns>Outcome representing the expected outcome (To get propery, pass in reference to double)</returns>
        public Outcome CalculateExpectedOutcome(out double probability)
        {
            // Formula for expected probability:
            //
            //  Expected White =
            //                 1
            //    ------------------------------
            //           (Black ELO - White ELO)
            //           -----------------------
            //                     400
            //    1 + 10^
            //
            //  Swap Black for White and vice versa for opponent. 
            //

            probability = 1.0/(
                1.0 + Math.Pow(10, (
                    (BlackPlayerELO - WhitePlayerELO)/400.0
                    ))
                );

            // We now have the probability of white winning stored in that variable
            // We'll allow a 1% probability change from 50% for a draw condition, partly due to imprecission.
            if (probability >= 0.49 && probability <= 0.51)
            {
                return Outcome.DRAW;
            }
            else if (probability > 0.5)
            {
                return Outcome.WHITE_WIN;
            }
            else
            {
                // Swap probability to 1 - probability to notate the probability of black winning
                probability = 1 - probability;
                return Outcome.BLACK_WIN;
            }
        }

        /// <summary>
        /// Determines the session the game took place in using a quick lambda search on the database
        /// </summary>
        /// <returns>The session the game was played during</returns>
        public Session Session()
        {
            return App.db.Sessions.Where(x => x.ID == SessionID).FirstOrDefault();
        }

        /// <summary>
        /// Returns the ELO change for ELO_A after a match
        /// </summary>
        /// <param name="K">K Factor for player A</param>
        /// <param name="ELO_A">ELO of player A at point of playing</param>
        /// <param name="ELO_B">ELO of player B at point of playing</param>
        /// <param name="A_WIN_STATE">1 if A won, 0 if draw, -1 if A lost</param>
        /// <param name="PROB_A">Probability of A winning</param>
        /// <returns>The Elo change for player A</returns>
        private int CalculateELOChange(int K, int ELO_A, int ELO_B, int A_WIN_STATE, double PROB_A)
        {
            if (A_WIN_STATE != 1 && A_WIN_STATE != 0 && A_WIN_STATE != -1)
                throw new ArgumentOutOfRangeException("A_WIN_STATE must be -1, 0 or 1");

            //                      Won?            Mod=1  Draw?          Mod=0.5   Lost? Mod = 0
            double A_WIN_MODIFIER = A_WIN_STATE == 1 ? 1 : A_WIN_STATE == 0 ? 0.5 : 0;

            return (int) Math.Round(K*(A_WIN_MODIFIER - PROB_A));
        }

        /// <summary>
        /// Calculates the change in Elo of the black player
        /// </summary>
        /// <returns>How much the black player's elo should change</returns>
        public int CalculateBlackELOChange()
        {
            // Get probability of black winning
            double probability;
            Outcome expectedOutcome = CalculateExpectedOutcome(out probability);
            if (expectedOutcome == Outcome.WHITE_WIN)
                // The method will have returned probability of black winning; switch it to white using 1 - (prob black)
                probability = 1 - probability;

            // 1 if they won, 0 if they drew, -1 if they lost
            int A_WIN_STATE = 0;
            switch (Winner)
            {
                case "White":
                    A_WIN_STATE = -1;
                    break;
                case "Black":
                    A_WIN_STATE = 1;
                    break;
            }

            return CalculateELOChange(BlackPlayerKFactor, BlackPlayerELO, WhitePlayerELO, A_WIN_STATE, probability);
        }

        /// <summary>
        /// Calculates the change in Elo of the white player
        /// </summary>
        /// <returns>How much the white player's elo should change</returns>
        public int CalculateWhiteELOChange()
        {
            // Get probability of white winning
            double probability;
            Outcome expectedOutcome = CalculateExpectedOutcome(out probability);
            if (expectedOutcome == Outcome.BLACK_WIN)
                // The method will have returned probability of black winning; switch it to white using 1 - (prob black)
                probability = 1 - probability;

            // 1 if they won, 0 if they drew, -1 if they lost
            int A_WIN_STATE = 0;
            switch (Winner)
            {
                case "Black":
                    A_WIN_STATE = -1;
                    break;
                case "White":
                    A_WIN_STATE = 1;
                    break;
            }

            return CalculateELOChange(WhitePlayerKFactor, WhitePlayerELO, BlackPlayerELO, A_WIN_STATE, probability);
        }

        /// <summary>
        /// Programmatically apply the elo changes to both players
        /// </summary>
        public void ApplyELOChange()
        {
            BlackPlayer.ELO += CalculateBlackELOChange();
            WhitePlayer.ELO += CalculateWhiteELOChange();
        }

        /// <summary>
        /// For page views to get a nicely formatted string out; using a simple case statement to form
        /// </summary>
        public string FriendlyWinType
        {
            get
            {
                switch (Outcome)
                {
                    case Enums.Outcome.BLACK_RESIGN:
                        return "Black resigned.";
                    case Enums.Outcome.BLACK_WIN:
                        return "Black checkmated white.";
                    case Enums.Outcome.WHITE_RESIGN:
                        return "White resigned.";
                    case Enums.Outcome.WHITE_WIN:
                        return "White checkmated black.";

                    case Enums.Outcome.DRAW:
                        string rString = "Draw";
                        // Elaborate on draw
                        switch (DrawType)
                        {
                            case Enums.DrawTypes.AGREEMENT:
                                return rString + " (agreement)";
                            case Enums.DrawTypes.FIFTY_MOVE_RULE:
                                return rString + " (50 move rule)";
                            case Enums.DrawTypes.INSUFFICIENT_CHECKMATING_MATERIAL:
                                return rString + " (not enough pieces)";
                            case Enums.DrawTypes.REPEATED_MOVES:
                                return rString + " (repeated move rule)";
                            case Enums.DrawTypes.STALEMATE:
                                return rString + " (stalemate)";
                        }
                        break;
                }
                // This is theoretically impossible to reach however the compiler requires it as the case statements have no default
                //  so as far as the compiler is concerned, it is possible to reach here and it wants a return statement.
                return "ERROR: UNKNOWN WIN TYPE";
            }
        }

        /// <summary>
        /// Returns the white Elo change as a string
        /// </summary>
        public string WhiteELOChange
        {
            get { return CalculateWhiteELOChange().ToString(); }
        }

        /// <summary>
        /// Returns the black Elo change as a string
        /// </summary>
        public string BlackELOChange
        {
            get { return CalculateBlackELOChange().ToString(); }
        }

        /// <summary>
        /// Returns the expected outcome as a string.
        /// </summary>
        public string ExpectedOutcome
        {
            get
            {
                double prob;
                return CalculateExpectedOutcome(out prob).ToString();
            }
        }

        /// <summary>
        /// Returns the expected outcome probability as a string.
        /// </summary>
        public string ExpectedOutcomeProbability
        {
            get
            {
                double prob;
                CalculateExpectedOutcome(out prob);
                return prob.ToString();
            }
        }

        /// <summary>
        /// For views to request a nice neat output for the expected outcome
        /// </summary>
        public string FriendlyExpectedOutcome
        {
            get { return ExpectedOutcome + "(" + Math.Round(double.Parse(ExpectedOutcomeProbability)*100) + "%)"; }
        }

        /// <summary>
        /// These methods are for the serializer to recognise not to serialize derived data
        /// </summary>
        public bool ShouldSerializeExpectedOutcome() { return false; }
        public bool ShouldSerializeExpectedOutcomeProbability() { return false; }
        public bool ShouldSerializeFriendlyExpectedOutcome() { return false; }
        public bool ShouldSerializeWhiteELOChange() { return false; }
        public bool ShouldSerializeBlackELOChange() { return false; }
        public bool ShouldSerializeSession() { return false; }
        public bool ShouldSerializeWinner() { return false; }
        public bool ShouldSerializeWhitePlayer() { return false; }
        public bool ShouldSerializeBlackPlayer() { return false; }
        public bool ShouldSerializeFriendlyWinType() { return false; }
    }
}