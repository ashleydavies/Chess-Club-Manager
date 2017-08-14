using Chess_Club.Common;
using Chess_Club.Models;
using Chess_Club.Printable;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
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
    public sealed partial class SplitMembers : PrintingPage {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();
        /// <summary>
        /// Handles delete time verification
        /// </summary>
        private DispatcherTimer deleteTimer = new DispatcherTimer();
        /// <summary>
        /// Assists in delete time verification
        /// </summary>
        private int deleteTime = -1;

        public ObservableDictionary DefaultViewModel {
            get { return this.defaultViewModel; }
        }

        public NavigationHelper NavigationHelper {
            get { return this.navigationHelper; }
        }

        public SplitMembers() {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.navigationHelper.SaveState += navigationHelper_SaveState;

            // Setup the logical page navigation components that allow
            // the page to only show one pane at a time.
            this.navigationHelper.GoBackCommand = new Chess_Club.Common.RelayCommand(() => this.GoBack(), () => this.CanGoBack());
            this.itemListView.SelectionChanged += itemListView_SelectionChanged;

            // Guests can't edit members
            if (App.isGuest) {
                btnEdit.Visibility = Visibility.Collapsed;
            }

            // Set up timer
            deleteTimer.Interval = new TimeSpan(0, 0, 1);
            deleteTimer.Tick += deleteTimer_Tick;

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
            // Set tile and items (We hide the deleted members so only select members where the deleted value is falsed from database)
            this.DefaultViewModel["Items"] = App.db.Members.Where(x => x.Deleted == false);
            this.DefaultViewModel["Title"] = "Members";

            this.itemListView.SelectedItem = null;

            if (e.NavigationParameter != null) {
                // If we've been given an item to display, select that. This is most likely because they clicked it from the main page grid.
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

        /// <summary>
        /// If they select a game, navigate to it
        /// </summary>
        private void grdGames_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            Frame.Navigate(typeof(SplitGames), grdGames.SelectedItem);
        }
        /// <summary>
        /// If they click edit, go to the add/edit page with this member as the parameter
        /// </summary>
        private void btnEdit_Click(object sender, RoutedEventArgs e) {
            Frame.Navigate(typeof(AddEditMember), this.itemListView.SelectedItem);
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
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e) {
            navigationHelper.OnNavigatedFrom(e);
            base.OnNavigatedFrom(e);
        }

        #endregion
        /// <summary>
        /// Update the list when the search filter changes
        /// </summary>
        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e) {
            updateList();
        }

        /// <summary>
        /// Update the list when the order selection changes
        /// </summary>
        private void cmbOrderBy_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            updateList();
        }

        /// <summary>
        /// Update the list, reflecting order and search
        /// </summary>
        private void updateList() {
            // Search & Order
            if (cmbOrderBy.SelectedItem != null) {
                switch ((cmbOrderBy.SelectedItem as ComboBoxItem).Name) {
                    case "cmbForename":
                        // Filter by search string (Lowercase to avoid case sensitivity) and then order by forename
                        itemListView.ItemsSource = App.db.Members.Where(x => x.Name.ToLower().Contains(txtSearch.Text.ToLower()) && x.Deleted == false).OrderBy(x => x.Forename);
                        break;
                    case "cmbSurname":
                        // Filter by search string (Lowercase to avoid case sensitivity) and then order by surname
                        itemListView.ItemsSource = App.db.Members.Where(x => x.Name.ToLower().Contains(txtSearch.Text.ToLower()) && x.Deleted == false).OrderBy(x => x.Surname);
                        break;
                    case "cmbELO":
                        // Filter by search string (Lowercase to avoid case sensitivity) and then order by elo
                        itemListView.ItemsSource = App.db.Members.Where(x => x.Name.ToLower().Contains(txtSearch.Text.ToLower()) && x.Deleted == false).OrderByDescending(x => x.ELO);
                        break;
                    case "cmbGamesPlayed":
                        // Filter by search string (Lowercase to avoid case sensitivity) and then order by games played count
                        itemListView.ItemsSource = App.db.Members.Where(x => x.Name.ToLower().Contains(txtSearch.Text.ToLower()) && x.Deleted == false).OrderByDescending(x => x.Games.Count);
                        break;
                    default:
                        // Filter by search string (Lowercase to avoid case sensitivity) with no necessary order
                        itemListView.ItemsSource = App.db.Members.Where(x => x.Name.ToLower().Contains(txtSearch.Text.ToLower()) && x.Deleted == false);
                        break;
                }
            } else {
                // Filter by search string (Lowercase to avoid case sensitivity) with no necessary order
                itemListView.ItemsSource = App.db.Members.Where(x => x.Name.ToLower().Contains(txtSearch.Text.ToLower()) && x.Deleted == false);
            }
        }

        /// <summary>
        /// Load the print content for the page
        /// </summary>
        protected override void PreparePrintContent() {
            firstPage = new MemberPrintout();

            if (itemListView.SelectedIndex == -1)
                return;

            Member selectedMember = itemListView.SelectedItem as Member;
            
            // List of controls on print canvas:
            //  imgMemberPhoto, lblName, lblEmail, lblGames, lblSessions, lblElo, lblGame<1-10>, lblSession<1-10>

            int gamesWon = selectedMember.Games.Count(x => (
                // This is a lambda (inline function) collecting all games where the selectedMember won:

                x.BlackPlayerID == selectedMember.ID && x.Winner == "Black" || x.WhitePlayerID == selectedMember.ID && x.Winner == "White"
                ));

            int gamesLost = selectedMember.Games.Count(x => (
                // This is a lambda (inline function) collecting all games where the selectedMember lost:

                x.BlackPlayerID == selectedMember.ID && x.Winner == "White" || x.WhitePlayerID == selectedMember.ID && x.Winner == "Black"
                ));

            int eloPercent;
            // Get number of players with more elo
            int above = App.db.Members.Count(member => member.ELO > selectedMember.ELO);

            // Calculate the percentage of members above this player
            eloPercent = (int)((double) (100 / App.db.Members.Count) * above);

            (firstPage.FindName("imgMemberPhoto") as Image).Source = selectedMember.Bitmap;
            (firstPage.FindName("lblName") as TextBlock).Text = "Name: " + selectedMember.Name;
            (firstPage.FindName("lblEmail") as TextBlock).Text = "Email: " + selectedMember.Email;
            (firstPage.FindName("lblGames") as TextBlock).Text = "Number of games played: " + selectedMember.Games.Count + " (" + gamesLost.ToString() + " Lost, " + gamesWon.ToString() + " Won)";
            // We do a quick inline function here to count the number of sessions rather than extracting it into a variable
            (firstPage.FindName("lblSessions") as TextBlock).Text = "Number of sessions attended: " + (App.db.Sessions.Count(x => x.Members.Contains(selectedMember)));
            (firstPage.FindName("lblElo") as TextBlock).Text = "Elo: " + selectedMember.ELO + " (Top " + eloPercent.ToString() + "% of all members)";

            // Finally we fill in the game and session information
            for (int i = 0; i < 10; i++) {
                if (selectedMember.Games.Count > i) {
                    Game game = (selectedMember.Games.OrderByDescending(x => x.ModelCreationTime).ToList())[i];

                    string outcome;
                    // Figure out whether they won, lost or drew
                    if (game.Winner == "White" && game.WhitePlayerID == selectedMember.ID || game.Winner == "Black" && game.BlackPlayerID == selectedMember.ID)
                        outcome = "Won against ";
                    else if (game.Winner == "White" || game.Winner == "Black")
                        outcome = "Lost to ";
                    else
                        outcome = "Drew with ";

                    // Append the other player's name
                    if (game.WhitePlayerID == selectedMember.ID)
                        outcome += game.BlackPlayer.Name;
                    else
                        outcome += game.WhitePlayer.Name;
                    // Set label text
                    (firstPage.FindName("lblGame" + (i + 1)) as TextBlock).Text = outcome;
                } else {
                    // Set label text
                    (firstPage.FindName("lblGame" + (i + 1)) as TextBlock).Text = "";
                }
            }
            for (int i = 0; i < 10; i++) {
                // Put up to ten sessions on the page
                List<Session> sessions = App.db.Sessions.Where(x => x.Members.Contains(selectedMember)).OrderByDescending(x => x.ModelCreationTime).ToList();
                if (sessions.Count > i) {
                    Session session = sessions[i];
                    
                    (firstPage.FindName("lblSession" + (i + 1)) as TextBlock).Text = session.Title;
                } else {
                    // If we run out then just make it blank
                    (firstPage.FindName("lblSession" + (i + 1)) as TextBlock).Text = "";
                }
            }
            // Add the first page to the page root and force it to render
            PrintingRoot.Children.Add(firstPage);
            PrintingRoot.InvalidateMeasure();
            PrintingRoot.UpdateLayout();
        }

        /// <summary>
        /// When print is clicked we refresh the print preview and bring up the printing UI
        /// </summary>
        private async void btnPrint_Click(object sender, RoutedEventArgs e) {
            PreparePrintContent();
            await Windows.Graphics.Printing.PrintManager.ShowPrintUIAsync();
        }

        /// <summary>
        /// When delete is clicked the verification loop is enabled until they click a second time
        /// </summary>
        private void btnDelete_Click(object sender, RoutedEventArgs e) {
            if (itemListView.SelectedIndex != -1) {
                if (deleteTime == -1) {
                    // Start timer
                    deleteTime = 5;
                    deleteTimer.Start();
                    btnDelete.Content = "Click to permanently delete... 5";
                } else if (deleteTime > 0) {
                    // If they clicked again before timer expired we go ahead and delete.
                    (itemListView.SelectedItem as Member).Deleted = true;
                    // Refresh the list
                    updateList();
                    // Asynchronously save
                    App.db.SaveData();
                    // Correct button
                    btnDelete.Content = "Delete";
                    // And finally stop timer
                    deleteTimer.Stop();
                }
            }
        }

        /// <summary>
        /// Every second we lower the deleteTime by one and perform any additional necessary action
        /// </summary>
        void deleteTimer_Tick(object sender, object e) {
            deleteTime--;
            if (deleteTime == 0) {
                // Reset delete button
                btnDelete.Content = "Delete";
                // Stop timer
                deleteTimer.Stop();
                // Set the delete time to -1 (The value when the timer is not ticking)
                deleteTime = -1;
            } else {
                // Update the countdown
                btnDelete.Content = "Click to permanently delete... " + deleteTime.ToString();
            }
        }  
    }
}
