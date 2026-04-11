using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bendric.Api
{
    public class AdvList<T> : List<T>
    {
        public AdvList() : base() { }
        public AdvList(params T[] items) : base(items) { }
        public AdvList(IEnumerable<T> enumerable) : base(enumerable) { }
        public void Add(params T[] items)
        {
            AddRange(items);
        }
    }
}
