using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using DashShared;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;

namespace Dash
{
	/// <summary>
	/// This class publishes a list of documents to a clean HTML linked format. Call StartPublication to get started.
	/// </summary>
    public class Publisher
	{
		private StorageFolder _folder;
		private readonly Dictionary<string, int> _fileCollisions = new Dictionary<string, int>();
		private readonly Dictionary<DocumentController, string> _fileNames = new Dictionary<DocumentController, string>();
		// since the sidebar is the same code for every page, we might as well as save it instead of generate it for every file
		private string _sidebarText;
		private bool _mediaFolderMade = false;
		private List<string> _regionColors = new List<string> { "#95B75F", "#65A4DE", "#ED726A", "#DF8CE1", "#977ABC", "#F8AC75", "#97DFC0", "#FF9FAB", "#B4A8FF", "#91DBF3" };
		private Dictionary<DocumentController, string> _colorPairs = new Dictionary<DocumentController, string>();
		private Dictionary<DocumentController, int> _pdfNumbers = new Dictionary<DocumentController, int>(); // for storing number of pages in a PDF -- kind of a hack until we find a not-async method to do this
		private Dictionary<DocumentController, Size> _pdfPageSize = new Dictionary<DocumentController, Size>(); // for storing pdf page sizes -- again, kind of a hack until we find a not-async method to do this
		private Dictionary<DocumentController, List<SelectableElement>> _pdfSelectableElements =
			new Dictionary<DocumentController, List<SelectableElement>>(); // stores the selectableElements on a pdf

		/// <summary>
		/// Use this method to start the publication process. Pass in a list of DocumentControllers to publish. Note that if any annotations are not in the list of DocumentControllers, they will not be published.
		/// </summary>
		/// <param name="dcs"></param>
		public async Task StartPublication(List<DocumentController> dcs)
		{
			StorageFolder outerFolder = await PickFolder();

			if (outerFolder != null)
			{
				// Get the current date in good string format for folder name
				DateTime thisDay = DateTime.Now;
				string daytimeString = "Dash Publication " + thisDay;

				// create a folder inside selected folder to save this export
				_folder = await outerFolder.CreateFolderAsync(RemoveUnsafeCharacters(daytimeString), CreationCollisionOption.ReplaceExisting);

				// add the .css file
				var css = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/PublishStyle.css"));
				await css.CopyAsync(_folder, "PublishStyle.css", NameCollisionOption.ReplaceExisting);

				// build fileNames dictionary
				BuildFileNames(dcs);

				// initialize color pairs
				InitializeColorPairs(dcs);

				// copy all the media
				await CopyMedia(dcs);

				// get the sidebar thing going
				_sidebarText = GetSidebarText(dcs);

				// create a new file for every DocumentController
				foreach (var dc in dcs)
				{
					var fileName = _fileNames[dc.GetDataDocument()];
					var fileContents = GetFileContents(dc);
					await CreateFile(fileContents, fileName);
				}
			}
		}

        /// <summary>
        /// This method generates the HTML content for each DocumentController.
        /// </summary>
        /// <param name="dc"></param>
        /// <returns></returns>
        private string GetFileContents(DocumentController dc)
		{
			var fileText = new List<string>
			{
				// CREATE THE INDEX
				"<!DOCTYPE html>",
				"<title>" + dc.Title + "</title>",
				"<link rel=\"stylesheet\" href=\"https://use.fontawesome.com/releases/v5.2.0/css/all.css\" integrity=\"sha384-hWVjflwFxL6sNzntih27bfxkr27PmbbK/iSvJ+a4+0owXq79v+lsFkW54bOGbiDQ\" crossorigin=\"anonymous\">",
				"<link href=\"PublishStyle.css\" rel=\"stylesheet\">",
				"<html><body>",

				// ADD IN THE SIDEBAR
				GetDecoratedSidebar(dc),
				
				// ADD IN MAIN CONTENT
				"<div id=\"main\">",
				"<div class=\"heading\" style=\"border-bottom:4px solid " + _colorPairs[dc.GetDataDocument()] + "\">" + dc.Title + "</div>",
				"<div id=\"noteContent\">" + RenderNoteToHtml(dc) + "</div>",
				"</div>",

				// ADD IN LINK CONTENT
				"<div id=\"annotations\">",
				"<div class=\"heading\">Links</div>",
				RenderAllLinksToHtml(dc),
				"</div>",

				// CLOSE THE FINAL TAGS
				"</body></html>"
			};

			return ConcatenateList(fileText);
		}

		/// <summary>
		/// This method gets the sidebar template HTML for the list of DocumentControllers.
		/// </summary>
		/// <param name="dcs"></param>
		/// <returns></returns>
		private string GetSidebarText(IEnumerable<DocumentController> dcs)
		{
			var sidebar = new List<string>();
			sidebar.Add("<div id=\"sidebar\">");

			sidebar.Add("<div class=\"heading\">NOTES</div>");
			sidebar.Add("<ul>");
			sidebar.AddRange(dcs.Select(dc => "<li><i class=\"" + GetIconClass(dc) + " icon\"></i><a href=\"" + _fileNames[dc.GetDataDocument()] + ".html\">" + (dc.Title.Length > 25 ? dc.Title.Substring(0, 25) + "..." : dc.Title) + "</a></li>"));
			sidebar.Add("</ul>");

			sidebar.Add("</div>");

			return ConcatenateList(sidebar);
		}
		
