using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

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

        public LinkButton(DocumentDecorations docdecs, Color color, String text, ToolTip tooltip)
        {
            this.InitializeComponent();
            _text = text;
            _docdecs = docdecs;
            _color = color;
            _tooltip = tooltip;
            xEllipse.Fill = new SolidColorBrush(_color);
            xLinkType.Text = text.Substring(0, 1);
        }

        private void LinkButton_PointerPressed(object sender, PointerRoutedEventArgs args)
        {
            foreach (var doc in _docdecs.SelectedDocs)
            {
                doc.ManipulationMode = ManipulationModes.None;
            }
        }

        private void LinkButton_PointerExited(object sender, PointerRoutedEventArgs args)
        {
            _tooltip.IsOpen = false;
        }

        private void LinkButton_PointerEntered(object sender, PointerRoutedEventArgs args)
        {
            _tooltip.IsOpen = true;
        }

        //follows the link. if there is only one link tagged under this tag, it zooms to that link's position. otherwise it zooms to the last link added with this tag.
        private void LinkButton_Tapped(object sender, TappedRoutedEventArgs args)
        {
            if (_tooltip.IsOpen)
            {
                _tooltip.IsOpen = false;
            }

            var doq = ((sender as FrameworkElement).Tag as Tuple<DocumentView, string>).Item1;
            if (doq != null)
            {
                new AnnotationManager(doq).FollowRegion(doq.ViewModel.DocumentController,
                    doq.GetAncestorsOfType<ILinkHandler>(), args.GetPosition(doq), _text);
            }
        }

        private void LinkButton_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            e.Handled = true;
            _docdecs.CurrentLinks = _docdecs.TagMap[_text];
            Tag tag = null;

            _docdecs.ToggleTagEditor(_docdecs._tagNameDict[_text], sender as FrameworkElement);

            foreach (var child in _docdecs.XTagContainer.Children)
            {
                var actualchild = child as Tag;
                if (actualchild.Text == _text)
                {
                    actualchild.Select();
                }
                else
                {
                    actualchild.Deselect();
                }
            }
        }

        private void LinkButton_DragStarting(UIElement sender, DragStartingEventArgs args)
        {
            DocumentView doq = ((sender as FrameworkElement)?.Tag as Tuple<DocumentView, string>)?.Item1;
            if (doq == null) return;

            args.Data.AddDragModel(new DragDocumentModel(doq.ViewModel.DocumentController, false, doq) { LinkType = _text });
            args.AllowedOperations = DataPackageOperation.Link | DataPackageOperation.Move | DataPackageOperation.Copy;
            args.Data.RequestedOperation = DataPackageOperation.Move | DataPackageOperation.Copy | DataPackageOperation.Link;
            doq.ViewModel.DecorationState = false;
        }
    }
}
