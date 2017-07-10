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


// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236
namespace Dash
{

    public sealed partial class DocumentView : UserControl
    {
        public string DebugName = "";
        /// <summary>
        /// Contains methods which allow the document to be moved around a free form canvas
        /// </summary>
        private ManipulationControls manipulator;
        public DocumentViewModel ViewModel { get; set; }


        public bool ProportionalScaling;
        public ManipulationControls Manipulator { get { return manipulator; } }

        public event OperatorView.IODragEventHandler IODragStarted;
        public event OperatorView.IODragEventHandler IODragEnded;

        public ICollectionView View { get; set; }

        public DocumentView()
        {
            this.InitializeComponent();
            DataContextChanged += DocumentView_DataContextChanged;
            
            // add manipulation code
            this.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
            manipulator = new ManipulationControls(this);
            manipulator.OnManipulatorTranslated += ManipulatorOnOnManipulatorTranslated;

            // set bounds
            MinWidth = 200;
            MinHeight = 50;

            DraggerButton.Holding += DraggerButtonHolding;
            DraggerButton.ManipulationDelta += Dragger_OnManipulationDelta;
            DraggerButton.ManipulationCompleted += Dragger_ManipulationCompleted;
            
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

            // reset the fields on the documetn to be those displayed by the documentViewModel
            ResetFields(documentViewModel);
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


            //Width = ActualWidth + dx;
            //Height = ActualHeight + dy;
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

        /// <summary>
        /// Resets the fields on the document to exactly resemble the fields the DocumentViewModel wants to display
        /// </summary>
        /// <param name="documentViewModel"></param>
        public void ResetFields(DocumentViewModel documentViewModel)
        {
            //clear any current children (fields)and then add them over again

            XGrid.Children.Clear();
            var element = documentViewModel.DocumentController.makeViewUI(documentViewModel.DocContextList);

            XGrid.Children.Add(element);
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
        /// Called whenever a field is changed on the document
        /// </summary>
        /// <param name="fieldReference"></param>
        private void DocumentModel_DocumentFieldUpdated(ReferenceFieldModel fieldReference)
        {
            // ResetFields(_vm);
            // Debug.WriteLine("DocumentView.DocumentModel_DocumentFieldUpdated COMMENTED OUT LINE");
        }

        /// <summary>
        /// The first time the local DocumentViewModel _vm can be set to the new datacontext
        /// this resets the fields otherwise does nothing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void DocumentView_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            Debug.WriteLine("datacontext changed");
            // if _vm has already been set return
            if (ViewModel != null) {
                return;
            }
            ViewModel = DataContext as DocumentViewModel;
            // if new _vm is not correct return
            if (ViewModel == null)
                return;

            //ObservableConvertCollection collection = new ObservableConvertCollection(_vm.DataBindingSource, this);
            //DocumentsControl.SetBinding(ItemsControl.ItemsSourceProperty, new Binding
            //{
            //    Source = collection,
            //});
            //collection.CollectionChanged += delegate { Debug.WriteLine("hi"); }; 


            ViewModel.OnLayoutChanged += delegate
            {
                ResetFields(ViewModel);
            };

            // otherwise layout the document according to the _vm
            ResetFields(ViewModel);

            #region LUKE HACKED THIS TOGETHER MAKE HIM FIX IT

            ViewModel.PropertyChanged += (o, eventArgs) =>
            {
                if (eventArgs.PropertyName == "IsMoveable")
                {
                    if (ViewModel.IsMoveable)
                    {
                        manipulator.AddAllAndHandle();
                    }
                    else
                    {
                        manipulator.RemoveAllButHandle();
                    }
                }
            };

            if (ViewModel.IsMoveable) manipulator.AddAllAndHandle();
            else manipulator.RemoveAllButHandle();

            #endregion
        }

        private void OuterGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ClipRect.Rect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height);
        }

        private void XEditButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            var position = e.GetPosition(OverlayCanvas.Instance);
            OverlayCanvas.Instance.OpenInterfaceBuilder(ViewModel, position);
        }
    }
}