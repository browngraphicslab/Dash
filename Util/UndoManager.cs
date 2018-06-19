using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    static class UndoManager
    {
        public static Stack<Stack<UndoCommand>> redoStack = new Stack<Stack<UndoCommand>>();
        public static Stack<Stack<UndoCommand>> undoStack = new Stack<Stack<UndoCommand>>();

        public static Stack<UndoCommand> currentBatch = new Stack<UndoCommand>();
        public static int batchCounter = 0;

        public static void EventOccured(UndoCommand e)
        {
            //only add events if you are in a batch
            if(batchCounter > 0)
            {
                currentBatch.Push(e);
            }
        }

        public static void startBatch()
        {
            batchCounter++;
        }

        public static void endBatch()
        {
            batchCounter--;
            //only finalize Batch is all batches closed and stuff happened in batch
            if((batchCounter == 0) && (currentBatch.Count != 0))
            {
                //add batch to redo stack
                undoStack.Push(currentBatch);
                currentBatch = new Stack<UndoCommand>();

                MenuToolbar.Instance.SetUndoEnabled(true);

                //once event occurs, you can no longer redo
                redoStack = new Stack<Stack<UndoCommand>>();
                MenuToolbar.Instance.SetRedoEnabled(false);
            }
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

            MenuToolbar.Instance.SetUndoEnabled(undoStack.Count > 0);
            MenuToolbar.Instance.SetRedoEnabled(true);
        }

        public static void RedoOccured()
        {
            //run redo action and remove from redo Stack
            Stack<UndoCommand> commands = redoStack.Pop();
            foreach(UndoCommand command in commands)
            {
                Action redo = command.redo;
                redo();
            }

            //Add command to undo stack
            undoStack.Push(commands);

            MenuToolbar.Instance.SetUndoEnabled(true);
            MenuToolbar.Instance.SetRedoEnabled(redoStack.Count > 0);
        }
    }
}
