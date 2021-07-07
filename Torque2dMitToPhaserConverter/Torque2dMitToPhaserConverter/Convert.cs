using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torque2dMitToPhaserConverter
{
    public static class Convert
    {
        public static string FromTorque2dClassStringToPhaserClassString(string torque2dClassString)
        {
            var torque2dClassStringToLower = torque2dClassString.ToLower();

            if (torque2dClassStringToLower == Torque2dConstants.SceneClassName.ToLower())
            {
                return PhaserConstants.SceneBaseClassName;
            }

            if (torque2dClassStringToLower == Torque2dConstants.SpriteClassName.ToLower())
            {
                return PhaserConstants.SpriteBaseClassName;
            }

            if (torque2dClassStringToLower == Torque2dConstants.TextSpriteClassName.ToLower())
            {
                return PhaserConstants.PhaserTextBaseClassName;
            }

            if (torque2dClassStringToLower == Torque2dConstants.SceneObjectClassName.ToLower())
            {
                return PhaserConstants.ObjectClassName;
            }

            return "CouldNotDetermineClassName";
        }
    }
}
