using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using DocumentMenu;
using Visibility = Windows.UI.Xaml.Visibility;

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


        public bool ProportionalScaling { get; set; }
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
            this.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
            manipulator = new ManipulationControls(this);
            manipulator.OnManipulatorTranslated += ManipulatorOnOnManipulatorTranslated;

            // set bounds
            MinWidth = 64;
            MinHeight = 64;

            startWidth = Width;
            startHeight = Height;

            //xContextMenu.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

            DraggerButton.Holding += DraggerButtonHolding;
            DraggerButton.ManipulationDelta += Dragger_OnManipulationDelta;
            DraggerButton.ManipulationCompleted += Dragger_ManipulationCompleted;

            Loaded += (s, e) => ParentCollection = this.GetFirstAncestorOfType<CollectionView>();
            Tapped += OnTapped;
            DoubleTapped += OnDoubleTapped;
        }

        public void ScreenCap()
        {
            Util.ExportAsImage(OuterGrid); 
        }

        public void GetJson()
        {
            Util.ExportAsJson(ViewModel.DocumentController.EnumFields());
        }

        private void SetUpMenu()
        {
            var layout = new Action(OpenLayout);
            var copy = new Action(CopyDocument);
            var delete = new Action(DeleteDocument);
            var makeDelegate = new Action(MakeDelegate);
            var documentButtons = new List<MenuButton>()
            {
                new MenuButton(Symbol.Pictures, "Layout", Colors.LightBlue,layout),
                new MenuButton(Symbol.Copy, "GetCopy", Colors.LightBlue,copy),
                new MenuButton(Symbol.SetTile, "Delegate", Colors.LightBlue, makeDelegate),
                new MenuButton(Symbol.Delete, "Delete", Colors.LightBlue,delete),
                new MenuButton(Symbol.Camera, "ScrCap", Colors.LightBlue, new Action(ScreenCap)),
                new MenuButton(Symbol.Page, "Json", Colors.LightBlue, new Action(GetJson))
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
        /// <param name="delta"></param>
        private void ManipulatorOnOnManipulatorTranslated(TransformGroupData delta)
        {
            var currentTranslate = ViewModel.GroupTransform.Translate;
            var currentScaleAmount = ViewModel.GroupTransform.ScaleAmount;

            var deltaTranslate = delta.Translate;
            var deltaScaleAmount = delta.ScaleAmount;

            var translate = new Point(currentTranslate.X + deltaTranslate.X, currentTranslate.Y + deltaTranslate.Y);
            //delta does contain information about scale center as is, but it looks much better if you just zoom from middle tbh.a
            var scaleCenter = new Point(currentTranslate.X + ActualWidth / 2, currentTranslate.Y + ActualHeight / 2);
            var scaleAmount = new Point(currentScaleAmount.X * deltaScaleAmount.X, currentScaleAmount.Y * deltaScaleAmount.Y);

            ViewModel.GroupTransform = new TransformGroupData(translate, scaleCenter, scaleAmount);
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

        private void updateIcon()
        {
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

        /// <summary>
        /// The first time the local DocumentViewModel _vm can be set to the new datacontext
        /// this resets the fields otherwise does nothing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void DocumentView_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            // if _vm has already been set return
            if (ViewModel != null)
            {
                return;
            }
            ViewModel = DataContext as DocumentViewModel;
            // if new _vm is not correct return
            if (ViewModel == null)
                return;
 
            if (ViewModel.DocumentController.DocumentModel.DocumentType.Type != null && ViewModel.DocumentController.DocumentModel.DocumentType.Type.Equals("operator")) {
                XGrid.Background = new SolidColorBrush(Colors.Transparent);
            }
            Debug.WriteLine(ViewModel.DocumentController.DocumentModel.DocumentType.Type);

            if (ViewModel.DocumentController.DocumentModel.DocumentType.Type != null && 
                ViewModel.DocumentController.DocumentModel.DocumentType.Type.Equals("collection")) {
            }

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
            ViewModel.UpdateGridViewIconGroupTransform(ActualWidth, ActualHeight);
            // update collapse info
            // collapse to icon view on resize
            int pad = 32;
            if (Width < MinWidth + pad && Height < MinHeight + pad)
            {
                updateIcon();
                XGrid.Visibility = Visibility.Collapsed;
                xIcon.Visibility = Visibility.Visible;
                xBorder.Visibility = Visibility.Collapsed;
                Tapped -= OnTapped;
                if (_docMenu != null) ViewModel.CloseMenu();
            } else {
                XGrid.Visibility = Visibility.Visible;
                xIcon.Visibility = Visibility.Collapsed;
                xBorder.Visibility = Visibility.Visible;
                Tapped += OnTapped;
            }
        }

        private void ExpandContract_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e) {
            // if in icon view expand to default size
            if (xIcon.Visibility == Visibility.Visible)
            {
                Height = 300;
                Width = 300;

                var dvm = DataContext as DocumentViewModel;
                dvm.Width = 300;
                dvm.Height = 300;

                // if in default view, show context menu
            }
            else
            {
                Height = MinWidth;
                Width = MinHeight;

                var dvm = DataContext as DocumentViewModel;
                dvm.Width = MinWidth;
                dvm.Height = MinHeight;
            }

            e.Handled = true; // prevent propagating
        }

  #region Menu

        public void OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (_docMenu.Visibility == Visibility.Collapsed && xIcon.Visibility == Visibility.Collapsed && !HasCollection)
                ViewModel.OpenMenu();
            else
                ViewModel.CloseMenu();
            e.Handled = true;
        }

        public void DeleteDocument()
        {
            FadeOut.Begin();
        }

        private void CopyDocument()
        {
            ParentCollection.ViewModel.CollectionFieldModelController.AddDocument(ViewModel.GetCopy());
        }

        private void MakeDelegate()
        {
            ParentCollection.ViewModel.CollectionFieldModelController.AddDocument(ViewModel.GetDelegate());
        }


        private void UserControl_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (ParentCollection == null) return;
            ParentCollection.MaxZ += 1;
            Canvas.SetZIndex(this.GetFirstAncestorOfType<ContentPresenter>(), ParentCollection.MaxZ);
        }

        private void XGrid_Tapped(object sender, TappedRoutedEventArgs e)
        {

        }

        private void OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            // TODO KB made to test doccontextlist, delete later 
            /* 
            ObservableCollection<DocumentController> docList = ViewModel.DocumentController.DocContextList; 
            Debug.WriteLine("count in this list is " + ViewModel.DocumentController.DocContextList.Count); 
            */

            //test exporting as json 
            //Util.ExportAsJson(ViewModel.DocumentController.EnumFields()); 

            //test exporting as image 
            //Util.ExportAsImage(OuterGrid);

            //test sending email 
            //Util.SendEmail("kyu_bin_kwon@brown.edu", "email message", "test");

            e.Handled = true;
        }

        private void FadeOut_Completed(object sender, object e)
        {
            ParentCollection.ViewModel.CollectionFieldModelController.RemoveDocument(ViewModel.DocumentController);
        }

        private void This_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;
            var point = e.GetCurrentPoint(ParentCollection);
            var scaleSign = point.Properties.MouseWheelDelta / 120.0f;
            var scale = scaleSign > 0 ? 1.05 : 1.0 / 1.05;
            var newScale = new Point(ViewModel.GroupTransform.ScaleAmount.X * scale, ViewModel.GroupTransform.ScaleAmount.Y * scale);
            ViewModel.GroupTransform = new TransformGroupData(ViewModel.GroupTransform.Translate,
                                                              ViewModel.GroupTransform.ScaleCenter,
                                                              newScale);
        }

        private void OpenLayout()
        {
            MainPage.Instance.DisplayElement(new InterfaceBuilder(ViewModel), new Point(0,0), this);
        }

        #endregion

    }
}