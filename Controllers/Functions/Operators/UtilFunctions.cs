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

        public static void Undo()
        {
            UndoManager.UndoOccured();
        }

        public static void Redo()
        {
            UndoManager.RedoOccured();
        }
    }
}
