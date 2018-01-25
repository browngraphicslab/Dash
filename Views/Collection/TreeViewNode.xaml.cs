﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Toolkit.Uwp.UI;

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

        private bool _isCollection = false;

        public TreeViewNode()
        {
            this.InitializeComponent();
        }

        private DocumentViewModel oldViewModel = null;
        private void TreeViewNode_OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (Equals(args.NewValue, oldViewModel))
            {
                return;
            }
            if (oldViewModel != null)
            {
                //TODO remove binding from old document
            }
            if (args.NewValue != null)
            {
                var dvm = (DocumentViewModel) args.NewValue;
                oldViewModel = dvm;

                var fieldBinding = new FieldBinding<TextController>
                {
                    Document = dvm.DocumentController.GetDataDocument(null),
                    Key = KeyStore.TitleKey,
                    FallbackValue = "Untitled",
                    Mode = BindingMode.OneWay,
                    Tag = "TreeViewNode text block binding"
                };

                var collection = dvm.DocumentController.GetDataDocument(null).GetField(KeyStore.GroupingKey) as ListController<DocumentController>;
                if (collection != null)
                {
                    _isCollection = true;
                    var collectionViewModel = new CollectionViewModel(
                        new DocumentFieldReference(dvm.DocumentController.GetDataDocument(null).Id,
                            KeyStore.GroupingKey));
                    collectionViewModel.BindableDocumentViewModels.SortDescriptions.Add(new SortDescription("YPos", SortDirection.Ascending));
                    CollectionTreeView.DataContext =
                        collectionViewModel;
                    CollectionTreeView.ContainingDocument = dvm.DocumentController.GetDataDocument(null);
                    XArrowBlock.Text = (string) Application.Current.Resources["ExpandArrowIcon"];
                    XArrowBlock.Visibility = Visibility.Visible;
                    fieldBinding.Tag = "TreeViewNodeCol";
                }
                else
                {
                    _isCollection = false;
                    XArrowBlock.Text = "";
                    XArrowBlock.Visibility = Visibility.Collapsed;
                    CollectionTreeView.DataContext = null;
                    CollectionTreeView.Visibility = Visibility.Collapsed;
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
            e.Handled = true;
            var docToFocus = (DataContext as DocumentViewModel).DocumentController;
            if (_isCollection)
            {
                var docsInGroup = docToFocus.GetDereferencedField<ListController<DocumentController>>(KeyStore.GroupingKey, null);
                if (docsInGroup != null)
                {
                    docToFocus = docsInGroup.TypedData.FirstOrDefault();
                }
            }
            if (! MainPage.Instance.NavigateToDocumentInWorkspace(docToFocus))
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
            args.Data.Properties["Operator Document"] = (DataContext as DocumentViewModel).DocumentController.GetDataDocument(null);
            args.AllowedOperations = DataPackageOperation.Link | DataPackageOperation.Copy;
        }
    }
}
