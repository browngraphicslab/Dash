using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Dash
{
    public class PDFSection
    {
        public List<SelectableElement> SectionElements { get; set; } = new List<SelectableElement>();
        public Rect Bounds { get; set; }

        public PDFSection(SelectableElement elem)
        {
            SectionElements.Add(elem);
            Bounds = elem.Bounds;
        }

        public PDFSection(PDFSection section)
        {
            SectionElements.AddRange(section.SectionElements);
            Bounds = section.Bounds;
        }
    }
}
