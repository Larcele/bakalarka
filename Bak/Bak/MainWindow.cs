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
        BackgroundWorker invalidater;

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
            OpenFileDialog fileDialog = new OpenFileDialog() { InitialDirectory = Directory.GetCurrentDirectory() + "\\GMaps", Title = "Open GMAP File", Filter = "GMAP files (*.gmap)|*.gmap" };
            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                ResetPanelSize();

                string[] filelines = File.ReadAllLines(fileDialog.FileName);
                LoadNewMap(fileDialog.FileName, filelines);
                this.Text = fileDialog.FileName;
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
            int clusternodeId = 0;
            int currentAbslayerID = 0;
            PRAbstractionLayer absl = new PRAbstractionLayer(currentAbslayerID);

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

                PRAClusterNode c = findNeighborClique(currentAbslayerID, clusternodeId, nodes, nodes[i], 4);
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

                    if (nodes.Count == 0)
                    { break; }
                }
            }
            #endregion

            #region 3-cliques
            //find all 3-cliques
            if (nodes.Count > 0)
            {
                for (int i = nodes.First().Key; i < nodes.Last().Key; ++i)
                {
                    if (!nodes.ContainsKey(i)) { continue; }

                    PRAClusterNode c = findNeighborClique(currentAbslayerID, clusternodeId, nodes, nodes[i], 3);
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
                for (int i = nodes.First().Key; i < nodes.Last().Key; ++i)
                {
                    if (!nodes.ContainsKey(i)) { continue; }

                    PRAClusterNode c = findNeighborClique(currentAbslayerID, clusternodeId, nodes, nodes[i], 2);
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
                PRAClusterNode c = new PRAClusterNode(currentAbslayerID, clusternodeId, new List<int>{ n.Key });
                c.calculateXY(gMap.Nodes);
                c.InitPRAClusterParents(gMap.Nodes);
                //raise ID
                clusternodeId++;
                //add clique to abstract layer
                absl.AddClusterNode(c);

            }
            #endregion

            //add the abstract layer to abstraction hierarchy
            PRAstarHierarchy.Add(absl.ID, absl);

            #region PRAClusterNode edge creation
            //add cluster connections
            foreach (var c in absl.ClusterNodes)
            {
                foreach (var c2 in absl.ClusterNodes)
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

            //build additional layers until a single abstract node is left for each non-disconnected map space
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
                PRAbstractionLayer currentLevel = new PRAbstractionLayer(currentAbslayerID);
                PRAbstractionLayer oneLevelLower = PRAstarHierarchy[currentAbslayerID - 1];

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
                            PRAClusterNode c = new PRAClusterNode(currentAbslayerID, clusternodeId, innerNodes);
                            c.calculateXY(oneLevelLower);
                            c.setParentToAllInnerNodes(oneLevelLower);

                            //raise ID
                            clusternodeId++;
                            //add clique to abstract layer
                            currentLevel.AddClusterNode(c);

                            //mark all nodes now contained in the clusernode as redolved
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
                    PRAClusterNode c = new PRAClusterNode(currentAbslayerID, clusternodeId, new List<int> { n.Key });
                    c.calculateXY(oneLevelLower);
                    c.setParentToAllInnerNodes(oneLevelLower);

                    //raise ID
                    clusternodeId++;
                    //add clique to abstract layer
                    currentLevel.AddClusterNode(c);
                }
                #endregion

                #region PRAClusterNode edge creation
                //add cluster connections
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
                }
                #endregion

                PRAstarHierarchy.Add(currentAbslayerID, currentLevel);

                //if the map has inconvenient properties such as multiple non-reachable areas that 
                //are already as abstracted as possible, break the while loop
                if (currentLevel.AllCLustersDisconnected())
                {
                    break;
                }
                
                //building PCN connection and, therefore, this abstraction layer.
                //check if this abstraction layer contains only a single node.
                //if it does, finish. if it doesn't, raise the currentAbslayerID, reset the clusterID and loop.
                if (currentLevel.ClusterNodes.Count == 1)
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

                #region deprecated
                /*foreach (var n in node.neighbors)
                {
                    List<int> neighborsToCheck = node.neighbors.Where(n1 => n1.Key != n.Key)
                                                               .Select(n2 => n2.Key).ToList();
                    neighborsToCheck.Add(node.ID);
                    if (!n.Value.HasAllNeighbors(neighborsToCheck))
                    {
                        return false;
                    }
                }
                return true;*/
                #endregion
            }
            
        }

        private PRAClusterNode findNeighborClique(int absLayerID, int clusterNodeId, Dictionary<int, Node> nodes, Node n, int cliqueSize)
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

            pathfinder = new BackgroundWorker();
            pathfinder.WorkerSupportsCancellation = true;
            
            invalidater = new BackgroundWorker();
            invalidater.WorkerSupportsCancellation = true;

            DisablePathFindingdControls();

            stopWatch = new Stopwatch();
            stopWatch.Start();
            switch ((string)c_selectedPathfinding.SelectedItem)
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

                //the next start node is going to be the end node of this search. 
                //since A* path returned is reversed, the first element was the last (end) node.
                startID = partialPath[0];
            }
            
            pathfinder.CancelAsync();
            invalidater.CancelAsync();

        }

        private void AddToPathfSol(List<int> partialPath)
        {
            //since A* traces the path from the end to start by assigned parent nodes, we need to reverse it.
            for (int i = partialPath.Count - 1; i >= 0; i--)
            {
                PathfindingSolution.Add(partialPath[i]);
                //set the bg color of the path node

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

            List<int> closedList = new List<int>();
            List<int> openList = new List<int>();

            Dictionary<int, NodeInfo> shortestDist = new Dictionary<int, NodeInfo>();

            //init the distances to each node from the starting node
            foreach (var nodeID in nodesToSearch)
            {
                shortestDist.Add(nodeID, new NodeInfo(Int32.MaxValue, Int32.MaxValue));
            }
            shortestDist[startPosition] = new NodeInfo(0, 0);
            
            int currNodeID = startPosition;
            openList.Add(currNodeID);
            while (openList.Count > 0)
            {
                //add all neighbors into the open list
                foreach (var n in gMap.Nodes[currNodeID].Neighbors)
                {
                    if (closedList.Contains(n.Key) 
                        || (!start.innerNodes.Contains(n.Key) && !nextDestination.innerNodes.Contains(n.Key) ) )
                    {
                        continue;
                    }

                    if (!openList.Contains(n.Key))
                    {
                        openList.Add(n.Key);
                        searchedNodes.Add(n.Key);
                        setSearchedBgColor(n.Key);
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
                        shortestDist[n.Key].PathCost = gMap.Nodes[n.Key].Neighbors[currNodeID];// n.Value;
                    }
                    else
                    {
                        shortestDist[n.Key].PathCost += gMap.Nodes[n.Key].Neighbors[currNodeID];
                    }
                }
                closedList.Add(currNodeID);
                //calculate F for all neighbors (G + H)
                Dictionary<int, double> F = new Dictionary<int, double>();
                foreach (var n in openList)
                {
                    F.Add(n, shortestDist[n].PathCost/*G(n)*/ + H_lowPRA(n, nextDestination));
                }

                //choose the lowest F node (LFN)  (O(n))
                currNodeID = F.Aggregate((l, r) => l.Value < r.Value ? l : r).Key;

                //drop the LFN from the open list and add it to the closed list
                openList.Remove(currNodeID);
                closedList.Add(currNodeID);

                if ((nextDestination.innerNodes.Contains(currNodeID) && !isFinalCluster) || (currNodeID == gMap.EndNodeID))
                {
                    break;
                }
            }
            int parentID = currNodeID;

            //add the end node to path
            pathCost += shortestDist[parentID].PathCost;
            sol.Add(parentID); 

            //trace the rest of the path to start
            while (parentID != startPosition)
            {
                parentID = shortestDist[parentID].Parent;
                pathCost += shortestDist[parentID].PathCost;

                if (parentID == Int32.MaxValue)
                {
                    break;
                }

                gMap.Nodes[parentID].BackColor = parentID != gMap.StartNodeID && parentID != gMap.EndNodeID ? ColorPalette.NodeColor_Path : ColorPalette.NodeTypeColor[gMap.Nodes[parentID].Type];
                sol.Add(parentID);
            }

            return sol;

        }

        private List<int> AstarAbstractionSearch(PRAClusterNode start, PRAClusterNode end, PRAbstractionLayer layer, Dictionary<int, PRAClusterNode> nodesToSearch)
        {
            List<int> sol = new List<int>();

            List<int> closedList = new List<int>();
            List<int> openList = new List<int>();

            Dictionary<int, NodeInfo> shortestDist = new Dictionary<int, NodeInfo>();

            //init the distances to each node from the starting node
            foreach (var cnodeID in nodesToSearch)
            {
                shortestDist.Add(cnodeID.Key, new NodeInfo(Int32.MaxValue, Int32.MaxValue));
            }
            shortestDist[start.ID] = new NodeInfo(0, 0);


            int currNodeID = start.ID;
            openList.Add(currNodeID);

            //since we know that a path exist between start and end cluster, 
            //we can be sure this loop will break when a path is found.
            while (true) 
            {
                //add all neighbors into the open list
                foreach (var n in nodesToSearch[currNodeID].neighbors)
                {
                    if (closedList.Contains(n.Key) || !nodesToSearch.ContainsKey(n.Key))
                    {
                        continue;
                    }

                    if (!openList.Contains(n.Key))
                    {
                        openList.Add(n.Key);
                    }

                    //setting Parent
                    if (shortestDist[n.Key].Parent == Int32.MaxValue)
                    {
                        shortestDist[n.Key].Parent = currNodeID;
                    }
                    
                    //check if the path for neighbor would be shorter if the path went through current node
                    //if yes, set neighbor's new parent and new pathcost
                    else if (shortestDist[n.Key].PathCost + nodesToSearch[currNodeID].neighborDist[n.Key] < shortestDist[n.Key].PathCost)
                    {
                        shortestDist[n.Key].Parent = currNodeID;
                        shortestDist[n.Key].PathCost = shortestDist[n.Key].PathCost + nodesToSearch[currNodeID].neighborDist[n.Key];
                        continue;
                    }

                    //setting pathCost
                    if (shortestDist[n.Key].PathCost == Int32.MaxValue)
                    {
                        shortestDist[n.Key].PathCost = nodesToSearch[n.Key].neighborDist[currNodeID];// n.Value;
                    }
                    else
                    {
                        shortestDist[n.Key].PathCost += nodesToSearch[n.Key].neighborDist[currNodeID];
                    }
                }
                closedList.Add(currNodeID);
                //calculate F for all neighbors (G + H)
                Dictionary<int, double> F = new Dictionary<int, double>();
                foreach (var n in openList)
                {
                    F.Add(n, shortestDist[n].PathCost/*G(n)*/ + H_PRA(n, layer, end));
                }
                
                //choose the lowest F node (LFN)  (O(n))
                currNodeID = F.Aggregate((l, r) => l.Value < r.Value ? l : r).Key;

                //drop the LFN from the open list and add it to the closed list
                openList.Remove(currNodeID);
                closedList.Add(currNodeID);

                if (currNodeID == end.ID)
                {
                    break;
                }
            }

            sol.Add(currNodeID);
            //NOT counting pathcost here, since this is still a high-level abstraction

            int parentID = end.ID;
            while (parentID != start.ID)
            {
                parentID = shortestDist[parentID].Parent;
                //pathCost += shortestDist[parentID].PathCost;

                if (parentID == Int32.MaxValue)
                {
                    break;
                }
                sol.Add(parentID);
            }

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

            pathfinder.CancelAsync();
            invalidater.CancelAsync();
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
            while (openList.Count > 0 || gMap.EndNodeID != currNodeID)
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
                Dictionary<int, double> F = new Dictionary<int, double>();
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

            PathfindingSolution.Add(currNodeID);
            pathCost += shortestDist[currNodeID].PathCost;

            int parentID = currNodeID;//gMap.EndNodeID;
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
            invalidater.CancelAsync();
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
                    return 1 * (Math.Abs(gMap.Nodes[n].Location.X - gMap.Nodes[gMap.EndNodeID].Location.X) + Math.Abs(gMap.Nodes[n].Location.Y - gMap.Nodes[gMap.EndNodeID].Location.Y));

                case Heuristic.DiagonalShortcut:
                    int h = 0;
                    int dx = Math.Abs(gMap.Nodes[n].Location.X - gMap.Nodes[gMap.EndNodeID].Location.X);
                    int dy = Math.Abs(gMap.Nodes[n].Location.Y - gMap.Nodes[gMap.EndNodeID].Location.Y);
                    if (dx > dy)
                        h = 14 * dy + 10 * (dx - dy);
                    else
                        h = 14 * dx + 10 * (dy - dx);
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
                    int h = 0;
                    int dx = Math.Abs(gMap.Nodes[n].Location.X - end.X);
                    int dy = Math.Abs(gMap.Nodes[n].Location.Y - end.Y);
                    if (dx > dy)
                        h = 14 * dy + 10 * (dx - dy);
                    else
                        h = 14 * dx + 10 * (dy - dx);
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
                    int h = 0;
                    int dx = Math.Abs(layer.ClusterNodes[n].X - end.X);
                    int dy = Math.Abs(layer.ClusterNodes[n].Y - end.Y);
                    if (dx > dy)
                        h = 14 * dy + 10 * (dx - dy);
                    else
                        h = 14 * dx + 10 * (dy - dx);
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
            tb_pathOutput.Text = "";

            if (PathfindingSolution.Count == 0)
            {
                tb_pathOutput.Text = "No solution";
                l_pathCost.Text = "- - - ";
                return;
            }

            StringBuilder s = new StringBuilder();
            foreach (int id in PathfindingSolution)
            {
                s.Append(id + ", ");
                gMap.Nodes[id].BackColor = id != gMap.StartNodeID && id != gMap.EndNodeID ? ColorPalette.NodeColor_Path : ColorPalette.NodeTypeColor[gMap.Nodes[id].Type];
            }

            tb_pathOutput.Text = s.ToString();
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
        
        private void setSearchedBgColor(int currNodeID)
        {
            gMap.Nodes[currNodeID].BackColor = currNodeID != gMap.StartNodeID && currNodeID != gMap.EndNodeID ? ColorPalette.NodeColor_Visited : ColorPalette.NodeTypeColor[gMap.Nodes[currNodeID].Type];
        }
    }


}
