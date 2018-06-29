using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
   public class UndoCommand
    {
        //need to save old value, new value
        //if I want to use specific object things, I can chnage type from object to dynamic
        //public object redo;
        //public object undo;

        public Action redo;
        public Action undo;

        public UndoCommand(Action re, Action un)
        {
            redo = re;
            undo = un;
        }

    }
}
