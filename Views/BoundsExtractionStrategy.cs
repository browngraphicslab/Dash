using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using iText.Kernel.Geom;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Point = Windows.Foundation.Point;
using Windows.UI.Xaml.Media;

namespace Dash
{
    public partial class BoundsExtractionStrategy : LocationTextExtractionStrategy
    {
        private Rectangle _pageSize;
        private double _pageOffset;
        private int _pageNumber;
        private double _pageRotation;
        private readonly List<SelectableElement> _elements = new List<SelectableElement>();
        private readonly List<Rectangle> _pages = new List<Rectangle>();

        public void SetPage(int pageNumber, double pageOffset, Rectangle pageSize, double rotation)
        {
            _pageNumber = pageNumber;
            _pageSize = pageSize;
            _pageOffset = pageOffset;
            _pageRotation = rotation;
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

            var rotated    = _pageRotation != 0;
            var mainData   = (TextRenderInfo)data;
            var pageHeight = _pageSize.GetHeight();
            var pageWidth  = _pageSize.GetWidth();
            var aspect     = rotated ? pageWidth / pageHeight : 1;

            foreach (var textData in mainData.GetCharacterRenderInfos())
            {
                // top left corner of the bounding box
                var rawStartPt = textData.GetAscentLine().GetStartPoint();
                // bottom right corner of the bounding box
                var rawEndPt = textData.GetDescentLine().GetEndPoint();

                var start = new Point(rawStartPt.Get(rotated ? 1 : 0) * aspect, (rotated ? pageHeight - rawStartPt.Get(0) / aspect : rawStartPt.Get(1)));
                var end   = new Point(rawEndPt.Get(rotated   ? 1 : 0) * aspect, (rotated ? pageHeight - rawEndPt.Get(0)   / aspect : rawEndPt.Get(1)));

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

                // if the space between this and the previous element is greater than or equal to some arbitrary constant space
                if (_elements.Any() && start.X - (_elements.Last().Bounds.X + _elements.Last().Bounds.Width) >=
                    textData.GetSingleSpaceWidth() * 0.3)
                {
                    // insert a space into that index
                    var width = start.X - (_elements.Last().Bounds.X + _elements.Last().Bounds.Width);
                    _elements.Add(new SelectableElement(-1, " ",
                        new Rect(_elements.Last().Bounds.X + _elements.Last().Bounds.Width,
                            _elements.Last().Bounds.Y, // pageHeight - (start.Y + _pageOffset),
                            width > 0 ? width : textData.GetSingleSpaceWidth(), Math.Abs(end.Y - start.Y))));
                }

                var newBounds = new Rect(start.X, pageHeight - start.Y + _pageOffset,
                    Math.Abs(end.X - start.X),
                    Math.Abs(end.Y - start.Y));
                if (!_elements.Any() || _elements.Last().Bounds != newBounds)
                {
                     _elements.Add(new SelectableElement(-1, textData.GetText(), newBounds));
                }
            }
        }

        /// <summary>
        ///     Given a start and an end page, this method will return all of the selectable elements
        ///     in that range. If the end page is the same as the start page, it will return all of
        ///     the selectable elements in that one page.
        /// </summary>
        public Tuple<List<SelectableElement>, string> GetSelectableElements(int startPage, int endPage)
        {
            // if any of the page requested are invalid, return an empty list
            if (_pages.Count < endPage || endPage < startPage)
            {
                return Tuple.Create(new List<SelectableElement>(), "");
            }

            var pageElements = new List<List<SelectableElement>>();
            // if the end and start page are the same, use just that one page, otherwise use the range between the two indices
            var requestedPages = endPage == startPage ? new List<Rectangle>{_pages[startPage]} : _pages.GetRange(startPage, endPage - startPage);
            // loop through each requested page (pages are stored as rectangles)
            foreach (var page in requestedPages)
            {
                // loop through each element in the list of total elements
                foreach (var selectableElement in _elements)
                {
                    // if the boundaries of the element is in the page rectangle
                    if (page.Contains(new Rectangle((float) selectableElement.Bounds.X,
                        (float) selectableElement.Bounds.Y,
                        (float) selectableElement.Bounds.Width, (float) selectableElement.Bounds.Height)))
                    {
                        // add the element to the page if the page (of selectable elements) already exists
                        if (pageElements.Count > _pages.IndexOf(page))
                        {
                            selectableElement.RawIndex = pageElements[_pages.IndexOf(page)].Count;
                            pageElements[_pages.IndexOf(page)].Add(selectableElement);
                        }
                        // otherwise create a new page of selectable elements and initialize it with this element
                        else
                        {
                            selectableElement.RawIndex = 0;
                            pageElements.Add(new List<SelectableElement> {selectableElement});
                        }
                    }
                }
            }

            var elements = new List<SelectableElement>(_elements.Count);
            // sort and add the elements in each page to a list of elements
            foreach (var page in pageElements)
            {
                var newElements = GetSortedSelectableElements(page, elements.Count);
                elements.AddRange(newElements);
            }

            StringBuilder sb = new StringBuilder(elements.Count);
            elements.ForEach(se => sb.Append(se.Type == SelectableElement.ElementType.Text ? (string)se.Contents : ""));

            return Tuple.Create(elements, sb.ToString());
        }

