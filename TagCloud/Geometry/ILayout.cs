using System.Collections.Generic;
using System.Linq;
using Gma.CodeCloud.Controls.TextAnalyses.Processing;
using Windows.Foundation;

namespace Gma.CodeCloud.Controls.Geometry
{
    public interface ILayout
    {
        void Arrange(IEnumerable<IWord> words, IGraphicEngine graphicEngine);
        IEnumerable<LayoutItem> GetWordsInArea(Rect area);
    }
}