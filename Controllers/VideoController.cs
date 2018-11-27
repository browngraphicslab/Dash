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
                    Uri data = VideoFieldModel.Data;
                    UndoCommand newEvent = new UndoCommand(() => Data = value, () => Data = data);

                    VideoFieldModel.Data = value;
                    UpdateOnServer(newEvent);
                    OnFieldModelUpdated(null);
                }
            }
        }

        public override string ToScriptString(DocumentController thisDoc)
        {
            return DSL.GetFuncName<VideoOperator>() + $"(\"{Data}\")";
        }

        public override StringSearchModel SearchForString(Search.SearchMatcher matcher)
		{
			var data = (Model as VideoModel)?.Data;
		    return matcher.Matches(data.AbsoluteUri);
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

