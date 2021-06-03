using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torque2dMitToPhaserConverter.AbstractSyntaxTreeClasses
{
    public abstract class CodeBlock
    {
        public PhaserObjectType PhaserObjectType { get; set; }
        public abstract string ConvertToCode();

        public CodeBlock DeepCopy()
        {
            if (this.GetType() == typeof(StringValue))
            {
                var thisAsStringValue = (StringValue)this;
                return new StringValue { Val = thisAsStringValue.Val };
            }

            if (this.GetType() == typeof(LocalVariable))
            {
                var thisAsLocalVariable = (LocalVariable)this;
                return new LocalVariable { Name = thisAsLocalVariable.Name, PhaserObjectType = thisAsLocalVariable.PhaserObjectType };
            }

            if (this.GetType() == typeof(GlobalVariable))
            {
                var thisAsGlobalVariable = (GlobalVariable)this;
                return new GlobalVariable { Name = thisAsGlobalVariable.Name, PhaserObjectType = thisAsGlobalVariable.PhaserObjectType };
            }

            return null;
        }
    }
}
