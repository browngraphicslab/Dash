using System;
using Windows.Foundation;

namespace Gma.CodeCloud.Controls.Geometry
{
    public interface IGraphicEngine : IDisposable
    {
        Size Measure(string text, int weight);
        void Draw(LayoutItem layoutItem);
        void DrawEmphasized(LayoutItem layoutItem);
    }
}
