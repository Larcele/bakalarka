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
            if (SquareSize < 10)
            {
                SquareSize = 10;
            }
            InitNodes(window, mapContent);
        }

        public override void DrawAllNodes()
        {
            foreach (var node in Nodes.Values)
            {
                ParentWindow.mainPanel.Controls.Add(node);
                if (node.ID > 5000)
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
                    Nodes.MapByNodeID(n);
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
                    Nodes.MapByNodeID(n);
                }
            }
            InitEdges();
        }

        private void InitEdges()
        {
            foreach (Node n in Nodes.Values)
            {
                // --------- Standart neighbours

                //left neighbour
                if (Nodes.ContainsKey(n.ID - 1) && SameRow(n.ID - 1, n.ID))
                    n.Neighbors.Add(n.ID - 1, 1);

                //right neighbour
                if (Nodes.ContainsKey(n.ID + 1) && SameRow(n.ID + 1, n.ID))
                    n.Neighbors.Add(n.ID + 1, 1);

                //bottom neighbour
                if (Nodes.ContainsKey(n.ID + Width))
                    n.Neighbors.Add(n.ID + Width, 1);

                //upper neighbour
                if (Nodes.ContainsKey(n.ID - Width))
                    n.Neighbors.Add(n.ID - Width, 1);

                // --------- "Cross" neighbours
                
                //bottom-right neighbour
                if (Nodes.ContainsKey(n.ID + Width + 1) && SameRow(n.ID + Width + 1, n.ID + Width))
                    n.Neighbors.Add(n.ID + Width + 1, 1.4f);

                //bottom-left neighbour
                if (Nodes.ContainsKey(n.ID + Width - 1) && SameRow(n.ID + Width - 1, n.ID + Width))
                    n.Neighbors.Add(n.ID + Width - 1, 1.4f);

                //upper-right neighbour
                if (Nodes.ContainsKey(n.ID - Width + 1) && SameRow(n.ID - Width + 1, n.ID - Width))
                    n.Neighbors.Add(n.ID - Width + 1, 1.4f);

                //upper-left neighbour
                if (Nodes.ContainsKey(n.ID - Width - 1) && SameRow(n.ID - Width - 1, n.ID - Width))
                    n.Neighbors.Add(n.ID - Width - 1, 1.4f);
            }
        }

        /// <summary>
        /// Checks if two neighboring nodes are on the same row
        /// </summary>
        /// <param name="id1">neighbor node 1 id</param>
        /// <param name="id2">neighbor node 2 id</param>
        /// <returns></returns>
        private bool SameRow(int id1, int id2)
        {
            if (Nodes.ContainsKey(id1) && Nodes.ContainsKey(id2))
            {
                return Nodes[id1].Location.Y == Nodes[id2].Location.Y;
            }
            return false; 
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
