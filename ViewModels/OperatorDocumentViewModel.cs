using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace Dash.ViewModels
{
    class OperatorDocumentViewModel : DocumentViewModel
    {
        public OperatorDocumentViewModel(OperatorDocumentModel doc, DocumentLayoutModelSource source) : base(doc, source)
        {
        }

        public override List<UIElement> GetUiElements()
        {
            List<UIElement> elements = base.GetUiElements();
            OperatorDocumentModel doc = DocumentModel as OperatorDocumentModel;
            Debug.Assert(doc != null);
            Grid g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = GridLength.Auto
            });
            g.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(1, GridUnitType.Star)
            });
            g.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = GridLength.Auto
            });
            int i = 0;
            foreach (var key in doc.OperatorField.Inputs)
            {
                TextBlock b = new TextBlock
                {
                    Text = key.Name
                };
                Ellipse e = new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = new SolidColorBrush(Colors.Black)
                };
                Grid.SetColumn(b, 0);
                Grid.SetColumn(e, 0);
                Canvas.SetTop(b, i * 40);
                Canvas.SetTop(e, i * 40 + 20);
                elements.Add(b);
                elements.Add(e);
                i++;
            }
            i = 0;
            foreach (var key in doc.OperatorField.Outputs)
            {
                TextBlock b = new TextBlock
                {
                    Text = key.Name
                };
                Ellipse e = new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = new SolidColorBrush(Colors.Black)
                };
                Grid.SetColumn(b, 2);
                Grid.SetColumn(e, 2);
                Canvas.SetTop(b, i * 40);
                Canvas.SetTop(e, i * 40 + 20);
                elements.Add(b);
                elements.Add(e);
                i++;
            }
            return elements;
        }
    }
}
