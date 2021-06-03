using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torque2dMitToPhaserConverter.AbstractSyntaxTreeClasses;

namespace Torque2dMitToPhaserConverter
{
    public class PhaserClass
    {
        public string ClassName { get; set; }


        // NOTE:
        // Item1 is method name
        // Item2 is the list of method parameters
        // Item3 is the content of the method (includes all method contents ie method name, parameters, curly brackets, etc)

        public List<Tuple<string,List<string>,List<CodeBlock>>> ClassMethods { get; set; }

        public void FormatToPhaserData()
        {
            var formattedClassMethods = new List<Tuple<string, List<string>, List<CodeBlock>>>();

            for (var i = 0; i < ClassMethods.Count; i++)
            {
                var classMethod = ClassMethods[i];
                Tuple<string, List<string>, List<CodeBlock>> newClassMethod = null;

                if (classMethod.Item1.ToLower() == "update")
                {
                    newClassMethod = Tuple.Create("torque2dUpdate", classMethod.Item2, classMethod.Item3);
                }

                if (classMethod.Item1.ToLower() == "init")
                {
                    newClassMethod = Tuple.Create("torque2dInit", classMethod.Item2, classMethod.Item3);
                }

                if (classMethod.Item1.ToLower() == "onsceneupdate")
                {
                    newClassMethod = Tuple.Create("update", classMethod.Item2, classMethod.Item3);
                }

                if (newClassMethod == null)
                {
                    formattedClassMethods.Add(classMethod);
                }
                else
                {
                    formattedClassMethods.Add(newClassMethod);
                }
            }

            ClassMethods = formattedClassMethods;
        }
    }
}
