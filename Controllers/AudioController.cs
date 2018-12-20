using DashShared;
using System;
using System.Diagnostics;

namespace Dash
{


    public class AudioController : FieldModelController<AudioModel>
    {

        public AudioController() : base(new AudioModel())
        {
        }

        public AudioController(Uri path) : base(new AudioModel(path))
        {
        }

        public AudioController(AudioModel audFieldModel) : base(audFieldModel)
        {
        }

        public AudioModel AudioFieldModel => Model as AudioModel;


        public Uri MediaSource
        {
            get => AudioFieldModel.Data;
            set
            {
                if (AudioFieldModel.Data != value)
                {
                    System.Uri data = AudioFieldModel.Data;
                    UndoCommand newEvent = new UndoCommand(() => Data = value, () => Data = data);

                    AudioFieldModel.Data = value;
                    UpdateOnServer(newEvent);
                    OnFieldModelUpdated(null);
                }
            }
        }

        public override StringSearchModel SearchForString(Search.SearchMatcher matcher)
        {
            return matcher.Matches(Data.AbsoluteUri);
        }

        public override string ToScriptString(DocumentController thisDoc)
        {
            return DSL.GetFuncName<AudioOperator>() + $"(\"{Data}\")";
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new AudioController(new Uri("ms-appx:///Assets/DefaultAudio.mp3"));
        }

        public override object GetValue()
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

        public override TypeInfo TypeInfo => TypeInfo.Audio;

        public override string ToString()
        {
            return AudioFieldModel.Data.AbsolutePath;
        }

        public override FieldControllerBase Copy()
        {
            return new AudioController(AudioFieldModel.Data);
        }
    }
}

