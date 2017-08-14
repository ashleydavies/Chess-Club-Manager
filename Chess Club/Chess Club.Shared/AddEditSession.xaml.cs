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
    public sealed partial class AddEditSession : Page {
        /// <summary>
        /// Title of the page
        /// </summary>
        public string title;
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();
        /// <summary>
        /// The session being edited if applicable
        /// </summary>
        public Session editingSession;

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
        
        public AddEditSession() {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.navigationHelper.SaveState += navigationHelper_SaveState;
        }

        /// <summary>
        /// Save data and return to previous page
        /// </summary>
        private async void Submit_Click(object sender, RoutedEventArgs e) {
            // Check if we're editing or not - We'll know since the session object would be set
            if (editingSession == null) {
                // Create a new session
                editingSession = new Session();
                // Initialize ID settings among others
                await editingSession.Initialize();
                // Add the new session to the database
                App.db.Sessions.Add(editingSession);
            }
            // Set variables
            editingSession.Name = txtName.Text;
            // .Date.Date due to UTC conversion; gets rid of the relative element as the current timezone is presumed to be the correct one.
            editingSession.Date = dtpDate.Date.Date;

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
            // Set default page title
            this.defaultViewModel["Title"] = "Add Session";

            Session editSession = null;

            // If we have been passed a session to edit we must set it to the edit session variable
            if (e.PageState != null && e.PageState.ContainsKey("Session")) {
                editSession = e.PageState["Session"] as Session;
            } else if (e.NavigationParameter != null) {
                editSession = e.NavigationParameter as Session;
            }
            // Again if we're editing we have to do something slightly different (loading data onto page)
            if (editSession != null) {
                // Multi purpose page, load the editing state

                this.editingSession = editSession;
                // And set the appropriate title to make it appear like this is a specialised page
                this.defaultViewModel["Title"] = "Edit Session";

                // And load the form values
                txtName.Text = editingSession.Name;
                dtpDate.Date = editingSession.Date;
            }
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
    }
}
