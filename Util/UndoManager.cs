using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    static class UndoManager
    {
        private class BatchHandle : IDisposable
        {
            public BatchHandle()
            {
                StartBatch();
            }

            public void Dispose()
            {
                EndBatch();
            }
        }

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

        /// <summary>
        /// If your batch is going to start and end in the same method, instead of calling StartBatch and EndBatch, call this method in a using like:
        /// using(UndoManager.GetBatchHandle()) {
        ///     (Your code that should be in a batch)
        /// }
        /// This will automatically start and end the batch for you
        /// </summary>
        /// <returns></returns>
        public static IDisposable GetBatchHandle()
        {
            return new BatchHandle();
        }

        /// <summary>
        /// Start an undo batch. If the batch you are starting will only last for one method, use GetBatchHandle instead
        /// </summary>
        public static void StartBatch()
        {
            _batchCounter++;
            //System.Diagnostics.Debug.WriteLine("START BATCH : " + _batchCounter);
        }

        /// <summary>
        /// Ends the current batch. This should only be called if you have previously called StartBatch, and there should be one EndBatch for every StartBatch.
        /// If the batch you are using will only last for one method, use GetBatchHandle instead.
        /// </summary>
        public static void EndBatch()
        {
            _batchCounter--;
            //System.Diagnostics.Debug.WriteLine("END BATCH : " + _batchCounter);
            //only finalize Batch is all batches closed and stuff happened in batch
            if ((_batchCounter == 0) && (_currentBatch.Count != 0))
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
