using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public static class UtilFunctions
    {
        [OperatorReturnName("Copy")]
        public static FieldControllerBase Copy(FieldControllerBase field)
        {
            return field.Copy();
        }

        [OperatorReturnName("CurrentTime")]
        public static DateTimeController Now()
        {
            return new DateTimeController(DateTime.Now);
        }

        [OperatorReturnName("Result")]
        public static TextController ToString(FieldControllerBase input = null)
        {
            return new TextController(input?.GetValue(null).ToString() ?? "<null>");
        }

        public static DocumentController MainDocument()
        {
            return MainPage.Instance.MainDocument;
        }

        public static void Undo()
        {
            UndoManager.UndoOccured();
        }

        public static void Redo()
        {
            UndoManager.RedoOccured();
        }

        public static void GlobalExport(TextController name, FieldControllerBase field)
        {
            MainPage.Instance.MainDocument.GetDataDocument().GetFieldOrCreateDefault<DocumentController>(KeyStore.GlobalDefinitionsKey)
                .SetField(KeyController.Get(name.Data), field, true);
        }

        public static IListController Sort(IListController list)
        {
            if (list is ListController<TextController> textList)
            {
                return textList.OrderBy(text => text.Data).ToListController();
            }
            if (list is ListController<NumberController> numberList)
            {
                return numberList.OrderBy(n => n.Data).ToListController();
            }
            if (list is ListController<DocumentController> docs)
            {
                return docs.OrderBy(doc => doc.Title).ToListController();
            }
            return list.AsEnumerable().OrderBy(f => f.GetValue(null)).ToListController();
        }

        [OperatorFunctionName("sort")]
        public static IListController SortDocs(ListController<DocumentController> docs, KeyController selector)
        {
            return docs.OrderBy(doc => doc.GetDereferencedField(selector).GetValue(null)).ToListController();
        }
    }
}
