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
        private List<Rectangle> _pages = new List<Rectangle>();
        private double _smallestSpaceWidth;

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
                //if (_elements.Any() &&
                //    Math.Abs(Math.Abs(start.Get(0) - (_elements.Last().Bounds.X + _elements.Last().Bounds.Width)) -
                //             textData.GetSingleSpaceWidth()) <=
                //    0.1)
                //{
                //    _elements.Add(new SelectableElement(-1, " ",
                //        new Rect(_elements.Last().Bounds.X + _elements.Last().Bounds.Width,
                //            _pageSize.GetHeight() - start.Get(1) - (_elements.Last().Bounds.Width + _pageOffset),
                //            textData.GetSingleSpaceWidth(),
                //            Math.Abs(end.Get(1) - start.Get(1)))));
                //}

                //if (!_elements.Any() || 
                //    ((!_elements.Last().Bounds.Contains(new Point(start.Get(0), start.Get(1))) ||
                //                          !_elements.Last().Bounds.Contains(new Point(
                //                              textData.GetAscentLine().GetEndPoint().Get(0),
                //                              textData.GetAscentLine().GetEndPoint().Get(1)))) &&
                //                         !(_elements.Last().Contents as string).Equals(textData.GetText())))

                if (_elements.Any() && start.Get(0) - (_elements.Last().Bounds.X + _elements.Last().Bounds.Width) >=
                    textData.GetSingleSpaceWidth())
                {
                    _elements.Add(new SelectableElement(-1, " ",
                        new Rect(_elements.Last().Bounds.X + _elements.Last().Bounds.Width,
                            _pageSize.GetHeight() -
                            (start.Get(1) +
                             _pageOffset), textData.GetSingleSpaceWidth(), Math.Abs(end.Get(1) - start.Get(1)))));
                }

                //if (_elements.Any() && Math.Abs(_elements.Last().Bounds.Y - textData.GetDescentLine().GetBoundingRectangle().GetY()) >
                //    _elements.Last().Bounds.Height)
                //{
                //    _elements.Add(new SelectableElement(-1, "\r\n",
                //        new Rect(_elements.Last().Bounds.X + _elements.Last().Bounds.Width,
                //            _pageSize.GetHeight() -
                //            (start.Get(1) +
                //             _pageOffset), textData.GetSingleSpaceWidth(), Math.Abs(end.Get(1) - start.Get(1)))));
                //}

                var newBounds = new Rect(start.Get(0),
                    _pageSize.GetHeight() - start.Get(1) + _pageOffset,
                    Math.Abs(end.Get(0) - start.Get(0)),
                    Math.Abs(end.Get(1) - start.Get(1)));
                if (!_elements.Any() || _elements.Last().Bounds != newBounds)
                {
                    _elements.Add(new SelectableElement(-1, textData.GetText(), newBounds));
                }

                if (textData.GetSingleSpaceWidth() < _smallestSpaceWidth)
                {
                    _smallestSpaceWidth = textData.GetSingleSpaceWidth();
                }
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
                var col = 0;
                var lineWidth = Math.Abs(line.First().Bounds.X - line.Last().Bounds.X);
                //var colWidth = Math.Abs(columns[col].First().Bounds.X - columns[col].Last().Bounds.X);

                if (columns.Any() && line.Any())
                {
                    var temp = line.First().Bounds.X;
                    while (columns[col].Any() && temp - lineWidth > columns[col].Min(i => i.Bounds.X) && lineWidth != 0.0)
                    {
                        temp -= lineWidth;
                        col++;
                        if (columns.Count - 1 < col)
                        {
                            columns.Add(new List<SelectableElement>());
                        }
                    }
                }

                columns[col].Add(element);
                //List<List<SelectableElement>> newColumns = new List<List<SelectableElement>>();
                //foreach (var column in columns)
                //{
                //    if (column.Any())
                //    {
                //        var colWidth = Math.Abs(column.First().Bounds.X - column.Last().Bounds.X);
                //        var lineWidth = Math.Abs(line.First().Bounds.X - line.Last().Bounds.X);
                //        if (colWidth / lineWidth > 2 && Math.Abs(column.First().Bounds.X - line.First().Bounds.X) > lineWidth / 2)
                //        {
                //            var temp = line.First().Bounds.X;
                //            while (temp >= column.First().Bounds.X + lineWidth && lineWidth != 0)
                //            {
                //                temp -= lineWidth;
                //                col++;
                //            }

                //            var colTemp = col;
                //            while (colTemp > columns.Count - 1 + newColumns.Count)
                //            {
                //                newColumns.Add(new List<SelectableElement>());
                //            }

                //            col = (int) Math.Floor(Math.Abs(colWidth - lineWidth) / line.First().Bounds.X);

                //            if (columns.Count - 1 + newColumns.Count < col)
                //            {
                //                col = columns.Count - 1 + newColumns.Count;
                //            }
                //        }
                //    }
                //}

                //columns.AddRange(newColumns);
                
                var currFontWidth = AverageFontSize(line);
                foreach (var selectableElement in line)
                {
                    if (selectableElement.Bounds.X - (element.Bounds.X + element.Bounds.Width) > 2.75 * currFontWidth)
                    {
                        col++;
                        if (columns.Count > col)
                        {
                            columns[col].Add(selectableElement);
                        }
                        else
                        {
                            columns.Add(new List<SelectableElement> {selectableElement});
                        }
                    }
                    //else if (selectableElement.Bounds.X - (element.Bounds.X + element.Bounds.Width) > _smallestSpaceWidth)
                    //{
                    //    columns[col].Add(new SelectableElement(-1, " ",
                    //        new Rect(element.Bounds.X + element.Bounds.Width, element.Bounds.Y, _smallestSpaceWidth,
                    //            element.Bounds.Height)));
                    //    columns[col].Add(selectableElement);
                    //}
                    else
                    {
                        columns[col].Add(selectableElement);
                    }

                    element = selectableElement;
                }
            }

            //var dictionary = new Dictionary<double, List<SelectableElement>>();
            //foreach (var line in lines)
            //{
            //    foreach (var e in line)
            //    {
            //        if (dictionary.ContainsKey(e.Bounds.X))
            //        {
            //            dictionary[e.Bounds.X].Add(e);
            //        }
            //        else
            //        {
            //            dictionary.Add(e.Bounds.X, new List<SelectableElement>{e});
            //        }
            //    }
            //}
            
            //var columnPositions = dictionary.Where(i => i.Value.Count > 5).OrderBy(j => j.Key);
            //var columns2 = new List<List<SelectableElement>>();
            //foreach (var columnPos in columnPositions)
            //{
            //    columns2.Add(new List<SelectableElement>());
            //    for (var i = 0; i < columnPos.Value.Count; i++)
            //    {
            //        var avgSize = AverageFontSize(columnPos.Value);
            //        var line = lines.Find(j => j.Contains(columnPos.Value[i]));
            //        for (var k = 0; k < line.Count; k++)
            //        {
            //            if (line[k + 1].Bounds.X - (line[k].Bounds.X + line[k].Bounds.Width) > 3.5 * avgSize)
            //            {
            //                columns2.Last().Add(line[k]);
            //            }
            //            else
            //            {
            //                columns2.Add(new List<SelectableElement> {line[k]});
            //            }
            //        }
            //    }
            //}

            List<SelectableElement> elements = new List<SelectableElement>();
            foreach (var column in columns)
            {
                foreach (var selectableElement in column)
                {
                    selectableElement.Index = elements.Count + elementCount;
                    elements.Add(selectableElement);
                }
            }

            //var columnThreshold = _pageSize.GetWidth() / columns.Count;
            //foreach (var column in columns)
            //{
            //    foreach (var line in lines)
            //    {
            //        var lineWidthBefore = LineWidth(lines[lines.IndexOf(line) > 0 ? lines.IndexOf(line) - 1 : 0],
            //            column);
            //        var lineWidth = LineWidth(line, column);
            //        var lineWidthAfter =
            //            LineWidth(
            //                lines[lines.IndexOf(line) < lines.Count - 1 ? lines.IndexOf(line) + 1 : lines.Count - 1],
            //                column);
            //        var averageWidth = lineWidthBefore + lineWidthAfter / 2;
            //        if (averageWidth - lineWidth > columnThreshold)
            //        {
            //            var nextColumn = columns.IndexOf(column) < columns.Count
            //                ? columns.IndexOf(column) + 1
            //                : columns.Count - 1;
            //            if (columns[nextColumn] != column)
            //            {
            //                var nextColumnLine = lines.First(ln => ln.Any(i => columns[nextColumn].Contains(i)));
            //                var range = elements.GetRange(nextColumnLine.First().Index,
            //                    nextColumnLine.Last().Index - nextColumnLine.First().Index);
            //                var insertIndex = line.Last(i => column.Contains(i)).Index;
            //                elements.RemoveRange(range.First().Index, range.Count);
            //                elements.InsertRange(insertIndex, range);
            //            }
            //        }
            //    }
            //}

            return elements;
        }

        private double AverageFontSize(List<SelectableElement> line)
        {
            var cumulativeWidth = 0.0;
            var numberOfSpaces = 0;
            for (var i = 1; i < line.Count - 1; i++)
            {
                if (Math.Abs(line[i + 1].Bounds.X - (line[i - 1].Bounds.X + line[i - 1].Bounds.Width)) >  line[i].Bounds.Width)
                {
                    cumulativeWidth += Math.Abs(line[i + 1].Bounds.X - (line[i - 1].Bounds.X + line[i - 1].Bounds.Width));
                    numberOfSpaces++;
                }
            }

            return cumulativeWidth / numberOfSpaces;
        }

        private double LineWidth(List<SelectableElement> line, List<SelectableElement> column = null)
        {
            var start = line.First(i => column?.Contains(i) ?? true);
            var end = line.Last(i => column?.Contains(i) ?? true);
            return end.Bounds.X + end.Bounds.Width - start.Bounds.X;
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