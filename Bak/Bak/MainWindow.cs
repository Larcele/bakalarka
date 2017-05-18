using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Bak
{
    public partial class MainWindow : Form
    {
        int agentPosition = 0;
        int agentSpeed = 500; //ms

        private Stopwatch stopWatch;
        private Node lastNodeInfo;
        private float pathCost = 0;
        private Heuristic heuristic;

        public PictureBox mainPanel;

        Dictionary<int, AbstractionLayer> HierarchicalGraph = new Dictionary<int, AbstractionLayer>();
        Dictionary<int, PRAbstractionLayer> PRAstarHierarchy = new Dictionary<int, PRAbstractionLayer>();

        BackgroundWorker pathfinder;
        BackgroundWorker invalidater;

        System.Threading.Timer agent;

        List<int> PathfindingSolution = new List<int>();
        HashSet<int> searchedNodes = new HashSet<int>();
        string FilePath = "GMaps\\simple.gmap";

        GameMap gMap;
        public MainWindow()
        {
            InitializeComponent();

            this.MaximizeBox = false;
            this.DoubleBuffered = true;
            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.DoubleBuffer,
                true);

            this.Width = 1120;
            this.Height = 750;

            c_selectedPathfinding.SelectedIndex = 0;
            pathLed.BackColor = ColorPalette.NodeColor_Path;
            visitedLed.BackColor = ColorPalette.NodeColor_Visited;

            mainPanel = pictureBox1;
            mainPanel.Width = 600;
            mainPanel.Height = 600;

            //mainPanel.AutoScroll = true;

            mainPanel.Paint += MainPanel_Paint;
            mainPanel.MouseDown += MainPanel_Click;

            
            gMap = new GridMap(this, 5, 5, FilePath);
            SetMapSize();
            this.Text = FilePath;

            RedrawMap();
            editingModesButton_Click(b_nontraversable, null);

            BuildHPAClusters();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            //e.Graphics.TranslateTransform(mainPanel.AutoScrollPosition.X, mainPanel.AutoScrollPosition.Y);
            base.OnPaint(e);
        }

        private void MainPanel_Click(object sender, MouseEventArgs e)
        {
            foreach (Node node in gMap.Nodes.Values)
            {
                if (node.IsHit(e.X, e.Y))
                {
                    if (e.Button == MouseButtons.Right)
                    {
                        node.InvokeNodeClick(e);
                    }
                    //else left button
                    else if (gMap.EditingNodeMode != node.Type)
                    {
                        node.InvokeNodeClick(e);
                        checkPRAClusters(node);
                    }
                    break;
                }
            }

            Invalidate();
            mainPanel.Invalidate();
            
        }

        private void checkPRAClusters(Node node)
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
                    recalculatePRALayers();

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
                        recalculatePRALayers();

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
                recalculatePRALayers();

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
                    recalculatePRALayers();

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
                        recalculatePRALayers();

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
                if (!clusterNeighbors.Contains(prcn)) {
                    clusterNeighbors.Add(prcn);
                }
            }
            return clusterNeighbors;
        }

        private void MainPanel_Paint(object sender, PaintEventArgs e)
        {
            Pen p = new Pen(Color.Black);

            foreach (Node node in gMap.Nodes.Values)
            {
                SolidBrush nodeBrush = new SolidBrush(node.BackColor);

                Rectangle r = new Rectangle(node.Location, node.Size);
                e.Graphics.FillRectangle(nodeBrush, r);
                node.PaintNode(e, r);
            }
            
            ///e.Graphics.TranslateTransform(mainPanel.AutoScrollPosition.X, mainPanel.AutoScrollPosition.Y);
    }

        
        #region SAVE
        private void saveMapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StringBuilder[] lines = initSaveFile();
            string[] mapContent = createGridMapRepresentation(lines);

            string filename = getFilenameFromPath();

            SaveFileDialog savefileDialog = new SaveFileDialog { FileName = filename, Filter = "GMAP files (*.gmap)|*.gmap"};
            if (savefileDialog.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllLines(savefileDialog.FileName, mapContent);
                this.Text = savefileDialog.FileName;
            }
        }

        internal void FillNodeInfo(Node node)
        {
            if (lastNodeInfo != null)
            {
                lastNodeInfo.BackColor = ColorPalette.NodeTypeColor[lastNodeInfo.Type];
            }

            lastNodeInfo = node;

            lastNodeInfo.BackColor = Color.Aquamarine;

            tb_nodeInfo.Text = "";

            tb_nodeInfo.Text += "ID: " + node.ID + Environment.NewLine;
            tb_nodeInfo.Text += "Location: " + node.Location + Environment.NewLine;
            tb_nodeInfo.Text += "Type: " + node.Type + Environment.NewLine;
            tb_nodeInfo.Text += "Neighbor IDs: " + node.PrintNeighbors() + Environment.NewLine;
        }

        private string getFilenameFromPath()
        {
            string[] tmp = FilePath.Split(new string[] { "\\" }, StringSplitOptions.None);
            return tmp[tmp.Length - 1];
        }

        private string[] createGridMapRepresentation(StringBuilder[] lines)
        {
            var tmpMap = cloneMapKeysAndSort();
            int counter = 0;
            int linePosition = 0;

            string[] res = new string[gMap.Height];
            foreach (var nodeID in tmpMap)
            {
                if (counter == gMap.Width)
                {
                    res[linePosition] = lines[linePosition].ToString();
                    linePosition++;
                    counter = 0;
                }
                Node n = gMap.Nodes[nodeID];
                lines[linePosition].Append(gMap.NodeTypeMapChar[n.Type]);
                counter++;
            }
            //last line
            res[linePosition] = lines[linePosition].ToString();
            return res;
        }

        private List<int> cloneMapKeysAndSort()
        {
            var mapKeys = gMap.Nodes.Keys.ToList();
            mapKeys.Sort((id1, id2) => id1.CompareTo(id2));
            return mapKeys;
        }

        private StringBuilder[] initSaveFile()
        {
            StringBuilder[] res = new StringBuilder[gMap.Height];
            for (int i = 0; i < gMap.Height; ++i)
            {  res[i] = new StringBuilder(""); }
            return res;
        }
        #endregion

        #region LOAD
        
        private void loadMapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog() { InitialDirectory = Directory.GetCurrentDirectory() + "\\GMaps", Title = "Open GMAP File" };//, Filter = "GMAP files (*.gmap)|*.gmap" };
            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                ResetPanelSize();

                string[] filelines = File.ReadAllLines(fileDialog.FileName);
                LoadNewMap(fileDialog.FileName, filelines);
                this.Text = fileDialog.FileName;
                SetMapSize();
                MainWindow_Resize(null, null);
            }
        }

        private void ResetPanelSize()
        {
            this.mainPanel.Width = 600;
            this.mainPanel.Height = 600;
        }

        private void SetMapSize()
        {
            if (gMap is GridMap && ((GridMap)gMap).SquareSize < 20)
            {
                mainPanel.Width = gMap.Width * ((GridMap)gMap).SquareSize;
                mainPanel.Height = gMap.Height * ((GridMap)gMap).SquareSize;

                panel.Width = mainPanel.Width;
                panel.Height = mainPanel.Height;
            }
        }

        public void LoadNewMap(string fileName, string[] mapContent)
        {
            HierarchicalGraph.Clear();
            PRAstarHierarchy.Clear();

            int width = mapContent[0].Length;
            int height = mapContent.Length;

            RemoveMapControls();

            gMap = new GridMap(this, width, height, mapContent);
            RedrawMap();
            editingModesButton_Click(b_nontraversable, null);

            BuildHPAClusters();
            BuildPRAbstractMap();
        }

        private void BuildPRAbstractMap()
        {
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
                PRAClusterNode c = new PRAClusterNode(currentAbslayerID, absl.LastAssignedClusterID, new List<int>{ n.Key });
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

                    List <List<int>> nodesToCheckForCliques = GetCombinations(nonResovledNeighbors);
                    nodesToCheckForCliques = nodesToCheckForCliques.OrderByDescending(n => n.Count).ToList();

                    for (int i = 0; i < nodesToCheckForCliques.Count; ++i)
                    {
                        //check for neighbors if they are a clique with node
                        if (nodesAreClique(oneLevelLower.ID, node.Value, nodesToCheckForCliques[i]))
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
                /*
                foreach (var c in currentLevel.ClusterNodes)
                {
                    foreach (var c2 in currentLevel.ClusterNodes)
                    {
                        if (c.Value == c2.Value) { continue; }

                        //check if any inner node in c has a neighbor that is c2's inner node
                        //=> means that inner nodes are cluster nodes one layer DOWN from abstract layer containing 
                        foreach (var n in c.Value.innerNodes)
                        {
                            var res = oneLevelLower.ClusterNodes[n].neighbors.Keys.Intersect(c2.Value.innerNodes);
                            if (res.Count() != 0)
                            {
                                //add neighbors (=> create edge)
                                c.Value.AddNeighbor(c2.Key, c2.Value);// Value.neighbors.Add(c2.Key, c2.Value);
                                c2.Value.AddNeighbor(c.Key, c.Value);

                            }
                        }

                    }
                }*/
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
        
        static List<List<int>> GetCombinations(List<int> list)
        {
            List<List<int>> res = new List<List<int>>();
            int index = 0;

            double count = Math.Pow(2, list.Count);
            for (int i = 1; i <= count - 1; i++)
            {
                string str = Convert.ToString(i, 2).PadLeft(list.Count, '0');
                for (int j = 0; j < str.Length; j++)
                {
                    if (str[j] == '1')
                    {
                        if (index >= res.Count)
                        {
                            res.Add(new List<int> { list[j] });
                        }
                        else
                        {
                            res[index].Add(list[j]);
                        }
                    }
                }
                index++;
            }
            return res;
        }

        private bool wouldBeValidClique(int nodeToAdd, List<int> nodeIDs)
        {
            int cliqueSize = nodeIDs.Count + 1;
            if (cliqueSize == 3)
            {
                int n0 = nodeIDs[0];
                int n1 = nodeIDs[1];

                if (gMap.Nodes[nodeToAdd].AreNeighbors(n0, n1) && AreTraversable(nodeToAdd, n0, n1))
                {
                    return true;
                }
                else return false;
            }
            else if (cliqueSize == 4)
            {
                int n0 = nodeIDs[0];
                int n1 = nodeIDs[1];
                int n2 = nodeIDs[2];

                if (gMap.Nodes[nodeToAdd].AreNeighbors(n0, n1, n2) && AreTraversable(nodeToAdd, n0, n1, n2))
                {
                    return true;
                }
                else return false;
            }
            else
            {
                MessageBox.Show("Cique size: " + cliqueSize + " seems invalid here...");
                return false;
            }
            
        }

        /// <summary>
        /// Determines whenever the nodes are in a clique with node
        /// </summary>
        /// <param name="node"></param>
        /// <param name="nodes"></param>
        /// <returns></returns>
        private bool nodesAreClique(int aLayerID, PRAClusterNode node, List<int> nodes)
        {
            //check for 2-clique
            if (nodes.Count == 1)
            {
                // if [0] -> [2]... then also [2] -> [0]... must be true; Then they are a 2-clique
                if (node.HasNeighbor(nodes[0]) && PRAstarHierarchy[aLayerID].ClusterNodes[nodes[0]].HasNeighbor(node.ID))
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
                    if (!PRAstarHierarchy[aLayerID].ClusterNodes[n].HasAllNeighbors(neighborsToCheck))
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

        private bool nonClusteredNodes(Dictionary<int, Node> nonClusteredNodes, params int[] nodes)
        {
            foreach (var nodeID in nodes)
            {
                //this is O(1) because Dictionary
                if (!nonClusteredNodes.ContainsKey(nodeID))
                {
                    return false;
                }
            }
            return true;
        }

        private bool AreTraversable(params int[] vals)
        {
            foreach (var n in vals)
            {
                if (!gMap.Nodes.ContainsKey(n) || !gMap.Nodes[n].IsTraversable())
                {
                    return false;
                }
            }
            return true;
        }

        private void RemoveMapControls()
        {
            //this.mainPanel.Controls.Clear();
            while (this.mainPanel.Controls.Count > 0)
            {
                Control oKill = this.mainPanel.Controls[0];
                this.mainPanel.Controls.RemoveAt(0);
                if (oKill != null)
                    oKill.Dispose();
            }
            foreach (Control c in gMap.Nodes.Values)
            {
                c.Dispose();
            }
        }

        private void RedrawMap()
        {
            gMap.DrawAllNodes();
            Update();
            Invalidate();
        }

        #endregion

        private void editingModesButton_Click(object sender, EventArgs e)
        {
            Button button = (Button)sender;
            setEditingButtonsDefualtColor();

            switch ((string)button.Tag)
            {
                case "traversable":
                    gMap.EditingNodeMode = GameMap.NodeType.Traversable;
                    break;

                case "!traversable":
                    gMap.EditingNodeMode = GameMap.NodeType.Obstacle;
                    break;

                case "end":
                    gMap.EditingNodeMode = GameMap.NodeType.EndPosition;
                    break;

                case "start":
                    gMap.EditingNodeMode = GameMap.NodeType.StartPosition;
                    break;
            }
            button.BackColor = ColorPalette.NodeTypeColor[gMap.EditingNodeMode];
        }
        
        private void setEditingButtonsDefualtColor()
        {
            foreach (var child in p_editingMapModes.Controls)
            {
                if (child is Button)
                {
                    ((Button)child).BackColor = DefaultBackColor;
                }
            }
        }
        
        private void b_startPathFinding_Click(object sender, EventArgs e)
        {
            pathCost = 0;
            PathfindingSolution.Clear();
            searchedNodes.Clear();

            b_mapRefresh.PerformClick();

            if (gMap.StartNodeID == -1 || gMap.EndNodeID == -1)
            {
                MessageBox.Show("Please set start and end node on map before search.");
                return;
            }

            string pathfinding = (string)c_selectedPathfinding.SelectedItem;

            if (pathfinding == "BackTrack" && gMap.Nodes.Count > 20)
            {
                MessageBox.Show("This map is too large for BackTracking to compute a solution in a reasonable amount of time. Please select another pathfinding approach.");
                return;
            }

            pathfinder = new BackgroundWorker();
            pathfinder.WorkerSupportsCancellation = true;
            
            invalidater = new BackgroundWorker();
            invalidater.WorkerSupportsCancellation = true;

            DisablePathFindingdControls();

            stopWatch = new Stopwatch();
            stopWatch.Start();
            switch (pathfinding)
            {
                case "PRA* (Manhattan heuristic)":
                    heuristic = Heuristic.Manhattan;
                    pathfinder.DoWork += StartPRAstarSearch;
                    pathfinder.RunWorkerCompleted += Pathfinder_RunWorkerCompleted;
                    pathfinder.RunWorkerAsync();

                    invalidater.DoWork += Invalidater_DoWork;
                    invalidater.RunWorkerAsync();
                    break;

                case "PRA* (Diagonal shortcut)":
                    heuristic = Heuristic.DiagonalShortcut;
                    pathfinder.DoWork += StartPRAstarSearch;
                    pathfinder.RunWorkerCompleted += Pathfinder_RunWorkerCompleted;
                    pathfinder.RunWorkerAsync();

                    invalidater.DoWork += Invalidater_DoWork;
                    invalidater.RunWorkerAsync();
                    break;

                case "HPA* (Manhattan Heuristic)":
                    pathfinder.DoWork += StartHPAstarSearch;
                    pathfinder.RunWorkerCompleted += Pathfinder_RunWorkerCompleted;
                    pathfinder.RunWorkerAsync();

                    invalidater.DoWork += Invalidater_DoWork;
                    invalidater.RunWorkerAsync();
                    break;

                case "A* (Manhattan Heuristic)":
                    heuristic = Heuristic.Manhattan;
                    pathfinder.DoWork += StartAstarSearch;
                    pathfinder.RunWorkerCompleted += Pathfinder_RunWorkerCompleted;
                    pathfinder.RunWorkerAsync();

                    invalidater.DoWork += Invalidater_DoWork;
                    invalidater.RunWorkerAsync();
                    break;

                case "A* (Diagonal Shortcut)":
                    heuristic = Heuristic.DiagonalShortcut;
                    pathfinder.DoWork += StartAstarSearch;
                    pathfinder.RunWorkerCompleted += Pathfinder_RunWorkerCompleted;
                    pathfinder.RunWorkerAsync();

                    invalidater.DoWork += Invalidater_DoWork;
                    invalidater.RunWorkerAsync();
                    break;

                case "BackTrack":
                    pathfinder.DoWork += StartBackTrackSearch;
                    pathfinder.RunWorkerCompleted += Pathfinder_RunWorkerCompleted;
                    pathfinder.RunWorkerAsync();
                    StartBackTrackSearch(null, null);

                    invalidater.DoWork += Invalidater_DoWork;
                    invalidater.RunWorkerAsync();
                    break;

                case "Dijkstra":
                    pathfinder.DoWork += StartDijkstraSearch;
                    pathfinder.RunWorkerCompleted += Pathfinder_RunWorkerCompleted;
                    pathfinder.RunWorkerAsync();

                    invalidater.DoWork += Invalidater_DoWork;
                    invalidater.RunWorkerAsync();
                    break;
            }
        }

        private void StartAgent()
        {
            //reset position
            agentPosition = 0;
            agent = new System.Threading.Timer(MoveAgent, null, agentSpeed, Timeout.Infinite);
        }

        private void MoveAgent(Object state)
        {
            if (agentPosition >= PathfindingSolution.Count)
            {
                invalidater.CancelAsync();
                return;
            }

            int id = PathfindingSolution[agentPosition];
            
            gMap.Nodes[id].BackColor = ColorPalette.NodeColor_Agent;

            if (agentPosition > 0)
            {
                int prev = PathfindingSolution[agentPosition - 1];
                gMap.Nodes[prev].BackColor = ColorPalette.NodeColor_Path;
            }

            agentPosition++;
            // Long running operation
            agent.Change(agentSpeed/2, Timeout.Infinite);
        }

        private void Invalidater_DoWork(object sender, DoWorkEventArgs e)
        {
            while(true)
            {
                if (invalidater.CancellationPending) { break; }

                mainPanel.Invalidate();
            }
        }

        private void Pathfinder_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            EnablePathFindingdControls();

            stopWatch.Stop();
            tb_elapsedTime.Text = stopWatch.Elapsed.TotalMilliseconds.ToString();

            printSolution();
            StartAgent();

            //Update();
            // Invalidate();
            mainPanel.Update();
        }

        private void DisablePathFindingdControls()
        {
            b_startPathFinding.Enabled = false;
        }

        private void EnablePathFindingdControls()
        {
            b_startPathFinding.Enabled = true;
        }
        
        /// <summary>
        /// determines the ID of a cluster to which a node belongs on a given abstraction layer.
        /// IMPORTANT: nodeID is the id of a *node*, NOT a (PRA)ClusterNode.
        /// </summary>
        /// <param name="abslayerID"></param>
        /// <returns></returns>
        public PRAClusterNode ClusterParent(int nodeID, int abslayerID)
        {
            if (abslayerID >= PRAstarHierarchy.Count)
            {
                throw new IndexOutOfRangeException("Abstraction layer ID is out of range.");
            }

            //starts form the bottom layer; zero
            int currentLayerCluster = gMap.Nodes[nodeID].PRAClusterParent;
            int currLayer = 0;

            while (currLayer != abslayerID)
            {
                currentLayerCluster = PRAstarHierarchy[currLayer].ClusterNodes[currentLayerCluster].PRAClusterParent;
                currLayer++;
            }

            return PRAstarHierarchy[currLayer].ClusterNodes[currentLayerCluster];
        }

        private void StartPRAstarSearch(object sender, DoWorkEventArgs e)
        {
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

            for (int level = startingLayer-1; level >= 0; level--)
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
            for (int i = abstractPath.Count - 1; i > 0 ; --i)
            {
                List<int> partialPath = new List<int>();

                partialPath = AstarLowestLevelSearch(PRAstarHierarchy[0].ClusterNodes[abstractPath[i]],
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
            
            pathfinder.CancelAsync();

        }

        private void AddToPathfSol(List<int> partialPath)
        {
            //since A* traces the path from the end to start by assigned parent nodes, we need to reverse it.
            for (int i = partialPath.Count - 1; i >= 0; i--)
            {
                PathfindingSolution.Add(partialPath[i]);
                //set the bg color of the path node
                gMap.Nodes[partialPath[i]].BackColor = ColorPalette.NodeColor_Path;
            }
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
        private List<int> AstarLowestLevelSearch(PRAClusterNode start, PRAClusterNode nextDestination, int startPosition)
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
            HashSet<int> openSet = new HashSet<int>();

            //starting node is in the open set
            openSet.Add(startPosition);

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
                currNode = openSet.OrderBy(i => fScore[i]).FirstOrDefault();
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
                    if (!openSet.Contains(neighbor.Key)) // Discover a new node
                    {
                        searchedNodes.Add(neighbor.Key);
                        setSearchedBgColor(neighbor.Key);
                        openSet.Add(neighbor.Key);
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
                sol.Add(currNode);
            }

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
            HashSet<int> openSet = new HashSet<int>();

            //starting node is in the open set
            openSet.Add(start.ID);

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
                currNode = openSet.OrderBy(i => fScore[i]).FirstOrDefault();
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
                    if (!openSet.Contains(neighbor.Key)) // Discover a new node
                    {
                        searchedNodes.Add(neighbor.Key);
                        openSet.Add(neighbor.Key);
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

        private void StartHPAstarSearch(object sender, DoWorkEventArgs e)
        {
            //do partial a* searches in clusters and visualise them

            //concat it all to pathfindingSolution

            StartAstarSearch(null, null);


        }

        private void BuildHPAClusters()
        {
            HierarchicalGraph.Clear();
            
            int xDiv = gMap.Width / 2;
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

            BuildClusterConnections(absl);
        }

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

        private void BuildClusterConnections(AbstractionLayer absl)
        {
            int clusterNodeID = 0;

            foreach (var c in absl.Clusters)
            {
                foreach (int nodeID in c.Value.OuterNodes)
                {
                    //look at neighbors of nodeID. 
                    //Check if another cluster contains the node's neighbor in its OuterNodes.
                    foreach (var neighbor in gMap.Nodes[nodeID].Neighbors)
                    {
                        int cID = clusterOuterNode(absl, c.Key, neighbor.Key);
                        if (cID != -1) //cluster does contain neighbor in its outer nodes
                        {
                            ClusterNode cln = new ClusterNode(cID, neighbor.Key);
                            c.Value.ClusterNodes.Add(clusterNodeID, cln);
                            
                            clusterNodeID++;
                        }
                    }
                }


            }
       }

        /// <summary>
        /// returns cluster ID(key) of the cluster that contains neighbor in its outerNodes
        /// </summary>
        /// <param name="abscl"></param>
        /// <param name="cID"></param>
        /// <param name="neighbor"></param>
        /// <returns></returns>
        private int clusterOuterNode(AbstractionLayer abscl, int cID, int neighbor)
        {
            foreach (var c in abscl.Clusters)
            {
                if (c.Key != cID)
                {
                    foreach (var n in c.Value.OuterNodes)
                    {
                        if (neighbor == n)
                        {
                            return c.Key;
                        }
                    }
                }
            }
            return -1;
        }

        private void StartDijkstraSearch(object sender, DoWorkEventArgs e)
        {
            Dictionary<int, NodeInfo> shortestDist = new Dictionary<int, NodeInfo>();

            //init the distances to each node from the starting node
            foreach (var nodeID in gMap.Nodes.Keys)
            {
                shortestDist.Add(nodeID, new NodeInfo(Int32.MaxValue, Int32.MaxValue));
            }
            shortestDist[gMap.StartNodeID] = new NodeInfo(0, 0);

            int currNodeID = gMap.StartNodeID;
            while (currNodeID > -1)
            {
                searchedNodes.Add(currNodeID);
                //change bg color to indicate it was searched
                setSearchedBgColor(currNodeID);
                
                foreach (var neighbor in gMap.Nodes[currNodeID].Neighbors)
                {
                    if (gMap.Nodes[neighbor.Key].Type != GameMap.NodeType.Obstacle && !searchedNodes.Contains(neighbor.Key))
                    {
                        if (shortestDist[currNodeID].PathCost + neighbor.Value < shortestDist[neighbor.Key].PathCost)
                        {
                            shortestDist[neighbor.Key].PathCost = shortestDist[currNodeID].PathCost + neighbor.Value;
                            shortestDist[neighbor.Key].Parent = currNodeID;
                            //update also the path 
                        }
                    }
                }
                currNodeID = closestNeighbor(shortestDist);
            }
            
            int parentID = gMap.EndNodeID;
            List<int> reversedPath = new List<int>();
            reversedPath.Add(parentID);
            while (parentID != gMap.StartNodeID)
            {
                parentID = shortestDist[parentID].Parent;
                if (parentID == Int32.MaxValue)
                {
                    //no path
                    reversedPath.Clear();
                    pathCost = 0;
                    break;
                }
                reversedPath.Add(parentID);
            }
            AddToPathfSol(reversedPath);

            pathCost = shortestDist[gMap.EndNodeID].PathCost;

            pathfinder.CancelAsync();
        }
        
        private int closestNeighbor(Dictionary<int, NodeInfo> shortestDistances)
        {
            var nonVisited = shortestDistances.Where(node => !searchedNodes.Contains(node.Key) && node.Value.PathCost != Int32.MaxValue);

            float smallestSeen = float.MaxValue;
            int minID = -1;
            foreach (var item in nonVisited)
            {
                if (item.Value.PathCost < smallestSeen)
                {
                    minID = item.Key;
                    smallestSeen = item.Value.PathCost;
                }
            }

            return minID;
        }

        private void StartAstarSearch(object sender, DoWorkEventArgs e)
        {
            bool[] closedSet = new bool[gMap.Nodes.Count];
            HashSet<int> openSet = new HashSet<int>();

            //starting node is in the open set
            openSet.Add(gMap.StartNodeID);

            // For each node, which node it can most efficiently be reached from.
            // If a node can be reached from many nodes, cameFrom will eventually contain the
            // most efficient previous step.
            Dictionary<int, int> cameFrom = new Dictionary<int, int>();

            // For each node, the cost of getting from the start node to that node.
            Dictionary<int, float> gScore = new Dictionary<int, float>();
            //default values are infinity
            foreach (int nodeID in gMap.Nodes.Keys)
            {
                gScore.Add(nodeID, float.MaxValue);
            }
            // The cost of going from start to start is zero.
            gScore[gMap.StartNodeID] = 0;

            // For each node, the total cost of getting from the start node to the goal
            // by passing by that node. That value is partly known, partly heuristic.
            Dictionary<int, float> fScore = new Dictionary<int, float>();
            //default values are infinity
            foreach (int nodeID in gMap.Nodes.Keys)
            {
                fScore.Add(nodeID, float.MaxValue);
            }

            // For the first node, that value is completely heuristic.
            fScore[gMap.StartNodeID] = H(gMap.StartNodeID);

            int currNode = 0; //default value
            bool pathFound = false;

            while (openSet.Count != 0)
            {
                //the node in openSet having the lowest fScore value
                currNode = openSet.OrderBy(i => fScore[i]).FirstOrDefault();
                if (currNode == gMap.EndNodeID)
                {
                    //break the loop and reconstruct path below
                    pathFound = true;
                    break;
                }

                openSet.Remove(currNode);
                closedSet[currNode] = true; //"added" to closedList

                foreach (var neighbor in gMap.Nodes[currNode].Neighbors)
                {
                    // Ignore the neighbor which is already evaluated or it is non-traversable.
                    if (gMap.Nodes[neighbor.Key].Type == GameMap.NodeType.Obstacle || closedSet[neighbor.Key] == true)
                    { continue; }

                    float tentativeG = gScore[currNode] + neighbor.Value; // The distance from start to a neighbor

                    if (!openSet.Contains(neighbor.Key)) // Discover a new node
                    {
                        searchedNodes.Add(neighbor.Key);
                        setSearchedBgColor(neighbor.Key); 
                        openSet.Add(neighbor.Key);
                    }
                    else if (tentativeG >= gScore[neighbor.Key])
                    {
                        continue; //not a better path
                    }

                    // This path is the best until now. Record it!
                    cameFrom[neighbor.Key] = currNode;
                    gScore[neighbor.Key] = tentativeG;
                    fScore[neighbor.Key] = gScore[neighbor.Key] + H(neighbor.Key);
                }
            }
            
            if (!pathFound)
            {
                PathfindingSolution.Clear();
                //No solution
            }
            else
            {
                List<int> reversedPath = new List<int>();

                reversedPath.Add(currNode);
                while (cameFrom.ContainsKey(currNode))
                {
                    currNode = cameFrom[currNode];
                    reversedPath.Add(currNode);
                }
                pathCost = gScore[gMap.EndNodeID];

                AddToPathfSol(reversedPath);
            }

            pathfinder.CancelAsync();
        }

        /// <summary>
        /// the estimated movement cost to move from that given square on the grid to the final destination 
        /// </summary>
        /// <param name="n">Node ID to get to</param>
        /// <returns></returns>
        private float H(int n)
        {
            switch (heuristic)
            {
                case Heuristic.Manhattan:
                    return 1 * (Math.Abs(gMap.Nodes[n].Location.X - gMap.Nodes[gMap.EndNodeID].Location.X) + Math.Abs(gMap.Nodes[n].Location.Y - gMap.Nodes[gMap.EndNodeID].Location.Y));

                case Heuristic.DiagonalShortcut:
                    
                    float h = 0;
                    int dx = Math.Abs(gMap.Nodes[n].Location.X - gMap.Nodes[gMap.EndNodeID].Location.X);
                    int dy = Math.Abs(gMap.Nodes[n].Location.Y - gMap.Nodes[gMap.EndNodeID].Location.Y);
                    if (dx > dy)
                        h = 1.4f * dy + 1 * (dx - dy);
                    else
                        h = 1.4f * dx + 1 * (dy - dx);
                    return h;

                default:
                    return 1 * (Math.Abs(gMap.Nodes[n].Location.X - gMap.Nodes[gMap.EndNodeID].Location.X) + Math.Abs(gMap.Nodes[n].Location.Y - gMap.Nodes[gMap.EndNodeID].Location.Y));

            }
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
                    return 1 * (Math.Abs(gMap.Nodes[n].Location.X - end.X) + Math.Abs(gMap.Nodes[n].Location.Y - end.Y));

                case Heuristic.DiagonalShortcut:
                    float h = 0;
                    int dx = Math.Abs(gMap.Nodes[n].Location.X - end.X);
                    int dy = Math.Abs(gMap.Nodes[n].Location.Y - end.Y);
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
                        h = 1.4f * dy + 1* (dx - dy);
                    else
                        h = 1.4f * dx + 1 * (dy - dx);
                    return h;

                default:
                    return 1 * (Math.Abs(layer.ClusterNodes[n].X - end.X) + Math.Abs(layer.ClusterNodes[n].Y - end.Y));

            }
        }

        #endregion

        private void StartBackTrackSearch(object sender, DoWorkEventArgs e)
        {
            List<int> path = new List<int>();
            backtrackMap(path, gMap.Nodes[gMap.StartNodeID]);
            pathfinder.CancelAsync();
            invalidater.CancelAsync();
        }

        private void printSolution()
        {
            tb_pathLength.Text = PathfindingSolution.Count+"";

            if (PathfindingSolution.Count == 0)
            {
                tb_pathLength.Text = "No solution";
                l_pathCost.Text = "- - - ";
                return;
            }
            
            foreach (int id in PathfindingSolution)
            {
                gMap.Nodes[id].BackColor = id != gMap.StartNodeID && id != gMap.EndNodeID ? ColorPalette.NodeColor_Path : ColorPalette.NodeTypeColor[gMap.Nodes[id].Type];
            }
            l_pathCost.Text = pathCost.ToString();
        }

        private void backtrackMap(List<int> path, Node current)
        {
            //not needed for pathfinding; remembered for future map refreshing, 
            //prevents accessing nodes that weren't changed.
            searchedNodes.Add(current.ID);

            path.Add(current.ID);
            gMap.Nodes[current.ID].BackColor = (gMap.Nodes[current.ID].Type != GameMap.NodeType.EndPosition && gMap.Nodes[current.ID].Type != GameMap.NodeType.StartPosition) ? ColorPalette.NodeColor_Visited : ColorPalette.NodeTypeColor[gMap.Nodes[current.ID].Type];
            if (current.Type == GameMap.NodeType.EndPosition)
            {
                if (path.Count < PathfindingSolution.Count || PathfindingSolution.Count == 0)
                {
                    int[] askldas = new int[path.Count];
                    path.CopyTo(askldas);
                    PathfindingSolution = askldas.ToList();
                }
                path.Remove(current.ID);
                return;
            }
            foreach (var node in current.Neighbors)
            {
                if (gMap.Nodes[node.Key].Type != GameMap.NodeType.Obstacle && !path.Contains(node.Key))
                {
                    backtrackMap(path, gMap.Nodes[node.Key]);
                }
            }
            path.Remove(current.ID);
        }

        private void b_mapRefresh_Click(object sender, EventArgs e)
        {
            foreach (int nodeID in gMap.Nodes.Keys)
            {
                gMap.Nodes[nodeID].BackColor = ColorPalette.NodeTypeColor[gMap.Nodes[nodeID].Type];
            }
            Invalidate();
            mainPanel.Invalidate();
        }

        private void MainWindow_Resize(object sender, EventArgs e)
        {
            panel.Height = Convert.ToInt32(this.Height * 0.7);
            panel.Width = Convert.ToInt32(this.Width * 0.9);
        }

        private void newMapMenuItem_Click(object sender, EventArgs e)
        {
            HierarchicalGraph.Clear();
            PRAstarHierarchy.Clear();

            ToolStripMenuItem t = (ToolStripMenuItem)sender;

            int size = Convert.ToInt32(t.Tag);            
            RemoveMapControls();

            gMap = new GridMap(this, size, size);
            RedrawMap();
            editingModesButton_Click(b_nontraversable, null);

            SetMapSize();

            BuildHPAClusters();

            Invalidate();
            mainPanel.Invalidate();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams handleParam = base.CreateParams;
                handleParam.ExStyle |= 0x02000000;   // WS_EX_COMPOSITED       
                return handleParam;
            }
        }

        private void showHPAClustersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int sfns = 50;
            foreach (AbstractionLayer a in HierarchicalGraph.Values)
            {
                foreach (Cluster c in a.Clusters.Values)
                {
                    Color col = Color.FromArgb(0, 0, sfns);
                    foreach (int nodeID in c.InnerNodes)
                    {
                        if (gMap.Nodes[nodeID].IsTraversable())
                        { gMap.Nodes[nodeID].BackColor = col; }
                    }
                    foreach (int nodeID in c.OuterNodes)
                    {
                        gMap.Nodes[nodeID].BackColor = Color.White;
                    }
                    foreach (var cNode in c.ClusterNodes.Values)
                    {
                        gMap.Nodes[cNode.GNodeID].BackColor = Color.Red;
                        gMap.Nodes[cNode.GNodeID].BackColor = Color.Red;

                    }
                    sfns += 50;
                }
            }

            Invalidate();
            mainPanel.Invalidate();
        }

        public enum Heuristic
        {
            Manhattan, //H = 1 * (abs(currentX - targetX) + abs(currentY - targetY))
            DiagonalShortcut 
        }

        private void showPRAClustersToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
        
        private void setSearchedBgColor(int currNodeID)
        {
            gMap.Nodes[currNodeID].BackColor = currNodeID != gMap.StartNodeID && currNodeID != gMap.EndNodeID ? ColorPalette.NodeColor_Visited : ColorPalette.NodeTypeColor[gMap.Nodes[currNodeID].Type];
        }
    }
}
