﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
using Dash.Annotations;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Views.TreeView
{
    public class ExpandedToArrowConverter : SafeDataToXamlConverter<bool, string>
    {
        public string ExpandedGlyph { get; set; } = "\uF107";
        public string CollapsedGlyph { get; set; } = "\uF105";

        public override string ConvertDataToXaml(bool data, object parameter = null)
        {
            return data ? ExpandedGlyph : CollapsedGlyph;
        }

        public override bool ConvertXamlToData(string xaml, object parameter = null)
        {
            throw new NotImplementedException();
        }
    }

    public sealed partial class TreeViewNode : UserControl, INotifyPropertyChanged
    {
        public DocumentViewModel ViewModel => DataContext as DocumentViewModel;

        private bool _isCollection;
        private bool _isExpanded;

        public static readonly DependencyProperty FilterFuncProperty = DependencyProperty.Register(
            "FilterFunc", typeof(Func<DocumentController>), typeof(TreeViewNode), new PropertyMetadata(default(Func<DocumentController>)));

        public Func<DocumentController> FilterFunc
        {
            get => (Func<DocumentController>)GetValue(FilterFuncProperty);
            set => SetValue(FilterFuncProperty, value);
        }

        public bool IsCollection
        {
            get => _isCollection;
            set
            {
                if (value == _isCollection)
                {
                    return;
                }

                _isCollection = value;
                OnPropertyChanged();
            }
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (value == _isExpanded)
                {
                    return;
                }

                _isExpanded = value;
                OnPropertyChanged();
            }
        }

        public TreeViewNode()
        {
            InitializeComponent();
        }

        private void XArrowBlock_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            IsExpanded = !IsExpanded;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private DocumentViewModel _oldViewModel;
        private void TreeViewNode_OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (Equals(ViewModel, _oldViewModel))
            {
                return;
            }

            _oldViewModel = ViewModel;

            if (ViewModel != null)
            {
                var collectionField = ViewModel.DocumentController.GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null);
                if (collectionField != null)
                {
                    IsCollection = true;
                    IsExpanded = false;
                    XTreeViewList.DataContext = new CollectionViewModel(ViewModel.DocumentController, KeyStore.DataKey);
                }
            }
        }
    }
}
