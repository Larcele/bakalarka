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
        public Dictionary<int, PRAClusterNode> nodes = new Dictionary<int, PRAClusterNode>();

        public PRAbstractionLayer(int id)
        {
            ID = id;
        }

        public void AddClusterNode(PRAClusterNode node)
        {
            this.nodes.Add(node.ID, node);
            node.abstractionLayerID = this.ID;
        }
    }
}
