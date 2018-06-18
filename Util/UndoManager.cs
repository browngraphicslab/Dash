using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
   public class UndoManager
    {
        //need to save old value, new value
        public Any redo;
        public Any undo;

        //also save what doc and field is this value 
        public DocumentController doc;
        public String field;

        public UndoManager(Any re, Any un, DocumentController d, String f)
        {
            redo = re;
            undo = un;
            doc = d;
            field = f;
        }

        public void addToStack()
        {
            //adds this undoManager to undo stack
        }

    }
}
