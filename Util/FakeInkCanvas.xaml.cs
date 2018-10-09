using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using Path = Windows.UI.Xaml.Shapes.Path;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class FakeInkCanvas : UserControl
    {
        private Dictionary<InkStroke, Path> _pathMap = new Dictionary<InkStroke, Path>();

        public FakeInkCanvas()
        {
            InitializeComponent();
        }

        public void AddStroke(InkStroke stroke)
        {
            if (!_pathMap.Keys.Contains(stroke))
            {
                var path = new Path();
                _pathMap.Add(stroke, path);
                xGrid.Children.Add(path);
            }
        }

        public void RemoveStroke(InkStroke stroke)
        {
            if (_pathMap.Keys.Contains(stroke))
            {
                xGrid.Children.Remove(_pathMap[stroke]);
                _pathMap.Remove(stroke);
            }
            else
            {
                throw new Exception("No inkstroke found to remove :(");
            }
        }
    }
}
