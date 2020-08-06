using System;
using System.Collections.Generic;

namespace VisualBasicUpgradeAssistant.Core.DataClasses
{
    /// <summary>
    /// Summary description for Property.
    /// </summary>
    public class Property
    {
        public String Name { get; set; }
        public String Type { get; set; }
        public String Direction { get; set; }
        public String Comment { get; set; }
        public String Scope { get; set; }
        public List<Parameter> ParameterList { get; set; }
        public List<String> LineList { get; set; }

        public Property()
        {
            LineList = new List<String>();
            ParameterList = new List<Parameter>();
        }
    }
}
