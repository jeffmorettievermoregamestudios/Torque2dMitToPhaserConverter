using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torque2dMitToPhaserConverter.AbstractSyntaxTreeClasses
{
    public class ClassMethod : CodeBlock
    {
        public string ClassName { get; set; }
        public string MethodName { get; set; }
        
        // This is one of the few code blocks that will not use 'ConvertToCode'
        public override string ConvertToCode()
        {
            return "";
        }
    }
}
