
using System;
using System.Diagnostics;
using Windows.Foundation;
using Windows.Media.SpeechRecognition;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Views.Collection
{
    public sealed partial class MarqueeInfo : UserControl
    {
        public MarqueeInfo()
        {
            this.InitializeComponent();
        }

        private void Collection_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var collection = this.GetFirstAncestorOfType<CollectionFreeformBase>();
            collection?.TriggerActionFromSelection(VirtualKey.C, true);
            e.Handled = true;
        }

        private static uint HResultPrivacyStatementDeclined = 0x80045509;

        private async void TestSpeech()
        {
            try
            {
                // Create an instance of SpeechRecognizer.
                var speechRecognizer = new Windows.Media.SpeechRecognition.SpeechRecognizer();

                // Listen for audio input issues.
                //speechRecognizer.RecognitionQualityDegrading += speechRecognizer_RecognitionQualityDegrading;

                // Add a web search grammar to the recognizer.
                //var webSearchGrammar = new Windows.Media.SpeechRecognition.SpeechRecognitionTopicConstraint(Windows.Media.SpeechRecognition.SpeechRecognitionScenario.WebSearch, "webSearch");

                speechRecognizer.UIOptions.AudiblePrompt = "Say what you want to search for...";
                speechRecognizer.UIOptions.ExampleText = @"Ex. 'weather for London'";
                //speechRecognizer.Constraints.Add(webSearchGrammar);
                // speechRecognizer.Constraints.Add(new SpeechRecognitionVoiceCommandDefinitionConstraint());
                //TODO: look into creating a SpeechRecognitionVoiceCommandDefinitionConstraint for speech commands

                // Compile the dictation grammar by default.
                await speechRecognizer.CompileConstraintsAsync();

                // Start recognition.
                Windows.Media.SpeechRecognition.SpeechRecognitionResult speechRecognitionResult = await speechRecognizer.RecognizeWithUIAsync();

                // Do something with the recognition result.
                var messageDialog = new Windows.UI.Popups.MessageDialog(speechRecognitionResult.Text, "Text spoken");
                await messageDialog.ShowAsync();
            }
            catch (Exception exception)
            {
                // Handle the speech privacy policy error.
                if ((uint)exception.HResult == HResultPrivacyStatementDeclined)
                {
                    Debug.WriteLine("The privacy statement was declined." +
                                           "Go to Settings -> Privacy -> Speech, inking and typing, and ensure you" +
                                           "have viewed the privacy policy, and 'Get To Know You' is enabled.");
                    // Open the privacy/speech, inking, and typing settings page.
                    await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-accounts"));
                }
                else
                {
                    var messageDialog = new Windows.UI.Popups.MessageDialog(exception.Message, "Exception");
                    await messageDialog.ShowAsync();
                }
            }
        }

        private async void Group_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            //TestSpeech();

            var collection = this.GetFirstAncestorOfType<CollectionFreeformBase>();
            collection?.TriggerActionFromSelection(VirtualKey.G, true);
            e.Handled = true;
        }

        private void Collection_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;
        }
    }
}
