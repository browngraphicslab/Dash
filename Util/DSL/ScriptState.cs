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

        public ScriptState(IEnumerable<KeyValuePair<string, FieldControllerBase>> existingState = null, string trackingId = null) : base(existingState, trackingId) {}

        public ScriptState(ScriptState existingState, string trackingId = null) : base(existingState?._dictionary?.ToArray(), trackingId){}

        /// <summary>
        /// To create a state object from a document controller.  
        /// Should be inverse of 'ConvertToDocumentController'
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public static ScriptState CreateStateFromDocumentController(DocumentController doc, string trackingId = null)
        {
            return new ScriptState(doc.EnumFields().Select(kvp => new KeyValuePair<string, FieldControllerBase>(kvp.Key.Name, kvp.Value)), trackingId);
        }

        /// <summary>
        /// To create a state object  with an existing field called 'this', as the passed in document controller
        /// </summary>
        /// <param name="thisDocument"></param>
        /// <returns></returns>
        public static ScriptState CreateStateWithThisDocument(DocumentController thisDocument, ScriptState existingState = null, string trackingId = null)
        {
            var state = existingState ?? new ScriptState(trackingId : trackingId);
            return state.AddOrUpdateValue(THIS_NAME, thisDocument) as ScriptState;
        }

        /// <summary>
        /// returns this state as a documentController where every variable is a key value pair in the document
        /// Should be inverse of the static initiliazer 'CreateStateFromDocumentController'
        /// </summary>
        /// <returns></returns>
        public DocumentController ConvertToDocumentController()
        {
            return new DocumentController(_dictionary.Select(kvp => new KeyValuePair<KeyController, FieldControllerBase>(new KeyController(kvp.Key), kvp.Value)).ToDictionary(k =>k.Key, v => v.Value), DocumentType.DefaultType);
        }

        protected override State<string> CreateNew(IEnumerable<KeyValuePair<string, FieldControllerBase>> existingState = null, string trackingId = null)
        {
            return new ScriptState(existingState, trackingId);
        }

        /// <summary>
        /// HACK
        /// </summary>
        /// <param name="variableName"></param>
        /// <param name="value"></param>
        public void ModifyStateDirectly(string variableName, FieldControllerBase value)
        {
            _dictionary[variableName] = value;
        }

        public static ScriptState ContentAware()
        {
            return new ScriptState(new Dictionary<string, FieldControllerBase>()
            {
                {"main", MainPage.Instance?.MainDocument },
                { "help", new TextController(OperatorScript.FunctionDocumentation)}
            });
        }
    }
}
