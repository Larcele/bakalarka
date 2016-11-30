using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bak
{
    public abstract class GameMap
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public NodeCollection GraphNodes;
        public GameMap(int width, int height)
        {
            Width = width;
            Height = height;
        }


    }
}
