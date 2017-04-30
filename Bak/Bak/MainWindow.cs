using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Bak
{
    public partial class MainWindow : Form
    {
        private Stopwatch stopWatch;
        private Node lastNodeInfo;
        private float pathCost = 0;
        private Heuristic heuristic;

        public PictureBox mainPanel;

        Dictionary<int, AbstractionLayer> HierarchicalGraph = new Dictionary<int, AbstractionLayer>();
        Dictionary<int, PRAbstractionLayer> PRAstarHierarchy = new Dictionary<int, PRAbstractionLayer>();

        BackgroundWorker pathfinder;

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


            c_selectedPathfinding.SelectedIndex = 0;
            pathLed.BackColor = ColorPalette.NodeColor_Path;
            visitedLed.BackColor = ColorPalette.NodeColor_Visited;

            mainPanel = pictureBox1;
            //mainPanel.Width = 1280;
            //mainPanel.Height = 600;

            //mainPanel.AutoScroll = true;

            mainPanel.Paint += MainPanel_Paint;
            mainPanel.MouseDown += MainPanel_Click;

            
            gMap = new GridMap(this, 5, 5, FilePath);
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
                    node.InvokeNodeClick(e);
                    break;
                }
            }

            Invalidate();
            mainPanel.Invalidate();
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
            OpenFileDialog fileDialog = new OpenFileDialog() { InitialDirectory = Directory.GetCurrentDirectory() + "\\GMaps", Title = "Open GMAP File", Filter = "GMAP files (*.gmap)|*.gmap" };
            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                ResetPanelSize();

                string[] filelines = File.ReadAllLines(fileDialog.FileName);
                LoadNewMap(fileDialog.FileName, filelines);
                SetMapSize();
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
            int clusternodeId = 0;
            PRAbstractionLayer absl = new PRAbstractionLayer(0);

            //filter out all traversable nodes and sort (O(nlogn)
            Dictionary<int, Node> nodes = gMap.Nodes.Where(n => n.Value.Type != GameMap.NodeType.Obstacle)
                                                    .OrderBy(n => n.Key)
                                                    .ToDictionary(n => n.Key, n => n.Value);

            //each is O(n) where n is the number of nodes
            #region 4-cliques
            //find all 4-cliques
            for (int i = nodes.First().Key; i < nodes.Last().Key; ++i)
            {
                if (!nodes.ContainsKey(i)) { continue; }

                PRAClusterNode c = findNeighborClique(clusternodeId, nodes, nodes[i], 4);
                if (c != null) //4clique that contains nodes[i] exists 
                {
                    //raise ID
                    clusternodeId++;

                    //add clique to abstract layer
                    absl.AddClusterNode(c);

                    //delete all nodes now contained in the clusernode
                    foreach (int n in c.innerNodes)
                    {
                        nodes.Remove(n);
                    }
                }
            }
            #endregion

            #region 3-cliques
            //find all 3-cliques
            for (int i = nodes.First().Key; i < nodes.Last().Key; ++i)
            {
                if (!nodes.ContainsKey(i)) { continue; }

                PRAClusterNode c = findNeighborClique(clusternodeId, nodes, nodes[i], 3);
                if (c != null) //4clique that contains nodes[i] exists 
                {
                    //raise ID
                    clusternodeId++;
                    //add clique to abstract layer
                    absl.AddClusterNode(c);

                    //delete all nodes now contained in the clusernode
                    foreach (int n in c.innerNodes)
                    {
                        nodes.Remove(n);
                    }
                }
            }
            #endregion

            #region 2-cliques
            //find all 2-cliques
            for (int i = nodes.First().Key; i < nodes.Last().Key; ++i)
            {
                if (!nodes.ContainsKey(i)) { continue; }

                PRAClusterNode c = findNeighborClique(clusternodeId, nodes, nodes[i], 2);
                if (c != null) //4clique that contains nodes[i] exists 
                {
                    //raise ID
                    clusternodeId++;
                    //add clique to abstract layer
                    absl.AddClusterNode(c);

                    //delete all nodes now contained in the clusernode
                    foreach (int n in c.innerNodes)
                    {
                        nodes.Remove(n);
                    }
                }
            }
            #endregion

            #region orphan nodes
            //remaining nodes are orphans. Create remaining PRAClusterNodes from these orphans.
            foreach (var n in nodes)
            {
                PRAClusterNode c = new PRAClusterNode(clusternodeId, new List<int>{ n.Key });
                //raise ID
                clusternodeId++;
                //add clique to abstract layer
                absl.AddClusterNode(c);
            }
            #endregion

            //add all cliques to abstract graph
            PRAstarHierarchy.Add(absl.ID, absl);

            #region PRAClusterNode edge creation
            //add cluster connections
            foreach (var c in absl.nodes)
            {
                foreach (var c2 in absl.nodes)
                {
                    if (c.Value == c2.Value) { continue; }

                    //check if any inner node in c has a neighbor that is c2's inner node
                    foreach (var n in c.Value.innerNodes)
                    {
                        var res = gMap.Nodes[n].Neighbors.Keys.Intersect(c2.Value.innerNodes);
                        if (res.Count() != 0)
                        {
                            //add neighbors (=> create edge)
                            c.Value.AddNeighbor(c2.Key, c2.Value);// Value.neighbors.Add(c2.Key, c2.Value);
                            c2.Value.AddNeighbor(c.Key, c.Value);

                        }
                    }

                }
            }
            #endregion

            //build additional layers until a single abstract node is left
            buildPRALayers();
        }

        private void buildPRALayers()
        {
            //at the start, there is only the first abstraction layer. 
            //This layer consists of nodes, where a node can contain a 4-clique, 3-clique, 2-clique or be an orphan node.
            //We continue building layers and abstracting until there is only a single node left.

            int clusternodeId = 0;
            int currentAbslayerID = 1;

            while (true)
            {
                PRAbstractionLayer absl = new PRAbstractionLayer(currentAbslayerID);
                PRAbstractionLayer last = PRAstarHierarchy[currentAbslayerID - 1];

                Dictionary<int, PRAClusterNode> resolvedNodes = new Dictionary<int, PRAClusterNode>();

                //extract the nodes of the previous layer and sort them by theeir neighbor count
                Dictionary<int, PRAClusterNode> nodes = last.nodes.OrderBy(n => n.Value.neighbors.Count)
                                                                  .ToDictionary(n => n.Key, n => n.Value);

                //a PRAClusterNode *p* is in clique with other PCNs if each of 
                //the neighbors of *p* contains all other neighbors and also *p*
                //Example: PCNs 0,2 and 4 are in a clique if  [0] -> [2][4] && [2] -> [0][4] && [4] -> [0][2]

                #region clique building
                //building 4-cliques, 3-cliques, 2-cliques
                foreach (var node in nodes)
                {
                    if (resolvedNodes.ContainsKey(node.Key)) { continue; }

                    //check for neighbors if they are a clique with node
                    if (nodesAreClique(node.Value))
                    {
                        //create PCN
                        List<int> innerNodes = new List<int> { node.Key };
                        innerNodes.AddRange(node.Value.neighbors.Keys);
                        PRAClusterNode c = new PRAClusterNode(clusternodeId, innerNodes);

                        //raise ID
                        clusternodeId++;
                        //add clique to abstract layer
                        absl.AddClusterNode(c);

                        //mark all nodes now contained in the clusernode as redolved
                        foreach (int n in c.innerNodes)
                        {
                            resolvedNodes.Add(n, last.nodes[n]);
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
                    PRAClusterNode c = new PRAClusterNode(clusternodeId, new List<int> { n.Key });
                    //raise ID
                    clusternodeId++;
                    //add clique to abstract layer
                    absl.AddClusterNode(c);
                }
                #endregion

                #region PRAClusterNode edge creation
                //add cluster connections
                foreach (var c in absl.nodes)
                {
                    foreach (var c2 in absl.nodes)
                    {
                        if (c.Value == c2.Value) { continue; }

                        //check if any inner node in c has a neighbor that is c2's inner node
                        foreach (var n in c.Value.innerNodes)
                        {
                            var res = gMap.Nodes[n].Neighbors.Keys.Intersect(c2.Value.innerNodes);
                            if (res.Count() != 0)
                            {
                                //add neighbors (=> create edge)
                                c.Value.AddNeighbor(c2.Key, c2.Value);// Value.neighbors.Add(c2.Key, c2.Value);
                                c2.Value.AddNeighbor(c.Key, c.Value);

                            }
                        }

                    }
                }
                #endregion

                PRAstarHierarchy.Add(currentAbslayerID, absl);

                //building PCN connection and, therefore, this abstraction layer.
                //check if this abstraction layer contains only a single node.
                //if it does, finish. if it doesn't, raise the currentAbslayerID, reset the clusterID and loop.
                if (absl.nodes.Count == 1)
                {
                    break;
                }
                else
                {
                    currentAbslayerID++;
                    clusternodeId = 0;
                }
            }
        }

        private bool nodesAreClique(PRAClusterNode node)
        {
            //Example: PCNs 0,2 and 4 are in a clique if  [0] -> [2][4] && [2] -> [0][4] && [4] -> [0][2]

            foreach (var n in node.neighbors)
            {
                List<int> neighborsToCheck = node.neighbors.Where(n1 => n1.Key != n.Key)
                                                           .Select(n2 => n2.Key).ToList();
                neighborsToCheck.Add(node.ID);
                if (!n.Value.HasAllNeighbors(neighborsToCheck))
                {
                    return false;
                }
            }
            return true;
        }

        private PRAClusterNode findNeighborClique(int clusterNodeId, Dictionary<int, Node> nodes, Node n, int cliqueSize)
        {
            PRAClusterNode res = null;

            switch (cliqueSize)
            {
                case 4:
                    #region probably deprecated
                    /*
                    //upper-right 4square
                    if (n.AreNeighbors(n.ID - gMap.Width, n.ID - gMap.Width + 1, n.ID + 1)
                        && AreTraversable(n.ID - gMap.Width, n.ID - gMap.Width + 1, n.ID + 1))
                    {
                        res = new PRAClusterNode(nodeId, new List<int> {n.ID, n.ID - gMap.Width, n.ID - gMap.Width + 1, n.ID + 1 });
                        nodeId++;
                    }


                    //upper-left 4square
                    else if (n.AreNeighbors(n.ID - gMap.Width, n.ID - gMap.Width - 1, n.ID - 1)
                        && AreTraversable(n.ID - gMap.Width, n.ID - gMap.Width - 1, n.ID - 1))
                    {
                        res = new PRAClusterNode(nodeId, new List<int> { n.ID, n.ID - gMap.Width, n.ID - gMap.Width - 1, n.ID - 1 });
                        nodeId++;
                    }
                    
                    //lower-left 4square
                    else if (n.AreNeighbors(n.ID + gMap.Width, n.ID + gMap.Width - 1, n.ID - 1)
                        && AreTraversable(n.ID + gMap.Width, n.ID + gMap.Width - 1, n.ID - 1))
                    {
                        res = new PRAClusterNode(nodeId, new List<int> { n.ID, n.ID + gMap.Width, n.ID + gMap.Width - 1, n.ID - 1 });
                        nodeId++;
                    }
                    */
                    #endregion
                    
                    //lower-right 4square
                    if (nonClusteredNodes(nodes, n.ID + gMap.Width, n.ID + gMap.Width + 1, n.ID + 1) 
                        && n.AreNeighbors(n.ID + gMap.Width, n.ID + gMap.Width + 1, n.ID + 1)
                        && AreTraversable(n.ID + gMap.Width, n.ID + gMap.Width + 1, n.ID + 1))
                    {
                        res = new PRAClusterNode(clusterNodeId, new List<int> { n.ID, n.ID + gMap.Width, n.ID + gMap.Width + 1, n.ID + 1 });
                    }

                    break;

                case 3:
                    //lower-right 3 (horizontal/vertical)
                    if (nonClusteredNodes(nodes, n.ID + gMap.Width, n.ID + 1)
                        && n.AreNeighbors(n.ID + gMap.Width, n.ID + 1)
                        && AreTraversable(n.ID + gMap.Width, n.ID + 1))
                    {
                        res = new PRAClusterNode(clusterNodeId, new List<int> { n.ID, n.ID + gMap.Width, n.ID + 1 });
                    }

                    //lower-right 3 (horizontal and diagonal)
                    else if (nonClusteredNodes(nodes, n.ID + gMap.Width + 1, n.ID + 1)
                        && n.AreNeighbors(n.ID + gMap.Width + 1, n.ID + 1)
                        && AreTraversable(n.ID + gMap.Width + 1, n.ID + 1))
                    {
                        res = new PRAClusterNode(clusterNodeId, new List<int> { n.ID, n.ID + gMap.Width + 1, n.ID + 1 });
                    }

                    //lower-right 3 (vertical and diagonal)
                    else if (nonClusteredNodes(nodes, n.ID + gMap.Width, n.ID + 1)
                        && n.AreNeighbors(n.ID + gMap.Width, n.ID + 1)
                        && AreTraversable(n.ID + gMap.Width, n.ID + 1))
                    {
                        res = new PRAClusterNode(clusterNodeId, new List<int> { n.ID, n.ID + gMap.Width, n.ID + 1 });
                    }
                    break;

                case 2:
                    //lower-right horizontal
                    if (nonClusteredNodes(nodes, n.ID + 1)
                        && n.AreNeighbors(n.ID + 1)
                        && AreTraversable(n.ID + 1))
                    {
                        res = new PRAClusterNode(clusterNodeId, new List<int> { n.ID, n.ID + 1 });
                    }

                    //lower-right vertical
                    if (nonClusteredNodes(nodes, n.ID + gMap.Width)
                        && n.AreNeighbors(n.ID + gMap.Width)
                        && AreTraversable(n.ID + gMap.Width))
                    {
                        res = new PRAClusterNode(clusterNodeId, new List<int> { n.ID, n.ID + gMap.Width });
                    }

                    //lower-right diagonal
                    if (nonClusteredNodes(nodes, n.ID + gMap.Width + 1)
                        && n.AreNeighbors(n.ID + gMap.Width + 1)
                        && AreTraversable(n.ID + gMap.Width + 1))
                    {
                        res = new PRAClusterNode(clusterNodeId, new List<int> { n.ID, n.ID + gMap.Width + 1 });
                    }
                    
                    break;
            }

            return res;
        }

        private bool nonClusteredNodes(Dictionary<int, Node> nonClusteredNodes, params int[] nodes)
        {
            foreach (var nodeID in nodes)
            {
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

            if (gMap.StartNodeID == -1 || gMap.EndNodeID == -1)
            {
                MessageBox.Show("Please set start and end node on map before search.");
                return;
            }

            pathfinder = new BackgroundWorker();
            pathfinder.WorkerSupportsCancellation = true;

            DisablePathFindingdControls();

            stopWatch = new Stopwatch();
            stopWatch.Start();
            switch ((string)c_selectedPathfinding.SelectedItem)
            {
                case "HPA* (Manhattan Heuristic)":
                    pathfinder.DoWork += StartHPAstarSearch;
                    pathfinder.RunWorkerCompleted += Pathfinder_RunWorkerCompleted;
                    pathfinder.RunWorkerAsync();
                    
                    break;
                case "A* (Manhattan Heuristic)":
                    heuristic = Heuristic.Manhattan;
                    pathfinder.DoWork += StartAstarSearch;
                    pathfinder.RunWorkerCompleted += Pathfinder_RunWorkerCompleted;
                    pathfinder.RunWorkerAsync();

                    break;
                case "A* (Diagonal Shortcut)":
                    heuristic = Heuristic.DiagonalShortcut;
                    pathfinder.DoWork += StartAstarSearch;
                    pathfinder.RunWorkerCompleted += Pathfinder_RunWorkerCompleted;
                    pathfinder.RunWorkerAsync();

                    break;
                case "BackTrack":
                    pathfinder.DoWork += StartBackTrackSearch;
                    pathfinder.RunWorkerCompleted += Pathfinder_RunWorkerCompleted;
                    pathfinder.RunWorkerAsync();
                    StartBackTrackSearch(null, null);

                    break;
                case "Dijkstra":
                    pathfinder.DoWork += StartDijkstraSearch;
                    pathfinder.RunWorkerCompleted += Pathfinder_RunWorkerCompleted;
                    pathfinder.RunWorkerAsync();
                    break;
            }
        }

        private void Pathfinder_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            EnablePathFindingdControls();

            stopWatch.Stop();
            tb_elapsedTime.Text = stopWatch.Elapsed.TotalMilliseconds.ToString();

            printSolution();

            //Update();
            // Invalidate();
            //mainPanel.Update();
            mainPanel.Invalidate();
        }

        private void DisablePathFindingdControls()
        {
            b_startPathFinding.Enabled = false;
        }

        private void EnablePathFindingdControls()
        {
            b_startPathFinding.Enabled = true;
        }

        private void StartHPAstarSearch(object sender, DoWorkEventArgs e)
        {
            //do partial a* searches in clusters and visualise them

            //concat it all to pathfindingSolution



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
                        lastCol(j, xDiv - 1)) { //this node could be one of hte outer nodes later
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
                            ClusterNode cln = new ClusterNode(absl.ID, c.Key, cID, nodeID, neighbor.Key);
                            absl.ClusterNodes.Add(clusterNodeID, cln);
                            
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

            foreach (var id in searchedNodes)
            {
                gMap.Nodes[id].BackColor = id != gMap.StartNodeID && id != gMap.EndNodeID ? ColorPalette.NodeColor_Visited : ColorPalette.NodeTypeColor[gMap.Nodes[id].Type];
            }
            
            int parentID = gMap.EndNodeID;
            while (parentID != gMap.StartNodeID)
            {
                parentID = shortestDist[parentID].Parent;
                if (parentID == Int32.MaxValue)
                {
                    //no path
                    PathfindingSolution.Clear();
                    pathCost = 0;
                    break;
                }

                gMap.Nodes[parentID].BackColor = parentID != gMap.StartNodeID && parentID != gMap.EndNodeID ? ColorPalette.NodeColor_Path : ColorPalette.NodeTypeColor[gMap.Nodes[parentID].Type];
                PathfindingSolution.Add(parentID);
            }
            pathCost = shortestDist[gMap.EndNodeID].PathCost;
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
            List<int> closedList = new List<int>();
            List<int> openList = new List<int>();

            Dictionary<int, NodeInfo> shortestDist = new Dictionary<int, NodeInfo>();

            //init the distances to each node from the starting node
            foreach (var nodeID in gMap.Nodes.Keys)
            {
                shortestDist.Add(nodeID, new NodeInfo(Int32.MaxValue, Int32.MaxValue));
            }
            shortestDist[gMap.StartNodeID] = new NodeInfo(0, 0);


            int currNodeID = gMap.StartNodeID;
            openList.Add(currNodeID);
            while (openList.Count > 0)
            {
                //add all neighbors into the open list
                foreach (var n in gMap.Nodes[currNodeID].Neighbors)
                {
                    if (!gMap.Nodes[n.Key].IsTraversable() || closedList.Contains(n.Key))
                    {
                        continue;
                    }

                    if (!openList.Contains(n.Key))
                    {
                        openList.Add(n.Key);
                        searchedNodes.Add(n.Key);
                    }

                    //setting Parent
                    if (shortestDist[n.Key].Parent == Int32.MaxValue)
                    {
                        shortestDist[n.Key].Parent = currNodeID;
                    }
                    //check if the path for neighbor would be shorter if the path went through current node
                    //if yes, set neighbor's new parent and new pathcost
                    else if (shortestDist[n.Key].PathCost + gMap.Nodes[currNodeID].Neighbors[n.Key] < shortestDist[n.Key].PathCost)
                    {
                        shortestDist[n.Key].Parent = currNodeID;
                        shortestDist[n.Key].PathCost = shortestDist[n.Key].PathCost + gMap.Nodes[currNodeID].Neighbors[n.Key];
                        continue;
                    }

                    //setting pathCost
                    if (shortestDist[n.Key].PathCost == Int32.MaxValue)
                    {
                        shortestDist[n.Key].PathCost = n.Value;
                    }
                    else
                    {
                        shortestDist[n.Key].PathCost += n.Value;
                    }
                }
                closedList.Add(currNodeID);
                //calculate F for all neighbors (G + H)
                Dictionary<int, float> F = new Dictionary<int, float>();
                foreach (var n in openList)
                {
                    F.Add(n, shortestDist[n].PathCost/*G(n)*/ + H(n));
                }

                //choose the lowest F node (LFN)  (O(n))
                currNodeID = F.Aggregate((l, r) => l.Value < r.Value ? l : r).Key;

                //drop the LFN from the open list and add it to the closed list
                openList.Remove(currNodeID);
                closedList.Add(currNodeID);


                if (currNodeID == gMap.EndNodeID)
                {
                    break;
                }
            }

            //paint path
            foreach (var id in searchedNodes)
            {
                gMap.Nodes[id].BackColor = id != gMap.StartNodeID && id != gMap.EndNodeID ? ColorPalette.NodeColor_Visited : ColorPalette.NodeTypeColor[gMap.Nodes[id].Type];
            }

            int parentID = gMap.EndNodeID;
            while (parentID != gMap.StartNodeID)
            {
                parentID = shortestDist[parentID].Parent;
                pathCost += shortestDist[parentID].PathCost;

                if (parentID == Int32.MaxValue)
                {
                    //no path
                    PathfindingSolution.Clear();
                    break;
                }

                gMap.Nodes[parentID].BackColor = parentID != gMap.StartNodeID && parentID != gMap.EndNodeID ? ColorPalette.NodeColor_Path : ColorPalette.NodeTypeColor[gMap.Nodes[parentID].Type];
                PathfindingSolution.Add(parentID);
            }

            pathfinder.CancelAsync();
        }

        /// <summary>
        /// the estimated movement cost to move from that given square on the grid to the final destination 
        /// </summary>
        /// <param name="n">Node ID to get to</param>
        /// <returns></returns>
        private int H(int n)
        {
            switch (heuristic)
            {
                case Heuristic.Manhattan:
                    return 10 * (Math.Abs(gMap.Nodes[n].Location.X - gMap.Nodes[gMap.EndNodeID].Location.X) + Math.Abs(gMap.Nodes[n].Location.Y - gMap.Nodes[gMap.EndNodeID].Location.Y));

                case Heuristic.DiagonalShortcut:
                    int h = 0;
                    int xDistance = Math.Abs(gMap.Nodes[n].Location.X - gMap.Nodes[gMap.EndNodeID].Location.X);
                    int yDistance = Math.Abs(gMap.Nodes[n].Location.Y - gMap.Nodes[gMap.EndNodeID].Location.Y);
                    if (xDistance > yDistance)
                        h = 14 * yDistance + 10 * (xDistance - yDistance);
                    else
                        h = 14 * xDistance + 10 * (yDistance - xDistance);
                    return h;
                default:
                    return 10 * (Math.Abs(gMap.Nodes[n].Location.X - gMap.Nodes[gMap.EndNodeID].Location.X) + Math.Abs(gMap.Nodes[n].Location.Y - gMap.Nodes[gMap.EndNodeID].Location.Y));

            }
        }

        private void StartBackTrackSearch(object sender, DoWorkEventArgs e)
        {
            List<int> path = new List<int>();
            backtrackMap(path, gMap.Nodes[gMap.StartNodeID]);
            pathfinder.CancelAsync();
        }

        private void printSolution()
        {
            tb_pathOutput.Text = "";

            if (PathfindingSolution.Count == 0)
            {
                tb_pathOutput.Text = "No solution";
                l_pathCost.Text = "- - - ";
                return;
            }
            
            foreach (int id in PathfindingSolution)
            {
                tb_pathOutput.Text += id + ",";
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
                    sfns += 50;
                }
                foreach (var cNode in a.ClusterNodes.Values)
                {
                    gMap.Nodes[cNode.Node1ID].BackColor = Color.Red;
                    gMap.Nodes[cNode.Node2ID].BackColor = Color.Red;
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
    }


}
