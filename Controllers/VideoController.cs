using DashShared;
using System;
using System.Diagnostics;

namespace Dash
{


	public class VideoController : FieldModelController<VideoModel>
	{

	    public VideoController() : base(new VideoModel())
	    {
	        }

	    public VideoController(Uri path) : base(new VideoModel(path))
	    {
	        }

        public VideoController(VideoModel vidFieldModel) : base(vidFieldModel)
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
                    SetData(value);
                }
            }
        }

        /*
       * Sets the data property and gives UpdateOnServer an UndoCommand 
       */
        private void SetData(Uri val, bool withUndo = true)
        {
            Uri data = VideoFieldModel.Data;
            UndoCommand newEvent = new UndoCommand(() => SetData(val, false), () => SetData(data, false));

            VideoFieldModel.Data = val;
            UpdateOnServer(withUndo ? newEvent : null);
            OnFieldModelUpdated(null);
        }

        public override StringSearchModel SearchForString(string searchString)
		{
			var data = (Model as VideoModel)?.Data;
            var reg = new System.Text.RegularExpressions.Regex(searchString);
            if (data != null && (data.AbsoluteUri.ToLower().Contains(searchString.ToLower()) || reg.IsMatch(data.AbsoluteUri)))
			{
				return new StringSearchModel(data.AbsoluteUri);
			}
			return StringSearchModel.False;
		}

	    public override string ToScriptString(DocumentController thisDoc)
	    {
	        return "VideoController";
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

		public override FieldControllerBase Copy()
		{
			return new VideoController(VideoFieldModel.Data);
		}
	}
}

