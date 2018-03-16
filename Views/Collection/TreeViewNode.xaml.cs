using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Toolkit.Uwp.UI;
using Dash.Models.DragModels;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
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

        public static readonly DependencyProperty ContainingDocumentProperty = DependencyProperty.Register(
            "ContainingDocument", typeof(DocumentController), typeof(TreeViewNode), new PropertyMetadata(default(DocumentController)));

        public DocumentController ContainingDocument
        {
            get { return (DocumentController) GetValue(ContainingDocumentProperty); }
            set { SetValue(ContainingDocumentProperty, value); }
        }

        public DocumentViewModel ViewModel => DataContext as DocumentViewModel;

        private bool _isCollection = false;

        public TreeViewNode()
        {
            this.InitializeComponent();
            //XHeader.Background = Application.Current.Resources["WindowsBlue"] as SolidColorBrush;                                                           //TODO get rid of this 
        }
        private DocumentViewModel oldViewModel = null;
        private void TreeViewNode_OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (Equals(args.NewValue, oldViewModel))
            {
                return;
            }
            if (args.NewValue != null)
            {
                var dvm = (DocumentViewModel) args.NewValue;
                oldViewModel = dvm;

                var textBlockBinding = new FieldBinding<TextController>
                {
                    Document = dvm.DataDocument,
                    Key = KeyStore.TitleKey,
                    FallbackValue = "Untitled",
                    Mode = BindingMode.OneWay,
                    Context = new Context(dvm.DocumentController.GetDataDocument(null)),
                    Tag = "TreeViewNode text block binding"
                };

                var textBoxBinding = new FieldBinding<TextController>
                {
                    Document = dvm.DataDocument,
                    Key = KeyStore.TitleKey,
                    FallbackValue = "Untitled",
                    Mode = BindingMode.TwoWay,
                    Context = new Context(dvm.DocumentController.GetDataDocument(null)),
                    FieldAssignmentDereferenceLevel = XamlDereferenceLevel.DontDereference,
                    Tag = "TreeViewNode text box binding"
                };

                //var headerBinding = new FieldBinding<NumberController>
                //{
                //    Document = dvm.DocumentController,
                //    Key = KeyStore.SelectedKey,
                //    FallbackValue = new SolidColorBrush(Colors.Transparent),
                //    Mode = BindingMode.OneWay,
                //    Context = new Context(dvm.DocumentController.GetDataDocument(null)),
                //    Tag = "TreeViewNode header binding", 
                //    Converter = new SelectedToColorConverter()
                //};
                //XHeader.AddFieldBinding(Panel.BackgroundProperty, headerBinding);

                dvm.DataDocument.OnSelectionChanged += (selected) =>
                {
                    Debug.WriteLine("trying to highlight");
                    XHeader.Background = selected ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.Transparent);
                }; 

                //var headerBinding = new Binding
                //{
                //    Source = dvm,
                //    Path = new PropertyPath(nameof(dvm.IsSelected)),
                //    Mode = BindingMode.OneWay,
                //    Converter = new BoolToBrushConverter()
                //};
                //XHeader.SetBinding(Panel.BackgroundProperty, headerBinding);

                var collection = dvm.DocumentController.GetDataDocument(null).GetField(KeyStore.CollectionKey) as ListController<DocumentController>;

                if (collection != null)
                {
                    _isCollection = true;
                    XIconBox.Visibility = Visibility.Visible;
                    if (dvm.LayoutDocument.LayoutName.ToLower().Contains("group"))//tfs: Hack
                    {
                        XIconBox.Symbol = Symbol.Copy;
                    }
                    else //Collection 
                    {
                        XIconBox.Symbol = Symbol.Library;
                    }
                    var collectionViewModel = new CollectionViewModel(
                        new DocumentFieldReference(dvm.DocumentController.GetDataDocument(null).Id,
                            KeyStore.CollectionKey));
                    CollectionTreeView.DataContext =
                        collectionViewModel;
                    CollectionTreeView.ContainingDocument = dvm.DocumentController.GetDataDocument(null);
                    XArrowBlock.Text = (string)Application.Current.Resources["ExpandArrowIcon"];

                    XArrowBlock.Visibility = Visibility.Visible;
                    textBlockBinding.Tag = "TreeViewNodeCol";
                }
                else
                {
                    _isCollection = false;
                    XArrowBlock.Text = "";
                    XArrowBlock.Visibility = Visibility.Collapsed;
                    XIconBox.Visibility = Visibility.Collapsed;
                    CollectionTreeView.DataContext = null;
                    CollectionTreeView.Visibility = Visibility.Collapsed;
                }
                XTextBlock.AddFieldBinding(TextBlock.TextProperty, textBlockBinding);
                XTextBox.AddFieldBinding(TextBox.TextProperty, textBoxBinding);
            }
        }

        private class SelectedToColorConverter : SafeDataToXamlConverter<double, Brush>
        {
            private readonly SolidColorBrush _unselectedBrush = new SolidColorBrush(Colors.Transparent);
            private readonly SolidColorBrush _selectedBrush = new SolidColorBrush(Color.FromArgb(0x35, 0xFF, 0xFF, 0xFF));
            public override Brush ConvertDataToXaml(double data, object parameter = null)
            {
                Debug.WriteLine("data is: " + data); 
                return data == 0 ? _unselectedBrush : _selectedBrush;
            }

            public override double ConvertXamlToData(Brush xaml, object parameter = null)
            {
                throw new NotImplementedException();
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
            e.Handled = true;
            var docToFocus = (DataContext as DocumentViewModel).DocumentController;
            if (_isCollection)
            {
                var docsInGroup = docToFocus.GetDereferencedField<ListController<DocumentController>>(KeyStore.CollectionKey, null);
                if (docsInGroup != null)
                {
                    docToFocus = docsInGroup.TypedData.FirstOrDefault();
                }
            }
            if (! MainPage.Instance.NavigateToDocumentInWorkspaceAnimated(docToFocus))
                MainPage.Instance.SetCurrentWorkspace((DataContext as DocumentViewModel).DocumentController);
            //var col = ContainingDocument?.GetField<ListController<DocumentController>>(KeyStore.CollectionKey);
            //var grp = ContainingDocument?.GetField<ListController<DocumentController>>(KeyStore.GroupingKey);
            //var myDoc = (DataContext as DocumentViewModel).DocumentController;
            //if (col != null && grp != null)
            //{
            //    if (grp.TypedData.Contains(myDoc))
            //    {
            //        if (!col.TypedData.Contains(myDoc))
            //        {
                        
            //        }
            //        else
            //        {
            //            Debug.WriteLine("solo");
            //        }
            //    }
            //    else
            //    {
            //        if (!col.TypedData.Contains(myDoc))
            //        {
            //            Debug.Fail("Error, where are we?");
            //        }
            //        else
            //        {
            //            Debug.WriteLine("Col but no group");
            //        }
            //    }
            //}
            //else
            //{
            //    Debug.WriteLine("Not a group");
            //}
            //MainPage.Instance.SetCurrentWorkspace((DataContext as DocumentViewModel).DocumentController);
        }

        private void XTextBlock_OnDragStarting(UIElement sender, DragStartingEventArgs args)
        {
            args.Data.Properties[nameof(DragDocumentModel)] = new DragDocumentModel((DataContext as DocumentViewModel).DocumentController, true);
            args.AllowedOperations = DataPackageOperation.Link | DataPackageOperation.Copy;
        }

        public void DeleteDocument()
        {
            var collTreeView = this.GetFirstAncestorOfType<TreeViewCollectionNode>();
            var cvm = collTreeView.ViewModel;
            var doc = ViewModel.DocumentController;
            cvm.RemoveDocument(doc);
            cvm.ContainerDocument.GetDereferencedField<ListController<DocumentController>>(KeyStore.CollectionKey, null)
                ?.Remove(doc);//TODO Kind of a hack
        }

        private void Delete_OnClick(object sender, RoutedEventArgs e)
        {
            DeleteDocument();
        }

        private void Rename_OnClick(object sender, RoutedEventArgs e)
        {
            xBorder.Visibility = Visibility.Visible;
            XTextBox.Focus(FocusState.Keyboard);
        }

        private void Open_OnClick(object sender, RoutedEventArgs e)
        {
            MainPage.Instance.SetCurrentWorkspace((DataContext as DocumentViewModel).DocumentController);
        }

        private void XTextBox_OnLostFocus(object sender, RoutedEventArgs e)
        {
            xBorder.Visibility = Visibility.Collapsed;
        }

        private void XTextBox_OnKeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                XTextBlock.Focus(FocusState.Programmatic);
            }
        }

        private void XTextBox_LosingFocus(UIElement sender, LosingFocusEventArgs args)
        {
            if (args.NewFocusedElement == this.GetFirstAncestorOfType<ListViewItem>())
                args.Cancel = true;
        }
    }
}
