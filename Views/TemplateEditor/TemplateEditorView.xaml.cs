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

namespace Dash
{
    public sealed partial class TemplateEditorView : UserControl
    {
        public DocumentController LinkedDocument
        {
            get;
            set;
        }

        public DocumentController LayoutDocument { get; set; }
        public DocumentController DataDocument { get; set; }

	    private TemplateOptionsPane _optionsPane;
	    private KeyValueTemplatePane _keyValuePane;
	    private CollectionFreeformView _workspace;

		public TemplateEditorView()
	    {
		    this.InitializeComponent();
	    }

	    public void Load()
        {
	        this.UpdatePanes();
		}

	    public void UpdatePanes()
	    {
			//make key value pane
		    if (DataPanel.Children.Count == 0)
		    {
				_keyValuePane = new KeyValueTemplatePane(this);
			    DataPanel.Children.Add(_keyValuePane);
			}
            //make central collection/canvas
            _workspace = new CollectionFreeformView();
            _workspace.DataContext = new CollectionViewModel(DataDocument, KeyStore.DataKey);
	        _workspace.ViewModel.AddDocument(LinkedDocument);
            //xWorkspaceOuterGrid.Children.Add(_workspace);

			//template - MAY HAVE TO MOVE OUTSIDE THIS EDITOR & PASS IN INSTEAD
		    var template = new TemplateNote(new Point(), new Size(xWorkspaceOuterGrid.ActualWidth, xWorkspaceOuterGrid.ActualHeight)).Document;
		   //xWorkspaceOuterGrid.Children.Add(template);

			//make edit pane
			_optionsPane = new TemplateOptionsPane(this);
			LayoutPanel.Children.Add(_optionsPane);
	    }
    }
}
