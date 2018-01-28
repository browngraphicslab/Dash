using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.UI.Xaml.Data;
using Dash.Converters;
using DashShared.Models;

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
        public override void Init()
        {
            FieldKey = ContentController<FieldModel>.GetController<KeyController>(((ReferenceModel)Model).KeyId);
            var docController = GetDocumentController(null);
            docController.AddFieldUpdatedListener(FieldKey, DocFieldUpdated);
            
        }

        protected void DocFieldUpdated(FieldControllerBase sender, FieldUpdatedEventArgs args, Context c)
        {
            var dargs = (DocumentController.DocumentFieldUpdatedEventArgs)args;
            //OnFieldModelUpdated(dargs, c);
            OnFieldModelUpdated(dargs.FieldArgs, c);
        }

        public override void DisposeField()
        {
            var docController = GetDocumentController(null);
            docController.RemoveFieldUpdatedListener(FieldKey, DocFieldUpdated);
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
            var refDoc = GetDocumentController(context);
            var opField = refDoc.GetDereferencedField(KeyStore.OperatorKey, context) as OperatorController;
            if (opField != null)
            {
                var str = "=" + (opField.Model as OperatorModel).Type + "(";
                foreach (var input in opField.Inputs)
                    str += refDoc.GetField(input.Key)?.GetValue(context)?.ToString().TrimStart('=') + ",";
                str = str.TrimEnd(',') + ")";
                return str;
            }
            return "=" + new DocumentControllerToStringConverter().ConvertDataToXaml(refDoc).Trim('<', '>') + "." + FieldKey.Name;
        }
        public override bool SetValue(object value)
        {
            var refValue = (Tuple<Context,object>)value;
            var doc = GetDocumentController(refValue.Item1);
            var field = doc.GetDereferencedField<FieldControllerBase>(FieldKey, refValue.Item1);
            if (refValue.Item2 is string)
                return doc.ParseDocField(FieldKey, refValue.Item2 as string, field);
            else if (refValue.Item2 is RichTextModel.RTD)
                return doc.SetField(FieldKey, new RichTextController(refValue.Item2 as RichTextModel.RTD), false);
            else
                ;
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
            controller.SaveOnServer();
        }

        public override void UpdateOnServer(Action<FieldModel> success = null, Action<Exception> error = null)
        {
            base.UpdateOnServer(success, error);
            var controller = GetDocumentController(null);
            controller.UpdateOnServer();
        }
    }
}
