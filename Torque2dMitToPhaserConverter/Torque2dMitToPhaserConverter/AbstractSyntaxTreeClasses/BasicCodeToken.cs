using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torque2dMitToPhaserConverter.AbstractSyntaxTreeClasses
{
    // Reserving this Token for any of the following code tokens:
    // - object name / method name / field name (ie myobject.DoSomething > this code token will store either 'myobject' or 'DoSomething'.  Also note
    // that the dot is stored as a 'Dot' token, by the Rules)
    // - object name 'as a constructor' (ie new TruckObject(), where TruckObject is the BasicCodeToken)
    // - fields (ie
    // new Sprite() {
    //  superclass = "enemy";
    //  class = "basicEnemy";
    // }
    // - function calls

    public class BasicCodeToken : CodeBlock
    {
        public string Value { get; set; }

        public string SuperClass { get; set; }
        public string Class { get; set; }

        public bool DoNotConvertVariable = false;

        public bool IsASceneGraphVariable = false;

        public override string ConvertToCode()
        {
            if (!string.IsNullOrEmpty(Class))
            {
                return Class;
            }

            if (!DoNotConvertVariable)
            {
                var conversionCheck = Convert.FromTorque2dClassStringToPhaserClassString(Value);
                if (conversionCheck != "CouldNotDetermineClassName")
                {
                    return conversionCheck;
                }

                // WARNING!!! This is a 'naive' approach to just simply rename all tokens labelled 'update' to 'torque2dUpdate' (to prevent clashes
                // with say, the way that this code converter has to convert 'onSceneUpdate' to 'update').  This could definitely be smarter,
                // but I think for now it will work
                if (Value.ToLower() == "update")
                {
                    return "torque2dUpdate";
                }

                if (Value.ToLower() == "init")
                {
                    return "torque2dInit";
                }

                if (Value.ToLower() == "preload")
                {
                    return "torque2dPreload";
                }

                if (Value.ToLower() == "vectordist")
                {
                    return "Distance.BetweenPoints";
                }

                if (Value.ToLower() == "scenelayer")
                {
                    return "depth";
                }
            }

            return Value;
        }
    }
}
