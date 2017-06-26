using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Dash.ViewModels;


// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236
namespace Dash
{

    public sealed partial class DocumentView : UserControl
    {
        /// <summary>
        /// Contains methods which allow the document to be moved around a free form canvas
        /// </summary>
        private ManipulationControls manipulator;
        private DocumentViewModel _vm;
        

        public bool ProportionalScaling;
        public ManipulationControls Manipulator { get { return manipulator; } }

        public event OperatorView.IODragEventHandler IODragStarted;
        public event OperatorView.IODragEventHandler IODragEnded;

        public DocumentView()
        {
            this.InitializeComponent();
            DataContextChanged += DocumentView_DataContextChanged;

            this.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
            // add manipulation code
            manipulator = new ManipulationControls(this);

            // set bounds
            MinWidth = 200;
            MinHeight = 200;

            DraggerButton.Holding += DraggerButtonHolding;
            DraggerButton.ManipulationDelta += Dragger_OnManipulationDelta;
            DraggerButton.ManipulationCompleted += Dragger_ManipulationCompleted;
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
            Width = ActualWidth + dx;
            Height = ActualHeight + dy;
            ////Changes width if permissible within size constraints.
            //if (OuterGridWidth + dx > CellSize || dx > 0)
            //{
            //    OuterGridWidth += dx;
            //    if (ProportionalScaling && DisplayingItems())
            //    {
            //        var scaleFactor = OuterGridWidth / (OuterGridWidth - dx);
            //        CellSize = CellSize * scaleFactor;
            //        ScaleDocumentsToFitCell();

            //        //Takes care of proportional height resizing if proportional dragger is used
            //        if (ListViewVisibility == Visibility.Visible)
            //        {
            //            OuterGridHeight = CellSize + 44;

            //        }
            //        else if (GridViewVisibility == Visibility.Visible)
            //        {
            //            var aspectRatio = OuterGridHeight / OuterGridWidth;
            //            OuterGridHeight += dx * aspectRatio;
            //        }
            //    }
            //}

            ////Changes height if permissible within size constraints; makes the height of the Grid track the height of the ListView if the ListView is showing and proportional scaling is allowed.
            //if ((OuterGridHeight + dy > CellSize + 50 || dy > 0) && (!ProportionalScaling || !DisplayingItems()))
            //{
            //    if (DisplayingItems() && ListViewVisibility == Visibility.Visible)
            //    {
            //        OuterGridHeight = CellSize + 44;
            //    }
            //    else
            //    {
            //        OuterGridHeight += dy;
            //    }
            //}

            //SetDimensions();
        }

        ///// <summary>
        ///// Sets the sizes and/or locations of all of the components of the CollectionView correspoding to the size of the Grid.
        ///// </summary>
        //public void SetDimensions()
        //{
        //    ContainerGridHeight = OuterGridHeight - 45;
        //    ContainerGridWidth = OuterGridWidth - 2;

        //    DraggerMargin = new Thickness(OuterGridWidth - 62, OuterGridHeight - 20, 0, 0);
        //    ProportionalDraggerMargin = new Thickness(OuterGridWidth - 22, OuterGridHeight - 20, 0, 0);
        //    CloseButtonMargin = new Thickness(OuterGridWidth - 34, 0, 0, 0);

        //    SelectButtonMargin = new Thickness(0, OuterGridHeight - 23, 0, 0);

        //    BottomBarMargin = new Thickness(0, OuterGridHeight - 21, 0, 0);

        //    DeleteButtonMargin = new Thickness(42, OuterGridHeight - 23, 0, 0);
        //}


        /// <summary>
        /// Resizes all of the documents to fit the CellSize, mainting their aspect ratios.
        /// </summary>
        //private void ScaleDocumentsToFitCell()
        //{
        //    foreach (var dvm in DocumentViewModels)
        //    {
        //        var aspectRatio = dvm.Width / dvm.Height;
        //        if (dvm.Width > dvm.Height)
        //        {
        //            //dvm.Width = CellSize;
        //            //dvm.Height = CellSize / aspectRatio;
        //        }
        //        else
        //        {
        //            //dvm.Height = CellSize;
        //            //dvm.Width = CellSize * aspectRatio;
        //        }
        //    }

