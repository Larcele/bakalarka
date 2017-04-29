using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Bak
{
    public partial class Node : UserControl
    {
        public int ID { get; private set; }
        public GameMap.NodeType Type { get; set; }

        /// <summary>
        /// neighboring nodes. Key -> the neigboring node's ID; Value -> edge value from Node(this) and the neighboring node.
        /// </summary>
        public Dictionary<int, float> Neighbors;
        
        public GameMap ParentMap;
        
        #region border override
        private BorderStyle border;

        public new BorderStyle BorderStyle
        {
            get { return border; }
            set
            {
                border = value;
                Invalidate();
            }
        }
        #endregion

        public Node(GameMap parent, int x, int y, int id, int size, GameMap.NodeType type)
        {
            InitializeComponent();
            
            Location = new Point(x, y);
            ID = id;
            Size = new Size(size, size);

           // Rec = new Rectangle(Location, Size);
           // Rec.Fill = 

            BackColor = ColorPalette.NodeTypeColor[type];
            base.BorderStyle = BorderStyle.None;
            this.BorderStyle = BorderStyle.FixedSingle;

            ParentMap = parent;
            Type = type;
            Neighbors = new Dictionary<int, float>();
            
            this.MouseDown += Node_MouseDown;
        }

        internal bool IsHit(int x, int y)
        {
            Rectangle rc = new Rectangle(this.Location, this.Size);
            return rc.Contains(x, y);
        }

        public void InvokeNodeClick(MouseEventArgs e)
        {
            Node_MouseDown(null, e);
        }

        private void Node_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Node_Click();
            }
            else if (e.Button == MouseButtons.Right)
            {
                ParentMap.ParentWindow.FillNodeInfo(this);
            }
        }

        private void Node_Click()
        {
            if (ParentMap.EditingNodeMode == GameMap.NodeType.EndPosition)
            {
                if (Type != GameMap.NodeType.Traversable)
                {
                    MessageBox.Show("You can place End point only on traversable map points.");
                    return;
                }

                if (ParentMap.EndNodeID != -1)
                {
                    Node previousEndNode = ParentMap.Nodes[ParentMap.EndNodeID];
                    previousEndNode.Type = GameMap.NodeType.Traversable;
                    previousEndNode.BackColor = ColorPalette.NodeTypeColor[previousEndNode.Type];
                }
                ParentMap.EndNodeID = this.ID;
            }
            else if (ParentMap.EditingNodeMode == GameMap.NodeType.StartPosition)
            {
                if (Type != GameMap.NodeType.Traversable)
                {
                    MessageBox.Show("You can place Start point only on traversable map points.");
                    return;
                }
                if (ParentMap.StartNodeID != -1)
                {
                    Node previousStartNode = ParentMap.Nodes[ParentMap.StartNodeID];
                    previousStartNode.Type = GameMap.NodeType.Traversable;
                    previousStartNode.BackColor = ColorPalette.NodeTypeColor[previousStartNode.Type];
                }
                ParentMap.StartNodeID = this.ID;
            }
            else if (ParentMap.EditingNodeMode == GameMap.NodeType.Obstacle || ParentMap.EditingNodeMode == GameMap.NodeType.Traversable)
            {
                if (ID == ParentMap.StartNodeID)
                {
                    ParentMap.StartNodeID = -1;
                }
                else if (ID == ParentMap.EndNodeID)
                {
                    ParentMap.EndNodeID = -1;
                }
            }

            Type = ParentMap.EditingNodeMode;
            BackColor = ColorPalette.NodeTypeColor[Type];
        }

        internal string PrintNeighbors()
        {
            string res = "{ ";
            int i = 0;
            foreach (var n in Neighbors)
            {
                res += n.Key;
                i++;
                if (i < Neighbors.Count)
                {
                    res += ", ";
                }
            }
            res += "}";
            return res;
        }

        public bool IsNeighbor(int n)
        {
            return this.Neighbors.ContainsKey(n);
        }

        public bool AreNeighbors(params int[] vals)
        {
            foreach (var n in vals)
            {
                if (!this.Neighbors.ContainsKey(n))
                {
                    return false;
                }
            }
            return true;
        }
        
        public bool IsTraversable()
        {
            return Type != GameMap.NodeType.Obstacle; //all traversable and also end/start points count as traversable
        }

        public void PaintNode(PaintEventArgs e, Rectangle r)
        {
            base.OnPaint(e);

            if (this.BorderStyle == BorderStyle.FixedSingle)
                ControlPaint.DrawBorder(e.Graphics, r, Color.FromArgb(40, 40, 40), ButtonBorderStyle.Solid);

        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (this.BorderStyle == BorderStyle.FixedSingle)
                ControlPaint.DrawBorder(e.Graphics, this.ClientRectangle, Color.FromArgb(40, 40, 40), ButtonBorderStyle.Solid);
        }
    }
    public class NodeCollection : Dictionary<int, Node>
    {
        public NodeCollection()
        {
        }

        public void MapByNodeID(Node n)
        {
            this.Add(n.ID, n);
        }
    }
}