        /// <summary>
        ///     Given a list of selectable elements (usually a page), returns a list of lists of selectable elements,
        ///     sorted into lines by their y-position.
        /// </summary>
        private List<List<SelectableElement>> SortIntoLines(IReadOnlyCollection<SelectableElement> page)
        {
            // initialize lines with the first item in the page
            var lines = new List<List<SelectableElement>> { new List<SelectableElement> { page.First() } };
            var element = page.First();

            // loop through every element
            foreach (var selectableElement in page.Skip(1))
            {
                // if the element is deemed to be on a new line, create a new one and add it
                if (selectableElement.Bounds.Y - element.Bounds.Y > element.Bounds.Height*2/3 ||
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

            return lines;//.Where((s,i) => i == 0).ToList();
        }

        class PdfColumnDef {

            public List<SelectableElement> SelectableElements = new List<SelectableElement>();
            public Rect Bounds;
            public bool Overlaps(SelectableElement sel)
            {
                var selCenter = (sel.Bounds.Left + sel.Bounds.Right)/ 2;
                return Bounds.Left < selCenter && selCenter < Bounds.Right;
            }
        }


        /// <summary>
        ///     Given a list of lists of selectable elements, resturns a list of lists of selectable elements
        ///     representing each column in the page.
        /// </summary>
        private List<List<SelectableElement>> SortIntoColumns(List<List<SelectableElement>> lines)
        {
            var columns = new List<PdfColumnDef> ();
            var strings = new List<string>(); 
            // loop through every line
            foreach (var line in lines)
            {
                line.Sort((e1, e2) => Math.Sign(e1.Bounds.X - e2.Bounds.X));
                while (line.FirstOrDefault() != null)
                    if (!string.IsNullOrWhiteSpace(line.FirstOrDefault().Contents as string))
                        break;
                    else line.RemoveAt(0);
                if (line.Count == 0)
                    continue;
                if (strings.Count > 0 && !string.IsNullOrWhiteSpace(strings[0]) && !strings[0].EndsWith(" "))
                    strings[0] += " ";
                    // sort each line horizontally
                var linestr = line.Aggregate("", (str, e) => str + (e.Contents as string));
                var element = line.First();
                var currFontWidth = AverageFontSize(line);
                // assume that each line starts at column 0
                var col = 0;
                // find the width of the previous line
                var lineWidth = Math.Abs(line.First().Bounds.X - line.Last().Bounds.X);

                if (columns.Any() && line.Any())
                {
                    var firstEle = line.First();
                    var temp = firstEle.Bounds.X;
                    // while there's space to move the content to another column, based on what the previous line width is
                    while ( firstEle.Bounds.Left > columns[col].Bounds.Right + currFontWidth)

                    //!string.IsNullOrWhiteSpace(firstEle.Contents as string) && columns[col].SelectableElements.Any() && temp - lineWidth > columns[col].SelectableElements.Min(i => i.RawIndex == -1 ? int.MaxValue : i.Bounds.X) &&
                    //       lineWidth != 0.0)
                    {
                        columns[col].SelectableElements.Add(new SelectableElement(-1, "", new Rect()) { RawIndex = -1 }); // there's a gap
                        // do the math that would end up doing it
                        temp -= lineWidth;
                        // tell the line that we are now one column over
                        col++;
                        // add a new column if need be
                        if (columns.Count - 1 < col)
                        {
                            columns.Add(new PdfColumnDef());
                            strings.Add("");
                            break;
                        }
                    }
                } else
                {
                    columns.Add(new PdfColumnDef());
                    strings.Add("");
                }

                double lastX = element.Bounds.Left;
                var lastRect = element.Bounds;

                if (!string.IsNullOrWhiteSpace(element.Contents as string))
                {
                    columns[col].SelectableElements.Add(element);
                    strings[col] += (element.Contents as string);
                    lastX = element.Bounds.Right;
                    var left = Math.Min(columns[col].Bounds.Left, element.Bounds.Left);
                    columns[col].Bounds = new Rect(left, 0,
                        Math.Max(columns[col].Bounds.Right, element.Bounds.Right)-left, 0);
                }
                else
                {
                    lastRect = new Rect(lastX, 0, element.Bounds.Width, 0);
                    columns[col].Bounds = new Rect(lastX, 0, element.Bounds.Width, 0);
                }

                /*
                 * this method of "column-izing" each line relies on the fact that we loop through each
                 * line left to right. if we ever want to stop doing so, or if we find a better way of looping
                 * through templates, this will break.
                 */

                if (linestr.Contains("AARTS"))
                    ;

                // find the average font size of the line's elements

                foreach (var selectableElement in line.Skip(1))
                {
                    var selectableLeft = selectableElement.Bounds.Left;
                    var selectableString = selectableElement.Contents as string;
                    var whiteSpace = string.IsNullOrWhiteSpace(selectableString); 
                    if (!whiteSpace || 
                        ((selectableElement.Bounds.Left+ selectableElement.Bounds.Right)/2 > lastX && Math.Abs(element.RawIndex - selectableElement.RawIndex) < 3))
                    {
                        // if the element is far enough away from the previous element (2.75 seems to be a nice constant?)
                        var nextColumn = selectableLeft > columns[col].Bounds.Right + currFontWidth ||
                                         selectableElement.Bounds.Left > lastX + currFontWidth*1.1;
                        if (nextColumn && !whiteSpace)
                        {
                            if (!string.IsNullOrWhiteSpace(strings[col]))
                                strings[col] += " ";
                            var newCol = -1;
                            for (int i = col+1; i < columns.Count; i++) 
                                if (columns[i].Overlaps(selectableElement))
                                {
                                    newCol = i;
                                    break;
                                }
                                else
                                    columns[i].SelectableElements.Add(new SelectableElement(-1, "", new Rect()) { RawIndex = -1 });
                            if (newCol == -1)
                            {
                                columns.Add(new PdfColumnDef() { Bounds = selectableElement.Bounds });
                                strings.Add("");
                                newCol = columns.Count-1;
                            }
                            col = newCol;
                            PdfColumnDef prev = columns[newCol-1];
                            if (selectableLeft < prev.Bounds.Right) prev.Bounds = new Rect(prev.Bounds.Left, prev.Bounds.Top, Math.Max(1,lastX - prev.Bounds.Left), prev.Bounds.Height);
                        }
                        // add to whatever column we're indexed in
                        if (!nextColumn || !whiteSpace)
                        {
                            if ((selectableElement.Bounds.Left + selectableElement.Bounds.Right*2) / 3 < lastRect.Right && 
                                 selectableElement.Bounds.Left - lastX < currFontWidth/2 && !nextColumn)
                            {
                                columns[col].SelectableElements.RemoveAt(columns[col].SelectableElements.Count - 1);
                                strings[col] = strings[col].Substring(0, strings[col].Length - 1);
                            }
                            if (strings[col].EndsWith(" ") && whiteSpace)
                                continue;
                            columns[col].SelectableElements.Add(selectableElement);
                            strings[col] += selectableString;

                            double right = Math.Max(columns[col].Bounds.Right, selectableElement.Bounds.Right);
                            columns[col].Bounds = new Rect(new Point(Math.Min(columns[col].Bounds.Left, whiteSpace ? lastX : selectableElement.Bounds.Left), 0),
                                new Point(right, 0));
                        }
                       // if (!whiteSpace)
                            lastX = selectableElement.Bounds.Right;
                        lastRect = selectableElement.Bounds;

                        element = selectableElement;
                    }
                }
            }
            return columns.Select((cdef) => cdef.SelectableElements).ToList();
        }

        /// <summary>
        ///     Given a list of selectable elements, removes any duplicates in the list. This method doesn't
        ///     return a list, it only modifies the list passed in.
        /// </summary>
        private void RemoveDuplicates(IReadOnlyCollection<List<SelectableElement>> columns)
        {
            // create a copy of the columns so we can loop through it and make changes in the original
            var columnCopy = new List<List<SelectableElement>>(columns);
            // loop through every column in the copy of columns
            foreach (var column in columnCopy)
            {
                var prevElem = column.First();
                var matchingColumn = columns.First(col => col.Equals(column));
                // this commented code is only necessary if we want to uncomment out the line break code
                //var prevLineY = new Vector((float)prevElem.Bounds.Y,
                //    (float)(prevElem.Bounds.Y + prevElem.Bounds.Height), 0);
                foreach (var selectableElement in column.Skip(1))
                {
                    // if the current and previous element are extremely close in x and positions, and
                    // they also have the same contents, then they are probably duplicates
                    if (selectableElement.Bounds.X - prevElem.Bounds.X <= 1 && Math.Abs(
                            selectableElement.Bounds.Y - prevElem.Bounds.Y) < selectableElement.Bounds.Height / 2 &&
                        prevElem.Contents as string == selectableElement.Contents as string)
                    {
                        // remove the duplicate, and don't change the previous element
                        var index = matchingColumn.IndexOf(selectableElement);
                        matchingColumn.RemoveAt(index);
                    }
                    else
                    {
                        #region Semi-Functional Code for Inserting Line Breaks

                        //var y = selectableElement.Bounds.Y;
                        //// determine if there is enough space to quantify the difference in y-positions as a new line
                        //if (y - prevLineY.Get(1) > selectableElement.Bounds.Height / 2)
                        //{
                        //    // find the index to insert the line break at
                        //    var index = matchingColumn.IndexOf(selectableElement);
                        //    var bounds = new Rect(selectableElement.Bounds.X + selectableElement.Bounds.Width,
                        //        selectableElement.Bounds.Y, selectableElement.Bounds.Width,
                        //        selectableElement.Bounds.Height);
                        //    // \r\n creates a line break
                        //    var lnBr = "\r\n";
                        //    // create more than one line break if deemed necessary
                        //    for (var i = 0; i < Math.Floor((y - prevLineY.Get(1)) / (selectableElement.Bounds.Height / 2)); i++)
                        //    {
                        //        lnBr += "\r\n";
                        //        bounds.Height += selectableElement.Bounds.Height;
                        //    }
                        //    matchingColumn.Insert(index, new SelectableElement(-1, lnBr, bounds));
                        //}
                        //prevLineY = new Vector((float)selectableElement.Bounds.Y,
                        //    (float)(selectableElement.Bounds.Y + selectableElement.Bounds.Height), 0);

                        #endregion

                        // set the previous element to the current element only if we didn't delete it
                        prevElem = selectableElement;
                    }
                }
            }
        }

        private List<SelectableElement> GetSortedSelectableElements(List<SelectableElement> page, int elementCount)
        {
            // sort the elements in a page vertically
            page.Sort((e1, e2) => Math.Sign(e1.Bounds.Y - e2.Bounds.Y));
            var elements = new List<SelectableElement>();
            var lines = SortIntoLines(page);
            var columns = SortIntoColumns(lines);
            //RemoveDuplicates(columns);

            var colIndexes = columns.Select((c) => 0).ToList();
            // loop through each column in increasing order
            while (true)
            {
                int whichCol = -1;
                var lowIndex = int.MaxValue;
                for (int i = 0; i < colIndexes.Count; i++) 
                    if (columns[i].Count > colIndexes[i] && columns[i][colIndexes[i]].RawIndex < lowIndex)
                    {
                        whichCol = i;
                        lowIndex = columns[i][colIndexes[i]].RawIndex;
                    }
                // foreach (var column in columns)
                if (whichCol == -1)
                    break;
                var column = columns[whichCol];
                {
                    // loop through each element
                    for (int idx = colIndexes[whichCol]; idx < column.Count; idx++)
                    {
                        var selectableElement = column[idx];
                        if (selectableElement.RawIndex == -1)
                        {
                            colIndexes[whichCol]++;
                            break;
                        }
                        else
                        {
                            // add it
                            selectableElement.Index = elements.Count + elementCount;
                            elements.Add(selectableElement);
                            colIndexes[whichCol]++;
                        }
                    }
                }
            }
            var outstr = elements.Aggregate("", ((seed, e) => seed + (e.Contents as string)));

            return elements;
        }

        /// <summary>
        ///     Given a list of selectable elements (usually a line), find the average font size of the elements.
        ///     This method takes into account offset elements, and ignores missing spaces
        /// </summary>
        private double AverageFontSize(List<SelectableElement> line)
        {
            var mx = 0.0;
            foreach (var sel in line)
                if (!string.IsNullOrWhiteSpace(sel.Contents as string))
                {
                    if (sel.Bounds.Width > mx)
                        mx = sel.Bounds.Width;
                }
            return mx;

            var cumulativeWidth = 0.0;
            var numberOfSpaces = 0;
            // skip the first and last items for simplicity
            for (var i = 0; i < line.Count - 1; i++)
            {
                // if the distance between the left and the right element is greater than the width of the ith element
                if (Math.Abs(line[i + 1].Bounds.X - (line[i].Bounds.X + line[i].Bounds.Width)) > 1 &&
                    Math.Abs(line[i + 1].Bounds.X - (line[i].Bounds.X + line[i].Bounds.Width)) <
                    1.5 * line[i].Bounds.Width)
                {
                    // add that distance to the cumulative width
                    cumulativeWidth += Math.Abs(line[i + 1].Bounds.X - (line[i].Bounds.X + line[i].Bounds.Width));
                    numberOfSpaces++;
                }
            }
            
            // return the average of each space width
            return numberOfSpaces == 0 ? 2 :  cumulativeWidth / numberOfSpaces;
        }
    }
}
