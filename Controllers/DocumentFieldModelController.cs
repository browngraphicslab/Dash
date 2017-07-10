using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Dash
{
    public class DocumentFieldModelController : FieldModelController
    {
        public DocumentFieldModelController(DocumentController document) : base(new DocumentModelFieldModel(document.DocumentModel))
        {
            Data = document;
        }

        /// <summary>
        ///     The <see cref="DocumentModelFieldModel" /> associated with this <see cref="DocumentFieldModelController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>
        public DocumentModelFieldModel DocumentModelFieldModel => FieldModel as DocumentModelFieldModel;


        private DocumentController _data;
        /// <summary>
        ///     A wrapper for <see cref="DocumentModelFieldModel.Data" />. Change this to propagate changes
        ///     to the server
        /// </summary>
        public DocumentController Data
        {
            get { return _data; }
            set
            {
                if (SetProperty(ref _data, value))
                {
                    // update local
                    // update server
                }
            }
        }
        public override TypeInfo TypeInfo => TypeInfo.Document;

        public override FrameworkElement GetTableCellView()
        {
            return GetTableCellViewOfScrollableText(BindTextOrSetOnce);
        }

        private void BindTextOrSetOnce(TextBlock textBlock)
        {
            textBlock.Text = $"Document of type: {DocumentModelFieldModel.Data.DocumentType}";
        }
    }
}