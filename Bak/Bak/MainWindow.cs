using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
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
        public Panel mainPanel;

        List<int> PathfindingSolution = new List<int>();
        List<int> searchedNodes = new List<int>();
        string FilePath = "C:\\Users\\Lenovo\\Desktop\\primitive.gmap";

        GameMap gameMap;
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
            
            gameMap = new GridMap(this, 5, 5, FilePath);
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
            int[] tmpMap = cloneMapKeysAndSort();
            int counter = 0;
            int linePosition = 0;

            string[] res = new string[gameMap.Height];
            foreach (var nodeID in tmpMap)
            {
                if (counter == gameMap.Width)
                {
                    res[linePosition] = lines[linePosition].ToString();
                    linePosition++;
                    counter = 0;
                }
                Node n = gameMap.GraphNodes[nodeID];
                lines[linePosition].Append(gameMap.NodeTypeMapChar[n.Type]);
                counter++;
            }
            //last line
            res[linePosition] = lines[linePosition].ToString();
            return res;
        }

        private int[] cloneMapKeysAndSort()
        {
            var mapKeys = gameMap.GraphNodes.Keys;
            int[] tmpMap = new int[mapKeys.Count];
            mapKeys.ToList().CopyTo(tmpMap);
            Array.Sort(tmpMap);
            return tmpMap;
        }

        private StringBuilder[] initSaveFile()
        {
            StringBuilder[] res = new StringBuilder[gameMap.Height];
            for (int i = 0; i < gameMap.Height; ++i)
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
            gameMap = new GridMap(this, width, height, mapContent);
            RedrawMap();
            editingModesButton_Click(b_nontraversable, null);
        }

        private void RedrawMap()
        {
            gameMap.DrawAllNodes();
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
                    gameMap.EditingNodeMode = GameMap.NodeType.Traversable;
                    break;

                case "!traversable":
                    gameMap.EditingNodeMode = GameMap.NodeType.Obstacle;
                    break;

                case "end":
                    gameMap.EditingNodeMode = GameMap.NodeType.EndPosition;
                    break;

                case "start":
                    gameMap.EditingNodeMode = GameMap.NodeType.StartPosition;
                    break;
            }
            button.BackColor = ColorPalette.NodeTypeColor[gameMap.EditingNodeMode];
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

            switch ((string)c_selectedPathfinding.SelectedItem)
            {
                case "A*":
                    MessageBox.Show("To be implemented soon");
                    break;
                case "BackTrack":
                    StartBackTrackSearch();
                    break;
            }
        }

        private void StartBackTrackSearch()
        {
            if (gameMap.StartNodeID == -1 || gameMap.EndNodeID == -1)
            {
                MessageBox.Show("Please set start and end node on map before search.");
                return;
            }
            List<int> path = new List<int>();
            backtrackMap(path, gameMap.GraphNodes[gameMap.StartNodeID]);
            tb_pathOutput.Text = "";
            foreach (int id in PathfindingSolution)
            {
                tb_pathOutput.Text += id + ",";
                gameMap.GraphNodes[id].BackColor = id != gameMap.StartNodeID && id != gameMap.EndNodeID ? ColorPalette.NodeColor_Path : ColorPalette.NodeTypeColor[gameMap.GraphNodes[id].Type];
            }
        }

        private void backtrackMap(List<int> path, Node current)
        {
            //not needed for pathfinding; remembered for future map refreshing, 
            //prevents accessing redundant, non-changed nodes.
            searchedNodes.Add(current.ID);

            path.Add(current.ID);
            gameMap.GraphNodes[current.ID].BackColor = (gameMap.GraphNodes[current.ID].Type != GameMap.NodeType.EndPosition && gameMap.GraphNodes[current.ID].Type != GameMap.NodeType.StartPosition) ? ColorPalette.NodeColor_Visited : ColorPalette.NodeTypeColor[gameMap.GraphNodes[current.ID].Type];
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
                if (gameMap.GraphNodes[nodeID].Type != GameMap.NodeType.Obstacle && !path.Contains(nodeID))
                {
                    backtrackMap(path, gameMap.GraphNodes[nodeID]);
                }
            }
            path.Remove(current.ID);
        }

        private void b_mapRefresh_Click(object sender, EventArgs e)
        {
            foreach (int nodeID in searchedNodes)
            {
                gameMap.GraphNodes[nodeID].BackColor = ColorPalette.NodeTypeColor[gameMap.GraphNodes[nodeID].Type];
            }
        }
    }
}
