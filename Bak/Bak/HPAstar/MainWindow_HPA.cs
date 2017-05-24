using Bak.HPAstar;
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
            List<int> path = new List<int>();

            //get the start node cluster
            int startCID = gMap.Nodes[gMap.StartNodeID].HPAClusterParent;
            int endCID = gMap.Nodes[gMap.EndNodeID].HPAClusterParent;
            Cluster startingCluster = HierarchicalGraph[0].Clusters[startCID];
            Cluster endingCluster = HierarchicalGraph[0].Clusters[endCID];

            //we create the temporary start clusterNode and calculate the distance between 
            //it and other cluster nodes
            ClusterNode tmpStart = new ClusterNode(gMap.StartNodeID);
            startingCluster.ClusterNodes.Add(tmpStart.GNodeID, tmpStart);
            tmpStart.ClusterParent = startCID;
            calculateDistInnerClusterNodes_Tmp(tmpStart, startingCluster);

            ClusterNode tmpEnd = new ClusterNode(gMap.EndNodeID);
            endingCluster.ClusterNodes.Add(tmpEnd.GNodeID, tmpEnd);
            tmpEnd.ClusterParent = endCID;
            calculateDistInnerClusterNodes_Tmp(tmpEnd, endingCluster);

            var abstractPath = AstarAbstractHPASearch(tmpStart, endingCluster);
            startingCluster.ClusterNodes.Remove(tmpStart.GNodeID);
            endingCluster.ClusterNodes.Remove(tmpEnd.GNodeID);

            stopWatch.Stop();
        }

        private void BuildHPAClusters()
        {
            HierarchicalGraph.Clear();

            AbstractionLayer layer = new AbstractionLayer(0, new List<Cluster>());

            //we make 10 x 10 clusters , from the pper left to the lower right.
            if (gMap.Width >= 20 && gMap.Height >= 20)
            {
                HPACsize = 10;

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
                HPACsize = gMap.Width / 2;

                for (j = 0; j <= gMap.Height / HPACsize; ++j)
                {
                    for (i = 0; i < gMap.Width; i += HPACsize)
                    {
                        CreateHPACluster(layer, i, j);
                    }
                }
            }

            HierarchicalGraph.Add(layer.ID, layer);
            
            BuildClusterConnections(layer);
            BuildClusterNodes(layer);
            BuildClusterNodeIntraEdges(layer);

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
            Cluster c = new Cluster(layer.LastAssignedClusterID);
            layer.LastAssignedClusterID++;

            HashSet<int> innerNodes = new HashSet<int>();
            Dictionary<char, OuterNodeArea> outerNodes = new Dictionary<char, OuterNodeArea>
            {
                {'U', new OuterNodeArea() },
                {'D', new OuterNodeArea() },
                {'L', new OuterNodeArea() },
                {'R', new OuterNodeArea() },
            };

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
                            char direction = 'D'; //default init

                            if (j == startY) //first row. 'U' outer node
                            { direction = 'U'; }

                            else if (i == startX) //first column. 'L' node
                            { direction = 'L'; }

                            else if (i == endX - 1) //last column. 'R' node
                            { direction = 'R'; }

                            else //j + gMap.Width >= endY. last row. 'D' node
                            { direction = 'D'; }

                            outerNodes[direction].Add(i + j);
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
                //for every cluster, we look at its OuterNodes - all 4 sides.
                //if a side is already resolved, we continue checking the othrs. 
                //If it is not, we look at the neighboring nodes. If they are traversable, 
                //we start tracking an entrance. We expand an entrance as long as both the neighboring nodes
                //are traversable. If one of them is not, we stop, look at the number of entrances and 
                //dependng on its size, create the corresponding number of cluster nodes. 

                foreach (var side in c.Value.OuterNodes)
                {
                   // if (side.Value.IsResolved()) { continue; }

                    switch (side.Key)
                    {
                        case 'U':
                            //get the upper neighbor
                            Cluster neighbor = c.Value.GetNeighbor('U');
                            if (neighbor == null) { continue; }

                            //now we start tracking entrances between these clusters.
                            List<Tuple<int, int>> possibleEntrances = new List<Tuple<int, int>>();
                            foreach (var n in side.Value)
                            {
                                int id = n - gMap.Width;
                                if (gMap.Nodes[n].IsTraversable() && gMap.Nodes[id].IsTraversable())
                                {
                                    //ITEM1 is the Cluster node, ITEM2 is the NEIGHBOR cluster node
                                    possibleEntrances.Add(Tuple.Create(n, id));
                                }
                                else
                                {
                                    //there is an obstacle on either side of the outer nodes. 
                                    //get the number of items in hte possibleEntrances and create the cluster Nodes.
                                    //after that, clear the possibleEntrances list and continue;
                                    if (possibleEntrances.Count != 0 && possibleEntrances.Count < 5)
                                    {
                                        //make a single entrance in the middle.
                                        //the FIRST tuple item is the node of 'c' and the second item is the node of neighbor
                                        Tuple<int, int> entrance = possibleEntrances[possibleEntrances.Count / 2];

                                        if (!HierarchicalGraph[0].AbstractNodes.ContainsKey(entrance.Item1))
                                        {
                                            ClusterNode c1 = new ClusterNode(entrance.Item1);
                                            HierarchicalGraph[0].AbstractNodes.Add(c1.GNodeID, c1);
                                            c.Value.ClusterNodes.Add(c1.GNodeID, c1);
                                        }

                                        if (!HierarchicalGraph[0].AbstractNodes.ContainsKey(entrance.Item2))
                                        {
                                            ClusterNode c2 = new ClusterNode(entrance.Item2);
                                            HierarchicalGraph[0].AbstractNodes.Add(c2.GNodeID, c2);
                                            neighbor.ClusterNodes.Add(c2.GNodeID, c2);
                                        }

                                        if (!HierarchicalGraph[0].AbstractNodes[entrance.Item1].Neighbors.ContainsKey(entrance.Item2))
                                        {
                                            HierarchicalGraph[0].AbstractNodes[entrance.Item1].Neighbors.Add(entrance.Item2, 1);
                                        }
                                        if (!HierarchicalGraph[0].AbstractNodes[entrance.Item2].Neighbors.ContainsKey(entrance.Item1))
                                        {
                                            HierarchicalGraph[0].AbstractNodes[entrance.Item2].Neighbors.Add(entrance.Item1, 1);
                                        }

                                        possibleEntrances.Clear();

                                    }
                                    else if (possibleEntrances.Count >= 5)//the continuous entrance length is higher than/equal to 5
                                    {
                                        //we create two entrances in this case, one on the start and one on the end.
                                        Tuple<int, int> entrance1 = possibleEntrances[0];
                                        Tuple<int, int> entrance2 = possibleEntrances[possibleEntrances.Count - 1];

                                        //------------------FIRST
                                        if (!HierarchicalGraph[0].AbstractNodes.ContainsKey(entrance1.Item1))
                                        {
                                            ClusterNode c1 = new ClusterNode(entrance1.Item1);
                                            HierarchicalGraph[0].AbstractNodes.Add(c1.GNodeID, c1);
                                            c.Value.ClusterNodes.Add(c1.GNodeID, c1);
                                        }

                                        if (!HierarchicalGraph[0].AbstractNodes.ContainsKey(entrance1.Item2))
                                        {
                                            ClusterNode c2 = new ClusterNode(entrance1.Item2);
                                            HierarchicalGraph[0].AbstractNodes.Add(c2.GNodeID, c2);
                                            neighbor.ClusterNodes.Add(c2.GNodeID, c2);
                                        }

                                        if (!HierarchicalGraph[0].AbstractNodes[entrance1.Item1].Neighbors.ContainsKey(entrance1.Item2))
                                        {
                                            HierarchicalGraph[0].AbstractNodes[entrance1.Item1].Neighbors.Add(entrance1.Item2, 1);
                                        }
                                        if (!HierarchicalGraph[0].AbstractNodes[entrance1.Item2].Neighbors.ContainsKey(entrance1.Item1))
                                        {
                                            HierarchicalGraph[0].AbstractNodes[entrance1.Item2].Neighbors.Add(entrance1.Item1, 1);
                                        }
                                        //-----------------SECOND
                                        if (!HierarchicalGraph[0].AbstractNodes.ContainsKey(entrance2.Item1))
                                        {
                                            ClusterNode c1 = new ClusterNode(entrance2.Item1);
                                            HierarchicalGraph[0].AbstractNodes.Add(c1.GNodeID, c1);
                                            c.Value.ClusterNodes.Add(c1.GNodeID, c1);
                                        }

                                        if (!HierarchicalGraph[0].AbstractNodes.ContainsKey(entrance2.Item2))
                                        {
                                            ClusterNode c2 = new ClusterNode(entrance2.Item2);
                                            HierarchicalGraph[0].AbstractNodes.Add(c2.GNodeID, c2);
                                            neighbor.ClusterNodes.Add(c2.GNodeID, c2);
                                        }

                                        if (!HierarchicalGraph[0].AbstractNodes[entrance2.Item1].Neighbors.ContainsKey(entrance2.Item2))
                                        {
                                            HierarchicalGraph[0].AbstractNodes[entrance2.Item1].Neighbors.Add(entrance2.Item2, 1);
                                        }
                                        if (!HierarchicalGraph[0].AbstractNodes[entrance2.Item2].Neighbors.ContainsKey(entrance2.Item1))
                                        {
                                            HierarchicalGraph[0].AbstractNodes[entrance2.Item2].Neighbors.Add(entrance2.Item1, 1);
                                        }

                                        possibleEntrances.Clear();
                                    }
                                }
                            }
                            break;

                        case 'D':
                            break;

                        case 'L':
                            break;

                        case 'R':
                            break;
                    }
                }
            }

            #region disgusting asduiakdj
            /* foreach (var c in layer.Clusters)
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

                             cN.Neighbors.Add(neighN.GNodeID, gMap.Nodes[cnode].Neighbors[cnodeNeighbor]);
                             neighN.Neighbors.Add(cN.GNodeID, gMap.Nodes[cnodeNeighbor].Neighbors[cnode]);

                             if (!c.Value.ClusterNodes.ContainsKey(cN.ID) && !neigh.Value.ClusterNodes.ContainsKey(neighN.ID))
                             {
                                 if (!c.Value.CNodeHasGNodeIdOf(cN) && !neigh.Value.CNodeHasGNodeIdOf(neighN))
                                 {
                                     //if the clusters don't contain these nodes, then set cluster parents, 
                                     //raise ID and add them
                                     c.Value.ClusterNodes.Add(cN.ID, cN);
                                     cN.ClusterParent = c.Value.ID;
                                     neigh.Value.ClusterNodes.Add(neighN.ID, neighN);
                                     neighN.ClusterParent = neigh.Value.ID;

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

                             cN.Neighbors.Add(neighN.GNodeID, gMap.Nodes[cnode1].Neighbors[cnodeNeighbor1]);
                             neighN.Neighbors.Add(cN.GNodeID, gMap.Nodes[cnodeNeighbor1].Neighbors[cnode1]);

                             if (!c.Value.ClusterNodes.ContainsKey(cN.ID) && !neigh.Value.ClusterNodes.ContainsKey(neighN.ID))
                             {
                                 if (!c.Value.CNodeHasGNodeIdOf(cN) && !neigh.Value.CNodeHasGNodeIdOf(neighN))
                                 {
                                     //if the clusters don't contain these nodes, then set cluster parents, 
                                     //raise ID and add them
                                     c.Value.ClusterNodes.Add(cN.ID, cN);
                                     cN.ClusterParent = c.Value.ID;
                                     neigh.Value.ClusterNodes.Add(neighN.ID, neighN);
                                     neighN.ClusterParent = neigh.Value.ID;

                                     c.Value.LastAssignedCNodeID++;
                                     neigh.Value.LastAssignedCNodeID++;
                                 }
                             }

                             ClusterNode cN2 = new ClusterNode(c.Value.LastAssignedCNodeID, cnode2);
                             ClusterNode neighN2 = new ClusterNode(neigh.Value.LastAssignedCNodeID, cnodeNeighbor2);

                             cN2.Neighbors.Add(neighN2.GNodeID, gMap.Nodes[cnode2].Neighbors[cnodeNeighbor2]);
                             neighN2.Neighbors.Add(cN2.GNodeID, gMap.Nodes[cnodeNeighbor2].Neighbors[cnode2]);

                             if (!c.Value.ClusterNodes.ContainsKey(cN2.ID) && !neigh.Value.ClusterNodes.ContainsKey(neighN2.ID))
                             {
                                 if (!c.Value.CNodeHasGNodeIdOf(cN2) && !neigh.Value.CNodeHasGNodeIdOf(neighN2))
                                 {
                                     //if the clusters don't contain these nodes, then set cluster parents, 
                                     //raise ID and add them
                                     c.Value.ClusterNodes.Add(cN2.ID, cN2);
                                     cN2.ClusterParent = c.Value.ID;
                                     neigh.Value.ClusterNodes.Add(neighN2.ID, neighN2);
                                     neighN2.ClusterParent = neigh.Value.ID;

                                     c.Value.LastAssignedCNodeID++;
                                     neigh.Value.LastAssignedCNodeID++;
                                 }
                             }

                         }
                     }

                 }
             }*/
            #endregion
        }
        
        private void BuildClusterNodeIntraEdges(AbstractionLayer layer)
        {
            foreach (var c in layer.Clusters.Values)
            {
                foreach (var cn in c.ClusterNodes.Values)
                {
                    calculateDistInnerClusterNodes(cn, c);
                }
            }
        }

        private void calculateDistInnerClusterNodes_Tmp(ClusterNode current, Cluster clusterOfNodesTosearch)
        {
            foreach (var node in clusterOfNodesTosearch.ClusterNodes.Values)
            {
                //the distance was computed already
                if (current.Neighbors.ContainsKey(node.GNodeID) || current == node) { continue; }

                float cost = AstarDistance(current.GNodeID, node.GNodeID, clusterOfNodesTosearch.InnerNodes);
                if (cost == 0)
                {
                    //there is no path
                }
                else
                {
                    //this is a tmp structure for start/end node conections; We will delete this tmp node
                    //(current) after the search, so we only give neighbors to current, not the oter way around
                    current.Neighbors.Add(node.GNodeID, cost);
                }
            }
        }

        private void calculateDistInnerClusterNodes(ClusterNode current, Cluster clusterOfNodesTosearch)
        {
            foreach (var node in clusterOfNodesTosearch.ClusterNodes.Values)
            {
                //the distance was computed already
                if (current.Neighbors.ContainsKey(node.GNodeID) 
                    || node.Neighbors.ContainsKey(current.GNodeID) 
                    || current == node) { continue; }

                float cost = AstarDistance(current.GNodeID, node.GNodeID, clusterOfNodesTosearch.InnerNodes);
                if (cost == 0)
                {
                    //there is no path
                }
                else
                {
                    //now add both the clusterNodes as neighbors the distance
                    current.Neighbors.Add(node.GNodeID, cost);
                    node.Neighbors.Add(current.GNodeID, cost);
                }
            }
        }

        private List<int> AstarAbstractHPASearch(ClusterNode start, Cluster end)
        {
            List<int> sol = new List<int>();
            
            Dictionary<int, bool> closedSet = new Dictionary<int, bool>();
            foreach (var c in HierarchicalGraph[0].Clusters)
            {
                foreach (var cn in c.Value.ClusterNodes.Values)
                {
                    closedSet.Add(cn.GNodeID, false);
                }
            }
            Dictionary<int, ClusterNode> openSet = new Dictionary<int, ClusterNode>();

            //starting node is in the open set
            openSet.Add(gMap.StartNodeID, start);

            // For each clusternode, which clusternode it can most efficiently be reached from.
            // If a cnode can be reached from many cnodes, cameFrom will eventually contain the
            // most efficient previous step.
            Dictionary<int, int> cameFrom = new Dictionary<int, int>();

            // For each node, the cost of getting from the start node to that node.
            Dictionary<int, float> gScore = new Dictionary<int, float>();
            //default values are infinity
            foreach (var c in HierarchicalGraph[0].Clusters)
            {
                foreach (var cn in c.Value.ClusterNodes.Values)
                {
                    gScore.Add(cn.GNodeID, float.MaxValue);
                }
            }
            // The cost of going from start to start is zero.
            gScore[start.GNodeID] = 0;

            // For each node, the total cost of getting from the start node to the goal
            // by passing by that node. That value is partly known, partly heuristic.
            Dictionary<int, float> fScore = new Dictionary<int, float>();
            //default values are infinity
            foreach (var c in HierarchicalGraph[0].Clusters)
            {
                foreach (var cn in c.Value.ClusterNodes.Values)
                {
                    fScore.Add(cn.GNodeID, float.MaxValue);
                }
            }

            // For the first node, that value is completely heuristic.
            fScore[start.GNodeID] = H_startEnd(start.GNodeID, gMap.EndNodeID);

            int currNode = 0; //default value

            while (openSet.Count != 0)
            {
                //the node in openSet having the lowest fScore value
                currNode = openSet.OrderBy(i => fScore[i.Key]).FirstOrDefault().Key;
                if (end.InnerNodes.Contains(currNode) && end.ClusterNodes[gMap.EndNodeID].Neighbors.ContainsKey(currNode))
                {
                    //break the loop and reconstruct path below
                    //we reached the end cluster. Break and finish the path to the end node below
                    break;
                }

                openSet.Remove(currNode);
                closedSet[currNode] = true; //"added" to closedList
                
                int cID = gMap.Nodes[currNode].HPAClusterParent;
                Cluster c = HierarchicalGraph[0].Clusters[cID];
                ClusterNode cn = HierarchicalGraph[0].GetCNodeByGID(c, currNode);

                foreach (var neighbor in cn.Neighbors)
                {
                    // Ignore the neighbor which is already evaluated.
                    if (!closedSet.ContainsKey(neighbor.Key) || closedSet[neighbor.Key] == true)
                    { continue; }

                    // The distance from start to a neighbor
                    float tentativeG = gScore[currNode] + neighbor.Value;
                    if (!openSet.ContainsKey(neighbor.Key)) // Discover a new node
                    {
                        Cluster neighC = HierarchicalGraph[0].Clusters[gMap.Nodes[neighbor.Key].HPAClusterParent];
                        openSet.Add(neighbor.Key, HierarchicalGraph[0].GetCNodeByGID(neighC, neighbor.Key));
                    }
                    else if (tentativeG >= gScore[neighbor.Key])
                    {
                        continue; //not a better path
                    }

                    // This path is the best until now. Record it!
                    cameFrom[neighbor.Key] = currNode;
                    gScore[neighbor.Key] = tentativeG;
                    fScore[neighbor.Key] = gScore[neighbor.Key] + H_startEnd(neighbor.Key, gMap.EndNodeID);
                }
            }

            sol.Add(currNode);
            while (cameFrom.ContainsKey(currNode))
            {
                currNode = cameFrom[currNode];
                sol.Add(currNode);
            }
            //NOT setting pathcost since this is still an abstraction layer
            //pathCost = gScore[gMap.EndNodeID];
            // pathfinder.CancelAsync();
            //invalidater.CancelAsync();

            return sol;


        }

        private float AstarDistance(int startID, int endID, HashSet<int> nodesToSearch)
        {
            float res = 0;

            List<int> closedSet = new List<int>();
            List<int> openSet = new List<int>();

            //starting node is in the open set
            openSet.Add(startID);

            // For each node, which node it can most efficiently be reached from.
            // If a node can be reached from many nodes, cameFrom will eventually contain the
            // most efficient previous step.
            Dictionary<int, int> cameFrom = new Dictionary<int, int>();

            // For each node, the cost of getting from the start node to that node.
            Dictionary<int, float> gScore = new Dictionary<int, float>();
            //default values are infinity
            foreach (int nodeID in nodesToSearch)
            {
                gScore.Add(nodeID, float.MaxValue);
            }
            // The cost of going from start to start is zero.
            gScore[startID] = 0;

            // For each node, the total cost of getting from the start node to the goal
            // by passing by that node. That value is partly known, partly heuristic.
            Dictionary<int, float> fScore = new Dictionary<int, float>();
            //default values are infinity
            foreach (int nodeID in nodesToSearch)
            {
                fScore.Add(nodeID, float.MaxValue);
            }

            // For the first node, that value is completely heuristic.
            fScore[startID] = H_startEnd(startID, endID);

            int currNode = 0; //default value
            bool pathFound = false;

            while (openSet.Count != 0)
            {
                //the node in openSet having the lowest fScore value
                currNode = openSet.OrderBy(i => fScore[i]).FirstOrDefault();
                if (currNode == endID)
                {
                    //break the loop and reconstruct path below
                    pathFound = true;
                    break;
                }

                openSet.Remove(currNode);
                closedSet.Add(currNode); //"added" to closedList

                foreach (var neighbor in gMap.Nodes[currNode].Neighbors)
                {
                    // Ignore the neighbor which is already evaluated or it is non-traversable.
                    if (!nodesToSearch.Contains(neighbor.Key) || gMap.Nodes[neighbor.Key].Type == GameMap.NodeType.Obstacle || closedSet.Contains(neighbor.Key))
                    { continue; }

                    float tentativeG = gScore[currNode] + neighbor.Value; // The distance from start to a neighbor

                    if (!openSet.Contains(neighbor.Key)) // Discover a new node
                    {
                        openSet.Add(neighbor.Key);
                    }
                    else if (tentativeG >= gScore[neighbor.Key])
                    {
                        continue; //not a better path
                    }

                    // This path is the best until now. Record it!
                    cameFrom[neighbor.Key] = currNode;
                    gScore[neighbor.Key] = tentativeG;
                    fScore[neighbor.Key] = gScore[neighbor.Key] + H_startEnd(neighbor.Key, endID);
                }
            }

            if (!pathFound)
            {
                //No solution
                res = 0;
            }
            else
            {
                res = gScore[endID];
            }
            return res;
        }

        private float H_startEnd(int startID, int endID)
        {
            return 1 * (Math.Abs(gMap.Nodes[startID].Location.X/30 - gMap.Nodes[endID].Location.X/30) + Math.Abs(gMap.Nodes[startID].Location.Y/30 - gMap.Nodes[endID].Location.Y/30));
        }
        
        private HashSet<Cluster> getHPAClusterNeighbors(Cluster c)
        {
            HashSet<Cluster> res = new HashSet<Cluster>();
            foreach (var pair in c.OuterNodes)
            {
                switch (pair.Key)
                {
                    case 'U':
                        foreach (int n in c.OuterNodes['U'])
                        {
                            //we look up grom the upper outer nodes
                            int newIndex = n - gMap.Width;

                            //if the map contains a node with this ID, we check all of the nodes one row up and
                            //determine whenever there is a path between them, and, therefore, if they are neighbors
                            if (gMap.Nodes.ContainsKey(newIndex))
                            {
                                if (gMap.Nodes[newIndex].IsTraversable() && gMap.Nodes[n].IsTraversable())
                                {
                                    //they are neighbors
                                    //get the HPAClusterParent of this node and add it to the list
                                    int parentID = gMap.Nodes[newIndex].HPAClusterParent;
                                    Cluster p = HierarchicalGraph[0].Clusters[parentID];

                                    //'p' cluster is the one *upper* to the 'c'
                                    //'c' cluster is the one *lower* to the 'p'
                                    p.AddNeighborDirection(c, 'D');
                                    c.AddNeighborDirection(p, 'U');

                                    res.Add(p);

                                    break;
                                }
                            }
                            else { break; } //no cluster exists on the up side of current cluster.
                        }
                        break;

                    case 'D':
                        foreach (int n in c.OuterNodes['D'])
                        {
                            //we look down from the lower outer nodes
                            int newIndex = n + gMap.Width;

                            //if the map contains a node with this ID, we check all of the nodes one row down and
                            //determine whenever there is a path between them, and, therefore, if they are neighbors
                            if (gMap.Nodes.ContainsKey(newIndex))
                            {
                                if (gMap.Nodes[newIndex].IsTraversable() && gMap.Nodes[n].IsTraversable())
                                {
                                    //they are neighbors
                                    //get the HPAClusterParent of this node and add it to the list
                                    int parentID = gMap.Nodes[newIndex].HPAClusterParent;
                                    Cluster p = HierarchicalGraph[0].Clusters[parentID];

                                    //'p' cluster is the one *lower* to the 'c'
                                    //'c' cluster is the one *upper* to the 'p'
                                    p.AddNeighborDirection(c, 'U');
                                    c.AddNeighborDirection(p, 'D');

                                    res.Add(p);

                                    break;
                                }
                            }
                            else { break; } //no cluster exists on the down side of current cluster.
                        }
                        break;

                    case 'L':
                        foreach (int n in c.OuterNodes['L'])
                        {
                            //we look to the left from the left outer nodes
                            int newIndex = n - 1;

                            //if the map contains a node with this ID, we check all of the nodes one row left and
                            //determine whenever there is a path between them, and, therefore, if they are neighbors
                            if (gMap.Nodes.ContainsKey(newIndex))
                            {
                                if (gMap.Nodes[newIndex].IsTraversable() && gMap.Nodes[n].IsTraversable() && ((GridMap)gMap).SameRow(n, newIndex))
                                {
                                    //they are neighbors
                                    //get the HPAClusterParent of this node and add it to the list
                                    int parentID = gMap.Nodes[newIndex].HPAClusterParent;
                                    Cluster p = HierarchicalGraph[0].Clusters[parentID];

                                    //'p' cluster is the one *more left* to the 'c'
                                    //'c' cluster is the one *more right* to the 'p'
                                    p.AddNeighborDirection(c, 'R');
                                    c.AddNeighborDirection(p, 'L');


                                    res.Add(p);

                                    break;
                                }
                            }
                            else { break; } //no cluster exists on the down side of current cluster.
                        }
                        break;

                    case 'R':
                        foreach (int n in c.OuterNodes['R'])
                        {
                            //we look to the right from the right outer nodes
                            int newIndex = n + 1;

                            //if the map contains a node with this ID, we check all of the nodes one row left and
                            //determine whenever there is a path between them, and, therefore, if they are neighbors
                            if (gMap.Nodes.ContainsKey(newIndex))
                            {
                                if (gMap.Nodes[newIndex].IsTraversable() && gMap.Nodes[n].IsTraversable() && ((GridMap)gMap).SameRow(n, newIndex))
                                {
                                    //they are neighbors
                                    //get the HPAClusterParent of this node and add it to the list
                                    int parentID = gMap.Nodes[newIndex].HPAClusterParent;
                                    Cluster p = HierarchicalGraph[0].Clusters[parentID];

                                    //'p' cluster is the one *more right* to the 'c'
                                    //'c' cluster is the one *more left* to the 'p'
                                    p.AddNeighborDirection(c, 'L');
                                    c.AddNeighborDirection(p, 'R');

                                    res.Add(p);

                                    break;
                                }
                            }
                            else { break; } //no cluster exists on the down side of current cluster.
                        }
                        break;
                }
            }
            return res;
        }
    }
}
