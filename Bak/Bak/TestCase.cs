using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bak
{
    public class TestCase
    {
        public string TestName;
        public string MapName;
        public Dictionary<int, GameMap.NodeType> nodeSwaps = new Dictionary<int, GameMap.NodeType>();
        public int startPos;
        public int endPos;

        public int triggerStep;

        public TestCase(string testname, string mapName, int start, int end, int trigger)
        {
            this.TestName = testname;
            this.MapName = mapName;
            this.startPos = start;
            this.endPos = end;
            triggerStep = trigger;
        }

        public override string ToString()
        {
            return TestName;
        }
    }
}
