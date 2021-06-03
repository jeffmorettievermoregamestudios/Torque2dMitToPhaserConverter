using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torque2dMitToPhaserConverter.AbstractSyntaxTreeClasses
{
    public class PhaserClassHierarchyObj
    {
        public string ClassName;
        public List<PhaserClassHierarchyObj> SubClasses { get; set; }
        public bool IsAPhaserApiClass { get; set; }
        public PhaserClassHierarchyObj Parent { get; set; }
    }
}
