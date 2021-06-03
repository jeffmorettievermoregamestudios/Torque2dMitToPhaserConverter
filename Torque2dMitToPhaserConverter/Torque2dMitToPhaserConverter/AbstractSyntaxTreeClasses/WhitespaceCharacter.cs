using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torque2dMitToPhaserConverter.AbstractSyntaxTreeClasses
{
    public class WhitespaceCharacter : CodeBlock
    {
        public char WhitespaceChar { get; set; }

        public override string ConvertToCode()
        {
            return WhitespaceChar.ToString();
        }
    }
}
