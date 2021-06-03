using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torque2dMitToPhaserConverter.AbstractSyntaxTreeClasses
{
    public class LocalVariable : Variable
    {
        public override string ConvertToCode()
        {
            if (Name.ToLower() == "%this")
            {
                return "this";
            }

            var variablePostfix = "";

            if (GlobalVars.AddVariablePostfixStringToPreventClassAndVariableCollision)
            {
                variablePostfix = "Var";
            }

            return Name.Substring(1) + variablePostfix;
        }
    }
}
