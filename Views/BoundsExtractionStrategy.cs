﻿using System;
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
        private List<Rectangle> _pages = new List<Rectangle>();

        public void SetPage(int pageNumber, double pageOffset, Rectangle pageSize)
        {
            _pageNumber = pageNumber;
            _pageSize = pageSize;
            _pageOffset = pageOffset;
            _pages.Add(new Rectangle(pageSize.GetX(), (float) (pageSize.GetY() + pageOffset), pageSize.GetWidth(),
                pageSize.GetHeight()));
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
            }
        }

        public List<SelectableElement> GetSelectableElements()
        {
            var pageElements = new List<List<SelectableElement>>();
            foreach (var page in _pages)
            {
                foreach (var selectableElement in _elements)
                {
                    if (page.Contains(new Rectangle((float) selectableElement.Bounds.X,
                        (float) selectableElement.Bounds.Y,
                        (float) selectableElement.Bounds.Width, (float) selectableElement.Bounds.Height)))
                    {
                        if (pageElements.Count > _pages.IndexOf(page))
                        {
                            pageElements[_pages.IndexOf(page)].Add(selectableElement);
                        }
                        else
                        {
                            pageElements.Add(new List<SelectableElement> {selectableElement});
                        }
                    }
                    else
                    {
                        continue;
                    }
                }
            }

            var elements = new List<SelectableElement>(_elements.Count);
            foreach (var page in pageElements)
            {
                elements.AddRange(GetSelectableElements(page, elements.Count));
            }

            return elements;
        }

        private List<SelectableElement> GetSelectableElements(List<SelectableElement> page, int elementCount)
        {
            page.Sort((e1, e2) => Math.Sign(e1.Bounds.Y - e2.Bounds.Y));
            List<List<SelectableElement>> lines = new List<List<SelectableElement>>();
            lines.Add(new List<SelectableElement> { page.First() });
            SelectableElement element = page.First();
            foreach (var selectableElement in page.Skip(1))
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

            List<List<SelectableElement>> columns = new List<List<SelectableElement>>();
            columns.Add(new List<SelectableElement>());
            foreach (var line in lines)
            {
                line.Sort((e1, e2) => Math.Sign(e1.Bounds.X - e2.Bounds.X));
                element = line.First();
                columns[0].Add(element);
                var col = 0;
                foreach (var selectableElement in line)
                {
                    var currFontWidth = selectableElement.Bounds.Width;
                    if (selectableElement.Bounds.X - element.Bounds.X > 4 * currFontWidth)
                    {
                        col++;
                        if (columns.Count > col)
                        {
                            columns[col].Add(selectableElement);
                        }
                        else
                        {
                            columns.Add(new List<SelectableElement> { selectableElement });
                        }
                    }
                    else
                    {
                        columns[col].Add(selectableElement);
                    }

                    element = selectableElement;
                }
            }

            List<SelectableElement> elements = new List<SelectableElement>();
            foreach (var column in columns)
            {
                foreach (var selectableElement in column)
                {
                    selectableElement.Index = elements.Count + elementCount;
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