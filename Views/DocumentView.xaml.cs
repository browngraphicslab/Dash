using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using DashShared;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using DocumentMenu;
using Visibility = Windows.UI.Xaml.Visibility;
using Windows.UI.Xaml.Media.Animation;


// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236
namespace Dash
{

    public sealed partial class DocumentView : UserControl
    {
        public string DebugName = "";
        public CollectionView ParentCollection;
        public bool HasCollection { get; set; }
        /// <summary>
        /// Contains methods which allow the document to be moved around a free form canvas
        /// </summary>
        private ManipulationControls manipulator;

        private OverlayMenu _docMenu;
        public DocumentViewModel ViewModel { get; set; }


        public bool ProportionalScaling;
        public ManipulationControls Manipulator { get { return manipulator; } }

        public event OperatorView.IODragEventHandler IODragStarted;
        public event OperatorView.IODragEventHandler IODragEnded;

        public void setBG(SolidColorBrush s) { XGrid.Background = s; }

        public ICollectionView View { get; set; }
        private double startWidth, startHeight; // used for restoring on double click in icon view

        public DocumentView()
        {
            this.InitializeComponent();
            DataContextChanged += DocumentView_DataContextChanged;
            
            // add manipulation code
            this.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY ;
            manipulator = new ManipulationControls(this);
            manipulator.OnManipulatorTranslated += ManipulatorOnOnManipulatorTranslated;

            // set bounds
            MinWidth = 120;
            MinHeight = 96;

            startWidth = Width;
            startHeight = Height;

            //xContextMenu.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

            DraggerButton.Holding += DraggerButtonHolding;
            DraggerButton.ManipulationDelta += Dragger_OnManipulationDelta;
            DraggerButton.ManipulationCompleted += Dragger_ManipulationCompleted;

            Loaded += (s, e) => ParentCollection = this.GetFirstAncestorOfType<CollectionView>();
            DoubleTapped += OnTapped;         
        }

        /// <summary>
        /// Creates the context menu for the document.
        /// </summary>
        private void SetUpMenu()
        {
            Action layout = new Action(OpenLayout);
            Action copy = new Action(CopyDocument);
            Color bgcolor = (Application.Current.Resources["WindowsBlue"] as SolidColorBrush).Color;
            Action delete = new Action(DeleteDocument);
            MenuButton deleteButton = new MenuButton(Symbol.Delete, "Delete", bgcolor, delete);
            var documentButtons = new List<MenuButton>()
            {
                new MenuButton(Symbol.Pictures, "Layout", bgcolor, layout),
                new MenuButton(Symbol.Copy, "Copy", bgcolor, copy),
                deleteButton
            };
            _docMenu = new OverlayMenu(null, documentButtons);
            Binding visibilityBinding = new Binding()
            {
                Source = ViewModel,
                Path = new PropertyPath(nameof(ViewModel.DocMenuVisibility)),
                Mode = BindingMode.OneWay
            };
            _docMenu.SetBinding(OverlayMenu.VisibilityProperty, visibilityBinding);
            xMenuCanvas.Children.Add(_docMenu);
        }

        /// <summary>
        /// Update viewmodel when manipulator moves document
        /// </summary>
        /// <param name="translationDelta"></param>
        private void ManipulatorOnOnManipulatorTranslated(Point translationDelta)
        {
            var documentViewModel = this.DataContext as DocumentViewModel;
            documentViewModel.Position = new Point(documentViewModel.Position.X + translationDelta.X, documentViewModel.Position.Y + translationDelta.Y);
        }

        public DocumentView(DocumentViewModel documentViewModel) : this()
        {
            DataContext = documentViewModel;
            
        }

        /// <summary>
        /// Resizes the CollectionView according to the increments in width and height. 
        /// The CollectionListView vertically resizes corresponding to the change in the size of its cells, so if ProportionalScaling is true and the ListView is being displayed, 
        /// the Grid must change size to accomodate the height of the ListView.
        /// </summary>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        public void Resize(double dx = 0, double dy = 0)
        {
            var dvm = DataContext as DocumentViewModel;
            dvm.Width = ActualWidth + dx;
            dvm.Height = ActualHeight + dy;


            // todo: remove this and replace with binding // debug why x:Bind fails
            Width = ActualWidth + dx;
            Height = ActualHeight + dy;
            
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
        /// Resizes the control based on the user's dragging the DraggerButton.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Dragger_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            Point p = Util.DeltaTransformFromVisual(e.Delta.Translation, sender as FrameworkElement);
            Resize(p.X, p.Y);
            e.Handled = true;
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
        /// Called whenever a field is changed on the document
        /// </summary>
        /// <param name="fieldReference"></param>
        private void DocumentModel_DocumentFieldUpdated(ReferenceFieldModel fieldReference)
        {
            // ResetFields(_vm);
            // Debug.WriteLine("DocumentView.DocumentModel_DocumentFieldUpdated COMMENTED OUT LINE");
        }

        private void updateIcon() {
            // when you want a new icon, you have to add a check for it here!
            if (ViewModel.IconType == IconTypeEnum.Document) {
                xIconImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/doc-icon.png"));
            } else if (ViewModel.IconType == IconTypeEnum.Collection) {
                xIconImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/col-icon.png"));
            } else if (ViewModel.IconType == IconTypeEnum.Api) {
                xIconImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/api-icon.png"));
            }
        }

        /// <summary>
        /// The first time the local DocumentViewModel _vm can be set to the new datacontext
        /// this resets the fields otherwise does nothing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void DocumentView_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            // if _vm has already been set return
            if (ViewModel != null) {
                return;
            }
            ViewModel = DataContext as DocumentViewModel;
            // if new _vm is not correct return
            if (ViewModel == null)
                return;

            // this gets called once when the datacontext is initially set!
            initDocumentOnDataContext();
        }

