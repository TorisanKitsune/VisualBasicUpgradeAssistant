using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using VisualBasicUpgradeAssistant.Core.DataClasses;

namespace VisualBasicUpgradeAssistant.Core.Model
{
    /// <summary>
    /// Parse VB6 properties and values to C#
    /// </summary>
    public class Tools
    {
        private Dictionary<String, Controltem>? _controlList;

        /// <summary>
        /// Load the control from resource file
        /// </summary>
        private void ControlListLoad()
        {
            // get executable directory
            String? binPath = AppDomain.CurrentDomain.BaseDirectory;

            // Saftey check, should never be null
            if (String.IsNullOrEmpty(binPath))
            {
                throw new DirectoryNotFoundException("Bad path");
            }

            // Read the control list and convert to resource
            String jsonPath = Path.Combine(binPath, "Resources", "ControlList.json");
            ControlList controlList = ControlList.ReadData(new FileInfo(jsonPath));
            _controlList = controlList.Controls.ToDictionary(x => x.VB6);
        }

        public Boolean ParseModule(Module sourceModule, Module targetModule)
        {
            if (_controlList == null)
            {
                ControlListLoad();
            }

            // module name
            targetModule.Name = sourceModule.Name;
            // file name
            targetModule.FileName = Path.GetFileNameWithoutExtension(sourceModule.FileName) + ".cs";
            // type
            targetModule.Type = sourceModule.Type;
            // version
            targetModule.Version = sourceModule.Version;
            // process own properties - forms
            ParseModuleProperties(targetModule, sourceModule.FormPropertyList, targetModule.FormPropertyList);
            // process controls - form
            ParseControls(targetModule, sourceModule.ControlList, targetModule.ControlList);

            // special exception for menu
            if (targetModule.MenuUsed)
            {
                // add main menu control
                ControlType control = new ControlType
                {
                    Name = "MainMenu",
                    Owner = targetModule.Name,
                    Type = "MainMenu",
                    Valid = true,
                    InvisibleAtRuntime = true
                };
                targetModule.ControlList.Insert(0, control);
                foreach (ControlType oMenuControl in targetModule.ControlList)
                    if (oMenuControl.Type == "MenuItem" && oMenuControl.Owner == targetModule.Name)
                        // rewrite previous owner
                        oMenuControl.Owner = control.Name;
            }

            ArrayList TempControlList = new ArrayList();
            Int32 TabControlIndex = 0;

            // check for TabDlg.SSTab
            foreach (ControlType oTargetControl in targetModule.ControlList)
            {
                if (oTargetControl.Type == "TabControl" && oTargetControl.Valid)
                {
                    // for each source table is necessary
                    //          this.tabControl1 = new System.Windows.Forms.TabControl();
                    //          this.tabPage1 = new System.Windows.Forms.TabPage();

                    Int32 Index = 0;
                    ControlType tabPage = null;
                    // each property
                    foreach (ControlProperty targetProperty in oTargetControl.PropertyList)
                    {
                        // TabCaption = create new tab
                        //      this.SSTab1.(TabCaption(0)) = "Tab 0";

                        Console.WriteLine(targetProperty.Name);

                        if (targetProperty.Name.IndexOf("TabCaption(" + Index.ToString() + ")", 0) > -1)
                        {
                            // new tab
                            tabPage = new ControlType
                            {
                                Type = "TabPage",
                                Name = "tabPage" + Index.ToString(),
                                Owner = oTargetControl.Name,
                                Container = true,
                                Valid = true,
                                InvisibleAtRuntime = false
                            };

                            // add some necessary properties
                            ControlProperty TargetProperty = new ControlProperty
                            {
                                Name = "Location",
                                Value = "new System.Drawing.Point(4, 22)",
                                Valid = true
                            };
                            tabPage.PropertyList.Add(TargetProperty);

                            TargetProperty = new ControlProperty
                            {
                                Name = "Size",
                                Value = "new System.Drawing.Size(477, 374)",
                                Valid = true
                            };
                            tabPage.PropertyList.Add(TargetProperty);

                            TargetProperty = new ControlProperty
                            {
                                Name = "Text",
                                Value = targetProperty.Value,
                                Valid = true
                            };
                            tabPage.PropertyList.Add(TargetProperty);

                            TargetProperty = new ControlProperty
                            {
                                Name = "TabIndex",
                                Value = Index.ToString(),
                                Valid = true
                            };
                            tabPage.PropertyList.Add(TargetProperty);

                            TempControlList.Add(tabPage);
                            Index++;
                        }

                        // Control = change owner of control to current tab
                        //      this.SSTab1.(Tab(0).Control(0) = "ImageControl";
                        if (targetProperty.Name.IndexOf(".Control(", 0) > -1)
                            if (targetProperty.Name.IndexOf("Enable", 0) == -1)
                            {
                                String TabName = targetProperty.Value.Substring(1, targetProperty.Value.Length - 2);
                                TabName = GetControlIndexName(TabName);
                                // search for "oTargetProperty.Value" control
                                // and replace owner of this control to current tab
                                foreach (ControlType oNewOwner in targetModule.ControlList)
                                    if (oNewOwner.Name == TabName && !oNewOwner.InvisibleAtRuntime)
                                        oNewOwner.Owner = tabPage.Name;
                            }
                    }
                }
                TabControlIndex++;
            }

            if (TempControlList.Count > 0)
            {
                // right order of tabs
                Int32 Position = 0;
                foreach (ControlType oControl in TempControlList)
                {
                    targetModule.ControlList.Insert(TabControlIndex + Position, oControl);
                    Position++;
                }
            }

            // process enums
            ParseEnums(sourceModule, targetModule);
            // process variables
            ParseVariables(sourceModule.VariableList, targetModule.VariableList);
            // process properties
            ParseClassProperties(sourceModule, targetModule);
            // process procedures
            ParseProcedures(sourceModule, targetModule);

            return true;
        }

