﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
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
            SaveOnServer();
        }

        public MeltOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("871A8ADC-5D15-4B31-9BE7-6256D9C961EE", "Melt");

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args, Scope scope = null)
        {
            var collection = inputs[InputCollection] as ListController<DocumentController>;;
            var variableName = inputs[VariableName] as TextController;
            var valueName = inputs[ValueName] as TextController;
            var columnVariables = inputs[ColumnVariables] as ListController<KeyController>;
            Debug.Assert(columnVariables != null);
            var columnKeys = columnVariables.TypedData;
            var allHeaderKeys = Util.GetTypedHeaders(collection);
            var dataKeys = allHeaderKeys.Keys.Except(columnKeys);

            var docType = new DocumentType(DashShared.UtilShared.GenerateNewId());
            var variableKey = new KeyController(DashShared.UtilShared.GenerateNewId(), variableName.Data);
            var valueKey = new KeyController(DashShared.UtilShared.GenerateNewId(), valueName.Data);

            var outputDocs = new List<DocumentController>();

            // iterate over all the original documents
            foreach (var originalDoc in collection.TypedData)
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
                        fields[valueKey] = new DocumentReferenceController(originalDoc.Id, dataKey);
                    }

                    foreach (var columnKey in columnKeys)
                    {
                        fields[columnKey] = new DocumentReferenceController(originalDoc.Id, columnKey);
                    }


                    outputDocs.Add(new DocumentController(fields, docType));
                }
            }

            outputs[OutputCollection] = new ListController<DocumentController>(outputDocs);
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new MeltOperatorController();
        }
    }
}