﻿namespace Dash
{
    public class EditableScriptViewModel : ViewModelBase
    {
        public FieldReference Reference { get; }

        public Context Context { get; }

        public DocumentController Document => Reference.GetDocumentController(null);
        public KeyController Key => Reference.FieldKey;
        public FieldControllerBase Value => Reference.Dereference(Context);


        public EditableScriptViewModel(FieldReference reference)
        {
            Reference = reference;
        }

    }
}