        // return control name
        private String GetControlIndexName(String tabName)
        {
            //  this.SSTab1.(Tab(1).Control(4) = "Option1(0)";
            Int32 Start = 0;
            Int32 End = 0;

            Start = tabName.IndexOf("(", 0);
            if (Start > -1)
            {
                End = tabName.IndexOf(")", 0);
                return tabName.Substring(0, Start) + tabName.Substring(Start + 1, End - Start - 1);
            }
            else
                return tabName;
        }

        public Boolean ParseControls(Module module, List<ControlType> sourceControlList, List<ControlType> targetControlList)
        {
            String Type = String.Empty;

            foreach (ControlType sourceControl in sourceControlList)
            {
                ControlType targetControl = new ControlType
                {
                    Name = sourceControl.Name,
                    Owner = sourceControl.Owner,
                    Container = sourceControl.Container,
                    Valid = true
                };

                // compare upper case type
                if (_controlList.ContainsKey(sourceControl.Type))
                {
                    Controltem item = (Controltem)_controlList[sourceControl.Type];

                    if (item.Unsupported)
                    {
                        Type = "Unsuported";
                        targetControl.Valid = false;
                    }
                    else
                    {
                        Type = item.CSharp;
                        if (Type == "MenuItem")
                            module.MenuUsed = true;
                    }
                    targetControl.InvisibleAtRuntime = item.InvisibleAtRuntime;
                }
                else
                    Type = sourceControl.Type;

                targetControl.Type = Type;
                ParseControlProperties(module, targetControl, sourceControl.PropertyList, targetControl.PropertyList);

                targetControlList.Add(targetControl);
            }
            return true;
        }

        public Boolean ParseModuleProperties(Module module, List<ControlProperty> sourcePropertyList, List<ControlProperty> targetPropertyList)
        {
            ControlProperty TargetProperty = null;

            // each property
            foreach (ControlProperty SourceProperty in sourcePropertyList)
            {
                TargetProperty = new ControlProperty();
                if (ParseProperties(module.Type.ToString(), SourceProperty, TargetProperty, sourcePropertyList))
                {
                    if (TargetProperty.Name == "BackgroundImage" || TargetProperty.Name == "Icon")
                        module.ImagesUsed = true;
                    targetPropertyList.Add(TargetProperty);
                }
            }
            return true;
        }

        public Boolean ParseEnums(Module sourceModule, Module targetModule)
        {
            foreach (EnumType SourceEnum in sourceModule.EnumList)
                targetModule.EnumList.Add(SourceEnum);
            return true;
        }

        public Boolean ParseVariables(List<Variable> sourceVariableList, List<Variable> targetVariableList)
        {
            Variable TargetVariable = null;

            // each property
            foreach (Variable SourceVariable in sourceVariableList)
            {
                TargetVariable = new Variable();
                if (ParseVariable(SourceVariable, TargetVariable))
                    targetVariableList.Add(TargetVariable);
            }
            return true;
        }

        private String VariableTypeConvert(String sourceType)
        {
            String TargetType;

            switch (sourceType)
            {
                case "Long":
                    TargetType = "int";
                    break;

                case "Integer":
                    TargetType = "short";
                    break;

                case "Byte":
                    TargetType = "byte";
                    break;

                case "String":
                    TargetType = "string";
                    break;

                case "Boolean":
                    TargetType = "bool";
                    break;

                case "Currency":
                    TargetType = "decimal";
                    break;

                case "Single":
                    TargetType = "float";
                    break;

                case "Double":
                    TargetType = "double";
                    break;

                case "ADODB.Recordset":
                case "DAO.Recordset":
                case "Recordset":
                    TargetType = "DataReader";
                    break;

                default:
                    TargetType = sourceType;
                    break;
            }
            return TargetType;
        }

