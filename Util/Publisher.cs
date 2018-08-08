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
    public static class Publisher
	{
		private static StorageFolder _folder;
		private static Dictionary<string, int> _fileNames = new Dictionary<string, int>();
		// since the sidebar is the same code for every page, we might as well as save it instead of generate it for every file
		private static string _sidebarText;

		/// <summary>
		/// Use this method to start the publication process. Pass in a list of DocumentControllers to publish. Note that if any annotations are not in the list of DocumentControllers, they will not be published.
		/// </summary>
		/// <param name="dcs"></param>
		public static async Task StartPublication(List<DocumentController> dcs)
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

				// get the sidebar thing going
				_sidebarText = GetSidebarText(dcs);

				// create a new file for every DocumentController
				foreach (var dc in dcs)
				{
					var fileName = GetFileName(dc);
					var fileContents = GetFileContents(dc);
					await CreateFile(fileContents, fileName);
				}
			}
		}

		private static string GetSidebarText(List<DocumentController> dcs)
		{
			List<string> sidebar = new List<string>();
			sidebar.Add("<div id=\"sidebar\">");

			sidebar.Add("<div class=\"heading\">NOTES</div>");
			sidebar.Add("<ul>");
			foreach (var dc in dcs)
			{
				sidebar.Add("<li>" + dc.Title + "</li>");
			}
			sidebar.Add("</ul>");

			sidebar.Add("</div>");

			return ConcatenateList(sidebar);
		}

		private static async Task CreateFile(string fileContents, string fileName)
		{
			if (_folder == null) return;

			fileName += ".html";
			var file = await _folder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);

			if (file == null) return;

			await FileIO.WriteTextAsync(file, fileContents);
		}

		/// <summary>
		/// This method generates the HTML content for each DocumentController.
		/// </summary>
		/// <param name="dc"></param>
		/// <returns></returns>
		private static string GetFileContents(DocumentController dc)
		{
			var fileText = new List<string>();

			fileText.Add("<!DOCTYPE html>");
			fileText.Add("<title>" + dc.Title + "</title>");
			// add in the stylesheets, our own and FontAwesome's
			fileText.Add("<link rel=\"stylesheet\" href=\"https://cdnjs.cloudflare.com/ajax/libs/font-awesome/4.7.0/css/font-awesome.min.css\">");
			fileText.Add("<link href=\"PublishStyle.css\" rel=\"stylesheet\">");
			fileText.Add("<html><body>");
			fileText.Add(_sidebarText);

			// the actual content part starts here
			fileText.Add("<div class=\"main\">");
			fileText.Add("HELLO WORLD");
			fileText.Add("</div>");

			fileText.Add("</body></html>");

			return ConcatenateList(fileText);
		}

		/// <summary>
		/// This method gets the user to pick a folder to save the exported materials in.
		/// </summary>
		/// <returns></returns>
		private static async Task<StorageFolder> PickFolder()
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
		private static string GetFileName(DocumentController dc)
		{
			var rawTitle = dc.GetDataDocument().GetDereferencedField<TextController>(KeyStore.TitleKey, null).Data ?? "Untitled";
			if (_fileNames.ContainsKey(rawTitle))
			{
				// get the last collision number associated with this title
				var lastCollisionCount = _fileNames[rawTitle];
				//increment it 
				_fileNames[rawTitle]++;
				// append the new collision count
				rawTitle += "(" + (lastCollisionCount + 1) + ")";
			}
			else
			{
				_fileNames.Add(rawTitle, 0);
			}

			return RemoveUnsafeCharacters(rawTitle);
		}

		/// <summary>
		/// Get a safe title to save in the file.
		/// </summary>
		/// <param name="title"></param>
		/// <returns></returns>
		private static string RemoveUnsafeCharacters(string title)
		{
			return title.Replace('/', ' ').Replace('\\', ' ').Replace(':', ' ').Replace('?', ' ')
				.Replace('*', ' ').Replace('"', ' ').Replace('<', ' ').Replace('>', ' ').Replace('|', ' ')
				.Replace('#', ' ');
		}

		private static string ConcatenateList(List<string> list)
		{
			var contents = "";
			foreach (var text in list)
			{
				contents += text;
			}

			return contents;
		}
	}
}
