using Dash.Views.Document_Menu;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;
using DashShared;
using Visibility = Windows.UI.Xaml.Visibility;


// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236
namespace Dash
{

    public sealed partial class DocumentView : SelectionElement
    {
        public CollectionView ParentCollection; // TODO document views should not be assumed to be in a collection this!

        public bool IsMainCollection { get; set; } //TODO document views should not be aware of if they are the main collection!

        /// <summary>
        /// Contains methods which allow the document to be moved around a free form canvas
        /// </summary>
        public ManipulationControls ManipulationControls;

        private Boolean useFixedMenu = false; // if true, doc menu appears fixed on righthand side of screen, otherwise appears next to doc

        private OverlayMenu _docMenu;
        public DocumentViewModel ViewModel { get; set; }
        // the document view that is being dragged
        public static DocumentView DragDocumentView;

        public bool ProportionalScaling { get; set; }

        public static int dvCount = 0;

        private Storyboard _storyboard;

        // == CONSTRUCTORs ==
        public DocumentView(DocumentViewModel documentViewModel) : this()
        {
            DataContext = documentViewModel;
        }

        public DocumentView()
        {
            InitializeComponent();
            Util.InitializeDropShadow(xShadowHost, xShadowTarget);

            DataContextChanged += DocumentView_DataContextChanged;

            // add manipulation code
            ManipulationControls = new ManipulationControls(OuterGrid, true, true, BorderRegion);
            ManipulationControls.OnManipulatorTranslatedOrScaled += ManipulatorOnManipulatorTranslatedOrScaled;
            // set bounds
            MinWidth = 100;
            MinHeight = 25;
            //OuterGrid.MinWidth = 100;
            //OuterGrid.MinHeight = 25;

            Loaded += This_Loaded;
            Unloaded += This_Unloaded;
            this.Drop += OnDrop;

            AddHandler(ManipulationCompletedEvent, new ManipulationCompletedEventHandler(DocumentView_ManipulationCompleted), true);
            AddHandler(ManipulationDeltaEvent, new ManipulationDeltaEventHandler(DocumentView_ManipulationDelta), true);

            //this.ManipulationCompleted += DocumentView_ManipulationCompleted;
            // this.ManipulationDelta += DocumentView_ManipulationDelta;
            AddHandler(TappedEvent, new TappedEventHandler(OnTapped), true);
            PointerPressed += DocumentView_PointerPressed;
            PointerReleased += DocumentView_PointerReleased;



        }


        private void DocumentView_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
        }

