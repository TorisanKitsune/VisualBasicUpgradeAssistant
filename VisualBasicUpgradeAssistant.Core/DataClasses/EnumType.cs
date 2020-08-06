using System;
using System.Collections.Generic;

namespace VisualBasicUpgradeAssistant.Core.DataClasses
{
    /// <summary>
    /// Summary description for Enum.
    /// </summary>
    public class EnumType
    {
        public String Name { get; set; }
        public String Scope { get; set; }
        public List<EnumItem> ItemList { get; }

        public EnumType()
        {
            ItemList = new List<EnumItem>();
        }
    }
}
