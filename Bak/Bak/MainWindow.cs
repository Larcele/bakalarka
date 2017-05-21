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
        int agentSpeed = 600; //ms
        int counter = 0;
        bool agentTerminate = false;
        int testPCount = 0;

        private Stopwatch stopWatch;
        private Node lastNodeInfo;
        private float pathCost = 0;
        private Heuristic heuristic;
        private string CurrentMapName = "";
        public PictureBox mainPanel;
        int HPACsize = 10;
        
        Dictionary<int, AbstractionLayer> HierarchicalGraph = new Dictionary<int, AbstractionLayer>();

        BackgroundWorker pathfinder;
        BackgroundWorker invalidater;

        System.Threading.Timer agent;
        System.Threading.Timer testRunner;

        List<int> PathfindingSolution = new List<int>();
        HashSet<int> searchedNodes = new HashSet<int>();
        string FilePath = Path.Combine(Directory.GetCurrentDirectory(), "GMaps", "map04.gmap");

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

            MinimumSize = new Size(800, 600);

            MapTests = TestCaseCreator.GetAllTestCases();

            this.Width = 1120;
            this.Height = 700;

            c_selectedPathfinding.SelectedIndex = 0;
            pathLed.BackColor = ColorPalette.NodeColor_Path;
            visitedLed.BackColor = ColorPalette.NodeColor_Visited;

            mainPanel = pictureBox1;
            mainPanel.Width = 600;
            mainPanel.Height = 600;
            
            mainPanel.Paint += MainPanel_Paint;
            mainPanel.MouseDown += MainPanel_Click;

            
            gMap = new GridMap(this, 5, 5, FilePath);
            SetMapSize();
            this.Text = FilePath;

            RedrawMap();
            editingModesButton_Click(b_nontraversable, null);

            BuildHPAClusters();
            BuildPRAbstractMap();
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
                agentTerminate = true;
                string[] filelines = File.ReadAllLines(fileDialog.FileName);
                LoadNewMap(fileDialog.FileName, filelines);
                this.Text = fileDialog.FileName;
                SetMapSize();
                MainWindow_Resize(null, null);

                CurrentMapName = Path.GetFileName(fileDialog.FileName);
                RefreshMaptests();

            }
        }

        private void RefreshMaptests()
        {
            cb_mapTests.Items.Clear();

            //add a default empty item
            cb_mapTests.Items.Add("No test selected");

            if (MapTests.ContainsKey(CurrentMapName))
            {
                List<TestCase> tests = MapTests[CurrentMapName];
                foreach (var t in tests)
                {
                    cb_mapTests.Items.Add(t);
                }
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
            testShouldRun = false;
            pathCost = 0;
            testPCount = 0;
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
                    testShouldRun = true;
                    heuristic = Heuristic.Manhattan;
                    pathfinder.DoWork += StartPRAstarSearch;
                    pathfinder.RunWorkerCompleted += Pathfinder_RunWorkerCompleted;
                    pathfinder.RunWorkerAsync();

                    invalidater.DoWork += Invalidater_DoWork;
                    invalidater.RunWorkerAsync();
                    break;

                case "PRA* (Diagonal shortcut)":
                    testShouldRun = true;
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
            //reset counter
            counter = 0;
            agentTerminate = false;
            agent = new System.Threading.Timer(MoveAgent, null, agentSpeed, Timeout.Infinite);
        }

        private void MoveAgent(Object state)
        {
            if (agentPosition >= PathfindingSolution.Count || agentTerminate)
            {
                invalidater.CancelAsync();
                return;
            }
            if (selectedTest != null && testShouldRun && selectedTest.triggerStep == counter)
            {
                //notify for test triggering
                StartTestExecution();
            }

            int id = PathfindingSolution[agentPosition];
            gMap.Nodes[id].BackColor = ColorPalette.NodeColor_Agent;

            if (agentPosition > 0)
            {
                int prev = PathfindingSolution[agentPosition - 1];
                gMap.Nodes[prev].BackColor = ColorPalette.NodeColor_Path;
            }

            agentPosition++;
            counter++;

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
        }
        
        private void DisablePathFindingdControls()
        {
            b_startPathFinding.Enabled = false;
            cb_mapTests.Enabled = false;
        }

        private void EnablePathFindingdControls()
        {
            b_startPathFinding.Enabled = true;
            cb_mapTests.Enabled = true;
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

        
        private void AddToPathfSol(List<int> partialPath)
        {
            //since A* traces the path from the end to start by assigned parent nodes, we need to reverse it.
            for (int i = partialPath.Count - 1; i >= 0; i--)
            {
                if (PathfindingSolution.Count == 0)
                {
                    PathfindingSolution.Add(partialPath[i]);
                    //set the bg color of the path node
                    gMap.Nodes[partialPath[i]].BackColor = ColorPalette.NodeColor_Path;
                }
                else
                {
                    if (PathfindingSolution[PathfindingSolution.Count - 1] != partialPath[i])
                    {
                        PathfindingSolution.Add(partialPath[i]);
                        //set the bg color of the path node
                        gMap.Nodes[partialPath[i]].BackColor = ColorPalette.NodeColor_Path;
                    }
                }
            }
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
            stopWatch.Stop();

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
            List<int> closedSet = new List<int>();
            List<int> openSet = new List<int>();

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
                closedSet.Add(currNode); //"added" to closedList

                foreach (var neighbor in gMap.Nodes[currNode].Neighbors)
                {
                    // Ignore the neighbor which is already evaluated or it is non-traversable.
                    if (gMap.Nodes[neighbor.Key].Type == GameMap.NodeType.Obstacle || closedSet.Contains(neighbor.Key))
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
                    reversedPath.Insert(reversedPath.Count-1, currNode);
                }
                pathCost = gScore[gMap.EndNodeID];
                AddToPathfSol(reversedPath);
            }

            stopWatch.Stop();
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

        private void StartBackTrackSearch(object sender, DoWorkEventArgs e)
        {
            List<int> path = new List<int>();
            backtrackMap(path, gMap.Nodes[gMap.StartNodeID]);
            pathfinder.CancelAsync();
            invalidater.CancelAsync();
        }

        private void printSolution()
        {
            ThreadHelperClass.SetText(this, tb_pathLength, PathfindingSolution.Count + testPCount + "");

            if (PathfindingSolution.Count == 0)
            {
                ThreadHelperClass.SetText(this, tb_pathLength, "No solution");
                ThreadHelperClass.SetText(this, l_pathCost, "- - - ");
                return;
            }
            
            foreach (int id in PathfindingSolution)
            {
                gMap.Nodes[id].BackColor = id != gMap.StartNodeID && id != gMap.EndNodeID ? ColorPalette.NodeColor_Path : ColorPalette.NodeTypeColor[gMap.Nodes[id].Type];
            }
            ThreadHelperClass.SetText(this, l_pathCost, pathCost.ToString());
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
            Random r = new Random();
            foreach (AbstractionLayer a in HierarchicalGraph.Values)
            {
                foreach (Cluster c in a.Clusters.Values)
                {
                    Color col = Color.FromArgb(r.Next(0, 256), r.Next(0, 256), r.Next(0, 256));
                    foreach (int nodeID in c.InnerNodes)
                    {
                       // if (gMap.Nodes[nodeID].IsTraversable())
                       // { gMap.Nodes[nodeID].BackColor = col; }
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

        private void cb_mapTests_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cb_mapTests.SelectedIndex == 0)
            {
                selectedTest = null;
                return;
            }
            selectedTest = ((TestCase)cb_mapTests.SelectedItem);

            //      set start/end nodes
            Node prevStart = gMap.Nodes[gMap.StartNodeID];
            Node prevEnd = gMap.Nodes[gMap.EndNodeID];

            prevStart.Type = GameMap.NodeType.Traversable;
            prevEnd.Type = GameMap.NodeType.Traversable;
            prevStart.BackColor = ColorPalette.NodeColor_Traversable;
            prevEnd.BackColor = ColorPalette.NodeColor_Traversable;

            gMap.StartNodeID = selectedTest.startPos;
            gMap.EndNodeID = selectedTest.endPos;

            Node currStart = gMap.Nodes[gMap.StartNodeID];
            Node currEnd = gMap.Nodes[gMap.EndNodeID];
            currStart.Type = GameMap.NodeType.StartPosition;
            currEnd.Type = GameMap.NodeType.EndPosition;
            currStart.BackColor = ColorPalette.NodeColor_Start;
            currEnd.BackColor = ColorPalette.NodeColor_End;

            // -------------------------

            mainPanel.Invalidate();

        }
    }
}
