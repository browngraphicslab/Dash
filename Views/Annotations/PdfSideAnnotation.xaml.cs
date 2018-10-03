using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class PdfSideAnnotation : UserControl
    {

        public DocumentViewModel ViewModel
        {
            get => DataContext as DocumentViewModel;
            set => DataContext = value;
        }

        private PdfView _pdf;

        public PdfSideAnnotation(PdfView pdf)
        {
            this.InitializeComponent();
            _pdf = pdf;
            
        }

        public void Annotation()
        {
            _pdf.BottomAnnotationBox.Children.Remove(this);
            _pdf.TopAnnotationBox.Children.Remove(this);
        }

        
    }
}
