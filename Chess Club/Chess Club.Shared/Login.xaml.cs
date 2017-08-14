using Chess_Club.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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
    public sealed partial class Login : Page {
        public Login() {
            this.InitializeComponent();
        }

        /// <summary>
        /// When the page is loaded, we ask the application to start loading all of the data
        /// </summary>
        private async void Page_Loaded(object sender, RoutedEventArgs e) {
            // Load, giving along a progress right and status textbox and the submit button. It will now control these.
            await App.loadData(prgRing, txtLoadingStatus, btnSubmit);
        }

        private void chkGuest_Checked(object sender, RoutedEventArgs e) {
            // Disable user input
            txtUsername.IsEnabled = false;
            txtPassword.IsEnabled = false;
            // Set the guest variable
            App.isGuest = true;
            // Update button submit's text to make it more intuitive
            btnSubmit.Content = "Login as Guest";
        }

        private void chkGuest_Unchecked(object sender, RoutedEventArgs e) {
            // Enable user input
            txtUsername.IsEnabled = true;
            txtPassword.IsEnabled = true;
            // Set the guest variable to false
            App.isGuest = false;
            // Set the button submit's text back to Login
            btnSubmit.Content = "Login";
        }

        private void btnSubmit_Click(object sender, RoutedEventArgs e) {
            if (chkGuest.IsChecked.Value) {
                // Log straight in, guest boolean is set to true in chkGuest_Checked so we're safe to proceed.
                Frame.Navigate(typeof(MainPage));
            } else {
                // Presence check
                if (txtUsername.Text.Trim() == "") {
                    // Show the error
                    txtUsernameError.Visibility = Visibility.Visible;
                    return;
                } else if (txtPassword.Password.Trim() == "") {
                    // Show the error
                    txtPasswordError.Visibility = Visibility.Visible;
                    return;
                }
                // Validate login information by searching database for it
                Member member = App.db.Members.FirstOrDefault(x => x.HasLogin == true && x.Username == txtUsername.Text);

                if (member == null) {
                    // No such username
                    txtUsernameError.Visibility = Visibility.Visible;
                    return;
                }

                if (member.Password != txtPassword.Password) {
                    // Incorrect password
                    txtPasswordError.Visibility = Visibility.Visible;
                    return;
                }

                // Login as the member successfully
                App.loggedIn = true;
                App.loggedInMember = member;
                Frame.Navigate(typeof(MainPage));
            }
        }

        private void txtUsername_TextChanged(object sender, TextChangedEventArgs e) {
            // Hide error when they type in the box
            txtUsernameError.Visibility = Visibility.Collapsed;
        }

        private void txtPassword_PasswordChanged(object sender, RoutedEventArgs e) {
            // Hide error when they type in the box
            txtPasswordError.Visibility = Visibility.Collapsed;
        }
    }
}
