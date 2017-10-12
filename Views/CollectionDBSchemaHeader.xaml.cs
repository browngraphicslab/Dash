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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Views
{
    public sealed partial class CollectionDBSchemaHeader : UserControl
    {
        public class HeaderViewModel : DependencyObject
        {
            public static readonly DependencyProperty WidthProperty = DependencyProperty.Register(
                "Width", typeof(double), typeof(CollectionDBSchemaRecordFieldViewModel), new PropertyMetadata(default(double)));
            public static readonly DependencyProperty SelectedProperty = DependencyProperty.Register(
                "Selected", typeof(bool), typeof(CollectionDBSchemaRecordFieldViewModel), new PropertyMetadata(default(bool)));
            public KeyController FieldKey;
            public bool Selected
            {
                get { return (bool)GetValue(SelectedProperty); }
                set { SetValue(SelectedProperty, value); }
            }
            public double Width
            {
                get { return (double)GetValue(WidthProperty); }
                set { SetValue(WidthProperty, value); }
            }
            public DocumentController SchemaDocument;
            public CollectionDBSchemaView SchemaView;
            public override string ToString()
            {
                return FieldKey.Name;
            }

        }
        public CollectionDBSchemaHeader()
        {
            this.InitializeComponent();
            ManipulationMode = ManipulationModes.All;
            ManipulationStarted += (sender, e) => e.Handled = true;
            ManipulationDelta += (sender, e) => e.Handled = true;
        }

        private void SelectTap(object sender, TappedRoutedEventArgs e)
        {
            var collection = VisualTreeHelperExtensions.GetFirstAncestorOfType<CollectionView>(this);
            if (collection != null)
            {
                var viewModel = (DataContext as HeaderViewModel);
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
    }
}
