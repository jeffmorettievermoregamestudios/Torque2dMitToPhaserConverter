using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torque2dMitToPhaserConverter.AbstractSyntaxTreeClasses
{
    public class CodeFile
    {
        public string Filename { get; set; }
        public List<CodeBlock> Contents { get; set; }

        public void WriteToFileSystem()
        {
            var fileString = "";

            foreach(var codeBlock in Contents)
            {
                fileString = fileString + codeBlock.ConvertToCode();
            }

            File.WriteAllText(Torque2dToPhaserConverterFunctionLibrary.ConvertTorquescriptFilePathToPhaserFilePath(Filename), fileString);
        }
    }
}
