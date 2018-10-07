using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DashShared;

namespace Dash.Controllers.Operators
{
    [OperatorType(Op.Name.get_doc)]
    public class DocumentPathOperator : OperatorController
    {
        public static readonly KeyController PathKey = new KeyController("Path");


        public static readonly KeyController DocumentKey = new KeyController("Document");


        public DocumentPathOperator() : base(new OperatorModel(TypeKey.KeyModel))
        {
            SaveOnServer();
        }

        public DocumentPathOperator(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("Document from Path", "abdb37c9-324c-41a7-92f6-f093d38b3afe");

        public override FieldControllerBase GetDefaultController()
        {
            return new DocumentPathOperator();
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(PathKey, new IOInfo(TypeInfo.Text, true)),

        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [DocumentKey] = TypeInfo.Document,

        };

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var path = (TextController)inputs[PathKey];
            DocumentController document = Execute(path);
            outputs[DocumentKey] = document;

            return Task.CompletedTask;
        }

        public DocumentController Execute(TextController path)
        {
            //Don't forget about nonexistant paths
            string text = path.Data;
            if (!text.StartsWith('/'))
            {
                throw new ScriptExecutionException(new TextErrorModel("Invalid path"));
            }

            var head = DocumentTree.MainPageTree.Head;
            var doc = SearchTree(text + "/", head);
            return doc;
        }

        //Recursively searches the documentTree for matches in the path name, extra code is for handling edge cases such as collection names with slashes and empty collection names;
        private DocumentController SearchTree(string path, DocumentNode node)
        {
            // Start at index 1 to skip the slash at the beginning
            if (path.Length <= 1)
                return node?.DataDocument;
            foreach (var child in node.Children)
            {
                string docName = "";
                string newPath = path;
                while (newPath.IndexOf('/', 1) != -1)
                {
                    int firstSlash = newPath.IndexOf('/', 1);

                    docName += newPath.Substring(0, firstSlash);
                    newPath = newPath.Substring(firstSlash);

                    if (child.DataDocument.Title == docName.Substring(1))
                    {
                        var doc = SearchTree(newPath, child);
                        if (doc != null)
                        {
                            return doc;
                        }
                    }
                    
                }
                
            }

            return null;
        }

    }
}
