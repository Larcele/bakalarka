using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bak
{
    public class PRAbstractionLayer
    {
        public int ID;
        public int LastAssignedClusterID = 0;
        public Dictionary<int, PRAClusterNode> ClusterNodes = new Dictionary<int, PRAClusterNode>();

        public PRAbstractionLayer(int id)
        {
            ID = id;
        }
        
        public void AddClusterNode(PRAClusterNode node)
        {
            this.ClusterNodes.Add(node.ID, node);
        }

        public bool AllCLustersDisconnected()
        {
            foreach (var n in ClusterNodes.Values)
            {
                if (n.neighbors.Count != 0)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
