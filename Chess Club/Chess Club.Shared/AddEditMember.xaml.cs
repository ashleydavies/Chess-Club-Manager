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
    // Windows phones will need the special interface for file open dialogs which we need for the pictures
#if WINDOWS_PHONE_APP
    public sealed partial class AddEditMember : Page, IFileOpenPickerContinuable {
#else
    public sealed partial class AddEditMember : Page {
#endif
        /// <summary>
        /// Stores whether an image is selected or not
        /// </summary>
        public bool imageSelected = false;
        /// <summary>
        /// The title of the page
        /// </summary>
        public string title;
        /// <summary>
        /// Generated objects for page management
        /// </summary>
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();
        /// <summary>
        /// If editing, this is the member that is being edited
        /// </summary>
        public Member editingMember;

        public ObservableDictionary DefaultViewModel {
            get { return this.defaultViewModel; }
        }

        public NavigationHelper NavigationHelper {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// The title of the page
        /// </summary>
        public string Title {
            get {
                return this.title;
            }
            set {
                this.title = value;
            }
        }
        
        public AddEditMember() {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.navigationHelper.SaveState += navigationHelper_SaveState;
        }

        /// <summary>
        /// Windows phone apps have a more convoluted way of opening files using a filepicker as it takes over the interface
        /// </summary>
#if WINDOWS_PHONE_APP
        public async void ContinueFileOpenPicker(FileOpenPickerContinuationEventArgs args) {
            // We should have an image
            if (args.Files.Count > 0) {
                StorageFile copiedFile = await args.Files[0].CopyAsync(await ApplicationData.Current.RoamingFolder.CreateFolderAsync("MemberPhotos", CreationCollisionOption.OpenIfExists), "temp.png", NameCollisionOption.ReplaceExisting);
                // Open the copied file into a bitmap
                BitmapImage image = new BitmapImage();
                // Set the source to the file's read stream
                image.SetSource(await copiedFile.OpenReadAsync());

                // And finally load it into the image
                imgMemberPicture.Source = image;

                imageSelected = true;
            }
        }
        
        /// <summary>
        /// When the browse button is clicked, we open a file explorer for them to choose a photograph.
        /// </summary>
        private void btnBrowse_Click(object sender, RoutedEventArgs e) {
#else
        /// <summary>
        /// This method signature is for computers, the distinct difference being it being asynchronous.
        /// </summary>
        private async void btnBrowse_Click(object sender, RoutedEventArgs e) {
#endif
            // Set up a file picker
            FileOpenPicker picker = new FileOpenPicker();
            // Add file extensions
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            // Set the view mode to thumbnail so they can see them
            picker.ViewMode = PickerViewMode.Thumbnail;
            // And put them in the pictures directory since that's probably where they want to go
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            // This gives a name to this picker so it doesn't default to other pickers' last locations
            picker.SettingsIdentifier = "AvatarPicker";
            picker.CommitButtonText = "Set as avatar";

#if WINDOWS_PHONE_APP
            // For a phone we just do this, and it redirects to the continue method above when finished
            picker.PickSingleFileAndContinue();
#else
            // On a computer we're able to continue here; we use await because it will pause for the file to be selected
            StorageFile file = await picker.PickSingleFileAsync();

            // Check if they selected a file
            if (file != null) {
                StorageFile copiedFile = await file.CopyAsync(await ApplicationData.Current.RoamingFolder.CreateFolderAsync("MemberPhotos", CreationCollisionOption.OpenIfExists), "temp.png", NameCollisionOption.ReplaceExisting);
                // Open the copied file into a bitmap
                BitmapImage image = new BitmapImage();
                // Set the source to the file's read stream
                image.SetSource(await copiedFile.OpenReadAsync());

                // And finally load it into the image
                imgMemberPicture.Source = image;
                imageSelected = true;
            }
#endif
        }

        /// <summary>
        /// When use default is clicked, we put the default user image back into the image.
        /// </summary>
        private void btnUseDefault_Click(object sender, RoutedEventArgs e) {
            // Load the default member picture into the image
            imgMemberPicture.Source = new BitmapImage(new Uri(BaseUri, "Assets/MemberDefaultPicture.png"));
            imageSelected = false;
            // If we're editing, we set the picture variable to false, if not, it'll be dealt with in create
            if (editingMember != null)
                editingMember.Picture = false;
        }

        /// <summary>
        /// Save data and return to previous page
        /// </summary>
        private async void Submit_Click(object sender, RoutedEventArgs e) {
            // VALIDATION
            if (txtForename.Text.Trim() == "") {
                // Show lack of forename error
                lblForenameError.Text = "Please enter a forename.";
                lblForenameError.Visibility = Visibility.Visible;
                return;
            } else if (txtForename.Text.Length > 12) {
                // Show long forename error
                lblForenameError.Text = "Please enter a shorter forename (<12 characters)";
                return;
            }

            if (txtSurname.Text.Trim() == "") {
                // Show lack of surname error
                lblSurnameError.Text = "Please enter a surname.";
                lblSurnameError.Visibility = Visibility.Visible;
                return;
            } else if (txtSurname.Text.Length > 12) {
                // Show long surname error
                lblSurnameError.Text = "Please enter a shorter surname (<12 characters)";
                return;
            }

            if (!txtEmail.Text.Contains("@") || txtEmail.Text.Length < 5) {
                // Show email validation error
                lblEmailError.Visibility = Visibility.Visible;
                return;
            }

            if (chkHasLogin.IsChecked.Value) {
                // Show username/password presence check errors if they are empty and this account is marked as having a login
                if (txtUsername.Text.Trim() == "") {
                    lblUsernameError.Visibility = Visibility.Visible;
                    return;
                }
                if (txtPassword.Password.Trim() == "") {
                    lblPasswordError.Visibility = Visibility.Visible;
                    return;
                }
            }

            // Check if we're editing or not - We'll know since the member object would be set
            if (editingMember == null) {
                // Create a new member
                editingMember = new Member();
                // Initialize ID settings among others
                await editingMember.Initialize();
                // Add it to the database
                App.db.Members.Add(editingMember);
                // Set it's elo to the default (1,200)
                editingMember.ELO = 1200;
            }
            // And set up basic variables from the form
            editingMember.Forename = txtForename.Text;
            editingMember.Surname = txtSurname.Text;
            editingMember.Email = txtEmail.Text;
            
            // If an image was selected
            if (imageSelected) {
                // Open the member photos folder
                StorageFolder memberPhotos = await ApplicationData.Current.RoamingFolder.GetFolderAsync("MemberPhotos");
                // Get the temp file
                StorageFile temp = await memberPhotos.GetFileAsync("temp.png");

                // Choose the next ID number
                StorageFile sFile = await memberPhotos.CreateFileAsync("ID", CreationCollisionOption.OpenIfExists);
                // Should store an integer in plaintext
                string idString = await FileIO.ReadTextAsync(sFile);
                int id;
                bool success = int.TryParse(idString, out id);
                if (!success) // File was empty unless tampered with; return 1.
                    id = 1;
                // Delete the file
                await sFile.DeleteAsync(StorageDeleteOption.PermanentDelete);
                // Create a new file
                sFile = await memberPhotos.CreateFileAsync("ID", CreationCollisionOption.OpenIfExists);
                // Store the ID for the next photograph in it
                await FileIO.WriteTextAsync(sFile, (id + 1).ToString());
                // And rename the image file to <ID>.png
                await temp.RenameAsync(id + ".png", NameCollisionOption.ReplaceExisting);

                // And finally set the user's picture value to true
                editingMember.Picture = true;
                editingMember.PictureNumber = id;
            }

            // If they have a login
            if (chkHasLogin.IsChecked.Value) {
                // Set login variables from form
                editingMember.Username = txtUsername.Text;
                editingMember.Password = txtPassword.Password;
                editingMember.HasLogin = true;
            } else {
                // Set haslogin as false
                editingMember.HasLogin = false;
            }
            // Save and go back
            await App.db.SaveData();
            if (navigationHelper.CanGoBack())
                navigationHelper.GoBack();
        }

        /// <summary>
        /// Called by the navigation helper when the page is loaded
        /// </summary>
        /// <param name="e">If the page has already been opened (and has been navigated into using back), contains the selected item under PageState</param>
        private void navigationHelper_LoadState(object sender, LoadStateEventArgs e) {
            // Set default title
            this.defaultViewModel["Title"] = "Add Member";

            Member editMember = null;

            // If a member was passed to edit then we set the member variable to them
            if (e.PageState != null && e.PageState.ContainsKey("Member")) {
                editMember = e.PageState["Member"] as Member;
            } else if (e.NavigationParameter != null ) {
                editMember = e.NavigationParameter as Member;
            }

            // If editing a member
            if (editMember != null) {
                // Multi purpose page, load the editing state
                // Cast e.PageState to Member
                // And set the appropriate title to make it appear like this is a specialised page
                this.defaultViewModel["Title"] = "Edit Member";

                // And load the form values
                txtForename.Text = editMember.Forename;
                txtSurname.Text = editMember.Surname;
                imgMemberPicture.Source = editMember.Bitmap;
                chkHasLogin.IsChecked = editMember.HasLogin;
                // If and only if they have a login we set the username and password fields too
                if (editMember.HasLogin) {
                    txtUsername.Text = editMember.Username;
                    txtPassword.Password = editMember.Password;
                }
                // And set editingmember for future reference
                editingMember = editMember;
            }
        }

        /// <summary>
        /// Called by the navigation helper when the page needs to be serialized
        /// </summary>
        /// <param name="e">Allows filling of an empty dictionary (PageState) for serialization</param>
        private void navigationHelper_SaveState(object sender, SaveStateEventArgs e) {
        }

        /// <summary>
        /// Enabled login related fields when login is checked
        /// </summary>
        private void chkHasLogin_Checked(object sender, RoutedEventArgs e) {
            txtUsername.IsEnabled = true;
            txtPassword.IsEnabled = true;
        }

        /// <summary>
        /// Disables login related fields when login is checked
        /// </summary>
        private void chkHasLogin_Unchecked(object sender, RoutedEventArgs e) {
            txtUsername.IsEnabled = false;
            txtPassword.IsEnabled = false;
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

        /// <summary>
        /// Hides the verification error when the user types
        /// </summary>
        private void txtUsername_TextChanged(object sender, TextChangedEventArgs e) {
            lblUsernameError.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Hides the verification error when the user types
        /// </summary>
        private void txtSurname_TextChanged(object sender, TextChangedEventArgs e) {
            lblSurnameError.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Hides the verification error when the user types
        /// </summary>
        private void txtForename_TextChanged(object sender, TextChangedEventArgs e) {
            lblForenameError.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Hides the verification error when the user types
        /// </summary>
        private void txtPassword_PasswordChanged(object sender, RoutedEventArgs e) {
            lblPasswordError.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Hides the verification error when the user types
        /// </summary>
        private void txtEmail_TextChanged(object sender, TextChangedEventArgs e) {
            lblEmailError.Visibility = Visibility.Collapsed;
        }
    }
}
