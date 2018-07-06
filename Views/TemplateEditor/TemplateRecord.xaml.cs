using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
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
    public sealed partial class TemplateRecord : UserControl
    {
        public string Title => xTemplateTitle.Text;

        public DocumentViewModel TemplateViewModel { get; private set; }
        private TemplateApplier _applier;

        public TemplateRecord(DocumentViewModel templateViewModel, TemplateApplier applier)
        {
            this.InitializeComponent();

            _applier = applier;

            if (templateViewModel != null)
            {
                var dataDoc = templateViewModel.DataDocument;
                var template = templateViewModel.DocumentController.GetViewCopy();
                dataDoc.SetField(KeyStore.DocumentContextKey, template, true);
                template.SetField(KeyStore.ActiveLayoutKey, dataDoc, true);
                var templateView = template.MakeViewUI(null);
                templateView.Loaded += TemplateView_Loaded;
                xPanel.Children.Add(templateView);
                var binding = new FieldBinding<TextController>()
                {
                    Mode = BindingMode.TwoWay,
                    Document = templateViewModel.LayoutDocument,
                    Key = KeyStore.TitleKey
                };
                xTemplateTitle.AddFieldBinding(TextBlock.TextProperty, binding);
                TemplateViewModel = templateViewModel;
            }
            else
            {
                xTemplateTitle.Text = "No results found";
            }
        }

        private void TemplateView_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Grid templateView)
            {
                // TODO: Better way of doing this? -sy
                templateView.RenderTransform = new ScaleTransform
                {
                    CenterX = 0,
                    CenterY = 0,
                    ScaleX = 0.5,
                    ScaleY = 0.5
                };
            }
        }

        private void xDelete_OnClick(object sender, RoutedEventArgs e)
        {
            _applier.TemplateRecords.Remove(this);
        }

        private void XApply_OnClick(object sender, RoutedEventArgs e)
        {
            _applier.Apply_Template(this);
        }

        public void showButtons()
        {
            xApply.Visibility = Visibility.Visible;
            xDelete.Visibility = Visibility.Visible;
        }


        public void hideButtons()
        {
            xApply.Visibility = Visibility.Collapsed;
            xDelete.Visibility = Visibility.Collapsed;
        }
    }
}
