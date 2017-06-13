using Windows.UI.Xaml;

namespace Dash
{
    public class DocumentCollectionTemplateModel : TemplateModel
    {
        public DocumentCollectionTemplateModel(double left = 0, double top = 0, double width = 0, double height = 0, Visibility visibility = Visibility.Visible) : base(left, top, width, height, visibility)
        {
        }
    }
}