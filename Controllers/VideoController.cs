using Dash.Models;
using DashShared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{


	public class VideoController : FieldModelController<VideoModel>
	{

		public VideoController() : base(new VideoModel()) { }

		public VideoController(Uri path) : base(new VideoModel(path)) { }

		public VideoController(VideoModel vidFieldModel) : base(vidFieldModel)
		{

		}

		public override void Init()
		{

		}


		public VideoModel VideoFieldModel => Model as VideoModel;


		public Uri MediaSource
		{
			get => VideoFieldModel.Data;
			set
			{
				if (VideoFieldModel.Data != value)
				{
					VideoFieldModel.Data = value;
					OnFieldModelUpdated(null);
				}
			}
		}

		public override StringSearchModel SearchForString(string searchString)
		{
			var data = (Model as VideoModel)?.Data;
			if (data != null && (data.AbsoluteUri.ToLower().Contains(searchString)))
			{
				return new StringSearchModel(data.AbsoluteUri);
			}
			return StringSearchModel.False;
		}

		public override FieldControllerBase GetDefaultController()
		{
			return new VideoController(new Uri("ms-appx:///Assets/DefaultVideo.mp4"));
		}

		public override object GetValue(Context context)
		{
			return Data;
		}

		public override bool TrySetValue(object value)
		{
			Debug.Assert(value is Uri);
			if (value is Uri u)
			{
				Data = u;
				return true;
			}
			return false;
		}

		public Uri Data
		{
			get => MediaSource;
			set => MediaSource = value;
		}

		public override TypeInfo TypeInfo => TypeInfo.Video;

		public override string ToString()
		{
			return VideoFieldModel.Data.AbsolutePath;
		}

		public override FieldModelController<VideoModel> Copy()
		{
			return new VideoController(VideoFieldModel.Data);
		}
	}
}

