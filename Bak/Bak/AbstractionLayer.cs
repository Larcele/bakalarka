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
        
        public Dictionary<int, Cluster> Clusters { get { return clusters; } }

        public AbstractionLayer(int id, List<Cluster> clusters)
        {
            this.id = id;

            foreach (var c in clusters)
            {
                this.clusters.Add(c.ID, c);
            }
            

        }
    }
}