        public Boolean ParseVariable(Variable sourceVariable, Variable targetVariable)
        {
            targetVariable.Scope = sourceVariable.Scope;
            targetVariable.Name = sourceVariable.Name;
            targetVariable.Type = VariableTypeConvert(sourceVariable.Type);

            return true;
        }

        public Boolean ParseClassProperties(Module sourceModule, Module targetModule)
        {
            Property TargetProperty;

            foreach (Property SourceProperty in sourceModule.PropertyList)
            {
                TargetProperty = new Property
                {
                    Name = SourceProperty.Name,
                    Comment = SourceProperty.Comment
                };
                switch (SourceProperty.Direction)
                {
                    case "Get":
                        TargetProperty.Direction = "get";
                        break;

                    case "Set":
                    case "Let":
                        TargetProperty.Direction = "set";
                        break;
                }
                TargetProperty.Scope = SourceProperty.Scope;
                TargetProperty.Type = VariableTypeConvert(SourceProperty.Type);
                // lines
                foreach (String Line in SourceProperty.LineList)
                    if (Line.Trim() != String.Empty)
                        TargetProperty.LineList.Add(Line);

                targetModule.PropertyList.Add(TargetProperty);
            }
            return true;
        }

        public Boolean ParseProcedures(Module sourceModule, Module targetModule)
        {
            const String Indent6 = "      ";
            Procedure TargetProcedure;
            String Temp;

            foreach (Procedure SourceProcedure in sourceModule.ProcedureList)
            {
                TargetProcedure = new Procedure
                {
                    Name = SourceProcedure.Name,
                    Scope = SourceProcedure.Scope,
                    Comment = SourceProcedure.Comment,
                    Type = SourceProcedure.Type,
                    ReturnType = VariableTypeConvert(SourceProcedure.ReturnType),

                    ParameterList = SourceProcedure.ParameterList
                };

                // lines
                foreach (String Line in SourceProcedure.LineList)
                {
                    Temp = Line.Trim();
                    if (Temp.Length > 0)
                    {
                        String TempLine = String.Empty;
                        // vbNullString = String.Empty
                        if (Temp.IndexOf("vbNullString", 0) > -1)
                        {
                            TempLine = Temp.Replace("vbNullString", "String.Empty");
                            Temp = TempLine;
                        }
                        // Nothing = null
                        if (Temp.IndexOf("Nothing", 0) > -1)
                        {
                            TempLine = Temp.Replace("Nothing", "null");
                            Temp = TempLine;
                        }
                        // Set
                        if (Temp.IndexOf("Set ", 0) > -1)
                        {
                            TempLine = Temp.Replace("Set ", " ");
                            Temp = TempLine;
                        }
                        // remark
                        if (Temp[0] == (Char)39) // '
                        {
                            TempLine = Temp.Replace("'", "//");
                            Temp = TempLine;
                        }
                        // & to +
                        if (Temp.IndexOf("&", 0) > -1)
                        {
                            TempLine = Temp.Replace("&", "+");
                            Temp = TempLine;
                        }
                        // Select Case
                        if (Temp.IndexOf("Select Case", 0) > -1)
                        {
                            TempLine = Temp.Replace("Select Case", "switch");
                            Temp = TempLine;
                        }
                        // End Select
                        if (Temp.IndexOf("End Select", 0) > -1)
                        {
                            TempLine = Temp.Replace("End Select", "}");
                            Temp = TempLine;
                        }
                        // _
                        if (Temp.IndexOf(" _", 0) > -1)
                        {
                            TempLine = Temp.Replace(" _", "\r\n");
                            Temp = TempLine;
                        }
                        // If
                        if (Temp.IndexOf("If ", 0) > -1)
                        {
                            TempLine = Temp.Replace("If ", "if ( ");
                            Temp = TempLine;
                        }
                        // Not
                        if (Temp.IndexOf("Not ", 0) > -1)
                        {
                            TempLine = Temp.Replace("Not ", "! ");
                            Temp = TempLine;
                        }
                        // then
                        if (Temp.IndexOf(" Then", 0) > -1)
                        {
                            TempLine = Temp.Replace(" Then", " )\r\n" + Indent6 + "{\r\n");
                            Temp = TempLine;
                        }
                        // else
                        if (Temp.IndexOf("Else", 0) > -1)
                        {
                            TempLine = Temp.Replace("Else", "}\r\n" + Indent6 + "else\r\n" + Indent6 + "{");
                            Temp = TempLine;
                        }
                        // End if
                        if (Temp.IndexOf("End If", 0) > -1)
                        {
                            TempLine = Temp.Replace("End If", "}");
                            Temp = TempLine;
                        }
                        // Unload Me
                        if (Temp.IndexOf("Unload Me", 0) > -1)
                        {
                            TempLine = Temp.Replace("Unload Me", "Close()");
                            Temp = TempLine;
                        }
                        // .Caption
                        if (Temp.IndexOf(".Caption", 0) > -1)
                        {
                            TempLine = Temp.Replace(".Caption", ".Text");
                            Temp = TempLine;
                        }
                        // True
                        if (Temp.IndexOf("True", 0) > -1)
                        {
                            TempLine = Temp.Replace("True", "true");
                            Temp = TempLine;
                        }
                        // False
                        if (Temp.IndexOf("False", 0) > -1)
                        {
                            TempLine = Temp.Replace("False", "false");
                            Temp = TempLine;
                        }

                        // New
                        if (Temp.IndexOf("New", 0) > -1)
                        {
                            TempLine = Temp.Replace("New", "new");
                            Temp = TempLine;
                        }

                        if (TempLine == String.Empty)
                            TargetProcedure.LineList.Add(Temp);
                        else
                            TargetProcedure.LineList.Add(TempLine);
                    }
                    else
                        TargetProcedure.LineList.Add(String.Empty);
                }

                targetModule.ProcedureList.Add(TargetProcedure);
            }
            return true;
        }

