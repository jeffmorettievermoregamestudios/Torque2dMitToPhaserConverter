using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torque2dMitToPhaserConverter.AbstractSyntaxTreeClasses
{
    // NOTE TO DEVELOPERS: Not currently used by Torque2dMitToPhaserConverter
    public class CssFontFaceStylesTemplate : CodeBlock
    {
        public override string ConvertToCode()
        {
            var codeString = "";

            //  Inject our CSS
            codeString += "var element = document.createElement('style');\n";
            codeString += "document.head.appendChild(element);\n";
            codeString += "var sheet = element.sheet;\n\n";

            codeString += "var styles = '';\n\n";

            foreach (var cssFontFaceStyle in GlobalVars.PhaserAssetRepo.PhaserCssFontFaceStyleList)
            {
                codeString += "styles = " + cssFontFaceStyle.ConvertToCode() + "\n";
                codeString += "sheet.insertRule(styles, 0);\n\n";
            }

            return codeString;
        }
    }
}
