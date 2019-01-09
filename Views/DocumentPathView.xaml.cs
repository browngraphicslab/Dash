using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class DocumentPathView : UserControl
    {
        private bool _useDataDocument = true;
        private DocumentController _document;

        public bool UseDataDocument
        {
            get => _useDataDocument;
            set
            {
                if (_useDataDocument != value)
                {
                    _useDataDocument = value;
                    UpdatePaths();
                }
            }
        }

        public DocumentController Document
        {
            get => _document;
            set
            {
                if (_document == value)
                {
                    return;
                }

                _document = value;
                UpdatePaths();
            }
        }

        public DocumentPathView()
        {
            this.InitializeComponent();
        }

        public void UpdatePaths()
        {
            XMainPathStackPanel.Children.Clear();
            XExtraPathsStackPanel.Children.Clear();
            if (Document == null)
            {
                XMainPathStackPanel.Children.Add(new TextBlock { Text = "No document specified" });
                return;
            }

            var paths = DocumentTree.GetPathsToDocuments(Document, UseDataDocument);
            if (paths.Count == 0)
            {
                return;
            }
            var shortestPath = paths.First();

            InitStackPanelWithPath(XMainPathStackPanel, shortestPath);
            foreach (var path in paths.Skip(1)) //Skip shortest
            {
                var p = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 0
                };

                InitStackPanelWithPath(p, path);

                XExtraPathsStackPanel.Children.Add(p);
            }

            UpdateExpander();
        }

        private void InitStackPanelWithPath(StackPanel panel, List<DocumentController> path)
        {
            panel.Children.Clear();

            var documentControllers = path.Skip(1).ToList();
            for (int index = 0; index < documentControllers.Count; index++)
            {
                var documentController = documentControllers[index];
                var child = index < documentControllers.Count - 1 ? documentControllers[index + 1] : null;
                panel.Children.Add(new TextBlock {Text = "/"});

                var tb = new TextBlock
                {
                    DataContext = (documentController, child),
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    MaxWidth = 200,
                    MaxLines = 1
                };
                tb.AddFieldBinding(TextBlock.TextProperty, new FieldBinding<TextController>
                {
                    Document = documentController,
                    Key = KeyStore.TitleKey,
                    Mode = BindingMode.OneWay
                });
                tb.Tapped += TbOnTapped;
                panel.Children.Add(tb);
            }
        }

        private void TbOnTapped(object sender, TappedRoutedEventArgs tappedRoutedEventArgs)
        {
            var tb = (TextBlock) sender;
            var (doc, child) = ((DocumentController, DocumentController)) tb.DataContext;
            Debug.Assert(doc != null);
            var split = this.GetFirstAncestorOfTypeFast<SplitFrame>() ?? SplitFrame.ActiveFrame;
            if (child != null)
            {
                split.OpenDocument(child, doc);
            }
            else
            {
                split.OpenDocument(doc);
            }
            FlyoutBase.GetAttachedFlyout(LayoutRoot).Hide();
        }

        private void UIElement_OnTapped(object sender, TappedRoutedEventArgs e)
        {
                FlyoutBase.ShowAttachedFlyout(LayoutRoot);
        }

        private void UpdateExpander()
        {
            if (XExtraPathsStackPanel.Children.Count > 0)
            {
                XExpandTextBlock.Text =  "  + " + XExtraPathsStackPanel.Children.Count;

                XExpandTextBlock.Visibility = Visibility.Visible;
            }
            else
            {
                XExpandTextBlock.Visibility = Visibility.Collapsed;
            }

        }
    }
}
