using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Windows.UI.Xaml.Shapes;
using System.ComponentModel;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
	public sealed partial class DashColorPicker : UserControl
	{
		public Color SelectedColor
		{
			get { return xColorPicker.Color;}
			set
			{
				xColorPicker.Color = value;
				// Call OnPropertyChanged whenever the property is updated
				SelectedColorChanged?.Invoke(this, value);
			}
		}

		public Flyout ParentFlyout
		{
			get => _flyout;
			set => _flyout = value;
		}

		public Flyout _flyout = null;
		public static readonly ObservableCollection<Color> SavedColors = new ObservableCollection<Color>();
		public event EventHandler<Color> SelectedColorChanged;
			

		public DashColorPicker()
		{
			this.InitializeComponent();
			SelectedColor = xColorPicker.Color;
			xColorPicker.ColorChanged += (sender, args) => SelectedColor = xColorPicker.Color;
		}


		private void XApplyColorButton_OnClick(object sender, RoutedEventArgs e)
		{
			//add chosen color to saved colors
			this.AddSavedColor(xColorPicker.Color);

			//close the flyout it is contained within
			_flyout?.Hide();
		}

		private void XSaveColorButton_OnClick(object sender, RoutedEventArgs e)
		{
			//add chosen color to saved colors
			this.AddSavedColor(xColorPicker.Color);
		}

		public void AddSavedColor(Color color)
		{
			if (SavedColors.Contains(color)) return;

			Rectangle box = new Rectangle();
			box.Width = 25;
			box.Height = 25;
			box.Fill = new SolidColorBrush(color);
			xSavedColorsStack.Children.Insert(0, box);
			//on click should trigger color change to that color
			box.PointerPressed += (s, e) => xColorPicker.Color = color;
			//TODO: add white border for hover
			
			//add to list of saved colors
			SavedColors.Add(color);
			
		}

		public void SetOpacity(byte opacity)
		{
			Color past = xColorPicker.Color;
			xColorPicker.Color = Color.FromArgb(opacity,past.R,past.B,past.G);
		}
	

	}
}
