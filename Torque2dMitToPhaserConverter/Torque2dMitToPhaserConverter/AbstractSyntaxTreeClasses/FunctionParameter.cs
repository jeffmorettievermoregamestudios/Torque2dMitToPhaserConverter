using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torque2dMitToPhaserConverter.AbstractSyntaxTreeClasses
{
    public class FunctionParameter : CodeBlock
    {
        public PhaserObjectType ObjectType { get; set; }
        public string Name { get; set; }

        public override string ConvertToCode()
        {
            return "";
        }
    }
}
