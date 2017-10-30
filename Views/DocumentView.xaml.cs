using Dash.Views.Document_Menu;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            ManipulationControls = new ManipulationControls(OuterGrid, true, true);
            ManipulationControls.OnManipulatorTranslatedOrScaled += ManipulatorOnManipulatorTranslatedOrScaled;
            // set bounds
            MinWidth = 100;
            MinHeight = 25;

            Loaded += This_Loaded;
            Unloaded += This_Unloaded;
            this.Drop += OnDrop;
            AddHandler(TappedEvent, new TappedEventHandler(OnTapped), true);
            AddHandler(PointerPressedEvent, new PointerEventHandler(hdlr), true);
        }

        private void hdlr(object sender, PointerRoutedEventArgs e)
        {
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems)) e.Handled = true;
            FileDropHelper.HandleDropOnDocument(this, e);
            ParentCollection?.ViewModel.ChangeIndicationColor(ParentCollection.CurrentView, Colors.Transparent);

            //handles drop from keyvaluepane 
            OnKeyValueDrop(e);
        }
        

        public DocumentController Choose()
        {
            //Selects it and brings it to the foreground of the canvas, in front of all other documents.
            if (ParentCollection != null)
            {
                ParentCollection.MaxZ += 1;
                Canvas.SetZIndex(this.GetFirstAncestorOfType<ContentPresenter>(), ParentCollection.MaxZ);
            }
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
        
        private AddMenuItem treeMenuItem;
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
            if (!IsMainCollection)
            {
                //TabMenu.Instance.SearchView.SearchList.AddToList(Choose, "Get : " + ViewModel.DocumentController.GetTitleFieldOrSetDefault()); // TODO: change this for tab menu
                if (ViewModel.DocumentController.GetField(KeyStore.OperatorKey) == null)
                {
                    // if we don't have a parent to add to then we can't add this to anything
                    if (ParentCollection != null)
                    {
                        // if the tree contains the parent collection
                        if (AddMenu.Instance.ViewToMenuItem.ContainsKey(ParentCollection))
                        {
                            treeMenuItem = new DocumentAddMenuItem(ViewModel.DocumentController.Title, AddMenuTypes.Document, Choose, ViewModel.DocumentController, ContentController<KeyModel>.GetController<KeyController>(DashConstants.KeyStore.TitleKey.Id)); // TODO: change this line for tree menu
                            AddMenu.Instance.AddToMenu(AddMenu.Instance.ViewToMenuItem[ParentCollection],
                                    treeMenuItem);
                        }
                    }
                }
            }
            new ManipulationControls(xKeyValuePane, false, false);
        }

        #region Xaml Styling Methods (used by operator/colelction view)
        private bool isOperator = false;
        private bool addItem = false;
        /// <summary>
        /// Applies custom override styles to the operator view. 
        /// width - the width of a single link node (generally App.xaml defines this, "InputHandleWidth")
        /// </summary>
        public void StyleOperator(double width, string title)
        {
            isOperator = true;
            xShadowTarget.Margin = new Thickness(width,0,width,0);
            xGradientOverlay.Margin = new Thickness(width, 0, width, 0);
            xShadowTarget.Margin = new Thickness(width, 0, width, 0);
            DraggerButton.Margin = new Thickness(0, 0, -(20 - width), -20);
            xTitle.Text = title;
            xTitleIcon.Text = Application.Current.Resources["OperatorIcon"] as string;
            xTitleBorder.Margin = new Thickness(width + xTitleBorder.Margin.Left, xTitleBorder.Margin.Top, width, xTitleBorder.Margin.Bottom);
            if (ParentCollection != null)
            {
                AddMenu.Instance.AddToMenu(AddMenu.Instance.ViewToMenuItem[ParentCollection],
                new AddMenuItem(title, AddMenuTypes.Operator, Choose)); // adds op view to menu
            }
        }
    
        #endregion
        SolidColorBrush bgbrush = (Application.Current.Resources["WindowsBlue"] as SolidColorBrush);
        /// <summary>
        /// Applies custom override styles to the operator view. 
        /// width - the width of a single link node (generally App.xaml defines this, "InputHandleWidth")
        /// </summary>
        public void StyleCollection(CollectionView view)
        {
            addItem = false;
            xTitleIcon.Text = Application.Current.Resources["CollectionIcon"] as string;
            xTitle.Text = "Collection";

            // add item to menu
            if (ParentCollection != null)
                AddMenu.Instance.RemoveFromMenu(AddMenu.Instance.ViewToMenuItem[ParentCollection], treeMenuItem); // removes docview of collection from menu
            
            if (!AddMenu.Instance.ViewToMenuItem.ContainsKey(view))
            {

                TreeMenuNode tree = new TreeMenuNode(MenuDisplayType.Hierarchy);
                tree.HeaderIcon = Application.Current.Resources["CollectionIcon"] as string;
                tree.HeaderLabel = "Collection";

                // if nested, add to parent collection, otherwise add to main collection
                if (!IsMainCollection && ParentCollection != null && AddMenu.Instance.ViewToMenuItem.ContainsKey(ParentCollection))
                {

                    AddMenu.Instance.AddNodeFromCollection(view, tree, AddMenu.Instance.ViewToMenuItem[ParentCollection]);
                } else
                {
                    AddMenu.Instance.AddNodeFromCollection(view, tree, null);
                }
            }
            
            
        }
    
        //}
        #region KEYVALUEPANE
        private static int KeyValPaneWidth = 200;
        private void OpenCloseKeyValuePane()
        {
            if (xKeyValPane.Width == 0)
            {
                xKeyValPane.Width = KeyValPaneWidth;
                ViewModel.Width += KeyValPaneWidth;
                ManipulatorOnManipulatorTranslatedOrScaled(new TransformGroupData(new Point(-KeyValPaneWidth*ManipulationControls.ElementScale, 0), new Point(0, 0), new Point(1, 1)));  
            }
            else
            {
                xKeyValPane.Width = 0;
                ViewModel.Width -= KeyValPaneWidth;
                ManipulatorOnManipulatorTranslatedOrScaled(new TransformGroupData(new Point(KeyValPaneWidth* ManipulationControls.ElementScale, 0), new Point(0, 0), new Point(1, 1)));
            }
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
            if (e.Data.Properties[KeyValuePane.DragPropertyKey] == null) return;

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
            var widthOffset = (layoutDocument.GetField(KeyStore.WidthFieldKey) as NumberFieldModelController).Data / 2;
            var heightOffset = (layoutDocument.GetField(KeyStore.HeightFieldKey) as NumberFieldModelController).Data / 2;
            var positionController = new PointFieldModelController(posInLayoutContainer.X - widthOffset, posInLayoutContainer.Y - heightOffset);
            layoutDocument.SetField(KeyStore.PositionFieldKey, positionController, forceMask: true);

            // add the document to the composite
            var data = ViewModel.LayoutDocument.GetDereferencedField(KeyStore.DataKey, context) as DocumentCollectionFieldModelController;
            data?.AddDocument(layoutDocument); 
        }
        #endregion

        DateTime copyDown = DateTime.MinValue;
        MenuButton copyButton;
        private void SetUpMenu()
        {
            var bgcolor = bgbrush.Color;
            bgcolor.A = 0;
            var red = new Color();
            red.A = 204;
            red.R = 190;
            red.B = 25;
            red.G = 25;

            copyButton = new MenuButton(Symbol.Copy, "Copy", bgcolor, CopyDocument);
            var moveButton = new MenuButton(Symbol.MoveToFolder, "Move", bgcolor, null);
            var copyDataButton = new MenuButton(Symbol.SetTile, "Copy Data", bgcolor, CopyDataDocument);
            var instanceDataButton = new MenuButton(Symbol.SetTile, "Instance", bgcolor, InstanceDataDocument);
            var copyViewButton = new MenuButton(Symbol.SetTile, "Alias", bgcolor, CopyViewDocument);
            var addButton = new MenuButton(Symbol.Add, "Add", bgcolor, OpenCloseKeyValuePane);
            var documentButtons = new List<MenuButton>
            {
                new MenuButton(Symbol.Pictures, "Layout",bgcolor,OpenLayout),
                moveButton,
                copyButton,
               // delegateButton,
               // copyDataButton
                instanceDataButton,
                copyViewButton,
                new MenuButton(Symbol.Delete, "Delete",bgcolor,DeleteDocument)
                //new MenuButton(Symbol.Camera, "ScrCap",bgcolor, ScreenCap),
                //new MenuButton(Symbol.Placeholder, "Commands",bgcolor, CommandLine)
                , addButton
            };
            var moveButtonView = moveButton.View;
            moveButtonView.CanDrag = true;
            moveButton.ManipulationMode = ManipulationModes.All;
            moveButton.ManipulationDelta += (s, e) => e.Handled = true;
            moveButton.ManipulationStarted += (s, e) => e.Handled = true;
            moveButtonView.DragStarting += (s, e) =>
            {
                e.Data.RequestedOperation = DataPackageOperation.Move;
                ViewModel.DocumentView_DragStarting(this, e, ParentCollection.ViewModel);
            };
            moveButtonView.DropCompleted += ButtonView_DropCompleted;
            var copyButtonView = copyButton.View;
            copyButtonView.CanDrag = true;
            copyButton.ManipulationMode = ManipulationModes.All;
            copyButton.ManipulationDelta += (s, e) => e.Handled = true;
            copyButton.ManipulationStarted += (s, e) => e.Handled = true;
            copyButton.AddHandler(PointerPressedEvent, new PointerEventHandler(CopyButton_PointerPressed), true);
            copyButtonView.DragStarting += (s, e) =>
            {
                _moveTimer.Stop();
                e.Data.RequestedOperation = copyButton.Contents.Symbol == Symbol.MoveToFolder ? DataPackageOperation.Move : DataPackageOperation.Copy;
                ViewModel.DocumentView_DragStarting(this, e, ParentCollection.ViewModel);
            };
            copyButtonView.DropCompleted += ButtonView_DropCompleted;
            var copyDataButtonView = copyDataButton.View;
            copyDataButtonView.CanDrag = true;
            copyDataButton.ManipulationMode = ManipulationModes.All;
            copyDataButton.ManipulationDelta += (s, e) => e.Handled = true;
            copyDataButton.ManipulationStarted += (s, e) => e.Handled = true;
            copyDataButtonView.DragStarting += (s, e) =>
            {
                e.Data.RequestedOperation = DataPackageOperation.Link;
                ViewModel.DocumentView_DragStarting(this, e, ParentCollection.ViewModel);
            };
            var instanceDataButtonView = instanceDataButton.View;
            instanceDataButtonView.CanDrag = true;
            instanceDataButtonView.ManipulationMode = ManipulationModes.All;
            instanceDataButtonView.ManipulationDelta += (s, e) => e.Handled = true;
            instanceDataButtonView.ManipulationStarted += (s, e) => e.Handled = true;
            instanceDataButtonView.DragStarting += (s, e) =>
            {
                e.Data.RequestedOperation = DataPackageOperation.Link;
                ViewModel.DocumentView_DragStarting(this, e, ParentCollection.ViewModel);
            };
            var copyViewButtonView = copyViewButton.View;
            copyViewButtonView.CanDrag = true;
            copyViewButton.ManipulationMode = ManipulationModes.All;
            copyViewButton.ManipulationDelta += (s, e) => e.Handled = true;
            copyViewButton.ManipulationStarted += (s, e) => e.Handled = true;
            copyViewButtonView.DragStarting += (s, e) =>
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
            _moveTimer.Interval = new TimeSpan(0, 0, 0, 0, 600);
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
                var layout = ViewModel.DocumentController.GetActiveLayout()?.Data ?? ViewModel.DocumentController;
                ViewModel.GroupTransform = new TransformGroupData(layout.GetDereferencedField<PointFieldModelController>(KeyStore.PositionFieldKey, null).Data, new Point(), currentScaleAmount);
            }
        }

        DispatcherTimer _moveTimer = new DispatcherTimer();

        private void CopyButton_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _moveTimer.Start();
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
            {
                var currentTranslate = ViewModel.GroupTransform.Translate;
                var currentScaleAmount = ViewModel.GroupTransform.ScaleAmount;

                var deltaTranslate = delta.Translate;
                var deltaScaleAmount = delta.ScaleAmount;

                var translate = new Point(currentTranslate.X + deltaTranslate.X, currentTranslate.Y + deltaTranslate.Y);
                //delta does contain information about scale center as is, but it looks much better if you just zoom from middle tbh
                var scaleCenter = new Point(0, 0);
                var scaleAmount = new Point(currentScaleAmount.X * deltaScaleAmount.X, currentScaleAmount.Y * deltaScaleAmount.Y);

                ViewModel.GroupTransform = new TransformGroupData(translate, scaleCenter, scaleAmount);
            }
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
            Point p = Util.DeltaTransformFromVisual(e.Delta.Translation, sender as FrameworkElement);
            Resize(p.X, p.Y);
            e.Handled = true;

            if (!Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down))
            {
                fitFreeFormChildrenToTheirLayouts();
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
            if (ViewModel == null) return;
            // when you want a new icon, you have to add a check for it here!
            if (ViewModel.IconType == IconTypeEnum.Document)
            {
                xIconImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/doc-icon.png"));
            }
            else if (ViewModel.IconType == IconTypeEnum.Collection)
            {
                xIconImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/col-icon.png"));
            }
            else if (ViewModel.IconType == IconTypeEnum.Api)
            {
                xIconImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/api-icon.png"));
            }
        }

        void initDocumentOnDataContext()
        {
            // document type specific styles >> use VERY sparringly
            var docType = ViewModel.DocumentController.Model.DocumentType;
            if (docType.Type != null)
            {

            }
            else
            {

                ViewModel.DocumentController.Model.DocumentType.Type = docType.Id.Substring(0, 5);
            }

            // if there is a readable document type, use that as label
            var sourceBinding = new Binding
            {
                Source = ViewModel.DocumentController.Model.DocumentType,
                Path = new PropertyPath(nameof(ViewModel.DocumentController.Model.DocumentType.Type)),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            xIconLabel.SetBinding(TextBox.TextProperty, sourceBinding);

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
                xKeyValuePane?.SetDataContextToDocumentController(ViewModel?.DocumentController);
        }

        private void OuterGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ViewModel != null)
            {
                xClipRect.Rect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height);
            }
            // update collapse info
            // collapse to icon view on resize
            int pad = 1;
            if (Height < MinHeight + 5)
            {
                xFieldContainer.Visibility = Visibility.Collapsed;
                xIcon.Visibility = Visibility.Collapsed;
            }
            else
                if (Width < MinWidth + pad && Height < MinWidth + xIconLabel.ActualHeight) // MinHeight + xIconLabel.ActualHeight)
            {
                updateIcon();
                xFieldContainer.Visibility = Visibility.Collapsed;
                xIcon.Visibility = Visibility.Visible;
                xDragImage.Opacity = 0;
                if (_docMenu != null) ViewModel.CloseMenu();
                UpdateBinding(true);
            }
            else if (xIcon.Visibility == Visibility.Visible )
            {
                xFieldContainer.Visibility = Visibility.Visible;
                xIcon.Visibility = Visibility.Collapsed;
                xDragImage.Opacity = 1;
                UpdateBinding(false);
            }
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

                AddMenu.Instance.ViewToMenuItem[ParentCollection].Remove(treeMenuItem);

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
            xDelegateStatusCanvas.Visibility = ViewModel.DocumentController.HasDelegatesOrPrototype ? Visibility.Visible : Visibility.Collapsed;  // TODO theoretically the binding should take care of this..
        }

        private void CopyDataDocument()
        {
            ParentCollection.ViewModel.AddDocument(ViewModel.DocumentController.GetDataCopy(), null);
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
            // KBTODO remove itself from tab menu 
            //if (!IsMainCollection) TabMenu.Instance.SearchView.SearchList.RemoveFromList(Choose, "Get : " + ViewModel.DocumentController.GetTitleFieldOrSetDefault());

            (ParentCollection.CurrentView as CollectionFreeformView)?.DeleteConnections(this);
            ParentCollection.ViewModel.RemoveDocument(ViewModel.DocumentController);
            ViewModel.CloseMenu();
        }

        private void OpenLayout()
        {
            MainPage.Instance.DisplayElement(new InterfaceBuilder(ViewModel.DocumentController), new Point(0, 0), this);
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

        public Rect ClipRect { get { return xClipRect.Rect; } }

        public async void OnTapped(object sender, TappedRoutedEventArgs e)
        { 
            if (!IsSelected)
            {
                await Task.Delay(100); // allows for double-tap

                //Selects it and brings it to the foreground of the canvas, in front of all other documents.
                if (ParentCollection != null)
                {
                    ParentCollection.MaxZ += 1;
                    Canvas.SetZIndex(this.GetFirstAncestorOfType<ContentPresenter>(), ParentCollection.MaxZ);

                    if (e != null)
                        e.Handled = true;
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

            if (xIcon.Visibility == Visibility.Collapsed && !IsMainCollection && isLowestSelected)
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
            var doc = ViewModel.DocumentController;
            var text = doc.GetField(KeyStore.SystemUriKey) as TextFieldModelController;
            if (text == null) return;
            var query = await Launcher.QueryAppUriSupportAsync(new Uri(text.Data));
            Debug.WriteLine(query);

        }

        private void XTitle_OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter || e.Key == VirtualKey.Tab)
            {
                e.Handled = true;
            }
        }
    }
    
}