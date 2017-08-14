using Chess_Club.Common;
using Chess_Club.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace Chess_Club
{
    public sealed partial class AddGame : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();
        /// <summary>
        /// Stores the selected session
        /// </summary>
        private Session gameSession { get; set; }
        /// <summary>
        /// Stores the selected white player
        /// </summary>
        private Member whitePlayer { get; set; }
        /// <summary>
        /// Stores the selected black player
        /// </summary>
        private Member blackPlayer { get; set; }
        /// <summary>
        /// Stores whether the verification for game adding has been completed
        /// </summary>
        private bool verified { get; set; }

        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        public AddGame()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.navigationHelper.SaveState += navigationHelper_SaveState;

            // Set date suggestions for today
            grdSessionSuggestions.ItemsSource = App.db.Sessions.Where(x => x.Date == DateTime.Now.Date);
            // And populate both player suggestions
            grdWhiteSuggestions.ItemsSource = App.db.Members;
            grdBlackSuggestions.ItemsSource = App.db.Members;
        }

        /// <summary>
        /// Save data and return to previous page
        /// </summary>
        private async void Submit_Click(object sender, RoutedEventArgs e)
        {
            // Validation
            // Presence check on session
            if (gameSession == null) {
                lblSessionError.Visibility = Visibility.Visible;
                return;
            }
            // Presence check on outcome
            if (cmbOutcome.SelectedIndex == -1) {
                lblOutcomeError.Visibility = Visibility.Visible;
                return;
            }
            // Presence check on white player
            if (whitePlayer == null) {
                lblWhiteError.Visibility = Visibility.Visible;
                return;
            }
            // Presence check on black player
            if (blackPlayer == null) {
                lblBlackError.Visibility = Visibility.Visible;
                return;
            }

            // Verification
            if (!verified) {
                verified = true;
                lblVerify.Visibility = Visibility.Visible;
                btnSubmit.Content = "I'm sure.";
                return;
            }
            
            // Create a new game
            Game game = new Game();
            // Initialize ID settings among others
            await game.Initialize();
            // Load form data into the game
            game.WhitePlayer = whitePlayer;
            game.BlackPlayer = blackPlayer;
            game.SessionID = gameSession.ID;
            // Initialise the one-set variables

            // Parse the cmbOutcome's selected item and correctly fill in the outcome/drawtype fields in the game
            switch ((cmbOutcome.SelectedItem as ComboBoxItem).Name) {
                case "checkW":
                    game.Outcome = Enums.Outcome.WHITE_WIN;
                    break;
                case "checkB":
                    game.Outcome = Enums.Outcome.BLACK_WIN;
                    break;
                case "resignW":
                    game.Outcome = Enums.Outcome.BLACK_RESIGN;
                    break;
                case "resignB":
                    game.Outcome = Enums.Outcome.WHITE_RESIGN;
                    break;
                case "drawStale":
                    game.Outcome = Enums.Outcome.DRAW;
                    game.DrawType = Enums.DrawTypes.STALEMATE;
                    break;  
                case "drawFifty":
                    game.Outcome = Enums.Outcome.DRAW;
                    game.DrawType = Enums.DrawTypes.FIFTY_MOVE_RULE;
                    break;
                case "drawAgree":
                    game.Outcome = Enums.Outcome.DRAW;
                    game.DrawType = Enums.DrawTypes.AGREEMENT;
                    break;
                case "drawRepeated":
                    game.Outcome = Enums.Outcome.DRAW;
                    game.DrawType = Enums.DrawTypes.REPEATED_MOVES;
                    break;
                case "drawInsufficient":
                    game.Outcome = Enums.Outcome.DRAW;
                    game.DrawType = Enums.DrawTypes.INSUFFICIENT_CHECKMATING_MATERIAL;
                    break;
            }
            // Set the other game variables from the player information (This is not violating normalization principles as this data is kept for historical record and not modified as the regular member's data is!)
            game.BlackPlayerELO = blackPlayer.ELO;
            game.WhitePlayerELO = whitePlayer.ELO;
            game.BlackPlayerKFactor = blackPlayer.K_Factor;
            game.WhitePlayerKFactor = whitePlayer.K_Factor;

            // Apply the elo change from the game
            game.ApplyELOChange();

            // And add the game to the database
            App.db.Games.Add(game);

            // We don't await this; multithreading makes it feel seamless and lagless.
            App.db.SaveData();

            // Go back if we can to the previous page
            if (navigationHelper.CanGoBack())
                navigationHelper.GoBack();
        }

        /// <summary>
        /// Called by the navigation helper when the page is loaded. Unlike the two other add pages, this page does not dual-feature as an editing page
        /// </summary>
        /// <param name="e">n/a</param>
        private void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            this.defaultViewModel["Title"] = "Add Game";
        }

        /// <summary>
        /// Called by the navigation helper when the page needs to be serialized
        /// </summary>
        /// <param name="e">Allows filling of an empty dictionary (PageState) for serialization</param>
        private void navigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            // Not implemented
        }

        /// <summary>
        /// Returns 5 if the desired string has a space and is a substring of the full name (i.e. it spans both names),
        /// 4 if the desired string is the first substring in the forename,
        /// 3 if the desired string is the first substring in the surname,
        /// 2 if the desired string is in the forename but not the first substring,
        /// 1 if the desired string is in the surname but not the first substring.
        /// </summary>
        /// <param name="forename">Forename of player</param>
        /// <param name="surname">Surname of player</param>
        /// <param name="desiredString">The search string</param>
        /// <returns>See summary</returns>
        private int nameRelevance(string forename, string surname, string desiredString)
        {
            // Lowercase everything to make it caps insensetive
            forename = forename.ToLower();
            surname = surname.ToLower();
            desiredString = desiredString.ToLower();

            if (forename.IndexOf(desiredString) == 0) // Start of forename
                return 4;
            else if (surname.IndexOf(desiredString) == 0) // Start of surname
                return 3;
            else if (forename.IndexOf(desiredString) != -1) // In forename but not start
                return 2;
            else if (surname.IndexOf(desiredString) != -1) // In surname but not start
                return 1;
            else
                return 5;
        }
        
        /// <summary>
        /// Essentially stops duplicate code from txtWhite's text changed and txtBlack's text changed events. Updates a grid view based on a search string
        /// </summary>
        private void updateMemberSearchGrid(string searchString, GridView grid)
        {
            // Update suggestions members whose name have a matching substring
            List<Member> members = new List<Member>();
            // Loop members
            foreach (Member member in App.db.Members)
            {
                // We check both lowercases against eachother to avoid caps issues
                if (member.Name.ToLower().Contains(searchString.ToLower()))
                {
                    // One final check, we want to skip any that have already been selected for the other box.
                    if ((grid == grdBlackSuggestions && whitePlayer == member) || (grid == grdWhiteSuggestions && blackPlayer == member))
                        continue;

                    members.Add(member);
                }
            }
            // Sort by relevance
            // We decide relevance by first name first character precedence
            while (true)
            {
                bool swap = false;
                // Loop members
                for (int x = 0; x < members.Count - 1; x++)
                {
                    // If the next entry has a more relevant name
                    if (nameRelevance(members[x + 1].Forename, members[x + 1].Surname, searchString) >
                        nameRelevance(members[x].Forename, members[x].Surname, searchString))
                    {
                        // Swap them
                        Member swapVar = members[x];
                        members[x] = members[x + 1];
                        members[x + 1] = swapVar;
                        swap = true;
                    }
                }

                if (swap == false)
                {
                    // We've finished sorting
                    break;
                }
            }

            if (members.Count > 0)
                 grid.ItemsSource = members;
            else
                grid.ItemsSource = null;
        }
        /// <summary>
        /// Update search grid when text changes
        /// </summary>
        private void txtWhite_TextChanged(object sender, TextChangedEventArgs e)
        {
            updateMemberSearchGrid(txtWhite.Text, grdWhiteSuggestions);
        }
        /// <summary>
        /// Update search grid when text changes
        /// </summary>
        private void txtBlack_TextChanged(object sender, TextChangedEventArgs e)
        {
            updateMemberSearchGrid(txtBlack.Text, grdBlackSuggestions);
        }
        /// <summary>
        /// Update search grid when date changes
        /// </summary>
        private void dtpSession_DateChanged(object sender, DatePickerValueChangedEventArgs e) {
            grdSessionSuggestions.ItemsSource = App.db.Sessions.Where(x => x.Date == dtpSession.Date.Date);
        }
        /// <summary>
        /// When a black player is selected we cache the player and update the search grid if double selected to stop them
        /// </summary>
        private void grdBlackSuggestions_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            blackPlayer = grdBlackSuggestions.SelectedItem as Member;
            // Cannot have the same player selected for both roles
            if (blackPlayer == whitePlayer) {
                blackPlayer = null;
                updateMemberSearchGrid(txtBlack.Text, grdBlackSuggestions);
            }

            lblBlackError.Visibility = Visibility.Collapsed;
        }
        /// <summary>
        /// When a black player is selected we cache the player and update the search grid if double selected to stop them
        /// </summary>
        private void grdWhiteSuggestions_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            whitePlayer = grdWhiteSuggestions.SelectedItem as Member;
            // Cannot have the same player selected for both roles
            if (whitePlayer == blackPlayer) {
                whitePlayer = null;
                updateMemberSearchGrid(txtWhite.Text, grdWhiteSuggestions);
            }

            lblWhiteError.Visibility = Visibility.Collapsed;
        }
        /// <summary>
        /// When they select a session hide the validation and set the cached session to the selected session
        /// </summary>
        private void grdSessionSuggestions_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            gameSession = grdSessionSuggestions.SelectedItem as Session;
            lblSessionError.Visibility = Visibility.Collapsed;
        }

        #region NavigationHelper registration

        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// 
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="GridCS.Common.NavigationHelper.LoadState"/>
        /// and <see cref="GridCS.Common.NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        /// <summary>
        /// When an outcome is selected we hide the validation message
        /// </summary>
        private void cmbOutcome_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            lblOutcomeError.Visibility = Visibility.Collapsed;
        }
    }
}
