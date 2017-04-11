using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bak
{
    internal static class ColorPalette
    {
        internal static Color NodeColor_Path = Color.FromArgb(188, 255, 100);
        internal static Color NodeColor_Traversable = Color.FromArgb(84, 191, 84);
        internal static Color NodeColor_Visited = Color.FromArgb(20, 70, 20);
        internal static Color NodeColor_Obstacle = Color.FromArgb(95, 81, 41);
        internal static Color NodeColor_Start = Color.FromArgb(210, 35, 35);
        internal static Color NodeColor_End = Color.FromArgb(242, 196, 73); 

        internal static Dictionary<GameMap.NodeType, Color> NodeTypeColor = new Dictionary<GameMap.NodeType, Color>
        {
            {GameMap.NodeType.Traversable, NodeColor_Traversable },
            {GameMap.NodeType.Obstacle, NodeColor_Obstacle },
            {GameMap.NodeType.StartPosition, NodeColor_Start },
            {GameMap.NodeType.EndPosition, NodeColor_End }
        };
    }
}
