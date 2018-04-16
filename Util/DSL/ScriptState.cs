using DashShared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public class ScriptState : State<string>
    {
        public static string THIS_NAME = "this";
        private Dictionary<string, FieldControllerBase> _dictionary = new Dictionary<string, FieldControllerBase>();

        public ScriptState(IEnumerable<KeyValuePair<string, FieldControllerBase>> existingState = null) : base(existingState){}

        public ScriptState(ScriptState existingState) : base(existingState?._dictionary?.ToArray()){}


        public ScriptState(FieldControllerBase thisController) : this(new [] {new KeyValuePair<string, FieldControllerBase>(THIS_NAME, thisController)}) { }

        /// <summary>
        /// To create a state object from a document controller.  
        /// Should be inverse of 'ConvertToDocumentController'
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public static ScriptState CreateStateFromDocumentController(DocumentController doc)
        {
            return new ScriptState(doc.EnumFields().Select(kvp => new KeyValuePair<string, FieldControllerBase>(kvp.Key.Name, kvp.Value)));
        }

        /// <summary>
        /// returns this state as a documentController where every variable is a key value pair in the document
        /// Should be inverse of the static initiliazer 'CreateStateFromDocumentController'
        /// </summary>
        /// <returns></returns>
        public DocumentController ConvertToDocumentController()
        {
            return new DocumentController(_dictionary.Select(kvp => new KeyValuePair<KeyController, FieldControllerBase>(new KeyController(new KeyModel(){Name = kvp.Key}), kvp.Value)).ToDictionary(k =>k.Key, v => v.Value), DocumentType.DefaultType);
        }

        protected override State<string> CreateNew(IEnumerable<KeyValuePair<string, FieldControllerBase>> existingState = null)
        {
            return new ScriptState(existingState);
        }

        public static ScriptState ContentAware()
        {
            return new ScriptState(new Dictionary<string, FieldControllerBase>()
            {
                {"main", MainPage.Instance.MainDocument },
            });
        }
    }
}
