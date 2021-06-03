using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torque2dMitToPhaserConverter.AbstractSyntaxTreeClasses
{
    public class StringConcatNewline : CodeBlock
    {
        public override string ConvertToCode()
        {
            return "+ \"\\n\" +";
        }
    }
}
