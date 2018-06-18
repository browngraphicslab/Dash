using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    class UndoManager
    {
        public static Stack<UndoCommand> redoStack;
        public static Stack<UndoCommand> undoStack;

        public static void EventOccured(UndoCommand e)
        {
            undoStack.Push(e);
        }

        public static void UndoOccured()
        {
            //run undo action and remove from undo Stack
            UndoCommand command = undoStack.Pop();
            Action undo = command.undo;
            undo();

            //Add command to redo stack
            redoStack.Push(command);
        }

        public static void RedoOccured()
        {
            //run redo action and remove from redo Stack
            UndoCommand command = redoStack.Pop();
            Action redo = command.redo;
            redo();

            //Add command to undo stack
            undoStack.Push(command);
        }
    }
}
