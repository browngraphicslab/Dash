using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Dash.Models;
using Dash.StaticClasses;
using DashShared;

namespace Dash
{
    public class MeltOperatorFieldModelController : OperatorFieldModelController
    {
        //Input Keys

        /// <summary>
        /// This key contains a DocumentCollectionFieldModelController which is the input
        /// to the melt operator
        /// </summary>
        public static readonly KeyController InputCollection =
            new KeyController("DCED5F30-1C96-4BD9-8DE9-6C9DDC37C239", "Input Collection");

        /// <summary>
        /// This key contains a list of key ids which correspond to the keys which are
        /// going to be used as columns in the output
        /// </summary>
        public static readonly KeyController ColumnVariables =
            new KeyController("AA00C8E9-EEAB-44BC-97C1-BF82FEA1B428", "Column Variables");

        /// <summary>
        /// The key contains a text field model controller which is the name
        /// of variables column in the output
        /// </summary>
        public static readonly KeyController VariableName =
            new KeyController("02D82E59-54C3-4BCD-B45B-78416FE08163", "Variable Name");

        /// <summary>
        /// This key contaisn a text field model controller which is the name
        /// of the values column in the output
        /// </summary>
        public static readonly KeyController ValueName =
            new KeyController("A2AD0B50-7CC9-48B4-9D68-CA17AA3DC741", "Value Name");

        // Output Keys
        public static readonly KeyController OutputCollection =
            new KeyController("4ECAF1CB-5FEF-4B6D-8A84-C134BD90C750", "Output Collection");


        public override ObservableDictionary<KeyController, IOInfo> Inputs { get; } =
            new ObservableDictionary<KeyController, IOInfo>()
            {
                [InputCollection] = new IOInfo(TypeInfo.Collection, true),
                [ColumnVariables] = new IOInfo(TypeInfo.List, true),
                [VariableName] = new IOInfo(TypeInfo.Text, true),
                [ValueName] = new IOInfo(TypeInfo.Text, true),

            };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } =
            new ObservableDictionary<KeyController, TypeInfo>
            {
                [OutputCollection] = TypeInfo.Collection
            };


        public MeltOperatorFieldModelController() : base(new OperatorFieldModel(OperatorType.Melt))
        {
        }

        public MeltOperatorFieldModelController(OperatorFieldModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs)
        {
            var collection = inputs[InputCollection] as DocumentCollectionFieldModelController;;
            var variableName = inputs[VariableName] as TextFieldModelController;
            var valueName = inputs[ValueName] as TextFieldModelController;
            var columnVariables = inputs[ColumnVariables] as ListFieldModelController<TextFieldModelController>;
            Debug.Assert(columnVariables != null);
            var columnKeys = columnVariables.Data.Cast<TextFieldModelController>().Select(tfm => tfm.Data).Select(keyId => ContentController<KeyModel>.GetController<KeyController>(keyId)).ToList();
            var allHeaderKeys = Util.GetTypedHeaders(collection);
            var dataKeys = allHeaderKeys.Keys.Except(columnKeys);

            var docType = new DocumentType(DashShared.Util.GenerateNewId());
            var variableKey = new KeyController(DashShared.Util.GenerateNewId(), variableName.Data);
            var valueKey = new KeyController(DashShared.Util.GenerateNewId(), valueName.Data);

            var outputDocs = new List<DocumentController>();

            // iterate over all the original documents
            foreach (var originalDoc in collection.Data)
            {
                // for each data key, create a new document
                // containing references for each column variable
                // and possibly a variable referencing data if that
                // exists
                foreach (var dataKey in dataKeys)
                {
                    var fields = new Dictionary<KeyController, FieldControllerBase>();

                    var dataValue = originalDoc.GetField(dataKey);
                    if (dataValue != null)
                    {
                        fields[variableKey] = new TextFieldModelController(dataKey.Name);
                        fields[valueKey] = new DocumentReferenceFieldController(originalDoc.Id, dataKey);
                    }

                    foreach (var columnKey in columnKeys)
                    {
                        fields[columnKey] = new DocumentReferenceFieldController(originalDoc.Id, columnKey);
                    }


                    outputDocs.Add(new DocumentController(fields, docType));
                }
            }

            outputs[OutputCollection] = new DocumentCollectionFieldModelController(outputDocs);
        }

        public override bool SetValue(object value)
        {
            return false;
        }

        public override object GetValue(Context context)
        {
            throw new NotImplementedException();
        }

        public override FieldModelController<OperatorFieldModel> Copy()
        {
            return new MeltOperatorFieldModelController();
        }
    }
}