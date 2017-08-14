using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Chess_Club.Enums;
using System.Diagnostics;
using System.Xml.Serialization;

namespace Chess_Club.Models
{
    // XML Serializer doesn't like the built in KVP so we make our own
    [XmlType (TypeName="KeyValPair")]
    // We take in two type parameters, K and V
    public struct CKeyValuePair<K, V> {
        // Both K and V have their own types and we have a key for k and a value for v
        public CKeyValuePair(K k, V v) : this() {
            // Set the variables
            this.Key = k;
            this.Val = v;
        }
        /// <summary>
        /// Stores the key for this key/value pair
        /// </summary>
        public K Key { get; set; }
        /// <summary>
        /// Stores the value for this key/value pair
        /// </summary>
        public V Val { get; set; }
    }


    public class TournamentRound : Model
    {
        /// <summary>
        /// ID of the tournament that this round is in
        /// </summary>
        public int TournamentID;
        /// <summary>
        /// Round number of this tournament round
        /// </summary>
        public int RoundNumber { get; set; }
        /// <summary>
        /// List of contestants
        /// </summary>
        public List<int> ContestantIDs = new List<int>();
        /// <summary>
        /// List of planned matches (Key = White Player, Value = Black Player)
        /// </summary>
        public List<CKeyValuePair<int, int>> PlannedMatches { get; set; }
        /// <summary>
        /// List of game IDs played so far
        /// </summary>
        public List<int> GameIDs = new List<int>();

        /// <summary>
        /// Plan matches for this round
        /// </summary>
        public void planMatches() {
            // We should  have an even number of contestants and can plan games
            // We must take different actions in sorting based on what matchmaking style we want: close ELO, furthest apart ELO or randomized
            PlannedMatches = new List<CKeyValuePair<int, int>>();
            // Create a scope to delete sorted and sortedStack after use
            {
                // Store the sorted members and get them as a stack for the convenient Pop method
                List<Member> sorted = GetContestantsBySortedELO();
                Stack<Member> sortedStack = new Stack<Member>(sorted);

                // What we do next varies based on the matchmaking style
                switch (Tournament.MatchmakingStyle) {
                    case MatchmakingStyle.CLOSE:
                        // Easy to do; split the list into pairs.
                        while (sortedStack.Count > 0) {
                            // Take the top two off the stack
                            PlannedMatches.Add(new CKeyValuePair<int, int>(sortedStack.Pop().ID, sortedStack.Pop().ID));
                        }
                        break;
                    case MatchmakingStyle.OPPOSITE:
                        // A stack can't be used here as you can only take from the top so we use a slight workaround with the list
                        while (sorted.Count > 0) {
                            // Take the first and last from the list
                            PlannedMatches.Add(new CKeyValuePair<int, int>(sorted.First().ID, sorted.Last().ID));
                            // Remove the two members we sorted
                            sorted.Remove(sorted.First());
                            sorted.Remove(sorted.Last());
                        }
                        break;
                    case MatchmakingStyle.RANDOM:
                        // Create a new random object (We don't store one or it'll use the same seed, creating a new one reseeds
                        Random memberRandom = new Random();

                        // We repeatedly choose two random members
                        while (sorted.Count > 0) {
                            // Store two random members
                            Member rM1, rM2;
                            // Select a random member
                            rM1 = sorted[memberRandom.Next(0, sorted.Count)];
                            // Remove it immediately so rM2 doesn't select the same one
                            sorted.Remove(rM1);
                            // Select another random member
                            rM2 = sorted[memberRandom.Next(0, sorted.Count)];
                            // ... And remove it
                            sorted.Remove(rM2);
                            // Now add them both as a match
                            PlannedMatches.Add(new CKeyValuePair<int, int>(rM1.ID, rM2.ID));
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Sort the contestants by their Elo using the bubble sort algorithm
        /// </summary>
        /// <returns>A list of members sorted by their elo</returns>
        private List<Member> GetContestantsBySortedELO() {
            List<Member> sortedList = new List<Member>();
            // We will use the insertion sort algorithm here to get a quick result.
            // We are sorting in a descending manner so the first entry should have the highest ELO and the last the lowest.
            foreach (Member member in Contestants) {
                int index = -1;
                foreach (Member sortedMember in sortedList) {
                    if (member.ELO > sortedMember.ELO) {
                        // Insert the new member at the current member's position, thereby pushing it and everything below it down to make space
                        index = sortedList.IndexOf(sortedMember);
                        break;
                    }
                }
                // It may be that our ELO is the lowest or the sorted list is empty, in which case we just drop our member at the end.
                if (index == -1) {
                    sortedList.Add(member);
                } else {
                    sortedList.Insert(index, member);
                }
            }
            return sortedList;
        }

        /// <summary>
        /// Returns the tournament from the tournament ID
        /// </summary>
        public Tournament Tournament {
            get {
                // Use a quick lambda expression to find the tournament
                return App.db.Tournaments.Where(x => x.ID == TournamentID).First();
            }
        }
        /// <summary>
        /// Get a list of contestants from the contestant IDs
        /// </summary>
        public List<Member> Contestants {
            get {
                List<Member> contestants = new List<Member>();
                // Loop the contestant IDs
                foreach (int contestantID in ContestantIDs) {
                    // Add the contestant with that ID into the database
                    contestants.Add(App.db.Members.Where(x => x.ID == contestantID).First());
                }
                // And return them
                return contestants;
            }
        }
        /// <summary>
        /// Get a list of games from the game IDs
        /// </summary>
        public List<Game> Games {
            get {
                List<Game> games = new List<Game>();
                // Loop the game IDs
                foreach (int gameID in GameIDs) {
                    // Add the game with this ID to the list
                    games.Add(App.db.Games.Where(x => x.ID == gameID).First());
                }
                // And return them
                return games;
            }
        }
        /// <summary>
        /// Returns which picture represents this round
        /// </summary>
        public string DisplayPicture {
            get {
                if (PlannedMatches.Count > 0) {
                    // If there are planned matches, we send the tournament in progress image
                    return "TournamentInProgress";
                } else {
                    // Else, the tournament over image
                    return "TournamentOver";
                }
            }
        }

        /// <summary>
        /// These methods are for the serializer to recognise not to serialize derived data
        /// </summary>
        public bool ShouldSerializeGames() { return false; }
        public bool ShouldSerializeContestants() { return false; }
        public bool ShouldSerializeTournament() { return false; }
        public bool ShouldSerializeDisplayPicture() { return false; }
    }
}
