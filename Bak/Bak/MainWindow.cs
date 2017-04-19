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

            int width = mapContent[0].Length;
            int height = mapContent.Length;

            RemoveMapControls();

            gMap = new GridMap(this, width, height, mapContent);
            RedrawMap();
            editingModesButton_Click(b_nontraversable, null);

            BuildHPAClusters();
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
    }


}
