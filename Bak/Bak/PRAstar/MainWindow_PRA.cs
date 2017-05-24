using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Bak
{
    partial class MainWindow : Form
    {
        /// <summary>
        /// the distances of all nodes from the start, from the last PRA* (!) search performed
        /// </summary>
        Dictionary<int, float> latestGScore = new Dictionary<int, float>();
        /// <summary>
        /// determines whenever a test has already triggered for a TestCase
        /// </summary>
        bool testShouldRun = false;
        int expandedNodesCount = 0;
        TestCase selectedTest;
        Dictionary<string, List<TestCase>> MapTests = new Dictionary<string, List<TestCase>>();
        Dictionary<int, PRAbstractionLayer> PRAstarHierarchy = new Dictionary<int, PRAbstractionLayer>();

        private void BuildPRAbstractMap()
        {
            praWatch = new System.Diagnostics.Stopwatch();
            praWatch.Start();
            
            int currentAbslayerID = 0;
            PRAbstractionLayer absl = new PRAbstractionLayer(currentAbslayerID);

            //filter out all traversable nodes and sort (O(nlogn)
            var nodes = gMap.Nodes.Where(n => n.Value.Type != GameMap.NodeType.Obstacle)
                                                     .OrderBy(n => n.Key)
                                                     .ToDictionary(n => n.Key, n => n.Value);

            //this is need to be in for loops since List is a O(1) lookup for First() and Last(). 
            //Not sure on Dictionary, but probably is O(n)
            List<int> nodeKeys = nodes.Keys.ToList();

            #region 4-cliques
            //find all 4-cliques
            for (int i = nodeKeys.First(); i < nodeKeys.Last(); ++i)
            {
                if (!nodes.ContainsKey(i)) { continue; }

                PRAClusterNode c = findNeighborClique(currentAbslayerID, absl.LastAssignedClusterID, nodes, nodes[i], 4);
                if (c != null) //4clique that contains nodes[i] exists 
                {
                    //raise ID
                    absl.LastAssignedClusterID++;

                    //add clique to abstract layer
                    absl.AddClusterNode(c);

                    //delete all nodes now contained in the clusernode
                    foreach (int n in c.innerNodes)
                    {
                        nodes.Remove(n);
                    }

                    if (nodes.Count == 0)
                    { break; }
                }
            }
            #endregion

            #region 3-cliques
            //find all 3-cliques
            if (nodes.Count > 0)
            {
                for (int i = nodeKeys.First(); i < nodeKeys.Last(); ++i)
                {
                    if (!nodes.ContainsKey(i)) { continue; }

                    PRAClusterNode c = findNeighborClique(currentAbslayerID, absl.LastAssignedClusterID, nodes, nodes[i], 3);
                    if (c != null) //3clique that contains nodes[i] exists 
                    {
                        //raise ID
                        absl.LastAssignedClusterID++;
                        //add clique to abstract layer
                        absl.AddClusterNode(c);

                        //delete all nodes now contained in the clusernode
                        foreach (int n in c.innerNodes)
                        {
                            nodes.Remove(n);
                        }

                        if (nodes.Count == 0)
                        { break; }
                    }
                }
            }
            #endregion

            #region 2-cliques
            //find all 2-cliques
            if (nodes.Count > 0)
            {
                for (int i = nodeKeys.First(); i < nodeKeys.Last(); ++i)
                {
                    if (!nodes.ContainsKey(i)) { continue; }

                    PRAClusterNode c = findNeighborClique(currentAbslayerID, absl.LastAssignedClusterID, nodes, nodes[i], 2);
                    if (c != null) //2clique that contains nodes[i] exists 
                    {
                        //raise ID
                        absl.LastAssignedClusterID++;
                        //add clique to abstract layer
                        absl.AddClusterNode(c);

                        //delete all nodes now contained in the clusernode
                        foreach (int n in c.innerNodes)
                        {
                            nodes.Remove(n);
                        }

                        if (nodes.Count == 0)
                        { break; }
                    }
                }
            }
            #endregion

            #region orphan nodes
            //remaining nodes are orphans. Create remaining PRAClusterNodes from these orphans.
            foreach (var n in nodes)
            {
                PRAClusterNode c = new PRAClusterNode(currentAbslayerID, absl.LastAssignedClusterID, new List<int> { n.Key });
                c.calculateXY(gMap.Nodes);
                c.InitPRAClusterParents(gMap.Nodes);
                //raise ID
                absl.LastAssignedClusterID++;
                //add clique to abstract layer
                absl.AddClusterNode(c);

            }
            #endregion

            //add the abstract layer to abstraction hierarchy
            PRAstarHierarchy.Add(absl.ID, absl);

            #region PRAClusterNode edge creation

            //add cluster connections
            foreach (PRAClusterNode c in absl.ClusterNodes.Values)
            {
                List<PRAClusterNode> innerNodesNeighbors = getInnerNodeNeighbors(c, 0);
                foreach (PRAClusterNode p in innerNodesNeighbors)
                {
                    //add neighbors (=> create edge)
                    c.AddNeighbor(p.ID, p);
                    p.AddNeighbor(c.ID, c);
                }

            }
            #endregion

            //build additional layers until a single abstract node is left for each non-disconnected map space
            buildPRALayers();

            praWatch.Stop();
        }
        
        private void StartPRAstarSearch(object sender, DoWorkEventArgs e)
        {
            latestGScore.Clear();

            //first we check if the start and end node are in the same cluster on the highet level.
            //if they are not, there is no path between them.
            if (ClusterParent(gMap.StartNodeID, PRAstarHierarchy.Count - 1) != ClusterParent(gMap.EndNodeID, PRAstarHierarchy.Count - 1))
            {
                pathfinder.CancelAsync();
                invalidater.CancelAsync();
                return;
            }

            int layerCount = PRAstarHierarchy.Count;

            int startingLayer = layerCount / 2;

            //make an abstract path on startingLayer using A*
            List<int> abstractPath = RefinePath(startingLayer, PRAstarHierarchy[startingLayer].ClusterNodes);
            Dictionary<int, PRAClusterNode> nodesToSearch = GetOneLevelLowerClusterNodes(abstractPath, startingLayer);

            for (int level = startingLayer - 1; level >= 0; level--)
            {
                //refine path to lower levels
                abstractPath = RefinePath(level, nodesToSearch);

                if (level > 0)
                {
                    nodesToSearch = GetOneLevelLowerClusterNodes(abstractPath, level);
                }
            }

            //goes through the low-level clusters and partially builds a path on the grid base.
            //abstractPath contains the indices of low-level clusters. Therefore,
            //by doing PRAstarHierarchy[0].ClusterNodes[i] we get the cluster we need
            int startID = gMap.StartNodeID;
            for (int i = abstractPath.Count - 1; i > 0; --i)
            {
                List<int> partialPath = new List<int>();

                partialPath = AstarPRALowestLevelSearch(PRAstarHierarchy[0].ClusterNodes[abstractPath[i]],
                                                     PRAstarHierarchy[0].ClusterNodes[abstractPath[i - 1]],
                                                     startID);

                AddToPathfSol(partialPath);

                if (i == abstractPath.Count - 1)
                {
                    StartAgent();
                }

                //the next start node is going to be the end node of this search. 
                //since A* path returned is reversed, the first element was the last (end) node.
                startID = partialPath[0];
            }
            pfStopWatch.Stop();

            pathfinder.CancelAsync();

        }

        private List<int> RefinePath(int startingLayer, Dictionary<int, PRAClusterNode> nodesToSearch)
        {
            List<int> path = new List<int>();

            PRAClusterNode startnodeCluster = ClusterParent(gMap.StartNodeID, startingLayer);
            PRAClusterNode endnodeCluster = ClusterParent(gMap.EndNodeID, startingLayer);

            path = AstarAbstractionSearch(startnodeCluster, endnodeCluster, PRAstarHierarchy[startingLayer], nodesToSearch);

            return path;

        }

        /// <summary>
        /// This runs A* assuming that both start and nextDestination 
        /// clusters are clusters belonging to the lowest level of abstraction.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="nextDestination"></param>
        /// <returns></returns>
        private List<int> AstarPRALowestLevelSearch(PRAClusterNode start, PRAClusterNode nextDestination, int startPosition)
        {
            bool isFinalCluster = false;
            if (nextDestination.innerNodes.Contains(gMap.EndNodeID))
            {
                isFinalCluster = true;
            }

            List<int> nodesToSearch = new List<int>();
            nodesToSearch.AddRange(start.innerNodes);
            nodesToSearch.AddRange(nextDestination.innerNodes);
            List<int> sol = new List<int>();

            Dictionary<int, bool> closedSet = new Dictionary<int, bool>();
            foreach (int id in nodesToSearch)
            {
                closedSet.Add(id, false);
            }
            Dictionary<int, Node> openSet = new Dictionary<int, Node>();

            //starting node is in the open set
            openSet.Add(startPosition, gMap.Nodes[startPosition]);

            // For each clusternode, which clusternode it can most efficiently be reached from.
            // If a cnode can be reached from many cnodes, cameFrom will eventually contain the
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
            gScore[start.ID] = 0;

            // For each node, the total cost of getting from the start node to the goal
            // by passing by that node. That value is partly known, partly heuristic.
            Dictionary<int, float> fScore = new Dictionary<int, float>();
            //default values are infinity
            foreach (int nodeID in nodesToSearch)
            {
                fScore.Add(nodeID, float.MaxValue);
            }

            // For the first node, that value is completely heuristic.
            fScore[startPosition] = H_lowPRA(startPosition, nextDestination);

            int currNode = 0; //default value

            while (openSet.Count != 0)
            {
                //the node in openSet having the lowest fScore value
                currNode = openSet.OrderBy(i => fScore[i.Key]).FirstOrDefault().Key;
                if ((nextDestination.innerNodes.Contains(currNode) && !isFinalCluster) || (currNode == gMap.EndNodeID))
                {
                    //break the loop and reconstruct path below
                    break;
                }

                openSet.Remove(currNode);
                closedSet[currNode] = true; //"added" to closedList

                foreach (var neighbor in gMap.Nodes[currNode].Neighbors)
                {
                    // Ignore the neighbor which is already evaluated or a neighbor 
                    //that doesn't belong to neither start nor nextDestination cluster.
                    if ((!start.innerNodes.Contains(neighbor.Key) && !nextDestination.innerNodes.Contains(neighbor.Key))
                        || closedSet[neighbor.Key] == true)
                    { continue; }

                    // The distance from start to a neighbor
                    float tentativeG = gScore[currNode] + neighbor.Value;
                    if (!openSet.ContainsKey(neighbor.Key)) // Discover a new node
                    {
                        searchedNodes.Add(neighbor.Key);
                        expandedNodesCount++;
                        setSearchedBgColor(neighbor.Key);
                        openSet.Add(neighbor.Key, gMap.Nodes[neighbor.Key]);
                    }
                    else if (tentativeG >= gScore[neighbor.Key])
                    {
                        continue; //not a better path
                    }

                    // This path is the best until now. Record it!
                    cameFrom[neighbor.Key] = currNode;
                    gScore[neighbor.Key] = tentativeG;
                    fScore[neighbor.Key] = gScore[neighbor.Key] + H_lowPRA(neighbor.Key, nextDestination);
                }
            }

            sol.Add(currNode);
            while (cameFrom.ContainsKey(currNode))
            {
                int child = currNode;
                currNode = cameFrom[currNode];

                pathCost += gMap.Nodes[currNode].Neighbors[child];
                latestGScore.Add(currNode, pathCost);

                sol.Add(currNode);
            }
            // pathfinder.CancelAsync();

            return sol;
        }

        private List<int> AstarAbstractionSearch(PRAClusterNode start, PRAClusterNode end, PRAbstractionLayer layer, Dictionary<int, PRAClusterNode> nodesToSearch)
        {
            List<int> sol = new List<int>();

            Dictionary<int, bool> closedSet = new Dictionary<int, bool>();
            foreach (int id in nodesToSearch.Keys)
            {
                closedSet.Add(id, false);
            }
            Dictionary<int, PRAClusterNode> openSet = new Dictionary<int, PRAClusterNode>();

            //starting node is in the open set
            openSet.Add(start.ID, layer.ClusterNodes[start.ID]);

            // For each clusternode, which clusternode it can most efficiently be reached from.
            // If a cnode can be reached from many cnodes, cameFrom will eventually contain the
            // most efficient previous step.
            Dictionary<int, int> cameFrom = new Dictionary<int, int>();

            // For each node, the cost of getting from the start node to that node.
            Dictionary<int, float> gScore = new Dictionary<int, float>();
            //default values are infinity
            foreach (int nodeID in nodesToSearch.Keys)
            {
                gScore.Add(nodeID, float.MaxValue);
            }
            // The cost of going from start to start is zero.
            gScore[start.ID] = 0;

            // For each node, the total cost of getting from the start node to the goal
            // by passing by that node. That value is partly known, partly heuristic.
            Dictionary<int, float> fScore = new Dictionary<int, float>();
            //default values are infinity
            foreach (int nodeID in nodesToSearch.Keys)
            {
                fScore.Add(nodeID, float.MaxValue);
            }

            // For the first node, that value is completely heuristic.
            fScore[start.ID] = H_PRA(start.ID, layer, end);

            int currNode = 0; //default value

            while (openSet.Count != 0)
            {
                //the node in openSet having the lowest fScore value
                currNode = openSet.OrderBy(i => fScore[i.Key]).FirstOrDefault().Key;
                if (currNode == end.ID)
                {
                    //break the loop and reconstruct path below
                    break;
                }

                openSet.Remove(currNode);
                closedSet[currNode] = true; //"added" to closedList

                foreach (var neighbor in nodesToSearch[currNode].neighbors)
                {
                    // Ignore the neighbor which is already evaluated.
                    if (!closedSet.ContainsKey(neighbor.Key) || closedSet[neighbor.Key] == true)
                    { continue; }

                    // The distance from start to a neighbor
                    float tentativeG = gScore[currNode] + nodesToSearch[currNode].neighborDist[neighbor.Key];
                    if (!openSet.ContainsKey(neighbor.Key)) // Discover a new node
                    {
                        searchedNodes.Add(neighbor.Key);
                        expandedNodesCount++;
                        openSet.Add(neighbor.Key, layer.ClusterNodes[neighbor.Key]);
                    }
                    else if (tentativeG >= gScore[neighbor.Key])
                    {
                        continue; //not a better path
                    }

                    // This path is the best until now. Record it!
                    cameFrom[neighbor.Key] = currNode;
                    gScore[neighbor.Key] = tentativeG;
                    fScore[neighbor.Key] = gScore[neighbor.Key] + H_PRA(neighbor.Key, layer, end);
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

        private Dictionary<int, PRAClusterNode> GetOneLevelLowerClusterNodes(List<int> clusterIDs, int currLayerID)
        {
            Dictionary<int, PRAClusterNode> res = new Dictionary<int, PRAClusterNode>();

            foreach (int cID in clusterIDs)
            {
                foreach (int id in PRAstarHierarchy[currLayerID].ClusterNodes[cID].innerNodes)
                {
                    //Add the corresponding cluster node one layer DOWN from the current one.
                    res.Add(id, PRAstarHierarchy[currLayerID - 1].ClusterNodes[id]);
                }
            }
            return res;
        }


        private void checkPRAClusters(Node node, bool rebuildAll = true)
        {
            if (PRAstarHierarchy.Count == 0)
            {
                MessageBox.Show("PRA clusters are not generated. Please generate the abstraction hierarchy.");
                return;
            }

            //if only an end point or start point were changed, 
            //we only need to change their PRAClusterParent (PRC) property.
            //O(n)
            if (node.Type == GameMap.NodeType.EndPosition || node.Type == GameMap.NodeType.StartPosition)
            {
                foreach (PRAClusterNode n in PRAstarHierarchy[0].ClusterNodes.Values)
                {
                    if (n.innerNodes.Contains(node.ID))
                    {
                        node.PRAClusterParent = n.ID;
                        return;
                    }
                }
            }
            else if (node.Type == GameMap.NodeType.Traversable)
            {
                #region blah blah
                //check their (traversable) neighbors. Cases are following:
                //1) if there is an orphan there, we can trivially assign the node to this cluster.
                //   set PRCParent, set neighbors (all neighboring clusters of node must have n as their neighbor cluster
                //   and n must have all the neighbor clusters as neighbors. This is not trivial if we are
                //   joining clusters that were disconnected before the change.
                //   This implies: if all the neighbors belong to the same cluster on the highest level of abstraction, we
                //   need not do any further action. If they were disconnected however, recompute all higher abstraction layers
                //   except the base one (since that we changed correctly in this function).
                //2) if there is a 3-clique or 2-clique, we check if adding this node would make it a valid n+1 clique.
                //   If yes, assign, change PRCParent / neighbors / coords. Above Implication remains true for this as well. 
                //3) If not, we create a new cluster (orphan) node and connect it
                //   to all neighboring clusters. We then recompute all higher levels.
                //4) if there are no traversable neighbors of the node, we create a new orphan node
                //   and create an orphan, disconnected node on each layer of abstraction until the last one. 
                //   No recomputing needed.
                #endregion

                var neighborClusters = GetCliqueOfNeighbors(node);
                //4) -> if there are no neighbors, this will be a new (orphan) node on all levels

                #region disconnected node
                if (neighborClusters.Count == 0)
                {
                    int level = 0;
                    int prevClusterID = node.ID;

                    while (level <= PRAstarHierarchy.Count - 1)
                    {
                        PRAbstractionLayer l = PRAstarHierarchy[level];
                        PRAClusterNode c = new PRAClusterNode(level, l.LastAssignedClusterID, new List<int> { prevClusterID });
                        l.LastAssignedClusterID++;//RAISE ID!!!

                        c.calculateXY(gMap.Nodes);
                        l.AddClusterNode(c);

                        if (level == 0) { node.PRAClusterParent = c.ID; }
                        else
                        {
                            PRAClusterNode prev = PRAstarHierarchy[level - 1].ClusterNodes[prevClusterID];
                            prev.PRAClusterParent = c.ID;
                        }
                        prevClusterID = c.ID;

                        level++;
                    }

                    return;
                }
                #endregion

                //1) -> check for orphan(s)

                #region connect to existing orphan

                var orphans = neighborClusters.Where(c => c.innerNodes.Count == 1).ToList();
                if (orphans.Count > 0)
                {
                    PRAClusterNode c = orphans[0]; //pick an orphan. doesn't matter which one really.
                    c.innerNodes.Add(node.ID);
                    c.calculateXY(gMap.Nodes); //recalculate center X/Y points

                    //create edges between the clusters, if they do not exist
                    c.AddNeighbors(neighborClusters);
                    c.recalculateNeighborsHDist();

                    foreach (var n in neighborClusters)
                    {
                        n.AddNeighbor(c.ID, c);
                        n.recalculateNeighborsHDist();
                    }

                    //set the PRACLusterParent
                    node.PRAClusterParent = c.ID;

                    //recalculate all higher abstraction layers
                    if (rebuildAll)
                    {
                        recalculatePRALayers();
                    }

                    return;

                }
                #endregion

                //2) -> check for 2-cliques and 3-cliques

                #region connect to existing clique

                var cliques = neighborClusters.Where(c => c.innerNodes.Count > 1 && c.innerNodes.Count < 4).ToList();
                foreach (var c in cliques)
                {
                    //check if adding node to this clique would make it a valid n+1 clique
                    if (wouldBeValidClique(node.ID, c.innerNodes))
                    {
                        c.innerNodes.Add(node.ID);
                        c.calculateXY(gMap.Nodes); //recalculate center X/Y points

                        //create edges between the clusters, if they do not exist
                        c.AddNeighbors(neighborClusters);
                        c.recalculateNeighborsHDist();

                        foreach (var n in neighborClusters)
                        {
                            n.AddNeighbor(c.ID, c);
                            n.recalculateNeighborsHDist();
                        }

                        //set the PRACLusterParent
                        node.PRAClusterParent = c.ID;

                        //recalculate all higher abstraction layers
                        if (rebuildAll)
                        {
                            recalculatePRALayers();
                        }
                        return;
                    }
                }

                #endregion

                //3) -> all previous actions failed, so we proceed to create a new node and connect it.

                #region new orphan node

                PRAClusterNode newNode = new PRAClusterNode(0, PRAstarHierarchy[0].LastAssignedClusterID, new List<int>() { node.ID });
                newNode.calculateXY(gMap.Nodes); //recalculate center X/Y points

                //RAISE THE ID!!!!
                PRAstarHierarchy[0].LastAssignedClusterID++;

                //create edges between the clusters, if they do not exist
                newNode.AddNeighbors(neighborClusters);
                newNode.recalculateNeighborsHDist();

                foreach (var n in neighborClusters)
                {
                    n.AddNeighbor(newNode.ID, newNode);
                    n.recalculateNeighborsHDist();
                }

                //set the PRACLusterParent
                node.PRAClusterParent = newNode.ID;

                //add node to layer
                PRAstarHierarchy[0].AddClusterNode(newNode);

                //recalculate all higher abstraction layers
                if (rebuildAll)
                {
                    recalculatePRALayers();
                }

                return;

                #endregion

            }
            else if (node.Type == GameMap.NodeType.Obstacle)
            {
                #region blah blah
                //check their (traversable) neighbors. Cases are following:
                //1) if a clique was a 4-clique, trivially the clique stays valid.
                //2) if a clique was a 3-clique, trivially the clique stays valid (since diagonal movement is valid as long as two nodes are
                //   traversable, their neighbors do not matter)
                //3) if a clique was a 2-clique, trivially the clique becomes an orphan.
                //4) if the clique was an orphan, the node is deleted. We remove the node form all previous neighbors
                // In all cases, we check all the cluster's neighbors if they still nemain neighbors after this change.
                //then proceed to recompute higher levels.
                #endregion

                PRAClusterNode changedCluster = PRAstarHierarchy[0].ClusterNodes[node.PRAClusterParent];

                //now the node is non-traversable, therefore won't belong to a cluster
                node.PRAClusterParent = -1;

                //orphan
                if (changedCluster.innerNodes.Count == 1)
                {
                    //this node is going to be deleted, so we remove it from its neighbors
                    foreach (var n in changedCluster.neighbors.Values)
                    {
                        n.neighbors.Remove(changedCluster.ID);
                        n.neighborDist.Remove(changedCluster.ID);
                    }

                    //we delete the node itself
                    PRAstarHierarchy[0].ClusterNodes.Remove(changedCluster.ID);

                    //recalculate all higher abstraction layers
                    if (rebuildAll)
                    {
                        recalculatePRALayers();
                    }

                    return;

                }
                //not an orphan
                else
                {
                    changedCluster.innerNodes.Remove(node.ID);
                    changedCluster.calculateXY(gMap.Nodes);

                    var currNeighbors = getInnerNodeNeighbors(changedCluster, 0);
                    List<PRAClusterNode> cutOffNeighbors = changedCluster.neighbors.Values.Except(currNeighbors).ToList();

                    //No connections were cut off -> therefore no abstraction rebuilding is necessary
                    if (cutOffNeighbors.Count() == 0)
                    {
                        return;
                    }
                    else
                    {
                        //delet all connections that were cut off
                        foreach (var n in cutOffNeighbors)
                        {
                            changedCluster.neighborDist.Remove(n.ID);
                            changedCluster.neighbors.Remove(n.ID);

                            n.neighbors.Remove(changedCluster.ID);
                            n.neighborDist.Remove(changedCluster.ID);
                        }

                        //recalculate all higher abstraction layers
                        if (rebuildAll)
                        {
                            recalculatePRALayers();
                        }
                        return;
                    }
                }
            }
        }

        private List<PRAClusterNode> GetCliqueOfNeighbors(Node node)
        {
            List<PRAClusterNode> clusterNeighbors = new List<PRAClusterNode>();

            List<int> gridNeighbors = node.Neighbors.Where(n => gMap.Nodes[n.Key].IsTraversable()).Select(n => n.Key).ToList();
            foreach (int gNodeID in gridNeighbors)
            {
                int cNodeID = gMap.Nodes[gNodeID].PRAClusterParent;
                PRAClusterNode prcn = PRAstarHierarchy[0].ClusterNodes[cNodeID];
                if (!clusterNeighbors.Contains(prcn))
                {
                    clusterNeighbors.Add(prcn);
                }
            }
            return clusterNeighbors;
        }

        private List<PRAClusterNode> getInnerNodeNeighbors(PRAClusterNode c, int layer)
        {
            List<PRAClusterNode> res = new List<PRAClusterNode>();
            foreach (var n in c.innerNodes)
            {
                if (layer == 0)
                {
                    foreach (var n2 in gMap.Nodes[n].Neighbors.Keys)
                    {
                        Node neigh = gMap.Nodes[n2];
                        if (neigh.IsTraversable() && !c.innerNodes.Contains(n2))
                        {
                            //get the PRAClusterParent of this node and add it to the list
                            int parentID = neigh.PRAClusterParent;
                            PRAClusterNode p = PRAstarHierarchy[0].ClusterNodes[parentID];
                            if (!res.Contains(p))
                            {
                                res.Add(p);
                            }
                        }
                    }
                }
                else
                {
                    PRAbstractionLayer prev = PRAstarHierarchy[layer - 1];
                    foreach (var n2 in prev.ClusterNodes[n].neighbors)
                    {
                        //get the PRAClusterParent of this node and add it to the list
                        int parentID = n2.Value.PRAClusterParent;
                        PRAClusterNode p = PRAstarHierarchy[layer].ClusterNodes[parentID];
                        if (!res.Contains(p))
                        {
                            res.Add(p);
                        }
                    }
                }
            }
            return res;
        }

        private void recalculatePRALayers()
        {
            //remember the first
            PRAbstractionLayer baseL = PRAstarHierarchy[0];

            PRAstarHierarchy.Clear();
            PRAstarHierarchy.Add(baseL.ID, baseL);

            //rebuild anew
            buildPRALayers();
        }


        private void buildPRALayers()
        {
            //at the start, there is only the first abstraction layer. 
            //This layer consists of nodes, where a node can contain a 4-clique, 3-clique, 2-clique or be an orphan node.
            //We continue building layers and abstracting until there is only a single node left.

            int currAbslayerID = 1;

            while (true)
            {
                PRAbstractionLayer currentLevel = new PRAbstractionLayer(currAbslayerID);
                PRAbstractionLayer oneLevelLower = PRAstarHierarchy[currAbslayerID - 1];

                Dictionary<int, PRAClusterNode> resolvedNodes = new Dictionary<int, PRAClusterNode>();

                //extract the nodes of the previous layer and sort them by their neighbor count
                Dictionary<int, PRAClusterNode> nodes = oneLevelLower.ClusterNodes.OrderBy(n => n.Value.neighbors.Count)
                                                                     .ToDictionary(n => n.Key, n => n.Value);

                //a PRAClusterNode *p* is in clique with other PCNs if each of (or a subset of)
                //the neighbors  of *p* contains all other (or the same subset o ) neighbors and also *p*
                //Example: PCNs 0,2 and 4 are in a clique if  [0] -> [2][4] && [2] -> [0][4] && [4] -> [0][2]
                //In the following configuration also i.e {0, 2} are in a clique because [0] -> [2]... && [2] -> [0]... 

                #region clique building
                //building 4-cliques, 3-cliques, 2-cliques
                foreach (var node in nodes)
                {
                    if (resolvedNodes.ContainsKey(node.Key)) { continue; }

                    //be sure to only make combinations that contains keys that have NOT been resolved already.
                    List<int> nonResovledNeighbors = node.Value.neighbors.Keys.Where(k => !resolvedNodes.ContainsKey(k)).ToList();

                    List<List<int>> nodesToCheckForCliques = GetCombinations(nonResovledNeighbors);
                    nodesToCheckForCliques = nodesToCheckForCliques.OrderByDescending(n => n.Count).ToList();

                    for (int i = 0; i < nodesToCheckForCliques.Count; ++i)
                    {
                        //check for neighbors if they are a clique with node
                        if (nodesAreClique(oneLevelLower, node.Value, nodesToCheckForCliques[i]))
                        {
                            //create PCN
                            List<int> innerNodes = new List<int> { node.Key };
                            innerNodes.AddRange(nodesToCheckForCliques[i]);
                            PRAClusterNode c = new PRAClusterNode(currAbslayerID, currentLevel.LastAssignedClusterID, innerNodes);
                            c.calculateXY(oneLevelLower);
                            c.setParentToAllInnerNodes(oneLevelLower);

                            //raise ID
                            currentLevel.LastAssignedClusterID++;
                            //add clique to abstract layer
                            currentLevel.AddClusterNode(c);

                            //mark all nodes now contained in the clusernode as resolved
                            foreach (int n in c.innerNodes)
                            {
                                resolvedNodes.Add(n, oneLevelLower.ClusterNodes[n]);
                            }
                            //stop checking cliques for node
                            break;
                        }
                    }
                }
                #endregion

                //building PCNs for this layer is finished. 
                //Remove all resovled nodes from nodes. The remaining nodes will be orphans.
                foreach (var n in resolvedNodes)
                {
                    nodes.Remove(n.Key);
                }

                #region orphans
                //create orphan PCNs 
                foreach (var n in nodes)
                {
                    PRAClusterNode c = new PRAClusterNode(currAbslayerID, currentLevel.LastAssignedClusterID, new List<int> { n.Key });
                    c.calculateXY(oneLevelLower);
                    c.setParentToAllInnerNodes(oneLevelLower);

                    //raise ID
                    currentLevel.LastAssignedClusterID++;
                    //add clique to abstract layer
                    currentLevel.AddClusterNode(c);
                }
                #endregion

                PRAstarHierarchy.Add(currAbslayerID, currentLevel);

                #region PRAClusterNode edge creation

                //add cluster connections
                foreach (PRAClusterNode c in currentLevel.ClusterNodes.Values)
                {
                    List<PRAClusterNode> innerNodesNeighbors = getInnerNodeNeighbors(c, currAbslayerID);
                    foreach (PRAClusterNode p in innerNodesNeighbors)
                    {
                        //add neighbors (=> create edge)
                        c.AddNeighbor(p.ID, p);
                        p.AddNeighbor(c.ID, c);
                    }
                }
                #endregion


                //if the map has properties such as multiple non-reachable areas that 
                //are already as abstracted as possible, break the while loop
                if (currentLevel.AllCLustersDisconnected())
                {
                    break;
                }

                //building PCN connection and, therefore, this abstraction layer.
                //check if this abstraction layer contains only a single node.
                //if it does, finish. if it doesn't, raise the currentAbslayerID and loop.
                if (currentLevel.ClusterNodes.Count == 1)
                {
                    break;
                }
                else
                {
                    currAbslayerID++;
                }
            }
        }

        /// <summary>
        /// Determines whenever the nodes are in a clique with node
        /// </summary>
        /// <param name="node"></param>
        /// <param name="nodes"></param>
        /// <returns></returns>
        private bool nodesAreClique(PRAbstractionLayer layer, PRAClusterNode node, List<int> nodes)
        {
            //check for 2-clique
            if (nodes.Count == 1)
            {
                // if [0] -> [2]... then also [2] -> [0]... must be true; Then they are a 2-clique
                if (node.HasNeighbor(nodes[0]) && layer.ClusterNodes[nodes[0]].HasNeighbor(node.ID))
                {
                    return true;
                }
                else { return false; }
            }

            //check for 3-clique and 4-clique
            else //nodes are of size 2 to 3
            {
                //Example: nodes 0,2 and 4 are in a clique if  [0] -> {2,4} && [2] -> {0, 4} && [4] -> {0, 2}
                foreach (var n in nodes)
                {
                    //according to prev. example, let's say node is 0 and n is 2.
                    //the aim for neighborsToCheckis to be {4, 0} and then evaluate if n has both 4 and 0 as neighbors.
                    //if it does, in the same way we check if {0, 2} are neighbors of 4. If they are, {0, 2, 4} are a clique.
                    List<int> neighborsToCheck = nodes.Where(n1 => n1 != n)
                                                      .Select(n1 => n1).ToList();

                    neighborsToCheck.Add(node.ID);
                    if (!layer.ClusterNodes[n].HasAllNeighbors(neighborsToCheck))
                    {
                        return false;
                    }
                }
                return true;
            }

        }

        private PRAClusterNode findNeighborClique(int absLayerID, int clusterNodeId, Dictionary<int, Node> nodes, Node n, int cliqueSize)
        {
            PRAClusterNode res = null;

            switch (cliqueSize)
            {
                case 4:
                    //lower-right 4square
                    if (nonClusteredNodes(nodes, n.ID + gMap.Width, n.ID + gMap.Width + 1, n.ID + 1)
                        && n.AreNeighbors(n.ID + gMap.Width, n.ID + gMap.Width + 1, n.ID + 1)
                        && AreTraversable(n.ID + gMap.Width, n.ID + gMap.Width + 1, n.ID + 1))
                    {
                        res = new PRAClusterNode(absLayerID, clusterNodeId, new List<int> { n.ID, n.ID + gMap.Width, n.ID + gMap.Width + 1, n.ID + 1 });
                        res.calculateXY(gMap.Nodes);
                        res.InitPRAClusterParents(gMap.Nodes);
                    }

                    break;

                case 3:
                    //lower-right 3 (horizontal/vertical)
                    if (nonClusteredNodes(nodes, n.ID + gMap.Width, n.ID + 1)
                        && n.AreNeighbors(n.ID + gMap.Width, n.ID + 1)
                        && AreTraversable(n.ID + gMap.Width, n.ID + 1))
                    {
                        res = new PRAClusterNode(absLayerID, clusterNodeId, new List<int> { n.ID, n.ID + gMap.Width, n.ID + 1 });
                        res.calculateXY(gMap.Nodes);
                        res.InitPRAClusterParents(gMap.Nodes);
                    }

                    //lower-right 3 (horizontal and diagonal)
                    else if (nonClusteredNodes(nodes, n.ID + gMap.Width + 1, n.ID + 1)
                        && n.AreNeighbors(n.ID + gMap.Width + 1, n.ID + 1)
                        && AreTraversable(n.ID + gMap.Width + 1, n.ID + 1))
                    {
                        res = new PRAClusterNode(absLayerID, clusterNodeId, new List<int> { n.ID, n.ID + gMap.Width + 1, n.ID + 1 });
                        res.calculateXY(gMap.Nodes);
                        res.InitPRAClusterParents(gMap.Nodes);
                    }

                    //lower-right 3 (vertical and diagonal)
                    else if (nonClusteredNodes(nodes, n.ID + gMap.Width, n.ID + 1)
                        && n.AreNeighbors(n.ID + gMap.Width, n.ID + 1)
                        && AreTraversable(n.ID + gMap.Width, n.ID + 1))
                    {
                        res = new PRAClusterNode(absLayerID, clusterNodeId, new List<int> { n.ID, n.ID + gMap.Width, n.ID + 1 });
                        res.calculateXY(gMap.Nodes);
                        res.InitPRAClusterParents(gMap.Nodes);
                    }
                    break;

                case 2:
                    //lower-right horizontal
                    if (nonClusteredNodes(nodes, n.ID + 1)
                        && n.AreNeighbors(n.ID + 1)
                        && AreTraversable(n.ID + 1))
                    {
                        res = new PRAClusterNode(absLayerID, clusterNodeId, new List<int> { n.ID, n.ID + 1 });
                        res.calculateXY(gMap.Nodes);
                        res.InitPRAClusterParents(gMap.Nodes);
                    }

                    //lower-right vertical
                    if (nonClusteredNodes(nodes, n.ID + gMap.Width)
                        && n.AreNeighbors(n.ID + gMap.Width)
                        && AreTraversable(n.ID + gMap.Width))
                    {
                        res = new PRAClusterNode(absLayerID, clusterNodeId, new List<int> { n.ID, n.ID + gMap.Width });
                        res.calculateXY(gMap.Nodes);
                        res.InitPRAClusterParents(gMap.Nodes);
                    }

                    //lower-right diagonal
                    if (nonClusteredNodes(nodes, n.ID + gMap.Width + 1)
                        && n.AreNeighbors(n.ID + gMap.Width + 1)
                        && AreTraversable(n.ID + gMap.Width + 1))
                    {
                        res = new PRAClusterNode(absLayerID, clusterNodeId, new List<int> { n.ID, n.ID + gMap.Width + 1 });
                        res.calculateXY(gMap.Nodes);
                        res.InitPRAClusterParents(gMap.Nodes);
                    }

                    break;
            }
            return res;
        }

        private void StartPRAstartOnNew()
        {
            pathfinder = new BackgroundWorker();
            pathfinder.WorkerSupportsCancellation = true;

            invalidater = new BackgroundWorker();
            invalidater.WorkerSupportsCancellation = true;

            pathfinder.DoWork += StartPRAstarSearch;
            pathfinder.RunWorkerCompleted += PathfinderTest_RunWorkerCompleted;
            pathfinder.RunWorkerAsync();

            invalidater.DoWork += Invalidater_DoWork;
            invalidater.RunWorkerAsync();
        }

        private void PathfinderTest_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            EnablePathFindingdControls();

            pfStopWatch.Stop();
            ThreadHelperClass.SetText(this, tb_elapsedTime, pfStopWatch.Elapsed.TotalMilliseconds.ToString());

            printSolution();
            StartAgent();

            //reset the start node to its previous position

            //change the current start pos to traversable node
            gMap.Nodes[gMap.StartNodeID].Type = GameMap.NodeType.Traversable;
            gMap.Nodes[gMap.StartNodeID].BackColor = ColorPalette.NodeColor_Traversable;

            //set the old start node
            gMap.StartNodeID = selectedTest.startPos;
            gMap.Nodes[gMap.StartNodeID].Type = GameMap.NodeType.StartPosition;
            gMap.Nodes[gMap.StartNodeID].BackColor = ColorPalette.NodeColor_Start;
        }
        
        #region Heuristics

        /// <summary>
        /// Calculates the distance from a low-level node to an estimated next cluster
        /// </summary>
        /// <param name="n"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        private float H_lowPRA(int n, PRAClusterNode end)
        {
            switch (heuristic)
            {
                case Heuristic.Manhattan:
                    return 1 * (Math.Abs(gMap.Nodes[n].Location.X/30 - end.X/30) + Math.Abs(gMap.Nodes[n].Location.Y/30 - end.Y/30));

                case Heuristic.DiagonalShortcut:
                    float h = 0;
                    int dx = Math.Abs(gMap.Nodes[n].Location.X/30 - end.X/30);
                    int dy = Math.Abs(gMap.Nodes[n].Location.Y/30 - end.Y/30);
                    if (dx > dy)
                        h = 1.4f * dy + 1 * (dx - dy);
                    else
                        h = 1.4f * dx + 1 * (dy - dx);
                    return h;

                default:
                    return 1 * (Math.Abs(gMap.Nodes[n].Location.X - end.X) + Math.Abs(gMap.Nodes[n].Location.Y - end.Y));

            }
        }

        /// <summary>
        /// Heuristic function for PRA* abstract search
        /// </summary>
        /// <param name="n"></param>
        /// <param name="layer"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        private float H_PRA(int n, PRAbstractionLayer layer, PRAClusterNode end)
        {
            switch (heuristic)
            {
                case Heuristic.Manhattan:
                    return 1 * (Math.Abs(layer.ClusterNodes[n].X - end.X) + Math.Abs(layer.ClusterNodes[n].Y - end.Y));

                case Heuristic.DiagonalShortcut:
                    float h = 0;
                    int dx = Math.Abs(layer.ClusterNodes[n].X - end.X);
                    int dy = Math.Abs(layer.ClusterNodes[n].Y - end.Y);
                    if (dx > dy)
                        h = 1.4f * dy + 1 * (dx - dy);
                    else
                        h = 1.4f * dx + 1 * (dy - dx);
                    return h;

                default:
                    return 1 * (Math.Abs(layer.ClusterNodes[n].X - end.X) + Math.Abs(layer.ClusterNodes[n].Y - end.Y));

            }
        }

        #endregion
        
        #region TMP PRA* LAYER BUILD

        private void StartTestExecution()
        {
            testRunner = new System.Threading.Timer(TriggerTest, null, 0, Timeout.Infinite);
        }
        private void TriggerTest(object sender)
        {
            foreach (var swap in selectedTest.nodeSwaps)
            {
                //get the node corresponding to id, set new node state by invoking click
                Node n = gMap.Nodes[swap.Key];
                gMap.EditingNodeMode = swap.Value;

                //ONLY invoke if node was non-traversable and now should be traversable and vice versa.
                //is buggy if trying to make a (non)traversable node from a (previously as well) (non)traversable node
                if (n.Type != swap.Value)
                {
                    n.Node_Click();
                    //check ONLY the base cluster, withouth rebuilding the whole abstraction
                    checkPRAClusters(n, false);
                }
            }

            //after applying all swaps, now rebuild the abstraction to a temporary structure
            Dictionary<int, PRAbstractionLayer> tmpLayers = new Dictionary<int, PRAbstractionLayer>();
            tmpLayers.Add(PRAstarHierarchy[0].ID, PRAstarHierarchy[0]);

            tmpLayers = buildTemporaryPRALayers(tmpLayers);

            //after the computation is over, we replace the old structure with the new one 
            PRAstarHierarchy = tmpLayers;
            //STOP the agent's thread/timer
            agentTerminate = true;

            //set a new start point  which is the current agent position
            Node agentPos = gMap.Nodes[PathfindingSolution[agentPosition]];
            gMap.StartNodeID = agentPos.ID;

            //remember the path cost from the start to this point. We should be able to get this from
            //the latestGScore, since that was updated on the latest PRA* search.
            pathCost = latestGScore[agentPos.ID];
            //the path count from start to agent position before the test trigger
            testPCount = agentPosition;

            //we prevent the test from looping since it already triggered once
            testShouldRun = false;

            PathfindingSolution.Clear();
            StartPRAstartOnNew();
        }

        #region tmp cluster building
        private Dictionary<int, PRAbstractionLayer> buildTemporaryPRALayers(Dictionary<int, PRAbstractionLayer> structure)
        {
            //at the start, there is only the first abstraction layer. 
            //This layer consists of nodes, where a node can contain a 4-clique, 3-clique, 2-clique or be an orphan node.
            //We continue building layers and abstracting until there is only a single node left.

            int currAbslayerID = 1;

            while (true)
            {
                PRAbstractionLayer currentLevel = new PRAbstractionLayer(currAbslayerID);
                PRAbstractionLayer oneLevelLower = structure[currAbslayerID - 1];

                Dictionary<int, PRAClusterNode> resolvedNodes = new Dictionary<int, PRAClusterNode>();

                //extract the nodes of the previous layer and sort them by their neighbor count
                Dictionary<int, PRAClusterNode> nodes = oneLevelLower.ClusterNodes.OrderBy(n => n.Value.neighbors.Count)
                                                                     .ToDictionary(n => n.Key, n => n.Value);

                //a PRAClusterNode *p* is in clique with other PCNs if each of (or a subset of)
                //the neighbors  of *p* contains all other (or the same subset o ) neighbors and also *p*
                //Example: PCNs 0,2 and 4 are in a clique if  [0] -> [2][4] && [2] -> [0][4] && [4] -> [0][2]
                //In the following configuration also i.e {0, 2} are in a clique because [0] -> [2]... && [2] -> [0]... 

                #region clique building
                //building 4-cliques, 3-cliques, 2-cliques
                foreach (var node in nodes)
                {
                    if (resolvedNodes.ContainsKey(node.Key)) { continue; }

                    //be sure to only make combinations that contains keys that have NOT been resolved already.
                    List<int> nonResovledNeighbors = node.Value.neighbors.Keys.Where(k => !resolvedNodes.ContainsKey(k)).ToList();

                    List<List<int>> nodesToCheckForCliques = GetCombinations(nonResovledNeighbors);
                    nodesToCheckForCliques = nodesToCheckForCliques.OrderByDescending(n => n.Count).ToList();

                    for (int i = 0; i < nodesToCheckForCliques.Count; ++i)
                    {
                        //check for neighbors if they are a clique with node
                        if (nodesAreClique(oneLevelLower, node.Value, nodesToCheckForCliques[i]))
                        {
                            //create PCN
                            List<int> innerNodes = new List<int> { node.Key };
                            innerNodes.AddRange(nodesToCheckForCliques[i]);
                            PRAClusterNode c = new PRAClusterNode(currAbslayerID, currentLevel.LastAssignedClusterID, innerNodes);
                            c.calculateXY(oneLevelLower);
                            c.setParentToAllInnerNodes(oneLevelLower);

                            //raise ID
                            currentLevel.LastAssignedClusterID++;
                            //add clique to abstract layer
                            currentLevel.AddClusterNode(c);

                            //mark all nodes now contained in the clusernode as resolved
                            foreach (int n in c.innerNodes)
                            {
                                resolvedNodes.Add(n, oneLevelLower.ClusterNodes[n]);
                            }
                            //stop checking cliques for node
                            break;
                        }
                    }
                }
                #endregion

                //building PCNs for this layer is finished. 
                //Remove all resovled nodes from nodes. The remaining nodes will be orphans.
                foreach (var n in resolvedNodes)
                {
                    nodes.Remove(n.Key);
                }

                #region orphans
                //create orphan PCNs 
                foreach (var n in nodes)
                {
                    PRAClusterNode c = new PRAClusterNode(currAbslayerID, currentLevel.LastAssignedClusterID, new List<int> { n.Key });
                    c.calculateXY(oneLevelLower);
                    c.setParentToAllInnerNodes(oneLevelLower);

                    //raise ID
                    currentLevel.LastAssignedClusterID++;
                    //add clique to abstract layer
                    currentLevel.AddClusterNode(c);
                }
                #endregion

                structure.Add(currAbslayerID, currentLevel);

                #region PRAClusterNode edge creation

                //add cluster connections
                foreach (PRAClusterNode c in currentLevel.ClusterNodes.Values)
                {
                    List<PRAClusterNode> innerNodesNeighbors = getInnerNodeNeighbors_tmp(c, structure, currAbslayerID);
                    foreach (PRAClusterNode p in innerNodesNeighbors)
                    {
                        //add neighbors (=> create edge)
                        c.AddNeighbor(p.ID, p);
                        p.AddNeighbor(c.ID, c);
                    }

                }
                #endregion

                //if the map has properties such as multiple non-reachable areas that 
                //are already as abstracted as possible, break the while loop
                if (currentLevel.AllCLustersDisconnected())
                {
                    break;
                }

                //building PCN connection and, therefore, this abstraction layer.
                //check if this abstraction layer contains only a single node.
                //if it does, finish. if it doesn't, raise the currentAbslayerID and loop.
                if (currentLevel.ClusterNodes.Count == 1)
                {
                    break;
                }
                else
                {
                    currAbslayerID++;
                }
            }
            return structure;
        }
        #endregion

        #region tmp innerNeighbors
        private List<PRAClusterNode> getInnerNodeNeighbors_tmp(PRAClusterNode c, Dictionary<int, PRAbstractionLayer> structure, int layer)
        {
            List<PRAClusterNode> res = new List<PRAClusterNode>();
            foreach (var n in c.innerNodes)
            {
                if (layer == 0)
                {
                    foreach (var n2 in gMap.Nodes[n].Neighbors.Keys)
                    {
                        Node neigh = gMap.Nodes[n2];
                        if (neigh.IsTraversable() && !c.innerNodes.Contains(n2))
                        {
                            //get the PRAClusterParent of this node and add it to the list
                            int parentID = neigh.PRAClusterParent;
                            PRAClusterNode p = structure[0].ClusterNodes[parentID];
                            if (!res.Contains(p))
                            {
                                res.Add(p);
                            }
                        }
                    }
                }
                else
                {
                    PRAbstractionLayer prev = structure[layer - 1];
                    foreach (var n2 in prev.ClusterNodes[n].neighbors)
                    {
                        //get the PRAClusterParent of this node and add it to the list
                        int parentID = n2.Value.PRAClusterParent;
                        PRAClusterNode p = structure[layer].ClusterNodes[parentID];
                        if (!res.Contains(p))
                        {
                            res.Add(p);
                        }
                    }
                }
            }
            return res;
        }
        #endregion

        #endregion
    }
}
