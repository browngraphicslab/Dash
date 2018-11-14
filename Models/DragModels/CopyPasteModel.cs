using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Dash
{
    public class CopyPasteModel
    {
        private List<DocumentController> _documents;
        private bool _copy;
        public CopyPasteModel(List<DocumentController> documents, bool copy)
        {
            _documents = documents;
            _copy = copy;
        }

        public List<DocumentController> GetDocuments(Point? pos)
        {
            var results = new List<DocumentController>();

            var realPos = pos ?? new Point();
            if (_copy)
            {
                foreach (var documentController in _documents)
                {
                    results.Add(documentController.GetViewCopy(realPos));
                    realPos.X += documentController.GetActualSize()?.X ?? 50 + 10;
                }
            }
            else
            {
                foreach (var documentController in _documents)
                {
                    documentController.SetPosition(realPos);
                    results.Add(documentController);
                    realPos.X += documentController.GetWidth() + 10;
                }
            }

            _copy = true; // Once we've pasted the original, make copies from now on

            return results;
        }
    }
}
