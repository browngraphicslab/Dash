using DashShared;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Devices.HumanInterfaceDevice;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;
using Dash.Controllers;
using static Dash.NoteDocuments;
using Dash.Controllers.Operators;
using Dash.Views;
using Visibility = Windows.UI.Xaml.Visibility;
using Windows.System;
using Windows.UI.Core;
using DashShared.Models;
using Flurl.Util;
using NewControls.Geometry;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionFreeformView : SelectionElement, ICollectionView
    {

        #region ScalingVariables    

        public double CanvasScale { get; set; } = 1;
        public BaseCollectionViewModel ViewModel { get; private set; }
        public const float MaxScale = 4;
        public const float MinScale = 0.25f;

        #endregion

        #region LinkingVariables

        public bool CanLink = false;
        public PointerRoutedEventArgs PointerArgs;
        private HashSet<uint> _currentPointers = new HashSet<uint>();
        private IOReference _currReference;
        private Path _connectionLine;
        private BezierConverter _converter;
        private MultiBinding<PathFigureCollection> _lineBinding;
        public Dictionary<FieldReference, Path> RefToLine = new Dictionary<FieldReference, Path>();
        public Dictionary<Path, BezierConverter> LineToConverter = new Dictionary<Path, BezierConverter>();
        private Dictionary<FieldReference, Path> _linesToBeDeleted = new Dictionary<FieldReference, Path>();
        private Canvas itemsPanelCanvas;

        #endregion


        public ManipulationControls ManipulationControls;

        public bool TagMode { get; set; }
        public KeyController TagKey { get; set; }

        #region Background Translation Variables
        private CanvasBitmap _bgImage;
        private bool _resourcesLoaded;
        private CanvasImageBrush _bgBrush;
        private readonly Uri _backgroundPath = new Uri("ms-appx:///Assets/transparent_grid_tilable.png");
        private const double NumberOfBackgroundRows = 2; // THIS IS A MAGIC NUMBER AND SHOULD CHANGE IF YOU CHANGE THE BACKGROUND IMAGE
        private const float BackgroundOpacity = 1.0f;

        #endregion

        public delegate void OnDocumentViewLoadedHandler(CollectionFreeformView sender, DocumentView documentView);
        public event OnDocumentViewLoadedHandler OnDocumentViewLoaded;

        private List<Tuple<FieldReference, DocumentFieldReference>> _linksToRetry;
        public Dictionary<Path, Tuple<KeyController, KeyController>> LineToElementKeysDictionary = new Dictionary<Path, Tuple<KeyController, KeyController>>();

        public CollectionFreeformView()
        {

            InitializeComponent();
            Loaded += Freeform_Loaded;
            Unloaded += Freeform_Unloaded;
            DataContextChanged += OnDataContextChanged;
            DragLeave += Collection_DragLeave;
            Window.Current.CoreWindow.KeyUp += CoreWindowOnKeyUp;
        }

        private void CoreWindowOnKeyUp(CoreWindow coreWindow, KeyEventArgs args)
        {
            if (args.VirtualKey == VirtualKey.Back)
            {
                ViewModel.RemoveDocuments(ViewModel.SelectionGroup.Select(vm => vm.DocumentController).ToList());
            }
        }

        public List<DocumentView> DocumentViews { get; private set; } = new List<DocumentView>();

        public IOReference GetCurrentReference()
        {
            return _currReference;
        }

        #region DataContext and Events

        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            var vm = DataContext as BaseCollectionViewModel;

            if (vm != null)
            {
                // remove old events
                if (ViewModel?.DocumentViewModels != null)
                    ViewModel.DocumentViewModels.CollectionChanged -= DocumentViewModels_CollectionChanged;

                // add new events
                ViewModel = vm;
                ViewModel.SetSelected(this, IsSelected);
                ViewModel.DocumentViewModels.CollectionChanged += DocumentViewModels_CollectionChanged;
            }
        }

        private void DocumentViewModels_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                DocumentViews = IterateDocumentViews().ToList();
            }

            IEnumerable<DocumentView> IterateDocumentViews()
            {
                var parentDoc = this.GetFirstAncestorOfType<DocumentView>();
                foreach (var doc in this.GetDescendantsOfType<DocumentView>())
                    if (doc.GetFirstAncestorOfType<DocumentView>()?.Equals(parentDoc) == true)
                        yield return doc;
            }
        }

        private void Freeform_Unloaded(object sender, RoutedEventArgs e)
        {
            ManipulationControls?.Dispose();
        }

        private void Freeform_Loaded(object sender, RoutedEventArgs e)
        {
            itemsPanelCanvas = xItemsControl.ItemsPanelRoot as Canvas;

            ManipulationControls = new ManipulationControls(this, doesRespondToManipulationDelta: true, doesRespondToPointerWheel: true);
            ManipulationControls.OnManipulatorTranslatedOrScaled += ManipulationControls_OnManipulatorTranslated;

            var parentGrid = this.GetFirstAncestorOfType<Grid>();
            parentGrid.PointerMoved += FreeformGrid_OnPointerMoved;
            parentGrid.PointerReleased += FreeformGrid_OnPointerReleased;

            xOuterGrid.PointerPressed += OnPointerPressed;
            xOuterGrid.PointerMoved += OnPointerMoved;
            xOuterGrid.PointerReleased += OnPointerReleased;

            if (InkController != null)
            {
                MakeInkCanvas();
            }

            LoadLines();
            fitFreeFormChildrenToTheirLayouts();
        }

        void fitFreeFormChildrenToTheirLayouts()
        {
            var parentOfFreeFormChild = VisualTreeHelperExtensions.GetFirstAncestorOfType<DocumentView>(this);
            //ManipulationControls?.FitToParent();
        }

        #endregion

        #region DraggingLinesAround


        /// <summary>
        /// Loads all of the links that should be shown in the collection by iterating through the list of user created links on each 
        /// document in the collection. The UserLinksKey corresponds to a list of keyIDs of fields on that document that reference fields 
        /// on other documents and were created by the user. We then check if the corresponding referenced document is in the collection 
        /// for each linked field, and if it is, we construct the link using 
        /// </summary>
        public void LoadLines()
        {
            _linksToRetry = new List<Tuple<FieldReference, DocumentFieldReference>>();
            foreach (var docVM in ViewModel.DocumentViewModels)
            {
                var doc = docVM.DocumentController;
                if (doc.GetField(KeyStore.UserLinksKey, true) is ListController<KeyController> linksListFMC)
                {
                    foreach (var key in linksListFMC.TypedData)
                    {

                        var field = doc.GetField(key);
                        if (field != null)
                        {
                            AddLineFromData((field as ReferenceController)?.GetFieldReference(), new DocumentFieldReference(doc.Id, key));

                        }
                    }
                }
            }

            //Hack to load links between operators; if loading fails the first time because the operators havent finished loading,
            //we wait 500 milliseconds and try again.
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Tick += (sender, o) =>
            {
                foreach (var linkData in _linksToRetry)
                {
                    var startRef = linkData.Item1;
                    var endRef = linkData.Item2;
                    AddLineFromData(startRef, endRef);
                }
                timer.Stop();
            };
            timer.Start();
        }

        public void AddLineFromData(FieldReference startReference, DocumentFieldReference endReference)
        {
            if (RefToLine.ContainsKey(startReference) &&
                itemsPanelCanvas.Children.Contains(RefToLine[startReference])) return;
            DocumentController referencingDoc = endReference.GetDocumentController(null);
            KeyController referencingFieldKey = endReference.FieldKey;
            var docId = startReference.GetDocumentId();
            var referencedFieldKey = startReference.FieldKey;
            //var fmController = startReference.DereferenceToRoot(null);
            DocumentController referencedDoc = null;
            foreach (var document in ViewModel.DocumentViewModels.Select(vm => vm.DocumentController))
            {
                if (document.GetId() == docId)
                {
                    referencedDoc = document;
                    break;
                }
            }
            if (referencedDoc == null) return;
            //TypeInfo type = 0;
            //if (fmController == null)
            //{
            //    var op = referencedDoc.GetField(KeyStore.OperatorKey) as OperatorController;
            //    type = op.Outputs[startReference.FieldKey];
            //}
            //else
            //{
            //    type = fmController.TypeInfo;
            //}
            var docView1 = GetDocView(referencedDoc);

            if (!docView1.ViewModel.KeysToFrameworkElements.ContainsKey(referencedFieldKey))
            {
                _linksToRetry.Add(new Tuple<FieldReference, DocumentFieldReference>(startReference, endReference));
                return;
            }

            var frameworkElement1 = docView1.ViewModel.KeysToFrameworkElements[referencedFieldKey];
            var docView2 = GetDocView(referencingDoc);
            if (!docView2.ViewModel.KeysToFrameworkElements.ContainsKey(referencingFieldKey))
            {
                _linksToRetry.Add(new Tuple<FieldReference, DocumentFieldReference>(startReference, endReference));
                return;
            }
            var frameworkElement2 = docView2.ViewModel.KeysToFrameworkElements[referencingFieldKey];
            var document2 = docView2.ViewModel.DocumentController;
            var inputRef = new DocumentFieldReference(document2.GetId(), referencingFieldKey);

            referencedDoc.IsConnected = true;
            document2.IsConnected = true;
            //IOReference outputtingReference = new IOReference(referencedFieldKey, reference, true, fieldTypeInfo, null, frameworkElement1, docView1);
            //IOReference inputtingReference = new IOReference(referencingFieldKey, new DocumentFieldReference(document2.GetId(), referencingFieldKey), false, fieldTypeInfo, null, frameworkElement2, docView2);


            ////Programmatically start the connection line
            //StartConnectionLine(outputtingReference, Util.PointTransformFromVisual(new Point(frameworkElement1.ActualWidth/2,frameworkElement1.ActualHeight/2), frameworkElement1, itemsPanelCanvas));
            ////Set the current reference to the outputting reference (the IOReference coming from the referenced field)

            //_currReference = outputtingReference;
            ////End the drag, passing in the IOReference that would be generated by the recieving field if the user actually dropped a link on it.
            //EndDrag(inputtingReference, false, true);
            var link = new UserCreatedLink
            {
                IsHitTestVisible = false,
                StrokeThickness = 5,
                IsHoldingEnabled = false,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                CompositeMode = ElementCompositeMode.SourceOver,
                referencingDocument = referencingDoc,
                referencingKey = referencingFieldKey,
                referencedDocument = referencedDoc,
                referencedKey = referencedFieldKey
            };
            Canvas.SetZIndex(link, -1);
            var converter = new BezierConverter(frameworkElement1, frameworkElement2, itemsPanelCanvas);
            converter.Pos2 = new Point(0, 0);
            _lineBinding = new MultiBinding<PathFigureCollection>(converter, null);
            _lineBinding.AddBinding(docView1, RenderTransformProperty);
            _lineBinding.AddBinding(docView1, WidthProperty);
            _lineBinding.AddBinding(docView1, HeightProperty);
            Binding lineBinding = new Binding
            {
                Source = _lineBinding,
                Path = new PropertyPath("Property")
            };
            PathGeometry pathGeo = new PathGeometry();
            BindingOperations.SetBinding(pathGeo, PathGeometry.FiguresProperty, lineBinding);
            link.Data = pathGeo;
            itemsPanelCanvas.Children.Add(link);
            //binding line position 
            _lineBinding.AddBinding(docView2, RenderTransformProperty);
            _lineBinding.AddBinding(docView2, WidthProperty);
            _lineBinding.AddBinding(docView2, HeightProperty);
            CheckLinePresence(inputRef);
            RefToLine.Add(startReference, link);

            if (!LineToConverter.ContainsKey(link)) LineToConverter.Add(link, converter);
            converter.OnPathUpdated += UpdateGradient;
            converter.setGradientAngle();
            link.Stroke = converter.GradientBrush;

            AddLineToKeysEntry(converter, link);
        }

        private void AddLineToKeysEntry(BezierConverter converter, Path link)
        {
            var docView1 = converter.Element1.GetFirstAncestorOfType<DocumentView>();
            var docView2 = converter.Element2.GetFirstAncestorOfType<DocumentView>();
            var frameworkKey1 = docView1.ViewModel.KeysToFrameworkElements.Keys.FirstOrDefault(key => docView1.ViewModel
                .KeysToFrameworkElements[key].Equals(converter.Element1));
            var frameworkKey2 = docView2.ViewModel.KeysToFrameworkElements.Keys.FirstOrDefault(key => docView2.ViewModel
                .KeysToFrameworkElements[key].Equals(converter.Element2));
            LineToElementKeysDictionary.Add(link, new Tuple<KeyController, KeyController>(frameworkKey1, frameworkKey2));
        }

        public DocumentView GetDocView(DocumentController doc)
        {
            return DocumentViews.FirstOrDefault(view => view.ViewModel.DocumentController.Equals(doc));
        }

        public void DeleteLine(FieldReference reff, Path line)
        {
            //Remove references to user created links from the UserLinks field of the document into which this link is inputting.
            var converter = LineToConverter[line];
            var view2 = converter.Element2.GetFirstAncestorOfType<DocumentView>();
            var view1 = converter.Element1.GetFirstAncestorOfType<DocumentView>();
            var doc2 = view2.ViewModel.DocumentController;
            var doc1 = view1.ViewModel.DocumentController;
            var linksList =
                doc2.GetField(KeyStore.UserLinksKey) as ListController<TextController>;
            if (linksList == null || linksList.TypedData.Count == 0)
            {
                linksList = doc1.GetField(KeyStore.UserLinksKey) as ListController<TextController>;
            }

            if (linksList != null)
            {
                var field =
                    view2.ViewModel.KeysToFrameworkElements.FirstOrDefault(kvp => kvp.Value.Equals(converter.Element2));
                if (field.Key != null)
                {
                    var keyId = field.Key.Id;
                    var textFMC = linksList.TypedData.FirstOrDefault();
                    if (textFMC != null) linksList.Remove(textFMC);
                }
            }

            //Remove the line visually and remove all references to it
            itemsPanelCanvas.Children.Remove(line);
            RefToLine.Remove(reff);
            LineToConverter[line].OnPathUpdated -= UpdateGradient;
            LineToConverter.Remove(line);
            LineToElementKeysDictionary.Remove(line);
        }

        /// <summary>
        /// Called when documentview is deleted; delete all connections coming from it as well  
        /// </summary>
        public void DeleteConnections(DocumentView docView)
        {
            foreach (var pair in LineToConverter.ToImmutableHashSet())
            {
                var converter = pair.Value;
                var view1 = converter.Element1.GetFirstAncestorOfType<DocumentView>();
                var view2 = converter.Element2.GetFirstAncestorOfType<DocumentView>();

                if (view1 == docView || view2 == docView) //view2?
                {
                    var fieldRef = RefToLine.FirstOrDefault(x => x.Value == pair.Key).Key;
                    var doc2 = view2.ViewModel.DocumentController;
                    var key = view2.ViewModel.KeysToFrameworkElements.FirstOrDefault(
                        keyAndElement => keyAndElement.Value.Equals(converter.Element2)).Key;
                    DeleteLine(fieldRef, pair.Key);
                    RemoveReference(doc2, key, fieldRef);
                    doc2.IsConnected = false;
                    fieldRef.GetDocumentController(null).IsConnected = false; // check

                }
            }
        }

        public void RemoveReference(DocumentController doc, KeyController fieldKey, FieldReference reference)
        {
            var outputCopy = reference.DereferenceToRoot(null)?.GetCopy();
            if (outputCopy == null) return;
            doc.SetField(fieldKey, outputCopy, true);
        }

        public void DeleteConnection(KeyValuePair<FieldReference, Path> pair)
        {
            if (RefToLine.ContainsKey(pair.Key))
            {
                var line = pair.Value;
                var converter = LineToConverter[line];
                var view1 = converter.Element1.GetFirstAncestorOfType<DocumentView>();
                var view2 = converter.Element2.GetFirstAncestorOfType<DocumentView>();
                var fieldRef = RefToLine.FirstOrDefault(x => x.Value == line).Key;
                DeleteLine(fieldRef, line);
            }
            _linesToBeDeleted = new Dictionary<FieldReference, Path>();
        }


        /// <summary>
        /// Adds the lines to be deleted as part of fading storyboard 
        /// </summary>
        /// <param name="fadeout"></param>
        public void AddToStoryboard(Windows.UI.Xaml.Media.Animation.Storyboard fadeout, DocumentView docView)
        {
            foreach (var pair in RefToLine)
            {
                var converter = LineToConverter[pair.Value];
                var view1 = converter.Element1.GetFirstAncestorOfType<DocumentView>();
                var view2 = converter.Element2.GetFirstAncestorOfType<DocumentView>();

                if (view1 == docView || view2 == docView)
                {
                    var animation = new Windows.UI.Xaml.Media.Animation.FadeOutThemeAnimation();
                    Windows.UI.Xaml.Media.Animation.Storyboard.SetTarget(animation, pair.Value);
                    fadeout.Children.Add(animation);
                    _linesToBeDeleted.Add(pair.Key, pair.Value);
                }
            }
        }

        private List<KeyValuePair<FieldReference, Path>> GetLinesToDelete()
        {
            var result = new List<KeyValuePair<FieldReference, Path>>();
            //var views = new HashSet<DocumentView>(_payload.Keys);
            foreach (var pair in RefToLine)
            {
                var converter = LineToConverter[pair.Value];
                var view1 = converter.Element1.GetFirstAncestorOfType<DocumentView>();
                var view2 = converter.Element2.GetFirstAncestorOfType<DocumentView>();
                //if (views.Contains(view1) || views.Contains(view2))
                if (view1 == null || view2 == null) // because at time of drop document disappears from VisualTree
                    result.Add(pair);
            }
            return result;
        }

        /// <summary>
        /// Update the bindings on lines when documentview is minimized to icon view 
        /// </summary>
        /// <param name="becomeSmall">whether the document has minimized or regained normal view</param>
        /// <param name="docView">the documentview that calls the method</param>
        public void UpdateBinding(bool becomeSmall, DocumentView docView)
        {
            return;
            //foreach (var converter in LineToConverter.Values)
            //{
            //    if (converter.Element1 == null || converter.Element2 == null)
            //    {
            //        return;
            //    }
            //    DocumentView view1 = converter.Element1.GetFirstAncestorOfType<DocumentView>();
            //    DocumentView view2 = converter.Element2.GetFirstAncestorOfType<DocumentView>();
            //    if (docView == view1)
            //    {
            //        if (becomeSmall)
            //        {
            //            if (!(converter.Element1 is Grid)) converter.Temp1 = converter.Element1;
            //            converter.Element1 = docView.xIcon;
            //        }
            //        else
            //        {
            //            converter.Element1 = converter.Temp1;
            //        }
            //    }
            //    else if (docView == view2)
            //    {
            //        if (becomeSmall)
            //        {
            //            if (!(converter.Element2 is Grid)) converter.Temp2 = converter.Element2;
            //            converter.Element2 = docView.xIcon;
            //        }
            //        else
            //        {
            //            converter.Element2 = converter.Temp2;
            //        }
            //    }
            //}
        }

        /// <summary>
        /// Helper method to start changing the connectionLines upon drag 
        /// </summary>
        /// <param name="dropPoint"> origin of manipulation </param>
        /// <param name="line"> the connection line to change </param>
        /// <param name="ioReference"> the reference for starting field </param>
        private void ChangeLineConnection(Point dropPoint, Path line, IOReference ioReference)
        {
            if (line.Stroke != (SolidColorBrush)App.Instance.Resources["AccentGreen"])
            {
                _converter = LineToConverter[line];
                //set up to manipulate connection line again 
                ViewModel.SetGlobalHitTestVisiblityOnSelectedItems(true);
                _connectionLine = line;
                _converter.Element2 = null;
                _converter.Pos2 = dropPoint;
                _currReference = ioReference;
                ManipulationControls.OnManipulatorTranslatedOrScaled -= ManipulationControls_OnManipulatorTranslated;

                //replace referenceControllers with the raw Controllers  
                var refField = RefToLine.FirstOrDefault(x => x.Value == line).Key;
                DocumentController inputController = refField.GetDocumentController(null);
                var rawField = inputController.GetField(refField.FieldKey);
                if (rawField as ReferenceController != null)
                    rawField = (rawField as ReferenceController).DereferenceToRoot(null);
                inputController.SetField(refField.FieldKey, rawField, false);
                RefToLine.Remove(refField);
            }
        }

        /// <summary>
        /// Sets the state of the collection to having a drag started, but not yet completed. 
        /// We keep track of the current IOReference with _currReference because when we end the drag we need to use the 
        /// IOReference from the inputting field *and* (usually) the IOReference from the field on the recieving end of the link.
        /// Sometimes this can be the other way around, if we drag from an input node to an output node, but the same general logic applies.
        /// We also keep track of the pointer so that when the pointer is moved, the link moves as well.
        /// </summary>
        /// <param name="ioReference">The IOReference generated by the field from which we are dragging</param>
        public void StartDrag(IOReference ioReference)
        {
            if (_currReference != null)
                return;

            if (!CanLink)
            {
                PointerArgs = ioReference.PointerArgs;
                return;
            }

            if (ioReference.PointerArgs == null)
            {
                return;
            }

            if (_currentPointers.Contains(ioReference.PointerArgs.Pointer.PointerId))
            {
                return;
            }

            ViewModel.SetGlobalHitTestVisiblityOnSelectedItems(true);
            //itemsPanelCanvas = xItemsControl.ItemsPanelRoot as Canvas;

            _currentPointers.Add(ioReference.PointerArgs.Pointer.PointerId);
            _currReference = ioReference;
            StartConnectionLine(ioReference, ioReference.PointerArgs.GetCurrentPoint(itemsPanelCanvas).Position);
        }

        /// <summary>
        /// Creates the visual link and stores the reference to it in _connectionLine, so that it can be used when the drag is ended.
        /// </summary>
        /// <param name="ioReference">The IOReference generated by the field outputting to the link</param>
        /// <param name="pos2">The second position on the link bezier curve</param>
        private void StartConnectionLine(IOReference ioReference, Point pos2)
        {
            _connectionLine = new UserCreatedLink
            {

                //TODO: made this hit test invisible because it was getting in the way of ink (which can do [almost] all the same stuff). sry :/
                IsHitTestVisible = false,
                StrokeThickness = 5,
                IsHoldingEnabled = false,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                CompositeMode = ElementCompositeMode.SourceOver //TODO Bug in xaml, shouldn't need this line when the bug is fixed 
                                                                //(https://social.msdn.microsoft.com/Forums/sqlserver/en-US/d24e2dc7-78cf-4eed-abfc-ee4d789ba964/windows-10-creators-update-uielement-clipping-issue?forum=wpdevelop)
            };


            //// set up for manipulation on lines 
            //_connectionLine.Tapped += (s, e) =>
            //{
            //    e.Handled = true;
            //    var line = s as Path;
            //    var green = _converter.GradientBrush;
            //    //line.Stroke = line.Stroke == green ? new SolidColorBrush(Colors.Goldenrod) : green;
            //    line.IsHoldingEnabled = !line.IsHoldingEnabled;
            //};

            //_connectionLine.Holding += (s, e) =>
            //{
            //    if (_connectionLine != null) return;
            //    ChangeLineConnection(e.GetPosition(itemsPanelCanvas), s as Path, ioReference);
            //};

            //_connectionLine.PointerPressed += (s, e) =>
            //{
            //    if (!e.GetCurrentPoint(itemsPanelCanvas).Properties.IsRightButtonPressed) return;
            //    ChangeLineConnection(e.GetCurrentPoint(itemsPanelCanvas).Position, s as Path, ioReference);
            //};


            Canvas.SetZIndex(_connectionLine, -1);
            _converter = new BezierConverter(ioReference.FrameworkElement, null, itemsPanelCanvas);
            _converter.Pos2 = pos2;

            _lineBinding = new MultiBinding<PathFigureCollection>(_converter, null);
            _lineBinding.AddBinding(ioReference.ContainerView, RenderTransformProperty);
            _lineBinding.AddBinding(ioReference.ContainerView, WidthProperty);
            _lineBinding.AddBinding(ioReference.ContainerView, HeightProperty);
            Binding lineBinding = new Binding
            {
                Source = _lineBinding,
                Path = new PropertyPath("Property")
            };
            PathGeometry pathGeo = new PathGeometry();
            BindingOperations.SetBinding(pathGeo, PathGeometry.FiguresProperty, lineBinding);
            _connectionLine.Data = pathGeo;

            _converter.setGradientAngle();
            _connectionLine.Stroke = _converter.GradientBrush;

            itemsPanelCanvas.Children.Add(_connectionLine);
        }


        public void CancelDrag(Pointer p)
        {
            ViewModel.SetGlobalHitTestVisiblityOnSelectedItems(false);
            ManipulationControls.OnManipulatorTranslatedOrScaled -= ManipulationControls_OnManipulatorTranslated;
            ManipulationControls.OnManipulatorTranslatedOrScaled += ManipulationControls_OnManipulatorTranslated;
            if (p != null) _currentPointers.Remove(p.PointerId);
            UndoLine();
        }

        /// <summary>
        /// Frees references and removes the line graphically 
        /// </summary>
        private void UndoLine()
        {
            if (_connectionLine != null) itemsPanelCanvas.Children.Remove(_connectionLine);
            _connectionLine = null;
            _currReference = null;
        }


        public void EndDrag(IOReference ioReference, bool isCompoundOperator, bool isLoadedLink = false)
        {
            // called when we release on a link, thus if the ioReference is output the _currReference
            // is the input since the _currReference must have been dragged from an input
            var inputReference = ioReference.IsOutput ? _currReference : ioReference;
            var outputReference = ioReference.IsOutput ? ioReference : _currReference;

            // condition checking 
            if (ioReference.PointerArgs != null) _currentPointers.Remove(ioReference.PointerArgs.Pointer.PointerId);
            if (_connectionLine == null) return;

            // checking conditions: only input-output pairs can be linked, can't link null or same fields, can't link if the field type is none.
            if (_currReference == null
                || _currReference.IsOutput == ioReference.IsOutput
                || (inputReference.Type & outputReference.Type) == 0
                || inputReference.FieldReference.Equals(outputReference.FieldReference)
                || _currReference.FieldReference == null)
            {
                UndoLine();
                return;
            }

            // get the document that has the input field
            var inputController = inputReference.FieldReference.GetDocumentController(null);
            var thisRef = (outputReference.ContainerView.DataContext as DocumentViewModel).DocumentController
                .GetDereferencedField(KeyStore.ThisKey, null);
            // TODO bob what does this mean
            if (inputController.DocumentType.Equals(DashConstants.TypeStore.OperatorType) &&
                inputReference.FieldReference is DocumentFieldReference && thisRef != null)
            {
                inputController.SetField(inputReference.FieldReference.FieldKey, thisRef, true);
            }
            else

            {
                inputController.SetField(inputReference.FieldReference.FieldKey,
                    new DocumentReferenceController(outputReference.FieldReference.GetDocumentId(), outputReference.FieldReference.FieldKey), true);
            }

            //Add the key to the inputController's list of user created links
            if (inputController.GetField(KeyStore.UserLinksKey) == null)
                inputController.SetField(KeyStore.UserLinksKey,
                    new ListController<KeyController>(), true);
            var linksList =
                inputController.GetField(KeyStore.UserLinksKey) as
                    ListController<KeyController>;
            linksList.Add(inputReference.FieldReference.FieldKey);


            //binding line position 
            _converter.Element2 = ioReference.FrameworkElement;
            _lineBinding.AddBinding(ioReference.ContainerView, RenderTransformProperty);
            _lineBinding.AddBinding(ioReference.ContainerView, WidthProperty);
            _lineBinding.AddBinding(ioReference.ContainerView, HeightProperty);

            //Add the link to the dictionaries mapping it to its converter and its field reference to the path.
            if (_connectionLine != null)
            {
                CheckLinePresence(ioReference.FieldReference);
                RefToLine.Add(ioReference.FieldReference, _connectionLine);
                var connectionLine = _connectionLine as UserCreatedLink;
                if (connectionLine != null)
                {
                    connectionLine.referencingDocument = inputController;
                    connectionLine.referencingKey = inputReference.FieldReference.FieldKey;
                    connectionLine.referencedKey = outputReference.FieldReference.FieldKey;
                    connectionLine.referencedDocument = outputReference.FieldReference.GetDocumentController(null);
                }

                if (!LineToConverter.ContainsKey(_connectionLine)) LineToConverter.Add(_connectionLine, _converter);
                if (!LineToElementKeysDictionary.ContainsKey(_connectionLine))
                {
                    AddLineToKeysEntry(_converter, _connectionLine);
                }
                _converter.OnPathUpdated += UpdateGradient;
                _connectionLine = null;
            }

            inputReference.FieldReference.GetDocumentController(null).IsConnected = true;
            outputReference.FieldReference.GetDocumentController(null).IsConnected = true; //this right?

            if (ioReference.PointerArgs != null) CancelDrag(ioReference.PointerArgs.Pointer);

            _currReference = null;

        }

        private static void AddLinkToList(KeyController key, DocumentController document)
        {
            if (document.GetField(KeyStore.UserLinksKey) == null)
            {
                document.SetField(KeyStore.UserLinksKey,
                    new ListController<TextController>(), true);
            }
            var linksList =
                document.GetField(KeyStore.UserLinksKey) as
                    ListController<TextController>;
            linksList.Add(new TextController(key.Id));
        }

        /// <summary>
        /// Fires when an element on the end of a path is moved, has the converter update the gradient 
        /// angle of the path's fill to match the position of the elements.
        /// </summary>
        /// <param name="converter"></param>
        private void UpdateGradient(BezierConverter converter)
        {
            var line = LineToConverter.FirstOrDefault(k => k.Value.Equals(converter)).Key;
            if (line != null) line.Stroke = converter.GradientBrush;
        }

        /// <summary>
        /// Helper function that checks if connection line is already present for input ellipse; if so, destroy that line and create a new one  
        /// </summary>
        private void CheckLinePresence(FieldReference reference)
        {
            if (!RefToLine.ContainsKey(reference)) return;
            var line = RefToLine[reference];
            itemsPanelCanvas.Children.Remove(line);
            RefToLine.Remove(reference);
            LineToConverter.Remove(line);
        }

        /// <summary>
        /// PointerMoved event. If drawing a link, updates the visual stroke line being drawn.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FreeformGrid_OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            // update stroke pointer for drawing links
            if (_connectionLine != null)
            {
                Point pos = e.GetCurrentPoint(itemsPanelCanvas).Position;
                _converter.Pos2 = pos;
                _converter.setGradientAngle();
                _connectionLine.Stroke = _converter.GradientBrush;
                _converter.UpdateLine();
                //_lineBinding.ForceUpdate();
            }
        }


        #endregion

        #region Manipulation
        public Rect ClipRect => xClippingRect.Rect;

        public void Move(TranslateTransform translate)
        {
            if (!IsHitTestVisible) return;
            var canvas = xItemsControl.ItemsPanelRoot as Canvas;

            var composite = new TransformGroup();
            composite.Children.Add(canvas.RenderTransform);
            composite.Children.Add(translate);

            var compValue = composite.Value;

            canvas.RenderTransform = new MatrixTransform { Matrix = compValue };
        }
        /// <summary>
        /// Pans and zooms upon touch manipulation 
        /// </summary>   
        private void ManipulationControls_OnManipulatorTranslated(TransformGroupData transformationDelta)
        {
            if (!IsHitTestVisible) return;
            var canvas = xItemsControl.ItemsPanelRoot as Canvas;
            Debug.Assert(canvas != null);
            var delta = transformationDelta.Translate;

            //Create initial translate and scale transforms
            //Translate is in screen space, scale is in canvas space
            var translate = new TranslateTransform
            {
                X = delta.X,
                Y = delta.Y
            };

            var scale = new ScaleTransform
            {
                CenterX = 0,
                CenterY = 0,
                ScaleX = transformationDelta.ScaleAmount.X,
                ScaleY = transformationDelta.ScaleAmount.Y
            };

            //Create initial composite transform
            var composite = new TransformGroup();

            composite.Children.Add(canvas.RenderTransform);
            composite.Children.Add(scale);
            composite.Children.Add(translate);

            canvas.RenderTransform = new MatrixTransform { Matrix = composite.Value };

            var compValue = composite.Value;

            if (compValue.OffsetY > 0)
            {
                compValue.OffsetY = 0;
            }

            var matrix = new MatrixTransform { Matrix = compValue };

            itemsPanelCanvas.RenderTransform = matrix;
            InkHostCanvas.RenderTransform = matrix;
            SetTransformOnBackground(compValue);

            // Updates line position if the collectionfreeformview canvas is manipulated within a compoundoperator view                                                                              
            if (this.GetFirstAncestorOfType<CompoundOperatorEditor>() != null)
            {
                foreach (var converter in LineToConverter.Values)
                    converter.UpdateLine();
            }
        }


        #endregion

        #region BackgroundTiling



        private double ClampBackgroundScaleForAliasing(double currentScale, double numberOfBackgroundRows)
        {
            while (currentScale / numberOfBackgroundRows > numberOfBackgroundRows)
            {
                currentScale /= numberOfBackgroundRows;
            }

            while (currentScale * numberOfBackgroundRows < numberOfBackgroundRows)
            {
                currentScale *= numberOfBackgroundRows;
            }
            return currentScale;
        }

        private void SetTransformOnBackground(Matrix transformMatrix)
        {
            var aliasSafeScale = ClampBackgroundScaleForAliasing(transformMatrix.M11, NumberOfBackgroundRows);

            if (_resourcesLoaded)
            {
                _bgBrush.Transform = new Matrix3x2((float)aliasSafeScale,
                    (float)transformMatrix.M12,
                    (float)transformMatrix.M21,
                    (float)aliasSafeScale,
                    (float)transformMatrix.OffsetX,
                    (float)transformMatrix.OffsetY);
                xBackgroundCanvas.Invalidate();
            }
        }

        private void SetInitialTransformOnBackground()
        {
            var composite = new TransformGroup();
            var scale = new ScaleTransform
            {
                CenterX = 0,
                CenterY = 0,
                ScaleX = CanvasScale,
                ScaleY = CanvasScale
            };

            composite.Children.Add(scale);
            SetTransformOnBackground(composite.Value);
        }

        private void CanvasControl_OnCreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
        {
            var task = Task.Run(async () =>
            {
                // Load the background image and create an image brush from it
                _bgImage = await CanvasBitmap.LoadAsync(sender, _backgroundPath);
                _bgBrush = new CanvasImageBrush(sender, _bgImage)
                {
                    Opacity = BackgroundOpacity
                };

                // Set the brush's edge behaviour to wrap, so the image repeats if the drawn region is too big
                _bgBrush.ExtendX = _bgBrush.ExtendY = CanvasEdgeBehavior.Wrap;

                _resourcesLoaded = true;
            });
            args.TrackAsyncAction(task.AsAsyncAction());

            task.ContinueWith(continuationTask =>
            {
                SetInitialTransformOnBackground();
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void CanvasControl_OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            if (!_resourcesLoaded) return;

            // Just fill a rectangle with our tiling image brush, covering the entire bounds of the canvas control
            var session = args.DrawingSession;
            session.FillRectangle(new Rect(new Point(), sender.Size), _bgBrush);
        }


        #endregion

        #region Clipping
        /// <summary>
        /// SizeChanged event. Updates the clipping rect's size on canvas resize.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void XOuterGrid_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            xClippingRect.Rect = new Rect(0, 0, xOuterGrid.ActualWidth, xOuterGrid.ActualHeight);
        }

        #endregion

        #region PointerChrome

        /// <summary>
        /// When the mouse hovers over the backgorund
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Background_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.IBeam, 1);
        }

        /// <summary>
        /// when the mouse leaves the background
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Background_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 1);
        }

        #endregion

        #region Tagging

        public bool TagNote(string tagValue, DocumentView docView)
        {
            if (!TagMode)
            {
                return false;
            }

            var groupDoc = docView.ManipulationControls.GetGroupForDocument(docView.ViewModel.DocumentController);
            ListController<DocumentController> group =
                groupDoc?.GetField<ListController<DocumentController>>(KeyStore.GroupingKey);
            if (groupDoc == null || group == null)
            {
                return false;
            }

            DocumentController image = null;
            foreach (var documentController in group.TypedData)
            {
                if (documentController.DocumentType.Equals(ImageBox.DocumentType))
                {
                    image = documentController.GetDataDocument(null);
                    break;
                }
            }

            if (image != null)
            {
                image.SetField(TagKey, new TextController(tagValue), true);
                return true;
            }
            return false;
        }

        public void ShowTagKeyBox()
        {
            TagKeyBox.Visibility = Visibility.Visible;
            var mousePos = CoreWindow.GetForCurrentThread().PointerPosition;
            mousePos = new Point(mousePos.X - Window.Current.Bounds.X, mousePos.Y - Window.Current.Bounds.Y);
            Debug.WriteLine(mousePos);
            mousePos = Util.PointTransformFromVisual(mousePos, Window.Current.Content, xOuterGrid);
            TagKeyBox.RenderTransform = new TranslateTransform { X = mousePos.X, Y = mousePos.Y };
        }

        public void HideTagKeyBox()
        {
            TagKeyBox.Visibility = Visibility.Collapsed;
        }

        private void TagKeyBox_OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var keys = ContentController<FieldModel>.GetControllers<KeyController>();
                var names = keys.Where(k => !k.Name.StartsWith("_"));
                TagKeyBox.ItemsSource = names;
            }
        }

        private void TagKeyBox_OnSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            sender.Text = ((KeyController)args.SelectedItem).Name;
        }

        private void TagKeyBox_OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion != null)
            {
                TagKey = (KeyController)args.ChosenSuggestion;
            }
            else
            {
                var keys = ContentController<FieldModel>.GetControllers<KeyController>();
                var key = keys.FirstOrDefault(k => k.Name == args.QueryText);

                if (key == null)
                {
                    TagKey = new KeyController(Guid.NewGuid().ToString(), args.QueryText);
                }
                else
                {
                    TagKey = key;
                }
            }
            TagMode = true;

            HideTagKeyBox();
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FreeformGrid_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            // If drawing a link node and you release onto the canvas, if the handle you're drawing from
            // is a document or a collection, this will create a new linked document/collection at the point
            // you released the mouse

            if (_currReference?.IsOutput == true && _currReference?.Type == TypeInfo.Document)
            {
                var pos = e.GetCurrentPoint(this).Position;
                var where = this.itemsPanelCanvas.RenderTransform.Inverse.TransformPoint(e.GetCurrentPoint(this).Position);
                var outputDoc = _currReference.FieldReference.GetReferenceController()?.DereferenceToRoot<DocumentController>(null);
                if (outputDoc != null)
                {
                    outputDoc.GetPositionField().Data = where;
                    ViewModel.AddDocument(outputDoc, null);
                }
                else
                {
                    var doc = new DocumentController(new Dictionary<KeyController, FieldControllerBase>
                    {
                        [KeyStore.DataKey] = _currReference.FieldReference.GetReferenceController()
                    }, DocumentType.DefaultType);
                    var layout = new DocumentBox(new DocumentReferenceController(doc.GetId(), KeyStore.DataKey), pos.X, pos.Y).Document;
                    doc.SetActiveLayout(layout, true, false);
                    ViewModel.AddDocument(doc, null);
                }
            }
            else if (_currReference?.IsOutput == true && _currReference?.Type == TypeInfo.List)
            {
                var droppedField = _currReference.FieldReference;
                var droppedSrcDoc = droppedField.GetDocumentController(null);

                var sourceViewType = droppedSrcDoc.GetActiveLayout()
                                         ?.GetDereferencedField<TextController>(KeyStore.CollectionViewTypeKey, null)
                                         ?.Data ??
                                     droppedSrcDoc
                                         .GetDereferencedField<TextController>(KeyStore.CollectionViewTypeKey, null)
                                         ?.Data ??
                                     CollectionView.CollectionViewType.Schema.ToString();

                var where = this.itemsPanelCanvas.RenderTransform.Inverse.TransformPoint(e.GetCurrentPoint(this).Position);
                var cnote = new CollectionNote(this.itemsPanelCanvas.RenderTransform.Inverse.TransformPoint(e.GetCurrentPoint(this).Position), (CollectionView.CollectionViewType)Enum.Parse(typeof(CollectionView.CollectionViewType), sourceViewType));
                cnote.Document.GetDataDocument(null).SetField(KeyStore.CollectionKey, new DocumentReferenceController(droppedSrcDoc.GetDataDocument(null).GetId(), droppedField.FieldKey), true);


                ViewModel.AddDocument(cnote.Document, null);
                DBTest.DBDoc.AddChild(cnote.Document);

                if (_currReference.FieldReference.FieldKey.Equals(KeyStore.CollectionOutputKey))
                {
                    var field = droppedSrcDoc.GetDataDocument(null)
                        .GetDereferencedField<TextController>(DBFilterOperatorController.FilterFieldKey, null)?.Data;
                    cnote.Document.GetDataDocument(null).SetField(DBFilterOperatorController.FilterFieldKey,
                        new TextController(field), true);
                }


                // bcz: hack to find the CollectionView for the newly created collection so that we can wire up the connection line as if it it had already been there
                UpdateLayout();
                for (int i = itemsPanelCanvas.Children.Count - 1; i >= 0; i--)
                    if (itemsPanelCanvas.Children[i] is ContentPresenter)
                    {
                        var cview = ((itemsPanelCanvas.Children[i] as ContentPresenter).Content as DocumentViewModel)
                            ?.Content as CollectionView;
                        EndDrag(
                            new IOReference(
                                new DocumentFieldReference(cnote.Document.GetId(), cview.ViewModel.CollectionKey),
                                false, TypeInfo.List, e, cview.ConnectionEllipseInput, cview.ParentDocument), false);
                        break;
                    }
            }
            CancelDrag(e.Pointer);
        }

        #region Marquee Select

        private Rectangle _marquee;
        private bool _multiSelect;
        private Point _marqueeAnchor;
        private bool _isSelecting;

        private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_marquee != null)
            {
                var pos = Util.PointTransformFromVisual(new Point(Canvas.GetLeft(_marquee), Canvas.GetTop(_marquee)),
                    SelectionCanvas, xItemsControl.ItemsPanelRoot);
                Rect marqueeRect = new Rect(pos, new Size(_marquee.Width, _marquee.Height));
                MarqueeSelectDocs(marqueeRect);
                _multiSelect = false;
                _marquee = null;
                _isSelecting = false;
                e.Handled = true;
            }

            xOuterGrid.ReleasePointerCapture(e.Pointer);
            Debug.WriteLine("number selected: " + ViewModel.SelectionGroup.Count());
        }

        private void OnPointerMoved(object sender, PointerRoutedEventArgs args)
        {
            var currentPoint = args.GetCurrentPoint(SelectionCanvas);
            if (!currentPoint.Properties.IsLeftButtonPressed || !_isSelecting) return;

            if ((args.KeyModifiers & VirtualKeyModifiers.Shift) != 0) _multiSelect = true;


            var pos = currentPoint.Position;
            var dX = pos.X - _marqueeAnchor.X;
            var dY = pos.Y - _marqueeAnchor.Y;

            //Height and width depend on the difference in position of the current point and the anchor (initial point)
            double newWidth = (dX > 0) ? dX : -dX;
            double newHeight = (dY > 0) ? dY : -dY;

            //Anchor point should also be moved if dX or dY are moved
            var newAnchor = _marqueeAnchor;
            if (dX < 0) newAnchor.X += dX;
            if (dY < 0) newAnchor.Y += dY;


            if (newWidth > 5 && newHeight > 5 && _marquee == null)
            {
                _marquee = new Rectangle()
                {
                    Stroke = new SolidColorBrush(Colors.Gray),
                    StrokeThickness = 1.5 / Zoom,
                    StrokeDashArray = new DoubleCollection { 5, 2 },
                    CompositeMode = ElementCompositeMode.SourceOver
                };
                SelectionCanvas.Children.Add(_marquee);
            }

            if (_marquee == null) return;

            //Adjust the marquee rectangle
            Canvas.SetLeft(_marquee, newAnchor.X);
            Canvas.SetTop(_marquee, newAnchor.Y);
            _marquee.Width = newWidth;
            _marquee.Height = newHeight;

            args.Handled = true;

        }

        private void OnPointerPressed(object sender, PointerRoutedEventArgs args)
        {
            if ((args.KeyModifiers & VirtualKeyModifiers.Control) == 0 &&
                (args.OriginalSource.Equals(XInkCanvas) || args.OriginalSource.Equals(xOuterGrid)) &&
                !args.GetCurrentPoint(xOuterGrid).Properties.IsRightButtonPressed)
            {
                xOuterGrid.CapturePointer(args.Pointer);
                var pos = args.GetCurrentPoint(SelectionCanvas).Position;
                _marqueeAnchor = pos;
                _isSelecting = true;
            }
        }

        private void MarqueeSelectDocs(Rect marquee)
        {
            SelectionCanvas.Children.Clear();
            if (!_multiSelect) DeselectAll();
            var selectedDocs = new List<DocumentView>();
            if (xItemsControl.ItemsPanelRoot != null)
            {
                IEnumerable<DocumentViewModel> docs =
                    xItemsControl.Items.OfType<DocumentViewModel>();
                foreach (var docvm in docs)
                {
                    var doc = docvm.DocumentController;
                    var position = doc.GetPositionField().Data;
                    var width = doc.GetWidthField().Data;
                    if (double.IsNaN(width)) width = 0;
                    var height = doc.GetHeightField().Data;
                    if (double.IsNaN(height)) height = 0;
                    var rect = new Rect(position, new Size(width, height));
                    if (marquee.IntersectsWith(rect) && xItemsControl.ItemContainerGenerator != null && xItemsControl
                            .ContainerFromItem(docvm) is ContentPresenter contentPresenter)
                    {
                        var documentView = contentPresenter.GetFirstDescendantOfType<DocumentView>();
                        if (documentView != null)
                            selectedDocs.Add(
                                documentView);
                    }
                }
            }
            foreach (var docView in selectedDocs)
            {
                Select(docView);
                //AddToPayload(docView);
            }
            //Makes the collectionview's selection mode "Multiple" if documents were selected.
            if (!IsSelectionEnabled && selectedDocs.Count > 0)
            {
                var parentView = this.GetFirstAncestorOfType<CollectionView>();
                parentView.MakeSelectionModeMultiple();
            }
        }

        #endregion

        #region Flyout
        #endregion

        #region DragAndDrop

        private void CollectionViewOnDrop(object sender, DragEventArgs e)
        {
            Debug.WriteLine("drop event from collection");
            ViewModel.CollectionViewOnDrop(sender, e);
        }

        public void SetDropIndicationFill(Brush fill)
        {
            XDropIndicationRectangle.Fill = fill;
        }

        #endregion

        #region Activation

        protected override void OnActivated(bool isSelected)
        {
            ViewModel.SetSelected(this, isSelected);
            ViewModel.UpdateDocumentsOnSelection(isSelected);
            if (InkController != null)
            {
                InkHostCanvas.IsHitTestVisible = isSelected;
                XInkCanvas.InkPresenter.IsInputEnabled = isSelected;
            }
        }

        protected override void OnLowestActivated(bool isLowestSelected)
        {
            ViewModel.SetLowestSelected(this, isLowestSelected);
        }

        private bool _singleTapped;

        private async void OnTapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;

            SelectionCanvas.Children.Clear();
            DeselectAll();

            _isSelecting = false;

            RenderPreviewTextbox(Util.GetCollectionFreeFormPoint(this, e.GetPosition(MainPage.Instance)));

            // so that doubletap is not overrun by tap events 
            _singleTapped = true;
            await Task.Delay(100);
            if (!_singleTapped) return;

            if (_connectionLine != null) CancelDrag(_currReference.PointerArgs.Pointer);

            if (ViewModel.IsInterfaceBuilder)
                return;

            if (!IsLowestSelected) OnSelected();

        }

        public void RenderPreviewTextbox(Point where)
        {
            previewTextBuffer = "";
            Canvas.SetLeft(previewTextbox, @where.X);
            Canvas.SetTop(previewTextbox, @where.Y);
            previewTextbox.Visibility = Visibility.Collapsed;
            previewTextbox.Visibility = Visibility.Visible;
            previewTextbox.Text = string.Empty;
            previewTextbox.Focus(FocusState.Programmatic);
            previewTextbox.LostFocus -= PreviewTextbox_LostFocus;
            previewTextbox.LostFocus += PreviewTextbox_LostFocus;
        }

        private void OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            _singleTapped = false;
            e.Handled = true;
            ChooseLowest(e);
        }

        private void ChooseLowest(DoubleTappedRoutedEventArgs e)
        {
            // get all descendants of free form views and call double tap on the lowest one
            var freeforms = xItemsControl.GetImmediateDescendantsOfType<CollectionFreeformView>();
            foreach (var ff in freeforms)
            {
                if (ff.xClippingRect.Rect.Contains(e.GetPosition(ff.xOuterGrid)))  // if the child collection is clicked 
                {
                    ff.ChooseLowest(e);
                    return;
                }
            }

            // in the lowest possible collectionfreeform 
            var docViews = xItemsControl.GetImmediateDescendantsOfType<DocumentView>();
            foreach (DocumentView view in docViews)
            {
                if (view.ClipRect.Contains(e.GetPosition(view.OuterGrid)))
                {
                    view.OnTapped(view, null); // hack to set selection on the lowest view
                    return;
                }
            }

            // if no docview to select, select the current collectionview 
            var parentView = this.GetFirstAncestorOfType<DocumentView>();
            parentView?.OnTapped(parentView, null);
        }

        #endregion

        #region SELECTION

        private bool _isSelectionEnabled;
        public bool IsSelectionEnabled
        {
            get { return _isSelectionEnabled; }
            set
            {
                _isSelectionEnabled = value;
                if (!value) // turn colors back ... 
                {
                    DeselectAll();
                }
            }
        }



        private Dictionary<DocumentView, DocumentController> _payload = new Dictionary<DocumentView, DocumentController>();

        private List<DocumentView> _documentViews = new List<DocumentView>();


        private bool _isToggleOn;
        public void ToggleSelectAllItems()
        {
            _isToggleOn = !_isToggleOn;
            foreach (var docView in DocumentViews)
                if (_isToggleOn)
                    Select(docView);
                else
                    Deselect(docView);
        }

        public void DeselectAll()
        {
            foreach (var docView in DocumentViews.Where(dv => ViewModel.SelectionGroup.Contains(dv.ViewModel)))
            {
                Deselect(docView);
            }
            ViewModel.SelectionGroup.Clear();
        }

        private void Deselect(DocumentView docView)
        {
            ViewModel.SelectionGroup.Remove(docView.ViewModel);
            docView.ToggleMultiSelected(false);

        }

        public void Select(DocumentView docView)
        {
            ViewModel.SelectionGroup.Add(docView.ViewModel);
            docView.ToggleMultiSelected(true);
        }

        private void Collection_DragLeave(object sender, DragEventArgs e)
        {
            Debug.WriteLine("CollectionViewOnDragLeave FreeForm");
            ViewModel.CollectionViewOnDragLeave(sender, e);
        }


        private void CollectionViewOnDragEnter(object sender, DragEventArgs e)
        {
            Debug.WriteLine("CollectionViewOnDragEnter FreeForm");
            ViewModel.CollectionViewOnDragEnter(sender, e);

        }

        public void DocView_OnDragStarting(object sender, DragStartingEventArgs e)
        {
            ViewModel.SetGlobalHitTestVisiblityOnSelectedItems(true);

            var docControllerList = new List<DocumentController>();
            foreach (var vm in ViewModel.SelectionGroup)
            {
                docControllerList.Add(vm.DocumentController);
            }
            e.Data.Properties.Add("DocumentControllerList", docControllerList);
            e.Data.Properties.Add("View", true);
            e.Data.RequestedOperation = DataPackageOperation.Link;
        }
        #endregion

        #region Ink

        public InkController InkController;
        public FreeformInkControl InkControl;
        public InkCanvas XInkCanvas;
        public Canvas SelectionCanvas;
        private bool loadingPermanentTextbox;
        public double Zoom { get { return ManipulationControls.ElementScale; } }
        private TextBox previewTextbox { get; set; }


        private void MakeInkCanvas()
        {
            XInkCanvas = new InkCanvas() { Width = 60000, Height = 60000 };
            SelectionCanvas = new Canvas();
            MakePreviewTextbox();

            InkControl = new FreeformInkControl(this, XInkCanvas, SelectionCanvas);
            Canvas.SetLeft(XInkCanvas, -30000);
            Canvas.SetTop(XInkCanvas, -30000);
            Canvas.SetLeft(SelectionCanvas, -30000);
            Canvas.SetTop(SelectionCanvas, -30000);
            InkHostCanvas.Children.Add(XInkCanvas);
            InkHostCanvas.Children.Add(SelectionCanvas);
        }

        private void MakePreviewTextbox()
        {
            previewTextbox = new TextBox
            {
                Width = 200,
                Height = 50,
                Background = new SolidColorBrush(Colors.Transparent),
                Visibility = Visibility.Collapsed
            };
            AddHandler(KeyDownEvent, new KeyEventHandler(PreviewTextbox_KeyDown), true);
            InkHostCanvas.Children.Add(previewTextbox);
            previewTextbox.LostFocus -= PreviewTextbox_LostFocus;
            previewTextbox.LostFocus += PreviewTextbox_LostFocus;
        }

        private void PreviewTextbox_LostFocus(object sender, RoutedEventArgs e)
        {
            previewTextbox.Visibility = Visibility.Collapsed;
            previewTextbox.LostFocus -= PreviewTextbox_LostFocus;
        }

        string previewTextBuffer = "";
        private async void PreviewTextbox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            var ctrlState = CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.Control)
                .HasFlag(CoreVirtualKeyStates.Down);
            previewTextbox.LostFocus -= PreviewTextbox_LostFocus;
            var text = KeyCodeToUnicode(e.Key);
            if (text is null) return;
            if (previewTextbox.Visibility == Visibility.Collapsed)
                return;
            e.Handled = true;
            var where = new Point(Canvas.GetLeft(previewTextbox), Canvas.GetTop(previewTextbox));
            if (text == "v" && ctrlState)
            {
                ViewModel.Paste(Clipboard.GetContent(), where);
                previewTextbox.Visibility = Visibility.Collapsed;
            }
            else
            {
                previewTextBuffer += text;
                if (text.Length > 0)
                    LoadNewActiveTextBox(text, where);
            }
        }

        public void LoadNewActiveTextBox(string text, Point where, bool resetBuffer = false)
        {
            if (!loadingPermanentTextbox)
            {
                if (resetBuffer)
                    previewTextBuffer = "";
                loadingPermanentTextbox = true;
                var postitNote = new RichTextNote(PostitNote.DocumentType, text: text, size: new Size(400, 32)).Document;
                Actions.DisplayDocument(ViewModel, postitNote, where);
            }
        }

        private string KeyCodeToUnicode(VirtualKey key)
        {

            var shiftState = CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.Shift)
                .HasFlag(CoreVirtualKeyStates.Down);
            var capState = CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.CapitalLock)
                .HasFlag(CoreVirtualKeyStates.Down) || CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.CapitalLock)
                               .HasFlag(CoreVirtualKeyStates.Locked);
            var virtualKeyCode = (uint)key;

            string character = null;

            // take care of symbols
            if (key == VirtualKey.Space)
            {
                character = " ";
            }
            if (key == VirtualKey.Multiply)
            {
                character = "*";
            }
            // TODO take care of more symbols

            //Take care of letters
            if (virtualKeyCode >= 65 && virtualKeyCode <= 90)
            {
                if (shiftState == false && capState == false ||
                    shiftState && capState)
                {
                    character = key.ToString().ToLower();
                }
                else
                {
                    character = key.ToString();
                }
            }

            //Take care of numbers
            if (virtualKeyCode >= 48 && virtualKeyCode <= 57)
            {
                character = (virtualKeyCode - 48).ToString();
                if ((shiftState != false || capState != false) &&
                    (!shiftState || !capState))
                {
                    switch ((virtualKeyCode - 48))
                    {
                        case 1: character = "!"; break;
                        case 2: character = "@"; break;
                        case 3: character = "#"; break;
                        case 4: character = "$"; break;
                        case 5: character = "%"; break;
                        case 6: character = "^"; break;
                        case 7: character = "&"; break;
                        case 8: character = "*"; break;
                        case 9: character = "("; break;
                        case 0: character = ")"; break;
                        default: break;
                    }
                }
            }

            if (virtualKeyCode >= 186 && virtualKeyCode <= 222)
            {
                var shifted = ((shiftState != false || capState != false) &&
                    (!shiftState || !capState));
                switch (virtualKeyCode)
                {
                    case 186: character = shifted ? ":" : ";"; break;
                    case 187: character = shifted ? "=" : "+"; break;
                    case 188: character = shifted ? "<" : ","; break;
                    case 189: character = shifted ? "_" : "-"; break;
                    case 190: character = shifted ? ">" : "."; break;
                    case 191: character = shifted ? "?" : "/"; break;
                    case 192: character = shifted ? "~" : "`"; break;
                    case 219: character = shifted ? "{" : "["; break;
                    case 220: character = shifted ? "|" : "\\"; break;
                    case 221: character = shifted ? "}" : "]"; break;
                    case 222: character = shifted ? "\"" : "'"; break;
                }

            }
            Debug.WriteLine("code=" + virtualKeyCode);
            //Take care of numpad numbers
            if (virtualKeyCode >= 96 && virtualKeyCode <= 105)
            {

                character = (virtualKeyCode - 96).ToString();
            }

            return character;
        }

        /// <summary>
        /// OnLoad handler. Interfaces with DocumentView to call corresponding functions.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DocumentViewOnLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is DocumentView documentView)
            {
                OnDocumentViewLoaded?.Invoke(this, documentView);
                DocumentViews.Add(documentView);

                if (loadingPermanentTextbox)
                {
                    var richEditBox = documentView.GetDescendantsOfType<RichEditBox>().FirstOrDefault();
                    if (richEditBox != null)
                    {
                        richEditBox.GotFocus -= RichEditBox_GotFocus;
                        richEditBox.GotFocus += RichEditBox_GotFocus;
                        richEditBox.Focus(FocusState.Programmatic);
                        documentView.OnSelected();
                    }
                }
            }

        }
        private void RichEditBox_GotFocus(object sender, RoutedEventArgs e)
        {
            previewTextbox.Visibility = Visibility.Collapsed;
            loadingPermanentTextbox = false;
            Debug.WriteLine("Got Focus");
            previewTextbox.Visibility = Visibility.Collapsed;
            var richEditBox = sender as RichEditBox;
            var text = previewTextBuffer;
            (sender as RichEditBox).GotFocus -= RichEditBox_GotFocus;
            previewTextbox.Text = string.Empty;
            richEditBox.Document.SetText(TextSetOptions.None, text);
            richEditBox.Document.Selection.SetRange(text.Length, text.Length);
        }

        #endregion

    }
}