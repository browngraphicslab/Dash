using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Views.Collection
{
    public sealed partial class TreeViewNode : UserControl
    {

        public static readonly DependencyProperty FilterStringProperty = DependencyProperty.Register(
            "FilterString", typeof(string), typeof(TreeViewNode), new PropertyMetadata(default(string)));

        public string FilterString
        {
            get { return (string) GetValue(FilterStringProperty); }
            set { SetValue(FilterStringProperty, value); }
        }

        private bool _isCollection = false;

        public TreeViewNode()
        {
            this.InitializeComponent();
        }

        private DocumentViewModel oldViewModel = null;
        private void TreeViewNode_OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (oldViewModel != null)
            {
                //TODO remove binding from old document
            }
            if (args.NewValue != null && args.NewValue != oldViewModel)
            {
                var dvm = (DocumentViewModel) args.NewValue;
                oldViewModel = dvm;

                var fieldBinding = new FieldBinding<TextController>
                {
                    Document = dvm.DocumentController.GetDataDocument(null),
                    Key = KeyStore.TitleKey,
                    FallbackValue = "Untitled",
                    Mode = BindingMode.OneWay,
                    FieldAssignmentDereferenceLevel = XamlDerefernceLevel.DereferenceToRoot,
                    XamlAssignmentDereferenceLevel =  XamlDerefernceLevel.DereferenceToRoot,
                    Tag = "TreeViewNode text block binding"
                };

                var collection = dvm.DocumentController.GetDataDocument(null).GetField(KeyStore.GroupingKey) as ListController<DocumentController>;
                if (collection != null)
                {
                    _isCollection = true;
                    CollectionTreeView.DataContext = new CollectionViewModel(new DocumentFieldReference(dvm.DocumentController.GetDataDocument(null).Id, KeyStore.GroupingKey));
                    XArrowBlock.Text = (string) Application.Current.Resources["ExpandArrowIcon"];
                }
                XTextBlock.AddFieldBinding(TextBlock.TextProperty, fieldBinding);
            }
        }

        private void XArrowBlock_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (_isCollection)
            {
                e.Handled = true;
                //Toggle visibility
                if (CollectionTreeView.Visibility == Visibility.Collapsed)
                {
                    CollectionTreeView.Visibility = Visibility.Visible;
                    XArrowBlock.Text = (string) Application.Current.Resources["ContractArrowIcon"];
                }
                else
                {
                    CollectionTreeView.Visibility = Visibility.Collapsed;
                    XArrowBlock.Text = (string) Application.Current.Resources["ExpandArrowIcon"];
                }
            }
        }

        private void XTextBlock_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            MainPage.Instance.SetCurrentWorkspace((DataContext as DocumentViewModel).DocumentController);
        }
    }
}