        public Boolean ParseControlProperties(Module module, ControlType control, List<ControlProperty> sourcePropertyList, List<ControlProperty> targetPropertyList)
        {
            ControlProperty TargetProperty = null;

            // each property
            foreach (ControlProperty SourceProperty in sourcePropertyList)
                if (SourceProperty.Name == "Index")
                    // Index           =   3
                    control.Name = control.Name + SourceProperty.Value;
                else
                {
                    TargetProperty = new ControlProperty();
                    if (ParseProperties(control.Type, SourceProperty, TargetProperty, sourcePropertyList))
                    {
                        if (TargetProperty.Name == "Image")
                            module.ImagesUsed = true;
                        targetPropertyList.Add(TargetProperty);
                    }
                }
            return true;
        }

        public Boolean ParseProperties(String type, ControlProperty sourceProperty, ControlProperty targetProperty, List<ControlProperty> sourcePropertyList)
        {
            Boolean validProperty = true;

            targetProperty.Valid = true;

            switch (sourceProperty.Name)
            {
                // not used
                case "Appearance":
                case "ScaleHeight":
                case "ScaleWidth":
                case "Style":             // button
                case "BackStyle":         //label
                case "IMEMode":
                case "WhatsThisHelpID":
                case "Mask":              // maskedit
                case "PromptChar":        // maskedit

                    validProperty = false;
                    break;

                // begin common properties

                case "Alignment":
                    //              0 - left
                    //              1 - right
                    //              2 - center
                    targetProperty.Name = "TextAlign";
                    targetProperty.Value = "System.Drawing.ContentAlignment.";
                    switch (sourceProperty.Value)
                    {
                        case "0":
                            targetProperty.Value = targetProperty.Value + "TopLeft";
                            break;

                        case "1":
                            targetProperty.Value = targetProperty.Value + "TopRight";
                            break;

                        case "2":
                        default:
                            targetProperty.Value = targetProperty.Value + "TopCenter";
                            break;
                    }
                    break;

                case "BackColor":
                case "ForeColor":
                    if (type != "ImageList")
                    {
                        targetProperty.Name = sourceProperty.Name;
                        targetProperty.Value = GetColor(sourceProperty.Value);
                    }
                    else
                        validProperty = false;
                    break;

                case "BorderStyle":
                    if (type == "Form")
                    {
                        targetProperty.Name = "FormBorderStyle";
                        // 0 - none
                        // 1 - fixed single
                        // 2 - sizable
                        // 3 - fixed dialog
                        // 4 - fixed toolwindow
                        // 5 - sizable toolwindow

                        // FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;

                        targetProperty.Value = "System.Windows.Forms.FormBorderStyle.";
                        switch (sourceProperty.Value)
                        {
                            case "0":
                                targetProperty.Value = targetProperty.Value + "None";
                                break;

                            default:
                            case "1":
                                targetProperty.Value = targetProperty.Value + "FixedSingle";
                                break;

                            case "2":
                                targetProperty.Value = targetProperty.Value + "Sizable";
                                break;

                            case "3":
                                targetProperty.Value = targetProperty.Value + "FixedDialog";
                                break;

                            case "4":
                                targetProperty.Value = targetProperty.Value + "FixedToolWindow";
                                break;

                            case "5":
                                targetProperty.Value = targetProperty.Value + "SizableToolWindow";
                                break;
                        }
                    }
                    else
                    {
                        targetProperty.Name = sourceProperty.Name;
                        targetProperty.Value = "System.Windows.Forms.BorderStyle.";
                        switch (sourceProperty.Value)
                        {
                            case "0":
                                targetProperty.Value = targetProperty.Value + "None";
                                break;

                            case "1":
                                targetProperty.Value = targetProperty.Value + "FixedSingle";
                                break;

                            case "2":
                            default:
                                targetProperty.Value = targetProperty.Value + "Fixed3D";
                                break;
                        }
                    }
                    break;

                case "Caption":
                case "Text":
                    targetProperty.Name = "Text";
                    targetProperty.Value = sourceProperty.Value;
                    break;

                // this.cmdExit.Size = new System.Drawing.Size(80, 40);
                case "Height":
                    targetProperty.Name = "Size";
                    targetProperty.Value = "new System.Drawing.Size(" + GetSize("Height", "Width", sourcePropertyList) + ")";
                    break;

                // this.cmdExit.Location = new System.Drawing.Point(616, 520);
                case "Left":
                    if (type != "ImageList" && type != "Timer")
                    {
                        targetProperty.Name = "Location";
                        targetProperty.Value = "new System.Drawing.Point(" + GetLocation(sourcePropertyList) + ")";
                    }
                    else
                        validProperty = false;
                    break;

                case "Top":
                case "Width":
                    // nothing, already processed by Height, Left
                    validProperty = false;
                    break;

                case "Enabled":
                case "Locked":
                case "TabStop":
                case "Visible":
                case "UseMnemonic":
                    targetProperty.Name = sourceProperty.Name;
                    targetProperty.Value = GetBool(sourceProperty.Value);
                    break;

                case "WordWrap":
                    if (type == "Text")
                    {
                        targetProperty.Name = sourceProperty.Name;
                        targetProperty.Value = GetBool(sourceProperty.Value);
                    }
                    else
                        validProperty = false;
                    break;

                case "Font":
                    ConvertFont(sourceProperty, targetProperty);
                    break;
                // end common properties

                case "MaxLength":
                    targetProperty.Name = sourceProperty.Name;
                    targetProperty.Value = sourceProperty.Value;
                    break;

                // PasswordChar
                case "PasswordChar":
                    targetProperty.Name = sourceProperty.Name;
                    // PasswordChar = '*';
                    targetProperty.Value = "'" + sourceProperty.Value.Substring(1, 1) + "'";
                    break;

                // Value
                case "Value":
                    switch (type)
                    {
                        case "RadioButton":
                            // .Checked = true;
                            targetProperty.Name = "Checked";
                            targetProperty.Value = GetBool(sourceProperty.Value);
                            break;

                        case "CheckBox":
                            //.CheckState = System.Windows.Forms.CheckState.Checked;
                            targetProperty.Name = "CheckState";
                            targetProperty.Value = "System.Windows.Forms.CheckState.";
                            // 0 - Unchecked
                            // 1 - checked
                            // 2 - grayed
                            switch (sourceProperty.Value)
                            {
                                default:
                                case "0":
                                    targetProperty.Value = targetProperty.Value + "Unchecked";
                                    break;

                                case "1":
                                    targetProperty.Value = targetProperty.Value + "Checked";
                                    break;

                                case "2":
                                    targetProperty.Value = targetProperty.Value + "Indeterminate";
                                    break;
                            }
                            break;

                        default:
                            targetProperty.Value = targetProperty.Value + "Both";
                            break;
                    }
                    break;

                // timer
                case "Interval":
                    targetProperty.Name = "Interval";
                    targetProperty.Value = sourceProperty.Value;
                    break;

                // this.cmdExit.Anchor = (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right);
                case "Cancel":
                    if (Int32.Parse(sourceProperty.Value) != 0)
                    {
                        targetProperty.Name = "DialogResult";
                        targetProperty.Value = "System.Windows.Forms.DialogResult.Cancel";
                    }
                    break;

                case "Default":
                    if (Int32.Parse(sourceProperty.Value) != 0)
                    {
                        targetProperty.Name = "DialogResult";
                        targetProperty.Value = "System.Windows.Forms.DialogResult.OK";
                    }
                    break;

                //                this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
                //                this.ClientSize = new System.Drawing.Size(704, 565);
                //                this.MinimumSize = new System.Drawing.Size(712, 592);

                // direct value
                case "TabIndex":
                case "Tag":
                    // except MenuItem
                    if (type != "MenuItem")
                    {
                        targetProperty.Name = sourceProperty.Name;
                        targetProperty.Value = sourceProperty.Value;
                    }
                    else
                        validProperty = false;
                    break;

                // -1 converted to true
                // 0 to false
                case "AutoSize":
                    // only for Label
                    if (type == "Label")
                    {
                        targetProperty.Name = sourceProperty.Name;
                        targetProperty.Value = GetBool(sourceProperty.Value);
                    }
                    else
                        validProperty = false;
                    break;

                case "Icon":
                    // "Form1.frx":0000;
                    // exist file ?

                    //          System.Drawing.Bitmap pic = null;
                    //          GetFRXImage(@"C:\temp\test\form1.frx", 0x13960, pic );

                    if (type == "Form")
                    {
                        //.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
                        targetProperty.Name = "Icon";
                        targetProperty.Value = sourceProperty.Value;
                    }
                    else
                    {
                        // ((System.Drawing.Bitmap)(resources.GetObject("Command1.Image")));
                        targetProperty.Name = "Image";
                        targetProperty.Value = sourceProperty.Value;
                    }
                    break;

                case "Picture":
                    // = "Form1.frx":13960;
                    if (type == "form")
                    {
                        // ((System.Drawing.Bitmap)(resources.GetObject("$this.BackgroundImage")));
                        targetProperty.Name = "BackgroundImage";
                        targetProperty.Value = sourceProperty.Value;
                    }
                    else
                    {
                        // ((System.Drawing.Bitmap)(resources.GetObject("Command1.Image")));
                        targetProperty.Name = "Image";
                        targetProperty.Value = sourceProperty.Value;
                    }
                    break;

                case "ScrollBars":
                    // ScrollBars = System.Windows.Forms.ScrollBars.Both;
                    targetProperty.Name = sourceProperty.Name;

                    if (type == "RichTextBox")
                        targetProperty.Value = "System.Windows.Forms.RichTextBoxScrollBars.";
                    else
                        targetProperty.Value = "System.Windows.Forms.ScrollBars.";
                    switch (sourceProperty.Value)
                    {
                        default:
                        case "0":
                            targetProperty.Value = targetProperty.Value + "None";
                            break;

                        case "1":
                            targetProperty.Value = targetProperty.Value + "Horizontal";
                            break;

                        case "2":
                            targetProperty.Value = targetProperty.Value + "Vertical";
                            break;

                        case "3":
                            targetProperty.Value = targetProperty.Value + "Both";
                            break;
                    }
                    break;

                // SS tab
                case "TabOrientation":
                    targetProperty.Name = "Alignment";
                    targetProperty.Value = "System.Windows.Forms.TabAlignment.";
                    switch (sourceProperty.Value)
                    {
                        default:
                        case "0":
                            targetProperty.Value = targetProperty.Value + "Top";
                            break;

                        case "1":
                            targetProperty.Value = targetProperty.Value + "Bottom";
                            break;

                        case "2":
                            targetProperty.Value = targetProperty.Value + "Left";
                            break;

                        case "3":
                            targetProperty.Value = targetProperty.Value + "Right";
                            break;
                    }
                    break;

                // begin Listview

                // unsupported properties
                case "_ExtentX":
                case "_ExtentY":
                case "_Version":
                case "OLEDropMode":
                    validProperty = false;
                    break;

                // this.listView.View = System.Windows.Forms.View.List;
                case "View":
                    targetProperty.Name = sourceProperty.Name;
                    targetProperty.Value = "System.Windows.Forms.View.";
                    targetProperty.Value = sourceProperty.Value switch
                    {
                        "0" => targetProperty.Value + "Details",
                        "1" => targetProperty.Value + "LargeIcon",
                        "2" => targetProperty.Value + "SmallIcon",
                        _ => targetProperty.Value + "List",
                    };
                    break;

                case "LabelEdit":
                case "LabelWrap":
                case "MultiSelect":
                case "HideSelection":
                    targetProperty.Name = sourceProperty.Name;
                    targetProperty.Value = GetBool(sourceProperty.Value);
                    break;

                // end List view

                // VB6 form unsupported properties
                case "MDIChild":
                case "WhatsThisButton":
                case "NegotiateMenus":
                case "HelpContextID":
                case "LinkTopic":
                case "PaletteMode":
                case "ClipControls":
                case "LockControls":
                case "FillStyle":
                    validProperty = false;
                    break;

                // supported properties

                case "ControlBox":
                case "KeyPreview":
                    targetProperty.Name = sourceProperty.Name;
                    targetProperty.Value = GetBool(sourceProperty.Value);
                    break;

                case "ClientHeight":
                    targetProperty.Name = "ClientSize";
                    targetProperty.Value = "new System.Drawing.Size(" + GetSize("ClientHeight", "ClientWidth", sourcePropertyList) + ")";
                    break;

                case "ClientWidth":
                    // nothing, already processed by Height, Left
                    validProperty = false;
                    break;

                case "ClientLeft":
                case "ClientTop":
                    validProperty = false;
                    break;

                case "MaxButton":
                    targetProperty.Name = "MaximizeBox";
                    targetProperty.Value = GetBool(sourceProperty.Value);
                    break;

                case "MinButton":
                    targetProperty.Name = "MinimizeBox";
                    targetProperty.Value = GetBool(sourceProperty.Value);
                    break;

                case "WhatsThisHelp":
                    targetProperty.Name = "HelpButton";
                    targetProperty.Value = GetBool(sourceProperty.Value);
                    break;

                case "ShowInTaskbar":
                    targetProperty.Name = sourceProperty.Name;
                    targetProperty.Value = GetBool(sourceProperty.Value);
                    break;

                case "WindowList":
                    targetProperty.Name = "MdiList";
                    targetProperty.Value = GetBool(sourceProperty.Value);
                    break;

                // this.WindowState = System.Windows.Forms.FormWindowState.Minimized;
                // 0 - normal
                // 1 - minimized
                // 2 - maximized
                case "WindowState":
                    targetProperty.Name = sourceProperty.Name;
                    targetProperty.Value = "System.Windows.Forms.FormWindowState.";
                    switch (sourceProperty.Value)
                    {
                        case "0":
                        default:
                            targetProperty.Value = targetProperty.Value + "Normal";
                            break;

                        case "1":
                            targetProperty.Value = targetProperty.Value + "Minimized";
                            break;

                        case "2":
                            targetProperty.Value = targetProperty.Value + "Maximized";
                            break;
                    }
                    break;

                case "StartUpPosition":
                    // 0 - manual
                    // 1 - center owner
                    // 2 - center screen
                    // 3 - windows default
                    targetProperty.Name = "StartPosition";
                    targetProperty.Value = "System.Windows.Forms.FormStartPosition.";
                    switch (sourceProperty.Value)
                    {
                        case "0":
                            targetProperty.Value = targetProperty.Value + "Manual";
                            break;

                        case "1":
                            targetProperty.Value = targetProperty.Value + "CenterParent";
                            break;

                        case "2":
                            targetProperty.Value = targetProperty.Value + "CenterScreen";
                            break;

                        case "3":
                        default:
                            targetProperty.Value = targetProperty.Value + "WindowsDefaultLocation";
                            break;
                    }
                    break;

                default:
                    targetProperty.Name = sourceProperty.Name;
                    targetProperty.Value = sourceProperty.Value;
                    targetProperty.Valid = false;
                    break;
            }
            return validProperty;
        }

