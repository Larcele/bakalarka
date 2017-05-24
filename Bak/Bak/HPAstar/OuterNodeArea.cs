using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bak.HPAstar
{
    public class OuterNodeArea : List<int>
    {
        private bool areaResolved = false;

        public OuterNodeArea()
        {
        }

        public void AddNode(int nodeID)
        {
            this.Add(nodeID);
        }

        public bool IsResolved()
        {
            return areaResolved;
        }

        public void SetAsResolved()
        {
            areaResolved = true;
        }
    }
}
