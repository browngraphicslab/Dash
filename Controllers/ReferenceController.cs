using System;
using DashShared;
using System.Collections.Generic;
using Dash.Converters;

namespace Dash
{
    public abstract class ReferenceController : FieldModelController<ReferenceModel>
    {
        public ReferenceController(ReferenceModel model) : base(model)
        {
            // bcz: TODO check DocContextList - maybe this should come from the constructor?
            //var fmc = ContentController.DereferenceToRootFieldModel(this);//TODO Uncomment this
            //var fmc = ContentController.GetController<DocumentController>(ReferenceFieldModel.DocId).GetDereferencedField(ReferenceFieldModel.FieldKey, DocContextList);

        }

        DocumentController _lastDoc = null;
        public override void Init()
        {
            FieldKey = ContentController<FieldModel>.GetController<KeyController>(((ReferenceModel)Model).KeyId);
            _lastDoc?.RemoveFieldUpdatedListener(FieldKey, DocFieldUpdated);
            _lastDoc = GetDocumentController(null);
            _lastDoc?.AddFieldUpdatedListener(FieldKey, DocFieldUpdated);
        }

        protected void DocFieldUpdated(FieldControllerBase sender, FieldUpdatedEventArgs args, Context c)
        {
            var dargs = (DocumentController.DocumentFieldUpdatedEventArgs)args;
            //OnFieldModelUpdated(dargs, c);
            OnFieldModelUpdated(dargs.FieldArgs, c);
        }

        public override void DisposeField()
        {
            _lastDoc?.RemoveFieldUpdatedListener(FieldKey, DocFieldUpdated);
        }

        public KeyController FieldKey { get; set; }

        public abstract DocumentController GetDocumentController(Context context);

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

        public abstract string GetDocumentId(Context context);

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
            return "=="+OperatorScriptParser.GetScriptForOperatorTree(this, context);

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
                return doc.ParseDocField(FieldKey, s, field, copyOnWrite);
            if (refValue.Item2 is RichTextModel.RTD rtd)
            {
                doc.SetField<RichTextController>(FieldKey, rtd, true);
                return true;
            }

            return false;
        }

        public override string GetTypeAsString()
        {
            return "Ref";
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

        public override void SaveOnServer(Action<FieldModel> success = null, Action<Exception> error = null)
        {
            base.SaveOnServer(success, error);
            var controller = GetDocumentController(null);
            controller?.SaveOnServer();
        }

        public override void UpdateOnServer(Action<FieldModel> success = null, Action<Exception> error = null)
        {
            base.UpdateOnServer(success, error);
            var controller = GetDocumentController(null);
            controller?.UpdateOnServer();
        }
    }
}
