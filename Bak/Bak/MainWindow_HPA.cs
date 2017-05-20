using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Bak
{
    partial class MainWindow : Form
    {
        private void StartHPAstarSearch(object sender, DoWorkEventArgs e)
        {
            //do partial a* searches in clusters and visualise them

            //concat it all to pathfindingSolution

            StartAstarSearch(null, null);

        }

        private void BuildHPAClusters()
        {
            HierarchicalGraph.Clear();

            AbstractionLayer layer = new AbstractionLayer(0, new List<Cluster>());

            //we make 10 x 10 clusters , from the pper left to the lower right.
            if (gMap.Width >= 20 && gMap.Height >= 20)
            {
                int i = 0;
                int j = 0;
                for (j = 0; j <= gMap.Height / HPACsize; ++j)
                {
                    for (i = 0; i < gMap.Width; i += HPACsize)
                    {
                        CreateHPACluster(layer, i, j);
                    }
                }
            }
            else //otherwise, we divide the map to 4 clusters by the size of width / 2 x height / 2
            {
                int i = 0;
                int j = 0;
                int tmpClusterW = gMap.Width / 2;
                int tmpClusterH = gMap.Height / 2;

                for (j = 0; j <= gMap.Height / tmpClusterH; ++j)
                {
                    for (i = 0; i < gMap.Width; i += tmpClusterW)
                    {
                        CreateHPACluster(layer, i, j);
                    }
                }
            }

            HierarchicalGraph.Add(layer.ID, layer);
            
            BuildClusterConnections(layer);

            #region shite
            /*int xDiv = gMap.Width / 2;
            int yDiv = gMap.Height / 2;

            int i;
            int j;

            Cluster c1 = new Cluster(0);
            Cluster c2 = new Cluster(1);
            Cluster c3 = new Cluster(2);
            Cluster c4 = new Cluster(3);

            List<int> inNodes = new List<int>();

            //upper left cluser
            for (i = 0; i < yDiv * gMap.Width; i += gMap.Width)
            {
                for (j = 0; j < xDiv; ++j)
                {
                    inNodes.Add(j + i);
                    if (firstRow(i, 0) ||
                        lastRow(i, yDiv * gMap.Width) ||
                        firstCol(j, 0) ||
                        lastCol(j, xDiv - 1)) { //this node could be one of the outer nodes later
                        if (gMap.Nodes[i + j].IsTraversable())
                        {
                            c1.OuterNodes.Add(i + j);
                        }
                    }
                }
            }
            c1.SetInnerNodes(inNodes);
            inNodes.Clear();
            //lower left cluster
            for (i = yDiv * gMap.Width; i < gMap.Height * gMap.Width; i += gMap.Width)
            {
                for (j = 0; j < xDiv; ++j)
                {
                    inNodes.Add(j + i);
                    if (firstRow(i, yDiv * gMap.Width) ||
                        lastRow(i, gMap.Height * gMap.Width) ||
                        firstCol(j, 0) ||
                        lastCol(j, xDiv - 1)) { //this node could be one of hte outer nodes later
                        if (gMap.Nodes[i + j].IsTraversable())
                        {
                            c2.OuterNodes.Add(i + j);
                        }
                    }
                }
            }
            c2.SetInnerNodes(inNodes);
            inNodes.Clear();
            //upper right cluster
            for (i = 0; i < yDiv * gMap.Width; i += gMap.Width)
            {
                for (j = xDiv; j < gMap.Width; ++j)
                {
                    inNodes.Add(j + i);
                    if (firstRow(i, 0) ||
                        lastRow(i, yDiv * gMap.Width) ||
                        firstCol(j, xDiv) ||
                        lastCol(j, gMap.Width - 1)) { //this node could be one of hte outer nodes later
                        
                        if (gMap.Nodes[i + j].IsTraversable())
                        {
                            c3.OuterNodes.Add(i + j);
                        }
                    }
                }
            }
            c3.SetInnerNodes(inNodes);
            inNodes.Clear();
            //lower right cluster
            for (i = yDiv * gMap.Width; i < gMap.Height * gMap.Width; i += gMap.Width)
            {
                for (j = xDiv; j < gMap.Width; ++j)
                {
                    inNodes.Add(j + i);
                    if (firstRow(i, yDiv * gMap.Width) ||
                        lastRow(i, gMap.Height * gMap.Width) ||
                        firstCol(j, xDiv) ||
                        lastCol(j, gMap.Width - 1) ) { //this node could be one of hte outer nodes later
                    
                        if (gMap.Nodes[i + j].IsTraversable())
                        {
                            c4.OuterNodes.Add(i + j);
                        }
                    }
                }
            }
            c4.SetInnerNodes(inNodes);
            inNodes.Clear();

            List<Cluster> clusters = new List<Cluster> { c1, c2, c3, c4 };
            AbstractionLayer absl = new AbstractionLayer(0, clusters);

            HierarchicalGraph.Add(0, absl);

            BuildClusterConnections(absl);*/
            #endregion
        }

        public void CreateHPACluster(AbstractionLayer layer, int columnPos, int rowPos)
        {
            Cluster c = new Cluster(layer.LastAssignedClusterID, new List<int>(), new List<int>());
            layer.LastAssignedClusterID++;

            List<int> innerNodes = new List<int>();
            List<int> outerNodes = new List<int>();

            int startY = rowPos * HPACsize * gMap.Width;
            int endY = startY == 0 ? HPACsize * gMap.Width : 2 * startY;// <- toto je uplna sracka. oprav to

            int startX = columnPos;
            int endX = columnPos + HPACsize; //non-inclusive

            for (int j = startY; j < endY; j += gMap.Width)
            {
                for (int i = startX; i < endX; ++i)
                {
                    if (gMap.Nodes.ContainsKey(i + j))
                    {
                        gMap.Nodes[i + j].HPAClusterParent = c.ID;
                        innerNodes.Add(i + j);
                        if (j == startY || i == startX || i == endX - 1 || j + gMap.Width > endY)
                        {
                            outerNodes.Add(i + j);
                        }
                    }

                }
            }

            if (innerNodes.Count != 0 && outerNodes.Count != 0)
            {
                c.SetInnerNodes(innerNodes);
                c.SetOuterNodes(outerNodes);
                layer.Clusters.Add(c.ID, c);
            }
            else
            {
                //do not add the cluster to layer; decrease the ID
                layer.LastAssignedClusterID--;
            }
        }

        private void BuildClusterConnections(AbstractionLayer absl)
        {
            foreach (var cnode in absl.Clusters)
            {
                if (cnode.Key == 11)
                {
                    string s = "";
                }
                var neighbors = getHPAClusterNeighbors(cnode.Value);
                foreach (var n in neighbors)
                {
                    cnode.Value.AddNeighbor(n.ID, n);
                    n.AddNeighbor(cnode.Key, cnode.Value);
                }

            }
        }

        private List<Cluster> getHPAClusterNeighbors(Cluster c)
        {
            List<Cluster> res = new List<Cluster>();
            foreach (int n in c.OuterNodes)
            {
                foreach (var n2 in gMap.Nodes[n].Neighbors.Keys)
                {
                    Node neigh = gMap.Nodes[n2];
                    if (neigh.IsTraversable() && !c.OuterNodes.Contains(n2))
                    {
                        //get the HPAClusterParent of this node and add it to the list
                        int parentID = neigh.HPAClusterParent;
                        Cluster p = HierarchicalGraph[0].Clusters[parentID];
                        if (!res.Contains(p))
                        {
                            res.Add(p);
                        }
                    }
                }
            }
            return res;
        }

        #region probably deprecated soon


        private bool firstCol(int col, int first)
        {
            return col == first;
        }

        private bool lastCol(int col, int last)
        {
            return col == last;
        }

        private bool lastRow(int row, int last)
        {
            return row <= last && row >= last - gMap.Width;
        }

        private bool firstRow(int row, int first)
        {
            return row == first;
        }


        #endregion

    }
}
