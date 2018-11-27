using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using DashShared;

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
        public static readonly Guid GlobalScopeID = new Guid("90C30F6F-0913-42E0-A1F8-778A06766A19");

        public static async Task InitGlobalScope()
        {
            var id = GlobalScopeID.ToString();
            var doc = await RESTClient.Instance.Fields.GetControllerAsync<DocumentController>(id);
            _globalDocumentScope = new DocumentScope(doc ??
                new DocumentController(new Dictionary<KeyController, FieldControllerBase>(), DocumentType.DefaultType, id));
        }

        private static DocumentScope _globalDocumentScope = null;
        public static DocumentScope GetGlobalScope()
        {
            Debug.Assert(_globalDocumentScope != null, "You need to call InitGlobalScope before trying to get the Global Scope");

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
