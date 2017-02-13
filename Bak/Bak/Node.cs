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
        public List<int> susedneID;

        ToolTip t = new ToolTip();

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

            BackColor = ColorPalette.NodeTypeColor[type];
            base.BorderStyle = BorderStyle.None;
            this.BorderStyle = BorderStyle.FixedSingle;

            ParentMap = parent;
            Type = type;
            susedneID = new List<int>();

            this.Click += Node_Click;
            this.MouseEnter += Node_MouseEnter;
        }

        private void Node_MouseEnter(object sender, EventArgs e)
        {
            t.Show(ID+"", this, 3000);
        }

        private void Node_Click(object sender, EventArgs e)
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