		/// <summary>
		/// Returns the FontAwesome icon class for the given document's type.
		/// </summary>
		/// <param name="dc"></param>
		/// <returns></returns>
		private string GetIconClass(DocumentController dc)
		{
			var content = "";
			switch (dc.DocumentType.Type)
			{
				case "Rich Text Note":
					content += "fas fa-file-alt";
					break;
				case "Markdown Note":
					content += "fas fa-sticky-note";
					break;
				case "Image Note":
					content += "fas fa-image";
					break;
				case "Pdf Note":
					content += "fas fa-file-pdf";
					break;
				case "Video Note":
					content += "fas fa-video";
					break;
				case "Audio Note":
					content += "fas fa-music";
					break;
				default:
					content += "fas fa-file";
					break;
			}

			return content;
		}

		#region RENDERING MAIN CONTENT

		/// <summary>
		/// This method renders the document's content and returns it.
		/// </summary>
		/// <param name="dc"></param>
		/// <param name="regionsToRender">if you want to selectively choose which regions to render (useful for annotation sidebar things), pass in a list here</param>
		/// <param name="truncate">Make the note smaller (used for annotation sidebar things)</param>
		/// <returns></returns>
		private string RenderNoteToHtml(DocumentController dc, List<DocumentController> regionsToRender = null, bool truncate = false)
		{
			var content = "";

			switch (dc.DocumentType.Type)
			{
				case "Rich Text Note":
					content += RenderRichTextToHtml(dc, regionsToRender, truncate);
					break;
				case "Markdown Note":
					content += RenderMarkdownToHtml(dc, regionsToRender, truncate);
					break;
				case "Image Note":
					content += RenderImageToHtml(dc, regionsToRender);
					break;
				case "Pdf Note":
					content += RenderPdfToHtml(dc, regionsToRender, truncate);
					break;
				case "Video Note":
					content += RenderVideoToHtml(dc, regionsToRender);
					break;
				case "Audio Note":
					content += RenderAudioToHtml(dc, regionsToRender);
					break;
                case "Html Note":
                    content += RenderHtmlToHtml(dc, regionsToRender);
                    break;
				default:
					break;
			}

			return content;
		}

		public string RenderHtmlToHtml(DocumentController htmlDoc, List<DocumentController> regionsToRender)
	    {
	        //Debug.Assert(htmlDoc.DocumentType == HtmlNote.DocumentType);

	        var htmlString = (htmlDoc.GetField(KeyStore.DataKey) as TextController)?.Data;
	        var startIndex = htmlString?.IndexOf("<body>");
	        var endIndex = htmlString?.IndexOf("</body>") + 7;
	        if (startIndex > 0)
	        {
	            return htmlString.Substring((int)startIndex, (int)(endIndex - startIndex));
	        }

            // This means the htmlString is a website url
	        return "<iframe src=\"" + htmlString + "\"></iframe>";
	    }

