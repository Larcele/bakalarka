using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bak
{
    class NodeInfo
    {
        public int Parent { get; set; }
        public float PathCost { get; set; }

        public NodeInfo(int parent, int pathcost)
        {
            Parent = parent;
            PathCost = pathcost;
        }

        public override string ToString()
        {
            return Parent + " ; " + PathCost;
        }

    }
}