        //}


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
        public DocumentView(DocumentViewModel documentViewModel)
        {
            DataContext = documentViewModel;

            // reset the fields on the documetn to be those displayed by the documentViewModel
            ResetFields(documentViewModel);
        }

        /// <summary>
        /// Resets the fields on the document to exactly resemble the fields the DocumentViewModel wants to display
        /// </summary>
        /// <param name="documentViewModel"></param>
        public void ResetFields(DocumentViewModel documentViewModel)
        {
            // clear any current children (fields) and then add them over again
            XGrid.Children.Clear();
            var elements = documentViewModel.GetUiElements(new Rect(0, 0, ActualWidth, ActualHeight));
            foreach (var element in elements)
            {
                //if (!(element is TextBlock))
                    XGrid.Children.Add(element);
            }
        }

        /// <summary>
        /// Hacky way of adding the editable fields to the document in the interface builder
        /// </summary>
        /// <param name="uiElements"></param>
        public void SetUIElements(List<FrameworkElement> uiElements)
        {
            XGrid.Children.Clear();
            foreach (var element in uiElements)
            {
                XGrid.Children.Add(element);
            }
        }


        /// <summary>
        /// Brings up OperationWindow when DocumentView is double tapped 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserControl_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (_vm != null && _vm.DoubleTapEnabled)
            {
                e.Handled = true;
                var window = new OperationWindow(1000, 800, new OperationWindowViewModel(_vm.DocumentModel));

                var center = RenderTransform.TransformPoint(e.GetPosition(this));

                throw new Exception("Operation Window needs to be a document to be added to the MainPage");
                //FreeformView.MainFreeformView.ViewModel.AddElement(window, (float)(center.X - window.Width / 2), (float)(center.Y - window.Height / 2));
                // MainPage.Instance.MainDocument.Children.Add(window);
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

        /// <summary>
        /// Right tapping to bring up the interface builder
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Grid_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var dvm = DataContext as DocumentViewModel;
            Debug.Assert(dvm != null);

            var interfaceBuilder = new InterfaceBuilder(dvm);
            var center = RenderTransform.TransformPoint(e.GetPosition(this));
            throw new Exception("interface builder needs to be a document to be added to the MainPage");
            // FreeformView.MainFreeformView.ViewModel.AddElement(interfaceBuilder, (float)(center.X - interfaceBuilder.Width / 2), (float)(center.Y - interfaceBuilder.Height / 2));
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
            if (_vm != null)
                return;
            _vm = DataContext as DocumentViewModel;
            // if new _vm is not correct return
            if (_vm == null)
                return;

            // otherwise layout the document according to the _vm
            ResetFields(_vm);
            _vm.IODragStarted += reference => IODragStarted?.Invoke(reference);
            _vm.IODragEnded += reference => IODragEnded?.Invoke(reference);
            // Add any methods
            //_vm.DocumentModel.DocumentFieldUpdated -= DocumentModel_DocumentFieldUpdated;
            //_vm.DocumentModel.DocumentFieldUpdated += DocumentModel_DocumentFieldUpdated;
        }

        private void DocumentView_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var parent = this.GetFirstAncestorOfType<Canvas>();
            if (parent == null) return;
            var maxZ = int.MinValue;
            foreach (var child in parent.GetDescendantsOfType<ContentPresenter>())
            {
                var childZ = Canvas.GetZIndex(child);
                if (childZ > maxZ && child.GetFirstDescendantOfType<DocumentView>() != this)
                    maxZ = childZ;
            }
            Canvas.SetZIndex(this.GetFirstAncestorOfType<ContentPresenter>(), maxZ + 1);
        }

        private void OuterGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ClipRect.Rect = new Rect(0,0, e.NewSize.Width, e.NewSize.Height);
        }
    }
}