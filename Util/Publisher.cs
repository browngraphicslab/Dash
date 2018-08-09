using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

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
				CopyMedia(dcs);

				// get the sidebar thing going
				_sidebarText = GetSidebarText(dcs);

				// create a new file for every DocumentController
				foreach (var dc in dcs)
				{
					var fileName = _fileNames[dc];
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
				"<link rel=\"stylesheet\" href=\"https://cdnjs.cloudflare.com/ajax/libs/font-awesome/4.7.0/css/font-awesome.min.css\">",
				"<link href=\"PublishStyle.css\" rel=\"stylesheet\">",
				"<html><body>",

				// ADD IN THE SIDEBAR
				GetDecoratedSidebar(dc),
				
				// ADD IN MAIN CONTENT
				"<div id=\"main\">",
				"<div class=\"heading\" style=\"border-bottom:4px solid " + _colorPairs[dc] + "\">" + dc.Title + "</div>",
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
			sidebar.AddRange(dcs.Select(dc => "<li><a href=\"" + _fileNames[dc] + ".html\">" + dc.Title + "</a></li>"));
			sidebar.Add("</ul>");

			sidebar.Add("</div>");

			return ConcatenateList(sidebar);
		}

		#region RENDERING MAIN CONTENT

		/// <summary>
		/// This method renders the document's content and returns it.
		/// </summary>
		/// <param name="dc"></param>
		/// <param name="regionsToRender">if you want to selectively choose which regions to render (useful for annotation sidebar things), pass in a list here</param>
		/// <returns></returns>
		private string RenderNoteToHtml(DocumentController dc, List<DocumentController> regionsToRender = null)
		{
			var content = "";
			switch (dc.DocumentType.Type)
			{
				case "Rich Text Note":
					content += RenderRichTextToHtml(dc, regionsToRender);
					break;
				case "Markdown Note":
					content += RenderMarkdownToHtml(dc, regionsToRender);
					break;
				case "Image Note":
					content += RenderImageToHtml(dc, regionsToRender);
					break;
				case "Pdf Note":
					break;
				case "Video Note":
					content += RenderVideoToHtml(dc, regionsToRender);
					break;
				case "Audio Note":
					break;
				default:
					break;
			}

			return content;
		}

		// TODO HI THERE tomorrow make every document have its own color instead of each pair having a color, and display the color below the title. then do youtube.

		private string RenderRichTextToHtml(DocumentController dc, List<DocumentController> regionsToRender = null)
		{
			var plainText = dc.GetDereferencedField<TextController>(KeyStore.DocumentTextKey, null).Data;
			var richText = dc.GetDereferencedField<RichTextController>(KeyStore.DataKey, null).Data.RtfFormatString;
			// TODO: replace URLs linked to actual websites as well
			
			// do the regioning
			var regions = regionsToRender ?? dc.GetRegions().TypedData; //?.Select(region => region.GetDataDocument());
			if (regions != null)
			{
				var stringsToReplace = new SortedDictionary<int, string>(); // int represents where to insert, string represents what to insert.
				foreach (var region in regions)
				{
					var regionLinkTo = region.GetDataDocument().GetLinks(KeyStore.LinkToKey);
					var regionLinkFrom = region.GetDataDocument().GetLinks(KeyStore.LinkFromKey);
					DocumentController oneTarget = null; // most of the time, each region will only link to one target, and this variable describes it.
					string htmlToInsert = "<b>"; // this is the string of formatting applied at the start of the link

					// trying to see if the one target is linkTo or linkFrom, and if it's one of them, set it to that target
					if (regionLinkTo == null && regionLinkFrom != null && regionLinkFrom.Count == 1)
					{
						oneTarget = regionLinkFrom.TypedData.First().GetDataDocument().GetDereferencedField<DocumentController>(KeyStore.LinkSourceKey, null).GetDataDocument();
					}
					else if (regionLinkFrom == null && regionLinkTo != null && regionLinkTo.Count == 1)
					{
						oneTarget = regionLinkTo.TypedData.First().GetDataDocument().GetDereferencedField<DocumentController>(KeyStore.LinkDestinationKey, null).GetDataDocument();
					}

					if (oneTarget == null) continue;
					// if we found a oneTarget, that might actually be a region, so check for that here and make sure we're looking at the big document.
					if (oneTarget.GetRegionDefinition() != null)
					{
						oneTarget = oneTarget.GetRegionDefinition().GetDataDocument();
					}
					// insert the appropriate color pair.
					htmlToInsert = "<a href=\"" + _fileNames[oneTarget] + ".html\" class=\"inlineLink\"><b style=\"color:" + _colorPairs[oneTarget] + "\">";

					var regionText = region.GetDataDocument().GetDereferencedField<TextController>(KeyStore.DocumentTextKey, null).Data;
					//var startIndex = plainText.IndexOf(regionText, StringComparison.Ordinal);
					var startAndEndIndices = HyperlinkIndex(richText, region.Id, regionText);
					if (startAndEndIndices[0] < 0) continue;
					stringsToReplace.Add(startAndEndIndices[1], "</b></a>");
					stringsToReplace.Add(startAndEndIndices[0], htmlToInsert);
				}

				plainText = stringsToReplace.Keys.Reverse().Aggregate(plainText, (current, key) => current.Insert(key, stringsToReplace[key]));
			}
			
			// be careful that this line needs to go after the hyperlink regions, or you'll be messing up the indicing 
			plainText = plainText.Replace("\n", "<br/>");

			return plainText;
		}

		private string RenderMarkdownToHtml(DocumentController dc, List<DocumentController> regionsToRender = null)
		{
			var content = dc.GetDereferencedField<TextController>(KeyStore.DocumentTextKey, null).Data;
			content = content.Replace("\n", "<br/>");
			return content;
		}

		private string RenderImageToHtml(DocumentController dc, List<DocumentController> regionsToRender = null)
		{
			var imgTitle = "img_" + _fileNames[dc] + ".jpg";
			var path = "media\\" + imgTitle;
			return "<img src=\"" + path + "\">";
		}

		//TODO: deal with YouTube videos
		private string RenderVideoToHtml(DocumentController dc, List<DocumentController> regionsToRender = null)
		{
			var vidTitle = "vid_" + _fileNames[dc] + ".mp4";
			var path = "media\\" + vidTitle;
			return "<video controls><source src=\"" + path + "\" > Your browser doesn't support the video tag :( </video>";
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
			var html = new List<string> {RenderImmediateLinksToHtml(dc)};

			var regions = dc.GetRegions()?.Select(region => region.GetDataDocument());
			if (regions != null)
			{
				html.AddRange(regions.Select(RenderImmediateLinksToHtml));
			}

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
			var html = new List<string>();

			// cycle through each link/region's links as necessary, building a new HTML segment as we go
			var linksTo = dc.GetLinks(KeyStore.LinkToKey)?.TypedData;
			if (linksTo != null)
			{
				// linksFrom uses LinkDestination to get the opposite document
				foreach (var link in linksTo)
				{
					html.Add(RenderLinkToHtml(link.GetDataDocument().GetDereferencedField<DocumentController>(KeyStore.LinkDestinationKey, null).GetDataDocument(), link.Title));
				}
			}

			var linksFrom = dc.GetLinks(KeyStore.LinkFromKey)?.TypedData;
			if (linksFrom != null)
			{
				// linksFrom uses LinkSource to get the opposite document
				foreach (var link in linksFrom)
				{
					html.Add(RenderLinkToHtml(link.GetDataDocument().GetDereferencedField<DocumentController>(KeyStore.LinkSourceKey, null).GetDataDocument(), link.Title));
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
		private string RenderLinkToHtml(DocumentController annotation, string linkTitle)
		{
			DocumentController region = null;
			DocumentController parentAnnotation = annotation;
			if (!_fileNames.ContainsKey(annotation))
			{
				// if it wasn't found in the filename, then it means that we're annotating to a region.
				// TODO: in the future stylize the regions a bit, e.g. only excerpts of text, a region over an image, etc.
				var parent = annotation.GetDataDocument().GetRegionDefinition().GetDataDocument();
				if (_fileNames.ContainsKey(parent))
				{
					region = annotation;
					parentAnnotation = parent;
				}
			}

			var html = new List<string>
			{
				"<div class=\"annotationWrapper\">",
				"<div>",
				"<div style=\"border-left:3px solid " + _colorPairs[parentAnnotation] + "\"/>",
				"<div class=\"annotation\">",
				RenderNoteToHtml(parentAnnotation, region == null ? null : new List<DocumentController> {region}),
				"</div>", // close annotation tag
				"</div>", // close top area div tag
				"<div class=\"annotationLink\"><a href=\"" + _fileNames[parentAnnotation] + ".html\">" + linkTitle + " → " + parentAnnotation.Title +
				"</a></div>",
				"</div>" //close the annotationWrapper tag
			};
			return ConcatenateList(html);
		}

		#endregion

		#region UTIL

		/// <summary>
		/// Gets the start and end indices of a hyperlink in plaintext when given richtext in the form of an int array. Returns -1 in the first field if the queryId isn't found
		/// </summary>
		private int[] HyperlinkIndex(string richText, string queryId, string query)
		{
			int len = query.Length;
			bool ignore = false;
			int curIndex = 0;
			string fieldString = "";
			int field = 0; //the number of curly braces (once the entire field property is closed, then we can continue search)
			int[] modIndex = new int[2];

			for (int i = richText.IndexOf(' ', richText.IndexOf("\\fs", StringComparison.Ordinal)) + 1; i < richText.Length; i++)
			{
				if (field > 0)
				{
					if (richText[i] == '{')
						field += 1;
					else if (richText[i] == '}')
						field -= 1;
					fieldString += richText[i];

					if (field == 0 && fieldString.Contains(queryId))
					{
						modIndex[0] = curIndex;
						modIndex[1] = curIndex + len;
						return modIndex;
					}
				}
				else if (ignore)
				{
					if (richText.Length > i + 7 && richText.Substring(i, 7).Equals("{\\field"))
					{
						field += 1;
						ignore = false;
					}
					if (richText[i] == ' ' || richText[i] == '\n' || richText[i] == '\r' || richText[i] == '\t' || richText[i] == '~')
					{
						ignore = false;
					}
				}
				else
				{
					if (richText.Length > i + 7 && richText.Substring(i, 7).Equals("{\\field"))
						field += 1;
					else if (richText.Length > i + 4 && richText.Substring((i, 4).Equals("\\par")))
						curIndex++;
					else if (richText[i] == '\\')
					{
						ignore = true;
					}
					else
					{
						curIndex++;
					}
				}
			}
			modIndex[0] = -1;
			return modIndex;
		}


		/// <summary>
		/// This takes the templated sidebar and adds in the decorations necessary to indicate the active link currently.
		/// </summary>
		/// <param name="dc"></param>
		/// <returns></returns>
		private string GetDecoratedSidebar(DocumentController dc)
		{
			string newSidebar = _sidebarText;
			return newSidebar.Replace("<a href=\"" + _fileNames[dc] + ".html\">",
				"<a href =\"" + _fileNames[dc] + ".html\" class=\"activeNote\">");
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
			var rawTitle = dc.GetDataDocument().GetDereferencedField<TextController>(KeyStore.TitleKey, null).Data ?? "Untitled";
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
			var file = await ApplicationData.Current.LocalFolder.GetFileAsync(url);
			await CopyMedia(file, newName);
		}

		/// <summary>
		/// This method copies all of the media from each of the documents. This is the only time these files are written.
		/// </summary>
		/// <param name="dcs"></param>
		private async void CopyMedia(IEnumerable<DocumentController> dcs)
		{
			foreach (var dc in dcs)
			{
				switch (dc.DocumentType.Type)
				{
					case "Image Note":
						await CopyImage(dc);
						break;
					case "Pdf Note":
						break;
					case "Video Note":
						await CopyVideo(dc);
						break;
					case "Audio Note":
						break;
					default:
						break;
				}
			}
		}

		private async Task CopyVideo(DocumentController dc)
		{
			var uriRaw = dc.GetDereferencedField(KeyStore.DataKey, null);
			if (uriRaw != null)
			{
				var olduri = uriRaw.ToString();

				// create file with unique title
				var vidTitle = "vid_" + _fileNames[dc] + ".mp4";
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
				var imgTitle = "img_" + _fileNames[dc] + ".jpg";
				await CopyMedia(olduri, imgTitle);
			}
		}

		#endregion
	}
}
