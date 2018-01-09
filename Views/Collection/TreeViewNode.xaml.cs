using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public ObservableCollection<DocumentViewModel> Children;

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
            if (args.NewValue != null)
            {
                var dvm = (DocumentViewModel) args.NewValue;
                oldViewModel = dvm;

                XTextBlock.AddFieldBinding(TextBlock.TextProperty, new FieldBinding<TextController>
                {
                    Document = dvm.DocumentController,
                    Key = KeyStore.TitleKey,
                    FallbackValue = "Untitled",
                    Mode = BindingMode.OneWay,
                    FieldAssignmentDereferenceLevel = XamlDerefernceLevel.DereferenceToRoot,
                    XamlAssignmentDereferenceLevel =  XamlDerefernceLevel.DereferenceToRoot,
                });

                var collection = dvm.DocumentController.GetDataDocument(null).GetField(KeyStore.CollectionKey) as ListController<DocumentController>;
                if (collection != null)
                {
                    CollectionTreeView.Visibility = Visibility.Visible;
                    CollectionTreeView.DataContext = new CollectionViewModel(dvm.DocumentController, collection);
                }
            }
        }
    }
}