        /// <summary>
        /// Called when the DataContext of a document is initially set.
        /// </summary>
        void initDocumentOnDataContext() {

            // document type specific styles >> use VERY sparringly
            var docType = ViewModel.DocumentController.DocumentModel.DocumentType;
            if (docType.Type != null) {
                // hide white background & drop shadow on operator views
                if (docType.Type.Equals("operator")) {
                    XGrid.Background = new SolidColorBrush(Colors.Transparent);
                    xBorder.Opacity = 0;
                }
            } else {

                ViewModel.DocumentController.DocumentModel.DocumentType.Type = docType.Id.Substring(0, 5);
            }

            // if there is a readable document type, use that as label
            var sourceBinding = new Binding {
                Source = ViewModel.DocumentController.DocumentModel.DocumentType,
                Path = new PropertyPath(nameof(ViewModel.DocumentController.DocumentModel.DocumentType.Type)),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            xIconLabel.SetBinding(TextBox.TextProperty, sourceBinding);


            Debug.WriteLine("view: " + ViewModel.DocumentController.DocumentModel.DocumentType.Type);

            Debug.WriteLine("text: " + xIconLabel.Text);


            SetUpMenu();
            ViewModel.CloseMenu();

            #region LUKE HACKED THIS TOGETHER MAKE HIM FIX IT

            //ViewModel.PropertyChanged += (o, eventArgs) =>
            //{
            //    if (eventArgs.PropertyName == "IsMoveable")
            //    {
            //        if (ViewModel.IsMoveable)
            //        {
            //            manipulator.AddAllAndHandle();
            //        }
            //        else
            //        {
            //            manipulator.RemoveAllButHandle();
            //        }
            //    }
            //};

            //if (ViewModel.IsMoveable) manipulator.AddAllAndHandle();
            //else manipulator.RemoveAllButHandle();

            #endregion
        }

        private void OuterGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ClipRect.Rect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height);

            // update collapse info
            // collapse to icon view on resize
            int pad = 1;
            if (Width < MinWidth + pad && Height < MinHeight + xIconLabel.ActualHeight) {
                updateIcon();
                XGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                xIcon.Visibility = Windows.UI.Xaml.Visibility.Visible;
                xBorder.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                xDragImage.Opacity = 0;
                DoubleTapped -= OnTapped;
                if (_docMenu != null) ViewModel.CloseMenu();
            } else {
                XGrid.Visibility = Windows.UI.Xaml.Visibility.Visible;
                xIcon.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                xBorder.Visibility = Windows.UI.Xaml.Visibility.Visible;
                xDragImage.Opacity = 1;
                DoubleTapped += OnTapped;
            }
        }
        private void ExpandContract_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e) {
            // if in icon view expand to default size
            if (xIcon.Visibility == Windows.UI.Xaml.Visibility.Visible) {
                Height = 300;
                Width = 300;

                var dvm = DataContext as DocumentViewModel;
                dvm.Width = 300;
                dvm.Height = 300;

                // if in default view, show context menu
            } else {
                Height = MinWidth;
                Width = MinHeight;

                var dvm = DataContext as DocumentViewModel;
                dvm.Width = MinWidth;
                dvm.Height = MinHeight;
            }

            e.Handled = true; // prevent propagating
        }

  #region Menu

        public void OnTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            // expand into full view on doubletap
            if (xIcon.Visibility == Visibility.Visible) {
                ExpandContract_DoubleTapped(sender, e);
                e.Handled = true;
                return;
            }
            if (_docMenu.Visibility == Visibility.Collapsed && !HasCollection) { 
                ViewModel.OpenMenu();
            } else {
                ViewModel.CloseMenu();
            }
            e.Handled = true;
        }

        public void DeleteDocument()
        {
            FadeOut.Begin();
        }

        private void CopyDocument()
        {
            throw new NotImplementedException();
        }

        private void UserControl_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (ParentCollection == null) return;
            ParentCollection.MaxZ += 1;
            Canvas.SetZIndex(this.GetFirstAncestorOfType<ContentPresenter>(), ParentCollection.MaxZ);
        }

        private void XGrid_Tapped(object sender, TappedRoutedEventArgs e) {

        }

        private void FadeOut_Completed(object sender, object e)
        {
            ParentCollection.ViewModel.CollectionFieldModelController.RemoveDocument(ViewModel.DocumentController);
        }

        private void OpenLayout()
        {
            MainPage.Instance.DisplayElement(new InterfaceBuilder(ViewModel), new Point(0,0), this);
        }

        #endregion

    }
}