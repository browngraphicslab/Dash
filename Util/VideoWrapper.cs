using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Dash
{
	public enum VideoType
	{
		Uri,
		StorageFile
	}
	
	/// <summary>
	/// This is a wrapper class for returning a video URI or StorageFile when importing, since parsing it is different depending on where it came from. The VideoType enum says which property is set for this instance. 
	/// </summary>
	public class VideoWrapper
	{
		public Uri Uri { get; set; }
		public StorageFile File { get; set; }
		public VideoType Type { get; set; }
	}
}
