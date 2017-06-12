using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DashShared
{
    public class WorkspaceModel : AuthorizableEntityBase
    {

        public WorkspaceModel(string name)
        {
            Name = name;
        }

        public IEnumerable<CollectionModel> Collections { get; set; }

    }
}
