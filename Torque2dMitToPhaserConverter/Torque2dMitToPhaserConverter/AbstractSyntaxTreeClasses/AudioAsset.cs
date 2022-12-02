using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torque2dMitToPhaserConverter.AbstractSyntaxTreeClasses
{
    public class AudioAsset : PhaserAsset
    {
        public string AssetKey { get; set; }
        public string ResourceUrl { get; set; }
        public bool IsLooping { get; set; }

        public override string ConvertToCode()
        {
            // example of code output:
            // this.load.audio('theme', 'assets/audio/oedipus_wizball_highscore.ogg');

            return "this.load.audio('" + AssetKey + "', '" + ResourceUrl + "');\n";
        }
    }
}
