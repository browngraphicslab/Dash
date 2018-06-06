using DashShared;
using System;
using System.Diagnostics;

namespace Dash
{


    public class AudioController : FieldModelController<AudioModel>
    {

        public AudioController() : base(new AudioModel()) { }

        public AudioController(Uri path) : base(new AudioModel(path)) { }

        public AudioController(AudioModel audFieldModel) : base(audFieldModel)
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

        public override FieldControllerBase Copy()
        {
            return new VideoController(VideoFieldModel.Data);
        }
    }
}

