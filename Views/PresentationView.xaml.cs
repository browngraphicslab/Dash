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

        public PresentationView()
        {
            this.InitializeComponent();
            DataContext = new PresentationViewModel();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = PinnedNodesListView.SelectedIndex;
            if (selectedIndex != 0)
            {
                PinnedNodesListView.SelectedItem = PinnedNodesListView.Items[selectedIndex - 1];
                NavigateToDocument((DocumentViewModel) PinnedNodesListView.SelectedItem);
            }
        }

        private void PlayStopButton_Click(object sender, RoutedEventArgs e)
        {
            if (PinnedNodesListView.Items.Count != 0)
            {
                if (IsPresentationPlaying)
                {
                    IsPresentationPlaying = false;
                    PlayStopButton.Icon = new SymbolIcon(Symbol.Play);
                    PlayStopButton.Label = "Play";
                    PinnedNodesListView.SelectionMode = ListViewSelectionMode.None;
                }
                else
                {
                    PinnedNodesListView.SelectionMode = ListViewSelectionMode.Single;
                    PinnedNodesListView.SelectedItem = PinnedNodesListView.Items[0];
                    PlayStopButton.Label = "Stop";
                    NavigateToDocument((DocumentViewModel)PinnedNodesListView.SelectedItem);

                    IsPresentationPlaying = true;
                    PlayStopButton.Icon = new SymbolIcon(Symbol.Stop);
                }
            }

            ResetBackNextButtons();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = PinnedNodesListView.SelectedIndex;
            if (selectedIndex != PinnedNodesListView.Items.Count - 1)
            {
                PinnedNodesListView.SelectedItem = PinnedNodesListView.Items[selectedIndex + 1];
                NavigateToDocument((DocumentViewModel) PinnedNodesListView.SelectedItem);
            }
        }

        private void DeletePin(object sender, RoutedEventArgs e)
        {
            ViewModel.RemovePinFromPinnedNodesCollection((sender as Button).Tag as DocumentViewModel);
        }

        private void PinnedNode_Click(object sender, ItemClickEventArgs e)
        {
            DocumentViewModel viewModel = (DocumentViewModel) e.ClickedItem;
            NavigateToDocument(viewModel);
        }

        private void NavigateToDocument(DocumentViewModel viewModel)
        {
            MainPage.Instance.NavigateToDocumentInWorkspaceAnimated(viewModel.DocumentController);
        }

        private void ResetBackNextButtons()
        {
            BackButton.IsEnabled = IsPresentationPlaying;
            NextButton.IsEnabled = IsPresentationPlaying;
            ResetButton.IsEnabled = IsPresentationPlaying;
            BackButton.Opacity = IsPresentationPlaying ? 1 : 0.3;
            NextButton.Opacity = IsPresentationPlaying ? 1 : 0.3;
            ResetButton.Opacity = IsPresentationPlaying ? 1 : 0.3;
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToDocument((DocumentViewModel) PinnedNodesListView.SelectedItem);
        }
    }
}
