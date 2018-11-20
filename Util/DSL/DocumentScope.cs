﻿using System.Collections.Generic;

namespace Dash
{
    public class DocumentScope : Scope
    {
        private readonly DocumentController _variableDoc;

        public DocumentController VariableDoc()
        {
            return _variableDoc;
        }

        public DocumentScope(DocumentController doc, Scope parent) : base(parent)
        {
            _variableDoc = doc;
        }

        private DocumentScope(DocumentController doc) : base(null, false)
        {
            _variableDoc = doc;
        }

        public DocumentScope() : base(null)
        {
            _variableDoc = new DocumentController();
        }

        private static DocumentScope _globalDocumentScope = null;
        public static DocumentScope GetGlobalScope()
        {
            if (_globalDocumentScope == null)
            {
                _globalDocumentScope = new DocumentScope(MainPage.Instance.MainDocument.GetDataDocument()
                    .GetFieldOrCreateDefault<DocumentController>(KeyStore.GlobalDefinitionsKey));
            }

            return _globalDocumentScope;
        }

        public override IEnumerator<KeyValuePair<string, FieldControllerBase>> GetEnumerator()
        {
            foreach (var enumDisplayableField in _variableDoc.EnumDisplayableFields())
            {
                yield return new KeyValuePair<string, FieldControllerBase>(enumDisplayableField.Key.Name, enumDisplayableField.Value);
            }

            if (Parent != null)
            {
                foreach (var kvp in Parent)
                {
                    yield return kvp;
                }
            }
        }

        protected override void SetLocalVariable(string name, FieldControllerBase field)
        {
            _variableDoc.SetField(KeyController.Get(name), field, true);
        }

        protected override FieldControllerBase GetLocalVariable(string name)
        {
            return _variableDoc.GetField(KeyController.Get(name));
        }

        protected override bool HasLocalVariable(string name)
        {
            return GetLocalVariable(name) != null;
        }

        protected override void DeleteLocalVariable(string variableName)
        {
            _variableDoc.RemoveField(KeyController.Get(variableName));
        }
    }
}
