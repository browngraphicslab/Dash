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

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class ShapeView : UserControl
    {
        private readonly ShapeViewModel _vm;

        public ShapeView(ShapeViewModel vm)
        {
            this.InitializeComponent();

            _vm = vm;
            DataContext = _vm;
        }

        private void ShapeOnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var translate = Util.TranslateInCanvasSpace(e.Delta.Translation, this);
            _vm.MoveShape(translate.X, translate.Y);
        }
    }
}