        private void ConvertFont(ControlProperty sourceProperty, ControlProperty targetProperty)
        {
            String FontName = String.Empty;
            Int32 FontSize = 0;
            Int32 FontCharSet = 0;
            Boolean FontBold = false;
            Boolean FontUnderline = false;
            Boolean FontItalic = false;
            Boolean FontStrikethrough = false;
            String Temp = String.Empty;
            //      BeginProperty Font
            //         Name            =   "Arial"
            //         Size            =   8.25
            //         Charset         =   238
            //         Weight          =   400
            //         Underline       =   0   'False
            //         Italic          =   0   'False
            //         Strikethrough   =   0   'False
            //      EndProperty

            foreach (ControlProperty oProperty in sourceProperty.PropertyList)
                switch (oProperty.Name)
                {
                    case "Name":
                        FontName = oProperty.Value;
                        break;

                    case "Size":
                        FontSize = GetFontSizeInt(oProperty.Value);
                        break;

                    case "Weight":
                        //        If tLogFont.lfWeight >= FW_BOLD Then
                        //          bFontBold = True
                        //        Else
                        //          bFontBold = False
                        //        End If
                        // FW_BOLD = 700
                        FontBold = Int32.Parse(oProperty.Value) >= 700;
                        break;

                    case "Charset":
                        FontCharSet = Int32.Parse(oProperty.Value);
                        break;

                    case "Underline":
                        FontUnderline = Int32.Parse(oProperty.Value) != 0;
                        break;

                    case "Italic":
                        FontItalic = Int32.Parse(oProperty.Value) != 0;
                        break;

                    case "Strikethrough":
                        FontStrikethrough = Int32.Parse(oProperty.Value) != 0;
                        break;
                }

            //      this.cmdExit.Font = new System.Drawing.Font("Tahoma", 12F,
            //        (System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Underline
            //        | System.Drawing.FontStyle.Strikeout), System.Drawing.GraphicsUnit.Point,
            //        ((System.Byte)(0)));

            // this.cmdExit.Font = new System.Drawing.Font("Tahoma", 12F,
            // System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point,
            // ((System.Byte)(238)));

            targetProperty.Name = "Font";
            targetProperty.Value = "new System.Drawing.Font(" + FontName + ",";
            targetProperty.Value = targetProperty.Value + FontSize.ToString() + "F,";

            Temp = String.Empty;
            if (FontBold)
                Temp = "System.Drawing.FontStyle.Bold";
            if (FontItalic)
            {
                if (Temp != String.Empty)
                    Temp = Temp + " | ";
                Temp = Temp + "System.Drawing.FontStyle.Italic";
            }
            if (FontUnderline)
            {
                if (Temp != String.Empty)
                    Temp = Temp + " | ";
                Temp = Temp + "System.Drawing.FontStyle.Underline";
            }
            if (FontStrikethrough)
            {
                if (Temp != String.Empty)
                    Temp = Temp + " | ";
                Temp = Temp + "System.Drawing.FontStyle.Strikeout";
            }
            if (Temp == String.Empty)
                targetProperty.Value = targetProperty.Value + " System.Drawing.FontStyle.Regular,";
            else
                targetProperty.Value = targetProperty.Value + " ( " + Temp + " ),";
            targetProperty.Value = targetProperty.Value + " System.Drawing.GraphicsUnit.Point, ";
            targetProperty.Value = targetProperty.Value + "((System.Byte)(" + FontCharSet.ToString() + ")))";
        }

