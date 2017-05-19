using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bak
{
    public static class TestCaseCreator
    {
        public static Dictionary<string, List<TestCase>> GetAllTestCases()
        {
            Dictionary<string, List<TestCase>> res = new Dictionary<string, List<TestCase>>();

            ///--------------------------------------------------------------------------------

            TestCase t_map08_corridor = new TestCase("New Corridor", "map08.gmap", 3933, 4087, 15);
            t_map08_corridor.nodeSwaps = new Dictionary<int, GameMap.NodeType>()
            {
                { 4176, GameMap.NodeType.Traversable}, {4177, GameMap.NodeType.Traversable},
                { 4178, GameMap.NodeType.Traversable}, {4179, GameMap.NodeType.Traversable},
                { 4180, GameMap.NodeType.Traversable}, {4181, GameMap.NodeType.Traversable},
                { 4182, GameMap.NodeType.Traversable}, {4082, GameMap.NodeType.Traversable},
                { 4083, GameMap.NodeType.Traversable}, {4084, GameMap.NodeType.Traversable},
                { 4085, GameMap.NodeType.Traversable}, {4086, GameMap.NodeType.Traversable}
            };
            if (res.ContainsKey(t_map08_corridor.MapName))
            {
                res[t_map08_corridor.MapName].Add(t_map08_corridor);
            }
            else
            {
                res.Add(t_map08_corridor.MapName, new List<TestCase>() { t_map08_corridor });
            }

            ///--------------------------------------------------------------------------------

            TestCase t_map07_tun = new TestCase("TunnelDown-late", "map07.gmap", 1224, 6164, 33);
            t_map07_tun.nodeSwaps = new Dictionary<int, GameMap.NodeType>()
            {
                {5363, GameMap.NodeType.Traversable},
                { 5463, GameMap.NodeType.Traversable},
                { 5563, GameMap.NodeType.Traversable},
                { 5663, GameMap.NodeType.Traversable},
                { 5763, GameMap.NodeType.Traversable},
                { 5863, GameMap.NodeType.Traversable}
            };
            if (res.ContainsKey(t_map07_tun.MapName))
            {
                res[t_map07_tun.MapName].Add(t_map07_tun);
            }
            else
            {
                res.Add(t_map07_tun.MapName, new List<TestCase>() { t_map07_tun });
            }

            ///--------------------------------------------------------------------------------

            TestCase t_map07_tun2 = new TestCase("TunnelDown-OK", "map07.gmap", 1224, 6164, 10);
            t_map07_tun2.nodeSwaps = new Dictionary<int, GameMap.NodeType>()
            {
                { 5363, GameMap.NodeType.Traversable},
                { 5463, GameMap.NodeType.Traversable},
                { 5563, GameMap.NodeType.Traversable},
                { 5663, GameMap.NodeType.Traversable},
                { 5763, GameMap.NodeType.Traversable},
                { 5863, GameMap.NodeType.Traversable}
            };
            if (res.ContainsKey(t_map07_tun2.MapName))
            {
                res[t_map07_tun2.MapName].Add(t_map07_tun2);
            }
            else
            {
                res.Add(t_map07_tun2.MapName, new List<TestCase>() { t_map07_tun2 });
            }



            return res;
        }

    }
}

