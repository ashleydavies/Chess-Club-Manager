using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Chess_Club.DAL;
using Chess_Club.Models;
using System.Diagnostics;
using Chess_Club.Common;
using System.Threading.Tasks;
#if (WINDOWS_PHONE_APP != true)
using Windows.UI.ApplicationSettings;
using System.Threading.Tasks;
#endif

namespace Chess_Club
{
    public sealed partial class App : Application
    {
#if WINDOWS_PHONE_APP
        private TransitionCollection transitions;
        // Class by Microsoft to handle continuing the program after interruptions such as file picking which we handle on the
        //  application level rather than handling for each individual interruption which would cause very bloated and confusing code.
        public ContinuationManager ContinuationManager { get; private set; }
#endif

        public static ChessClubContext db;
        public static Boolean loggedIn;
        public static Boolean isGuest;
        public static Member loggedInMember;

        public App()
        {
            this.InitializeComponent();
            this.Suspending += this.OnSuspending;

            db = new ChessClubContext();
        }

        /// <summary>
        /// Used for debugging. If true then upon startup the database will be seeded with fresh
        /// and random data.
        /// </summary>
        public static bool seed = false;

// Windows phones don't have the settings pane so compile it away if we're on a windows phone
#if (WINDOWS_PHONE_APP != true)
        /// <summary>
        /// When the window is created on a computer or tablet we tell it about the settings flyout
        /// </summary>
        protected override void OnWindowCreated(WindowCreatedEventArgs e) {
            SettingsPane.GetForCurrentView().CommandsRequested += Settings_CommandRequested;
        }

        /// <summary>
        /// Initialize settings flyout settings for computer
        /// </summary>
        private void Settings_CommandRequested(SettingsPane sender, SettingsPaneCommandsRequestedEventArgs args) {
            args.Request.ApplicationCommands.Add(new SettingsCommand("Settings", "Settings", (handler) => ShowSettingsFlyout()));
        }
        /// <summary>
        /// Show settings flyout function to display the settings flyout
        /// </summary>
        private void ShowSettingsFlyout() {
            ChessClubSettingsFlyout settingsFlyout = new ChessClubSettingsFlyout();
            settingsFlyout.Show();
        }
#endif

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used when the application is launched to open a specific file, to display
        /// search results, and so forth.
        /// </summary>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
#if DEBUG
            // If we're debugging we turn on the internal frame rate counter
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif

            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.CacheSize = 3;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    // TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
#if WINDOWS_PHONE_APP
                // Removes the turnstile navigation for startup.
                if (rootFrame.ContentTransitions != null)
                {
                    this.transitions = new TransitionCollection();
                    foreach (var c in rootFrame.ContentTransitions)
                    {
                        this.transitions.Add(c);
                    }
                }

                rootFrame.ContentTransitions = null;
                rootFrame.Navigated += this.RootFrame_FirstNavigated;
#endif

                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                if (!rootFrame.Navigate(typeof(Login), e.Arguments))
                {
                    throw new Exception("Failed to create initial page");
                }
            }
            
