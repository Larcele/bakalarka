using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bak
{

    public class AbstractionLayer
    {
        private int id;
        Dictionary<int, Cluster> clusters = new Dictionary<int, Cluster>();

        public int ID { get { return id; } }

        AbstractionLayer higherLevel;
        AbstractionLayer lowerLevel;

        public Dictionary<int, Cluster> Clusters { get { return clusters; } }
        public Dictionary<int, ClusterNode> ClusterNodes = new Dictionary<int, ClusterNode>();

        public AbstractionLayer(int id, List<Cluster> clusters, AbstractionLayer h = null, AbstractionLayer l = null)
        {
            this.id = id;

            foreach (var c in clusters)
            {
                this.clusters.Add(c.ID, c);
            }

            higherLevel = h;
            lowerLevel = l;

        }
    }
}
