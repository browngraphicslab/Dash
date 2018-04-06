using DashShared;
using DashShared.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;


namespace Dash
{

		/// <summary>
		/// A Field Model which holds video data
		/// </summary>
		[FieldModelTypeAttribute(TypeInfo.Video)]
		public class VideoModel : FieldModel
		{

		private Uri _uriCache;

			public Uri Data
			{
				get => _uriCache;
				set
				{
					if (value == null)
					{
						return;
					}

					// if the value is a file and the file exists in the local folder then set localFile to the filename
					if (value.IsFile && File.Exists(ApplicationData.Current.LocalFolder.Path + "\\" + value.Segments.Last()))
					{
						localFile = value.Segments.Last();
					}
					else
					{
						// otherwise assume the file is a globalUri like http so set it there
						globalUri = value;
					}

					_uriCache = localFile == null ? globalUri : new Uri(ApplicationData.Current.LocalFolder.Path + "\\" + localFile);
				}
			}

			private string localFile;

			private Uri globalUri;

			public VideoModel() : base(null)
			{

			}


			/// <summary>
			/// Create a new Video Field Model which represents the video pointed to by the <paramref name="data"/>
			/// </summary>
			/// <param name="data">The uri that the video this field model encapsulates is sourced from</param>
			public VideoModel(Uri path, string id = null) : base(id)
			{
				Data = path;
			}
		}
	}

