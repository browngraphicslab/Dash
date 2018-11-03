﻿using System;
using System.Diagnostics;
using System.Linq;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Media.SpeechSynthesis;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Dash.FontIcons;
using Dash.Views.Collection;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionView : UserControl
    {
        private CollectionViewModel _lastViewModel = null;
        public int                 MaxZ        { get; set; }
        public ICollectionView     CurrentView { get; set; }
        public UserControl         UserControl => this;
        public CollectionViewModel ViewModel   => DataContext as CollectionViewModel;
        public event Action<object, RoutedEventArgs> CurrentViewLoaded;

        public CollectionView()
        {
            Loaded   += CollectionView_Loaded;
            Unloaded += CollectionView_Unloaded;
            InitializeComponent();

            InitializeView(CollectionViewType.Freeform);
            DragLeave += (sender, e) => ViewModel.CollectionViewOnDragLeave(sender, e);
            DragEnter += (sender, e) => ViewModel.CollectionViewOnDragEnter(sender, e);
            DragOver  += (sender, e) => ViewModel.CollectionViewOnDragOver(sender, e);
            Drop      += (sender, e) => ViewModel.CollectionViewOnDrop(sender, e);
            DataContextChanged += (ss, ee) =>
            {
                if (ee.NewValue != _lastViewModel)
                {
                    ViewModel?.ContainerDocument.RemoveFieldUpdatedListener(KeyStore.CollectionViewTypeKey, ViewTypeHandler);
                    ViewModel?.ContainerDocument.AddFieldUpdatedListener(KeyStore.CollectionViewTypeKey, ViewTypeHandler);
                    InitializeView(ViewModel?.ViewType ?? CurrentView.ViewType);
                    _lastViewModel = ViewModel;
                }
            };

            xOuterGrid.PointerPressed += OnPointerPressed;
            var color = xOuterGrid.Background;
        }
        ~CollectionView()
        {
            //Debug.WriteLine("Finalizing CollectionView");
        }
        /// <summary>
        /// pan/zooms the document so that all of its contents are visible.  
        /// This only applies of the CollectionViewType is Freeform/Standard, and the CollectionFitToParent field is true
        /// </summary>
        public void FitContents()
        {
            if (!LocalSqliteEndpoint.SuspendTimer &&
                ViewModel.ContainerDocument.GetFitToParent() && CurrentView is CollectionFreeformView freeform)
            {
                var parSize = ViewModel.ContainerDocument.GetActualSize() ?? new Point();
                var ar = freeform.GetItemsControl().ItemsPanelRoot?.Children.OfType<ContentPresenter>().Select((cp) => cp.GetFirstDescendantOfType<DocumentView>())
                    .Aggregate(Rect.Empty, (rect, dv) => { rect.Union(dv.RenderTransform.TransformBounds(new Rect(new Point(), new Point(dv.ActualWidth, dv.ActualHeight)))); return rect; });

                if (ar is Rect r && !r.IsEmpty && r.Width != 0 && r.Height != 0)
                {
                    var rect = new Rect(new Point(), new Point(parSize.X, parSize.Y));
                    var scaleWidth = r.Width / r.Height > rect.Width / rect.Height;
                    var scaleAmt = scaleWidth ? rect.Width / r.Width : rect.Height / r.Height;
                    var trans = new Point(-r.Left * scaleAmt, -r.Top * scaleAmt);
                    if (scaleAmt > 0)
                    {
                        ViewModel.TransformGroup = new TransformGroupData(trans, new Point(scaleAmt, scaleAmt));
                    }
                }
            }
        }
        private void ViewTypeHandler(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
        {
            InitializeView(Enum.Parse<CollectionViewType>(args.NewValue.ToString()));
        }

        /// <summary>
        /// Begins panning events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnPointerPressed(object sender, PointerRoutedEventArgs args)
        {
            var docview = this.GetFirstAncestorOfType<DocumentView>();
            //if (args.Pointer.PointerDeviceType == PointerDeviceType.Touch)
            //{
            //    if (!SelectionManager.IsSelected(docview))
            //        SelectionManager.Select(docview, false);
            //    SelectionManager.TryInitiateDragDrop(docview, args, null);
            //}
            if (args.GetCurrentPoint(this).Properties.IsRightButtonPressed ) 
            {
                docview.ManipulationMode = ManipulationModes.All;
                CurrentView.UserControl.ManipulationMode = SelectionManager.IsSelected(docview) ||
                this.GetFirstAncestorOfType<DocumentView>().IsTopLevel() ?
                    ManipulationModes.All : ManipulationModes.None;
                    args.Handled = true;
            } else
            {
                docview.ManipulationMode = ManipulationModes.None;
            }
        }
        
        private void CollectionView_Unloaded(object sender, RoutedEventArgs e)
        {
            //Debug.WriteLine($"CollectionView {id} unloaded {--count}");
            _lastViewModel?.Loaded(false);
            _lastViewModel?.ContainerDocument.RemoveFieldUpdatedListener(KeyStore.CollectionViewTypeKey, ViewTypeHandler);
        }
    
        private void CollectionView_Loaded(object s, RoutedEventArgs args)
        {
            //TODO This causes a memory leak currently
            //var ParentDocumentView = this.GetFirstAncestorOfType<DocumentView>();
            //ParentDocumentView.DocumentSelected   -= ParentDocumentView_DocumentSelected;
            //ParentDocumentView.DocumentSelected   += ParentDocumentView_DocumentSelected;
            //ParentDocumentView.DocumentDeselected -= ParentDocumentView_DocumentDeselected;
            //ParentDocumentView.DocumentDeselected += ParentDocumentView_DocumentDeselected;

            //Debug.WriteLine($"CollectionView {id} loaded : {++count}");
            ViewModel?.Loaded(true);
        }

        private void ParentDocumentView_DocumentDeselected(DocumentView obj)
        {
            CurrentView.OnDocumentSelected(false);
        }

        private void ParentDocumentView_DocumentSelected(DocumentView obj)
        {
            CurrentView.OnDocumentSelected(true);
        }

        private void InitializeView(CollectionViewType viewType)
        {
            if (CurrentView?.UserControl != null)
                CurrentView.UserControl.Loaded -= CurrentView_Loaded;
            if (CurrentView?.ViewType == viewType)
                return;
            var initialViewType = CurrentView?.ViewType;
            switch (viewType)
            {
                case CollectionViewType.Icon:     if (CurrentView != null)
                                                  {
                                                      ViewModel.ContainerDocument.SetField<TextController>  (KeyStore.CollectionOpenViewTypeKey, CurrentView.ViewType.ToString(), true);
                                                      ViewModel.ContainerDocument.SetField<NumberController>(KeyStore.CollectionOpenWidthKey,    ViewModel.ContainerDocument.GetWidth(), true);
                                                      ViewModel.ContainerDocument.SetField<NumberController>(KeyStore.CollectionOpenHeightKey,   ViewModel.ContainerDocument.GetHeight(), true);
                                                  }
                                                  CurrentView = new CollectionIconView(); break;
                case CollectionViewType.Freeform: CurrentView = new CollectionFreeformView(); break;
                case CollectionViewType.Stacking: CurrentView = new CollectionStackView(); break;
                case CollectionViewType.Grid:     CurrentView = new CollectionGridView(); break;
                case CollectionViewType.Page:     CurrentView = new CollectionPageView();  break;
                case CollectionViewType.DB:       CurrentView = new CollectionDBView(); break;
                case CollectionViewType.Schema:   CurrentView = new CollectionDBSchemaView(); break;
                case CollectionViewType.TreeView: CurrentView = new CollectionTreeView();  break;
                case CollectionViewType.Timeline: CurrentView = new CollectionTimelineView(); break;
                case CollectionViewType.Graph:    CurrentView = new CollectionGraphView(); break;
                default: throw new NotImplementedException("You need to add support for your collectionview here");
            }
            CurrentView.UserControl.Loaded += CurrentView_Loaded;

            if (initialViewType == CollectionViewType.Icon && CurrentView.ViewType != CollectionViewType.Icon)
            {
                ViewModel.ContainerDocument.SetWidth (ViewModel.ContainerDocument.GetField<NumberController>(KeyStore.CollectionOpenWidthKey)?.Data ?? 300) ;
                ViewModel.ContainerDocument.SetHeight(ViewModel.ContainerDocument.GetField<NumberController>(KeyStore.CollectionOpenHeightKey)?.Data ?? 300);
            }

            xContentControl.Content = CurrentView;
        }

        private void CurrentView_Loaded(object sender, RoutedEventArgs e)
        {
            CurrentViewLoaded?.Invoke(sender, e);
        }

      
        #region Menu
        public void SetupContextMenu(MenuFlyout contextMenu)
        {
            // add another horizontal separator
            contextMenu.Items.Add(new MenuFlyoutSeparator());
            contextMenu.Items.Add(new MenuFlyoutItem()
            {
                Text = "Collection",
                FontWeight = Windows.UI.Text.FontWeights.Bold
            });
            contextMenu.Items.Add(new MenuFlyoutSeparator());

            contextMenu.Items.Add(new MenuFlyoutItem()
            {
                Text = "Make Speech Document",
                Icon = new FontIcons.FontAwesome { Icon = FontAwesomeIcon.Microphone }
            });
            (contextMenu.Items.Last() as MenuFlyoutItem).Click += MakeSpeechDoc;

            var unfrozen = ViewModel.DocumentViewModels.FirstOrDefault()?.AreContentsHitTestVisible == true;
            contextMenu.Items.Add(new MenuFlyoutItem()
            {
                Text = unfrozen ? "Freeze Contents" : "Unfreeze Contents",
                Icon = new FontIcons.FontAwesome { Icon = unfrozen ? FontAwesomeIcon.Lock : FontAwesomeIcon.Unlock }
            });
            (contextMenu.Items.Last() as MenuFlyoutItem).Click += (ss, ee) => FreezeContents_OnClick(!unfrozen);

            contextMenu.Items.Add(new MenuFlyoutItem()
            {
                Text = "Convert to Template",
                Icon = new FontIcons.FontAwesome { Icon = FontAwesomeIcon.WindowMinimize }
            });
            (contextMenu.Items.Last() as MenuFlyoutItem).Click += (ss, ee) => Templatize_OnClick();
            contextMenu.Items.Add(new MenuFlyoutItem()
            {
                Text = "Iconify",
                Icon = new FontIcons.FontAwesome { Icon = FontAwesomeIcon.WindowMinimize }
            });
            (contextMenu.Items.Last() as MenuFlyoutItem).Click += (ss, ee) => Iconify_OnClick();
            contextMenu.Items.Add(new MenuFlyoutItem()
            {
                Text = "Buttonize",
                Icon = new FontIcons.FontAwesome { Icon = FontAwesomeIcon.WindowMinimize }
            });
            (contextMenu.Items.Last() as MenuFlyoutItem).Click += (ss, ee) => Buttonize_OnClick();

            // add the outer SubItem to "View collection as" to the context menu, and then add all the different view options to the submenu 
            contextMenu.Items.Add(new MenuFlyoutSubItem()
            {
                Text = "View As",
                Icon = new FontIcons.FontAwesome { Icon = FontAwesomeIcon.Eye }
            });
            foreach (var n in Enum.GetValues(typeof(CollectionViewType)).Cast<CollectionViewType>())
            {
                (contextMenu.Items.Last() as MenuFlyoutSubItem).Items.Add(new MenuFlyoutItem() { Text = n.ToString() });
                ((contextMenu.Items.Last() as MenuFlyoutSubItem).Items.Last() as MenuFlyoutItem).Click += (ss, ee) => {
                    using (UndoManager.GetBatchHandle())
                    {
                        ViewModel.ViewType = n;
                    }
                };
            }
            CurrentView?.SetupContextMenu(contextMenu);
            

            // add the outer SubItem to "View collection as" to the context menu, and then add all the different view options to the submenu 
            contextMenu.Items.Add(new MenuFlyoutItem()
            {
                Text = ViewModel.ContainerDocument.GetFitToParent() ? "Make Unbounded" : "Fit to Parent",
                Icon = new FontIcons.FontAwesome { Icon = FontAwesomeIcon.WindowMaximize }
            });
            (contextMenu.Items.Last() as MenuFlyoutItem).Click += FitToParent_OnClick;

            // add a horizontal separator in context menu
            contextMenu.Items.Add(new MenuFlyoutSeparator());
            contextMenu.Items.Add(new MenuFlyoutItem()
            {
                Text = "Scripting",
                FontWeight = Windows.UI.Text.FontWeights.Bold
            });
            contextMenu.Items.Add(new MenuFlyoutSeparator());

            // add the item to create a repl
            contextMenu.Items.Add(new MenuFlyoutItem()
            {
                Text = "Create Scripting REPL",
                Icon = new FontIcons.FontAwesome { Icon = FontAwesomeIcon.Code }
            });
            (contextMenu.Items.Last() as MenuFlyoutItem).Click += ReplFlyout_OnClick;

            // add the item to create a scripting view
            contextMenu.Items.Add(new MenuFlyoutItem()
            {
                Text = "Create Script Editor",
                Icon = new FontIcons.FontAwesome { Icon = FontAwesomeIcon.WindowMaximize }
            });
            (contextMenu.Items.Last() as MenuFlyoutItem).Click += ScriptEdit_OnClick;
        }

        private static uint HResultPrivacyStatementDeclined = 0x80045509;
        private async void MakeSpeechDoc(object sender, RoutedEventArgs e)
        {
            var menuflyout = (sender as MenuFlyoutItem).GetFirstAncestorOfType<FrameworkElement>();
            var topPoint = Util.PointTransformFromVisual(new Point(), menuflyout);
            var where = Util.GetCollectionFreeFormPoint(CurrentView as CollectionFreeformBase, topPoint);
            //TODO: offset where by menu height

            try
            {
                // Create an instance of SpeechRecognizer.
                var speechRecognizer = new Windows.Media.SpeechRecognition.SpeechRecognizer();

                // Listen for audio input issues.
                //speechRecognizer.RecognitionQualityDegrading += speechRecognizer_RecognitionQualityDegrading;

                // Add a web search grammar to the recognizer.
                //var webSearchGrammar = new Windows.Media.SpeechRecognition.SpeechRecognitionTopicConstraint(Windows.Media.SpeechRecognition.SpeechRecognitionScenario.WebSearch, "webSearch");

                speechRecognizer.UIOptions.AudiblePrompt = "Say what you want to save...";
                speechRecognizer.UIOptions.ExampleText = @"Ex. 'this document explains...'";
                //speechRecognizer.Constraints.Add(webSearchGrammar);
                // speechRecognizer.Constraints.Add(new SpeechRecognitionVoiceCommandDefinitionConstraint());
                //TODO: look into creating a SpeechRecognitionVoiceCommandDefinitionConstraint for speech commands

                // Compile the dictation grammar by default.
                await speechRecognizer.CompileConstraintsAsync();

                // Start recognition.
                Windows.Media.SpeechRecognition.SpeechRecognitionResult speechRecognitionResult = await speechRecognizer.RecognizeWithUIAsync();

                // Make doc out of result
                var textDoc = new RichTextNote(text: "text", where: where, size: new Size(300, double.NaN)).Document;
                Actions.DisplayDocument(this.ViewModel, textDoc, where);
                
                //generate audio for this text
                // The object for controlling the speech synthesis engine (voice).
                var synth = new Windows.Media.SpeechSynthesis.SpeechSynthesizer();
                // Generate the audio stream from plain text.
                SpeechSynthesisStream stream = await synth.SynthesizeTextToStreamAsync("text");
                var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                StorageFile file = await localFolder.CreateFileAsync("audio.mp3", CreationCollisionOption.GenerateUniqueName);
                using (var reader = new DataReader(stream))
                {
                    await reader.LoadAsync((uint)stream.Size);
                    IBuffer buffer = reader.ReadBuffer((uint)stream.Size);
                    await FileIO.WriteBufferAsync(file, buffer);
                }
                
                var audDoc = new AudioNote(new Uri(file.Path)).Document;
                Actions.DisplayDocument(this.ViewModel, audDoc, new Point(where.X, where.Y + 40));
            }
            catch (Exception exception)
            {
                // Handle the speech privacy policy error.
                if ((uint)exception.HResult == HResultPrivacyStatementDeclined)
                {
                    var messageDialog = new Windows.UI.Popups.MessageDialog("The privacy statement was declined. " +
                                           "Go to Settings -> Privacy -> Speech, inking and typing, and ensure you " +
                                           "have viewed the privacy policy, and 'Get To Know You' is enabled.");
                    await messageDialog.ShowAsync();
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

        private void ScriptEdit_OnClick(object sender, RoutedEventArgs e)
        {
            var menuflyout = (sender as MenuFlyoutItem).GetFirstAncestorOfType<FrameworkElement>();
            var topPoint = Util.PointTransformFromVisual(new Point(), menuflyout);
            var where = Util.GetCollectionFreeFormPoint(CurrentView as CollectionFreeformBase, topPoint);
            var note = new DishScriptBox(@where.X, @where.Y).Document;
            Actions.DisplayDocument(ViewModel, note, @where);
        }
        private void ReplFlyout_OnClick(object sender, RoutedEventArgs e)
        {
            var menuflyout = (sender as MenuFlyoutItem).GetFirstAncestorOfType<FrameworkElement>();
            var topPoint = Util.PointTransformFromVisual(new Point(), menuflyout);
            Point where = Util.GetCollectionFreeFormPoint(CurrentView as CollectionFreeformBase, topPoint);
            DocumentController note = new DishReplBox(@where.X, @where.Y, 300, 400).Document;
            Actions.DisplayDocument(ViewModel, note, @where);
        }
        private void FitToParent_OnClick(object sender, RoutedEventArgs e)
        {
            ViewModel.ContainerDocument.SetFitToParent(!ViewModel.ContainerDocument.GetFitToParent());
            if (ViewModel.ContainerDocument.GetFitToParent())
                FitContents();
        }
        private void Templatize_OnClick()
        {
            CollectionViewModel.ConvertToTemplate(ViewModel.ContainerDocument, ViewModel.ContainerDocument);
        }
        private void Iconify_OnClick() 
        {
            ViewModel.ViewType = CollectionViewType.Icon;
            ViewModel.ContainerDocument.SetWidth(double.NaN);
            ViewModel.ContainerDocument.SetHeight(double.NaN);
        }
        private void FreezeContents_OnClick(bool unfrozen)
        {
            foreach (var child in ViewModel.DocumentViewModels)
            {
                child.AreContentsHitTestVisible = unfrozen;
            }
        }
        private void Buttonize_OnClick()
        {
            var newdoc = new RichTextNote(ViewModel.ContainerDocument.Title,
                ViewModel.ContainerDocument.GetPosition() ?? new Point()).Document;
            newdoc.Link(ViewModel.ContainerDocument, LinkBehavior.Follow, "Button");
            newdoc.SetIsButton(true);
            var thisView = this.GetFirstAncestorOfType<DocumentView>();
            thisView.ParentCollection?.ViewModel.AddDocument(newdoc);
            thisView.DeleteDocument();
        }

        #endregion
    }
}
