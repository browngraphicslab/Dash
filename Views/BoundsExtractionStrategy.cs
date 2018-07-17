using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.Foundation;
using iText.Kernel.Geom;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Point = Windows.Foundation.Point;

namespace Dash
{
    public partial class BoundsExtractionStrategy : LocationTextExtractionStrategy
    {
        private Rectangle _pageSize;
        private double _pageOffset;
        private int _pageNumber;
        private double _largestSpaceWidth;
        private List<SelectableElement> _elements = new List<SelectableElement>();

        public void SetPage(int pageNumber, double pageOffset, Rectangle pageSize)
        {
            _pageNumber = pageNumber;
            _pageSize = pageSize;
            _pageOffset = pageOffset;
            //TODO Cache rects per page, since elements can't cross over pages
        }

        public override void EventOccurred(IEventData data, EventType type)
        {
            base.EventOccurred(data, type);
            if (type != EventType.RENDER_TEXT)
            {
                return;
            }

            var mainTextData = (TextRenderInfo)data;
            foreach (var textData in mainTextData.GetCharacterRenderInfos())
            {
                var start = textData.GetAscentLine().GetStartPoint();
                var end = textData.GetDescentLine().GetEndPoint();
                if (_elements.Any() &&
                    Math.Abs(Math.Abs(start.Get(0) - _elements.Last().Bounds.X) - textData.GetSingleSpaceWidth()) <=
                    0.1)
                {
                    _elements.Add(new SelectableElement(-1, " ",
                        new Rect(_elements.Last().Bounds.X,
                            _pageSize.GetHeight() - start.Get(1) - _elements.Last().Bounds.Width + _pageOffset,
                            Math.Abs(start.Get(0) - _elements.Last().Bounds.X),
                            Math.Abs(end.Get(1) - start.Get(1)))));
                }

                _elements.Add(new SelectableElement(-1, textData.GetText(),
                    new Rect(start.Get(0),
                        _pageSize.GetHeight() - start.Get(1) + _pageOffset,
                        Math.Abs(end.Get(0) - start.Get(0)),
                        Math.Abs(end.Get(1) - start.Get(1)))));
                if (textData.GetSingleSpaceWidth() > _largestSpaceWidth)
                {
                    _largestSpaceWidth = textData.GetSingleSpaceWidth();
                }
            }
        }

        public List<SelectableElement> GetSelectableElements()
        {
            FindColumns();
            
            List<List<SelectableElement>> lines = new List<List<SelectableElement>>();
            lines.Add(new List<SelectableElement> { _elements.First() });
            SelectableElement element = _elements.First();
            foreach (var selectableElement in _elements.Skip(1))
            {
                if (selectableElement.Bounds.Y - element.Bounds.Y > element.Bounds.Height ||
                    Math.Abs(selectableElement.Bounds.Height - element.Bounds.Height) > element.Bounds.Height / 2)
                {
                    element = selectableElement;
                    lines.Add(new List<SelectableElement> { element });
                }
                else
                {
                    lines.Last().Add(selectableElement);
                }
            }

            List<SelectableElement> elements = new List<SelectableElement>(_elements.Count);
            foreach (var line in lines)
            {
                foreach (var selectableElement in line)
                {
                    selectableElement.Index = elements.Count;
                    elements.Add(selectableElement);
                }
            }
            return elements;
        }

        private void FindColumns()
        {
            List<List<SelectableElement>> lines = new List<List<SelectableElement>>();
            lines.Add(new List<SelectableElement> {_elements.First()});
            SelectableElement element = _elements.First();
            foreach (var selectableElement in _elements.Skip(1))
            {
                if (selectableElement.Bounds.X - element.Bounds.X > 3 * _largestSpaceWidth)
                {
                    element = selectableElement;
                    lines.Add(new List<SelectableElement> {element});
                }
                else
                {
                    lines.Last().Add(selectableElement);
                }
            }

            List<SelectableElement> elements = new List<SelectableElement>(_elements.Count);
            foreach (var line in lines)
            {
                foreach (var selectableElement in line)
                {
                    elements.Add(selectableElement);
                }
            }

            _elements = elements;
        }
    }

}