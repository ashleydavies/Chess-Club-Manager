using Chess_Club.Models;
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

namespace Chess_Club {
    sealed partial class ChessClubSettingsFlyout : SettingsFlyout {
        public ChessClubSettingsFlyout() {
            this.InitializeComponent();

            if (App.isGuest) {
                // Show the navigation buttons for guests
                btnGamesPage.IsEnabled = true;
                btnMembersPage.IsEnabled = true;
                btnSessionsPage.IsEnabled = true;
                btnTournamentsPage.IsEnabled = true;
            } else if (App.loggedIn) {
                // Show all buttons
                btnAddGame.IsEnabled = true;
                btnAddMember.IsEnabled = true;
                btnAddSession.IsEnabled = true;
                btnAddTournament.IsEnabled = true;
                btnDeleteAll.IsEnabled = true;
                btnDeleteGames.IsEnabled = true;
                btnDeleteTournaments.IsEnabled = true;
                btnGamesPage.IsEnabled = true;
                btnMembersPage.IsEnabled = true;
                btnResetElo.IsEnabled = true;
                btnSessionsPage.IsEnabled = true;
                btnTournamentsPage.IsEnabled = true;
            }
        }

        // Verification variables
        bool resetEloVerification;
        bool deleteGamesVerification;
        bool deleteTournamentsVerification;
        bool deleteAllVerification;

        /// <summary>
        /// Reset all player's elo back to 1200 (The default)
        /// </summary>
        private void btnResetElo_Click(object sender, RoutedEventArgs e) {
            if (!resetEloVerification) {
                // Engage Verification
                resetEloVerification = true;
                btnResetElo.Content = "Permanently reset Elo?";
            } else {
                // Reset elo
                foreach (Member member in App.db.Members) {
                    member.ELO = 1200;
                    // Reset button text
                    btnResetElo.Content = "Reset all member Elo";
                }
                // Save
                App.db.SaveData();
            }
        }

        /// <summary>
        /// Delete all game data from the database
        /// </summary>
        private void btnDeleteGames_Click(object sender, RoutedEventArgs e) {
            if (!deleteGamesVerification) {
                // Engage Verification
                deleteGamesVerification = true;
                btnDeleteGames.Content = "Permanently delete games data?";
            } else {
                // Delete games
                App.db.Games.Clear();
                App.db.SaveData();
                // Reset button text
                btnDeleteGames.Content = "Delete all games";
            }
        }

        /// <summary>
        /// Delete all tournament data from the database
        /// </summary>
        private void btnDeleteTournaments_Click(object sender, RoutedEventArgs e) {
            if (!deleteTournamentsVerification) {
                // Engage Verification
                deleteTournamentsVerification = true;
                btnDeleteTournaments.Content = "Permanently delete tournament data?";
            } else {
                // Delete tournaments
                App.db.TournamentRounds.Clear();
                App.db.Tournaments.Clear();
                App.db.SaveData();
                // Reset button text
                btnDeleteTournaments.Content = "Delete all tournament data";
            }
        }

        /// <summary>
        /// Delete all data from the database
        /// </summary>
        private async void btnDeleteAll_Click(object sender, RoutedEventArgs e) {
            if (!deleteAllVerification) {
                // Engage Verification
                deleteAllVerification = true;
                btnDeleteAll.Content = "Permanently delete all data?";
            } else {
                // Log out and delete all data
                App.loggedIn = false;
                App.loggedInMember = null;
                App.db.Members.Clear();
                App.db.Sessions.Clear();
                App.db.Games.Clear();
                App.db.TournamentRounds.Clear();
                App.db.Tournaments.Clear();
                await App.db.SaveData();
                // Get the page frame and logout
                Frame pageFrame = Window.Current.Content as Frame;
                pageFrame.Navigate(typeof(Login));
            }
        }

        /// <summary>
        /// Capture the frame (page) value and make it navigate to the add game page
        /// </summary>
        private void btnAddGame_Click(object sender, RoutedEventArgs e) {
            Frame pageFrame = Window.Current.Content as Frame;
            pageFrame.Navigate(typeof(AddGame));
        }

        /// <summary>
        /// Capture the frame (page) value and make it navigate to the add session page
        /// </summary>
        private void btnAddSession_Click(object sender, RoutedEventArgs e) {
            Frame pageFrame = Window.Current.Content as Frame;
            pageFrame.Navigate(typeof(AddEditSession));
        }

        /// <summary>
        /// Capture the frame (page) value and make it navigate to the add member page
        /// </summary>
        private void btnAddMember_Click(object sender, RoutedEventArgs e) {
            Frame pageFrame = Window.Current.Content as Frame;
            pageFrame.Navigate(typeof(AddEditMember));
        }

        /// <summary>
        /// Capture the frame (page) value and make it navigate to the add tournament page
        /// </summary>
        private void btnAddTournament_Click(object sender, RoutedEventArgs e) {
            Frame pageFrame = Window.Current.Content as Frame;
            pageFrame.Navigate(typeof(AddEditTournament));
        }

        /// <summary>
        /// Capture the frame (page) value and make it navigate to the games page
        /// </summary>
        private void btnGamesPage_Click(object sender, RoutedEventArgs e) {
            Frame pageFrame = Window.Current.Content as Frame;
            pageFrame.Navigate(typeof(SplitGames));
        }

        /// <summary>
        /// Capture the frame (page) value and make it navigate to the sessions page
        /// </summary>
        private void btnSessionsPage_Click(object sender, RoutedEventArgs e) {
            Frame pageFrame = Window.Current.Content as Frame;
            pageFrame.Navigate(typeof(SplitSessions));
        }

        /// <summary>
        /// Capture the frame (page) value and make it navigate to the members page
        /// </summary>
        private void btnMembersPage_Click(object sender, RoutedEventArgs e) {
            Frame pageFrame = Window.Current.Content as Frame;
            pageFrame.Navigate(typeof(SplitMembers));
        }

        /// <summary>
        /// Capture the frame (page) value and make it navigate to the tournaments page
        /// </summary>
        private void btnTournamentsPage_Click(object sender, RoutedEventArgs e) {
            Frame pageFrame = Window.Current.Content as Frame;
            pageFrame.Navigate(typeof(SplitTournaments));
        }
    }
}