        private void DocumentView_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
        }

        private void DocumentView_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            var docView = sender as DocumentView;
            CheckForDropOnLink(docView);

            Snap(false);

        }

        private void DocumentView_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            Snap(true);
        }

        #region Snapping

        /// <summary>
        /// Enum used for snapping.
        /// TODO: Move this to the top of the class definition.
        /// </summary>
        private enum Side
        {
            Top = 1,
            Bottom = ~Top,
            Left = 2,
            Right = ~Left,
        };

        /// <summary>
        /// TODO: Move this to the top of the class definition.
        /// </summary>
        private const double ALIGNING_RECTANGLE_SENSITIVITY = 15.0;

        /// <summary>
        /// TODO: Move this to the top of the class definition.
        /// </summary>
        private const double ALIGNMENT_THRESHOLD = .2;

        /// <summary>
        /// Top level function for snapping
        /// </summary>
        private void Snap(bool preview)
        {
            //No snapping if main collection manipulated (i.e., panned)
            if (IsMainCollection)
            {
                return;
            }
            /*
            if (Equals(MainPage.Instance.xMainDocView))
            {
                return;
            }
            if (Parent is CollectionFreeformView)
            {
                
            }
            */

            MainPage.Instance.TemporaryRectangle.Width = MainPage.Instance.TemporaryRectangle.Height = 0;

            //Find the closest other DocumentView and snap to it.
            var closestDocumentView = GetClosestDocumentView();
            if (preview)
            {
                PreviewSnap(closestDocumentView);
            }
            else
            {
                SnapToDocumentView(closestDocumentView);
            }
        }

        private void PreviewSnap(Tuple<DocumentView, Side, double> closestDocumentView)
        {
            if (closestDocumentView == null)
            {
                //Debug.WriteLine("Hiding rectangle!");
                //Debug.WriteLine("Width: " + ActualWidth.ToString());
                return;
            }

            var documentView = closestDocumentView.Item1;
            var side = closestDocumentView.Item2;

            var closestDocumentViewScreenBoundingBox = documentView.GetBoundingBoxScreenSpace();
            var currentScreenBoundingBox = GetBoundingBoxScreenSpace();
            var newBoundingBox =
                CalculateAligningRectangleForSide(~side, closestDocumentViewScreenBoundingBox, currentScreenBoundingBox.Width, currentScreenBoundingBox.Height);

            //Debug.WriteLine("Showing rectangle!");
            //Debug.WriteLine("Width: " + ActualWidth.ToString());

            MainPage.Instance.TemporaryRectangle.Width = newBoundingBox.Width;
            MainPage.Instance.TemporaryRectangle.Height = newBoundingBox.Height;

            Canvas.SetLeft(MainPage.Instance.TemporaryRectangle, newBoundingBox.X);
            Canvas.SetTop(MainPage.Instance.TemporaryRectangle, newBoundingBox.Y);

        }

        /// <summary>
        /// Gets the closest DocumentView from all sides and returns the "closest" one
        /// </summary>
        /// <param name="topLeftScreenPoint"></param>
        /// <param name="bottomRightScreenPoint"></param>
        /// <returns></returns>
        private Tuple<DocumentView, Side, double> GetClosestDocumentView()
        {
            //List of all DocumentViews hit, along with a double representing how close they are
            var allDocumentViewsHit = HitTestFromSides();

            //Return closest DocumentView (using the double that represents the confidence)
            return allDocumentViewsHit.FirstOrDefault(item => item.Item3 == allDocumentViewsHit.Max(i2 => i2.Item3)); //Sadly no better argmax one-liner 
        }

        /// <summary>
        /// Snaps location of this DocumentView to the DocumentView passed in, also inheriting its width or height dimensions.
        /// </summary>
        /// <param name="closestDocumentView"></param>
        private void SnapToDocumentView(Tuple<DocumentView, Side, double> closestDocumentView)
        {
            if (closestDocumentView == null)
            {
                return;
            }

            var documentView = closestDocumentView.Item1;
            var side = closestDocumentView.Item2;
            var currentScaleAmount = ViewModel.GroupTransform.ScaleAmount;

            var topLeftPoint = new Point(documentView.ViewModel.GroupTransform.Translate.X,
                documentView.ViewModel.GroupTransform.Translate.Y);
            var bottomRightPoint = new Point(documentView.ViewModel.GroupTransform.Translate.X + documentView.ActualWidth,
                documentView.ViewModel.GroupTransform.Translate.Y + documentView.ActualHeight);

            var newBoundingBox =
                CalculateAligningRectangleForSide(~side, topLeftPoint, bottomRightPoint, ViewModel.Width, ViewModel.Height);

            var translate = new Point(newBoundingBox.X, newBoundingBox.Y);
            ViewModel.GroupTransform = new TransformGroupData(translate, new Point(0, 0), currentScaleAmount); 

            ViewModel.Width = newBoundingBox.Width;
            ViewModel.Height = newBoundingBox.Height;
        }

        
        /// <summary>
        /// Returns a list of DocumentViews hit by the side, as well as a double representing how close they are
        /// </summary>
        /// <param name="side"></param>
        /// <param name="topLeftScreenPoint"></param>
        /// <param name="bottomRightScreenPoint"></param>
        /// <returns></returns>
        private List<Tuple<DocumentView, Side, double>> HitTestFromSides()
        {
            var mainView = MainPage.Instance.GetMainCollectionView().CurrentView as CollectionFreeformView;
            var documentViewsAboveThreshold = new List<Tuple<DocumentView, Side, double>>();

            var currentBoundingBox = GetBoundingBoxScreenSpace();
            var topLeftScreenPoint = new Point(currentBoundingBox.X, currentBoundingBox.Y);
            var bottomRightScreenPoint = new Point(currentBoundingBox.X + currentBoundingBox.Width, currentBoundingBox.Y + currentBoundingBox.Height);

            Side[] sides = { Side.Top, Side.Bottom, Side.Left, Side.Right };
            foreach (var side in sides)
            {
                //Rect that will be hittested for
                var rect = CalculateAligningRectangleForSide(side, topLeftScreenPoint, bottomRightScreenPoint, ALIGNING_RECTANGLE_SENSITIVITY, ALIGNING_RECTANGLE_SENSITIVITY);
                var hitDocumentViews = VisualTreeHelper.FindElementsInHostCoordinates(rect, mainView, true).ToArray().Where(el => el is DocumentView).ToArray();

                foreach (var obj in hitDocumentViews)
                {
                    var documentView = obj as DocumentView;
                    if ((!documentView.Equals(MainPage.Instance.xMainDocView)) && (!documentView.Equals(this)))
                    {
                        var confidence = CalculateSnappingConfidence(side, rect, documentView);
                        if (confidence >= ALIGNMENT_THRESHOLD)
                        {
                            documentViewsAboveThreshold.Add(new Tuple<DocumentView, Side, double>(documentView, side, confidence));
                        }
                    }
                }
            }

            return documentViewsAboveThreshold;
        }

        private double CalculateSnappingConfidence(Side side, Rect hitTestRect, DocumentView otherDocumentView)
        {
            Rect otherDocumentViewBoundingBox = otherDocumentView.GetBoundingBoxScreenSpace();

            var midX = hitTestRect.X + hitTestRect.Width / 2;
            var midY = hitTestRect.Y + hitTestRect.Height / 2;

            double distanceToMid = -1;

            //Get normalized x or y distance from the complementary edge of the other DocumentView and the midpoint of the hitTestRect
            switch (side)
            {
                case Side.Top:
                    distanceToMid = Math.Abs(midY - (otherDocumentViewBoundingBox.Y + otherDocumentViewBoundingBox.Height));
                    distanceToMid = 1.0f - Math.Min(1.0, distanceToMid / hitTestRect.Height);
                    return distanceToMid * GetSharedRectWidthProportion(hitTestRect, otherDocumentViewBoundingBox);
                case Side.Bottom:
                    distanceToMid = Math.Abs(otherDocumentViewBoundingBox.Y - midY);
                    distanceToMid = 1.0f - Math.Min(1.0, distanceToMid / hitTestRect.Height);
                    return distanceToMid * GetSharedRectWidthProportion(hitTestRect, otherDocumentViewBoundingBox);
                case Side.Left:
                    distanceToMid = Math.Abs(midX - (otherDocumentViewBoundingBox.X + otherDocumentViewBoundingBox.Width));
                    distanceToMid = 1.0f - Math.Min(1.0, distanceToMid / hitTestRect.Width);
                    return distanceToMid * GetSharedRectHeightProportion(hitTestRect, otherDocumentViewBoundingBox);
                case Side.Right:
                    distanceToMid = Math.Abs(otherDocumentViewBoundingBox.X - midX);
                    distanceToMid = 1.0f - Math.Min(1.0, distanceToMid / hitTestRect.Width);
                    return distanceToMid * GetSharedRectHeightProportion(hitTestRect, otherDocumentViewBoundingBox);
            }
            return distanceToMid;
        }

        private double GetSharedRectWidthProportion(Rect source, Rect target)
        {
            var targetMin = target.X;
            var targetMax = target.X + target.Width;

            var sourceStart = Math.Max(targetMin, source.X);
            var sourceEnd = Math.Min(targetMax, source.X + source.Width);
            return (sourceEnd - sourceStart) / source.Width;
        }

        private double GetSharedRectHeightProportion(Rect source, Rect target)
        {
            var targetMin = target.Y;
            var targetMax = target.Y + target.Height;

            var sourceStart = Math.Max(targetMin, source.Y);
            var sourceEnd = Math.Min(targetMax, source.Y + source.Height);

            return (sourceEnd - sourceStart) / source.Height;
        }

        public Rect GetBoundingBoxScreenSpace()
        {
            Point topLeftObjectPoint = new Point(0, 0);
            Point bottomRightObjectPoint = new Point(ViewModel.Width, ViewModel.Height);

            var topLeftPoint = Util.PointTransformFromVisual(topLeftObjectPoint, this);
            var bottomRightPoint = Util.PointTransformFromVisual(bottomRightObjectPoint, this);

            return new Rect(topLeftPoint, bottomRightPoint);
        }
        private Rect CalculateAligningRectangleForSide(Side side, Point topLeftPoint, Point bottomRightPoint, double w, double h)
        {
            Point newTopLeft, newBottomRight;

            switch (side)
            {
                case Side.Top:
                    newTopLeft = new Point(topLeftPoint.X, topLeftPoint.Y - h);
                    newBottomRight = new Point(bottomRightPoint.X, topLeftPoint.Y);
                    break;
                case Side.Bottom:
                    newTopLeft = new Point(topLeftPoint.X, bottomRightPoint.Y);
                    newBottomRight = new Point(bottomRightPoint.X, bottomRightPoint.Y + h);
                    break;
                case Side.Left:
                    newTopLeft = new Point(topLeftPoint.X - w, topLeftPoint.Y);
                    newBottomRight = new Point(topLeftPoint.X, bottomRightPoint.Y);
                    break;
                case Side.Right:
                    newTopLeft = new Point(bottomRightPoint.X, topLeftPoint.Y);
                    newBottomRight = new Point(bottomRightPoint.X + w, bottomRightPoint.Y);
                    break;
            }
            return new Rect(newTopLeft, newBottomRight);
        }

        private Rect CalculateAligningRectangleForSide(Side side, Rect boundingBox, double w, double h)
        {
            Point topLeftPoint = new Point(boundingBox.X, boundingBox.Y);
            Point bottomRightPoint = new Point(boundingBox.X + boundingBox.Width, boundingBox.Y + boundingBox.Height);
            return CalculateAligningRectangleForSide(side, topLeftPoint, bottomRightPoint, w, h);
        }


        #endregion


        private void CheckForDropOnLink(DocumentView docView)
        {
            if (docView != null)
            {
                var docType = docView.ViewModel?.DocumentController?.GetActiveLayout()?.DocumentType;
                if (docType != null && docView.ViewModel?.DocumentController?.IsConnected == false)
                {
                    if (docType.Equals(DashConstants.TypeStore.OperatorBoxType))
                    {
                        //Get the coordinates of the view
                        Point screenCoords = docView.TransformToVisual(Window.Current.Content)
                            .TransformPoint(new Point(0, 0));

                        //parent freeform view
                        var freeformView = docView.ParentCollection?.CurrentView as CollectionFreeformView;
                        if (freeformView?.RefToLine != null && !IsConnected(docView))
                        {
                            // iterate through all the links in this freeform view to check for overlap
                            foreach (var link in freeformView.RefToLine)
                            {
                                //Get the slope of the line through the endpoints of the link
                                var converter = freeformView.LineToConverter[link.Value];

                                // first end point of link
                                var curvePoint1 = converter.Element1
                                    .TransformToVisual(freeformView.xItemsControl.ItemsPanelRoot)
                                    .TransformPoint(new Point(converter.Element1.ActualWidth / 2,
                                        converter.Element1.ActualHeight / 2));

                                // second end point of link
                                var curvePoint2 = converter.Element2
                                    .TransformToVisual(freeformView.xItemsControl.ItemsPanelRoot)
                                    .TransformPoint(new Point(converter.Element2.ActualWidth / 2,
                                        converter.Element2.ActualHeight / 2));

                                // calculate slope
                                var slope = (curvePoint2.Y - curvePoint1.Y) / (curvePoint2.X - curvePoint1.X);

                                // Figure out the x coordinates where the line intersects the top and bottom bounding horizontal lines of the rectangle of the document view
                                var intersectionTopX = curvePoint1.X - (1 / slope) * (-screenCoords.Y + curvePoint1.Y);
                                var intersectionBottomX =
                                    curvePoint1.X - (1 / slope) * (-(screenCoords.Y + docView.ActualHeight) + curvePoint1.Y);

                                // If the top intersection point is to the left of the documentView, or the bottom intersection is to the right, when the slope is positive,
                                // the link is outside the document.
                                if ((slope < 0 && !(intersectionTopX < screenCoords.X ||
                                                   intersectionBottomX > screenCoords.X + docView.ActualWidth)
                                    || slope > 0 && !(intersectionTopX > screenCoords.X ||
                                                      intersectionBottomX < screenCoords.X + docView.ActualWidth)))
                                {
                                    // if the document is between the vertical bounds of the link endpoints
                                    if (screenCoords.Y > (Math.Min(curvePoint1.Y, curvePoint2.Y))
                                                      && (screenCoords.Y + docView.ActualHeight < (Math.Max(curvePoint1.Y, curvePoint2.Y))))
                                    {
                                        // connect the dropped document to the documents linked by the path
                                        ChangeConnections(freeformView, docView, link);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void DisconnectFromLink()
        {
            (ParentCollection.CurrentView as CollectionFreeformView)?.DeleteConnections(this);
            ViewModel.DocumentController.IsConnected = false;
        }

        /// <summary>
        /// Returns true if a document view is already linked to another, false if not
        /// </summary>
        /// <param name="docView"></param>
        /// <returns></returns>
        private bool IsConnected(DocumentView docView)
        {
            var userLinks = docView.ViewModel.DocumentController.GetField(KeyStore.UserLinksKey) as ListController<TextController>;
            if (userLinks == null || userLinks.Data.Count <= 0)
            {
                return false;
            }
            return true;
        }



        /// <summary>
        /// Changes the connections to connect the dropped document with the documents connected by the link
        /// </summary>
        /// <param name="ffView"></param>
        /// <param name="docView"></param>
        /// <param name="link"></param>
        private void ChangeConnections(CollectionFreeformView ffView, DocumentView docView, KeyValuePair<FieldReference, Path> link)
        {
            // the old connection is [referencedDoc] -> [referencingDoc]
            // the new connections are [referencedDoc] -> [droppedDoc] -> [referencingDoc]

            // get all the ingredients
            var droppedDoc = docView.ViewModel.DocumentController;
            var droppedDocOpFMController = droppedDoc.GetField(KeyStore.OperatorKey) as OperatorController;
            var droppedDocInputKey = droppedDocOpFMController.Inputs.Keys.FirstOrDefault();
            var droppedDocOutputKey = droppedDocOpFMController.Outputs.Keys.FirstOrDefault();

            var userLink = link.Value as UserCreatedLink;

            var referencingDoc = userLink.referencingDocument;
            var referencingKey = userLink.referencingKey;

            var referencedKey = userLink.referencedKey;
            var referencedDoc = userLink.referencedDocument;

            

            // Check if nodes inputs/outputs are of the same type
            var droppedDocOutputType = droppedDocOpFMController.Outputs[droppedDocOutputKey];
            var droppedDocInputType = droppedDocOpFMController.Inputs[droppedDocInputKey];

            var referencedDocOpFMController = referencedDoc.GetField(KeyStore.OperatorKey) as OperatorController;
            var referencedDocOutputType = referencedDocOpFMController?.Outputs[referencedKey];

            var referencingDocOpFMController = referencingDoc.GetField(KeyStore.OperatorKey) as OperatorController;
            var referencingDocInputType = referencingDocOpFMController?.Inputs[referencingKey];

            if(droppedDocOutputType == referencingDocInputType?.Type || referencedDocOutputType == droppedDocInputType?.Type)
            {
                // delete the current connection between referenced doc and referencing doc
                ffView.DeleteLine(link.Key, userLink); // check

                //Add connection between dropped and right node
                MakeConnection(ffView, droppedDoc, droppedDocOutputKey, referencingDoc, referencingKey);

                //Add connection between dropped and right node
                MakeConnection(ffView, referencedDoc, referencedKey, droppedDoc, droppedDocInputKey);

                referencedDoc.IsConnected = true;
                referencingDoc.IsConnected = true;
                droppedDoc.IsConnected = true;
            }
        }

        /// <summary>
        /// Makes a link between 2 documents
        /// </summary>
        /// <param name="ffView"></param>
        /// <param name="referencedDoc"></param>
        /// <param name="referencedKey"></param>
        /// <param name="referencingDoc"></param>
        /// <param name="referencingKey"></param>
        /// <returns></returns>
        private static void MakeConnection(CollectionFreeformView ffView, DocumentController referencedDoc, KeyController referencedKey, DocumentController referencingDoc, KeyController referencingKey)
        {
            // set the field of the referencing field to be a field reference to the referenced document/field
            var fieldRef = new DocumentFieldReference(referencedDoc.GetId(), referencedKey);
            var thisRef = (referencedDoc.GetDereferencedField(KeyStore.ThisKey, null));

            if (referencedDoc.DocumentType.Equals(DashConstants.TypeStore.OperatorBoxType) &&
                fieldRef is DocumentFieldReference && thisRef != null)
                referencingDoc.SetField(referencedKey, thisRef, true);
            else
            {
                referencingDoc.SetField(referencingKey,
                new DocumentReferenceController(fieldRef.GetDocumentId(), referencedKey), true);
            }                                                                                       

            // add line visually
            ffView.AddLineFromData(fieldRef, new DocumentFieldReference(referencingDoc.GetId(), referencingKey));
        }


        private void OnDrop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems)) e.Handled = true;
            FileDropHelper.HandleDropOnDocument(this, e);
            ParentCollection?.ViewModel.ChangeIndicationColor(ParentCollection.CurrentView, Colors.Transparent);

            //handles drop from keyvaluepane 
            OnKeyValueDrop(e);
        }

        public void ToFront()
        {
            if (ParentCollection == null) return;
            ParentCollection.MaxZ += 1;
            Canvas.SetZIndex(this.GetFirstAncestorOfType<ContentPresenter>(), ParentCollection.MaxZ);
        }


        public DocumentController Choose()
        {
            OnSelected();
            // bring document to center? 
            var mainView = MainPage.Instance.GetMainCollectionView().CurrentView as CollectionFreeformView;
            if (mainView != null)
            {
                var pInWorld = Util.PointTransformFromVisual(new Point(Width / 2, Height / 2), this, mainView);
                var worldMid = new Point(mainView.ClipRect.Width / 2, mainView.ClipRect.Height / 2);
                mainView.Move(new TranslateTransform { X = worldMid.X - pInWorld.X, Y = worldMid.Y - pInWorld.Y });
            }
            return null;
        }

        private void This_Unloaded(object sender, RoutedEventArgs e)
        {
            //Debug.WriteLine($"Unloaded: Num DocViews = {--dvCount}");
            DraggerButton.Holding -= DraggerButtonHolding;
            DraggerButton.ManipulationDelta -= Dragger_OnManipulationDelta;
            DraggerButton.ManipulationCompleted -= Dragger_ManipulationCompleted;
        }

        
        private void This_Loaded(object sender, RoutedEventArgs e)
        {
            //Debug.WriteLine($"Loaded: Num DocViews = {++dvCount}");
            DraggerButton.Holding -= DraggerButtonHolding;
            DraggerButton.Holding += DraggerButtonHolding;
            DraggerButton.ManipulationDelta -= Dragger_OnManipulationDelta;
            DraggerButton.ManipulationDelta += Dragger_OnManipulationDelta;
            DraggerButton.ManipulationCompleted -= Dragger_ManipulationCompleted;
            DraggerButton.ManipulationCompleted += Dragger_ManipulationCompleted;

            ParentCollection = this.GetFirstAncestorOfType<CollectionView>();

            // Adds a function to tabmenu, which brings said DocumentView to focus 
            // this gets the hierarchical view of the document, clicking on this will shimmy over to this
            IsMainCollection = (this == MainPage.Instance.MainDocView);

            // add corresponding instance of this to hierarchical view
            if (!IsMainCollection && ViewModel != null)
            {
                
                if (double.IsNaN(ViewModel.Width) &&
                    (ParentCollection?.CurrentView is CollectionFreeformView))
                {
                    ViewModel.Width = 50;
                    ViewModel.Height = 50;
                }
            }
            new ManipulationControls(xKeyValuePane, false, false);
        }

        #region Xaml Styling Methods (used by operator/collection view)
        private bool isOperator = false;
        private bool addItem = false;
        /// <summary>
        /// Applies custom override styles to the operator view. 
        /// width - the width of a single link node (generally App.xaml defines this, "InputHandleWidth")
        /// </summary>
        public void StyleOperator(double width, string title)
        {
            isOperator = true;
            xShadowTarget.Margin = new Thickness(width, 0, width, 0);
            xGradientOverlay.Margin = new Thickness(width, 0, width, 0);
            xShadowTarget.Margin = new Thickness(width, 0, width, 0);
            DraggerButton.Margin = new Thickness(0, 0, -(20 - width), -20);
            xTitle.Text = title;
            xTitleIcon.Text = Application.Current.Resources["OperatorIcon"] as string;
            xTitleBorder.Margin = new Thickness(width + xTitleBorder.Margin.Left, xTitleBorder.Margin.Top, width, xTitleBorder.Margin.Bottom);
            if (ParentCollection != null)
            {
                //ViewModel.DocumentController.SetTitleField(title);
                var dataDoc = ViewModel.DocumentController.GetDataDocument(null);
                dataDoc.SetTitleField(title);
                var layoutDoc = ViewModel.DocumentController.GetActiveLayout(null) ?? ViewModel.DocumentController;
               
            }
        }

        static int CollectionCount = 0; // 100% a hack for labelling collection uniquely
    
        #endregion
        SolidColorBrush bgbrush = (Application.Current.Resources["WindowsBlue"] as SolidColorBrush);

        /// <summary>
        /// Applies custom override styles to the operator view. 
        /// width - the width of a single link node (generally App.xaml defines this, "InputHandleWidth")
        /// </summary>
        public void StyleCollection(CollectionView view)
        {
            
            var width = 20;
            
            xShadowTarget.Margin = new Thickness(width, 0, width, 0);
            xGradientOverlay.Margin = new Thickness(width, 0, width, 0);
            xShadowTarget.Margin = new Thickness(width, 0, width, 0);
            DraggerButton.Margin = new Thickness(0, 0, -(20 - width), -20);
            
            addItem = false;
            xTitleIcon.Text = Application.Current.Resources["CollectionIcon"] as string;
            xTitle.Text = "Collection (" + CollectionCount + ")";
            xTitleBorder.Margin = new Thickness(width + xTitleBorder.Margin.Left, xTitleBorder.Margin.Top, width, xTitleBorder.Margin.Bottom);
            CollectionCount++;
        }
        
        #region KEYVALUEPANE
        private static int KeyValPaneWidth = 200;
        private void OpenCloseKeyValuePane()
        {
            if (xKeyValPane.Visibility == Visibility.Collapsed)
            {
                xKeyValPane.Width = KeyValPaneWidth;
                xKeyValPane.Visibility = Visibility.Visible;
                ViewModel.Width += KeyValPaneWidth;
                ManipulatorOnManipulatorTranslatedOrScaled(new TransformGroupData(new Point(-KeyValPaneWidth * ManipulationControls.ElementScale, 0), new Point(0, 0), new Point(1, 1)));
            }
            else
            {
                xKeyValPane.Visibility = Visibility.Collapsed;
                xKeyValPane.Width = 0;
                ViewModel.Width -= KeyValPaneWidth;
                ManipulatorOnManipulatorTranslatedOrScaled(new TransformGroupData(new Point(KeyValPaneWidth * ManipulationControls.ElementScale, 0), new Point(0, 0), new Point(1, 1)));
            }
        }


        private void ShowContext()
        {
            ViewModel.DocumentController.GetDataDocument(null).RestoreNeighboringContext();
        }
        private void xKeyValPane_Tapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
        }
        private void xKeyValPane_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void OnKeyValueDrop(DragEventArgs e)
        {
            // if the drop wasn't from the key value pane, then return
            // if the view is in the interface builder return
            if (e.Data?.Properties[KeyValuePane.DragPropertyKey] == null || (ViewModel?.IsInInterfaceBuilder ?? true)) return;

            // get data variables from the DragArgs
            var kvp = (KeyValuePair<KeyController, DocumentController>)e.Data.Properties[KeyValuePane.DragPropertyKey];

            var dataDocController = kvp.Value;
            if (!dataDocController.Equals(ViewModel.DocumentController)) return; // return if it's not sent from the appropriate keyvaluepane 

            var dataKey = kvp.Key;
            var context = new Context(dataDocController);
            var dataField = dataDocController.GetDereferencedField(dataKey, context);

            // get a layout document for the data - use the most abstract prototype as the field reference document
            //  (otherwise, the layout would point directly to the data instance which would make it impossible to
            //   create Data copies since the layout would point directly to the (source) data instance and not the common prototype).
            var dataPrototypeDoc = kvp.Value;
            while (dataPrototypeDoc.GetPrototype() != null)
                dataPrototypeDoc = dataPrototypeDoc.GetPrototype();
            var layoutDocument = InterfaceBuilder.GetLayoutDocumentForData(dataField, dataPrototypeDoc, dataKey, null);
            if (layoutDocument == null)
                return;

            // apply position if we are dropping on a freeform
            var posInLayoutContainer = e.GetPosition(xFieldContainer);
            var widthOffset = (layoutDocument.GetField(KeyStore.WidthFieldKey) as NumberController).Data / 2;
            var heightOffset = (layoutDocument.GetField(KeyStore.HeightFieldKey) as NumberController).Data / 2;
            var positionController = new PointController(posInLayoutContainer.X - widthOffset, posInLayoutContainer.Y - heightOffset);
            layoutDocument.SetField(KeyStore.PositionFieldKey, positionController, forceMask: true);

            // add the document to the composite
            var data = ViewModel.LayoutDocument.GetDereferencedField(KeyStore.DataKey, context) as ListController<DocumentController>;
            data?.Add(layoutDocument);
        }
        #endregion

        DateTime copyDown = DateTime.MinValue;
        MenuButton copyButton;
        private void SetUpMenu()
        {
            var red = new Color();
            red.A = 204;
            red.R = 190;
            red.B = 25;
            red.G = 25;

            copyButton = new MenuButton(Symbol.Copy, "Copy", CopyDocument);
            var moveButton = new MenuButton(Symbol.MoveToFolder, "Move", null);
            var copyDataButton = new MenuButton(Symbol.SetTile, "Copy Data", CopyDataDocument);
            var instanceDataButton = new MenuButton(Symbol.SetTile, "Instance", InstanceDataDocument);
            var copyViewButton = new MenuButton(Symbol.SetTile, "Alias", CopyViewDocument);
            var addButton = new MenuButton(Symbol.Add, "Add", OpenCloseKeyValuePane);
            var showContextButton = new MenuButton(Symbol.Add, "Context", ShowContext);

            var documentButtons = new List<MenuButton>
            {
                //moveButton,
                new MenuButton(Symbol.Delete, "Delete",DeleteDocument),
                copyButton,
               // delegateButton,
               // copyDataButton
               // instanceDataButton,
                copyViewButton,
                new MenuButton(Symbol.Pictures, "Layout",OpenLayout),
                //new MenuButton(Symbol.Camera, "ScrCap",bgcolor, ScreenCap),
                //new MenuButton(Symbol.Placeholder, "Commands",bgcolor, CommandLine)
                addButton,
                showContextButton
            };
            moveButton.DragStarting += (s, e) =>
            {
                e.Data.RequestedOperation = DataPackageOperation.Move;
                ViewModel.DocumentView_DragStarting(this, e, ParentCollection.ViewModel);
            };
            moveButton.DropCompleted += ButtonView_DropCompleted;
            copyButton.AddHandler(PointerPressedEvent, new PointerEventHandler(CopyButton_PointerPressed), true);
            copyButton.DragStarting += (s, e) =>
            {
                _moveTimer.Stop();
                e.Data.RequestedOperation = copyButton.Contents.Symbol == Symbol.MoveToFolder ? DataPackageOperation.Move : DataPackageOperation.Copy;
                ViewModel.DocumentView_DragStarting(this, e, ParentCollection.ViewModel);
            };
            copyButton.DropCompleted += ButtonView_DropCompleted;
            copyDataButton.DragStarting += (s, e) =>
            {
                e.Data.RequestedOperation = DataPackageOperation.Link;
                ViewModel.DocumentView_DragStarting(this, e, ParentCollection.ViewModel);
            };
            instanceDataButton.DragStarting += (s, e) =>
            {
                e.Data.RequestedOperation = DataPackageOperation.Link;
                ViewModel.DocumentView_DragStarting(this, e, ParentCollection.ViewModel);
            };
            addButton.DragStarting += (s, e) =>
            {
                e.Data.RequestedOperation = DataPackageOperation.Link;
                ViewModel.DocumentView_DragStarting(this, e, ParentCollection.ViewModel);
            };
            copyViewButton.DragStarting += (s, e) =>
            {
                e.Data.RequestedOperation = DataPackageOperation.Link;
                e.Data.Properties.Add("View", true);
                ViewModel.DocumentView_DragStarting(this, e, ParentCollection.ViewModel);
            };


            _docMenu = new OverlayMenu(null, documentButtons);

            Binding visibilityBinding = new Binding
            {
                Source = ViewModel,
                Path = new PropertyPath(nameof(ViewModel.DocMenuVisibility)),
                Mode = BindingMode.OneWay
            };
            xMenuCanvas.SetBinding(VisibilityProperty, visibilityBinding);

            if (!useFixedMenu)
                xMenuCanvas.Children.Add(_docMenu);
            _moveTimer.Tick += Timer_Tick;
        }

        private void ButtonView_DropCompleted(UIElement sender, DropCompletedEventArgs args)
        {
            if (args.DropResult == DataPackageOperation.Move)
            {
                var coll = this.GetFirstAncestorOfType<CollectionView>();
                Debug.Assert(coll != null);
                coll.ViewModel.RemoveDocument(ViewModel.DocumentController);
            }
            else
            { // HACK ... It seems that setting the Position doesn't trigger the transform to update...
                var currentTranslate = ViewModel.GroupTransform.Translate;
                var currentScaleAmount = ViewModel.GroupTransform.ScaleAmount;
                var layout = ViewModel.DocumentController.GetActiveLayout() ?? ViewModel.DocumentController;
                ViewModel.GroupTransform = new TransformGroupData(layout.GetDereferencedField<PointController>(KeyStore.PositionFieldKey, null).Data, new Point(), currentScaleAmount);
            }
        }

        DispatcherTimer _moveTimer = new DispatcherTimer()
        {
            Interval = new TimeSpan(0, 0, 0, 0, 600),
        };

        private void CopyButton_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
           // _moveTimer.Start();
        }

        private void Timer_Tick(object sender, object e)
        {
            copyButton.Contents.Symbol = Symbol.MoveToFolder;
            copyButton.ButtonText.Text = "Move";
        }

        /// <summary>
        /// Update viewmodel when manipulator moves document
        /// </summary>
        /// <param name="delta"></param>
        private void ManipulatorOnManipulatorTranslatedOrScaled(TransformGroupData delta)
        {
            if (ViewModel != null)
                ViewModel.TransformDelta(delta);
        }

        /// <summary>
        /// Resizes the CollectionView according to the increments in width and height. 
        /// The CollectionListView vertically resizes corresponding to the change in the size of its cells, so if ProportionalScaling is true and the ListView is being displayed, 
        /// the Grid must change size to accomodate the height of the ListView.
        /// </summary>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        public Size Resize(double dx = 0, double dy = 0)
        {
            var dvm = DataContext as DocumentViewModel;
            if (dvm != null)
            {
                Debug.Assert(dvm != null, "dvm != null");
                Debug.Assert(dvm.Width != double.NaN);
                Debug.Assert(dvm.Height != double.NaN);
                dvm.Width = Math.Max(dvm.Width + dx, MinWidth);
                dvm.Height = Math.Max(dvm.Height + dy, MinHeight);
                // should we allow documents with NaN's for width & height to be resized?
                return new Size(dvm.Width, dvm.Height);
            }
            return new Size();
        }

        public void ProporsionalResize(ManipulationDeltaRoutedEventArgs e)
        {
            var pos = Util.PointTransformFromVisual(e.Position, e.Container);
            var origin = Util.PointTransformFromVisual(new Point(0, 0), this);
            Debug.WriteLine(pos);
            double dx = (pos.X - origin.X) / ViewModel.Width;
            double dy = (pos.Y - origin.Y) / ViewModel.Height;
            Debug.WriteLine(pos);
            Debug.WriteLine(new Point(dx, dy));
            double scale = Math.Max(Math.Max(dx, dy), 0.1);
            Debug.WriteLine(scale);
            var gt = ViewModel.GroupTransform;
            ViewModel.GroupTransform = new TransformGroupData(gt.Translate, gt.ScaleCenter, new Point(scale, scale));
        }

        /// <summary>
        /// Called when the user holds the dragger button, or finishes holding it; 
        /// if the button is held down, initiates the proportional resizing mode.
        /// </summary>
        /// <param name="sender">DraggerButton in the DocumentView class</param>
        /// <param name="e"></param>
        public void DraggerButtonHolding(object sender, HoldingRoutedEventArgs e)
        {
            if (e.HoldingState == HoldingState.Started)
            {
                ProportionalScaling = true;
            }
            else if (e.HoldingState == HoldingState.Completed)
            {
                ProportionalScaling = false;
            }
        }

        /// <summary>
        /// Resizes the control based on the user's dragging the DraggerButton.  The contents will adjust to fit the bounding box
        /// of the control *unless* the Shift button is held in which case the control will be resized but the contents will remain.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Dragger_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down))
            {
                ProportionalScaling = true;
            }
            else
            {
                ProportionalScaling = false;
            }

            if (ProportionalScaling)
            {
                ProporsionalResize(e);
            }
            else
            {
                Point p = Util.DeltaTransformFromVisual(e.Delta.Translation, sender as FrameworkElement);
                Resize(p.X, p.Y);
            }
            e.Handled = true;

            if (!Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down))
            {
               // fitFreeFormChildrenToTheirLayouts(); uncomment to make children in collection stretch
            }
        }

        void fitFreeFormChildrenToTheirLayouts()
        {
            var freeFormChild = VisualTreeHelperExtensions.GetFirstDescendantOfType<CollectionFreeformView>(this);
            var parentOfFreeFormChild = freeFormChild != null ? VisualTreeHelperExtensions.GetFirstAncestorOfType<DocumentView>(freeFormChild) : null;
            if (this == parentOfFreeFormChild)
            {   // if this document directly contains a free form child, then initialize its contents to fit its layout.
                freeFormChild?.ManipulationControls?.FitToParent();
            }
        }

        /// <summary>
        /// If the user was resizing proportionally, ends the proportional resizing and 
        /// changes the DraggerButton back to its normal appearance.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Dragger_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            if (ProportionalScaling)
            {
                ProportionalScaling = false;
            }
        }

        /// <summary>
        /// Updates the minimized-view icon from the ViewModel's corresponding IconType array.
        /// </summary>
        private void updateIcon()
        {
            return;
            //if (ViewModel == null) return;
            //// when you want a new icon, you have to add a check for it here!
            //if (ViewModel.IconType == IconTypeEnum.Document)
            //{
            //    xIconImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/doc-icon.png"));
            //}
            //else if (ViewModel.IconType == IconTypeEnum.Collection)
            //{
            //    xIconImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/col-icon.png"));
            //}
            //else if (ViewModel.IconType == IconTypeEnum.Api)
            //{
            //    xIconImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/api-icon.png"));
            //}
        }

        void initDocumentOnDataContext()
        {
            // document type specific styles >> use VERY sparringly
            var docType = ViewModel.DocumentController.DocumentModel.DocumentType;
            if (docType.Type != null)
            {

            }
            else
            {

                ViewModel.DocumentController.DocumentModel.DocumentType.Type = docType.Id.Substring(0, 5);
            }

            // if there is a readable document type, use that as label
            //var sourceBinding = new Binding
            //{
            //    Source = ViewModel.DocumentController.DocumentModel.DocumentType,
            //    Path = new PropertyPath(nameof(ViewModel.DocumentController.DocumentModel.DocumentType.Type)),
            //    Mode = BindingMode.TwoWay,
            //    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            //};
            //xIconLabel.SetBinding(TextBox.TextProperty, sourceBinding);

        }

        /// <summary>
        /// The first time the local DocumentViewModel _vm can be set to the new datacontext
        /// this resets the fields otherwise does nothing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void DocumentView_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            ViewModel = DataContext as DocumentViewModel;
            if (ViewModel != null)
            {
                updateIcon();
                // binds the display title of the document to the back end representation
                var context = new Context(ViewModel.DocumentController);
                var dataDoc = ViewModel.DocumentController.GetDataDocument(context);
                context.AddDocumentContext(dataDoc);
                var keyList = dataDoc.GetDereferencedField<ListController<KeyController>>(KeyStore.PrimaryKeyKey, null);
                var key = KeyStore.TitleKey;
                if (key == null || !(keyList?.Data?.Count() > 0))
                {
                    dataDoc.GetTitleFieldOrSetDefault(context);
                }
                else
                    key = keyList?.Data?.First() as KeyController;

                var Binding = new FieldBinding<TextController>()
                {
                    Mode = BindingMode.TwoWay,
                    Document = dataDoc,
                    Key = key,
                    Context = context
                };
                xTitle.AddFieldBinding(TextBox.TextProperty, Binding);

                xKeyValuePane.SetDataContextToDocumentController(ViewModel.DocumentController);
                ViewModel.SetHasTitle(this.IsLowestSelected);
            }

            //initDocumentOnDataContext();
        }

        private void OuterGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            return;
            if (ViewModel != null)
            {
                // xClipRect.Rect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height);
            }
            // update collapse info
            // collapse to icon view on resize
            //int pad = 1;
            //if (Height < MinHeight + 5)
            //{
            //    xFieldContainer.Visibility = Visibility.Collapsed;
            //    xGradientOverlay.Visibility = Visibility.Collapsed;
            //    xShadowTarget.Visibility = Visibility.Collapsed;
            //    xIcon.Visibility = Visibility.Collapsed;
            //}
            //else
            //    if (Width < MinWidth + pad && Height < MinWidth + xIconLabel.ActualHeight) // MinHeight + xIconLabel.ActualHeight)
            //{
            //    xFieldContainer.Visibility = Visibility.Collapsed;
            //    xGradientOverlay.Visibility = Visibility.Collapsed;
            //    xShadowTarget.Visibility = Visibility.Collapsed;
            //    if (xIcon.Visibility == Visibility.Collapsed)
            //        xIcon.Visibility = Visibility.Visible;
            //    xDragImage.Opacity = 0;
            //    if (_docMenu != null) ViewModel.CloseMenu();
            //    UpdateBinding(true);
            //}
            //else
            //{
            //    xFieldContainer.Visibility = Visibility.Visible;
            //    xGradientOverlay.Visibility = Visibility.Visible;
            //    xShadowTarget.Visibility = Visibility.Visible;
            //    xIcon.Visibility = Visibility.Collapsed;
            //    xDragImage.Opacity = .25;
            //    UpdateBinding(false);
            //}
        }

        /// <summary>
        /// Updates the bindings on the lines when documentview is minimized/vice versa 
        /// </summary>
        /// <param name="becomeSmall"></param>
        private void UpdateBinding(bool becomeSmall)
        {
            var view = OuterGrid.GetFirstAncestorOfType<CollectionView>();
            if (view == null) return; // we can't always assume we're on a collection		

            (view.CurrentView as CollectionFreeformView)?.UpdateBinding(becomeSmall, this);
        }


        #region Menu

        public void DeleteDocument()
        {
            if (ParentCollection != null)
            {
                (ParentCollection.CurrentView as CollectionFreeformView)?.AddToStoryboard(FadeOut, this);
                FadeOut.Begin();
                
                if (useFixedMenu)
                    MainPage.Instance.HideDocumentMenu();
            }
        }

        private void CopyDocument()
        {
            _moveTimer.Stop();
            ParentCollection.ViewModel.AddDocument(ViewModel.DocumentController.GetCopy(null), null);
        }
        private void CopyViewDocument()
        {
            _moveTimer.Stop();
            ParentCollection.ViewModel.AddDocument(ViewModel.DocumentController.GetViewCopy(null), null);
            //xDelegateStatusCanvas.Visibility = ViewModel.DocumentController.HasDelegatesOrPrototype ? Visibility.Visible : Visibility.Collapsed;  // TODO theoretically the binding should take care of this..
        }

        private void CopyDataDocument()
        {
            ParentCollection.ViewModel.AddDocument(ViewModel.DocumentController.GetDataCopy(), null);
        }

        private void KeyValueViewDocument()
        {
            ParentCollection.ViewModel.AddDocument(ViewModel.DocumentController.GetKeyValueAlias(), null);
        }
        private void InstanceDataDocument()
        {
            ParentCollection.ViewModel.AddDocument(ViewModel.DocumentController.GetDataInstance(), null);
        }
        public void ScreenCap()
        {
            Util.ExportAsImage(OuterGrid);
        }

        public void CommandLine()
        {
            FlyoutBase.ShowAttachedFlyout(xFieldContainer);
        }

        public void GetJson()
        {
            Util.ExportAsJson(ViewModel.DocumentController.EnumFields());
        }

        private void FadeOut_Completed(object sender, object e)
        {
            (ParentCollection.CurrentView as CollectionFreeformView)?.DeleteConnections(this);
            ParentCollection.ViewModel.RemoveDocument(ViewModel.DocumentController);
            ViewModel.CloseMenu();
        }

        private void OpenLayout()
        {
            MainPage.Instance.DisplayElement(new InterfaceBuilder(ViewModel.DocumentController), new Point(10, 10), this);
        }

        private void CommandLine_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = sender as TextBox;
            Debug.Assert(tb != null, "tb != null");
            if (!tb.Text.EndsWith("\r"))
                return;
            var docController = (DataContext as DocumentViewModel).DocumentController;
            foreach (var tag in (sender as TextBox).Text.Split('#'))
                if (tag.Contains("="))
                {
                    var eqPos = tag.IndexOfAny(new[] { '=' });
                    var word = tag.Substring(0, eqPos).TrimEnd(' ').TrimStart(' ');
                    var valu = tag.Substring(eqPos + 1, Math.Max(0, tag.Length - eqPos - 1)).TrimEnd(' ', '\r');
                    var key = new KeyController(word, word);
                    foreach (var keyFields in docController.EnumFields())
                        if (keyFields.Key.Name == word)
                        {
                            key = keyFields.Key;
                            break;
                        }

                    //DBTest.ResetCycleDetection();
                    docController.ParseDocField(key, valu);
                }
        }
        #endregion

        #region Activation

        public Rect ClipRect => new Rect();

        public async void OnTapped(object sender, TappedRoutedEventArgs e)
        {
            // handle the event right away before any possible async delays
            if (e != null) e.Handled = true;
            if (!IsSelected)
            {
                await Task.Delay(100); // allows for double-tap

                //Selects it and brings it to the foreground of the canvas, in front of all other documents.
                if (ParentCollection != null)
                {
                    ParentCollection.MaxZ += 1;
                    Canvas.SetZIndex(this.GetFirstAncestorOfType<ContentPresenter>(), ParentCollection.MaxZ);
                    OnSelected();

                    // if the documentview contains a collectionview, assuming that it only has one, set that as selected 
                    this.GetFirstDescendantOfType<CollectionView>()?.CurrentView.OnSelected();
                }
            }
        }

        protected override void OnActivated(bool isSelected)
        {
            ViewModel?.SetSelected(this, isSelected);
            // if we are being deselected
            if (!isSelected)
            {
                colorStoryboardOut.Begin();
                colorStoryboardOut.Completed += delegate
                {
                    xShadowTarget.Fill = Resources["DocumentBackground"] as SolidColorBrush;
                };
                if (useFixedMenu)
                    MainPage.Instance.HideDocumentMenu();
            }
            else
            {
                // update the main toolbar in the overlay canvas
                if (_docMenu == null)
                {
                    SetUpMenu();
                }
                if (_docMenu != null && MainPage.Instance != null)
                {
                    colorStoryboard.Begin();
                    if (useFixedMenu)
                    {
                        MainPage.Instance.SetOptionsMenu(_docMenu);
                        if (MainPage.Instance.MainDocView != this)
                            MainPage.Instance.ShowDocumentMenu();
                    }
                }
            }
        }

        protected override void OnLowestActivated(bool isLowestSelected)
        {
            ViewModel?.SetLowestSelected(this, isLowestSelected);

            if (!IsMainCollection && isLowestSelected)
            {
                if (_docMenu == null)
                {
                    SetUpMenu();
                }
                ViewModel?.OpenMenu();
                _docMenu.AddAndPlayOpenAnimation();
            }
            else
            {
                ViewModel?.CloseMenu();
            }
            ViewModel?.SetHasTitle(isLowestSelected);
        }

        #endregion


        private void DocumentView_OnDragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                e.AcceptedOperation = (DataPackageOperation.Copy | DataPackageOperation.Move) & (e.DataView.RequestedOperation == DataPackageOperation.None ? DataPackageOperation.Copy : e.DataView.RequestedOperation);
            }
        }

        private async void DocumentView_OnRightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            this.OnTapped(sender, new TappedRoutedEventArgs());
            var doc = ViewModel.DocumentController;
            var text = doc.GetField(KeyStore.SystemUriKey) as TextController;
            if (text == null) return;
            var query = await Launcher.QueryAppUriSupportAsync(new Uri(text.Data));

        }

        private void XTitle_OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter || e.Key == VirtualKey.Tab)
            {
                this.Focus(FocusState.Programmatic);
                e.Handled = true;
            }
        }

        private void XTitle_LostFocus(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null)
                return;
            // change the titlekey 
            var titleField = ViewModel.DocumentController.GetDereferencedField<TextController>(KeyStore.TitleKey, null);
            if (titleField == null)
                ViewModel.DocumentController.SetField(KeyStore.TitleKey, new TextController(xTitle.Text), true);
            else ViewModel.DocumentController.GetDereferencedField<TextController>(KeyStore.TitleKey, null).Data = xTitle.Text;
       }

        private void DeepestPrototypeFlyoutItem_OnClick(object sender, RoutedEventArgs e)
        {
            var prototypes = ViewModel.DocumentController.GetAllPrototypes();
            var deepestPrototype = prototypes.First.Value;
            MainPage.Instance.DisplayElement(new InterfaceBuilder(deepestPrototype), new Point(0, 0), this);
            var same = deepestPrototype.Equals(ViewModel.DocumentController);
        }

        private void DocumentView_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            ToFront();
        }

        public void MoveToContainingCollection()
        {
            var rawPointerPosition = Windows.UI.Core.CoreWindow.GetForCurrentThread().PointerPosition;
            var rawWindowBounds = Windows.UI.Core.CoreWindow.GetForCurrentThread().Bounds;
            var pointerPosition = MainPage.Instance.TransformToVisual(this.GetFirstAncestorOfType<ContentPresenter>()).TransformPoint(rawPointerPosition);
            var self = this.ViewModel.DocumentController;
            var opos = new Windows.Foundation.Point(rawPointerPosition.X - rawWindowBounds.Left, rawPointerPosition.Y - rawWindowBounds.Top);
            var collection = this.GetFirstAncestorOfType<CollectionView>();
            if (collection != null)
            {
                var eles = VisualTreeHelper.FindElementsInHostCoordinates(opos, MainPage.Instance);
                foreach (var nestedCollection in eles.Select((el) => el as CollectionView))
                    if (nestedCollection != null)
                    {
                        var nestedCollectionDocument = nestedCollection.ViewModel.ContainerDocument;
                        if (nestedCollectionDocument.Equals(self))
                            continue;
                        if (!nestedCollection.Equals(collection) )
                        {
                            var where = nestedCollection.CurrentView is CollectionFreeformView ?
                                Util.GetCollectionFreeFormPoint((nestedCollection.CurrentView as CollectionFreeformView), opos) :
                                new Point();
                           nestedCollection.ViewModel.AddDocument(ViewModel.DocumentController.GetSameCopy(where), null);
                           collection.ViewModel.RemoveDocument(ViewModel.DocumentController);
                        }
                        break;
                    }
            }
        }
    }

}