        private string RenderPdfToHtml(DocumentController dc, List<DocumentController> regionsToRender, bool truncate)
		{
			var html = new List<string>();
			var numPages = _pdfNumbers[dc];
			//var regions = regionsToRender ?? dc.GetRegions()?.TypedData;
			var regions = regionsToRender ?? dc.GetRegions().ToList();
			var pageSize = _pdfPageSize[dc];
			var pageNums = new List<int>();

			if (truncate && regionsToRender != null)
			{
				foreach (var region in regionsToRender)
				{
					switch (region.GetAnnotationType())
					{
						case AnnotationType.None:
							break;
						case AnnotationType.Selection:
						case AnnotationType.Region:
							// something about a single selection/region doc being able to contain both types of annotations
							var selectionRegions = region
								.GetDereferencedField<ListController<PointController>>(KeyStore.SelectionRegionTopLeftKey, null);
							if (selectionRegions != null)
							{
								foreach (var point in selectionRegions)
								{
									var page1 = GetPageAtOffset(point.Data.Y);
									pageNums.Add(page1);
								}
							}

							var selectionIndices = region
								.GetDereferencedField<ListController<PointController>>(KeyStore.SelectionIndicesListKey, null);
							break;
						case AnnotationType.Ink:
							break;
						case AnnotationType.Pin:
							var page = GetPageAtOffset(region.GetDereferencedField<PointController>(KeyStore.PositionFieldKey, null).Data.Y);
							pageNums.Add(page);
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
				}

				pageNums = pageNums.Distinct().ToList();
				// at the very least render the first page if there were no regions in it
				if (pageNums.Count == 0) pageNums.Add(1);
			}
			else if (truncate)
			{
				// pure truncation? just display the first page
				pageNums.Add(1);
			}
			else
			// no truncation, just add in all the page numbers to render
			{
				for (var i = 1; i <= numPages; i++)
					pageNums.Add(i);
			}

			// render the whole pdf
			foreach (var i in pageNums)
			{
				var fileName = _fileNames[dc.GetDataDocument()] + "_page" + i + ".jpg";
				var path = "media\\" + _fileNames[dc.GetDataDocument()] + "/" + fileName;
				html.Add("<div class=\"pdf\">");
				html.Add("<img src=\"" + path + "\">");
				html.Add("</div>");
			}

			// do the regioning: use IndexOf to find the appropriate <img src> endpoint to insert the annotation
			if (regions != null)
			{
				foreach (var region in regions)
				{
					switch (region.GetAnnotationType())
					{
						case AnnotationType.Pin:
							var pos = region.GetDereferencedField<PointController>(KeyStore.PositionFieldKey, null);
							if (pos != null)
							{
								var offsets = GetPercentileOffsets(pos.Data.X, pos.Data.Y);

								var indexToInsert = html.IndexOf("<img src=\"media\\" + _fileNames[dc.GetDataDocument()] + "/" + _fileNames[dc.GetDataDocument()] + "_page" + GetPageAtOffset(pos.Data.Y) + ".jpg\">");
								if (indexToInsert < 0)
								{
									Debug.WriteLine("UH OH");
									continue;
								}

								var insert = "<div class=\"tooltip\" style=\"position:absolute; top:" + offsets.Y + "%; left:" + offsets.X +
								             "%;\"><i style=\"font-size: 200%; color:" + _colorPairs[dc.GetDataDocument()] + ";\" class=\"fas fa-thumbtack\"></i>";
								insert += "<span class=\"tooltipItem\">" + RenderNoteToHtml(GetOppositeLinkTarget(region).GetDataDocument()) + "</span></div>";
								//var insert = "<div class=\"pdfPinAnnotation\" style=\"top:" + offsets.Y + "%; left:" +
								//             offsets.X + "%; background-color:" + _colorPairs[dc] + ";\">" + RenderNoteToHtml(GetOppositeLinkTarget(region)) + "</div>";
								//if (!truncate)
								//	insert += "<i style=\"font-size: 200%; position:absolute; top:" + offsets.Y + "%; left:" + offsets.X +
								//	          "%; color:" + _colorPairs[dc] + ";\" class=\"fas fa-thumbtack\"></i></div>";
								html.Insert(indexToInsert + 1, insert); // insert an ellipse to go right after the img src thing
							}
							break;
						case AnnotationType.Region:
							var point = region.GetDereferencedField<ListController<PointController>>(KeyStore.SelectionRegionTopLeftKey, null).First().Data;
							var size = region.GetDereferencedField<ListController<PointController>>(KeyStore.SelectionRegionSizeKey, null).First().Data;
							var offset = GetPercentileOffsets(point.X + 20, point.Y + 20);
							var oneTarget = GetOppositeLinkTarget(region);
							if (oneTarget != null)
							{
								var index = html.IndexOf("<img src=\"media\\" + _fileNames[dc.GetDataDocument()] + "/" + _fileNames[dc.GetDataDocument()] + "_page" + GetPageAtOffset(point.Y) + ".jpg\">");
								if (index < 0)
								{
									Debug.WriteLine("UH OH");
									continue;
								}
                                if (!_fileNames.ContainsKey(oneTarget.GetDataDocument()))
                                      _fileNames.Add(oneTarget.GetDataDocument(), GetFileName(oneTarget.GetDataDocument()));
                                if (!_colorPairs.ContainsKey(oneTarget.GetDataDocument()))
                                    _colorPairs.Add(oneTarget.GetDataDocument(), _regionColors[0]);
                                var rect = "<a href=\"" + _fileNames[oneTarget.GetDataDocument()] + ".html\"><svg class=\"pdfOverlay\" height=\"" + size.Y / pageSize.Height * 100 + "%\" width=\"" + size.X / pageSize.Width * 100 + "%\" style=\"position:absolute; top:" + 
								           offset.Y + "%; left:" + offset.X + "%\"><rect height=\"100%\" width=\"100%\" style=\"fill:" + _colorPairs[oneTarget.GetDataDocument()] + "\"></rect></svg></a>";
								html.Insert(index + 1, rect);
							}

							break;
						case AnnotationType.Selection:
							break;
						default:
							break;
					}
				}

				Point GetPercentileOffsets(double xpos, double ypos)
				{
					var pageNumber = GetPageAtOffset(ypos);

					// the 8 and 16 constants are from the fact that it looks like the height is affected by some external XAML visual bound thing...?
					var pageTop = (pageSize.Height + 8) * (pageNumber - 1) + 8;
					var yDiff = ((ypos - pageTop - 16) / (pageSize.Height + 10) * 100);
					var xDiff = (xpos - 16) / (pageSize.Width) * 100;
					return new Point(xDiff, yDiff);
				}
			}

			// this method takes in the vertical offset of something and returns what page of the PDF it's on
			int GetPageAtOffset(double verticalOffset)
			{
				int page = 1;
				while (verticalOffset > (pageSize.Height + 10) * page)
				{
					page++;
				}
				return page;
			}

			return ConcatenateList(html);
		}

	    private DocumentController ValidateLink(DocumentController link, KeyController sourceOrDestination)
	    {
	        var linkOperator = link.GetDataDocument().GetDereferencedField<BoolController>(LinkDescriptionTextOperator.ShowDescription, null);
	        if (linkOperator == null || linkOperator.Data == false)
	        {
	            link = link.GetDataDocument().GetDereferencedField<DocumentController>(sourceOrDestination, null);
            }

	        return link;
	    }

        private DocumentController GetOppositeLinkTarget(DocumentController link)
		{
			DocumentController oneTarget = null; // most of the time, each region will only link to one target, and this variable describes it.

			var regionLinkTo = link.GetDataDocument().GetLinks(KeyStore.LinkToKey);
			var regionLinkFrom = link.GetDataDocument().GetLinks(KeyStore.LinkFromKey);
			// trying to see if the one target is linkTo or linkFrom, and if it's one of them, set it to that target
			if ((regionLinkTo == null || regionLinkTo.Count == 0) && regionLinkFrom != null && regionLinkFrom.Count == 1)
			{
				oneTarget = regionLinkFrom.First().GetDataDocument().GetDereferencedField<DocumentController>(KeyStore.LinkSourceKey, null);
            }
			else if ((regionLinkFrom == null || regionLinkFrom.Count == 0) && regionLinkTo != null && regionLinkTo.Count == 1)
			{
				oneTarget = regionLinkTo.First().GetDataDocument().GetDereferencedField<DocumentController>(KeyStore.LinkDestinationKey, null);
            }

			return oneTarget;
		}

		private string RenderRichTextToHtml(DocumentController dc, List<DocumentController> regionsToRender = null, bool truncate = false)
		{
			var plainText = dc.GetDataDocument().GetDereferencedField<TextController>(KeyStore.DocumentTextKey, null).Data;
			var richText = dc.GetDataDocument().GetDereferencedField<RichTextController>(KeyStore.DataKey, null).Data.RtfFormatString;
			// TODO: replace URLs linked to actual websites as well
			
			// do the regioning
			var regions = regionsToRender ?? dc.GetDataDocument().GetRegions()?.ToList();
			var stringsToInsert = new SortedDictionary<int, string>();
			if (regions != null)
			{
				foreach (var region in regions)
				{
					string htmlToInsert = "<b>"; // this is the string of formatting applied at the start of the link
					var oneTarget = GetOppositeLinkTarget(region);

					if (oneTarget != null)
					{
						// if we found a oneTarget, that might actually be a region, so check for that here and make sure we're looking at the big document.
						if (oneTarget.GetRegionDefinition() != null)
						{
							oneTarget = oneTarget.GetRegionDefinition();
						}
						// insert the appropriate color pair.
						htmlToInsert = "<a href=\"" + _fileNames[oneTarget.GetDataDocument()] + ".html\" class=\"inlineLink\"><b style=\"color:" + _colorPairs[oneTarget.GetDataDocument()] + "\">";
					}
					var regionText = region.GetDataDocument().GetDereferencedField<TextController>(KeyStore.DocumentTextKey, null).Data;

					var startIndex = plainText.IndexOf(regionText, StringComparison.Ordinal);
					stringsToInsert.Add(startIndex, htmlToInsert);
					stringsToInsert.Add(startIndex + regionText.Length, "</b></a>");
				}

				// insert the HTML into the plaintext, going from last position to first position
				plainText = stringsToInsert.Keys.Reverse().Aggregate(plainText, (current, pos) => current.Insert(pos, stringsToInsert[pos]));
			}

			if (truncate && plainText.Length > 500)
			{
				// try to figure out where the links are and get long text to some reasonable length
				var truncated = "";
				var remainingText = plainText;
				var totalKeys = stringsToInsert.Keys.Count;
				if (totalKeys != 0)
				{
					var threshold = 500 / totalKeys / 2; // this is how much space is roughly "allocated" to each link
					for (int i = 0; i < totalKeys; i++)
					{
						var nextIndex =
							remainingText.IndexOf(GetString(i), StringComparison.Ordinal); // next place that it occurs in the remaining text
						if (nextIndex > threshold / 2) // divide by two since threshold is front and back
						{
							// was too far away, so now we have to truncate off the front a bit
							var diff = nextIndex - threshold;
							truncated += " ... ";
							try
							{
								diff = remainingText.IndexOf(" ", diff, StringComparison.Ordinal) < 0 ? diff : remainingText.IndexOf(" ", diff, StringComparison.Ordinal);
							}
							catch (Exception)
							{
								// ignored
							}
							
							remainingText = remainingText.Substring(diff);
						}

						// now within acceptable distance, we want to find the end of the URL and add it to truncated
						i++;
						var lastLinkIndex = remainingText.IndexOf(GetString(i), StringComparison.Ordinal) + GetString(i).Length;
						try
						{
							var heuristic =
								remainingText.IndexOf(" ", lastLinkIndex + threshold / 3,
									StringComparison.Ordinal); // heuristically find the next occurrence of a word break to stop at
							lastLinkIndex =
								heuristic < 0
									? lastLinkIndex
									: heuristic; // if it returned -1, then it means it didn't actually find the space, so keep where it was before
						}
						catch (Exception)
						{
							// ignored
						}

						truncated += remainingText.Substring(0, lastLinkIndex);
						remainingText =
							remainingText.Substring(lastLinkIndex); // remove everything up to this point
					}

					if (remainingText.Length < threshold / 4)
					{
						truncated += remainingText;
					}
					else
					{
						truncated += "...";
					}

					string GetString(int i)
					{
						return stringsToInsert[stringsToInsert.Keys.ToList()[i]];
					}

					plainText = truncated;
				}
				else
				{
					var heuristicWrap = plainText.IndexOf(" ", 500, StringComparison.Ordinal);
					if (heuristicWrap < 0) heuristicWrap = 500;
					plainText = plainText.Substring(0, heuristicWrap) + "...";
				}
			}

			// be careful that this line needs to go after the hyperlink regions, or you'll be messing up the indicing 
			plainText = plainText.Replace("\n", "<br/>");
			return plainText;
		}

		private string RenderMarkdownToHtml(DocumentController dc, List<DocumentController> regionsToRender = null, bool truncate = false)
		{
			var content = dc.GetDataDocument().GetDereferencedField<TextController>(KeyStore.DocumentTextKey, null).Data;
			var result = CommonMark.CommonMarkConverter.Convert(content);
			if (result.Length > 500 && truncate)
			{
				result = result.Substring(0, 500);
				result += "...";
			}
			return result;
		}

		private string RenderAudioToHtml(DocumentController dc, List<DocumentController> regionsToRender = null)
		{
		    if (_fileNames.ContainsKey(dc.GetDataDocument()))
		    {
		        var audioTitle = "aud_" + _fileNames[dc.GetDataDocument()] + ".mp3";
		        var path = "Media\\" + audioTitle;
		        return "<audio controls><source src=\"" + path + "\"> Your browser doesn't support the audio tag :( </audio>";
            }

		    return "";
		}

		private string RenderImageToHtml(DocumentController dc, List<DocumentController> regionsToRender = null)
		{
			var regions = regionsToRender ?? dc.GetDataDocument().GetRegions()?.ToList();
		    var dataDoc = dc.GetDataDocument();
		    var html = "";
		    if (_fileNames.ContainsKey(dataDoc))
		    {
		        var imgTitle = "img_" + _fileNames[dc.GetDataDocument()] + ".jpg";
		        var path = "Media\\" + imgTitle;
		        html = "<div class=\"imgNote\"><img src=\"" + path + "\">";

		        if (regions != null)
		        {
		            foreach (var region in regions)
		            {
		                switch (region.GetAnnotationType())
		                {
		                case AnnotationType.Region:
		                    // currently the only supported one for images?
		                    var oneTarget = GetOppositeLinkTarget(region);
		                    var point = region.GetDereferencedField<ListController<PointController>>(KeyStore.SelectionRegionTopLeftKey, null)?.First()?.Data;
		                    var size = region.GetDereferencedField<ListController<PointController>>(KeyStore.SelectionRegionSizeKey, null)?.First()?.Data;
		                    var imgSize = region.GetDereferencedField<DocumentController>(KeyStore.RegionDefinitionKey, null)?
		                        .GetDereferencedField<PointController>(KeyStore.InitialSizeKey, null)?.Data;
		                    if (oneTarget != null && point is Point pt && size is Point sz && imgSize is Point imgSz)
		                    {
		                        var rect = "<a href=\"" + _fileNames[oneTarget.GetDataDocument()] + ".html\"><svg class=\"pdfOverlay\" height=\"" +
		                                   sz.Y / imgSz.Y * 100 + "%\" width=\"" + sz.X / imgSz.X * 100 +
		                                   "%\" style=\"position:absolute; top:" +
		                                   pt.Y / imgSz.Y * 100 + "%; left:" + pt.X / imgSz.X * 100 + "%\"><rect height=\"100%\" width=\"100%\" style=\"fill:" + _colorPairs[oneTarget.GetDataDocument()] + "\"></rect></svg></a>";
		                        html += rect;
		                    }
		                    break;
		                default:
		                    break;
		                }
		            }
		        }

		        html += "</div>";
            }
			return html;
		}

		private string RenderVideoToHtml(DocumentController dc, List<DocumentController> regionsToRender = null)
		{
			// distinguish between YouTube and file-linked videos
			if (dc.GetDataDocument().GetDereferencedField<TextController>(KeyStore.YouTubeUrlKey, null) != null)
			{
				var url = dc.GetDataDocument().GetDereferencedField<TextController>(KeyStore.YouTubeUrlKey, null).Data;
				return "<iframe src=\"" + url + "\"></iframe></div>";
			}

            // if not a YouTube video, then it's on here
		    if (_fileNames.ContainsKey(dc.GetDataDocument()))
		    {
		        var vidTitle = "vid_" + _fileNames[dc.GetDataDocument()] + ".mp4";
		        var path = "Media\\" + vidTitle;
		        return "<video controls><source src=\"" + path +
		               "\" > Your browser doesn't support the video tag :( </video>";
		    }

		    return "";
		}

		#endregion

		#region RENDERING LINKING

		/// <summary>
		/// This method takes in a DocumentController and finds all of its links, renders each of them to an annotationWrapper CSS class, and returns it all in one string.
		/// </summary>
		/// <param name="dc"></param>
		/// <returns></returns>
		private string RenderAllLinksToHtml(DocumentController dc)
		{
			// first get all of the document's immediate links (not through its regions)
			var html = new List<string> {RenderImmediateLinksToHtml(dc)};

			// then add in its regions
		    var regions = dc.GetDataDocument().GetRegions();
			if (regions != null)
			{
				html.AddRange(regions.Select(RenderImmediateLinksToHtml));
			}

			//if (dc.DocumentType.Type.Equals("Pdf Note"))
			//{
			//	var annotations = dc.GetDereferencedField<ListController<DocumentController>>(KeyStore.PinAnnotationsKey, null)?.Select(pin => pin);
			//	if (annotations != null)
			//	{
			//		html.AddRange(annotations.Select(RenderLinkToHtml));
			//	}
			//}

			return ConcatenateList(html);
		}

		/// <summary>
		/// This method returns all of the to and from targets in HTML, whether it's a main document or a region.
		/// </summary>
		/// <param name="dc"></param>
		/// <param name="parent"></param>
		/// <returns></returns>
		private string RenderImmediateLinksToHtml(DocumentController dc)
		{
		    dc = dc.GetDataDocument(); // precaution
			var html = new List<string>();

            // in the case we're rendering an intermediate document
		    var linkSource = dc.GetLinkedDocument(LinkDirection.ToDestination);
		    if (linkSource != null)
		    {
		        html.Add(RenderLinkToHtml(linkSource));
		    }

		    var linkDestination = dc.GetLinkedDocument(LinkDirection.ToSource);
		    if (linkSource != null)
		    {
                html.Add(RenderLinkToHtml(linkDestination));
		    }

            // cycle through each link/region's links as necessary, building a new HTML segment as we go
            var linksTo = dc.GetLinks(KeyStore.LinkToKey);
			if (linksTo != null)
			{
				// linksFrom uses LinkDestination to get the opposite document
				foreach (var link in linksTo)
				{
					html.Add(RenderLinkToHtml(ValidateLink(link, KeyStore.LinkDestinationKey)));
				}
			}

			var linksFrom = dc.GetLinks(KeyStore.LinkFromKey);
			if (linksFrom != null)
			{
				// linksFrom uses LinkSource to get the opposite document
				foreach (var link in linksFrom)
				{
					html.Add(RenderLinkToHtml(ValidateLink(link, KeyStore.LinkSourceKey)));
				}
			}

			return ConcatenateList(html);
		}

		/// <summary>
		/// This method takes in an annotation's DocumentController and renders it in an annotationWrapper CSS class, using RenderNoteToHtml to help with it.
		/// </summary>
		/// <param name="annotation"></param>
		/// <param name="linkTitle"></param>
		/// <returns></returns>
		private string RenderLinkToHtml(DocumentController annotation)
		{
			DocumentController region = null;
			DocumentController parentAnnotation = annotation;
			bool hasOwnPage = true;
			List<string> html;
			if (!_fileNames.ContainsKey(annotation.GetDataDocument()))
			{
				// if it wasn't found in the filename, then it means that we're annotating to a region.
				var parent = annotation.GetRegionDefinition();
				// if the parent is also null, then this annotation doesn't have a file to link to. In that case, we don't actually link it to anything.
				if (parent != null && _fileNames.ContainsKey(parent))
				{
					region = annotation;
					parentAnnotation = parent;
				}
				else
				{
					hasOwnPage = false;
				}
			}

			if (hasOwnPage)
			{
				html = new List<string>
				{
					"<div class=\"annotationWrapper\">",
					"<div>",
					"<div style=\"border-left:3px solid " + _colorPairs[parentAnnotation.GetDataDocument()] + "\"/>",
					"<div class=\"annotation\">",
					RenderNoteToHtml(parentAnnotation.GetDataDocument(), region == null ? new List<DocumentController>() : new List<DocumentController> {region}, true),
					"</div>", // close annotation tag
					"</div>", // close top area div tag
					"<div class=\"annotationLink\"><a href=\"" + _fileNames[parentAnnotation.GetDataDocument()] + ".html\">" + parentAnnotation.Title + "</a></div>",
					"</div>" //close the annotationWrapper tag
				};
			}
			else
			{
				html = new List<string>
				{
					"<div class=\"annotationWrapper\">",
					"<div>",
					"<div><i class=\"fas fa-thumbtack\"></i></div>",
					"<div class=\"annotation\">",
					RenderNoteToHtml(parentAnnotation.GetDataDocument(), new List<DocumentController>(), true),
					"</div>",
					"</div>",
					"</div>"
				};
			}
			return ConcatenateList(html);
		}

		#endregion 

		#region UTIL

		/// <summary>
		/// This takes the templated sidebar and adds in the decorations necessary to indicate the active link currently.
		/// </summary>
		/// <param name="dc"></param>
		/// <returns></returns>
		private string GetDecoratedSidebar(DocumentController dc)
		{
			string newSidebar = _sidebarText;
			return newSidebar.Replace("<a href=\"" + _fileNames[dc.GetDataDocument()] + ".html\">",
				"<a href =\"" + _fileNames[dc.GetDataDocument()] + ".html\" class=\"activeNote\">");
		}

		/// <summary>
		/// This method gets the user to pick a folder to save the exported materials in.
		/// </summary>
		/// <returns></returns>
		private async Task<StorageFolder> PickFolder()
		{
			StorageFolder stFolder;
			if (!(Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.Phone.UI.Input.HardwareButtons")))
			{
				//create folder picker with basic properties
				var savePicker = new FolderPicker
				{
					SuggestedStartLocation = PickerLocationId.Desktop,
					ViewMode = PickerViewMode.Thumbnail
				};
				savePicker.FileTypeFilter.Add("*");

				stFolder = await savePicker.PickSingleFolderAsync();

				//return the folder that the user picked - it is a task bc async
				return stFolder;
			}

			//If the user can't pick a folder, it just makes a folder in data called Dash
			var local = ApplicationData.Current.LocalFolder;
			stFolder = await local.CreateFolderAsync("Dash");
			return stFolder;
		}

		/// <summary>
		/// This method returns a collision-free filename for each DocumentController.
		/// </summary>
		/// <param name="dc"></param>
		/// <returns></returns>
		private string GetFileName(DocumentController dc)
		{
			var rawTitle = dc.GetDereferencedField<TextController>(KeyStore.TitleKey, null).Data ?? "Untitled";
			if (_fileCollisions.ContainsKey(rawTitle))
			{
				// get the last collision number associated with this title
				var lastCollisionCount = _fileCollisions[rawTitle];
				//increment it 
				_fileCollisions[rawTitle]++;
				// append the new collision count
				rawTitle += "(" + (lastCollisionCount + 1) + ")";
			}
			else
			{
				_fileCollisions.Add(rawTitle, 0);
			}

			var safeTitle = RemoveUnsafeCharacters(rawTitle);
			return safeTitle;
		}

		/// <summary>
		/// Get a safe title to save in the file.
		/// </summary>
		/// <param name="title"></param>
		/// <returns></returns>
		private string RemoveUnsafeCharacters(string title)
		{
			return title.Replace('/', '-').Replace('\\', '-').Replace(':', '-').Replace('?', '-')
				.Replace('*', '-').Replace('"', '-').Replace('<', '-').Replace('>', '-').Replace('|', '-')
				.Replace('#', '-');
		}

		private string ConcatenateList(List<string> list)
		{
			var contents = "";
			foreach (var text in list)
			{
				contents += text;
			}

			return contents;
		}

		/// <summary>
		/// Writes a text file to the folder.
		/// </summary>
		/// <param name="fileContents"></param>
		/// <param name="fileName"></param>
		/// <returns></returns>
		private async Task CreateFile(string fileContents, string fileName)
		{
			if (_folder == null) return;

			fileName += ".html";
			var file = await _folder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);

			if (file == null) return;

			await FileIO.WriteTextAsync(file, fileContents);
		}

		/// <summary>
		/// This method populates the _fileNames dictionary, which the sidebar needs to link files. It doesn't create anything or write anything.
		/// </summary>
		/// <param name="dcs"></param>
		private void BuildFileNames(List<DocumentController> dcs)
		{
			foreach (var dc in dcs)
			{
				_fileNames.Add(dc, GetFileName(dc));
			}
		}

		/// <summary>
		/// Initializes all the dictionaries.
		/// </summary>
		private void InitializeColorPairs(List<DocumentController> dcs)
		{
			var i = 0;
			foreach (var dc in dcs)
			{
				_colorPairs.Add(dc, _regionColors[i++]);
				if (i > 9) i = 0;
			}
		}

		#endregion

		#region COPYING MEDIA 

		/// <summary>
		/// Copies a file to the Media folder.
		/// </summary>
		/// <param name="file"></param>
		/// <param name="newName"></param>
		private async Task CopyMedia(IStorageFile file, string newName)
		{
			// if we haven't made a Media folder yet, make it now
			if (!_mediaFolderMade)
			{
				_mediaFolderMade = true;
				await _folder.CreateFolderAsync("Media");
			}

			await file.CopyAsync(await _folder.GetFolderAsync("Media"), newName);
		}

		/// <summary>
		/// Copies a file to the Media folder.
		/// </summary>
		/// <param name="rawUri"></param>
		/// <param name="newName"></param>
		/// <returns></returns>
		private async Task CopyMedia(string rawUri, string newName)
		{
			var parts = rawUri.Split('/');
			var url = parts[parts.Length - 1];
            try
            {
                var file = await ApplicationData.Current.LocalFolder.GetFileAsync(url);
                await CopyMedia(file, newName);
            } catch (Exception)
            {

            }
		}

		/// <summary>
		/// This method copies all of the media from each of the documents. This is the only time these files are written.
		/// </summary>
		/// <param name="dcs"></param>
		private async Task CopyMedia(IEnumerable<DocumentController> dcs)
		{
			foreach (var dc in dcs)
			{
				switch (dc.DocumentType.Type)
				{
					case "Image Note":
						await CopyImage(dc);
						break;
					case "Pdf Note":
						await CopyPdf(dc);
						break;
					case "Video Note":
						await CopyVideo(dc);
						break;
					case "Audio Note":
						await CopyAudio(dc);
						break;
					default:
						break;
				}
			}
		}

		/// <summary>
		/// Pdfs are copied as a series of images.
		/// </summary>
		/// <returns></returns>
		private async Task CopyPdf(DocumentController dc)
		{
			// if we haven't made a Media folder yet, make it now
			if (!_mediaFolderMade)
			{
				_mediaFolderMade = true;
				await _folder.CreateFolderAsync("Media");
			}

			var media = await _folder.GetFolderAsync("Media");
			var folder = await media.CreateFolderAsync(_fileNames[dc]);

			var pdf = await DataVirtualizationSource.GetPdf(dc);
			var numPages = pdf.PageCount;
			_pdfNumbers[dc] = (int) numPages;
			var sizee = dc.GetDereferencedField<PointController>(KeyStore.PdfHeightKey, null).Data;
			_pdfPageSize[dc] = new Size(sizee.X, sizee.Y);

			// get selectableelements
			var pdfUri = dc.GetField<PdfController>(KeyStore.DataKey).Data;
			var file = await StorageFile.GetFileFromPathAsync(pdfUri.LocalPath);
            var reader = new PdfReader(await file.OpenStreamForReadAsync());
			var pdfDocument = new PdfDocument(reader);
			var strategy = new BoundsExtractionStrategy();
			var processor = new PdfCanvasProcessor(strategy);
			double offset = 0;

			await Task.Run(() =>
			{
				for (var i = 1; i <= pdfDocument.GetNumberOfPages(); ++i)
				{
					var page = pdfDocument.GetPage(i);
					var size = page.GetPageSize();
					strategy.SetPage(i - 1, offset, size, page.GetRotation());
					offset += page.GetPageSize().GetHeight() + 10;
					processor.ProcessPageContent(page);
				}
			});

			var selectableElements = strategy.GetSelectableElements(0, pdfDocument.GetNumberOfPages());
			_pdfSelectableElements[dc] = selectableElements.elements;

			for (var i = 0; i < numPages; i++)
			{
				var bitmap = await DataVirtualizationSource.GetImageFromPdf(pdf, (uint)i);
				var newFile = await folder.CreateFileAsync(_fileNames[dc.GetDataDocument()] + "_page" + (i + 1) + ".jpg",
					CreationCollisionOption.GenerateUniqueName);
				using (var stream = await newFile.OpenStreamForWriteAsync())
				{
					var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream.AsRandomAccessStream());
					var pixelStream = bitmap.PixelBuffer.AsStream();
					byte[] pixels = new byte[bitmap.PixelBuffer.Length];

					await pixelStream.ReadAsync(pixels, 0, pixels.Length);
					encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)bitmap.PixelWidth, (uint)bitmap.PixelHeight, 96,
						96, pixels);
					await encoder.FlushAsync();
				}
			}
		}

