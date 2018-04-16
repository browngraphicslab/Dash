using System;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Microsoft.HockeyApp;
using DashShared;

namespace Dash
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// The instance of the app this can be used to access the services for dependency injection
        /// </summary>
        public static App Instance;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            HockeyClient.Current.Configure("76a6328a3b574146b9d1a171d67f9af2");
            Instance = this;
            this.InitializeComponent();
            this.Suspending += OnSuspending;
            this.Resuming += OnResuming;
        }


        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                //this.DebugSettings.EnableFrameRateCounter = true;
                //this.DebugSettings.IsTextPerformanceVisualizationEnabled = true;
            }
#endif

            // register dependency injection container
            Container = RegisterServices();

            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    KeyStore.RegisterDocumentTypeRenderer(ApiOperatorBox.DocumentType, ApiOperatorBox.MakeView);
                    KeyStore.RegisterDocumentTypeRenderer(BackgroundBox.DocumentType, BackgroundBox.MakeView);
                    KeyStore.RegisterDocumentTypeRenderer(DBSearchOperatorBox.DocumentType, DBSearchOperatorBox.MakeView);
                    KeyStore.RegisterDocumentTypeRenderer(RichTextBox.DocumentType, RichTextBox.MakeView);
                    KeyStore.RegisterDocumentTypeRenderer(ExecuteHtmlOperatorBox.DocumentType, ExecuteHtmlOperatorBox.MakeView);
                    KeyStore.RegisterDocumentTypeRenderer(DashConstants.TypeStore.ExtractSentencesDocumentType, ExtractSentencesOperatorBox.MakeView);
                    KeyStore.RegisterDocumentTypeRenderer(DataBox.DocumentType, DataBox.MakeView);
                    KeyStore.RegisterDocumentTypeRenderer(GridViewLayout.DocumentType, GridViewLayout.MakeView);
                    KeyStore.RegisterDocumentTypeRenderer(DashConstants.TypeStore.CollectionBoxType, CollectionBox.MakeView);
                    KeyStore.RegisterDocumentTypeRenderer(DashConstants.TypeStore.FreeFormDocumentType, FreeFormDocument.MakeView);
                    KeyStore.RegisterDocumentTypeRenderer(GridLayout.DocumentType, GridLayout.MakeView);
                    KeyStore.RegisterDocumentTypeRenderer(ImageBox.DocumentType, ImageBox.MakeView);
                    KeyStore.RegisterDocumentTypeRenderer(InkBox.DocumentType, InkBox.MakeView);
                    KeyStore.RegisterDocumentTypeRenderer(KeyValueDocumentBox.DocumentType, KeyValueDocumentBox.MakeView);
                    KeyStore.RegisterDocumentTypeRenderer(ListViewLayout.DocumentType, ListViewLayout.MakeView);
                    KeyStore.RegisterDocumentTypeRenderer(DashConstants.TypeStore.MeltOperatorBoxDocumentType, MeltOperatorBox.MakeView);
                    KeyStore.RegisterDocumentTypeRenderer(DashConstants.TypeStore.OperatorBoxType, OperatorBox.MakeView);
                    KeyStore.RegisterDocumentTypeRenderer(PdfBox.DocumentType, PdfBox.MakeView);
                    KeyStore.RegisterDocumentTypeRenderer(PreviewDocument.DocumentType, PreviewDocument.MakeView);
                    KeyStore.RegisterDocumentTypeRenderer(DashConstants.TypeStore.QuizletOperatorType, QuizletOperatorBox.MakeView);
                    KeyStore.RegisterDocumentTypeRenderer(DashConstants.TypeStore.SearchOperatorType, SearchOperatorBox.MakeView);
                    KeyStore.RegisterDocumentTypeRenderer(StackLayout.DocumentType, StackLayout.MakeView);
                    KeyStore.RegisterDocumentTypeRenderer(TextingBox.DocumentType, TextingBox.MakeView);
                    KeyStore.RegisterDocumentTypeRenderer(WebBox.DocumentType, WebBox.MakeView);
					KeyStore.RegisterDocumentTypeRenderer(VideoBox.DocumentType, VideoBox.MakeView);
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    //TODO Navigate to a prelogged in page if we can!
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                    ListContainedFieldFlag.Enabled = true; // works but slows things down A LOT!
                }

                // Ensure the current window is active
                Window.Current.Activate();
            }

            //var result = RESTClient.Instance.Keys.AddKey(new DashShared.Key("B2C37434-04FD-4663-898A-944E3DB5AB78", "mykey")).Result;
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

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
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }


        /// <summary>
        /// called when the application resumes after having suspended
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="o"></param>
        private void OnResuming(object sender, object o)
        {
            BrowserView.ForceInit();
        }

    }
}
