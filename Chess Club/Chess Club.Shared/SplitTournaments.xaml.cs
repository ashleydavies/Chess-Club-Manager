using Chess_Club.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
using Chess_Club.Models;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using Windows.UI;
using System.Diagnostics;

namespace Chess_Club {
    public sealed partial class SplitTournaments : Page {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        public ObservableDictionary DefaultViewModel {
            get { return this.defaultViewModel; }
        }

        public NavigationHelper NavigationHelper {
            get { return this.navigationHelper; }
        }

        public SplitTournaments() {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.navigationHelper.SaveState += navigationHelper_SaveState;

            // Setup the logical page navigation components that allow
            // the page to only show one pane at a time.
            this.navigationHelper.GoBackCommand = new Chess_Club.Common.RelayCommand(() => this.GoBack(), () => this.CanGoBack());
            this.itemListView.SelectionChanged += itemListView_SelectionChanged;
            
            // Start listening for Window size changes 
            // to change from showing two panes to showing a single pane
            Window.Current.SizeChanged += Window_SizeChanged;
            this.InvalidateVisualState();
        }  

        /// <summary>
        /// While changing page is automated on the design side, we need to notify the navigation helper that the back button may need to be updated if it's
        ///  only displaying one pane (So, if it has to hide the list, we're telling it that the back button goes back to the list and not the previous page)
        /// </summary>
        void itemListView_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (this.UsingLogicalPageNavigation()) {
                this.navigationHelper.GoBackCommand.RaiseCanExecuteChanged();
            }


            // Manually update rounds grid
            if (itemListView.SelectedIndex != -1) {
                Tournament tournament = itemListView.SelectedItem as Tournament;
                grdRounds.ItemsSource = tournament.TournamentRounds;

                // If there's a winner we hide both buttons (tournament is over so you can't go to current round or add new round)
                if (tournament.WinnerID != -1) {
                    btnCurrentRound.Visibility = Visibility.Collapsed;
                    btnNextRound.Visibility = Visibility.Collapsed;
                }
                // If there's a round in progress only hide the next round button
                else if (tournament.RoundInProgress) {
                    btnCurrentRound.Visibility = Visibility.Visible;
                    btnNextRound.Visibility = Visibility.Collapsed;
                // Otherwise there's not a round in progress but not completed so show the next round button
                } else {
                    btnCurrentRound.Visibility = Visibility.Collapsed;
                    btnNextRound.Visibility = Visibility.Visible;
                }
#if (WINDOWS_PHONE_APP != true)
                // For unknown reasons this does not run on the phone. Due to a lack of a debugger, this will not be fixed until the next Chess Club Manager version.
                generateTournamentBrackets();
#endif
            } else {
                grdRounds.ItemsSource = null;
            }
        }

        /// <summary>
        /// Called by the navigation helper when the page is loaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">If the page has already been opened (and has been navigated into using back), contains the selected item under PageState</param>
        private void navigationHelper_LoadState(object sender, LoadStateEventArgs e) {
            // Set default title and items of the grid
            this.itemListView.SelectedItem = null;

            this.DefaultViewModel["Title"] = "Tournaments";
            this.defaultViewModel.Add("Items", App.db.Tournaments);

            if (e.NavigationParameter != null) {
                // If we've been given an item to display, select that. This is most likely because they clicked it from the main page grid profile.
                this.itemsViewSource.View.MoveCurrentTo(e.NavigationParameter);
            } else if (e.PageState == null) {
                // If we've got screen wide enough to display details & list, then select the first
                // Naturally, if there's only enough room for one view, we want to show the list first.
                if (!this.UsingLogicalPageNavigation() && this.itemsViewSource.View != null) {
                    this.itemsViewSource.View.MoveCurrentToFirst();
                }
            } else {
                // If we had a page loaded previously, throw it up
                if (e.PageState.ContainsKey("SelectedItem") && this.itemsViewSource.View != null) {
                    // Load the serialized selected item from memory and load it into the details pane
                    itemsViewSource.View.MoveCurrentTo(e.PageState["SelectedItem"]);
                }
            }
        }

