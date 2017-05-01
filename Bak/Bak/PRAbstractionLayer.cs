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
        public Dictionary<int, PRAClusterNode> ClusterNodes = new Dictionary<int, PRAClusterNode>();

        public PRAbstractionLayer(int id)
        {
            ID = id;
        }

        /// <summary>
        /// IMPORTANT! this assignes AbstractionLayerID to the node as well
        /// </summary>
        /// <param name="node"></param>
        public void AddClusterNode(PRAClusterNode node)
        {
            this.ClusterNodes.Add(node.ID, node);
            node.abstractionLayerID = this.ID;
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
