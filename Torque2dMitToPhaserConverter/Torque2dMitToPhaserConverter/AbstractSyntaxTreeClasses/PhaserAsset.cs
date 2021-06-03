using System.IO;

namespace Torque2dMitToPhaserConverter.AbstractSyntaxTreeClasses
{
    public abstract class PhaserAsset : CodeBlock
    {
        public FileInfo Torque2dAssetFileReference { get; set; }
    }
}
