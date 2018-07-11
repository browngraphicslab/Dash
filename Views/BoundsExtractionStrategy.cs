﻿using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using iText.Kernel.Geom;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;

namespace Dash
{
    public class BoundsExtractionStrategy : LocationTextExtractionStrategy
    {
        private PageSize _pageSize;
        private double _pageSpacing;
        private int _pageNumber;

        private List<SelectableElement> _elements = new List<SelectableElement>();

        public enum ElementType
        {
            Text,
            Image
        }

        public class SelectableElement
        {
            public SelectableElement(int index, string text, Rect bounds)
            {
                Index = index;
                Contents = text;
                ElementType = ElementType.Text;
                Bounds = bounds;
            }

            public Rect Bounds { get; }
            public int Index { get; set; }
            public object Contents { get; }
            public ElementType ElementType { get; }
        }

        public void SetPageNumber(int pageNumber)
        {
            _pageNumber = pageNumber;
            //TODO Cache rects per page, since elements can't cross over pages
        }

        public BoundsExtractionStrategy(PageSize pageSize, double pageSpacing)
        {
            this._pageSize = pageSize;
            this._pageSpacing = pageSpacing;
        }

        public override void EventOccurred(IEventData data, EventType type)
        {
            base.EventOccurred(data, type);
            if (type != EventType.RENDER_TEXT)
            {
                return;
            }

            var textData = (TextRenderInfo)data;
            var start = textData.GetAscentLine().GetStartPoint();
            var end = textData.GetDescentLine().GetEndPoint();
            _elements.Add(new SelectableElement(-1, textData.GetText(),
                new Rect(start.Get(0),
                        _pageSize.GetHeight() - start.Get(1) + _pageSpacing * _pageNumber,
                        end.Get(0) - start.Get(0),
                        start.Get(1) - end.Get(1))));
        }

        public List<SelectableElement> GetSelectableElements()
        {
            _elements.Sort((e1, e2) => Math.Sign(e1.Bounds.Y - e2.Bounds.Y));
            List<List<SelectableElement>> lines = new List<List<SelectableElement>>();
            lines.Add(new List<SelectableElement>{_elements.First()});
            SelectableElement element = _elements.First();
            foreach (var selectableElement in _elements.Skip(1))
            {
                if (selectableElement.Bounds.Y - element.Bounds.Y > element.Bounds.Height ||
                    Math.Abs(selectableElement.Bounds.Height - element.Bounds.Height) > element.Bounds.Height / 2)
                {
                    element = selectableElement;
                    lines.Add(new List<SelectableElement>{element});
                }
                else
                {
                    lines.Last().Add(selectableElement);
                }
            }

            List<SelectableElement> elements = new List<SelectableElement>(_elements.Count);
            foreach (var line in lines)
            {
                line.Sort((e1, e2) => Math.Sign(e1.Bounds.X - e2.Bounds.X));
                foreach (var selectableElement in line)
                {
                    selectableElement.Index = elements.Count;
                    elements.Add(selectableElement);
                }
            }
            return elements;
        }
    }

}