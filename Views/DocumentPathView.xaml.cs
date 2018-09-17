using System;
using System.Collections.Generic;
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

namespace Dash
{
    public sealed partial class DocumentPathView : UserControl
    {
        private bool _useDataDocument;
        private DocumentController _document;

        public bool UseDataDocument
        {
            get => _useDataDocument;
            set
            {
                if (_useDataDocument == value)
                {
                    return;
                }

                _useDataDocument = value;
                UpdatePaths();
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
                    Orientation = Orientation.Horizontal
                };

                InitStackPanelWithPath(p, path);

                XExtraPathsStackPanel.Children.Add(p);
            }

            UpdateExpander();
        }

        private void InitStackPanelWithPath(StackPanel panel, List<DocumentController> path)
        {
            panel.Children.Clear();

            foreach (var documentController in path.Skip(1))//Skip the main document (root)
            {
                panel.Children.Add(new TextBlock { Text = "/" });

                TextBlock tb = new TextBlock
                {
                    Text = documentController.Title,
                    DataContext = documentController
                };
                tb.Tapped += TbOnTapped;
                panel.Children.Add(tb);
            }

        }

        private void TbOnTapped(object sender, TappedRoutedEventArgs tappedRoutedEventArgs)
        {
            var tb = (TextBlock) sender;
            var doc = (DocumentController) tb.DataContext;
            Debug.Assert(doc != null);

            SplitFrame.ActiveFrame.OpenDocument(doc);
        }

        private void UIElement_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (XExtraPathsStackPanel.Visibility == Visibility.Collapsed)
            {
                XExtraPathsStackPanel.Visibility = Visibility.Visible;
                XExpandTextBlock.Text = "Hide";
            }
            else
            {
                XExtraPathsStackPanel.Visibility = Visibility.Collapsed;
                XExpandTextBlock.Text = "+ " + XExtraPathsStackPanel.Children.Count;
            }
        }

        private void UpdateExpander()
        {
            if (XExtraPathsStackPanel.Children.Count > 0)
            {
                XExpandTextBlock.Text = XExtraPathsStackPanel.Visibility == Visibility.Visible ? "Hide" : ("+ " + XExtraPathsStackPanel.Children.Count);

                XExpandTextBlock.Visibility = Visibility.Visible;
            }
            else
            {
                XExpandTextBlock.Visibility = Visibility.Collapsed;
            }

        }
    }
}
