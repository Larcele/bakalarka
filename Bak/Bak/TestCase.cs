using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bak
{
    public class TestCase
    {
        string mapName;
        Dictionary<int, GameMap.NodeType> nodeSwaps = new Dictionary<int, GameMap.NodeType>();
        int startPos;
        int endPos;

        public TestCase(string mapName, int start, int end)
        {
            this.mapName = mapName;
            this.startPos = start;
            this.endPos = end;
        }
    }
}