        private Int32 GetFontSizeInt(String value)
        {
            Int32 Position = 0;

            Position = value.IndexOf(",", 0);
            if (Position > -1)
                return Int32.Parse(value.Substring(0, Position));

            Position = value.IndexOf(".", 0);
            if (Position > 0)
                return Int32.Parse(value.Substring(0, Position));
            return Int32.Parse(value);
        }

        private String GetColor(String value)
        {
            Color color = SystemColors.Control;
            String ColorValue;

            ColorValue = "0x" + value.Substring(2, value.Length - 3);
            color = ColorTranslator.FromWin32(Convert.ToInt32(ColorValue, 16));

            if (!color.IsSystemColor)
                if (color.IsNamedColor)
                    // System.Drawing.Color.Yellow;
                    return "System.Drawing.Color." + color.Name;
                else
                    return "System.Drawing.Color.FromArgb(" + color.ToArgb() + ")";
            else
                return "System.Drawing.SystemColors." + color.Name;
        }

        private String GetBool(String value)
        {
            if (Int32.Parse(value) == 0)
                return "false";
            else
                return "true";
        }

        private String GetSize(String height, String width, List<ControlProperty> propertyList)
        {
            Int32 HeightValue = 0;
            Int32 WidthValue = 0;

            // each property
            foreach (ControlProperty oProperty in propertyList)
            {
                if (oProperty.Name == height)
                    HeightValue = Int32.Parse(oProperty.Value) / 15;
                if (oProperty.Name == width)
                    WidthValue = Int32.Parse(oProperty.Value) / 15;
            }
            // 0, 120
            return WidthValue.ToString() + ", " + HeightValue.ToString();
        }

