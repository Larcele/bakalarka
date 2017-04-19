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
    }
}