            // Ensure the current window is active
            Window.Current.Activate();
        }

        /// <summary>
        /// Method used to load data upon application launch
        /// </summary>
        public static async Task loadData(ProgressRing prgRing, TextBlock txtStatus, Button btnSubmit) {
            Random random = new Random();
            if (!seed) {
                // Load the database
                await db.LoadData();
                // if !...any checks if none exist
                if (!db.Members.Where(x => x.HasLogin == true).Any()) {
                    // Initialise a new admin staff member if none exist
                    Member member = new Member();
                    await member.Initialize();
                    member.Forename = "Admin";
                    member.Surname = "Account";
                    member.HasLogin = true;
                    member.Username = "admn";
                    member.Password = "root";
                    db.Members.Add(member);
                }
            } else {
                // Update status text
                txtStatus.Text = "Loading... Clearing Data";
                // Clear data
                await db.ClearData();
                // Update status text
                txtStatus.Text = "Loading... Initialising Members";
                // Form random names
                string[] names = { "Boris", "Greg", "James", "Jane", "June", "April", "Eric", "Dave", "Tom", "David", "Jeremy", "Lewis", "Luke" };
                // Seed members
                for (int i = 0; i < 10; i++) {
                    Member member = new Member();
                    // Create 10 members with random names and form the first one as a staff member
                    await member.Initialize();
                    member.ELO = 1200;
                    member.Forename = names[random.Next(0, names.Length)];
                    // Add a surname suffix on for telling them apart
                    member.Surname = names[random.Next(0, names.Length)] + (random.Next(0, 2) == 1 ? "son" : "sfield");
                    // First member should be a staff member
                    if (i == 0) {
                        member.Forename = "Ashley";
                        member.Surname = "Davies-Lyons";
                        member.HasLogin = true;
                        member.Username = "adl";
                        member.Password = "qwerty";
                    }
                    db.Members.Add(member);
                }
                // Add five sessions with 10 games each
                for (int i = 0; i < 5; i++) {
                    Session session = new Session();
                    await session.Initialize();
                    // Choose a random date between today and 31 days ago
                    session.Date = DateTime.Now.AddDays(-random.Next(0, 31)).Date;
                    // Update status text
                    txtStatus.Text = "Loading... Initialising Session " + (i + 1);
                    // Create 10 games
                    for (int g = 0; g < 10; g++) {
                        // Choose random players
                        Member newWhitePlayer = App.db.Members[random.Next(0, App.db.Members.Count)];
                        Member newBlackPlayer;
                        do {
                            newBlackPlayer = App.db.Members[random.Next(0, App.db.Members.Count)];
                        } while (newBlackPlayer == newWhitePlayer);
                        // Create and initialize game
                        Game newGame = new Game();
                        await newGame.Initialize();
                        // Set basic bariables to random things
                        newGame.Outcome = (Enums.Outcome)random.Next(0, 5);
                        newGame.DrawType = (Enums.DrawTypes)random.Next(0, 5);
                        // Set elo and kfactor to player details
                        newGame.BlackPlayerELO = newBlackPlayer.ELO;
                        newGame.WhitePlayerELO = newWhitePlayer.ELO;
                        newGame.BlackPlayerKFactor = newBlackPlayer.K_Factor;
                        newGame.WhitePlayerKFactor = newWhitePlayer.K_Factor;
                        newGame.ID = session.ID;
                        // Set the player variables
                        newGame.WhitePlayer = newWhitePlayer;
                        newGame.BlackPlayer = newBlackPlayer;
                        // change ELOs
                        newWhitePlayer.ELO += newGame.CalculateWhiteELOChange();
                        newBlackPlayer.ELO += newGame.CalculateBlackELOChange();
                        // add to games storage
                        db.Games.Add(newGame);
                    }
                    // add to sessions storage
                    db.Sessions.Add(session);
                }
                // Update status textbox
                txtStatus.Text = "Loading... Saving data to file";
                // Save
                await db.SaveData(txtStatus);
            }
            // Hide the status and progress tickers and show the button to submit
            btnSubmit.Visibility = Visibility.Visible;
            txtStatus.Visibility = Visibility.Collapsed;
            prgRing.Visibility = Visibility.Collapsed;
        }

#if WINDOWS_PHONE_APP
        /// <summary>
        /// Restores the content transitions after the app has launched.
        /// </summary>
        /// <param name="sender">The object where the handler is attached.</param>
        /// <param name="e">Details about the navigation event.</param>
        private void RootFrame_FirstNavigated(object sender, NavigationEventArgs e)
        {
            var rootFrame = sender as Frame;
            rootFrame.ContentTransitions = this.transitions ?? new TransitionCollection() { new NavigationThemeTransition() };
            rootFrame.Navigated -= this.RootFrame_FirstNavigated;
        }

        /// <summary>
        /// Called when the application is restored on the phone from e.g. a file picker dialog
        /// </summary>
        protected async override void OnActivated(IActivatedEventArgs args)
        {
            base.OnActivated(args);
            ContinuationManager = new ContinuationManager();

            if (args.PreviousExecutionState == ApplicationExecutionState.Terminated) {
                try {
                    await SuspensionManager.RestoreAsync();
                } catch (Exception e) { }
            }
            
            // Get the event args for the continuation, and run them through the continuationmanager to feed the right information into the page that paused for it
            IContinuationActivatedEventArgs eventArgs = (IContinuationActivatedEventArgs)args;
            if (eventArgs != null) {
                ContinuationManager.Continue(eventArgs, (Frame)Window.Current.Content);
            }
            // Load the app into the front of the phone
            Window.Current.Activate();
        }
#endif

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();

            // TODO: Save application state and stop any background activity
            deferral.Complete();
        }
    }
}