        private String GetLocation(List<ControlProperty> propertyList)
        {
            Int32 Left = 0;
            Int32 Top = 0;

            // each property
            foreach (ControlProperty oProperty in propertyList)
            {
                if (oProperty.Name == "Left")
                {
                    Left = Int32.Parse(oProperty.Value);
                    if (Left < 0)
                        Left = 75000 + Left;
                    Left = Left / 15;
                }
                if (oProperty.Name == "Top")
                    Top = Int32.Parse(oProperty.Value) / 15;
            }
            // 616, 520
            return Left.ToString() + ", " + Top.ToString();
        }

        public void GetFRXImage(String imageFile, Int32 imageOffset, out Byte[] imageString)
        {
            Byte[] header;
            Int32 bytesToRead = 0;

            // open file
            FileStream Stream = new FileStream(imageFile, FileMode.Open, FileAccess.Read);
            BinaryReader Reader = new BinaryReader(Stream);
            // Start from offset
            Reader.BaseStream.Seek(imageOffset, SeekOrigin.Begin);
            // Get the four byte header
            header = new Byte[4];
            header = Reader.ReadBytes(4);
            // Convert This Header Into The Number Of Bytes
            // To Read For This Image
            bytesToRead = header[0];
            bytesToRead += header[1] * 0x100;
            bytesToRead += header[2] * 0x10000;
            bytesToRead += header[3] * 0x1000000;
            // Get image information
            imageString = new Byte[bytesToRead];
            imageString = Reader.ReadBytes(bytesToRead);

            //      Stream = new FileStream( @"C:\temp\test\Ba.bmp", FileMode.CreateNew, FileAccess.Write );
            //      BinaryWriter Writer = new BinaryWriter( Stream );
            //      Writer.Write(ImageString, 8, ImageString.GetLength(0) - 8);
            //      Stream.Close();
            //      Writer.Close();

            //  FileStream inFile = new FileStream(@"C:\WINdows\Blue Lace 16.bmp", FileMode.Open, FileAccess.Read);
            //  ReturnImage = Image.FromStream(inFile, false);

            Stream.Close();
            Reader.Close();
        }
    }
}
