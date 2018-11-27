using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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
        }

        private void LinkButton_PointerPressed(object sender, PointerRoutedEventArgs args)
        {
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
            if (_documentView != null)
            {
                new AnnotationManager(_documentView).FollowRegion(_documentView, _documentView.ViewModel.DocumentController,
                    _documentView.GetAncestorsOfType<ILinkHandler>(), args.GetPosition(_documentView), _text);
            }
            args.Handled = true;
        }

        private void LinkButton_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            _tooltip.IsOpen = false;
            e.Handled = true;
            _docdecs.CurrentLinks = _docdecs.TagMap[_text];

            _docdecs.ToggleTagEditor(_docdecs._tagNameDict[_text], sender as FrameworkElement);
            if (_docdecs.CurrentLinks.Count == 1)
                foreach (var actualchild in _docdecs.XTagContainer.Children.OfType<Tag>())
                {
                    if (actualchild.Text == _text)
                    {
                        actualchild.Select();
                    } else
                    {
                        actualchild.Deselect();
                    }
                }
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
    }
}
