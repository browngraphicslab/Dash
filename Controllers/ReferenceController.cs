using System;
using DashShared;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dash.Converters;

namespace Dash
{
    public abstract class ReferenceController : FieldModelController<ReferenceModel>
    {
        protected ReferenceController(ReferenceModel model) : base(model)
        {
            // bcz: TODO check DocContextList - maybe this should come from the constructor?
            //var fmc = ContentController.DereferenceToRootFieldModel(this);//TODO Uncomment this
            //var fmc = ContentController.GetController<DocumentController>(ReferenceFieldModel.DocId).GetDereferencedField(ReferenceFieldModel.FieldKey, DocContextList);

        }

        public override async Task InitializeAsync()
        {
            FieldKey = await RESTClient.Instance.Fields.GetControllerAsync<KeyController>(ReferenceFieldModel.KeyId);
            //TODO DB
            //_lastDoc?.RemoveFieldUpdatedListener(FieldKey, DocFieldUpdated);
            //_lastDoc = GetDocumentController(null);
            //_lastDoc?.AddFieldUpdatedListener(FieldKey, DocFieldUpdated);
        }

        DocumentController _lastDoc = null;

        protected void DocFieldUpdated(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args, Context c)
        {
            //OnFieldModelUpdated(dargs, c);
            OnFieldModelUpdated(args.FieldArgs, c);
        }

        public override void DisposeField()
        {
            _lastDoc?.RemoveFieldUpdatedListener(FieldKey, DocFieldUpdated);
        }

        public KeyController FieldKey { get; protected set; }

        public abstract DocumentController GetDocumentController(Context context);

        public abstract FieldControllerBase GetDocumentReference();

        public override IEnumerable<DocumentController> GetReferences()
        {
            yield return GetDocumentController(null);
        }

        public override FieldControllerBase Dereference(Context context)
        {
            return GetFieldReference().Dereference(context);
        }

        public override FieldControllerBase DereferenceToRoot(Context context)
        {
            return GetFieldReference().DereferenceToRoot(context);
        }

        public abstract FieldReference GetFieldReference();

        public override TypeInfo TypeInfo => TypeInfo.Reference;

        public override TypeInfo RootTypeInfo => GetFieldReference().GetRootFieldType();

        /// <summary>
        ///     The <see cref="ReferenceFieldModel" /> associated with this <see cref="ReferenceController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>
        public ReferenceModel ReferenceFieldModel => Model as ReferenceModel;

        public override FieldControllerBase GetDefaultController()
        {
            throw new NotImplementedException();
        }


        public override object GetValue(Context context)
        {
            return TypescriptToOperatorParser.GetScriptForOperatorTree(this, context);

            /*
            var refDoc = GetDocumentController(context);
            var opField = refDoc.GetDereferencedField(KeyStore.OperatorKey, context) as OperatorController;
            if (opField != null)
            {
                var str = "=" + (opField.Model as OperatorModel).Type + "(";
                foreach (var input in opField.Inputs)
                    str += refDoc.GetField(input.Key)?.ToString().TrimStart('=') + ",";
                str = str.TrimEnd(',') + ")";
                return str;
            }
            return "=" + new DocumentControllerToStringConverter().ConvertDataToXaml(refDoc).Trim('<', '>') + "." + FieldKey.Name;*/
        }
        public override bool TrySetValue(object value)
        {
            var refValue = (Tuple<Context,object>)value;
            var doc = GetDocumentController(refValue.Item1);
            var copyOnWrite = (doc.GetField(FieldKey) is DocumentReferenceController dref3) ? (dref3.ReferenceFieldModel as DocumentReferenceModel).CopyOnWrite: false;
            var field = doc.GetDereferencedField<FieldControllerBase>(FieldKey, refValue.Item1);
            if (refValue.Item2 is string s)
                return doc.ParseDocField(FieldKey, s, field, copyOnWrite || field?.ReadOnly == true);
            if (refValue.Item2 is RichTextModel.RTD rtd)
            {
                doc.SetField<RichTextController>(FieldKey, rtd, true);
                return true;
            }

            return false;
        }

        /// <summary>
        /// we dont want referenced obejects to search again, for now.  
        /// This could lead to big issues with performance, not knowing how to display such a distant result, and also with inifinite loops
        /// </summary>
        /// <param name="searchString"></param>
        /// <returns></returns>
        public override StringSearchModel SearchForString(string searchString)
        {
            return StringSearchModel.False;
        }
    }
}
