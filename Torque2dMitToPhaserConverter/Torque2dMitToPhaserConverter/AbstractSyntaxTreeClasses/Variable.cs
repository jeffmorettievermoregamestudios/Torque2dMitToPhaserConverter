using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torque2dMitToPhaserConverter.AbstractSyntaxTreeClasses
{
    public abstract class Variable : CodeBlock
    {
        public string Name { get; set; }

        public string Class { get; set; }
    }
}
