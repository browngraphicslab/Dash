using System;
using System.Diagnostics;
using DashShared;

namespace Dash
{
    /// <summary>
    /// Controls data represeting an pdf in a Document.
    /// </summary>
    class PdfController : FieldModelController<PdfModel>
    {
        // == CONSTRUCTORS ==
        public PdfController() : base(new PdfModel())
        {
            SaveOnServer();
        }

        public PdfController(Uri path, string data = null) : base(new PdfModel(path, data))
        {
            SaveOnServer();
        }

        public PdfController(PdfModel pdfFieldModel) : base(pdfFieldModel)
        {

        }

        // == METHODS ==
        public override void Init()
        {
            // TODO: put init code here
        }

        /// <summary>
        ///     The <see cref="PdfFieldModel" /> associated with this <see cref="PdfController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>
        public PdfModel PdfFieldModel => Model as PdfModel;

        /// <summary>
        ///     The uri which this pdf is sourced from. This is a wrapper for <see cref="PdfModel.Data" />
        /// </summary>
        public Uri PdfSource
        {
            get => PdfFieldModel.Data;
            set
            {
                if (PdfFieldModel.Data != value)
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
            Uri data = PdfFieldModel.Data;
            UndoCommand newEvent = new UndoCommand(() => SetData(val, false), () => SetData(data, false));

            PdfFieldModel.Data = val;
            UpdateOnServer(withUndo ? newEvent : null);
            OnFieldModelUpdated(null);
        }

        public Uri Data
        {
            get => PdfSource;
            set => PdfSource = value;
        }

        // == OVERRIDEN FROM PARENT ==

        public override StringSearchModel SearchForString(string searchString)
        {
            var data = (Model as PdfModel)?.Data;
            if (searchString == null)
                return new StringSearchModel(data.AbsoluteUri);

            var reg = new System.Text.RegularExpressions.Regex(searchString);
            if (data != null && (data.AbsoluteUri.ToLower().Contains(searchString.ToLower()) || reg.IsMatch(data.AbsoluteUri)))
            {
                return new StringSearchModel(data.AbsoluteUri);
            }
            return StringSearchModel.False;
        }

        public override string ToScriptString(DocumentController thisDoc)
        {
            return "PdfController";
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new PdfController(new Uri("ms-appx:///Assets/DefaultImage.png"));
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

        public override TypeInfo TypeInfo => TypeInfo.Image;

        public override string ToString()
        {
            return PdfFieldModel.Data.AbsolutePath;
        }

        public override FieldControllerBase Copy()
        {
            return new PdfController(PdfFieldModel.Data, PdfFieldModel.ByteData);
        }


    }
}
