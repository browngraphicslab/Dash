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

namespace Dash.Views
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
                var pinnedNode = (PresentationPinnedNode)PinnedNodesListView.SelectedItem;
                NavigateToDocument(pinnedNode.Data.Key);
                ZoomToPinnedScale(pinnedNode.Data.Key, pinnedNode.Data.Value);
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
                    var pinnedNode = (PresentationPinnedNode)PinnedNodesListView.SelectedItem;
                    NavigateToDocument(pinnedNode.Data.Key);
                    ZoomToPinnedScale(pinnedNode.Data.Key, pinnedNode.Data.Value);

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
                var pinnedNode = (PresentationPinnedNode)PinnedNodesListView.SelectedItem;
                var docViewModel = pinnedNode.Data.Key;
                NavigateToDocument(docViewModel);
                ZoomToPinnedScale(pinnedNode.Data.Key, pinnedNode.Data.Value);
            }
        }

        // remove from viewmodel
        private void DeletePin(object sender, RoutedEventArgs e)
        {
            var pinnedNode = (PresentationPinnedNode)(sender as Button).Tag;
            ViewModel.RemovePinFromPinnedNodesCollection(pinnedNode);
        }

        // if we click a node, we should navigate to it immediately. Note that IsItemClickable is always enabled.
        private void PinnedNode_Click(object sender, ItemClickEventArgs e)
        {
            PresentationPinnedNode node = (PresentationPinnedNode) e.ClickedItem;
            var viewModel = node.Data.Key;
            var scale = node.Data.Value; 
            NavigateToDocument(viewModel);
            ZoomToPinnedScale(viewModel, scale);
        }

        // helper method for moving the mainpage screen
        private void NavigateToDocument(DocumentViewModel viewModel)
        {
            MainPage.Instance.NavigateToDocumentInWorkspaceAnimated(viewModel.DocumentController);
        }

        // helper method for zooming the parent collection to the level it was at when the document was pinned
        private void ZoomToPinnedScale(DocumentViewModel viewModel, double scale)
        {
            // main collection for now
            var collection = MainPage.Instance.MainDocView.GetFirstDescendantOfType<CollectionFreeformBase>();
            MainPage.Instance.ZoomToLevel(collection, viewModel, scale);
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
            var pinnedNode = (PresentationPinnedNode)PinnedNodesListView.SelectedItem;
            NavigateToDocument(pinnedNode.Data.Key);
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
    }
}
