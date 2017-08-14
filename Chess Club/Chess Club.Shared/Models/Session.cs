using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Chess_Club.Models {
    public class Session : Model {
        /// <summary>
        /// Name of the session
        /// </summary>
        public string Name;
        /// <summary>
        /// Date the session took place
        /// </summary>
        public DateTime Date;
        /// <summary>
        /// Derived propery to return a nice title for the session based on whether the name is present or not
        /// </summary>
        public string Title
        {
            get
            {
                // If there's an name it's as simple as returning it
                if (Name != null && Name.Trim() != string.Empty)
                    return Name;

                // If not, it's a bit more complicated.

                // We use a nice formatting to get the date in <Mon/Tues/Weds/etc> <Year> format. We don't add the day yet since oddly .NET has no
                //  method of indicating a day suffix (i.e. nd/rd/th/st ala 1st, 3rd) so we'll need to do this manually.
                string dateFormatted = string.Format("{0:MMM yyyy}", Date);

                // 01st 02nd 03rd 04th 05th 06th 07th 08th 09th 10th
                // 11th 12th 13th 14th 15th 16th 17th 18th 19th 20th
                // 21st 22nd 23rd 24th 25th 26th 27th 28th 29th 30th
                // 31st

                // Self explanatory:
                if (Date.Day == 1 || Date.Day == 21 || Date.Day == 31)
                    dateFormatted = Date.Day + "st " + dateFormatted;
                else if (Date.Day == 2 || Date.Day == 22)
                    dateFormatted = Date.Day + "nd " + dateFormatted;
                else if (Date.Day == 3 || Date.Day == 23)
                    dateFormatted = Date.Day + "rd " + dateFormatted;
                else
                    dateFormatted = Date.Day + "th " + dateFormatted;

                // And return the date formatted as it is
                return dateFormatted;
            }
        }

        /// <summary>
        /// Return a list of members by querying database to find any members that went to session
        /// </summary>
        public List<Member> Members {
            get {
                List<Member> members = new List<Member>();
                // Loop each game
                foreach (int gameID in GameIDs) {
                    // Find the game in database
                    Game iGame = App.db.Games.Where(game => game.ID == gameID).First();

                    // Add the members from the game into the list of attendees unless they're already in it
                    if (!members.Contains(iGame.WhitePlayer)) {
                        members.Add(iGame.WhitePlayer);
                    }
                    if (!members.Contains(iGame.BlackPlayer)) {
                        members.Add(iGame.BlackPlayer);
                    }
                }
                // And return the list
                return members;
            }
        }
        /// <summary>
        /// Return a list of games by querying database to find each one
        /// </summary>
        public List<Game> Games {
            get {
                List<Game> games = new List<Game>();
                // Loop each game ID
                foreach (int gameID in GameIDs) {
                    // Find the game in the database
                    games.Add(App.db.Games.Where(game => game.ID == gameID).First());
                }
                // Add it to the list of games
                return games;
            }
        }
        /// <summary>
        /// Return a list of gameIDs by querying database to determine which games were played in this session
        /// </summary>
        public List<int> GameIDs {
            get {
                List<int> gameIDs = new List<int>();
                // Select all games where the session ID is equal to this ID and turn it into a list
                foreach (Game game in App.db.Games.Where(x => x.SessionID == ID).ToList()) {
                    // Now add it to the list
                    gameIDs.Add(game.ID);
                }
                // And return the list
                return gameIDs;
            }
        }

        /// <summary>
        /// These methods are for the serializer to recognise not to serialize derived data
        /// </summary>
        public Boolean ShouldSerializeMembers() { return false; }
        public Boolean ShouldSerializeGames() { return false; }
        public Boolean ShouldSerializeGameIDs() { return false; }
        public Boolean ShouldSerializeTitle() { return false; }
    }
}
