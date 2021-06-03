using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torque2dMitToPhaserConverter.AbstractSyntaxTreeClasses
{
    public class SpritesheetAsset : PhaserAsset
    {
        public string AssetKey { get; set; }
        public string ResourceUrl { get; set; }
        public double FrameWidth { get; set; }
        public double FrameHeight { get; set; }
        public int EndFrame { get; set; }

        public override string ConvertToCode()
        {
            // example of code output:
            // this.load.spritesheet('boom', 'assets/sprites/explosion.png', { frameWidth: 64, frameHeight: 64, endFrame: 23 });

            return "this.load.spritesheet('" + AssetKey + "', '" + ResourceUrl + "', { frameWidth: " + FrameWidth + ", frameHeight: " + FrameHeight + ", endFrame: " + EndFrame + " });\n";
        }
    }
}
