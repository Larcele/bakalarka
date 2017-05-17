using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bak
{
    public class PRAClusterNode
    {
        public int ID;
        public int abstractionLayerID;

        /// <summary>
        /// the cluster parent containing this cluster in its children clusters, if a higher abstraction layer exists. 
        ///  -1 is the default value if a higher level of abstraction doesn't exist.
        /// </summary>
        public int PRAClusterParent = -1;

        public int X {  get; private set; }
        public int Y { get; private set; }

        public List<int> innerNodes = new List<int>();
        public Dictionary<int, PRAClusterNode> neighbors = new Dictionary<int, PRAClusterNode>(); //neighboring PRAClusterNodes
        public Dictionary<int, float> neighborDist = new Dictionary<int, float>(); //the estimated heuristic distances between neighboring clusters

        public PRAClusterNode(int absID, int id, List<int> nodes)
        {
            abstractionLayerID = absID;
            ID = id;
            innerNodes = nodes;
        }

        public void setParentToAllInnerNodes(PRAbstractionLayer lowerlevel)
        {
            if (abstractionLayerID != 0)
            {
                foreach (var nodeID in innerNodes)
                {
                    lowerlevel.ClusterNodes[nodeID].PRAClusterParent = this.ID;
                }
            }
        }

        public override string ToString()
        {
            return "PRA* Cluster; AbsID:" + abstractionLayerID + "; { " + innerNodesString() + " } ";
        }

        /// <summary>
        /// calculates the average X and Y value of all inner nodes for *base* layer. Sets these values as X and Y
        /// </summary>
        public void calculateXY(NodeCollection nodes)
        {
            int aX = 0;
            int aY = 0;
            foreach (var nodeID in innerNodes)
            {
                aX += nodes[nodeID].Location.X;
                aY += nodes[nodeID].Location.Y;
            }

            X = aX / innerNodes.Count;
            Y = aY / innerNodes.Count;
        }

        /// <summary>
        /// calculates the average X and Y value of all inner nodes for *higher* layers. Sets these values as X and Y
        /// </summary>
        public void calculateXY(PRAbstractionLayer lowerLayer)
        {
            int aX = 0;
            int aY = 0;
            foreach (var nodeID in innerNodes)
            {
                aX += lowerLayer.ClusterNodes[nodeID].X;
                aY += lowerLayer.ClusterNodes[nodeID].Y;
            }

            X = aX / innerNodes.Count;
            Y = aY / innerNodes.Count;
        }

        private string innerNodesString()
        {
            string s = "";
            for (int i = 0; i < innerNodes.Count; ++i)
            {
                s += (i < innerNodes.Count - 1) ? innerNodes[i] + ", " : innerNodes[i]+"";
            }
            return s;
        }

        internal void AddNeighbors(List<PRAClusterNode> clusters)
        {
            foreach (var c in clusters)
            {
                AddNeighbor(c.ID, c);
            }
        }

        /// <summary>
        /// Remembers the PRACluster as a Neighbor. If it already is a neighbor, returns and does nothing.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        internal void AddNeighbor(int key, PRAClusterNode value)
        {
            if (!neighbors.ContainsKey(key) && key != this.ID)
            {
                neighbors.Add(key, value);
                calculateHDist(value);
            }
        }

        internal void calculateHDist(PRAClusterNode neighbor)
        {
            //sqrt(2) * min( abs(dx), abs(dy) ) + (abs(dx) - abs(dy))
            if (!neighborDist.ContainsKey(neighbor.ID))
            {
                int dx = Math.Abs(this.X - neighbor.X);
                int dy = Math.Abs(this.Y - neighbor.Y);

                float dist = 1.4f * Math.Min(dx, dy) + Math.Abs(dx - dy);

                this.neighborDist.Add(neighbor.ID, dist);
            }
            
        }

        internal bool HasNeighbor(int nodeID)
        {
            return this.neighbors.ContainsKey(nodeID);
        }

        internal bool HasAllNeighbors(List<int> neighbors)
        {
            foreach (int n in neighbors)
            {
                if (!this.neighbors.ContainsKey(n))
                {
                    return false;
                }
            }
            return true;
        }

        internal void InitPRAClusterParents(NodeCollection nodes)
        {
            if (abstractionLayerID == 0)
            {
                foreach (var nodeID in innerNodes)
                {
                    nodes[nodeID].InitNodePRAClusterParent(this.ID);
                }
            }
        }
    }
}
