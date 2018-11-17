using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Dash.Popups;
using static System.String;

namespace Dash
{
    public static class UIFunctions
    {
        public static async Task<ListController<DocumentController>> ManageBehaviors(DocumentController layoutDoc)
        {
            var manageBehaviors = new ManageBehaviorsPopup { DataContext = new ManageBehaviorsViewModel() };
            var updatedBehaviors = await manageBehaviors.OpenAsync(layoutDoc);
            var dataDoc = layoutDoc.GetDataDocument();

            dataDoc.SetField(KeyStore.DocumentBehaviorsKey, updatedBehaviors, true);

            dataDoc.ClearBehaviors();

            foreach (var bDoc in updatedBehaviors)
            {
                var script = bDoc.GetField<TextController>(KeyStore.ScriptTextKey).Data;
                if (IsNullOrEmpty(script)) continue;

                var trigger = bDoc.GetField<TextController>(KeyStore.TriggerKey).Data;
                var indices = bDoc.GetField<ListController<NumberController>>(KeyStore.BehaviorIndicesKey);
                var triggerInd = (int)indices[0].Data;
                var triggerModifierInd = (int)indices[1].Data;

                var signature = triggerInd == 2 ? "function(layoutDoc, fieldName, updatedValue) {" : "function(layoutDoc) {";
                script = $"{signature}\n\t{script}\n}}";
            
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

                switch (trigger.Split(" ").Last())
                {
                    case "Tapped":
                        Debug.Assert(triggerInd == 0);
                        KeyController triggerKey = null;
                        switch (triggerModifierInd)
                        {
                            case 0:
                                triggerKey = KeyStore.LeftTappedOpsKey;
                                break;
                            case 1:
                                triggerKey = KeyStore.RightTappedOpsKey;
                                break;
                            case 2:
                                triggerKey = KeyStore.DoubleTappedOpsKey;
                                break;
                            default:
                                Debug.Fail($"Trigger modifier combo box index {trigger} not supported!");
                                break;
                        }
                        dataDoc.AddToListField(triggerKey, op);
                        break;
                    case "Priority":
                        Debug.Assert(triggerInd == 1);
                        ListController<DocumentController> opGroup = null;
                        triggerKey = null;
                        switch (triggerModifierInd)
                        {
                            case 0:
                                triggerKey = KeyStore.LowPriorityOpsKey;
                                opGroup = MainPage.Instance.LowPriorityOps;
                                break;
                            case 1:
                                triggerKey = KeyStore.ModeratePriorityOpsKey;
                                opGroup = MainPage.Instance.ModeratePriorityOps;
                                break;
                            case 2:
                                triggerKey = KeyStore.HighPriorityOpsKey;
                                opGroup = MainPage.Instance.HighPriorityOps;
                                break;
                            default:
                                Debug.Fail($"Trigger modifier combo box index {trigger} not supported!");
                                break;
                        }
                        var opDoc = new DocumentController();
                        opDoc.SetField(KeyStore.ScheduledOpKey, op, true);
                        opDoc.SetField(KeyStore.ScheduledDocKey, layoutDoc, true);
                        opGroup?.Add(opDoc);
                        dataDoc.AddToListField(triggerKey, opDoc);
                        break;
                    case "Updated":
                        Debug.Assert(triggerInd == 2);

                        opDoc = new DocumentController();
                        opDoc.AddToListField(KeyStore.OperatorKey, op);

                        var watchKeyParts = bDoc.GetField<KeyController>(KeyStore.WatchFieldKey).Name.Split(".");
                        Debug.Assert(watchKeyParts.Length == 2);
                        var specifier = watchKeyParts[0];
                        var watchKey = KeyController.Get(watchKeyParts[1]);
                        Debug.Assert(specifier.Equals("v") || specifier.Equals("d"));
                        bool layout = specifier.Equals("v");

                        opDoc.SetField(op?.Inputs[0].Key, layoutDoc, true);
                        opDoc.SetField<TextController>(op?.Inputs[1].Key, watchKey.Name, true);
                        var reference = layout ? new DocumentReferenceController(layoutDoc, watchKey)
                            : (ReferenceController)new PointerReferenceController(new DocumentReferenceController(layoutDoc, KeyStore.DocumentContextKey), watchKey);
                        opDoc.SetField(op?.Inputs[2].Key, reference, true);

                        dataDoc.AddToListField(KeyStore.FieldUpdatedOpsKey, opDoc);
                        break;
                    default:
                        throw new Exception();
                }
            }

            return updatedBehaviors;
        }
    }
}
