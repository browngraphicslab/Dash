using System;
using System.Collections.Generic;
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
using System.Diagnostics;
using System.Text;
using Windows.UI;
using Zu.TypeScript.TsTypes;

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
            //Toolbar TODO
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
            switch (xShapeOptionsDropdown.SelectedIndex)
            {
                case 0: //RECTANGLE
                    _currentDocController?.GetDataDocument().SetField<TextController>(KeyStore.DataKey, BackgroundShape.AdornmentShape.Rectangular.ToString(), true);
                    break;
                case 1: //ELLIPSE
                    _currentDocController?.GetDataDocument().SetField<TextController>(KeyStore.DataKey, BackgroundShape.AdornmentShape.Elliptical.ToString(), true);
                    break;
                case 2: //ROUNDED RECTANGLE
                    _currentDocController?.GetDataDocument().SetField<TextController>(KeyStore.DataKey, BackgroundShape.AdornmentShape.Rounded.ToString(), true);
                    break;
                case 3: //ARBITRARY POLYGON

                    //TODO: collect user input points with a nice UI.

                    break;
                default: //DEFAULT = RECTANGLE
                    _currentDocController?.GetDataDocument().SetField<TextController>(KeyStore.DataKey, BackgroundShape.AdornmentShape.Rectangular.ToString(), true);
                    break;
            }
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

            if (shape == "Rectangular")
            {
                xShapeOptionsDropdown.SelectedIndex = 0;
            }
            else if (shape == "Elliptical")
            {
                xShapeOptionsDropdown.SelectedIndex = 1;
            }
            else if (shape == "Rounded")
            {
                xShapeOptionsDropdown.SelectedIndex = 2;
            }

            //COLOR: If it's present, retrieves the stored color associated with this group and assigns it to the current color... 
            //...doesn't interact with color picker, but changing opacity will do so in the context of the proper color
            _currentColorString = _currentDocController?.GetDataDocument().GetDereferencedField<TextController>(KeyStore.BackgroundColorKey, null)?.Data;
            
            //Toolbar TODO
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
