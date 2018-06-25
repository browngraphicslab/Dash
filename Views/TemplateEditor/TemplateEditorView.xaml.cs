using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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

        public ObservableCollection<DocumentViewModel> DocumentViewModels { get; set; }

	    private TemplateOptionsPane _optionsPane;
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
            _optionsPane = new TemplateOptionsPane(this);
			xLayoutPanel.Children.Add(_optionsPane);
        }

        private void XWorkspace_OnLoaded(object sender, RoutedEventArgs e)
        {
            foreach (var doc in DocumentControllers)
            {
                var test = new DocumentViewModel(doc.GetViewCopy(new Point(0, 0)));
                DocumentViewModels.Add(test);
                Canvas.SetLeft(test.Content, xWorkspace.Width / 2);
                Canvas.SetTop(test.Content, xWorkspace.Height / 2);
            }

            xItemsControl.ItemsSource = DocumentViewModels;
        }

        private void XWorkspace_OnUnloaded(object sender, RoutedEventArgs e)
        {
        }
    }
}
