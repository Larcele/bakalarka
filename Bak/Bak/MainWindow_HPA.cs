﻿using System;
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
            BuildClusterNodes(layer);

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
            int endY = startY == 0 ? HPACsize * gMap.Width : startY + gMap.Width * 10;

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
                        if (j == startY || i == startX || i == endX - 1 || j + gMap.Width >= endY)
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

        private void BuildClusterConnections(AbstractionLayer layer)
        {
            foreach (var cnode in layer.Clusters)
            {
                var neighbors = getHPAClusterNeighbors(cnode.Value);
                foreach (var n in neighbors)
                {
                    cnode.Value.AddNeighbor(n.ID, n);
                    n.AddNeighbor(cnode.Key, cnode.Value);
                }

            }
        }

        private void BuildClusterNodes(AbstractionLayer layer)
        {
            foreach (var c in layer.Clusters)
            {
                //get the neighbors of the cluster's outerNodes
                var r1 = getHPAOuterNodesNeighbors(c.Value);
                foreach (var neigh in c.Value.Neighbors)
                {
                    var traversableOuters = neigh.Value.OuterNodes.Where(n => gMap.Nodes[n].IsTraversable()).ToList();
                    List<int> res = new List<int>();

                    if (r1.Count > traversableOuters.Count)
                    {
                        res = r1.Keys.Intersect(traversableOuters).ToList();
                    }
                    else
                    {
                        res = traversableOuters.Intersect(r1.Keys).ToList();
                    }

                    if (res.Count() != 0)
                    {
                        //if the intersection is less than 4 we create a single connection.
                        if (res.Count() <= 5)
                        {
                            int index = res.Count() / 2;
                            int cnodeNeighbor = res[index];

                            //the second node is the corresponding neighbor from r1
                            int cnode = r1[cnodeNeighbor];

                            ClusterNode cN = new ClusterNode(c.Value.LastAssignedCNodeID, cnode);
                            ClusterNode neighN = new ClusterNode(neigh.Value.LastAssignedCNodeID, cnodeNeighbor);

                            cN.Neighbors.Add(neighN.ID, gMap.Nodes[cnode].Neighbors[cnodeNeighbor]);
                            neighN.Neighbors.Add(cN.ID, gMap.Nodes[cnodeNeighbor].Neighbors[cnode]);

                            if (!c.Value.ClusterNodes.ContainsKey(cN.ID) && !neigh.Value.ClusterNodes.ContainsKey(neighN.ID))
                            {
                                if (!c.Value.CNodeHasGNodeIdOf(cN) && !neigh.Value.CNodeHasGNodeIdOf(neighN))
                                {
                                    //if the clusters don't contain these nodes, then raise ID and add them
                                    c.Value.ClusterNodes.Add(cN.ID, cN);
                                    neigh.Value.ClusterNodes.Add(neighN.ID, neighN);

                                    c.Value.LastAssignedCNodeID++;
                                    neigh.Value.LastAssignedCNodeID++;
                                }
                            }
                        }
                        else
                        {
                            int index1 = res.Count() - 1;
                            int cnodeNeighbor1 = res[index1];

                            int index2 = 0;
                            int cnodeNeighbor2 = res[index2];

                            //the second node is the corresponding neighbor from r1
                            int cnode1 = r1[cnodeNeighbor1];

                            //the second node is the corresponding neighbor from r1
                            int cnode2 = r1[cnodeNeighbor2];

                            ClusterNode cN = new ClusterNode(c.Value.LastAssignedCNodeID, cnode1);
                            ClusterNode neighN = new ClusterNode(neigh.Value.LastAssignedCNodeID, cnodeNeighbor1);

                            cN.Neighbors.Add(neighN.ID, gMap.Nodes[cnode1].Neighbors[cnodeNeighbor1]);
                            neighN.Neighbors.Add(cN.ID, gMap.Nodes[cnodeNeighbor1].Neighbors[cnode1]);

                            if (!c.Value.ClusterNodes.ContainsKey(cN.ID) && !neigh.Value.ClusterNodes.ContainsKey(neighN.ID))
                            {
                                if (!c.Value.CNodeHasGNodeIdOf(cN) && !neigh.Value.CNodeHasGNodeIdOf(neighN))
                                {
                                    //if the clusters don't contain these nodes, then raise ID and add them
                                    c.Value.ClusterNodes.Add(cN.ID, cN);
                                    neigh.Value.ClusterNodes.Add(neighN.ID, neighN);

                                    c.Value.LastAssignedCNodeID++;
                                    neigh.Value.LastAssignedCNodeID++;
                                }
                            }

                            ClusterNode cN2 = new ClusterNode(c.Value.LastAssignedCNodeID, cnode2);
                            ClusterNode neighN2 = new ClusterNode(neigh.Value.LastAssignedCNodeID, cnodeNeighbor2);

                            cN2.Neighbors.Add(neighN2.ID, gMap.Nodes[cnode2].Neighbors[cnodeNeighbor2]);
                            neighN2.Neighbors.Add(cN2.ID, gMap.Nodes[cnodeNeighbor2].Neighbors[cnode2]);

                            if (!c.Value.ClusterNodes.ContainsKey(cN2.ID) && !neigh.Value.ClusterNodes.ContainsKey(neighN2.ID))
                            {
                                if (!c.Value.CNodeHasGNodeIdOf(cN2) && !neigh.Value.CNodeHasGNodeIdOf(neighN2))
                                {
                                    //if the clusters don't contain these nodes, then raise ID and add them
                                    c.Value.ClusterNodes.Add(cN2.ID, cN2);
                                    neigh.Value.ClusterNodes.Add(neighN2.ID, neighN2);

                                    c.Value.LastAssignedCNodeID++;
                                    neigh.Value.LastAssignedCNodeID++;
                                }
                            }

                        }
                    }

                }
            }
        }

       /* private List<Tuple<int,int>> getAdjacentClusterPairs(Dictionary<int, int> c1OuterNodesAndNeighbors, List<int> c2OuterNodes)
        {
            //c1OuterNodesAndNeighbors.Keys are the outerNodes of c2OuterNodes we check for intersection

            List<Tuple<int, int>> res = new List<Tuple<int, int>>();
            if (c1OuterNodeNeighbors.Count > c2OuterNodes.Count)
            {
                foreach (int neigh in c1OuterNodeNeighbors)
                {

                }
            }
            else
            {

            }

        }*/

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

        /// <summary>
        /// Builds a dictionary of pairs of nodes; 
        /// the key is the NEIGHBOR node of the Clucter c; the value is THE node from Cluster c.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private Dictionary<int, int> getHPAOuterNodesNeighbors(Cluster c)
        {
            Dictionary<int,int> res = new Dictionary<int, int>();
            foreach (int n in c.OuterNodes)
            {
                if (!gMap.Nodes[n].IsTraversable()) { continue;}

                foreach (var n2 in gMap.Nodes[n].Neighbors.Keys)
                {
                    Node neigh = gMap.Nodes[n2];
                    if (neigh.IsTraversable() && !c.OuterNodes.Contains(n2) && !c.InnerNodes.Contains(neigh.ID))
                    {
                        if (!res.ContainsKey(neigh.ID))
                        {
                            res.Add(neigh.ID, n);
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
