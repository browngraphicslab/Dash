using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    class UndoManager
    {
        public static Stack<Stack<UndoCommand>> redoStack = new Stack<Stack<UndoCommand>>();
        public static Stack<Stack<UndoCommand>> undoStack = new Stack<Stack<UndoCommand>>();

        public static Stack<UndoCommand> currentBatch = new Stack<UndoCommand>();

        public static void EventOccured(UndoCommand e)
        {
            currentBatch.Push(e);
        }

        public static void UndoOccured()
        {
            //run undo action and remove from undo Stack
            Stack<UndoCommand> commands = undoStack.Pop();

            foreach(UndoCommand command in commands)
            {
                Action undo = command.undo;
                undo();
            }
            
            //Add command to redo stack
            redoStack.Push(commands);
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
