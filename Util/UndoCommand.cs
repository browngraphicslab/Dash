using System;

namespace Dash
{
   public struct UndoCommand
    {
        //need to save old value, new value
        //if I want to use specific object things, I can chnage type from object to dynamic
        //public object redo;
        //public object undo;

        public readonly Action Redo;
        public readonly Action Undo;

        public UndoCommand(Action re, Action un)
        {
            Redo = re;
            Undo = un;
        }

    }
}
