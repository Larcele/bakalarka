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

        public List<int> innerNodes = new List<int>();
        public Dictionary<int, PRAClusterNode> neighbors = new Dictionary<int, PRAClusterNode>(); //neighboring PRAClusterNodes

        public PRAClusterNode(int id, List<int> nodes)
        {
            ID = id;
            innerNodes = nodes;
        }

        public override string ToString()
        {
            return "PRA* Cluster; AbsID:" + abstractionLayerID + "; { " + innerNodesString() + " } ";
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

        internal void AddNeighbor(int key, PRAClusterNode value)
        {
            if (!neighbors.ContainsKey(key))
            {
                neighbors.Add(key, value);
            }
        }
    }
}
