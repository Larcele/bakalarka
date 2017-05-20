using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bak
{
    public class Cluster
    {
        List<int> innerNodes = new List<int>();
        List<int> outerNodes = new List<int>();
        int id;

        public int ID { get { return id; } }
        public List<int> InnerNodes { get { return innerNodes; } }
        public List<int> OuterNodes { get { return outerNodes; } }
        public Dictionary<int, Cluster> neighbors = new Dictionary<int, Cluster>(); //neighboring HPAClusters
        public Dictionary<int, float> outerNodesDist = new Dictionary<int, float>(); //the distances between outer nodes, pre-computed by A*
        public Dictionary<int, ClusterNode> ClusterNodes = new Dictionary<int, ClusterNode>();
        
        public Cluster(int id)
        {
            this.id = id;
        }

        public Cluster(int id, List<int> innerNodes, List<int> outerNodes)
        {
            this.id = id;
            this.innerNodes = innerNodes;
            this.outerNodes = outerNodes;
        }

        public void SetInnerNodes(List<int> inNodes)
        {
            InnerNodes.Clear();
            InnerNodes.AddRange(inNodes);
        }

        public void SetOuterNodes(List<int> outNodes)
        {
            OuterNodes.Clear();
            OuterNodes.AddRange(outNodes);
        }

        /// <summary>
        /// Remembers the Cluster as a Neighbor. If it already is a neighbor, returns and does nothing.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        internal void AddNeighbor(int key, Cluster value)
        {
            if (!neighbors.ContainsKey(key) && key != this.ID)
            {
                neighbors.Add(key, value);
            }
        }

        private string getNeighbors()
        {
            string s = "";
            foreach (var n in neighbors)
            {
                s += n.Value.ID + "; ";
            }
            return s;
        }

        public override string ToString()
        {
            return "HPACluster ID: " + ID + " Neighbors: {" + getNeighbors() + "} ";
        }
    }
}
