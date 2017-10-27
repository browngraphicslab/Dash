using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Dash;
using Dash.Controllers.Operators;
using System.ComponentModel;
using Windows.UI.Input;
using static Windows.ApplicationModel.Core.CoreApplication;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionDBSchemaHeader : UserControl
    {
        public class HeaderViewModel : ViewModelBase
        {
            double _width;
            public KeyController          FieldKey;
            public DocumentController     SchemaDocument;
            public CollectionDBSchemaView SchemaView;

            public override string ToString() { return FieldKey.Name; }
            public double Width
            {
                get => _width;
                set => SetProperty(ref _width, value); 
            }
        }
        public CollectionDBSchemaHeader()
        {
            this.InitializeComponent();
            MainView.CoreWindow.PointerPressed -= CoreWindow_PointerPressed;
            MainView.CoreWindow.PointerPressed += CoreWindow_PointerPressed;
        }
        
        static void CoreWindow_PointerPressed(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.PointerEventArgs args)
        {
            DragModel = null;
        }

        private void SelectTap(object sender, TappedRoutedEventArgs e)
        {
            var viewModel = (DataContext as HeaderViewModel);
            var collection = VisualTreeHelperExtensions.GetFirstAncestorOfType<CollectionView>(this);
            if (collection != null)
            {
                viewModel.SchemaView.Sort(viewModel);
            }
        }

        private void TextBlock_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            var collection = VisualTreeHelperExtensions.GetFirstAncestorOfType<CollectionView>(this);
            if (collection != null)
            {
                var viewModel = (DataContext as HeaderViewModel);
                viewModel.SchemaDocument.SetField(DBFilterOperatorFieldModelController.FilterFieldKey, new TextFieldModelController(viewModel.FieldKey.Name), true);
                collection.SetDBView();
            }
        }

        public class HeaderDragData  
        {
            public ReferenceFieldModelController HeaderColumnReference;
            public KeyController FieldKey;
            public CollectionView.CollectionViewType ViewType;
        }
        public static HeaderDragData DragModel = null;
        
        private void UserControl_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            var viewModel = (DataContext as HeaderViewModel);
            DragModel = new HeaderDragData()
            {
                HeaderColumnReference = new DocumentReferenceFieldController(viewModel.SchemaDocument.GetId(), (viewModel.SchemaView.DataContext as CollectionViewModel).CollectionKey),
                FieldKey = viewModel.FieldKey,
                ViewType = CollectionView.CollectionViewType.DB
            };
            e.Handled = true;
            e.Complete();
        }
    }
}
