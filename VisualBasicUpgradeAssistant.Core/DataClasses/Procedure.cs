using System;
using System.Collections.Generic;

namespace VisualBasicUpgradeAssistant.Core.DataClasses
{
    public enum ProcedureType
    {
        Event = 1,
        Subroutine = 2,
        Function = 3
    }

    /// <summary>
    /// Summary description for Procedure.
    /// </summary>
    public class Procedure
    {
        public String Name { get; set; }
        public String Comment { get; set; }
        public String Scope { get; set; }
        public ProcedureType Type { get; set; }
        public List<Parameter> ParameterList { get; set; }
        public String ReturnType { get; set; }
        public List<String> LineList { get; set; }

        public Procedure()
        {
            LineList = new List<String>();
            ParameterList = new List<Parameter>();
        }
    }
}
