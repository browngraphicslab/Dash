using System;
using System.Collections.Generic;
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
				"<div class=\"heading\">" + dc.Title + "</div>",
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
		/// This method takes in a DocumentController and finds all of its links, renders each of them to an annotationWrapper CSS class, and returns it all in one string.
		/// </summary>
		/// <param name="dc"></param>
		/// <returns></returns>
		private string RenderAllLinksToHtml(DocumentController dc)
		{
			var html = new List<string>();
			var linksTo = dc.GetLinks(KeyStore.LinkToKey)?.TypedData;
			var linksFrom = dc.GetLinks(KeyStore.LinkFromKey)?.TypedData;

			if (linksTo != null)
			{
				// linksTo uses LinkDestination to get the opposite document
				foreach (var link in linksTo)
				{
					var opposite = link.GetDataDocument().GetDereferencedField<DocumentController>(KeyStore.LinkDestinationKey, null)
						.GetDataDocument();
					if (_fileNames.ContainsKey(opposite))
						html.Add(RenderLinkToHtml(opposite));
				}
			}

			if (linksFrom != null)
			{
				// linksFrom uses LinkSource to get the opposite document
				foreach (var link in linksFrom)
				{
					var opposite = link.GetDataDocument().GetDereferencedField<DocumentController>(KeyStore.LinkSourceKey, null)
						.GetDataDocument();
					if (_fileNames.ContainsKey(opposite))
						html.Add(RenderLinkToHtml(opposite));
				}
			}

			return ConcatenateList(html);
		}

		/// <summary>
		/// This method takes in a link's DocumentController and renders it in an annotationWrapper CSS class, using RenderNoteToHtml to help with it.
		/// </summary>
		/// <param name="link"></param>
		/// <returns></returns>
		private string RenderLinkToHtml(DocumentController link)
		{
			var html = new List<string> {
				"<div class=\"annotationWrapper\">",
				"<div>",
				"<div style=\"border-left:3px solid #95B75F\"/>",
				"<div class=\"annotation\">",
				RenderNoteToHtml(link),
				"</div>", // close annotation tag
				"</div>", // close top area div tag
				"<div class=\"annotationLink\"><a href=\"" + _fileNames[link] + ".html\">" + link.Title + "</a></div>",
				"</div>" //close the annotationWrapper tag
			};

			return ConcatenateList(html);
		}

		/// <summary>
		/// This method renders the document's content and returns it.
		/// </summary>
		/// <param name="dc"></param>
		/// <returns></returns>
		private string RenderNoteToHtml(DocumentController dc)
		{
			var content = "";
			switch (dc.DocumentType.Type)
			{
				case "Rich Text Note":
					content += dc.GetDereferencedField<TextController>(KeyStore.DocumentTextKey, null).Data;
					content = content.Replace("\n", "<br/>");
					break;
				case "Markdown Note":
					content += dc.GetDereferencedField<TextController>(KeyStore.DocumentTextKey, null).Data;
					content = content.Replace("\n", "<br/>");
					break;
				case "Image Note":
					content = RenderImageToHtml(dc);
					break;
				case "Pdf Note":
					break;
				case "Video Note":
					break;
				case "Audio Note":
					break;
				default:
					break;
			}

			return content;
		}

		private string RenderImageToHtml(DocumentController dc)
		{
			var imgTitle = "img_" + _fileNames[dc] + ".jpg";
			var path = "media\\" + imgTitle;
			return "<img src=\"" + path + "\">";
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

		#region UTIL

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
						break;
					case "Audio Note":
						break;
					default:
						break;
				}
			}
		}

		private async Task CopyImage(DocumentController dc)
		{
			//string version of the image uri
			var uriRaw = dc.GetDereferencedField<ImageController>(KeyStore.DataKey, null);
			if (uriRaw != null)
			{
				var olduri = uriRaw.ToString();

				//create image with unique title
				var imgTitle = "img_" + _fileNames[dc] + ".jpg";
				await CopyMedia(olduri, imgTitle);
			}
		}

		#endregion
	}
}
