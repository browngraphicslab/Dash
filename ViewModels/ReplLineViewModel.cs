using System;
using System.Collections.Generic;

namespace Dash
{
    //public class ReplLineViewModel : ViewModelBase
    //{
    //    public ReplLineViewModel(string lineText, FieldControllerBase value, FieldControllerBase outputValue)
    //    {
    //        _outputValue = outputValue;
    //        LineText = lineText;
    //        LineValueText = GetValueFromResult(value);
    //        _value = value;
    //    }

    //    private static string GetValueFromResult(FieldControllerBase controller)
    //    {
    //        var result = "";
    //        try
    //        {
    //            if (controller != null)
    //            {
    //                switch (controller)
    //                {
    //                    case NumberController number:
    //                        result = number.Data.ToString("G");
    //                        break;
    //                    case TextController text:
    //                        result = text.Data;
    //                        break;
    //                    case DocumentController doc:
    //                        result = FormatFieldOutput(doc);
    //                        break;
    //                    case ReferenceController r:
    //                        result = $"REFERENCE[{r.FieldKey.Name}  :  {r.GetDocumentController(null)}]";
    //                        break;
    //                    case FunctionOperatorController _:
    //                        result = ((FunctionOperatorController) controller).getFunctionString();
    //                        break;
    //                    case BaseListController list:
    //                        var toJoin = new List<string>();
    //                        foreach (var element in list.Data) { toJoin.Add(FormatFieldOutput(element)); }
    //                        result = $"[{string.Join(",  ", toJoin)}]";
    //                        break;
    //                }
    //            }
    //            else
    //            {
    //                result = "error, result controller was null";
    //            }
    //        }
    //        catch (DSLException e)
    //        {
    //            result = "      \nException: " + e.GetHelpfulString();
    //        }
    //        catch (Exception e)
    //        {
    //            result = "Unknown annoying error occurred : " + e.StackTrace;
    //        }

    //        return result;
    //    }

    //    private static string FormatFieldOutput(FieldControllerBase docController)
    //    {
    //        string prefix;
    //        string type;

    //        switch (docController)
    //        {
    //            case DocumentController doc:
    //                prefix = $"@{doc}";
    //                type = doc.DocumentType.Type;
    //                if (type.EndsWith(" Box")) type = type.Substring(0, type.Length - 4);
    //                break;
    //            default:
    //                prefix = "!Doc";
    //                type = "";
    //                break;
    //        }
    //        return prefix + $" ({type})";
    //    }

    //    //TODO have this value be dragged out onto the workspace
    //    //this is the stored value of every line;
    //    private FieldControllerBase _value;

    //    public FieldControllerBase Value
    //    {
    //        get => _value;
    //    }

    //    private string _lineText = "";
    //    public string LineText
    //    {
    //        get => ">> "+_lineText;
    //        set => SetProperty(ref _lineText, value);
    //    }

    //    private string _lineValueText = "";
    //    public string LineValueText
    //    {
    //        get => "     " + _lineValueText;
    //        set => SetProperty(ref _lineValueText, value);
    //    }


    //    public string GetLineText()
    //    {
    //        return _lineText;

    //    }

    //    private FieldControllerBase _outputValue;
    //}
}
