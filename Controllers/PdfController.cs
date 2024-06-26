﻿using System;
using System.Diagnostics;
using DashShared;

namespace Dash
{
    /// <summary>
    /// Controls data represeting an pdf in a Document.
    /// </summary>
    public class PdfController : FieldModelController<PdfModel>
    {
        // == CONSTRUCTORS ==
        public PdfController() : base(new PdfModel())
        {
        }

        public PdfController(Uri path, string data = null) : base(new PdfModel(path, data))
        {
        }

        public PdfController(PdfModel pdfFieldModel) : base(pdfFieldModel)
        {

        }

        // == METHODS ==

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
                    Uri data = PdfFieldModel.Data;
                    UndoCommand newEvent = new UndoCommand(() => Data = value, () => Data = data);

                    PdfFieldModel.Data = value;
                    UpdateOnServer(newEvent);
                    OnFieldModelUpdated(null);
                }
            }
        }

        public Uri Data
        {
            get => PdfSource;
            set => PdfSource = value;
        }

        // == OVERRIDEN FROM PARENT ==

        public override StringSearchModel SearchForString(Search.SearchMatcher matcher)
        {
            var data = (Model as PdfModel)?.Data;

            return matcher.Matches(data.AbsoluteUri);
        }

        public override string ToScriptString(DocumentController thisDoc)
        {
            return DSL.GetFuncName<PdfOperator>() + $"(\"{Data}\")";
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new PdfController(new Uri("ms-appx:///Assets/DefaultImage.png"));
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
