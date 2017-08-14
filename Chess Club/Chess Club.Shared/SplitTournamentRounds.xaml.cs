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

namespace Chess_Club {
    public sealed partial class SplitTournamentRounds : Page {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        public ObservableDictionary DefaultViewModel {
            get { return this.defaultViewModel; }
        }

        public NavigationHelper NavigationHelper {
            get { return this.navigationHelper; }
        }

        public SplitTournamentRounds() {
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
        }

        /// <summary>
        /// Called by the navigation helper when the page is loaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">If the page has already been opened (and has been navigated into using back), contains the selected item under PageState</param>
        private void navigationHelper_LoadState(object sender, LoadStateEventArgs e) {

            this.itemListView.SelectedItem = null;

            if (e.NavigationParameter != null) {
                // There should always be a navigation parameter.
                // It might be a Tournament or a TournamentRound so we need to check
                Tournament tournament;
                if (e.NavigationParameter.GetType() == typeof(Tournament)) {
                    tournament = e.NavigationParameter as Tournament;
                } else {
                    tournament = (e.NavigationParameter as TournamentRound).Tournament;
                }

                this.DefaultViewModel["Items"] = tournament.TournamentRounds;
                this.DefaultViewModel["Title"] = "Tournaments Rounds (" + tournament.Name + ")";

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

        private void btnAddGame_Click(object sender, RoutedEventArgs e) {
            if (itemListView.SelectedIndex == -1) 
                return;
            TournamentRound selectedRound = itemListView.SelectedItem as TournamentRound;
            brdAddGame.Visibility = Visibility.Visible;
            // Populate combobox for add games
            cmbAddGameGames.Items.Clear();
            foreach (CKeyValuePair<int, int> plannedGame in selectedRound.PlannedMatches) {
                Member memberWhite = App.db.Members.Where(x => x.ID == plannedGame.Key).First();
                Member memberBlack = App.db.Members.Where(x => x.ID == plannedGame.Val).First();

                ComboBoxItem cmbItemNew = new ComboBoxItem();
                cmbItemNew.Name = plannedGame.Key + ":" + plannedGame.Val;
                cmbItemNew.Content = "White " + memberWhite.Name + ", Black " + memberBlack.Name;
                cmbAddGameGames.Items.Add(cmbItemNew);
            }
        }

        private async void btnAddGameSubmit_Click(object sender, RoutedEventArgs e) {
            // Stop if there is no tournament selected
            if (itemListView.SelectedIndex == -1)
                return;

            TournamentRound selectedRound = itemListView.SelectedItem as TournamentRound;
            // We can add the game from the details they have provided usually
            // First check they have selected
            if (cmbAddGameGames.SelectedIndex == -1 || cmbAddGameOutcome.SelectedIndex == -1) {
                return;
            }
            
            // The combobox item for addGameGames should be named in the form WhitePlayerID:BlackPlayerID
            string matchDetails = (cmbAddGameGames.SelectedItem as ComboBoxItem).Name;
            string[] matchDetailsSplit = matchDetails.Split(new char[] {':'});
            int whitePlayerID = int.Parse(matchDetailsSplit[0]);
            int blackPlayerID = int.Parse(matchDetailsSplit[1]);

            CKeyValuePair<int, int> selectedGame = selectedRound.PlannedMatches.Where(x => x.Key == whitePlayerID && x.Val == blackPlayerID).First();
            // We have the selected game and all details required to insert it into the database and update the tournament
            selectedRound.PlannedMatches.Remove(selectedGame);

            Game game = new Game();
            await game.Initialize();
            game.WhitePlayerID = whitePlayerID;
            game.BlackPlayerID = blackPlayerID;

            game.BlackPlayerELO = game.BlackPlayer.ELO;
            game.WhitePlayerELO = game.WhitePlayer.ELO;
            game.BlackPlayerKFactor = game.BlackPlayer.K_Factor;
            game.WhitePlayerKFactor = game.WhitePlayer.K_Factor;


            switch ((cmbAddGameOutcome.SelectedItem as ComboBoxItem).Name) {
                case "checkW":
                    // Remove loser from tournament
                    selectedRound.Tournament.RemainingContestantIDs.Remove(blackPlayerID);
                    game.Outcome = Enums.Outcome.WHITE_WIN;
                    break;
                case "checkB":
                    // Remove loser from tournament
                    selectedRound.Tournament.RemainingContestantIDs.Remove(whitePlayerID);
                    game.Outcome = Enums.Outcome.BLACK_WIN;
                    break;
                case "resignW":
                    // Remove loser from tournament
                    selectedRound.Tournament.RemainingContestantIDs.Remove(blackPlayerID);
                    game.Outcome = Enums.Outcome.BLACK_RESIGN;
                    break;
                case "resignB":
                    // Remove loser from tournament
                    selectedRound.Tournament.RemainingContestantIDs.Remove(whitePlayerID);
                    game.Outcome = Enums.Outcome.WHITE_RESIGN;
                    break;
            }
            
            // Find the most recent session
            Session mostRecent = null;
            foreach (Session session in App.db.Sessions) {
                if (mostRecent == null || session.Date < DateTime.Now && session.Date > mostRecent.Date) {
                    mostRecent = session;
                }
            }

            game.ApplyELOChange();
            game.SessionID = mostRecent.ID;
            selectedRound.GameIDs.Add(game.ID);

            // Check if the tournament has been won
            if (selectedRound.Tournament.RemainingContestantIDs.Count == 1) {
                selectedRound.Tournament.WinnerID = selectedRound.Tournament.RemainingContestantIDs.First();
                // Award them the prize ELO
                selectedRound.Tournament.Winner.ELO += selectedRound.Tournament.ELOPrize;
            }

            App.db.Games.Add(game);
            App.db.SaveData();

            grdGames.ItemsSource = selectedRound.Games;

            brdAddGame.Visibility = Visibility.Collapsed;
        }
    }
}
