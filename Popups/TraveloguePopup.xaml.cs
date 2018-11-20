using System;
using System.Collections.Generic;
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
using Dash.Popups;
using System.Threading.Tasks;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class TraveloguePopup : UserControl, DashPopup
    {
        private List<DocumentController> _collections = new List<DocumentController>();
        private List<string> _tags = new List<string>();

        public TraveloguePopup()
        {
            this.InitializeComponent();

            var events = EventManager.GetEvents();

            var tags = new List<string>();
            var collections = new List<DocumentController>();

            foreach (var eventDoc in events)
            {
                var eventTagsString = eventDoc.GetDataDocument().GetField<TextController>(KeyStore.EventTagsKey).Data;
                var eventTags = eventTagsString.ToUpper().Split(", ");
                foreach (var eventTag in eventTags)
                {
                    if (!tags.Contains(eventTag))
                    {
                        tags.Add(eventTag);
                    }
                }

                var eventCollection =
                    eventDoc.GetDataDocument().GetField<DocumentController>(KeyStore.EventCollectionKey);
                if (!collections.Contains(eventCollection))
                {
                    collections.Add(eventCollection);
                }
            }

            xTagsList.ItemsSource = tags;
            xCollectionsList.ItemsSource = collections;
        }

        private void Popup_OnOpened(object sender, object e)
        {

        }

        private void CollectionsChecked(object sender, RoutedEventArgs e)
        {
            _collections.Add((sender as CheckBox).DataContext as DocumentController);
        }

        private void CollectionsUnchecked(object sender, RoutedEventArgs e)
        {
            _collections.Remove((sender as CheckBox).DataContext as DocumentController);
        }

        private void TagsUnchecked(object sender, RoutedEventArgs e)
        {
            _tags.Remove((sender as CheckBox).DataContext as string);
        }

        private void TagsChecked(object sender, RoutedEventArgs e)
        {
            _tags.Add((sender as CheckBox).DataContext as string);
        }

        public Task<(List<DocumentController>, List<string>)> GetFormResults()
        {
            xLayoutPopup.IsOpen = true;
            var tcs = new TaskCompletionSource<(List<DocumentController>, List<string>)>();
            xConfirmButton.Click += delegate
            {
                tcs.SetResult((_collections, _tags));
                xLayoutPopup.IsOpen = false;
            };
            xCancelButton.Click += delegate
            {
                tcs.SetResult((null, null));
                xLayoutPopup.IsOpen = false;
            };

            return tcs.Task;
        }

        public void SetHorizontalOffset(double offset)
        {
            xLayoutPopup.HorizontalOffset = offset;
        }

        public void SetVerticalOffset(double offset)
        {
            xLayoutPopup.VerticalOffset = offset;
            xLayoutPopup.VerticalAlignment = VerticalAlignment.Top;
        }

        public FrameworkElement Self()
        {
            return this;
        }
    }
}
