using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torque2dMitToPhaserConverter.AbstractSyntaxTreeClasses
{
    public class PhaserProjectConfig : CodeBlock
    {
        public string PhaserProjectType { get; set; }
        public int PhaserProjectWidthInPixels { get; set; }
        public int PhaserProjectHeightInPixels { get; set; }

        public override string ConvertToCode()
        {
            return "";
        }
    }
}
