using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    static class UndoManager
    {
        private static Stack<List<UndoCommand>> _redoStack = new Stack<List<UndoCommand>>();
        private static Stack<List<UndoCommand>> _undoStack = new Stack<List<UndoCommand>>();

        private static List<UndoCommand> _currentBatch = new List<UndoCommand>();
        private static int _batchCounter;

        public static void EventOccured(UndoCommand e)
        {
            //only add events if you are in a batch
            if(_batchCounter > 0)
            {
                _currentBatch.Add(e);
            }
        }

        public static void startBatch()
        {
            _batchCounter++;
        }

        public static void endBatch()
        {
            _batchCounter--;
            //only finalize Batch is all batches closed and stuff happened in batch
            if((_batchCounter == 0) && (_currentBatch.Count != 0))
            {
                //add batch to redo stack
                _undoStack.Push(_currentBatch);
                _currentBatch = new List<UndoCommand>();

                MenuToolbar.Instance.SetUndoEnabled(true);

                //once event occurs, you can no longer redo
                _redoStack = new Stack<List<UndoCommand>>();
                MenuToolbar.Instance.SetRedoEnabled(false);
            }
        }

        public static void UndoOccured()
        {
            if (_undoStack.Count > 0)
            {
                //run undo action and remove from undo Stack
                List<UndoCommand> commands = _undoStack.Pop();

                for (int i = commands.Count - 1; i >= 0; i--)
                {
                    Action undo = commands[i].undo;
                    undo();
                }

                //Add command to redo stack
                _redoStack.Push(commands);

                MenuToolbar.Instance.SetUndoEnabled(_undoStack.Count > 0);
                MenuToolbar.Instance.SetRedoEnabled(true);
            }
        }

        public static void RedoOccured()
        {
            if (_redoStack.Count > 0)
            {
                //run redo action and remove from redo Stack
                List<UndoCommand> commands = _redoStack.Pop();
                foreach (UndoCommand command in commands)
                {
                    Action redo = command.redo;
                    redo();
                }

                //Add command to undo stack
                _undoStack.Push(commands);

                MenuToolbar.Instance.SetUndoEnabled(true);
                MenuToolbar.Instance.SetRedoEnabled(_redoStack.Count > 0);
            }
        }
    }
}
