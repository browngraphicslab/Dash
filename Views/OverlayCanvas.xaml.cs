using System.Diagnostics;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash 
{
    public sealed partial class OverlayCanvas : UserControl
    {
        public static OverlayCanvas Instance = null;


        public TappedEventHandler OnAddDocumentsTapped,
            OnAddCollectionTapped,
            OnAddAPICreatorTapped,
            OnAddImageTapped,
            OnAddShapeTapped,
            OnOperatorAdd,
            OnToggleEditMode;

        public OverlayCanvas()
        {
            this.InitializeComponent();
            Debug.Assert(Instance == null);
            Instance = this;
        }

        private void AddDocumentsTapped(object sender, TappedRoutedEventArgs e)
        {
            OnAddDocumentsTapped?.Invoke(sender, e);
        }

        private void AddCollectionTapped(object sender, TappedRoutedEventArgs e)
        {
            OnAddCollectionTapped?.Invoke(sender, e);
        }

        private void AddShapeTapped(object sender, TappedRoutedEventArgs e)
        {
            OnAddShapeTapped?.Invoke(sender, e);
        }

        private void image1_Tapped(object sender, TappedRoutedEventArgs e)
        {
            OnAddImageTapped?.Invoke(sender, e);
        }

        private void image_Tapped(object sender, TappedRoutedEventArgs e)
        {
            OnAddAPICreatorTapped?.Invoke(sender, e);
        }

        private void AddOperator_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            OnOperatorAdd?.Invoke(sender, e);
        }

        private void EditorButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            OnToggleEditMode?.Invoke(sender, e);
            //EditButton.Content = FreeformView.MainFreeformView.ViewModel.IsEditorMode ? "STOP" : "EDIT";
        }

        public void OpenInterfaceBuilder(DocumentViewModel vm, Point position)
        {
            var interfaceBuilder = new InterfaceBuilder(vm)
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };
            interfaceBuilder.RenderTransform = new TranslateTransform()
            {
                X = position.X,
                Y = position.Y
            };
            xOuterGrid.Children.Add(interfaceBuilder);
        }
    }
}
