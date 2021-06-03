using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torque2dMitToPhaserConverter.AbstractSyntaxTreeClasses
{
    public class NumericValue : CodeBlock
    {
        // Note that we are storing the 'number' as a string, since it may have a decimal in it and thus
        // might be a float, or perhaps does not have a decimal and thus is an int.  Also could be
        // exponential number (TODO?) etc
        //
        // Will return 'as is' for now, but could consider improving...
        public string NumberAsString { get; set; }

        public override string ConvertToCode()
        {
            return NumberAsString;
        }
    }
}
