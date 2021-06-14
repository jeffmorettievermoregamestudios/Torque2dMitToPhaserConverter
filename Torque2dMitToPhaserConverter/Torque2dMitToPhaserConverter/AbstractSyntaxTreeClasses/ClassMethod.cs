using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torque2dMitToPhaserConverter.AbstractSyntaxTreeClasses
{
    public class ClassMethod : CodeBlock
    {
        // NOTE: Will have to specially handle all class methods at the end (ie need to pair up all class
        // methods with matching 'ClassName's etc)

        public string ClassName { get; set; }
        public string MethodName { get; set; }
        
        // This is one of the few code blocks that will not use 'ConvertToCode'
        public override string ConvertToCode()
        {
            return "";
        }
    }
}
