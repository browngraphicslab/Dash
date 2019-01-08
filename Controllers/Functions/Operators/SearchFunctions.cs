using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public static class SearchFunctions
    {
        public static ListController<DocumentController> AliasOf(DocumentController doc)
        {
            var dataDoc = doc.GetDataDocument();
            var aliases = DocumentTree.MainPageTree.Where(dn => dn.DataDocument == dataDoc).Select(dn => dn.ViewDocument);
            return aliases.ToListController();
        }

        public static ListController<DocumentController> Library()
        {
            return DocumentTree.MainPageTree.DistinctBy(dn => dn.DataDocument)
                .Select(dn => dn.ViewDocument).ToListController();
        }
    }
}
