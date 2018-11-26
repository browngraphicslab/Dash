using System;
using System.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Media.SpeechRecognition;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Navigation;
using DashShared;
using Microsoft.Extensions.DependencyInjection;
using Windows.Storage;
using Microsoft.Office.Interop.Word;
using Application = Windows.UI.Xaml.Application;
using Frame = Windows.UI.Xaml.Controls.Frame;
using Task = System.Threading.Tasks.Task;
using Window = Windows.UI.Xaml.Window;

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

        //.NET interop stuff
        public static BackgroundTaskDeferral AppServiceDeferral = null;
        public static AppServiceConnection Connection = null;
        public static event Action AppServiceConnected;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
#if !DEBUG
            HockeyClient.Current.Configure("76a6328a3b574146b9d1a171d67f9af2");
#endif
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
        protected async override void OnLaunched(LaunchActivatedEventArgs e)
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
                    KeyStore.RegisterDocumentTypeRenderer(BackgroundShape.DocumentType, BackgroundShape.MakeView, null);
                    KeyStore.RegisterDocumentTypeRenderer(RichTextBox.DocumentType, (doc, context) => RichTextBox.MakeView(doc, KeyStore.DataKey, context), RichTextBox.MakeRegionDocument);
                    KeyStore.RegisterDocumentTypeRenderer(DiscussionBox.DocumentType, (doc, context) => DiscussionBox.MakeView(doc, KeyStore.DataKey, context), null);
                    KeyStore.RegisterDocumentTypeRenderer(ExecuteHtmlOperatorBox.DocumentType, ExecuteHtmlOperatorBox.MakeView, null);
                    KeyStore.RegisterDocumentTypeRenderer(DashConstants.TypeStore.ExtractSentencesDocumentType, ExtractSentencesOperatorBox.MakeView, null);
                    KeyStore.RegisterDocumentTypeRenderer(DataBox.DocumentType, DataBox.MakeView, null);
                    KeyStore.RegisterDocumentTypeRenderer(DashConstants.TypeStore.CollectionBoxType, CollectionBox.MakeView, null);
                    KeyStore.RegisterDocumentTypeRenderer(TableBox.DocumentType, (doc, context) => TableBox.MakeView(doc, KeyStore.DataKey, context), null);
                    KeyStore.RegisterDocumentTypeRenderer(DashConstants.TypeStore.FreeFormDocumentType, FreeFormDocument.MakeView, null);
                    KeyStore.RegisterDocumentTypeRenderer(ImageBox.DocumentType, (doc, context) => ImageBox.MakeView(doc,KeyStore.DataKey, context), ImageBox.MakeRegionDocument);
                    KeyStore.RegisterDocumentTypeRenderer(InkBox.DocumentType, InkBox.MakeView, null);
                    KeyStore.RegisterDocumentTypeRenderer(KeyValueDocumentBox.DocumentType, KeyValueDocumentBox.MakeView, null);
                    KeyStore.RegisterDocumentTypeRenderer(DashConstants.TypeStore.MeltOperatorBoxDocumentType, MeltOperatorBox.MakeView, null);
                    KeyStore.RegisterDocumentTypeRenderer(DashConstants.TypeStore.OperatorBoxType, OperatorBox.MakeView, null);
                    KeyStore.RegisterDocumentTypeRenderer(PdfBox.DocumentType, (doc, context) => PdfBox.MakeView(doc, KeyStore.DataKey, context), PdfBox.MakeRegionDocument);
                    KeyStore.RegisterDocumentTypeRenderer(PreviewDocument.DocumentType, PreviewDocument.MakeView, null);
                    KeyStore.RegisterDocumentTypeRenderer(DashConstants.TypeStore.QuizletOperatorType, QuizletOperatorBox.MakeView, null);
                    KeyStore.RegisterDocumentTypeRenderer(DashConstants.TypeStore.SearchOperatorType, SearchOperatorBox.MakeView, null);
                    KeyStore.RegisterDocumentTypeRenderer(TextingBox.DocumentType, (doc, context) => TextingBox.MakeView(doc, KeyStore.DataKey, context), null);
                    KeyStore.RegisterDocumentTypeRenderer(MarkdownBox.DocumentType, MarkdownBox.MakeView, null);
                    KeyStore.RegisterDocumentTypeRenderer(DishReplBox.DocumentType, DishReplBox.MakeView, null);
                    KeyStore.RegisterDocumentTypeRenderer(DishScriptBox.DocumentType, DishScriptBox.MakeView, null);
                    KeyStore.RegisterDocumentTypeRenderer(EditableScriptBox.DocumentType, EditableScriptBox.MakeView, null);
                    KeyStore.RegisterDocumentTypeRenderer(WebBox.DocumentType, WebBox.MakeView, null);
					KeyStore.RegisterDocumentTypeRenderer(VideoBox.DocumentType, (doc, context) => VideoBox.MakeView(doc, KeyStore.DataKey, context), null);
                    KeyStore.RegisterDocumentTypeRenderer(AudioBox.DocumentType, (doc, context) => AudioBox.MakeView(doc, KeyStore.DataKey, context), null);
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    //TODO Navigate to a prelogged in page if we can!
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                    ListContainedFieldFlag.Enabled = true; // works but slows things down A LOT!
                }

                ApplicationView.PreferredLaunchViewSize = new Size(1800, 1000);
                ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

                // Ensure the current window is active
                Window.Current.Activate();
            }

            //set up voice commands
            try
            {
                // Install the main VCD. 
                StorageFile vcdStorageFile =
                    await Package.Current.InstalledLocation.GetFileAsync(@"DashCommands.xml");

                var file = new Uri("ms-appx:///DashCommands.xml");
                var storageFile = await StorageFile.GetFileFromApplicationUriAsync(file);

                var defs = Windows.ApplicationModel.VoiceCommands.VoiceCommandDefinitionManager.InstalledCommandDefinitions;
                //TODO: idk which type works
                await Windows.ApplicationModel.VoiceCommands.VoiceCommandDefinitionManager.
                    InstallCommandDefinitionsFromStorageFileAsync(vcdStorageFile);

                // Update phrase list.
                //ViewModel.ViewModelLocator locator = App.Current.Resources["ViewModelLocator"] as ViewModel.ViewModelLocator;
                //if (locator != null)
                //{
                //    await locator.TripViewModel.UpdateDestinationPhraseList();
                //}
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Installing Voice Commands Failed: " + ex.ToString());
            }

            //var result = RESTClient.Instance.Keys.AddKey(new DashShared.Key("B2C37434-04FD-4663-898A-944E3DB5AB78", "mykey")).Result;
        }

        /// <summary>
        /// Entry point for an application activated by some means other than normal launching. 
        /// This includes voice commands, URI, share target from another app, and so on. 
        /// 
        /// NOTE:
        /// A previous version of the VCD file might remain in place 
        /// if you modify it and update the app through the store. 
        /// Activations might include commands from older versions of your VCD. 
        /// Try to handle these commands gracefully.
        /// </summary>
        /// <param name="args">Details about the activation method.</param>
        protected override void OnActivated(IActivatedEventArgs args)
        {
            base.OnActivated(args);

            Type navigationToPageType = typeof(MainPage);
            DashVoiceCommand navigationCommand = null;

            // Voice command activation.
            if (args.Kind == ActivationKind.VoiceCommand)
            {
                // Event args can represent many different activation types. 
                // Cast it so we can get the parameters we care about out.
                var commandArgs = args as VoiceCommandActivatedEventArgs;

                Windows.Media.SpeechRecognition.SpeechRecognitionResult speechRecognitionResult = commandArgs.Result;

                // Get the name of the voice command and the text spoken. 
                // See VoiceCommands.xml for supported voice commands.
                string voiceCommandName = speechRecognitionResult.RulePath[0];
                string textSpoken = speechRecognitionResult.Text;

                // commandMode indicates whether the command was entered using speech or text.
                // Apps should respect text mode by providing silent (text) feedback.
                string commandMode = this.SemanticInterpretation("commandMode", speechRecognitionResult);

                switch (voiceCommandName)
                {
                case "showTripToDestination":
                    // Access the value of {destination} in the voice command.
                    string destination = this.SemanticInterpretation("destination", speechRecognitionResult);

                    // Create a navigation command object to pass to the page. 
                    navigationCommand = new DashVoiceCommand(
                        voiceCommandName,
                        commandMode,
                        textSpoken,
                        destination);

                    // Set the page to navigate to for this voice command.
                    //TODO: save XAML page to navigate to
                    //navigationToPageType = typeof(View.TripDetails);
                    break;
                case "searchForTerm":
                    // Access the value of {destination} in the voice command.
                   // string term = this.SemanticInterpretation("destination", speechRecognitionResult);
                    string term = textSpoken.Substring("Dash search for ".Length);

                    // Create a navigation command object to pass to the page. 
                    navigationCommand = new DashVoiceCommand(
                        voiceCommandName,
                        commandMode,
                        textSpoken,
                        term);

                    // Set the page to navigate to for this voice command.
                    navigationToPageType = typeof(MainPage);
                    break;
                default:
                    // If we can't determine what page to launch, go to the default entry point.
                    navigationToPageType = typeof(MainPage);
                    break;
                }
            }
            // Protocol activation occurs when a card is clicked within Cortana (using a background task).
            else if (args.Kind == ActivationKind.Protocol)
            {
                // Extract the launch context. In this case, we're just using the destination from the phrase set (passed
                // along in the background task inside Cortana), which makes no attempt to be unique. A unique id or 
                // identifier is ideal for more complex scenarios. We let the destination page check if the 
                // destination trip still exists, and navigate back to the trip list if it doesn't.
                var commandArgs = args as ProtocolActivatedEventArgs;
                Windows.Foundation.WwwFormUrlDecoder decoder = new Windows.Foundation.WwwFormUrlDecoder(commandArgs.Uri.Query);
                var destination = decoder.GetFirstValueByName("LaunchContext");

                navigationCommand = new DashVoiceCommand(
                                        "protocolLaunch",
                                        "text",
                                        "destination",
                                        destination);

                navigationToPageType = typeof(MainPage);
            }
            else
            {
                // If we were launched via any other mechanism, fall back to the main page view.
                // Otherwise, we'll hang at a splash screen.

                navigationToPageType = typeof(MainPage);
            }



            Container = RegisterServices();
            // Repeat the same basic initialization as OnLaunched() above, taking into account whether
            // or not the app is already active.
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                    //App.NavigationService = new NavigationService(rootFrame);

                 rootFrame.NavigationFailed += OnNavigationFailed;

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }
            if (rootFrame.Content == null)
            {
                KeyStore.RegisterDocumentTypeRenderer(BackgroundShape.DocumentType, BackgroundShape.MakeView, null);
                KeyStore.RegisterDocumentTypeRenderer(RichTextBox.DocumentType, (doc, context) => RichTextBox.MakeView(doc, KeyStore.DataKey, context), RichTextBox.MakeRegionDocument);
                KeyStore.RegisterDocumentTypeRenderer(DiscussionBox.DocumentType, (doc, context) => DiscussionBox.MakeView(doc, KeyStore.DataKey, context), null);
                KeyStore.RegisterDocumentTypeRenderer(ExecuteHtmlOperatorBox.DocumentType, ExecuteHtmlOperatorBox.MakeView, null);
                KeyStore.RegisterDocumentTypeRenderer(DashConstants.TypeStore.ExtractSentencesDocumentType, ExtractSentencesOperatorBox.MakeView, null);
                KeyStore.RegisterDocumentTypeRenderer(DataBox.DocumentType, DataBox.MakeView, null);
                KeyStore.RegisterDocumentTypeRenderer(DashConstants.TypeStore.CollectionBoxType, CollectionBox.MakeView, null);
                KeyStore.RegisterDocumentTypeRenderer(TableBox.DocumentType, (doc, context) => TableBox.MakeView(doc, KeyStore.DataKey, context), null);
                KeyStore.RegisterDocumentTypeRenderer(DashConstants.TypeStore.FreeFormDocumentType, FreeFormDocument.MakeView, null);
                KeyStore.RegisterDocumentTypeRenderer(ImageBox.DocumentType, (doc, context) => ImageBox.MakeView(doc, KeyStore.DataKey, context), ImageBox.MakeRegionDocument);
                KeyStore.RegisterDocumentTypeRenderer(InkBox.DocumentType, InkBox.MakeView, null);
                KeyStore.RegisterDocumentTypeRenderer(KeyValueDocumentBox.DocumentType, KeyValueDocumentBox.MakeView, null);
                KeyStore.RegisterDocumentTypeRenderer(DashConstants.TypeStore.MeltOperatorBoxDocumentType, MeltOperatorBox.MakeView, null);
                KeyStore.RegisterDocumentTypeRenderer(DashConstants.TypeStore.OperatorBoxType, OperatorBox.MakeView, null);
                KeyStore.RegisterDocumentTypeRenderer(PdfBox.DocumentType, (doc, context) => PdfBox.MakeView(doc, KeyStore.DataKey, context), PdfBox.MakeRegionDocument);
                KeyStore.RegisterDocumentTypeRenderer(PreviewDocument.DocumentType, PreviewDocument.MakeView, null);
                KeyStore.RegisterDocumentTypeRenderer(DashConstants.TypeStore.QuizletOperatorType, QuizletOperatorBox.MakeView, null);
                KeyStore.RegisterDocumentTypeRenderer(DashConstants.TypeStore.SearchOperatorType, SearchOperatorBox.MakeView, null);
                KeyStore.RegisterDocumentTypeRenderer(TextingBox.DocumentType, (doc, context) => TextingBox.MakeView(doc, KeyStore.DataKey, context), null);
                KeyStore.RegisterDocumentTypeRenderer(MarkdownBox.DocumentType, MarkdownBox.MakeView, null);
                KeyStore.RegisterDocumentTypeRenderer(DishReplBox.DocumentType, DishReplBox.MakeView, null);
                KeyStore.RegisterDocumentTypeRenderer(DishScriptBox.DocumentType, DishScriptBox.MakeView, null);
                KeyStore.RegisterDocumentTypeRenderer(EditableScriptBox.DocumentType, EditableScriptBox.MakeView, null);
                KeyStore.RegisterDocumentTypeRenderer(WebBox.DocumentType, WebBox.MakeView, null);
                KeyStore.RegisterDocumentTypeRenderer(VideoBox.DocumentType, (doc, context) => VideoBox.MakeView(doc, KeyStore.DataKey, context), null);
                KeyStore.RegisterDocumentTypeRenderer(AudioBox.DocumentType, (doc, context) => AudioBox.MakeView(doc, KeyStore.DataKey, context), null);
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                //TODO Navigate to a prelogged in page if we can!
                // Since we're expecting to always show a details page, navigate even if 
                // a content frame is in place (unlike OnLaunched).
                // Navigate to either the main trip list page, or if a valid voice command
                // was provided, to the details page for that trip.
                rootFrame.Navigate(navigationToPageType, navigationCommand);
                ListContainedFieldFlag.Enabled = true; // works but slows things down A LOT!
            }
            else
            {
                MainPage.Instance.handleVoidCommand(navigationCommand);
            }

            ApplicationView.PreferredLaunchViewSize = new Size(1800, 1000);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

            // Ensure the current window is active
            Window.Current.Activate();
        }

        /// <summary>
        /// Returns the semantic interpretation of a speech result. 
        /// Returns null if there is no interpretation for that key.
        /// </summary>
        /// <param name="interpretationKey">The interpretation key.</param>
        /// <param name="speechRecognitionResult">The speech recognition result to get the semantic interpretation from.</param>
        /// <returns></returns>
        private string SemanticInterpretation(string interpretationKey, SpeechRecognitionResult speechRecognitionResult)
        {
            return speechRecognitionResult.SemanticInterpretation.Properties[interpretationKey].FirstOrDefault();
        }


        protected override void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            // connection established from the fulltrust process
            if (args.TaskInstance.TriggerDetails is AppServiceTriggerDetails)
            {
                AppServiceDeferral = args.TaskInstance.GetDeferral();
                args.TaskInstance.Canceled += OnTaskCanceled;

                if (args.TaskInstance.TriggerDetails is AppServiceTriggerDetails details)
                {
                    Connection = details.AppServiceConnection;
                    AppServiceConnected?.Invoke();
                }
            }
        }

        private void OnTaskCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            if (AppServiceDeferral != null)
            {
                AppServiceDeferral.Complete();
            }
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

            // close the connection to the 

            Task.Run(async () =>
            {
                await Container.GetRequiredService<IModelEndpoint<FieldModel>>().Close();
            });

            deferral.Complete();
        }


        /// <summary>
        /// called when the application resumes after having suspended
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="o"></param>
        private void OnResuming(object sender, object o)
        {
        }

    }
}
