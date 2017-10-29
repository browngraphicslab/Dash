using System;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;

namespace Gma.CodeCloud.Controls.Geometry
{
    public interface IGraphicEngine
    {
        Size Measure(string text, int weight);
        void Draw(Panel grid, LayoutItem layoutItem);
    }
}
