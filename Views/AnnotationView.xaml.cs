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

namespace Dash.Views
{
	public sealed partial class AnnotationView : UserControl
	{
		public AnnotationView()
		{
			this.InitializeComponent();
		}

		/**
		 * Method for adding an annotation to the visual stack.
		 */
		public void AddAnnotation()
		{
			//UI 
			TextBox newA = new TextBox();
			newA.Margin = new Thickness(5);
			newA.Height = 50;
			newA.Background = new SolidColorBrush(Colors.White);
			newA.PlaceholderText = "Comment";
			newA.BorderThickness = new Thickness(3.0);
			newA.BorderBrush = new SolidColorBrush(Colors.White);

			xViewPanel.Children.Add(newA);
		}

		/**

		public void CollapseView()
		{
			xViewPanel.Visibility = Visibility.Collapsed;
		}

		public void OpenView()
		{
			xViewPanel.Visibility = Visibility.Visible;
		}

		public void ToggleView()
		{
			xViewPanel.Visibility = (xViewPanel.Visibility == Visibility.Collapsed) ? Visibility.Visible : Visibility.Collapsed;
		}
	*/
		
	}
}
