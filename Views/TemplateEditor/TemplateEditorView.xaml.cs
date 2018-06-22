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
        private double xPos;
        private double yPos;

        public DocumentView Document
        {
            get;
            set;
        }

        public DocumentController LayoutDocument { get; set; }
        public DocumentController DataDocument { get; set; }

	    public TemplateEditorView()
	    {
		    this.InitializeComponent();
	    }

	    public void Load()
        {
	        if (Document == null) return;
			xEditorControl.RenderTransform = new TranslateTransform
            {
                X = 10
            };
	        this.UpdatePanes();
		}

	    public void UpdatePanes()
	    {
		    if (Document != null && DataPanel.Children.Count == 0)
		    {
			    DataPanel.Children.Add(new KeyValueTemplatePane(this));
			}
			   
		}
    }
}
