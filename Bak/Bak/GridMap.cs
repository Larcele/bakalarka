using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Bak
{
    public class GridMap : GameMap
    {
        int SquareSize;
        public GridMap(MainWindow window, int w, int h) : base(w, h, window)
        {
            SquareSize = w > h ? window.mainPanel.Width / w : window.mainPanel.Height / h;
            InitNodes(window);
        }
        public GridMap(MainWindow window, int w, int h, string filename) : base(w,h,window)
        {
            string[] mapContent = File.ReadAllLines(filename);
            w = mapContent[0].Length;
            h = mapContent.Length;
            Width = w;
            Height = h;
            SquareSize = w > h ? window.mainPanel.Width / w : window.mainPanel.Height / h;
            InitNodes(window, mapContent);
        }

        public GridMap(MainWindow window, int w, int h, string[] mapContent) : base(w, h, window)
        {
            SquareSize = w > h ? window.mainPanel.Width / w : window.mainPanel.Height / h;
            InitNodes(window, mapContent);
        }

        public override void DrawAllNodes()
        {
            foreach (var node in GraphNodes.Values)
            {
                MW.mainPanel.Controls.Add(node);
                if (node.ID > 2000)
                    break;
            }
        }

        public override void InitNodes(MainWindow w)
        {
            for (int i = 0; i < Height; ++i)
            {
                for (int j = 0; j < Width; ++j)
                {
                    Node n = new Node(this, j * SquareSize, i * SquareSize, NodeIdAssignment, SquareSize, NodeType.Traversable);
                    NodeIdAssignment++;
                    GraphNodes.MapByNodeID(n);
                }
            }
            InitEdges();
        }

        public void InitNodes(MainWindow w, string[] mapContent)
        {
            for (int i = 0; i < Height; ++i)
            {
                for (int j = 0; j < Width; ++j)
                {
                    NodeType type = ResolveNodeType(mapContent[i][j]);
                    Node n = new Node(this, j * SquareSize, i * SquareSize, NodeIdAssignment, SquareSize, type);
                    NodeIdAssignment++;
                    GraphNodes.MapByNodeID(n);
                }
            }
            InitEdges();
        }

        private void InitEdges()
        {
            foreach (Node n in GraphNodes.Values)
            {
                if (GraphNodes.ContainsKey(n.ID - 1) && GraphNodes[n.ID - 1].Location.Y == GraphNodes[n.ID].Location.Y)
                    n.susedneID.Add(n.ID - 1);

                if(GraphNodes.ContainsKey(n.ID + 1) && GraphNodes[n.ID + 1].Location.Y == GraphNodes[n.ID].Location.Y)
                    n.susedneID.Add(n.ID + 1);

                if (GraphNodes.ContainsKey(n.ID + Width))
                    n.susedneID.Add(n.ID + Width);

                if (GraphNodes.ContainsKey(n.ID - Width))
                    n.susedneID.Add(n.ID - Width);
            }
        }

        private NodeType ResolveNodeType(char c)
        {
            NodeType type = CharToNodeType[c];

            if (type == NodeType.StartPosition)
            {
                //if there was previously read a start node from the save file
                if (StartNodeID != -1) { //set node's type to traversable instead
                    type = NodeType.Traversable;
                }
                else { //otherwise just set currently creating node as start node
                    StartNodeID = NodeIdAssignment;
                }
            }
            else if (type == NodeType.EndPosition)
            {
                //if there was previously read an end node from the save file
                if (EndNodeID != -1)
                { //set node's type to traversable instead
                    type = NodeType.Traversable;
                }
                else { //otherwise just set currently creating node as end node
                    EndNodeID = NodeIdAssignment;
                }
            }
            return type;
        }

    }
}
