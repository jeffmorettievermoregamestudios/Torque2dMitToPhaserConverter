using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torque2dMitToPhaserConverter.AbstractSyntaxTreeClasses
{
    // Note that a 'Dot' is not created for numeric value tokens (the dot is part of the numeric value in this case)
    public class Dot : CodeBlock
    {
        public override string ConvertToCode()
        {
            return ".";
        }
    }
}
