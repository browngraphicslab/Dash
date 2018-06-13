﻿using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class GroupingSubtoolbar : ICommandBarBased
    {
        //INSTANCE VARIABLES
        private DocumentController _currentDocController;
        private string _currentColorString;

        //ORIENTATION property registration and declaration
        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
            "Orientation", typeof(Orientation), typeof(GroupingSubtoolbar), new PropertyMetadata(default(Orientation)));

        public Orientation Orientation
        {
            get => (Orientation) GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value); 
        }

        //CONSTRUCTOR
        public GroupingSubtoolbar()
        {
            this.InitializeComponent();

            //Initial values
            xOpacitySlider.Value = 128; //Effectively an opacity of 0.5
            _currentColorString = "80FF0000"; //Red with an opacity of 0.5

            FormatDropdownMenu();

            //Sets orientation binding when the actual command bar UI element is loaded. 
            //Potential bug fix: if visibility of command bar is collapsed, will never load and will fail to add event handler
            xGroupCommandbar.Loaded += delegate
            {
                var sp = xGroupCommandbar.GetFirstDescendantOfType<StackPanel>();
                sp.SetBinding(StackPanel.OrientationProperty, new Binding
                {
                    Source = this,
                    Path = new PropertyPath(nameof(Orientation)),
                    Mode = BindingMode.OneWay
                });
                Visibility = Visibility.Collapsed;
            };
        }

        //SETUP AND HELPER METHODS

        /*
         * Draws on ToolbarConstants to set width, height and margin of the ComboBox (shape selection dropdown menu)
         */
        private void FormatDropdownMenu()
        {
            xShapeOptionsDropdown.Width = ToolbarConstants.ComboBoxWidth;
            xShapeOptionsDropdown.Height = ToolbarConstants.ComboBoxHeight;
            xShapeOptionsDropdown.Margin = new Thickness(ToolbarConstants.ComboBoxMarginOpen);
        }

        /*
         * Ensures current color reflects desired opacity and then updates the appropriate bindings for...
         */
        private void UpdateColor()
        {
            _currentColorString = GetColorWithUpdatedOpacity();
            //...shape's background color
            _currentDocController?.GetDataDocument().SetField<TextController>(KeyStore.BackgroundColorKey, _currentColorString, true);
            //...indirectly, the shape's opacity
            _currentDocController?.GetDataDocument().SetField<TextController>(KeyStore.OpacitySliderValueKey, xOpacitySlider.Value.ToString("G"), true);
        }

        /*
         * Runs the current ARGB color through the "filter" of the current opacity slider value by replacing default alpha prefix with the desired substitution
         */
        private string GetColorWithUpdatedOpacity()
        {
            if (_currentColorString == null) return "#80000000"; //A fallback during startup (edge case) where current color string is null
            var alpha = (byte)(xOpacitySlider.Value / xOpacitySlider.Maximum * 255); //Ratio of current value to maximum determines the relative desired opacity
            var rgb = _currentColorString.Substring(3); //Hex component of color
            return "#" + alpha.ToString("X2") + rgb; //Concatenation of new alpha and old hex value
        }

    //ACCESSORS AND MUTATORS

        /*
        * Whenever a group is clicked, it receives the document view associated with the click for editing, etc, stored in this mutator. 
        */
        public void SetGroupBinding(DocumentView selection) => _currentDocController = selection.ViewModel.DocumentController;

        /*
         * Determines whether or not to hide or display the combo box: in context, this applies only to toggling rotation which is not currently supported
         */
        public void SetComboBoxVisibility(Visibility visibility) => xShapeOptionsDropdown.Visibility = visibility;

        /*
         * Opens and or closes (i.e. hides/displays text labels on) the subtoolbar while maintaining overall visibility and adjusting the combo box margin for the new dimensions
         */
        public void CommandBarOpen(bool status)
        {
            xGroupCommandbar.IsOpen = status;

            //Whether or not open or closed, should always be visible if some content is selected
            xGroupCommandbar.IsEnabled = true;
            xGroupCommandbar.Visibility = Visibility.Visible;
            //Updates combo box dimensions
            xShapeOptionsDropdown.Margin = status ? new Thickness(ToolbarConstants.ComboBoxMarginOpen) : new Thickness(ToolbarConstants.ComboBoxMarginClosed);
        }

        /*
        * A key piece in allowing the group to be made editable on control click (might be some other modifier down the road). If not an adornment group, cannot edit via toolbar, with the converse being true.
        */
        public void TryMakeGroupEditable(bool makeAdornmentGroup)
        {
            _currentDocController?.GetDataDocument().SetField<TextController>(KeyStore.AdornmentKey, makeAdornmentGroup ? "false" : "true", !makeAdornmentGroup);
        }

    //EVENT HANDLERS

        /*
         * The shape-selection logic for the combo box
         */
        private void ShapeOptionsDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var switchDictionary = new Dictionary<int, string>
            {
                [0] = BackgroundShape.AdornmentShape.Rectangular.ToString(),
                [1] = BackgroundShape.AdornmentShape.Elliptical.ToString(),
                [2] = BackgroundShape.AdornmentShape.RoundedRectangle.ToString(),
                [3] = BackgroundShape.AdornmentShape.RoundedFrame.ToString(),
                [4] = BackgroundShape.AdornmentShape.Pentagonal.ToString(),
                [5] = BackgroundShape.AdornmentShape.Hexagonal.ToString(),
                [6] = BackgroundShape.AdornmentShape.Octagonal.ToString(),
                [7] = BackgroundShape.AdornmentShape.CustomPolygon.ToString(),
                [8] = BackgroundShape.AdornmentShape.Clover.ToString(),
            };

            var index = xShapeOptionsDropdown.SelectedIndex;
            var selectedLabel = switchDictionary.ContainsKey(index) ? switchDictionary[index] : BackgroundShape.AdornmentShape.Rectangular.ToString();
            _currentDocController?.GetDataDocument().SetField<TextController>(KeyStore.DataKey, selectedLabel, true);
        }

        /*
         * Resets opacity of the group to 0.5 opacity on right click
         */
        private void XOpacitySlider_OnRightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            xOpacitySlider.Value = 128;
            UpdateColor();
        }

        /*
         * Edits the alpha prefix of the current color string based on the new opacity slider value
         */
        private void XOpacitySlider_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e) => UpdateColor();

        /*
         * Create a new group with the current selections
         */
        private void XGroup_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            // TODO: when multiselect is eventually implemented, group all selected elements with as small a group as possible
            //For proper toolbar UI behavior on click
            xGroupCommandbar.IsOpen = true;
            xGroupCommandbar.IsEnabled = true;
        }

        /*
         * In effect, delete current group
         */
        private void XUngroup_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            // TODO: Delete groups on tapped
            //For proper toolbar UI behavior on click
            xGroupCommandbar.IsOpen = true;
            xGroupCommandbar.IsEnabled = true;
        }

        /*
         * Called whenever a group is clicked, the subtoolbar extracts its shape, color and opacity
         */
        public void AcknowledgeAttributes()
        {
            //SHAPE: If it's present, retrieves the stored shape associated with this group and reflects it in the combo box field
            var shape = _currentDocController?.GetDataDocument().GetDereferencedField<TextController>(KeyStore.DataKey, null)?.Data;

            var switchDictionary = new Dictionary<string, int>
            {
                [BackgroundShape.AdornmentShape.Rectangular.ToString()] = 0,
                [BackgroundShape.AdornmentShape.Elliptical.ToString()] = 1,
                [BackgroundShape.AdornmentShape.RoundedRectangle.ToString()] = 2,
                [BackgroundShape.AdornmentShape.RoundedFrame.ToString()] = 3,
                [BackgroundShape.AdornmentShape.Pentagonal.ToString()] = 4,
                [BackgroundShape.AdornmentShape.Hexagonal.ToString()] = 5,
                [BackgroundShape.AdornmentShape.Octagonal.ToString()] = 6,
                [BackgroundShape.AdornmentShape.CustomPolygon.ToString()] = 7,
                [BackgroundShape.AdornmentShape.Clover.ToString()] = 8,
            };

            xShapeOptionsDropdown.SelectedIndex = switchDictionary.ContainsKey(shape) ? switchDictionary[shape] : 0;

            // POTENTIALLY LET USER INPUT NUMBER OF SIDES
            //if (xShapeOptionsDropdown.SelectedIndex == 7)
            //{
            //    var margin = xOpacitySlider.Margin;
            //    margin.Left = 50;
            //    xOpacitySlider.Margin = margin;
            //    xSliderWidth.Width = new GridLength(294);
            //}
            //else
            //{
            //    var margin = xOpacitySlider.Margin;
            //    margin.Left = 22;
            //    xOpacitySlider.Margin = margin;
            //    xSliderWidth.Width = new GridLength(322);
            //}

            //COLOR: If it's present, retrieves the stored color associated with this group and assigns it to the current color... 
            //...doesn't interact with color picker, but changing opacity will do so in the context of the proper color
            _currentColorString = _currentDocController?.GetDataDocument().GetDereferencedField<TextController>(KeyStore.BackgroundColorKey, null)?.Data;
            
            //OPACITY: If it's present, retrieves the stored slider value (double stored as a string) associated with this group and...
            var storedSliderValue = _currentDocController?.GetDataDocument().GetDereferencedField<TextController>(KeyStore.OpacitySliderValueKey, null)?.Data;

            //...parses it to extract double and sets slider value to computed value
            if (double.TryParse(storedSliderValue, out var doubleSliderValue)) xOpacitySlider.Value = doubleSliderValue;
        }

        /*
         * Updates the value of the current color (as string) and updates color/opacity bindings
         */
        private void XGroupForegroundColorPicker_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            _currentColorString = xGroupForegroundColorPicker.SelectedColor.ToString();
            UpdateColor();
        }
    }
}
