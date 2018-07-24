using System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
	public sealed partial class DashColorPicker : UserControl
	{
		//the currently selected color of the color picker
		public Color SelectedColor
		{
			get => xColorPicker.Color;
		    set
			{
				xColorPicker.Color = value;
				// Call OnPropertyChanged whenever the property is updated
				SelectedColorChanged?.Invoke(this, value);
			}
		}

	    public Color LastDiscreteSelection { get; set; }
        private DocumentController _dbDoc;

	    public ListController<ColorController> SavedColors
	    {
	        get => _dbDoc?.GetField<ListController<ColorController>>(KeyStore.SavedColorsKey);
            set => _dbDoc?.SetField(KeyStore.SavedColorsKey, value, true);
	    }

	    //the flyout that contains the color picker
		//The flyout is created outside this class, but the color picker still needs access to it in order to enable a working "close" button.
		public Flyout ParentFlyout
		{
			get => Flyout;
			set => Flyout = value;
		}
        
		//a list of colors that the user has saved
		//public static ObservableCollection<Color> SavedColors;
		public event EventHandler<Color> SelectedColorChanged;
		public Flyout Flyout;

		public DashColorPicker()
		{
			InitializeComponent();
			SelectedColor = xColorPicker.Color;
			xColorPicker.ColorChanged += (sender, args) => SelectedColor = xColorPicker.Color;
		
		    Loaded += (sender, args) =>
		    {
		        _dbDoc = MainPage.Instance?.MainDocument?.GetDataDocument();
                if (SavedColors == null) SavedColors = new ListController<ColorController>();

		        SavedColors.FieldModelUpdated -= SavedColorsOnFieldModelUpdated;
                SavedColors.FieldModelUpdated += SavedColorsOnFieldModelUpdated;
		        foreach (ColorController color in SavedColors)
		        {
		            AddPreviewColorBox(color.Data);
		        }
            };

		    Unloaded += (sender, args) =>
		    {
		        SelectedColor = LastDiscreteSelection;
                xSavedColorsStack.Children.Clear();
		    };
		}

	    private void XApplyColorButton_OnClick(object sender, RoutedEventArgs e)
	    {
	        LastDiscreteSelection = SelectedColor;
			//close the flyout it is contained within
			Flyout?.Hide();
		}

		private void XSaveColorButton_OnClick(object sender, RoutedEventArgs e)
		{
            //add chosen color to saved colors
		    LastDiscreteSelection = SelectedColor;
            this.AddSavedColor(xColorPicker.Color);
		}

		public void AddSavedColor(Color color)
		{
		    foreach (ColorController c in SavedColors) { if (c.Data.Equals(color)) return; }

			//add to list of saved colors
			SavedColors.Add(new ColorController(color));
		}

		public void AddPreviewColorBox(Color color)
		{
		    UIElementCollection colors = xSavedColorsStack.Children;

            if (colors.Count == 9) colors.RemoveAt(8);
		    var box = new Rectangle
            {
                Width = 25,
                Height = 25,
                Fill = new SolidColorBrush(color),
                Tag = color
            };
            colors.Insert(0, box);
			//on click should trigger color change to that color
			box.PointerPressed += (s, e) =>
			{
				UndoManager.StartBatch();
				xColorPicker.Color = color;
			    LastDiscreteSelection = color;
				UndoManager.EndBatch();
			};
			//TODO: add white border for hover
		}

	    //updates Recent Colors panel in all instantiated Dash color pickers
        private void SavedColorsOnFieldModelUpdated(FieldControllerBase fieldControllerBase, FieldUpdatedEventArgs e, Context context)
        {
            if (!(e is ListController<ColorController>.ListFieldUpdatedEventArgs args)) return;

            foreach (ColorController item in args.NewItems)
            {
                if (item.Data is Color c) AddPreviewColorBox(c);
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
