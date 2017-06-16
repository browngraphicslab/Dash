using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Dash.Models
{
    /// <summary>
    /// Defines a model template for images.
    /// </summary>
    public class ImageTemplateModel : TemplateModel
    {
        public ImageTemplateModel(double left = 0, double top = 0, double width = 0, double height = 0,
            Visibility visibility = Visibility.Visible)
            : base(left, top, width, height, visibility)
        {
            
        }
    }
}
