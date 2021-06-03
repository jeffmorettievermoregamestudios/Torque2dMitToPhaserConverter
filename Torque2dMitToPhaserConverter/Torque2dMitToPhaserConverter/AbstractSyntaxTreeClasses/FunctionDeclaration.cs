using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torque2dMitToPhaserConverter.AbstractSyntaxTreeClasses
{
    public class FunctionDeclaration : CodeBlock
    {
        public string Name { get; set; }

        // NOTE: Not a req'd field
        public List<FunctionParameter> Parameters { get; set; }

        // NOTE: Also not a req'd field
        public List<CodeBlock> Contents { get; set; }

        public bool CanWriteAsEmptyFunction { get; set; }

        public override string ConvertToCode()
        {
            if ((!CanWriteAsEmptyFunction) && (Contents == null || Contents.Count == 0) )
            {
                return "function " + Name; // rest of function code is 'defined in elsewhere/preceding CodeBlocks'
            }

            var codeString = "function " + Name + "(";

            if (Parameters != null)
            {
                foreach (var funcParameter in Parameters)
                {
                    codeString = codeString + funcParameter.ConvertToCode() + ", ";
                }

                if (Parameters.Count > 0)
                {
                    codeString = codeString.Substring(0, codeString.Length - 2);
                }
            }

            codeString = codeString + ")\n{";

            if (Contents != null)
            {
                foreach (var cb in Contents)
                {
                    codeString = codeString + cb.ConvertToCode();
                }
            }

            codeString = codeString + "}\n\n";

            return codeString;
        }
    }
}