        /// <summary>
        /// Called by the navigation helper when the page needs to be serialized
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">Allows filling of an empty dictionary (PageState) for serialization</param>
        private void navigationHelper_SaveState(object sender, SaveStateEventArgs e) {
            if (this.itemsViewSource.View != null) {
                e.PageState["SelectedItem"] = this.itemListView.SelectedItem;
            }
        }

        #region Logical page navigation

        // The split page isdesigned so that when the Window does have enough space to show
        // both the list and the dteails, only one pane will be shown at at time.
        //
        // This is all implemented with a single physical page that can represent two logical
        // pages.  The code below achieves this goal without making the user aware of the
        // distinction.

        private const int MinimumWidthForSupportingTwoPanes = 1000;

        /// <summary>
        /// Invoked to determine whether the page should act as one logical page or two.
        /// </summary>
        /// <returns>True if the window should show act as one logical page, false
        /// otherwise.</returns>
        private bool UsingLogicalPageNavigation() {
            return Window.Current.Bounds.Width < MinimumWidthForSupportingTwoPanes;
        }

        /// <summary>
        /// Invoked with the Window changes size
        /// </summary>
        /// <param name="sender">The current Window</param>
        /// <param name="e">Event data that describes the new size of the Window</param>
        private void Window_SizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e) {
            this.InvalidateVisualState();
        }

        /// <summary>
        /// Invoked when an item within the list is selected.
        /// </summary>
        /// <param name="sender">The GridView displaying the selected item.</param>
        /// <param name="e">Event data that describes how the selection was changed.</param>
        private void ItemListView_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            // Invalidate the view state when logical page navigation is in effect, as a change
            // in selection may cause a corresponding change in the current logical page.  When
            // an item is selected this has the effect of changing from displaying the item list
            // to showing the selected item's details.  When the selection is cleared this has the
            // opposite effect.
            if (this.UsingLogicalPageNavigation()) this.InvalidateVisualState();
        }

        private bool CanGoBack() {
            if (this.UsingLogicalPageNavigation() && this.itemListView.SelectedItem != null) {
                return true;
            } else {
                return this.navigationHelper.CanGoBack();
            }
        }
        private void GoBack() {
            if (this.UsingLogicalPageNavigation() && this.itemListView.SelectedItem != null) {
                // When logical page navigation is in effect and there's a selected item that
                // item's details are currently displayed.  Clearing the selection will return to
                // the item list.  From the user's point of view this is a logical backward
                // navigation.
                this.itemListView.SelectedItem = null;
            } else {
                this.navigationHelper.GoBack();
            }
        }

        private void InvalidateVisualState() {
            var visualState = DetermineVisualState();
            VisualStateManager.GoToState(this, visualState, false);
            this.navigationHelper.GoBackCommand.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Invoked to determine the name of the visual state that corresponds to an application
        /// view state.
        /// </summary>
        /// <returns>The name of the desired visual state.  This is the same as the name of the
        /// view state except when there is a selected item in portrait and snapped views where
        /// this additional logical page is represented by adding a suffix of _Detail.</returns>
        private string DetermineVisualState() {
            if (!UsingLogicalPageNavigation())
                return "PrimaryView";

            // Update the back button's enabled state when the view state changes
            var logicalPageBack = this.UsingLogicalPageNavigation() && this.itemListView.SelectedItem != null;

            return logicalPageBack ? "SinglePane_Detail" : "SinglePane";
        }

        #endregion

        #region NavigationHelper registration

        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// 
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="GridCS.Common.NavigationHelper.LoadState"/>
        /// and <see cref="GridCS.Common.NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e) {
            navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        /// <summary>
        /// When they click next round, set up the next tournament round
        /// </summary>
        private async void btnNextRound_Click(object sender, RoutedEventArgs e) {
            // Don't do anything if nothing is selected
            if (itemListView.SelectedIndex == -1)
                return;

            // Hide the button to stop double clicking
            btnNextRound.Visibility = Visibility.Collapsed;

            Tournament tournament = itemListView.SelectedItem as Tournament;
            TournamentRound newRound = new TournamentRound();
            // This button can only be seen if it's possible to create a new tournament round
            newRound.Initialize();
            // IMPORTANT:
            // If it's the first round, we must ensure that we have a specific number of competitors
            // The idea is that e.g. if there's 7 competitors, we can't have 3.5 games
            // so we must skip some to round two.
            List<int> ContestantIDs = tournament.RemainingContestantIDs.ToList();

            // If this is the first round and the number of players is not a power of two we need to sort it out so that next round it will be (see user documentation for more information on why this is the case)
            // To check if it's a power of two, an efficient method using bitwise operators is used.
            // To understand why this works, consider that a power of two represented in binary looks as follows:
            //  10000000 (i.e. 1 + n 0s)
            // A power of two with one taken away in binary looks as follows:
            //  01111111 (i.e. 0 + n 1s)
            // This means that using an AND bitwise operator on these will return 0 if and only if it is a power of two.
            if (tournament.TournamentRounds.Count == 0 && (ContestantIDs.Count & (ContestantIDs.Count - 1)) != 0) {
                int nearestPow2 = 1;
                while (nearestPow2 < tournament.ContestantIDs.Count) {
                    nearestPow2 *= 2;
                }

                if (nearestPow2 > tournament.ContestantIDs.Count) {
                    nearestPow2 /= 2;
                }

                // We now know the nearest power of two below the number of members
                // Now we need to double the difference and put that many players into round one
                int numberOfPlayers = tournament.ContestantIDs.Count - nearestPow2;
                numberOfPlayers *= 2;

                List<Member> firstRoundContestants = new List<Member>();
                for (int i = 0; i < numberOfPlayers; i++) {
                    // Get the lowest ELO player
                    Member lowestELO = null;
                    foreach (Member member in tournament.Contestants) {
                        // If we don't have a lowestELO yet or they have a lower ELO we set them to the variable
                        if ((lowestELO == null || member.ELO < lowestELO.ELO) && !firstRoundContestants.Contains(member)) {
                            lowestELO = member;
                        }
                    }
                    // Add them to the first contestants
                    firstRoundContestants.Add(lowestELO);
                }
                // Form a list of contestantIDs from the first round contestants
                ContestantIDs = new List<int>();
                foreach (Member member in firstRoundContestants) {
                    ContestantIDs.Add(member.ID);
                }
            }
            // Set up basic values from known data
            newRound.ContestantIDs = ContestantIDs;
            newRound.RoundNumber = tournament.TournamentRounds.Count + 1;
            newRound.TournamentID = tournament.ID;
            // Plan matches
            newRound.planMatches();
            // Add to database and save data
            App.db.TournamentRounds.Add(newRound);
            App.db.SaveData();
            // Refresh the rounds grid to reflect changes
            grdRounds.ItemsSource = null;
            grdRounds.ItemsSource = tournament.TournamentRounds;
            // Show the current round button as a round is now in progress
            btnCurrentRound.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Go to the current round when the current round button is clicked
        /// </summary>
        private void btnCurrentRound_Click(object sender, RoutedEventArgs e) {
            if (itemListView.SelectedIndex != -1)
                this.Frame.Navigate(typeof(SplitTournamentRounds), (itemListView.SelectedItem as Tournament).CurrentRound);
        }

        /// <summary>
        /// Go to the selected round when a round is selected
        /// </summary>
        private void grdRounds_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (grdRounds.SelectedIndex != -1)
                this.Frame.Navigate(typeof(SplitTournamentRounds), (grdRounds.SelectedItem as TournamentRound));
        }

        /// <summary>
        /// A very complicated method to generate tournament brackets. This is currently limited to PC and tablet builds of the Chess Club Manager due to unknown compatibility issues
        /// </summary>
        private void generateTournamentBrackets() {
            // Get the tournament and clear the grid ready for use
            Tournament trn = itemListView.SelectedItem as Tournament;
            Grid grd = grdTournamentBracket;
            grd.RowDefinitions.Clear();
            grd.ColumnDefinitions.Clear();
            grd.Children.Clear();

            // Create rows and columns
            // Formula for number of rows is number of members, but you subtract one if it's even (as a bye does not need it's own space)
            int rowCount;
            if (trn.ContestantIDs.Count % 2 == 0)
                rowCount = trn.ContestantIDs.Count - 1;
            else
                rowCount = trn.ContestantIDs.Count;
            
            // Formula for number of columns is one less than the double of the rounded up log2 of the number of contestants
            int colCount = (int)Math.Ceiling(Math.Log(trn.ContestantIDs.Count, 2)) * 2 - 1;

            // Now create the setup
            for (int i = 0; i < rowCount; i++) {
                // Create rowCount row definitions with height 70 and put them in the grid
                RowDefinition rd = new RowDefinition();

                rd.Height = new GridLength(70);
                grd.RowDefinitions.Add(rd);
            }
            for (int i = 0; i < colCount; i++) {
                // Create colCount col definitions with width 120 and put them in the grid
                ColumnDefinition cd = new ColumnDefinition();

                cd.Width = new GridLength(120);
                grd.ColumnDefinitions.Add(cd);
            }

            // Now we start filling in the grid
            // Initialize variables for use in the loop
            TournamentRound lastRound = null;
            TournamentRound tRound = null;
            List<Game> lastOrderedGames = null;
            List<Game> orderedGames = null;
            List<KeyValuePair<int, int>> lastYPositions = new List<KeyValuePair<int, int>>();
            List<KeyValuePair<int, int>> yPositions = new List<KeyValuePair<int, int>>();

            if (trn.TournamentRounds.Count < 2)
            {
                // We need at least two rounds to generate any useful graphics
                return;
            }

            for (int x = 0; x < colCount; x++) {
                // For the first round we need a more complicated sorting algorithm to sort out the next round
                if (x == 0) {
                    tRound = trn.TournamentRounds[0];
                    orderedGames = new List<Game>();
                    // Try to order games in pairs corresponding to who plays eachother next round.
                    Debug.WriteLine("SORTING GAMES X: " + x);
                    List<Game> copiedGames = tRound.Games.ToList();
                    // Add them to these lists depending on whether the game contributed to a game in the next round
                    //  where two people from this round participated (Alternatively it might only be this one and the
                    //  other person might have received a bye at the start)
                    List<Game> oneAdvance = new List<Game>();
                    List<Game> twoAdvance = new List<Game>();
                    foreach (Game game in copiedGames) {
                        // We check if they participate in the next round
                        Member successor;
                        if (game.Winner == "Black")
                            successor = game.BlackPlayer;
                        else
                            successor = game.WhitePlayer;

                        IEnumerable<Game> successorGames = trn.TournamentRounds[(x / 2) + 1].Games.Where(lx => (lx.WhitePlayerID == successor.ID || lx.BlackPlayerID == successor.ID));
                        if (!successorGames.Any()) {
                            // No successor games; skip ahead in loop
                            continue;
                        }
                        // Find the other player in the game
                        Game successorGame = successorGames.First();
                        Member otherPlayer;
                        if (successorGame.BlackPlayerID == successor.ID) {
                            otherPlayer = successorGame.WhitePlayer;
                        } else {
                            otherPlayer = successorGame.BlackPlayer;
                        }

                        // Check if the other player was in this round
                        if (tRound.ContestantIDs.Contains(otherPlayer.ID)) {
                            // This game has a legacy of two successors
                            twoAdvance.Add(game);
                        } else {
                            // This successor is the only successor of this round who played in their game next round
                            oneAdvance.Add(game);
                        }
                    }

                    // Now we order the two successor games so that their order pairs them up
                    List<Game> twoAdvanceSorted = new List<Game>();

                    foreach (Game game in twoAdvance) {
                        if (twoAdvanceSorted.Contains(game))
                            // If we've already sorted this game jump to the next one
                            continue;

                        // Find winner
                        Member winner;
                        if (game.Winner == "Black")
                            winner = game.BlackPlayer;
                        else
                            winner = game.WhitePlayer;
                        // Find the game they participated in in the next round
                        Game nextRoundGame = trn.TournamentRounds[x / 2 + 1].Games.Where(lx => (lx.BlackPlayerID == winner.ID || lx.WhitePlayerID == winner.ID)).First();

                        Member otherWinner;
                        // Find the other player in that game
                        if (nextRoundGame.BlackPlayerID == winner.ID) {
                            otherWinner = nextRoundGame.WhitePlayer;
                        } else {
                            otherWinner = nextRoundGame.BlackPlayer;
                        }
                        // Find the other game they played in
                        Game game2 = tRound.Games.Where(xl => (xl.WhitePlayerID == otherWinner.ID || xl.BlackPlayer.ID == otherWinner.ID)).First();
                        // Put them in order in the twoAdvanceSorted list to keep double advances together
                        twoAdvanceSorted.Add(game);
                        twoAdvanceSorted.Add(game2);
                    }

                    // Now we put twoAdvanceSorted and oneAdvance into the single orderedGames list
                    // To do this we need to pad the twoAdvanceSorted with oneAdvances so that
                    //  all the paired games end up together.
                    int numBefore = oneAdvance.Count / 2;
                    if (Math.Floor((double)numBefore) > 0) {
                        for (int i = 0; i < numBefore; i++) {
                            // Add them to the ordered list and remove them from the oneAdvance list
                            orderedGames.Add(oneAdvance.First());
                            oneAdvance.Remove(oneAdvance.First());
                        }
                    }
                    // Dump in all values from the twoAdvance list
                    // We use a foreach loop to preserve order
                    orderedGames.AddRange(twoAdvanceSorted);
                    // And finally throw in any remaining oneAdvance games
                    orderedGames.AddRange(oneAdvance);
                } else if (x % 2 == 0) {
                    // We must look at the order of the last column's games and determine from that what order we put this column's games in
                    //  using lastOrderedGames
                    tRound = trn.TournamentRounds[x / 2];
                    orderedGames = tRound.Games.ToList();
                } else {
                    tRound = null;
                }
                for (int y = 0; y < rowCount; y++) {
                    // x, y represent a grid position
                    if (x % 2 == 0) {
                        // If we're on an even row, we put in games (Odd rows are for connecting lines)
                        int round = x / 2;
                        // Check how many rows the games require
                        int numRows = 2 * tRound.GameIDs.Count - 1;
                        // Now interpolate how far spacing we should give
                        int spacing = (rowCount - numRows) / 2;
                        
                        // Now check if we're not in the spacing area
                        if (y >= Math.Floor((double)spacing) && y < Math.Ceiling((double)spacing) + numRows) {
                            // Now check how far into the non-spacing area we are
                            int depth = y - (int)Math.Floor((double)spacing);
                            // Every two cells we want a game
                            if (depth % 2 == 0) {
                                // We can actually get the game details using depth / 2 and checking the game index
                                int gameNumber = depth / 2;

                                Game game = orderedGames[gameNumber];
                                // Set up a textblock to represent this game
                                TextBlock b = new TextBlock();
                                b.SetValue(Grid.ColumnProperty, x);
                                b.SetValue(Grid.RowProperty, y);
                                b.TextWrapping = TextWrapping.Wrap;
                                b.HorizontalAlignment = HorizontalAlignment.Center;
                                b.VerticalAlignment = VerticalAlignment.Center;
                                b.TextAlignment = TextAlignment.Center;
                                b.Foreground = new SolidColorBrush(Colors.Black);
                                
                                int winnerID;
                                if (game.Winner == "Black")
                                    winnerID = game.BlackPlayerID;
                                else
                                    winnerID = game.WhitePlayerID;
                                // Add the game to the yPositions to keep track of it for future round ordering
                                yPositions.Add(new KeyValuePair<int, int>(winnerID, y));
                                // Depending on if white or black won we swap the formatting around
                                if (game.Winner == "Black") {
                                    b.Text = game.BlackPlayer.Name + " won against " + game.WhitePlayer.Name + ".";
                                } else {
                                    b.Text = game.WhitePlayer.Name + " won against " + game.BlackPlayer.Name + ".";
                                }
                                // Form a new rectangle as a sort of background for the game; initialize with basic properties
                                Rectangle rect = new Rectangle();
                                rect.Fill = new SolidColorBrush(Colors.CornflowerBlue);
                                rect.HorizontalAlignment = HorizontalAlignment.Center;
                                rect.VerticalAlignment = VerticalAlignment.Center;
                                rect.Width = 120;
                                rect.Height = 80;
                                rect.SetValue(Grid.ColumnProperty, x);
                                rect.SetValue(Grid.RowProperty, y);
                                grd.Children.Add(rect);
                                grd.Children.Add(b);

                                // If they played in the last round
                                if (lastRound != null && lastYPositions.Where(xl => xl.Key == game.BlackPlayerID).Count() > 0) {
                                    // Find the last game this player was in
                                    int yO = lastYPositions.First(xl => xl.Key == game.BlackPlayerID).Value;

                                    // Draw a line between the two games
                                    Line line = new Line();
                                    line.X1 = 0;
                                    line.X2 = 120;
                                    line.SetValue(Grid.RowSpanProperty, Math.Abs(y - yO) + 1);
                                    line.SetValue(Grid.ColumnProperty, x - 1);
                                    line.SetValue(Grid.RowProperty, y);
                                    line.Stroke = new SolidColorBrush(Colors.Yellow);

                                    // Slightly complicated math to determine the Y positions of the line.
                                    // Essentially rowspan must be considered as increasing the height by 70 each row
                                    //  thus 35 is halfway
                                    // and as a result of this the difference between yO and y add 1 is
                                    //  how many rows difference, and multiply that by 70 to get the pixels difference
                                    //  then take 35 to make it aligned centrally.
                                    // If yO < y, this must be done the other way around since Y1 is going to be above Y2
                                    if (yO > y) {
                                        line.Y1 = 70 * (Math.Abs(yO - y) + 1) - 35;
                                        line.Y2 = 35;
                                    } else {
                                        line.SetValue(Grid.RowProperty, yO);
                                        line.Y1 = 35;
                                        line.Y2 = 70 * (Math.Abs(yO - y) + 1) - 35;
                                    }
                                    
                                    grd.Children.Add(line);
                                }
                                if (lastRound != null && lastYPositions.Where(xl => xl.Key == game.WhitePlayerID).Count() > 0) {
                                    // Find the last game this player was in
                                    int yO = lastYPositions.Where(xl => xl.Key == game.WhitePlayerID).First().Value;

                                    // Draw a line
                                    Line line = new Line();
                                    line.X1 = 0;
                                    line.X2 = 120;
                                    line.SetValue(Grid.RowSpanProperty, Math.Abs(y - yO) + 1);
                                    line.SetValue(Grid.ColumnProperty, x - 1);
                                    line.SetValue(Grid.RowProperty, y);
                                    line.Stroke = new SolidColorBrush(Colors.Yellow);

                                    // Slightly complicated math to determine the Y positions of the line.
                                    // Essentially rowspan must be considered as increasing the height by 70 each row
                                    //  thus 35 is halfway
                                    // and as a result of this the difference between yO and y add 1 is
                                    //  how many rows difference, and multiply that by 70 to get the pixels difference
                                    //  then take 35 to make it aligned centrally.
                                    // If yO < y, this must be done the other way around since Y1 is going to be above Y2
                                    if (yO > y) {
                                        line.Y1 = 70 * (Math.Abs(yO - y) + 1) - 35;
                                        line.Y2 = 35;
                                    } else {
                                        line.SetValue(Grid.RowProperty, yO);
                                        line.Y1 = 35;
                                        line.Y2 = 70 * (Math.Abs(yO - y) + 1) - 35;
                                    }

                                    grd.Children.Add(line);
                                }
                            }
                        }
                    }
                }
                // If we're in a game row we update the old yPositions with the new one and clear the new one
                if (x % 2 == 0) {
                    lastYPositions = yPositions;
                    yPositions = new List<KeyValuePair<int, int>>();
                }

                // Now set the lastRound variable ready for the next round
                if (tRound != null) {
                    lastRound = tRound;
                    lastOrderedGames = orderedGames;
                }
            }
        }
    }
}
