using System;
using System.Threading.Tasks;
using Windows.Storage;
using DashShared;
using Microsoft.Toolkit.Uwp.Helpers;
using System.Diagnostics;
using Windows.ApplicationModel.DataTransfer;

namespace Dash
{
	public class VideoToDashUtil : IFileParser
	{
		/// <summary>
		/// Parse a file and save it to the local filesystem
		/// </summary>
		public async Task<DocumentController> ParseFileAsync(FileData fileData, DataPackageView dataView = null)
		{
			var localFile = await CopyFileToLocal(fileData);

			var title = (fileData.File as StorageFile)?.DisplayName ?? fileData.File.Name;

			return CreateVideoBoxFromLocalFile(localFile, title);
		}

		/// <summary>
		/// Parse a file and save it to the local filesystem
		/// </summary>
		public async Task<DocumentController> ParseFileAsync(StorageFile file)
		{
			var localFile = await CopyFileToLocal(file);

			var title = file.DisplayName;

			return CreateVideoBoxFromLocalFile(localFile, title);
		}


		/// <summary>
		/// Copy a file to the local file system, returns a refernece to the file in the local filesystem
		/// </summary>
		private static async Task<StorageFile> CopyFileToLocal(FileData fileData)
		{
			var localFile = await CreateUniqueLocalFile();

			// if the uri filepath is a local file then copy it locally
			if (!fileData.File.FileType.EndsWith(".url"))
			{
				await fileData.File.CopyAndReplaceAsync(localFile);
			}
			// otherwise stream it from the internet
			else
			{
				await fileData.FileUri.GetHttpStreamToStorageFileAsync(localFile);
			}
			return localFile;
		}

		/// <summary>
		/// Copy a file to the local file system, returns a refernece to the file in the local filesystem
		/// </summary>
		private static async Task<StorageFile> CopyFileToLocal(StorageFile fileData)
		{
			var localFile = await CreateUniqueLocalFile();

			// if the uri filepath is a local file then copy it locally
			if (!fileData.FileType.EndsWith(".url"))
			{
				await fileData.CopyAndReplaceAsync(localFile);
			}
			// otherwise stream it from the internet
			else
			{
				throw new NotImplementedException();
			}
			return localFile;
		}


		/// <summary>
		/// Create a unique file in the local folder
		/// </summary>
		private static async Task<StorageFile> CreateUniqueLocalFile()
		{
			var localFolder = ApplicationData.Current.LocalFolder;
			var uniqueFilePath = UtilShared.GenerateNewId() + ".mp4"; //might have to change
			var localFile = await localFolder.CreateFileAsync(uniqueFilePath, CreationCollisionOption.ReplaceExisting);
			return localFile;
		}

		/// <summary>
		/// Create a unique file in the local folder
		/// </summary>
		private static async Task<StorageFile> CreateLocalFile(string fileName)
		{
			var localFolder = ApplicationData.Current.LocalFolder;
			var uniqueFilePath = fileName + ".mp4"; // might have to change
			var localFile = await localFolder.CreateFileAsync(uniqueFilePath, CreationCollisionOption.ReplaceExisting);
			return localFile;
		}

		/// <summary>
		/// Convert a local file which stores a video into an VideoBox. If the title is null the VideoBox doesn't have a Title
		/// </summary>
		private static DocumentController CreateVideoBoxFromLocalFile(IStorageFile localFile, string title)
		{
			Debug.WriteLine("LOCAL FILE: " + localFile);

			// return a video note
			return CreateVideoBoxFromUri(new Uri(localFile.Path));
		}

		public static DocumentController CreateVideoBoxFromUri(Uri uri)
		{
			return new VideoNote(uri).Document;
		}

	}
}
