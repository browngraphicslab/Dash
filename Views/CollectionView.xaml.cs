using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionView : UserControl
    {
      
        public CollectionViewModel ViewModel;

        public Grid OuterGrid
        {
            get { return Grid; }
            set { Grid = value; }
        }

        public CollectionView(CollectionViewModel vm)
        {
            this.InitializeComponent();
            DataContext = ViewModel = vm;
            SetEventHandlers();

        }

        public void SetContainerChild(FrameworkElement elem)
        {
            ContainerGrid.Children.Clear();
            ContainerGrid.Children.Add(elem);
        }

        private void SetEventHandlers()
        {
            GridOption.Tapped += ViewModel.GridViewButton_Tapped;
            ListOption.Tapped += ViewModel.ListViewButton_Tapped;
            CloseButton.Tapped += ViewModel.CloseButton_Tapped;
            SelectButton.Tapped += ViewModel.SelectButton_Tapped;

            DeleteSelected.Tapped += ViewModel.DeleteSelected_Tapped;
            Deselect.Tapped += ViewModel.Deselect_Tapped;
            ProportionalDragger.ManipulationDelta += ViewModel.ProportionalDragger_OnManipulationDelta;
            ProportionalDragger.ManipulationCompleted += ViewModel.ProportionalDragger_ManipulationCompleted;
            ProportionalDragger.ManipulationStarted += ViewModel.ProportionalDragger_ManipulationStarted;
            Dragger.ManipulationDelta += ViewModel.Dragger_OnManipulationDelta;
            Dragger.ManipulationStarted += ViewModel.Dragger_ManipulationStarted;
            Dragger.ManipulationCompleted += ViewModel.Dragger_ManipulationCompleted;

            //GridView.DragItemsStarting += ViewModel.GridDragItemsStarting;
            //GridView.DragItemsCompleted += ViewModel.GridDragItemsCompleted;

            

            Grid.DoubleTapped += ViewModel.OuterGrid_DoubleTapped;
        }


        private void GridItem_DragStarting(UIElement sender, DragStartingEventArgs args)
        {
            throw new System.NotImplementedException();
        }
    }
}
