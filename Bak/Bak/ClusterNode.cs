using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bak
{
    public class ClusterNode
    {
        int clusterNodeID;
        int gNodeID;
        int parent;

        public int ID { get { return clusterNodeID; } set { clusterNodeID = value; } }
        public int GNodeID { get { return gNodeID; } set { gNodeID = value; } }
        public int ClusterParent { get { return parent; } set { parent = value;  } }

        /// <summary>
        /// Neighboring ClusterNodes (in the same cluster); key = clusterNodeID, value = distance from this to node
        /// </summary>
        public Dictionary<int, float> Neighbors = new Dictionary<int, float>();

        public ClusterNode(int clusternodeID, int gNodeID)
        {
            this.clusterNodeID = clusternodeID;
            this.gNodeID = gNodeID;
        }

        public override string ToString()
        {
            return "CNode; gNodeID: " + gNodeID + " ; clusterNodeID: " + clusterNodeID;
        }
    }
}
