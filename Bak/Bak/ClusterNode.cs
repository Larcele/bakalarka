using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bak
{
    public class ClusterNode
    {
        int clusterID;
        int gNodeID;

        public int ID { get { return clusterID; } set { clusterID = value; } }
        public int GNodeID { get { return gNodeID; } set { gNodeID = value; } }

        public Dictionary<int, float> Neighbors = new Dictionary<int, float>();

        public ClusterNode(int clusterID, int gNodeID)
        {
            this.clusterID = clusterID;
            this.gNodeID = gNodeID;
        }

        public override string ToString()
        {
            return "CNode; gNodeID: " + gNodeID + " ; clusterID: " + clusterID;
        }
    }
}
