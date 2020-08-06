using System;

namespace VisualBasicUpgradeAssistant.Core.DataClasses
{
    /// <summary>
    /// Summary description for ControlListItem.
    /// </summary>
    public class Controltem
    {
        public String VB6 { get; set; } = String.Empty;
        public String CSharp { get; set; } = String.Empty;
        public Boolean Unsupported { get; set; }
        public Boolean InvisibleAtRuntime { get; set; }
    }
}
