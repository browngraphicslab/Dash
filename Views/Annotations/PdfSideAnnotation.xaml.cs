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
