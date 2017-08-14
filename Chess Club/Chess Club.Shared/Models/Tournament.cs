using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chess_Club.Enums;

namespace Chess_Club.Models
{
    public class Tournament : Model
    {
        /// <summary>
        /// Name of the tournament
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Elo that the winner gets as a prize
        /// </summary>
        public int ELOPrize { get; set; }
        /// <summary>
        /// The ID of the winner if the competition has been won
        /// </summary>
        public int WinnerID = -1;
        /// <summary>
        /// A list of contestant IDs
        /// </summary>
        public List<int> ContestantIDs = new List<int>();
        /// <summary>
        /// Stores matchmaking style used by the rounds
        /// </summary>
        public MatchmakingStyle MatchmakingStyle;
        /// <summary>
        /// Stores a list of the remaining contestant's IDs
        /// </summary>
        public List<int> RemainingContestantIDs = new List<int>();

        /// <summary>
        /// Decides what picture to use to represent this tournament
        /// </summary>
        public string DisplayPicture {
            get {
                // If we have members left we use the TournamentInProgress image, if not the TournamentOver image
                if (RemainingContestantIDs.Count > 0) {
                    return "TournamentInProgress";
                } else {
                    return "TournamentOver";
                }
            }
        }

        /// <summary>
        /// Decide whether there is a round in progress
        /// </summary>
        public bool RoundInProgress {
            get {
                foreach (TournamentRound round in TournamentRounds) {
                    // If the round has planned matches remaining it must be in progress so we return true
                    if (round.PlannedMatches.Count > 0) {
                        return true;
                    }
                }
                // None were in progress
                return false;
            }
        }
        /// <summary>
        /// Gets the current round and returns it
        /// </summary>
        public TournamentRound CurrentRound {
            get {
                foreach (TournamentRound round in TournamentRounds) {
                    // If there is planned matches in the round we return it
                    if (round.PlannedMatches.Count > 0) {
                        return round;
                    }
                }
                // Looks like no rounds have any matches left
                return null;
            }
        }
        /// <summary>
        /// Gets the winner of the tournament
        /// </summary>
        public Member Winner {
            get {
                // If there is a winner
                if (WinnerID != -1) {
                    // Find them in the database by searching for members whose ID matches the winner's ID
                    return App.db.Members.Where(x => x.ID == WinnerID).First();
                } else {
                    // Otherwise just return null
                    return null;
                }
            }
        }

        /// <summary>
        /// Get or set a list of all the contestants without touching IDs outside of this code
        /// </summary>
        public List<Member> Contestants {
            get
            {
                // Turn the list of IDs into a list of members
                return ContestantIDs.Select(contestantID => App.db.Members.Where(x => x.ID == contestantID).First()).ToList();
            }
            set {
                // value = List<Member> we want a List of int
                List<int> memberIDs = new List<int>();
                foreach (Member member in (List<Member>)value)
                    // For each member, add the ID to the list of IDs
                    memberIDs.Add(member.ID);
                // Set contestant IDs equal to the accumulated member IDs
                ContestantIDs = memberIDs;
            }
        }
        /// <summary>
        /// See above, but for remaining contestants
        /// </summary>
        public List<Member> RemainingContestants {
            get {
                // Turn the list of IDs into a list of members
                List<Member> remainingContestants = new List<Member>();
                foreach (int remainingContestantID in RemainingContestantIDs)
                    remainingContestants.Add(App.db.Members.Where(x => x.ID == remainingContestantID).First());
                // And then return the list of members
                return remainingContestants;
            }
            set {
                // value = List<Member> we want a List of int
                List<int> memberIDs = new List<int>();
                foreach (Member member in (List<Member>)value)
                    memberIDs.Add(member.ID);
                // And set the remaining contestant IDs equal to the gathered member IDs
                RemainingContestantIDs = memberIDs;
            }
        }
        /// <summary>
        /// Get a list of games for the tournament
        /// </summary>
        public List<Game> Games {
            get {
                List<Game> games = new List<Game>();
                // Loop all of the rounds
                foreach (TournamentRound tournamentRound in TournamentRounds) {
                    // Add all of the games from this tournament round
                    games.AddRange(tournamentRound.Games);
                }
                // Return the games
                return games;
            }
        }

        /// <summary>
        /// Get a list of tournament rounds
        /// </summary>
        public List<TournamentRound> TournamentRounds {
            get {
                // Grab every tournament round from the database who's tournament ID matches this ID
                return App.db.TournamentRounds.Where(x => x.TournamentID == ID).ToList();
            }
        }

        /// <summary>
        /// These methods are for the serializer to recognise not to serialize derived data
        /// </summary>
        public bool ShouldSerializeDisplayPicture() { return false; }
        public bool ShouldSerializeRoundInProgress() { return false; }
        public bool ShouldSerializeWinner() { return false; }
        public bool ShouldSerializeContestants() { return false; }
        public bool ShouldSerializeRemainingContestants() { return false; }
        public bool ShouldSerializeGames() { return false; }
        public bool ShouldSerializeTournamentRounds() { return false; }
    }
}
