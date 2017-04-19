using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bak
{
    public class ClusterNode
    {

        int abstractionLayerID;
        int cluster1ID;
        int cluster2ID;
        int node1ID;
        int node2ID;

        public int C1ID { get { return cluster1ID; } set { cluster1ID = value; } }
        public int C2ID { get { return cluster2ID; } set { cluster2ID = value; } }
        public int Node1ID { get { return node1ID; } set { node1ID = value; } }
        public int Node2ID { get { return node2ID; } set { node2ID = value; } }
        public Dictionary<int, float> Neighbors = new Dictionary<int, float>();

        public ClusterNode(int abstractionLayerID, int cluster1ID, int cluster2ID, int node1ID, int node2ID)
        {
            this.abstractionLayerID = abstractionLayerID;
            this.cluster1ID = cluster1ID;
            this.cluster2ID = cluster2ID;
            this.node1ID = node1ID;
            this.node2ID = node2ID;
        }
    }
}
