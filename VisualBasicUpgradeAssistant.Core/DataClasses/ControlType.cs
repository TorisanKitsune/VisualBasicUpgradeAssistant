using System;
using System.Collections.Generic;

namespace VisualBasicUpgradeAssistant.Core.DataClasses
{
    /// <summary>
    /// Summary description for Control.
    /// </summary>
    public class ControlType
    {
        public String Name { get; set; }
        public String Type { get; set; }
        public String Owner { get; set; }
        public Boolean Container { get; set; }
        public Boolean Valid { get; set; }
        public Boolean InvisibleAtRuntime { get; set; }
        public List<ControlProperty> PropertyList { get; }

        public ControlType()
        {
            PropertyList = new List<ControlProperty>();
        }
    }
}