		private async Task CopyAudio(DocumentController dc)
		{
			var uriRaw = dc.GetDereferencedField(KeyStore.DataKey, null);
			if (uriRaw != null)
			{
				var olduri = uriRaw.ToString();

				// create file with unique title
				var audTitle = "aud_" + _fileNames[dc.GetDataDocument()] + ".mp3";
				await CopyMedia(olduri, audTitle);
			}
		}

		private async Task CopyVideo(DocumentController dc)
		{
			// youtube videos don't have a saved file for copying
			if (dc.GetDereferencedField<TextController>(KeyStore.YouTubeUrlKey, null) != null) return;
			var uriRaw = dc.GetDereferencedField(KeyStore.DataKey, null);
			if (uriRaw != null)
			{
				var olduri = uriRaw.ToString();

				// create file with unique title
				var vidTitle = "vid_" + _fileNames[dc.GetDataDocument()] + ".mp4";
				await CopyMedia(olduri, vidTitle);
			}
		}

		private async Task CopyImage(DocumentController dc)
		{
			//string version of the image uri
			var uriRaw = dc.GetDereferencedField<ImageController>(KeyStore.DataKey, null);
			if (uriRaw != null)
			{
				var olduri = uriRaw.ToString();

				// create file with unique title
				var imgTitle = "img_" + _fileNames[dc.GetDataDocument()] + ".jpg";
				await CopyMedia(olduri, imgTitle);
			}
		}

		#endregion
	}
}
