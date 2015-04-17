using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Prototype.NGFM
{
    public class TreeListModelNode
    {
            public string Property { get; private set; }
            public string Value { get; private set; }
            public List<TreeListModelNode> Children { get; private set; }
            public TreeListModelNode(string Property, string Value)
            {
                this.Property = Property;
                this.Value = Value;
                this.Children = new List<TreeListModelNode>();
            }
    }
}
