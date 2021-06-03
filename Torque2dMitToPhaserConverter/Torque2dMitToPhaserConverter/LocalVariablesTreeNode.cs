using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torque2dMitToPhaserConverter.AbstractSyntaxTreeClasses;

namespace Torque2dMitToPhaserConverter
{
    public class LocalVariablesTreeNode
    {
        // note that 'Parent' will be null for the root node
        public LocalVariablesTreeNode Parent;

        // list of 'already' discovered local variables for this tree node
        public List<LocalVariable> LocalVariablesDiscovered;

        public bool ContainsLocalVariable(LocalVariable localVariable)
        {
            if (this.LocalVariablesDiscovered.FirstOrDefault(lv => lv.Name.ToLower() == localVariable.Name.ToLower()) != null)
            {
                return true;
            }

            if (this.Parent != null)
            {
                return Parent.ContainsLocalVariable(localVariable);
            }

            return false;
        }

        public void SetPhaserObjectTypeForLocalVariables(LocalVariable localVariableWithPhaserObjectType)
        {
            if (this.LocalVariablesDiscovered.FirstOrDefault(lv => lv.Name.ToLower() == localVariableWithPhaserObjectType.Name.ToLower()) != null)
            {
                this.LocalVariablesDiscovered.FirstOrDefault(lv => lv.Name.ToLower() == localVariableWithPhaserObjectType.Name.ToLower()).PhaserObjectType = localVariableWithPhaserObjectType.PhaserObjectType;
            }

            if (this.Parent != null)
            {
                Parent.SetPhaserObjectTypeForLocalVariables(localVariableWithPhaserObjectType);
            }
        }

        public LocalVariable GetLocalVariableByName(string name)
        {
            if (this.LocalVariablesDiscovered.FirstOrDefault(lv => lv.Name.ToLower() == name.ToLower()) != null)
            {
                return this.LocalVariablesDiscovered.FirstOrDefault(lv => lv.Name.ToLower() == name.ToLower());
            }

            if (this.Parent != null)
            {
                return Parent.GetLocalVariableByName(name);
            }

            return null;
        }
    }
}
