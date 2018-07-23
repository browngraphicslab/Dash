using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
		//the currently selected color of the color picker
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

		//the flyout that contains the color picker
		//The flyout is created outside this class, but the color picker still needs access to it in order to enable a working "close" button.
		public Flyout ParentFlyout
		{
			get => _flyout;
			set => _flyout = value;
		}

		
		//a list of colors that the user has saved
		public static ObservableCollection<Color> SavedColors;
		public event EventHandler<Color> SelectedColorChanged;
		public Flyout _flyout = null;

		public DashColorPicker()
		{
			this.InitializeComponent();
			SelectedColor = xColorPicker.Color;

		    //add any saved colors to the Recent Colors panel
		    if (SavedColors == null)
		    {
		        SavedColors = new ObservableCollection<Color>();
		    }
		    else
		    {
		        foreach (var color in SavedColors)
		        {
		            this.AddPreviewColorBox(color);
		        }
		    }
            Loaded += DashColorPicker_Loaded;
            Unloaded += DashColorPicker_Unloaded;
		}

        private void DashColorPicker_Loaded(object sender, RoutedEventArgs e)
        {
            xColorPicker.ColorChanged += OnXColorPickerOnColorChanged;
            SavedColors.CollectionChanged += OnSavedColorsChanged;

            // lol this isn't working!!!!
            foreach (var textBox in xColorPicker.GetDescendantsOfType<TextBox>().ToList())
            {
                textBox.IsEnabled = true;
                textBox.IsHitTestVisible = true;
                textBox.IsReadOnly = false;
            }
        }

        private void OnXColorPickerOnColorChanged(ColorPicker sender, ColorChangedEventArgs args)
	    {
	        SelectedColor = xColorPicker.Color;
	    }

	    private void DashColorPicker_Unloaded(object sender, RoutedEventArgs e)
        {
            AddSavedColor(xColorPicker.Color);
            SavedColors.CollectionChanged -= OnSavedColorsChanged;
            xColorPicker.ColorChanged -= OnXColorPickerOnColorChanged;
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
			
			//add to list of saved colors
			SavedColors.Add(color);
		}

		public void AddPreviewColorBox(Color color)
		{
			Rectangle box = new Rectangle();
			box.Width = 25;
			box.Height = 25;
			box.Fill = new SolidColorBrush(color);
			xSavedColorsStack.Children.Insert(0, box);
			//on click should trigger color change to that color
			box.PointerPressed += (s, e) =>
			{
				UndoManager.StartBatch();
				xColorPicker.Color = color;
				UndoManager.EndBatch();
			};
			//TODO: add white border for hover
		}

		//updates Recent Colors panel in all instantiated Dash color pickers
		private void OnSavedColorsChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			foreach (var item in e.NewItems)
			{
				Color color = (Color) item;
				if (color != null) AddPreviewColorBox(color);
			}
		}

		//enables other classes to set the opacity of the currently selected color
		public void SetOpacity(byte opacity)
		{
			Color past = xColorPicker.Color;
			xColorPicker.Color = Color.FromArgb(opacity,past.R,past.B,past.G);
		}
	

	}
}
