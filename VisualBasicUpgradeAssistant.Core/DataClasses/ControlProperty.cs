using System;
using System.Collections.Generic;

namespace VisualBasicUpgradeAssistant.Core.DataClasses
{
    /// <summary>
    /// Summary description for Property.
    /// </summary>
    public class ControlProperty
    {
        public String Name { get; set; }
        public String Value { get; set; }
        public String Comment { get; set; }
        public Boolean Valid { get; set; }
        public List<ControlProperty> PropertyList { get; private set; }

        public ControlProperty()
        {
            PropertyList = new List<ControlProperty>();
            Valid = false;
        }
    }
}
