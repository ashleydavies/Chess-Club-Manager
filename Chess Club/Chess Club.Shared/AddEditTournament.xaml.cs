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

namespace Chess_Club {
    public sealed partial class AddEditTournament : Page {
        /// <summary>
        /// Title of the page
        /// </summary>
        public string title;
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();
        /// <summary>
        /// List of the selected members
        /// </summary>
        List<Member> selectedMembers = new List<Member>();

        public ObservableDictionary DefaultViewModel {
            get { return this.defaultViewModel; }
        }

        public NavigationHelper NavigationHelper {
            get { return this.navigationHelper; }
        }

        public string Title {
            get {
                return this.title;
            }
            set {
                this.title = value;
            }
        }

        public AddEditTournament() {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.navigationHelper.SaveState += navigationHelper_SaveState;
        }

        /// <summary>
        /// Save data and return to previous page
        /// </summary>
        private async void Submit_Click(object sender, RoutedEventArgs e) {
            // Validation
            if (txtName.Text.Trim() == "")
            {
                // Presence check on name
                lblNameError.Visibility = Visibility.Visible;
                return;
            }
            int eloPrize;
            // While we do a type check on teh elo prize we'll also put it into the elo prize variable, effectively solving two problems at once
            if (!int.TryParse(txtELOPrize.Text, out eloPrize))
            {
                // Show the validation error for elo
                lblEloError.Visibility = Visibility.Visible;
                return;
            }
            // Check at least two members are selected
            if (selectedMembers.Count < 2)
            {
                // If not, show the validation error
                lblContestantsError.Visibility = Visibility.Visible;
                return;
            }

            // Create a new tournament
            Tournament editingTournament = new Tournament();
            // Initialize ID settings among others
            await editingTournament.Initialize();
            //Save it to database
            App.db.Tournaments.Add(editingTournament);
            // Set it's remaining contestants to selected members
            editingTournament.RemainingContestants = selectedMembers;
            // Set basic variables from the form
            editingTournament.Name = txtName.Text;
            editingTournament.Contestants = selectedMembers;
            editingTournament.ELOPrize = int.Parse(txtELOPrize.Text);

            // Parse the combobox into a type of matchmaking and set it
            switch ((cmbMatchmaking.SelectedItem as ComboBoxItem).Name) {
                case "cmbFurthestELO":
                    editingTournament.MatchmakingStyle = Enums.MatchmakingStyle.OPPOSITE;
                    break;
                case "cmbRandom":
                    editingTournament.MatchmakingStyle = Enums.MatchmakingStyle.RANDOM;
                    break;
                case "cmbClosestELO":
                    editingTournament.MatchmakingStyle = Enums.MatchmakingStyle.CLOSE;
                    break;
            }
            // Save the database
            App.db.SaveData();

            // Go back if we can to the previous page
            if (navigationHelper.CanGoBack())
                navigationHelper.GoBack();
        }

        /// <summary>
        /// Called by the navigation helper when the page is loaded
        /// </summary>
        /// <param name="e">If the page has already been opened (and has been navigated into using back), contains the selected item under PageState</param>
        private void navigationHelper_LoadState(object sender, LoadStateEventArgs e) {
            this.defaultViewModel["Title"] = "Add Tournament";

            // Load the initial states of the page's grids
            updateGrids();
        }

        /// <summary>
        /// Called by the navigation helper when the page needs to be serialized
        /// </summary>
        /// <param name="e">Allows filling of an empty dictionary (PageState) for serialization</param>
        private void navigationHelper_SaveState(object sender, SaveStateEventArgs e) {
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

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e) {
            navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private void grdContestantSuggestions_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            // Add the selected member to the list of members
            if (grdContestantSuggestions.SelectedItem != null && !selectedMembers.Contains(grdContestantSuggestions.SelectedItem as Member)) {
                selectedMembers.Add(grdContestantSuggestions.SelectedItem as Member);
                // Update the search grid to reflect this
                updateGrids();
            }
            lblContestantsError.Visibility = Visibility.Collapsed;
        }

        private void grdContestants_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            // Remove the selected member from the list of members
            if (grdContestants.SelectedIndex != -1 && selectedMembers.Contains(grdContestants.SelectedItem as Member)) {
                selectedMembers.Remove(grdContestants.SelectedItem as Member);
                // And update the grid to reflect
                updateGrids();
            }
        }

        /// <summary>
        /// When they search for members we update the grids
        /// </summary>
        private void txtSearchMembers_TextChanged(object sender, TextChangedEventArgs e) {
            updateGrids();
        }

        private void updateGrids() {
            // Take the text of the search box
            string searchString = txtSearchMembers.Text;
            // Filter out members whose names have this in them and convert it to a list
            List<Member> searchMembers = App.db.Members.Where(x => x.Name.ToLower().Contains(searchString.ToLower())).ToList();
            // Now subtract the members who have been selected
            searchMembers.RemoveAll(x => (
                // We have a lambda for x for each entry in searchMembers, now we check if x is contained in selectedMembers
                selectedMembers.Contains(x)
                ));
            // Now pass it in as the source for contestants
            grdContestantSuggestions.ItemsSource = searchMembers;

            // For some reason, grid views hate being given a reference to the list so we copy it with ToList.
            grdContestants.ItemsSource = selectedMembers.ToList();
        }

        private void txtName_TextChanged(object sender, TextChangedEventArgs e)
        {
            lblNameError.Visibility = Visibility.Collapsed;
        }

        private void txtELOPrize_TextChanged(object sender, TextChangedEventArgs e)
        {
            lblEloError.Visibility = Visibility.Collapsed;
        }
    }
}
