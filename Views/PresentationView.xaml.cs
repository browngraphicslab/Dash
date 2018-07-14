using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
    public sealed partial class PresentationView : UserControl
    {
        public PresentationViewModel ViewModel => DataContext as PresentationViewModel;
        public bool IsPresentationPlaying = false;
        private PresentationViewTextBox _textbox;
        private bool _giveTextBoxFocusUponFlyoutClosing = false;

        public PresentationView()
        {
            this.InitializeComponent();
            DataContext = new PresentationViewModel();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = PinnedNodesListView.SelectedIndex;

            // only move back if there is a step to go back to
            if (selectedIndex != 0)
            {
                PinnedNodesListView.SelectedItem = PinnedNodesListView.Items[selectedIndex - 1];
                NavigateToDocument((DocumentViewModel) PinnedNodesListView.SelectedItem);
            }
        }

        private void PlayStopButton_Click(object sender, RoutedEventArgs e)
        {
            // can't play/stop if there's nothing in it
            if (PinnedNodesListView.Items.Count != 0)
            {
                if (IsPresentationPlaying)
                {
                    // if it's currently playing, then it means the user just clicked the stop button. Reset.
                    IsPresentationPlaying = false;
                    PlayStopButton.Icon = new SymbolIcon(Symbol.Play);
                    PlayStopButton.Label = "Play";
                    PinnedNodesListView.SelectionMode = ListViewSelectionMode.None;
                }
                else
                {
                    // zoom to first item in the listview
                    PinnedNodesListView.SelectionMode = ListViewSelectionMode.Single;
                    PinnedNodesListView.SelectedItem = PinnedNodesListView.Items[0];
                    NavigateToDocument((DocumentViewModel)PinnedNodesListView.SelectedItem);

                    IsPresentationPlaying = true;
                    PlayStopButton.Icon = new SymbolIcon(Symbol.Stop);
                    PlayStopButton.Label = "Stop";
                }
            }

            // back/next/reset buttons change appearance depending on state of presentation
            ResetBackNextButtons();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = PinnedNodesListView.SelectedIndex;

            // can only move forward if there's a node to move forward to
            if (selectedIndex != PinnedNodesListView.Items.Count - 1)
            {
                PinnedNodesListView.SelectedItem = PinnedNodesListView.Items[selectedIndex + 1];
                NavigateToDocument((DocumentViewModel) PinnedNodesListView.SelectedItem);
            }
        }

        // remove from viewmodel
        private void DeletePin(object sender, RoutedEventArgs e)
        {
            ViewModel.RemovePinFromPinnedNodesCollection((sender as Button).Tag as DocumentViewModel);
        }

        // if we click a node, we should navigate to it immediately. Note that IsItemClickable is always enabled.
        private void PinnedNode_Click(object sender, ItemClickEventArgs e)
        {
            DocumentViewModel viewModel = (DocumentViewModel) e.ClickedItem;
            NavigateToDocument(viewModel);
        }

        // helper method for moving the mainpage screen
        private void NavigateToDocument(DocumentViewModel viewModel)
        {
            MainPage.Instance.NavigateToDocumentInWorkspaceAnimated(viewModel.DocumentController);
        }

        // these buttons are only enabled when the presentation is playing
        private void ResetBackNextButtons()
        {
            BackButton.IsEnabled = IsPresentationPlaying;
            NextButton.IsEnabled = IsPresentationPlaying;
            ResetButton.IsEnabled = IsPresentationPlaying;
            BackButton.Opacity = IsPresentationPlaying ? 1 : 0.3;
            NextButton.Opacity = IsPresentationPlaying ? 1 : 0.3;
            ResetButton.Opacity = IsPresentationPlaying ? 1 : 0.3;
        }

        // if user strays in middle of presentation, hitting this will bring them back to the selected node
        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToDocument((DocumentViewModel) PinnedNodesListView.SelectedItem);
        }

        private void PinnedNodesListView_OnRightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            ListView listView = (ListView) sender;
            PinnedNodeFlyout.ShowAt(listView, e.GetPosition(listView));
            var source = (FrameworkElement) e.OriginalSource;
            _textbox = source.GetFirstDescendantOfType<PresentationViewTextBox>() ?? source.GetFirstAncestorOfType<PresentationViewTextBox>();
        }

        private void Edit_OnClick(object sender, RoutedEventArgs e)
        {
            _giveTextBoxFocusUponFlyoutClosing = true;
        }

        private void Reset_OnClick(object sender, RoutedEventArgs e)
        {
            _textbox.ResetTitle();
        }

        private void Flyout_Closed(object sender, object e)
        {
            if (_giveTextBoxFocusUponFlyoutClosing)
            {
                _textbox.TriggerEdit();
                _giveTextBoxFocusUponFlyoutClosing = false;
            }
        }

        private double distSqr(Point a, Point b)
        {
            return ((a.Y - b.Y)* (a.Y - b.Y) + (a.X - b.X) * (a.X - b.X));
        }

        private void ShowLinesButton_OnClick(object sender, RoutedEventArgs e)
        {
           //draw lines between members of presentation 
            var docs = PinnedNodesListView.Items?.ToList();
          
            for(var i=0; i < docs?.Count - 1; i++)
            {
                //use bounds to find closest sides on each neighboring doc
                //get midpoitns of every side of both docs
                var docA = (docs[i] as DocumentViewModel).Bounds;
                var docAsides = new List<Point>();
                //the order goes left, top, right, bottom - in regualr UWP fashion
                docAsides.Add(new Point(docA.Left, docA.Y + docA.Height / 2));
                docAsides.Add(new Point(docA.X + docA.Width / 2, docA.Top));
                docAsides.Add(new Point(docA.Right, docA.Y + docA.Height / 2));
                docAsides.Add(new Point(docA.X + docA.Width / 2, docA.Bottom));

                var docB = (docs[i + 1] as DocumentViewModel).Bounds;
                var docBsides = new List<Point>();
                docBsides.Add(new Point(docB.Left, docB.Y + docB.Height / 2));
                docBsides.Add(new Point(docB.X + docB.Width / 2, docB.Top));
                docBsides.Add(new Point(docB.Right, docB.Y + docB.Height / 2));
                docBsides.Add(new Point(docB.X + docB.Width / 2, docB.Bottom));

                var minDist = Double.PositiveInfinity;
                Point startPoint;
                Point endPoint;
                //TODO: get control points as some amount out from side - maybe have docAsides and B have a pair of points instead
                Point startControlPt;
                Point endControlPt;


                foreach (var aside in docAsides)
                {
                    foreach (var bside in docBsides)
                    {
                        var dist = distSqr(aside, bside);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            startPoint = aside;
                            endPoint = bside;
                        }
                    }
                }

                var segment = new BezierSegment() { Point3=endPoint};
                //MainPage.Instance.xCanvas.Children.Add();
            }
        }
    }
}
