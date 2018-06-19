using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    static class UndoManager
    {
        public static Stack<List<UndoCommand>> redoStack = new Stack<List<UndoCommand>>();
        public static Stack<List<UndoCommand>> undoStack = new Stack<List<UndoCommand>>();

        public static List<UndoCommand> currentBatch = new List<UndoCommand>();
        public static int batchCounter = 0;

        public static void EventOccured(UndoCommand e)
        {
            //only add events if you are in a batch
            if(batchCounter > 0)
            {
                currentBatch.Add(e);
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
                currentBatch = new List<UndoCommand>();

                MenuToolbar.Instance.SetUndoEnabled(true);

                //once event occurs, you can no longer redo
                redoStack = new Stack<List<UndoCommand>>();
                MenuToolbar.Instance.SetRedoEnabled(false);
            }
        }

        public static void UndoOccured()
        {
            //run undo action and remove from undo Stack
            List<UndoCommand> commands = undoStack.Pop();

            for(int i= commands.Count - 1; i >= 0; i--)
            {
                Action undo = commands[i].undo;
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
            List<UndoCommand> commands = redoStack.Pop();
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
