using System;
using System.Collections.Generic;
using VisualBasicUpgradeAssistant.Core.Model;

namespace VisualBasicUpgradeAssistant.Core.DataClasses
{
    /// <summary>
    /// Summary description for Module.
    /// </summary>
    public class Module
    {
        public String Name { get; set; }
        public FileType Type { get; set; }
        public String Version { get; set; }
        public String FileName { get; set; }
        public Boolean ImagesUsed { get; set; } = false;
        public Boolean MenuUsed { get; set; } = false;
        public List<String> ImageList { get; set; }
        public List<ControlType> ControlList { get; set; }
        public List<ControlProperty> FormPropertyList { get; set; }
        public List<Variable> VariableList { get; set; }
        public List<Property> PropertyList { get; set; }
        public List<Procedure> ProcedureList { get; set; }
        public List<EnumType> EnumList { get; set; }

        public Module()
        {
            FormPropertyList = new List<ControlProperty>();
            ControlList = new List<ControlType>();
            ImageList = new List<String>();
            VariableList = new List<Variable>();
            PropertyList = new List<Property>();
            ProcedureList = new List<Procedure>();
            EnumList = new List<EnumType>();
        }
    }
}
