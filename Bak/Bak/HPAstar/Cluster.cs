using Bak.HPAstar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bak
{
    public class Cluster
    {
        HashSet<int> innerNodes = new HashSet<int>();
        int id;

        public int ID { get { return id; } }
        public HashSet<int> InnerNodes { get { return innerNodes; } }
        public Dictionary<char, OuterNodeArea> OuterNodes = new Dictionary<char, OuterNodeArea>();
        //public HashSet<int> OuterNodes { get { return outerNodes; } }

        public int LastAssignedCNodeID = 0;

        /// <summary>
        /// neighboring HPAClusters
        /// </summary>
        public Dictionary<int, Cluster> Neighbors = new Dictionary<int, Cluster>();

        public Dictionary<char, Cluster> DirectioNeighbor = new Dictionary<char, Cluster>();
        /// <summary>
        /// the distances between cluster nodes, pre-computed by A*
        /// </summary>
        public Dictionary<int, float> ClusterNodesDist = new Dictionary<int, float>(); public Dictionary<int, ClusterNode> ClusterNodes = new Dictionary<int, ClusterNode>();
        
        public Cluster(int id)
        {
            this.id = id;

            OuterNodes.Add('U', new OuterNodeArea());
            OuterNodes.Add('D', new OuterNodeArea());
            OuterNodes.Add('L', new OuterNodeArea());
            OuterNodes.Add('R', new OuterNodeArea());
        }
        
        public void SetInnerNodes(HashSet<int> inNodes)
        {
            InnerNodes.Clear();
            foreach (var rn in inNodes)
            {
                InnerNodes.Add(rn);
            }
        }

        public void SetOuterNodes(Dictionary<char, OuterNodeArea> outNodes)
        {
            OuterNodes.Clear();
            foreach (var rn in outNodes)
            {
                OuterNodes[rn.Key] = rn.Value;
            }
        }

        /// <summary>
        /// Remembers the Cluster as a Neighbor. If it already is a neighbor, returns and does nothing.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        internal void AddNeighbor(int key, Cluster value)
        {
            if (!Neighbors.ContainsKey(key) && key != this.ID)
            {
                Neighbors.Add(key, value);
            }
        }

        internal void AddNeighborDirection(Cluster neighbor, char dir)
        {
            if (!this.DirectioNeighbor.ContainsKey(dir))
            {
                DirectioNeighbor.Add(dir, neighbor);
            }
        }

        private string getNeighbors()
        {
            string s = "";
            foreach (var n in Neighbors)
            {
                s += n.Value.ID + "; ";
            }
            return s;
        }

        public bool CNodeHasGNodeIdOf(ClusterNode c)
        {
            foreach (var node in ClusterNodes.Values)
            {
                if (node.GNodeID == c.GNodeID)
                {
                    return true;
                }
            }
            return false;
        }

        public override string ToString()
        {
            return "HPACluster ID: " + ID + " Neighbors: {" + getNeighbors() + "} ";
        }
        
    }
}
