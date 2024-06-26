﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class LinkButton : UserControl
    {

        public string Text
        {
            get => _text;
            set { _text = value; }
        }

        public Color Color
        {
            get => _color;
            set { _color = value; }
        }
        private string _text;
        private Color _color;
        private DocumentDecorations _docdecs;
        private ToolTip _tooltip;
        private DocumentView _documentView;
        private List<DocumentController> _allKeys;

        public List<DocumentController> AllKeys
        {
            get => _allKeys;
            set { _allKeys = value; }
        }

        public LinkButton(DocumentDecorations docdecs, Color color, string text, ToolTip tooltip, DocumentView documentView)
        {
            InitializeComponent();
            _text = text;
            _documentView = documentView;
            _docdecs = docdecs;
            _color = color;
            _tooltip = tooltip;
            xEllipse.Fill = new SolidColorBrush(_color);
            xLinkType.Text = text.Substring(0, 1);

            setLinkKeys();
        }

        private void setLinkKeys()
        {
            var toKeys     = _documentView?.ViewModel.DataDocument.GetLinks(null)?.ToList() ?? new List<DocumentController>();
            var regionKeys = _documentView?.ViewModel.DataDocument.GetRegions()?.SelectMany(r => r.GetDataDocument().GetLinks(null)?.ToList() ?? new List<DocumentController>());
            toKeys.AddRange(regionKeys ?? new List<DocumentController>());
            var matchingLinkDocs = toKeys.Where((k) => {
                var tagName = k.GetDataDocument().GetField<TextController>(KeyStore.LinkTagKey)?.Data;
                return tagName == _text;
            });
            xLinkList.ItemsSource = matchingLinkDocs;
            if (matchingLinkDocs.Count() != 0)
            {
                xLinkMenu.DataContext = new DocumentViewModel(matchingLinkDocs.First());
                xLinkList.SelectedItem = matchingLinkDocs.First();
            }
            _allKeys = matchingLinkDocs.ToList();
        }
        
        private void LinkButton_PointerExited(object sender, PointerRoutedEventArgs args)
        {
            _tooltip.IsOpen = false;
        }

        private void LinkButton_PointerEntered(object sender, PointerRoutedEventArgs args)
        {
            _tooltip.IsOpen = true;
        }

        private bool _doubleTapped = false;
        //follows the link. if there is only one link tagged under this tag, it zooms to that link's position. otherwise it zooms to the last link added with this tag.
        private async void LinkButton_Tapped(object sender, TappedRoutedEventArgs args)
        {
            _doubleTapped = false;
            await Task.Delay(new TimeSpan(0, 0, 0, 0, 100));
            if (!_doubleTapped)
            {
                _tooltip.IsOpen = false;
                OpenFlyout(sender as FrameworkElement, null);
                xLinkList.SelectedItem = null;
            }
            args.Handled = true;
        }

        private void LinkButton_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            _doubleTapped = true;
            if (_tooltip.IsOpen)
            {
                _tooltip.IsOpen = false;
            }
            if (_documentView != null)
            {
                AnnotationManager.FollowRegion(this, _documentView.ViewModel.DocumentController,
                    _documentView.GetAncestorsOfType<ILinkHandler>(), e.GetPosition(_documentView), _text);
            }
            e.Handled = true;
        }

        public void LinkButton_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            OpenFlyout(sender as FrameworkElement, null);
            xLinkList.SelectedItem = null;
            e.Handled = true;   
        }

        public void OpenFlyout(FrameworkElement fwe, DocumentController linkDoc)
        {
            if (_overrideBehavior == LinkBehavior.Annotate) xOverrideAnnotate.IsChecked = true;
            if (_overrideBehavior == LinkBehavior.Dock) xOverrideDock.IsChecked = true;
            if (_overrideBehavior == LinkBehavior.Float) xOverrideFloat.IsChecked = true;
            if (_overrideBehavior == LinkBehavior.Follow) xOverrideFollow.IsChecked = true;
            if (_overrideBehavior == null) xOverrideDefault.IsChecked = true;
            xLinkList.SelectedItem = linkDoc;
            xLinkBehaviorOverride.Visibility = linkDoc != null ? Visibility.Collapsed : Visibility.Visible;
            xLinkList.Visibility = linkDoc != null ? Visibility.Collapsed : Visibility.Visible;
            xLinkDivider.Visibility = linkDoc != null ? Visibility.Collapsed : Visibility.Visible;
            xOverrideBehaviorDivider.Visibility = linkDoc != null ? Visibility.Collapsed : Visibility.Visible;
            xStackPanel.Visibility = linkDoc != null ? Visibility.Collapsed : Visibility.Visible;
            xLinkMenu.Visibility = linkDoc != null ? Visibility.Visible : Visibility.Collapsed;
            if (xLinkList.SelectedIndex != -1)
            {
                xLinkMenu.DataContext = new DocumentViewModel(_allKeys.ElementAt(xLinkList.SelectedIndex));
            }
            FlyoutBase.ShowAttachedFlyout(fwe);
            _tooltip.IsOpen = false;
            //ChangeLinkBehavior(fwe);
        }

        private void LinkButton_DragStarting(UIElement sender, DragStartingEventArgs args)
        {
            if (_documentView != null)
            {
                args.Data.SetDragModel(new DragDocumentModel(_documentView) { DraggedLinkType = _text, DraggingLinkButton = true });
                args.AllowedOperations = DataPackageOperation.Link | DataPackageOperation.Move | DataPackageOperation.Copy;
                args.Data.RequestedOperation = DataPackageOperation.Move | DataPackageOperation.Copy | DataPackageOperation.Link;
            }
        }

        private void TextBoxLoaded(object sender, RoutedEventArgs e)
        {
            var textBox = (sender as TextBox);
            if (textBox?.DataContext is DocumentController link)
            {
                var linkDoc = link.GetDataDocument();
                var srcLinkDoc = linkDoc.GetLinkedDocument(LinkDirection.ToSource);
                var linkedFrom = srcLinkDoc?.GetDataDocument();
                var matches = _documentView.ViewModel.DataDocument.GetRegions()?.Contains(srcLinkDoc) == true || linkedFrom.Equals(_documentView.ViewModel.DataDocument);
                var displayDoc = linkDoc.GetDereferencedField<DocumentController>(matches ? KeyStore.LinkDestinationKey : KeyStore.LinkSourceKey, null);
                if (displayDoc.GetRegionDefinition() is DocumentController parent)
                {
                    displayDoc.SetTitle(parent.Title + " region");
                }

                var fieldBinding = new FieldBinding<TextController>
                {
                    Key = KeyStore.TitleKey,
                    Document = displayDoc,
                    Mode = BindingMode.OneWay,
                    Context = null
                };
                textBox.AddFieldBinding(TextBox.TextProperty, fieldBinding);
            }
        }

        private static LinkBehavior? _overrideBehavior = null;

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (xLinkBehaviorOverride.Visibility == Visibility.Collapsed)
                return;

            if (sender != xOverrideAnnotate) xOverrideAnnotate.IsChecked = false;
            if (sender != xOverrideDock) xOverrideDock.IsChecked = false;
            if (sender != xOverrideFloat) xOverrideFloat.IsChecked = false;
            if (sender != xOverrideFollow) xOverrideFollow.IsChecked = false;
            if (sender != xOverrideDefault) xOverrideDefault.IsChecked = false;
            _overrideBehavior = xOverrideAnnotate.IsChecked == true ? LinkBehavior.Annotate :
                                (xOverrideDock.IsChecked == true ? LinkBehavior.Dock :
                                (xOverrideFloat.IsChecked == true ? LinkBehavior.Float :
                                (xOverrideFollow.IsChecked == true ? (LinkBehavior?)LinkBehavior.Follow : null)));
        }

        private void SymbolIcon_SettingsTapped(object sender, TappedRoutedEventArgs e)
        {
            xLinkBehaviorOverride.Visibility = Visibility.Collapsed;
            var index = xLinkList.Items.IndexOf((sender as SymbolIcon).DataContext);
            xLinkMenu.DataContext = new DocumentViewModel(_allKeys.ElementAt(index));
            xLinkMenu.Visibility = Visibility.Visible;
            xLinkList.Visibility = Visibility.Collapsed;
            xLinkDivider.Visibility = Visibility.Collapsed;
            xOverrideBehaviorDivider.Visibility = Visibility.Collapsed;
            //xStackPanel.Visibility = Visibility.Collapsed;
        }

        private void SymbolIcon_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            (sender as SymbolIcon).Foreground = new SolidColorBrush(Color.FromArgb(255, 109, 168, 222));
        }

        private void SymbolIcon_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            (sender as SymbolIcon).Foreground = new SolidColorBrush(Color.FromArgb(255, 64, 123, 177));
        }

        private void xLinkList_DragItemsStarting(object sender, DragItemsStartingEventArgs args)
        {
            if (args.Items.Count == 1)
            {
                var linkdoc = (DocumentController)args.Items[0];
                args.Data.SetDragModel(new DragDocumentModel(linkdoc) { });
                args.Data.RequestedOperation = DataPackageOperation.Move | DataPackageOperation.Copy | DataPackageOperation.Link;
            }
        }

        private void SymbolIcon_DeleteTapped(object sender, TappedRoutedEventArgs e)
        {
            var index          = xLinkList.Items.IndexOf((sender as SymbolIcon).DataContext);
            var linkDoc        = _allKeys.ElementAt(index);
            var destinationDoc = linkDoc.GetDataDocument().GetField<DocumentController>(KeyStore.LinkDestinationKey);
            var sourceDoc      = linkDoc.GetDataDocument().GetField<DocumentController>(KeyStore.LinkSourceKey);
            destinationDoc.GetDataDocument().GetLinks(KeyStore.LinkFromKey).Remove(linkDoc);
            sourceDoc.GetDataDocument().GetLinks(KeyStore.LinkToKey).Remove(linkDoc);
            setLinkKeys();
            MainPage.Instance.XDocumentDecorations.RebuildMenu();
        }

        private void XLinkList_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            ChangeLinkBehavior(sender);
        }

        private void ChangeLinkBehavior(object sender)
        {
            int index = xLinkList.SelectedIndex;
            if (_allKeys != null && !(sender is SymbolIcon) && index != -1)
            {
                var link = _allKeys.ElementAt(index);
                var srcLinkDoc = link.GetLinkedDocument(LinkDirection.ToSource);
                var linkedFrom = srcLinkDoc?.GetDataDocument();
                var matches = _documentView.ViewModel.DataDocument.GetRegions()?.Contains(srcLinkDoc) == true || linkedFrom.Equals(_documentView.ViewModel.DataDocument);
                AnnotationManager.FollowLink(link,
                    matches ? LinkDirection.ToDestination : LinkDirection.ToSource,
                    _documentView.GetAncestorsOfType<ILinkHandler>(), _overrideBehavior);
            }
        }

        private void XFlyout_OnClosing(FlyoutBase sender, FlyoutBaseClosingEventArgs args)
        {
            MainPage.Instance.XDocumentDecorations.RebuildMenu();
        }
    }
}
