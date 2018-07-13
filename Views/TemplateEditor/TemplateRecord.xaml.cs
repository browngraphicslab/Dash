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
        private DocumentViewModel _dvm;
        private bool _favorited;

        /// <summary>
        ///     creates a record view of a template, useful when viewing template
        ///     boxes in lists or cells. templaterecord should never be created in
        ///     xaml files. this constructor should be the only use case for this.
        ///     passing in null for the first parameter will yield a record that
        ///     displays "No results found".
        /// </summary>
        /// <param name="templateViewModel"></param>
        /// <param name="applier"></param>
        public TemplateRecord(DocumentViewModel templateViewModel, TemplateApplier applier)
        {
            this.InitializeComponent();

            _applier = applier;
            _dvm = templateViewModel;
            _favorited = false;

            // if null is passed into the first parameter
            if (templateViewModel != null)
            {
                // creates and sets a preview for the template
                var dataDoc = templateViewModel.DataDocument;
                var template = templateViewModel.DocumentController.GetViewCopy();
                dataDoc.SetField(KeyStore.DocumentContextKey, template, true);
                template.SetField(KeyStore.ActiveLayoutKey, dataDoc, true);
                var templateView = template.MakeViewUI(null);
                templateView.Loaded += TemplateView_Loaded;
                

                // binds the template title to the title of the template's layout doc
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
                // display default text, used for displaying search results
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
            _applier.Templates.Remove(_dvm);
        }

        private void XApply_OnClick(object sender, RoutedEventArgs e)
        {
            _applier.Apply_Template(this);
        }

        public void showButtons()
        {
            xApply.Visibility = Visibility.Visible;
            xDelete.Visibility = Visibility.Visible;
            xStarButton.Margin = new Thickness(10, 0, 0, 0);

        }


        public void hideButtons()
        {
            xApply.Visibility = Visibility.Collapsed;
            xDelete.Visibility = Visibility.Collapsed;
            xStarButton.Margin = new Thickness(0, 0, 0, 0);

        }

        private void XFavorite_OnClick(object sender, RoutedEventArgs e)
        {
            
            if (xStar.Opacity == 0.5)
            {
                
                xStar.Foreground = new SolidColorBrush(Color.FromArgb(255, 243, 166, 33));
                xStar.Opacity = 1;
                _favorited = true;
            }
            else
            {
                xStar.Opacity = 0.5;
                xStar.Foreground = new SolidColorBrush(Colors.Gray);
                _favorited = false;
            }
        }
    }
}
