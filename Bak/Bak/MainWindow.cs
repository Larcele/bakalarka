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

        public Panel mainPanel;

        BackgroundWorker pathfinder;

        List<int> PathfindingSolution = new List<int>();
        HashSet<int> searchedNodes = new HashSet<int>();
        string FilePath = "GMaps\\simple.gmap";

        GameMap gMap;
        public MainWindow()
        {
            InitializeComponent();
            this.MaximizeBox = false;
            c_selectedPathfinding.SelectedIndex = 0;
            pathLed.BackColor = ColorPalette.NodeColor_Path;
            visitedLed.BackColor = ColorPalette.NodeColor_Visited;

            mainPanel = panel;
            mainPanel.Width = 600;
            mainPanel.Height = 600;

            mainPanel.AutoScroll = true;
            
            gMap = new GridMap(this, 5, 5, FilePath);
            this.Text = FilePath;

            RedrawMap();
            editingModesButton_Click(b_nontraversable, null);
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
                string[] filelines = File.ReadAllLines(fileDialog.FileName);
                LoadNewMap(fileDialog.FileName, filelines);
            }
        }

        public void LoadNewMap(string fileName, string[] mapContent)
        {
            int width = mapContent[0].Length;
            int height = mapContent.Length;

            RemoveMapControls();

            gMap = new GridMap(this, width, height, mapContent);
            RedrawMap();
            editingModesButton_Click(b_nontraversable, null);
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
            PathfindingSolution.Clear();
            searchedNodes.Clear();

            if (gMap.StartNodeID == -1 || gMap.EndNodeID == -1)
            {
                MessageBox.Show("Please set start and end node on map before search.");
                return;
            }

            pathfinder = new BackgroundWorker();
            pathfinder.WorkerSupportsCancellation = true;


            stopWatch = new Stopwatch();
            stopWatch.Start();
            switch ((string)c_selectedPathfinding.SelectedItem)
            {
                case "A*":
                    pathfinder.DoWork += StartAstarSearch;
                    pathfinder.RunWorkerAsync();

                    break;
                case "BackTrack":
                    pathfinder.DoWork += StartBackTrackSearch;
                    pathfinder.RunWorkerAsync();
                    StartBackTrackSearch(null, null);

                    break;
                case "Dijkstra":
                    pathfinder.DoWork += StartDijkstraSearch;
                    pathfinder.RunWorkerAsync();
                    break;
            }
            stopWatch.Stop();
            tb_elapsedTime.Text = stopWatch.Elapsed.ToString();
            
            printSolution();
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
                    break;
                }

                gMap.Nodes[parentID].BackColor = parentID != gMap.StartNodeID && parentID != gMap.EndNodeID ? ColorPalette.NodeColor_Path : ColorPalette.NodeTypeColor[gMap.Nodes[parentID].Type];
                PathfindingSolution.Add(parentID);
            }
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
            while (true)
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
                    //path would be shorter for the neighbor if the path went through current node
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
                //calculate F for all neighbors (G + H)  H = 1*(abs(currentX-targetX) + abs(currentY-targetY))
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
            return 1 * (Math.Abs(gMap.Nodes[n].Location.X - gMap.Nodes[gMap.EndNodeID].Location.X) + Math.Abs(gMap.Nodes[n].Location.Y - gMap.Nodes[gMap.EndNodeID].Location.Y));
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
                return;
            }

            foreach (int id in PathfindingSolution)
            {
                tb_pathOutput.Text += id + ",";
                gMap.Nodes[id].BackColor = id != gMap.StartNodeID && id != gMap.EndNodeID ? ColorPalette.NodeColor_Path : ColorPalette.NodeTypeColor[gMap.Nodes[id].Type];
            }
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
            foreach (int nodeID in searchedNodes)
            {
                gMap.Nodes[nodeID].BackColor = ColorPalette.NodeTypeColor[gMap.Nodes[nodeID].Type];
            }
        }

        private void MainWindow_Resize(object sender, EventArgs e)
        {
            foreach (Control c in this.Controls)
            {
                
            }
        }
    }
}
