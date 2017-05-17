using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Bak
{
    public abstract class GameMap
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public MainWindow ParentWindow;
        public NodeCollection Nodes;
        public int StartNodeID { get; set; }
        public int EndNodeID { get; set; }
        public int NodeIdAssignment { get; set; }
        public NodeType EditingNodeMode;

        public Dictionary<NodeType, char> NodeTypeMapChar = new Dictionary<NodeType, char>
        {
            {NodeType.Traversable, '1' },
            {NodeType.Obstacle, '0' },
            {NodeType.StartPosition, 's' },
            {NodeType.EndPosition, 'e' }
        };

        public Dictionary<char, NodeType> CharToNodeType = new Dictionary<char, NodeType>
        {
            {'S', NodeType.Obstacle },
            {'T', NodeType.Obstacle },
            {'.', NodeType.Traversable },
            {'@', NodeType.Obstacle },
            {'1', NodeType.Traversable },
            {'0', NodeType.Obstacle },
            {'s', NodeType.StartPosition },
            {'e', NodeType.EndPosition }
        };

        public GameMap(int width, int height, MainWindow window)
        {
            NodeIdAssignment = 0;
            ParentWindow = window;
            Width = width;
            Height = height;
            StartNodeID = -1;
            EndNodeID = -1;
            Nodes = new NodeCollection();
        }

        public abstract void InitNodes(MainWindow w);

        public abstract void DrawAllNodes();


        public enum NodeType
        {
            Obstacle,
            Traversable,
            StartPosition,
            EndPosition
        }
    }
}
