using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class MeltOperatorController : OperatorController
    {
        //Input Keys

        /// <summary>
        /// This key contains a ListController<DocumentController> which is the input
        /// to the melt operator
        /// </summary>
        public static readonly KeyController InputCollection =
            KeyController.Get("Input Collection");

        /// <summary>
        /// This key contains a list of key ids which correspond to the keys which are
        /// going to be used as columns in the output
        /// </summary>
        public static readonly KeyController ColumnVariables =
            KeyController.Get("Column Variables");

        /// <summary>
        /// The key contains a text field model controller which is the name
        /// of variables column in the output
        /// </summary>
        public static readonly KeyController VariableName =
            KeyController.Get("Variable Name");

        /// <summary>
        /// This key contaisn a text field model controller which is the name
        /// of the values column in the output
        /// </summary>
        public static readonly KeyController ValueName =
            KeyController.Get("Value Name");

        // Output Keys
        public static readonly KeyController OutputCollection =
            KeyController.Get("Output Collection");


        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } =
            new ObservableCollection<KeyValuePair<KeyController, IOInfo>>()
            {
                new KeyValuePair<KeyController, IOInfo>(InputCollection, new IOInfo(TypeInfo.List, true)),
                new KeyValuePair<KeyController, IOInfo>(ColumnVariables, new IOInfo(TypeInfo.List, true)),
                new KeyValuePair<KeyController, IOInfo>(VariableName, new IOInfo(TypeInfo.Text, true)),
                new KeyValuePair<KeyController, IOInfo>(ValueName, new IOInfo(TypeInfo.Text, true)),

            };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } =
            new ObservableDictionary<KeyController, TypeInfo>
            {
                [OutputCollection] = TypeInfo.List
            };


        public MeltOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
        }

        public MeltOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("Melt");

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var collection = (ListController<DocumentController>) inputs[InputCollection];;
            var variableName = (TextController) inputs[VariableName];
            var valueName = (TextController) inputs[ValueName];
            var columnVariables = (ListController<KeyController>) inputs[ColumnVariables];
            Debug.Assert(columnVariables != null);
            var columnKeys = columnVariables;
            var allHeaderKeys = Util.GetDisplayableTypedHeaders(collection);
            var dataKeys = allHeaderKeys.Keys.Except(columnKeys).ToList();

            var docType = new DocumentType(DashShared.UtilShared.GenerateNewId());
            var variableKey = KeyController.Get(variableName.Data);
            var valueKey = KeyController.Get(valueName.Data);

            var outputDocs = new List<DocumentController>();

            // iterate over all the original documents
            foreach (var originalDoc in collection)
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
                        fields[variableKey] = new TextController(dataKey.Name);
                        fields[valueKey] = new DocumentReferenceController(originalDoc, dataKey);
                    }

                    foreach (var columnKey in columnKeys)
                    {
                        fields[columnKey] = new DocumentReferenceController(originalDoc, columnKey);
                    }


                    outputDocs.Add(new DocumentController(fields, docType));
                }
            }

            outputs[OutputCollection] = new ListController<DocumentController>(outputDocs);
            return Task.CompletedTask;
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new MeltOperatorController();
        }
    }
}
