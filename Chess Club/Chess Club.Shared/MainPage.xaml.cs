using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Chess_Club.DAL;
using Chess_Club.Models;
using System.Diagnostics;

namespace Chess_Club {
    public sealed partial class MainPage : Page {
        public MainPage() {
            this.InitializeComponent();
            // Show either the small logout button or the logged in user grid based on whether the user is on the phone or not
#if WINDOWS_PHONE_APP
            btnSmallLogout.Visibility = Visibility.Visible;            
#else
            grdLoggedInUser.Visibility = Visibility.Visible;
#endif
        }

        /// <summary>
        /// Load the contents of the database into the list of members when it loads up
        /// </summary>
        private void grdMembers_Loaded(object sender, RoutedEventArgs e) {
            (sender as GridView).ItemsSource = App.db.Members;
        }

        /// <summary>
        /// Load the contents of the database into the list of games when it loads up
        /// </summary>
        private void grdGames_Loaded(object sender, RoutedEventArgs e) {
            (sender as GridView).ItemsSource = App.db.Games;
        }

        /// <summary>
        /// Load the contents of the database into the list of sessions when it loads up
        /// </summary>
        private void grdSessions_Loaded(object sender, RoutedEventArgs e) {
            (sender as GridView).ItemsSource = App.db.Sessions;
        }

        /// <summary>
        /// Load the contents of the database into the list of tournaments when it loads up
        /// </summary>
        private void grdTournaments_Loaded(object sender, RoutedEventArgs e) {
            (sender as GridView).ItemsSource = App.db.Tournaments;
        }

        /// <summary>
        /// When a new member is selected, go to the SplitMembers page, notifying it of which member to select.
        /// </summary>
        private void grdMembers_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            this.Frame.Navigate(typeof(SplitMembers), (sender as GridView).SelectedItem as Member);
        }

