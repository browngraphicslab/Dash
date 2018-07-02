using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Sources.FilePicker.PDF {
    public sealed partial class PDFViewer : UserControl, INotifyPropertyChanged {
        public event PropertyChangedEventHandler PropertyChanged;

        public Uri Source {
            get => (Uri)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(Uri), typeof(PDFViewer),
                new PropertyMetadata(null, OnSourceChanged));

        public bool IsZoomEnabled {
            get { return (bool)GetValue(IsZoomEnabledProperty); }
            set { SetValue(IsZoomEnabledProperty, value); }
        }

        public static readonly DependencyProperty IsZoomEnabledProperty =
            DependencyProperty.Register("IsZoomEnabled", typeof(bool), typeof(PDFViewer),
                new PropertyMetadata(true, OnIsZoomEnabledChanged));

        internal ZoomMode ZoomMode {
            get { return IsZoomEnabled ? ZoomMode.Enabled : ZoomMode.Disabled; }
        }

        public bool AutoLoad { get; set; }

        internal ObservableCollection<BitmapImage> PdfPages {
            get;
            set;
        } = new ObservableCollection<BitmapImage>();

        public PDFViewer() {
            this.Background = new SolidColorBrush(Colors.DarkGray);
            this.InitializeComponent();
        }

        private static void OnIsZoomEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            ((PDFViewer)d).OnIsZoomEnabledChanged();
        }

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            ((PDFViewer)d).OnSourceChanged();
        }

        private void OnIsZoomEnabledChanged() {
            OnPropertyChanged(nameof(ZoomMode));
        }

        private async void OnSourceChanged() {
            if (AutoLoad) {
                await LoadAsync();
            }
        }

        public async Task LoadAsync() {
            if (Source == null) {
                PdfPages.Clear();
            } else {
                if (Source.IsFile) {
                    await LoadFromLocalAsync();
                }  else {
                    throw new ArgumentException($"Source '{Source.ToString()}' could not be recognized!");
                }
            }
        }
        

        private async Task LoadFromLocalAsync() {
            StorageFile f = await
                StorageFile.GetFileFromApplicationUriAsync(Source);
            PdfDocument doc = await PdfDocument.LoadFromFileAsync(f);

            Load(doc);
        }

        public async void Load(PdfDocument pdfDoc) {
            PdfPages.Clear();

            for (uint i = 0; i < pdfDoc.PageCount; i++) {
                var image = new BitmapImage();

                var page = pdfDoc.GetPage(i);


                using (var stream = new InMemoryRandomAccessStream()) {
                    await page.RenderToStreamAsync(stream);
                    await image.SetSourceAsync(stream);
                }

                PdfPages.Add(image);
            }
        }

        public void OnPropertyChanged([CallerMemberName]string property = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        private void Image_DragStarting(UIElement sender, DragStartingEventArgs args) {
             args.Data.Properties.Add("image", sender);
        }
    }
}
