using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Dash.Converters;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class TemplateEditorView : UserControl
    {
        public DocumentController LayoutDocument { get; set; }
        public DocumentController DataDocument { get; set; }

        public ObservableCollection<DocumentController> DocumentControllers {
            get;
            set;
        }

        public TransformGroup VerticalAlignmentRotation
        {
            get
            {
                var transform = new TransformGroup();
                var rotation = new RotateTransform
                {
                    Angle = 90
                };
                var translate = new TranslateTransform
                {
                    X = 60
                };
                transform.Children.Add(rotation);
                transform.Children.Add(translate);
                return transform;
            }
        }

        public ObservableCollection<DocumentViewModel> DocumentViewModels { get; set; }
        public DocumentView SelectedDocument { get; set; }

        private KeyValueTemplatePane _keyValuePane;

		public TemplateEditorView()
	    {
		    this.InitializeComponent();
            DocumentControllers = new ObservableCollection<DocumentController>();
	        DocumentViewModels = new ObservableCollection<DocumentViewModel>();
	    }

	    public void Load()
        {
	        this.UpdatePanes();
            var rect = new Rect(0, 0, 500, 500);
            var rectGeo = new RectangleGeometry {Rect = rect};
            xWorkspace.Clip = rectGeo;
        }

	    public void UpdatePanes()
	    {
			//make key value pane
		    if (xDataPanel.Children.Count == 0)
		    {
				_keyValuePane = new KeyValueTemplatePane(this);
			    xDataPanel.Children.Add(_keyValuePane);
			}

            //make central collection/canvas
	        DocumentControllers =
	            new ObservableCollection<DocumentController>(DataDocument
	                .GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null).TypedData);

			//_workspace.ViewModel.

		    //_workspace.ViewModel.SetCollectionRef(DataDocument, KeyStore.DataKey);

			//LayoutPanel.Children.Add(template);

            //make edit pane
           
        }

        private void XWorkspace_OnLoaded(object sender, RoutedEventArgs e)
        {
            foreach (var doc in DocumentControllers)
            {
                var dvm = new DocumentViewModel(doc.GetViewCopy(new Point(0, 0))) {Editor = this};
                DocumentViewModels.Add(dvm);
                Canvas.SetLeft(dvm.Content, xWorkspace.Width / 2);
                Canvas.SetTop(dvm.Content, xWorkspace.Height / 2);
            }

            xItemsControl.ItemsSource = DocumentViewModels;
        }

        private void XWorkspace_OnUnloaded(object sender, RoutedEventArgs e)
        {

        }

        private void TextButton_OnClick(object sender, RoutedEventArgs e)
        {
            DocumentViewModels.Add();
        }

        private void ImageButton_OnClick(object sender, RoutedEventArgs e)
        {

        }

        private void VideoButton_OnClick(object sender, RoutedEventArgs e)
        {

        }

        private void AudioButton_OnClick(object sender, RoutedEventArgs e)
        {

        }

        private void ToggleButton_OnChecked(object sender, RoutedEventArgs e)
        {

        }

        private void ToggleButton_OnUnchecked(object sender, RoutedEventArgs e)
        {

        }

        private void LeftBorder_OnChecked(object sender, RoutedEventArgs e)
        {

        }

        private void TopBorder_OnChecked(object sender, RoutedEventArgs e)
        {

        }

        private void RightBorder_OnChecked(object sender, RoutedEventArgs e)
        {

        }

        private void BottomBorder_OnChecked(object sender, RoutedEventArgs e)
        {

        }

        private void LeftBorder_OnUnchecked(object sender, RoutedEventArgs e)
        {

        }

        private void TopBorder_OnUnchecked(object sender, RoutedEventArgs e)
        {

        }

        private void RightBorder_OnUnchecked(object sender, RoutedEventArgs e)
        {

        }

        private void BottomBorder_OnUnchecked(object sender, RoutedEventArgs e)
        {

        }
    }
}