        /// <summary>
        /// When a new session is selected, go to the SplitSessions page, notifying it of which session to select.
        /// </summary>
        private void grdSessions_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            this.Frame.Navigate(typeof(SplitSessions), (sender as GridView).SelectedItem as Session);
        }

        /// <summary>
        /// When a new game is selected, go to the SplitGames page, notifying it of which game to select.
        /// </summary>
        private void grdGames_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            this.Frame.Navigate(typeof(SplitGames), (sender as GridView).SelectedItem as Game);
        }

        /// <summary>
        /// When a new tournament is selected, go to the SplitTournaments page, notifying it of which tournament to select.
        /// </summary>
        private void grdTournaments_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            this.Frame.Navigate(typeof(SplitTournaments), (sender as GridView).SelectedItem as Tournament);
        }

        /// <summary>
        /// Fired whenever one of the headers is clicked; use for navigational purposes
        /// </summary>
        private void Hub_SectionHeaderClick(object sender, HubSectionHeaderClickEventArgs e) {
            switch (e.Section.Name) {
                // check which section has been clicked and go to that page
                case "sctMembers":
                    this.Frame.Navigate(typeof(SplitMembers));
                    break;
                case "sctSessions":
                    this.Frame.Navigate(typeof(SplitSessions));
                    break;
                case "sctGames":
                    this.Frame.Navigate(typeof(SplitGames));
                    break;
                case "sctTournaments":
                    this.Frame.Navigate(typeof(SplitTournaments));
                    break;
            }
        }

        /// <summary>
        /// Fired when the add member button is clicked
        /// </summary>
        private void btnAddMember_Click(object sender, RoutedEventArgs e) {
            this.Frame.Navigate(typeof(AddEditMember));
        }

        /// <summary>
        /// Fired when the add session button is clicked
        /// </summary>
        private void btnAddSession_Click(object sender, RoutedEventArgs e) {
            this.Frame.Navigate(typeof(AddEditSession));
        }

        /// <summary>
        /// Called by the click of the add game button
        /// </summary>
        private void btnAddGame_Click(object sender, RoutedEventArgs e) {
            this.Frame.Navigate(typeof(AddGame));
        }

        /// <summary>
        /// Called by the click of the add tournament button
        /// </summary>
        private void btnAddTournament_Click(object sender, RoutedEventArgs e) {
            this.Frame.Navigate(typeof(AddEditTournament));
        }

        #region Load Statistics Labels

        /// <summary>
        /// Displays the number of sessions that have been hosted
        /// </summary>
        private void txtSessionCount_Loaded(object sender, RoutedEventArgs e) {
            ((TextBlock)sender).Text = string.Format("In total, there have been {0} sessions.", App.db.Sessions.Count);
        }

        /// <summary>
        /// Displays the number of members in the database
        /// </summary>
        private void txtMemberCount_Loaded(object sender, RoutedEventArgs e) {
            ((TextBlock)sender).Text = string.Format("There are {0} members.", App.db.Members.Count);
        }

        /// <summary>
        /// Displays the number of participations (Σ Session (MemberIDs.Count))
        /// </summary>
        private void txtAttendeesTotal_Loaded(object sender, RoutedEventArgs e) {
            int totalCount = 0;
            foreach (Session session in App.db.Sessions)
                totalCount += session.Members.Count;
            ((TextBlock)sender).Text = string.Format("Collectively, they have attended {0} sessions.", totalCount);
        }

        /// <summary>
        /// Displays the average number of sessions the members have participated in
        /// </summary>
        private void txtAverageMemberAttendance_Loaded(object sender, RoutedEventArgs e) {
            int average = 0;
            foreach (Session session in App.db.Sessions)
                average += session.Members.Count;
            average /= App.db.Members.Count;
            ((TextBlock)sender).Text = string.Format("Each member has attended an average of {0} sessions.", average);
        }

        #endregion

        /// <summary>
        /// When the welcome label loads, update the information based on whether they are a guest or not
        /// </summary>
        private void txtWelcomeLabel_Loaded(object sender, RoutedEventArgs e) {
            if (App.isGuest) {
                txtWelcomeLabel.Text = "Welcome Guest!";
                // Hide profile button for guests
                btnViewProfile.Visibility = Visibility.Collapsed;
            } else {
                txtWelcomeLabel.Text = "Welcome, " + App.loggedInMember.Name;
                // Show picture for staff
                imgLoggedInUser.Source = App.loggedInMember.Bitmap;
            }
        }

        /// <summary>
        /// Handles the view profile click; navigates to member profile
        /// </summary>
        private void btnViewProfile_Click(object sender, RoutedEventArgs e) {
            this.Frame.Navigate(typeof(SplitMembers), App.loggedInMember);
        }

        /// <summary>
        /// Handles logout by navigating to the login page and clearing the login related variables
        /// </summary>
        private void btnLogout_Click(object sender, RoutedEventArgs e) {
            // Reset login variables
            App.loggedIn = false;
            App.loggedInMember = null;
            App.isGuest = false;
            // Go to login page
            Frame.Navigate(typeof(Login));
        }

        /// <summary>
        /// When the add button loads, hide it if the user is a guest
        /// </summary>
        private void btnAddMember_Loaded(object sender, RoutedEventArgs e) {
            if (App.isGuest)
            {
                (sender as Button).Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// When the add button loads, hide it if the user is a guest
        /// </summary>
        private void btnAddSession_Loaded(object sender, RoutedEventArgs e) {
            if (App.isGuest)
            {
                (sender as Button).Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// When the add button loads, hide it if the user is a guest
        /// </summary>
        private void btnAddGame_Loaded(object sender, RoutedEventArgs e) {
            if (App.isGuest)
            {
                (sender as Button).Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// When the add button loads, hide it if the user is a guest
        /// </summary>
        private void btnAddTournament_Loaded(object sender, RoutedEventArgs e) {
            if (App.isGuest)
            {
                (sender as Button).Visibility = Visibility.Collapsed;
            }
        }
    }
}
