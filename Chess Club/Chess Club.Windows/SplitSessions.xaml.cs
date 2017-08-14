using Chess_Club.Common;
using Chess_Club.Models;
using Chess_Club.Printable;
using System;
using System.Collections.Generic;
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
    public sealed partial class SplitSessions : PrintingPage {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        public ObservableDictionary DefaultViewModel {
            get { return this.defaultViewModel; }
        }

        public NavigationHelper NavigationHelper {
            get { return this.navigationHelper; }
        }

        public SplitSessions() {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.navigationHelper.SaveState += navigationHelper_SaveState;

            // Setup the logical page navigation components that allow
            // the page to only show one pane at a time.
            this.navigationHelper.GoBackCommand = new Chess_Club.Common.RelayCommand(() => this.GoBack(), () => this.CanGoBack());
            this.itemListView.SelectionChanged += itemListView_SelectionChanged;
            
            // Guests can't edit sessions
            if (App.isGuest) {
                btnEdit.Visibility = Visibility.Collapsed;
            }
            
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
            this.DefaultViewModel["Items"] = App.db.Sessions;
            this.DefaultViewModel["Title"] = "Sessions";

            this.itemListView.SelectedItem = null;

            if (e.NavigationParameter != null) {
                // If we've been given an item to display, select that. This is most likely because they clicked it from the main page grid, or from a member's profile.
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

        // The split page is designed so that when the Window does have enough space to show
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
        }

        #endregion

        /// <summary>
        /// When a new game is selected go to it
        /// </summary>
        private void grdGames_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (grdGames.SelectedIndex != -1)
                // Navigate to selections
                this.Frame.Navigate(typeof(SplitGames), grdGames.SelectedItem);
        }

        /// <summary>
        /// When edit is clicked, navigate to the editing page with the relevant parameter
        /// </summary>
        private void btnEdit_Click(object sender, RoutedEventArgs e) {
            Frame.Navigate(typeof(AddEditSession), this.itemListView.SelectedItem);
        }

        /// <summary>
        /// Prepares the content for the printer based on the current selection
        /// </summary>
        protected override void PreparePrintContent() {
            // Create a session printout
            firstPage = new SessionPrintout();

            // If there's no selection then stop
            if (itemListView.SelectedIndex == -1)
                return;
            // Otherwise load the selected session
            Session selectedSession = itemListView.SelectedItem as Session;
            // Set the basic variables
            (firstPage.FindName("lblTitle") as TextBlock).Text = "Session: " + selectedSession.Title;
            (firstPage.FindName("lblGames") as TextBlock).Text = "Games Played: " + selectedSession.GameIDs.Count.ToString();
            (firstPage.FindName("lblMembers") as TextBlock).Text = "Members attended: " + selectedSession.Members.Count.ToString();

            // Finally we fill in the game information
            // Loop games
            foreach (Game game in selectedSession.Games.OrderByDescending(x => x.ModelCreationTime)) {
                // Make a new textblock and set up basic properties
                TextBlock block = new TextBlock();
                block.FontSize = 22;
                block.Margin = new Thickness(0, 5, 0, 5);
                block.Foreground = new SolidColorBrush(Windows.UI.Colors.Black);
                block.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Left;
                // Copy the style from the title
                block.Style = (firstPage.FindName("lblPnlGamesTitle") as TextBlock).Style;

                // We take the white player's name first
                block.Text = game.WhitePlayer.Name;
                // Then concatenate whether they won or lost
                block.Text += (game.Winner == "White" ? " won against " : game.Winner == "Black" ? " lost to " : " drew with ");
                // Finally add the black player's name
                block.Text += game.BlackPlayer.Name;

                // Add it to the panel of games
                (firstPage.FindName("pnlGames") as StackPanel).Children.Add(block);
            }

            PrintingRoot.Children.Add(firstPage);
            PrintingRoot.InvalidateMeasure();
            PrintingRoot.UpdateLayout();
        }

        /// <summary>
        /// When print is clicked prepare the content and bring up the UI
        /// </summary>
        private async void btnPrint_Click(object sender, RoutedEventArgs e) {
            PreparePrintContent();
            await Windows.Graphics.Printing.PrintManager.ShowPrintUIAsync();
        }
    }
}
