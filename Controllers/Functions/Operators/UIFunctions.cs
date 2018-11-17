using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dash.Popups;
using static System.String;

namespace Dash
{
    public static class UIFunctions
    {
        public static async Task<ListController<DocumentController>> ManageBehaviors(DocumentController docRef)
        {
            var manageBehaviors = new ManageBehaviorsPopup { DataContext = new ManageBehaviorsViewModel() };
            var updatedBehaviors = await manageBehaviors.OpenAsync(docRef);

            docRef.SetField(KeyStore.DocumentBehaviorsKey, updatedBehaviors, true);

            docRef.ClearBehaviors();

            foreach (var bDoc in updatedBehaviors)
            {
                var script = bDoc.GetField<TextController>(KeyStore.ScriptTextKey).Data;
                if (IsNullOrEmpty(script)) continue;

                var trigger = bDoc.GetField<TextController>(KeyStore.TriggerKey).Data;
                var indices = bDoc.GetField<ListController<NumberController>>(KeyStore.BehaviorIndicesKey);
                var triggerInd = (int)indices[0].Data;

                string signature = triggerInd == 2 ? "function(doc, field) " : "function(doc) ";
                script = $"{signature}{{\n\t{script}\n}}";
            
                OperatorController op;
                try
                {
                    op = TypescriptToOperatorParser.Interpret(script).Result as OperatorController;
                }
                catch (Exception e)
                {
                    var warning = $"Warning - execution threw an exception!\n\n{e.Message}\n\n";
                    bDoc.SetField<TextController>(KeyStore.ScriptTextKey, warning + script, true);
                    Console.WriteLine(e);
                    throw;
                }

                switch (trigger)
                {
                    case "Tapped":
                        Debug.Assert(triggerInd == 0);
                        KeyController triggerKey = null;
                        switch ((int)indices[1].Data)
                        {
                            case 0:
                                triggerKey = KeyStore.LeftTappedOpsKey;
                                break;
                            case 1:
                                triggerKey = KeyStore.DoubleTappedOpsKey;
                                    break;
                            case 2:
                                triggerKey = KeyStore.RightTappedOpsKey;
                                break;
                            default:
                                Debug.Fail($"Trigger modifier combo box index {trigger} not supported!");
                                break;
                        }
                        docRef.AddBehavior(triggerKey, op);
                    break;
                    case "Deleted":
                        break;
                    case "Field Updated":
                        Debug.Assert(triggerInd == 2);

                        var opDoc = new DocumentController();
                        opDoc.AddToListField(KeyStore.OperatorKey, op);
                        var watchKey = bDoc.GetField<KeyController>(KeyStore.WatchFieldKey);
                        Debug.Assert(watchKey != null);
                        opDoc.SetField(op?.Inputs[0].Key, docRef, true);
                        opDoc.SetField(op?.Inputs[1].Key, new PointerReferenceController(new DocumentReferenceController(docRef, KeyStore.DocumentContextKey), watchKey), true);
                        docRef.AddToListField(KeyStore.FieldUpdatedOpsKey, opDoc);
                        break;
                    default:
                        throw new Exception();
                }
            }

            return updatedBehaviors;
        }
    }
}
