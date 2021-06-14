using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torque2dMitToPhaserConverter.AbstractSyntaxTreeClasses
{
    public class FunctionParameter : CodeBlock
    {
        /*TODO: Define.  Use Enumeration?  Include library function in this class to convert a 'string?' to a PhaserObjectType*/
        public PhaserObjectType ObjectType { get; set; }
        public string Name { get; set; }

        public override string ConvertToCode()
        {
            return "";
        }
    }
}
