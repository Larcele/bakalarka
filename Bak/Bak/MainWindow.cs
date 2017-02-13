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

        public Panel mainPanel;

        List<int> PathfindingSolution = new List<int>();
        HashSet<int> searchedNodes = new HashSet<int>();
        string FilePath = "C:\\Users\\Lenovo\\Desktop\\primitive.gmap";

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
            OpenFileDialog fileDialog = new OpenFileDialog() { Title = "Open GMAP File", Filter = "GMAP files (*.gmap)|*.gmap" };
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

            this.mainPanel.Controls.Clear();
            gMap = new GridMap(this, width, height, mapContent);
            RedrawMap();
            editingModesButton_Click(b_nontraversable, null);
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

            switch ((string)c_selectedPathfinding.SelectedItem)
            {
                case "A*":
                    StartAstarSearch();
                    break;
                case "BackTrack":
                    StartBackTrackSearch();
                    break;
                case "Dijkstra":
                    StartDijkstraSearch();
                    break;
            }
        }

        private void StartDijkstraSearch()
        {
            stopWatch = new Stopwatch();
            stopWatch.Start();

            Dictionary<int, NodeInfo> shortestDistances = new Dictionary<int, NodeInfo>();

            //init the distances to each node from the starting node
            foreach (var nodeID in gMap.Nodes.Keys)
            {
                shortestDistances.Add(nodeID, new NodeInfo(Int32.MaxValue, Int32.MaxValue));
            }
            shortestDistances[gMap.StartNodeID] = new NodeInfo(0, 0);

            int currentNodeID = gMap.StartNodeID;
            while (currentNodeID > -1)
            {
                searchedNodes.Add(currentNodeID);
                foreach (var neighbor in gMap.Nodes[currentNodeID].susedneID)
                {
                    if (gMap.Nodes[neighbor].Type != GameMap.NodeType.Obstacle && !searchedNodes.Contains(neighbor))
                    {
                        //+1 since that is the cell's edge value
                        if (shortestDistances[currentNodeID].PathCost + 1 < shortestDistances[neighbor].PathCost)
                        {
                            shortestDistances[neighbor].PathCost = shortestDistances[currentNodeID].PathCost + 1;
                            shortestDistances[neighbor].Parent = currentNodeID;
                            //update also the path 
                        }
                    }
                }
                currentNodeID = closestNeighbor(shortestDistances);
            }
            stopWatch.Stop();
            tb_elapsedTime.Text = stopWatch.Elapsed.ToString();

            foreach (var id in searchedNodes)
            {
                gMap.Nodes[id].BackColor = id != gMap.StartNodeID && id != gMap.EndNodeID ? ColorPalette.NodeColor_Visited : ColorPalette.NodeTypeColor[gMap.Nodes[id].Type];
            }

            int parentID = gMap.EndNodeID;
            while (parentID != gMap.StartNodeID)
            {
                parentID = shortestDistances[parentID].Parent;
                gMap.Nodes[parentID].BackColor = parentID != gMap.StartNodeID && parentID != gMap.EndNodeID ? ColorPalette.NodeColor_Path : ColorPalette.NodeTypeColor[gMap.Nodes[parentID].Type];
                
            }
        }

        private int closestNeighbor(Dictionary<int, NodeInfo> shortestDistances)
        {
            var nonVisited = shortestDistances.Where(node => !searchedNodes.Contains(node.Key) && node.Value.PathCost != Int32.MaxValue);

            int smallestSeen = Int32.MaxValue;
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

        private void StartAstarSearch()
        {
            MessageBox.Show("To Be Implemented..");
            //throw new NotImplementedException();
        }

        private void StartBackTrackSearch()
        {
            stopWatch = new Stopwatch();
            stopWatch.Start();

            List<int> path = new List<int>();
            backtrackMap(path, gMap.Nodes[gMap.StartNodeID]);
            stopWatch.Stop();
            tb_elapsedTime.Text = stopWatch.Elapsed.ToString();

            tb_pathOutput.Text = "";
            foreach (int id in PathfindingSolution)
            {
                tb_pathOutput.Text += id + ",";
                gMap.Nodes[id].BackColor = id != gMap.StartNodeID && id != gMap.EndNodeID ? ColorPalette.NodeColor_Path : ColorPalette.NodeTypeColor[gMap.Nodes[id].Type];
            }
        }

        private void backtrackMap(List<int> path, Node current)
        {
            //not needed for pathfinding; remembered for future map refreshing, 
            //prevents accessing redundant, non-changed nodes.
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
            foreach (int nodeID in current.susedneID)
            {
                if (gMap.Nodes[nodeID].Type != GameMap.NodeType.Obstacle && !path.Contains(nodeID))
                {
                    backtrackMap(path, gMap.Nodes[nodeID]);
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
    }
}
