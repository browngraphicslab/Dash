using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Dash
{
    public class EditableTextBlock
    {
        private TextBox _box; 
        private TextBlock _block = new TextBlock(); 

        //public event RoutedEventHandler GotFocus;
        //public event RoutedEventHandler LostFocus; 

        public EditableTextBlock()
        {
            _box = new TextBox
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                TextWrapping = TextWrapping.Wrap
            }; 
        }

        public FrameworkElement MakeView()
        {
            return _block; 
        }

        public void SetManipulation() {
            _box.GotFocus += (s, e) => _box.ManipulationMode = ManipulationModes.None;
            _box.LostFocus += (s, e) => _box.ManipulationMode = ManipulationModes.All;
        }
}
