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
        public List<int> ElementCounts = new List<int>();

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
            
            var mainData = (TextRenderInfo)data;

            foreach (var textData in mainData.GetCharacterRenderInfos())
            {
                // top left corner of the bounding box
                var start = textData.GetAscentLine().GetStartPoint();
                // bottom right corner of the bounding box
                var end = textData.GetDescentLine().GetEndPoint();

                #region Commented Out Code

                /* functioning code to prevent double text from appearing, seems to be a bit too aggressive */
                //if (!_elements.Any() || 
                //    ((!_elements.Last().Bounds.Contains(new Point(start.Get(0), start.Get(1))) ||
                //                          !_elements.Last().Bounds.Contains(new Point(
                //                              textData.GetAscentLine().GetEndPoint().Get(0),
                //                              textData.GetAscentLine().GetEndPoint().Get(1)))) &&
                //                         !(_elements.Last().Contents as string).Equals(textData.GetText())))

                /* semi-functioning code to add line breaks when necessary (math may just be wrong) */
                //if (_elements.Any())
                //{
                //    var prevY = _elements.Last().Bounds.Y;
                //    var prevYEnd = prevY + _elements.Last().Bounds.Height;
                //    var y1 = start.Get(1);
                //    var y2 = end.Get(1);
                //    var inline = (prevY <= y1 && y1 <= prevYEnd) || (prevY <= y2 && y2 <= prevYEnd);
                //    if (!inline)
                //    {
                //        var bounds = new Rect(_elements.Last().Bounds.X + _elements.Last().Bounds.Width, prevY,
                //            _elements.Last().Bounds.Width, _elements.Last().Bounds.Height);
                //        _elements.Add(new SelectableElement(-1, "\r\n", bounds));
                //    }
                //}

                #endregion

                /* code to insert spaces into the text when there is too much space between elements */

                // if the space between this and the previous element is greater than or equal approximately the width of a single space
                if (_elements.Any() && start.Get(0) - (_elements.Last().Bounds.X + _elements.Last().Bounds.Width) >=
                    textData.GetSingleSpaceWidth() * 0.3)
                {
                    var width = start.Get(0) - _elements.Last().Bounds.X + _elements.Last().Bounds.Width;
                    _elements.Add(new SelectableElement(-1, " ",
                        new Rect(_elements.Last().Bounds.X + _elements.Last().Bounds.Width,
                            _pageSize.GetHeight() - (start.Get(1) + _pageOffset),
                            width > 0 ? width : textData.GetSingleSpaceWidth(), Math.Abs(end.Get(1) - start.Get(1)))));
                }

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

        public List<SelectableElement> GetSelectableElements(int startPage, int endPage)
        {
            var pageElements = new List<List<SelectableElement>>();
            var requestedPages = _pages.GetRange(startPage, endPage - startPage);
            foreach (var page in requestedPages)
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

            for (var i = 0; i < startPage; i++)
            {
                ElementCounts.Add(0);
            }

            var elements = new List<SelectableElement>(_elements.Count);
            foreach (var page in pageElements)
            {
                var newElements = GetSelectableElements(page, elements.Count);
                elements.AddRange(newElements);
                ElementCounts.Add(newElements.Count);
            }

            return elements;
        }

        private List<SelectableElement> GetSelectableElements(List<SelectableElement> page, int elementCount)
        {
            // sort the elements in a page vertically
            page.Sort((e1, e2) => Math.Sign(e1.Bounds.Y - e2.Bounds.Y));
            var lines = new List<List<SelectableElement>> {new List<SelectableElement> {page.First()}};
            var element = page.First();

            // loop through every element
            foreach (var selectableElement in page.Skip(1))
            {
                // if the element is deemed to be on a new line, create a new one and add it
                if (selectableElement.Bounds.Y - element.Bounds.Y > element.Bounds.Height ||
                    Math.Abs(selectableElement.Bounds.Height - element.Bounds.Height) > element.Bounds.Height / 2)
                {
                    element = selectableElement;
                    lines.Add(new List<SelectableElement> { element });
                }
                else
                {
                    // otherwise, just add it to the most recently added line
                    lines.Last().Add(selectableElement);
                }
            }

            var columns = new List<List<SelectableElement>>();
            columns.Add(new List<SelectableElement>());
            // loop through every line
            foreach (var line in lines)
            {
                // sort each line horizontally
                line.Sort((e1, e2) => Math.Sign(e1.Bounds.X - e2.Bounds.X));
                element = line.First();
                var col = 0;
                var lineWidth = Math.Abs(line.First().Bounds.X - line.Last().Bounds.X);
                //var colWidth = Math.Abs(columns[col].First().Bounds.X - columns[col].Last().Bounds.X);

                if (columns.Any() && line.Any())
                {
                    var temp = line.First().Bounds.X;
                    // while there's space to move the content to another column
                    while (columns[col].Any() && temp - lineWidth > columns[col].Min(i => i.Bounds.X) &&
                           lineWidth != 0.0)
                    {
                        // do the math that would end up doing it
                        temp -= lineWidth;
                        col++;
                        // add a new column if need be
                        if (columns.Count - 1 < col)
                        {
                            columns.Add(new List<SelectableElement>());
                        }
                    }
                }
                
                columns[col].Add(element);

                /*
                 * this method of "column-izing" each line relies on the fact that we loop through each
                 * line left to right. if we ever want to stop doing so, or if we find a better way of looping
                 * through templates, this will break.
                 */

                // find the average font size of the line's elements
                var currFontWidth = AverageFontSize(line);
                foreach (var selectableElement in line.Skip(1))
                {
                    // if the element is far enough away from the previous element (2.75 seems to be a nice constant?)
                    if (selectableElement.Bounds.X - (element.Bounds.X + element.Bounds.Width) > 2.75 * currFontWidth)
                    {
                        // establish that we are moving over one column
                        col++;
                        // build a new column if need be, otherwise, just add it
                        if (columns.Count > col)
                        {
                            columns[col].Add(selectableElement);
                        }
                        else
                        {
                            columns.Add(new List<SelectableElement> {selectableElement});
                        }
                    }
                    // otherwise, just add it to whatever column we're indexed in
                    else
                    {
                        columns[col].Add(selectableElement);
                    }

                    element = selectableElement;
                }
            }

            var columnCopy = new List<List<SelectableElement>>(columns);
            foreach (var column in columnCopy)
            {
                var prevElem = column.First();
                var matchingColumn = columns.First(col => col.Equals(column));
                var prevLineY = new Vector((float) prevElem.Bounds.Y,
                    (float) (prevElem.Bounds.Y + prevElem.Bounds.Height), 0);
                foreach (var selectableElement in column.Skip(1))
                {
                    if (selectableElement.Bounds.X - prevElem.Bounds.X <= 1 && Math.Abs(
                            selectableElement.Bounds.Y - prevElem.Bounds.Y) < selectableElement.Bounds.Height / 2 &&
                        prevElem.Contents as string == selectableElement.Contents as string)
                    {
                        var index = matchingColumn.IndexOf(selectableElement);
                        matchingColumn.RemoveAt(index);
                    }
                    else
                    {
                        var y = selectableElement.Bounds.Y;
                        if (y - prevLineY.Get(1) > selectableElement.Bounds.Height / 2)
                        {

                            var index = matchingColumn.IndexOf(selectableElement);
                            var bounds = new Rect(selectableElement.Bounds.X + selectableElement.Bounds.Width,
                                selectableElement.Bounds.Y, selectableElement.Bounds.Width,
                                selectableElement.Bounds.Height);
                            var lnBr = "\r\n";
                            for (var i = 0; i < Math.Floor((y - prevLineY.Get(1)) / selectableElement.Bounds.Height); i++)
                            {
                                lnBr += "\r\n";
                                bounds.Height += selectableElement.Bounds.Height;
                            }
                            matchingColumn.Insert(index, new SelectableElement(-1, lnBr, bounds));
                        }
                        prevElem = selectableElement;
                        prevLineY = new Vector((float)selectableElement.Bounds.Y,
                            (float)(selectableElement.Bounds.Y + selectableElement.Bounds.Height), 0);
                    }
                }
            }

            var elements = new List<SelectableElement>();
            // loop through each column in increasing order
            foreach (var column in columns)
            {
                // loop through each element
                foreach (var selectableElement in column)
                {
                    // add it
                    selectableElement.Index = elements.Count + elementCount;
                    elements.Add(selectableElement);
                }
            }
            
            return elements;
        }

        private double AverageFontSize(List<SelectableElement> line)
        {
            var cumulativeWidth = 0.0;
            var numberOfSpaces = 0;
            // skip the first and last items for simplicity
            for (var i = 0; i < line.Count - 1; i++)
            {
                // if the distance between the left and the right element is greater than the width of the ith element
                if (Math.Abs(line[i + 1].Bounds.X - (line[i].Bounds.X + line[i].Bounds.Width)) > 1 &&
                    Math.Abs(line[i + 1].Bounds.X - (line[i].Bounds.X + line[i].Bounds.Width)) <
                    2 * line[i].Bounds.Width)
                {
                    // add that distance to the cumulative width
                    cumulativeWidth += Math.Abs(line[i + 1].Bounds.X - (line[i].Bounds.X + line[i].Bounds.Width));
                    numberOfSpaces++;
                }
            }
            
            // return the average of each space width
            return 2;
        }
    }
}