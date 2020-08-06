using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Resources;
using System.Text;
using VisualBasicUpgradeAssistant.Core.DataClasses;
using VisualBasicUpgradeAssistant.Core.Extensions;

namespace VisualBasicUpgradeAssistant.Core.Model
{
    /// <summary>
    /// Summary description for Convert.
    /// </summary>
    public class ConvertCode
    {
        private FileType _fileType;
        private Module _sourceModule;
        private Module _targetModule;
        private readonly ArrayList _ownerStock;
        private readonly Tools _tools;
        private const String FORM_FIRST_LINE = "VERSION 5.00";
        private const String MODULE_FIRST_LINE = "ATTRIBUTE";
        private const String CLASS_FIRST_LINE = "1.0 CLASS";

        private const String Indent2 = "  ";
        private const String Indent4 = "    ";
        private const String Indent6 = "      ";

        public ConvertCode()
        {
            _ownerStock = new ArrayList();
            _tools = new Tools();
        }

        public String ActionResult { get; private set; }

        public String OutSourceCode { get; private set; }

        /// <summary>
        /// Parse the file into CS code
        /// </summary>
        /// <param name="inputFile"></param>
        /// <param name="baseFolder"></param>
        /// <param name="outDir"></param>
        /// <returns></returns>
        public Boolean ParseFile(FileInfo inputFile, DirectoryInfo baseFolder, DirectoryInfo outDir)
        {
            String temp;
            String version = String.Empty;
            Int32 position;

            // try recognize source code type depend by file extension
            String extension = inputFile.Extension.ToLowerInvariant();
            _fileType = extension switch
            {
                ".frm" => FileType.Form,
                ".bas" => FileType.Module,
                ".cls" => FileType.Class,
                _ => FileType.Unknown,
            };

            // open file
            using FileStream inputStream = inputFile.OpenRead();
            StreamReader reader = new StreamReader(inputStream);

            String line = reader.ReadLine();
            // verify type of file based on first line - form, module, class

            // get first word from first line
            position = 0;
            temp = GetWord(line, ref position);
            switch (temp.ToUpper())
            {
                // module first line
                // 'Attribute VB_Name = "ModuleName"'
                case MODULE_FIRST_LINE:
                    _fileType = FileType.Module;
                    break;
                // form or class first line
                // 'VERSION 5.00' or 'VERSION 1.0 CLASS'
                case "VERSION":
                    position++;
                    version = GetWord(line, ref position);

                    //Debug.WriteLine (Line + " " + Version);

                    if (line.IndexOf(CLASS_FIRST_LINE, 0) > -1)
                        _fileType = FileType.Class;
                    else
                        _fileType = FileType.Form;
                    break;

                default:
                    _fileType = FileType.Unknown;
                    break;
            }
            // if file is still unknown
            if (_fileType == FileType.Unknown)
            {
                ActionResult = "Unknown file type";
                return false;
            }

            _sourceModule = new Module
            {
                Version = version,
                FileName = inputFile.FullName
            };

            // now parse specifics of each type
            switch (_fileType)
            {
                case FileType.Form:
                    _sourceModule.Type = FileType.Form;
                    ParseForm(reader);
                    break;

                case FileType.Module:
                    _sourceModule.Type = FileType.Module;
                    ParseModule(reader);
                    break;

                case FileType.Class:
                    _sourceModule.Type = FileType.Class;
                    ParseClass(reader);
                    break;
            }
            // parse remain - variables, functions, procedures
            ParseProcedures(reader);

            inputStream.Close();
            reader.Close();

            // generate output file
            OutSourceCode = GenerateOutput(baseFolder, outDir);

            return true;
        }

        private Boolean ParseForm(StreamReader reader)
        {
            Boolean process = false;
            Boolean finish = false;
            Boolean end = true;
            String line;
            String name;
            String owner = null;
            String word;
            Int32 index;
            Int32 comment;
            String type;
            Int32 position = 0;
            Int32 level = 0;
            ControlType control = null;
            ControlProperty nestedProperty = null;
            Boolean bNestedProperty = false;

            // parse only visual part of form
            while (!finish) // ( ( bFinish || (oReader.Peek() > -1)) )
            {
                line = reader.ReadLine();
                line = line.Trim();
                position = 0;
                // get first word in line
                word = GetWord(line, ref position);
                switch (word)
                {
                    case "Begin":
                        process = true;
                        // new level
                        level++;
                        // next word - control type
                        position++;
                        type = GetWord(line, ref position);
                        // next word - control name
                        position++;
                        name = GetWord(line, ref position);
                        // detected missing end -> it indicate that next control is container
                        if (!end)
                        {
                            // add container control to colection
                            if (!(control == null))
                            {
                                control.Container = true;
                                _sourceModule.ControlList.Add(control);
                            }
                            // save name of previous control as owner for current and next controls
                            _ownerStock.Add(owner);
                        }
                        end = false;

                        switch (type)
                        {
                            case "Form":            // VERSION 2.00 - VB3
                            case "VB.Form":
                            case "VB.MDIForm":
                                _sourceModule.Name = name;
                                // first owner
                                // save control name for possible next controls as owner
                                owner = name;
                                break;

                            default:
                                // new control
                                control = new ControlType
                                {
                                    Name = name,
                                    Type = type
                                };
                                // save control name for possible next controls as owner
                                owner = name;
                                // set current container name
                                control.Owner = (String)_ownerStock[_ownerStock.Count - 1];
                                break;
                        }
                        break;

                    case "End":
                        // double end - we leaving some container
                        if (end)
                            // remove last item from stock
                            _ownerStock.Remove((String)_ownerStock[_ownerStock.Count - 1]);
                        else
                            // level 1 is form and all higher levels are controls
                            if (level > 1)
                            // add control to colection
                            _sourceModule.ControlList.Add(control);
                        // form or control end detected
                        end = true;
                        // back to previous level
                        level--;

                        break;

                    case "Object":
                        // used controls in form
                        break;

                    case "BeginProperty":
                        bNestedProperty = true;

                        nestedProperty = new ControlProperty();
                        // next word - nested property name
                        position++;
                        name = GetWord(line, ref position);
                        nestedProperty.Name = name;
                        //            Debug.WriteLine(sName);
                        break;

                    case "EndProperty":
                        bNestedProperty = false;
                        // add property to control or form
                        if (level == 1)
                            // add property to form
                            _sourceModule.FormPropertyList.Add(nestedProperty);
                        else
                            // to controls
                            control.PropertyList.Add(nestedProperty);
                        break;

                    default:
                        // parse property
                        ControlProperty property = new ControlProperty();

                        index = line.IndexOf("=");
                        if (index > -1)
                        {
                            property.Name = line.Substring(0, index - 1).Trim();
                            comment = line.IndexOf("'", index);
                            if (comment > -1)
                            {
                                property.Value = line.Substring(index + 1, comment - index - 1).Trim();
                                property.Comment = line.Substring(comment + 1, line.Length - comment - 1).Trim();
                            }
                            else
                                property.Value = line.Substring(index + 1, line.Length - index - 1).Trim();

                            if (bNestedProperty)
                                nestedProperty.PropertyList.Add(property);
                            else
                                // depend by level insert property to form or control
                                if (level > 1)
                                // add property to control
                                control.PropertyList.Add(property);
                            else
                                // add property to form
                                _sourceModule.FormPropertyList.Add(property);
                        }
                        break;
                }

                if (level == 0 && process)
                    // visual part of form is finish
                    finish = true;
            }
            return true;
        }

        private Boolean ParseModule(StreamReader reader)
        {
            String line;
            Int32 position;

            // name of module
            // Attribute VB_Name = "ModuleName"

            // Start from begin again
            reader.DiscardBufferedData();
            reader.BaseStream.Seek(0, SeekOrigin.Begin);
            // search for module name
            while (reader.Peek() > -1)
            {
                line = reader.ReadLine();
                position = line.IndexOf('"');
                _sourceModule.Name = line.Substring(position + 1, line.Length - position - 2);
                return true;
            }
            return false;
        }

        private Boolean ParseClass(StreamReader reader)
        {
            Int32 position = 0;
            String line;
            String word;

            //VERSION 1.0 CLASS
            //BEGIN
            //  MultiUse = -1  'True
            //END
            //Attribute VB_Name = "CList"
            //Attribute VB_GlobalNameSpace = False
            //Attribute VB_Creatable = True
            //Attribute VB_PredeclaredId = False
            //Attribute VB_Exposed = True

            // Start from begin again
            reader.DiscardBufferedData();
            reader.BaseStream.Seek(0, SeekOrigin.Begin);
            while (reader.Peek() > -1)
            {
                position = 0;
                // verify type of file based on first line
                // form, module, class
                line = reader.ReadLine();
                // next word - control type
                word = GetWord(line, ref position);
                if (word == "Attribute")
                {
                    position++;
                    word = GetWord(line, ref position);
                    switch (word)
                    {
                        case "VB_Name":
                            position++;
                            word = GetWord(line, ref position);
                            position++;
                            _sourceModule.Name = GetWord(line, ref position);
                            break;

                        case "VB_Exposed":
                            return true;
                            //break;
                    }
                }
            }
            return false;
        }

        private Boolean ParseProcedures(StreamReader reader)
        {
            String line;
            String tempString;
            String comments = null;
            String scope;

            Int32 position = 0;
            //bool bProcess = false;

            Boolean bEnum = false;
            Boolean bVariable = false;
            Boolean bProperty = false;
            Boolean bProcedure = false;
            Boolean bEnd = false;

            Variable variable = null;
            Property property = null;
            Procedure procedure = null;
            EnumType enumType = null;
            EnumItem enumItem = null;

            while (reader.Peek() > -1)
            {
                line = reader.ReadLine();
                //Line = Line.Trim();

                position = 0;

                if (line != null && line != String.Empty)
                    // check if next line is same command, join it together ?
                    while (line.Substring(line.Length - 1, 1) == "_")
                        line = line + reader.ReadLine();
                // : is command delimiter

                //  Debug.WriteLine(Line);

                // get first word in line
                tempString = GetWord(line, ref position);
                switch (tempString)
                {
                    // ignore this section

                    //Attribute VB_Name = "frmAttachement"
                    // ...
                    //Option Explicit
                    case "Attribute":
                    case "Option":
                        break;

                    // comments
                    case "'":
                        comments = comments + line + "\r\n";
                        break;

                    // next can be declaration of variables

                    //Private mlParentID As Long
                    //Private mlOwnerType As ENUM_FORM_TYPE
                    //Private moAttachement As Attachement

                    case "Public":
                    case "Private":
                        // save it for later use
                        scope = tempString.ToLower();
                        // read next word
                        // next word - control type
                        position++;
                        tempString = GetWord(line, ref position);

                        switch (tempString)
                        {
                            // functions or procedures
                            case "Sub":
                            case "Function":

                                procedure = new Procedure
                                {
                                    Comment = comments
                                };
                                comments = String.Empty;
                                ParseProcedureName(procedure, line);

                                bProcedure = true;
                                break;

                            case "Enum":
                                enumType = new EnumType
                                {
                                    Scope = scope
                                };
                                // next word is enum name
                                position++;
                                enumType.Name = GetWord(line, ref position);
                                bEnum = true;
                                break;

                            case "Property":
                                property = new Property
                                {
                                    Comment = comments
                                };
                                comments = String.Empty;
                                ParsePropertyName(property, line);
                                bProperty = true;

                                break;

                            default:
                                // variable declaration
                                variable = ParseVariableDeclaration(line);
                                bVariable = true;
                                break;
                        }

                        break;

                    case "Dim":
                        // variable declaration
                        variable = ParseVariableDeclaration(line);
                        variable.Comment = comments;
                        comments = String.Empty;
                        bVariable = true;
                        break;

                    // functions or procedures
                    case "Sub":
                    case "Function":

                        break;

                    case "End":
                        bEnd = true;
                        break;

                    default:
                        if (bEnum)
                        {
                            // first word is name, second =, thirt value if is preset
                            enumItem = new EnumItem
                            {
                                Comment = comments
                            };
                            comments = String.Empty;
                            ParseEnumItem(enumItem, line);
                            // add item
                            enumType.ItemList.Add(enumItem);
                        }
                        if (bProperty)
                            // add line of property
                            property.LineList.Add(line);
                        if (bProcedure)
                            procedure.LineList.Add(line);
                        break;

                        // events
                        //Private Sub cmdCancel_Click()
                        //  mbEdit = False
                        //  If mbNew Then
                        //    Unload Me
                        //  Else
                        //    ShowCurRec
                        //    SetControls False
                        //  End If
                        //End Sub
                        //
                        //Private Sub cmdClose_Click()
                        //  Unload Me
                        //End Sub
                }

                // if something end
                if (bEnd)
                {
                    //
                    if (bEnum)
                    {
                        _sourceModule.EnumList.Add(enumType);
                        bEnum = false;
                    }
                    if (bProperty)
                    {
                        _sourceModule.PropertyList.Add(property);
                        bProperty = false;
                    }
                    if (bProcedure)
                    {
                        _sourceModule.ProcedureList.Add(procedure);
                        bProcedure = false;
                    }
                    bEnd = false;
                }
                else
                    if (bVariable)
                    _sourceModule.VariableList.Add(variable);

                bVariable = false;
            }

            return true;
        }

        //Public Enum ENUM_BUG_LEVEL
        //  BUG_LEVEL_PROJECT = 1
        //  BUG_LEVEL_VERSION = 2
        //End Enum
        private void ParseEnumItem(EnumItem enumItem, String line)
        {
            String TempString = String.Empty;
            Int32 iPosition = 0;

            line = line.Trim();
            // first word is ame
            enumItem.Name = GetWord(line, ref iPosition);
            iPosition++;
            // next word =
            TempString = GetWord(line, ref iPosition);
            iPosition++;
            // optional
            enumItem.Value = GetWord(line, ref iPosition);
        }

        //Private mlID As Long

        public Variable ParseVariableDeclaration(String line)
        {
            Variable variable = new Variable();
            String TempString = String.Empty;
            Int32 iPosition = 0;
            Boolean Status = false;

            // next word - control type
            TempString = GetWord(line, ref iPosition);
            switch (TempString)
            {
                case "Dim":
                case "Private":
                    variable.Scope = "private";
                    break;

                case "Public":
                    variable.Scope = "public";
                    break;

                default:
                    variable.Scope = "private";
                    // variable name
                    variable.Name = TempString;
                    Status = true;
                    break;
            }

            // variable name
            if (!Status)
            {
                iPosition++;
                TempString = GetWord(line, ref iPosition);
                variable.Name = TempString;
            }
            // As
            iPosition++;
            TempString = GetWord(line, ref iPosition);
            // variable type
            iPosition++;
            TempString = GetWord(line, ref iPosition);
            variable.Type = TempString;

            return variable;
        }

        // properties Let, Get, Set
        //Public Property Let ParentID(ByVal lValue As Long)
        //  mlParentID = lValue
        //End Property
        //
        //Public Property Get FormType() As ENUM_FORM_TYPE
        //  FormType = FORM_ATTACHEMENT
        //End Property

        private void ParsePropertyName(Property property, String line)
        {
            String TempString = String.Empty;
            Int32 iPosition = 0;
            Int32 Start = 0;
            Boolean Status = false;

            // next word - control type
            TempString = GetWord(line, ref iPosition);
            switch (TempString)
            {
                case "Private":
                    property.Scope = "private";
                    break;

                case "Public":
                    property.Scope = "public";
                    break;

                default:
                    property.Scope = "private";
                    Status = true;
                    break;
            }

            if (!Status)
            {
                // property
                iPosition++;
                TempString = GetWord(line, ref iPosition);
            }

            // direction Let,Get, Set
            iPosition++;
            TempString = GetWord(line, ref iPosition);
            property.Direction = TempString;

            //Public Property Let ParentID(ByVal lValue As Long)

            // name
            Start = iPosition;
            iPosition = line.IndexOf("(", Start + 1);
            property.Name = line.Substring(Start, iPosition - Start);

            // + possible parameters
            iPosition++;
            Start = iPosition;
            iPosition = line.IndexOf(")", Start);

            if (iPosition - Start > 0)
            {
                TempString = line.Substring(Start, iPosition - Start);
                List<Parameter> ParameterList = new List<Parameter>();
                // process parametres
                ParseParametries(ParameterList, TempString);
                property.ParameterList = ParameterList;
            }

            // As
            iPosition++;
            iPosition++;
            TempString = GetWord(line, ref iPosition);

            // type
            iPosition++;
            TempString = GetWord(line, ref iPosition);
            property.Type = TempString;
        }

        // ByVal lValue As Long, ByVal sValue As string

        private void ParseParametries(List<Parameter> parametreList, String line)
        {
            Boolean bFinish = false;
            Int32 Position = 0;
            Int32 Start = 0;
            Boolean Status = false;
            String TempString = String.Empty;

            // parameters delimited by comma
            while (!bFinish)
            {
                Parameter oParameter = new Parameter();

                // next word - control type
                TempString = GetWord(line, ref Position);
                switch (TempString)
                {
                    case "Optional":

                        break;

                    case "ByVal":
                    case "ByRef":
                        oParameter.Pass = TempString;
                        break;
                    // missing is byref
                    default:
                        oParameter.Pass = "ByRef";
                        // variable name
                        oParameter.Name = TempString;
                        Status = true;
                        break;
                }

                // variable name
                if (!Status)
                {
                    Position++;
                    TempString = GetWord(line, ref Position);
                    oParameter.Name = TempString;
                }
                // As
                Position++;
                TempString = GetWord(line, ref Position);
                // parameter type
                Position++;
                TempString = GetWord(line, ref Position);
                oParameter.Type = TempString;

                parametreList.Add(oParameter);

                // next parameter
                Position++;
                Start = Position;
                Position = line.IndexOf(",", Start);

                if (Position == -1)
                    // end
                    bFinish = true;
            }
        }

        private void ParseProcedureName(Procedure procedure, String line)
        {
            String tempString = String.Empty;
            Int32 position = 0;
            Int32 start = 0;
            Boolean status = false;

            //Private Sub cmdOk_Click()
            //private void cmdShow_Click(object sender, System.EventArgs e)

            //Private Sub Form_Load()
            //private void frmConvert_Load(object sender, System.EventArgs e)

            //Public Function Rozbor_DefaultFields(ByVal MKf As String) As String
            //public static bool ParseProcedures( Module SourceModule, Module TargetModule )

            tempString = GetWord(line, ref position);
            switch (tempString)
            {
                case "Private":
                    procedure.Scope = "private";
                    status = true;
                    break;

                case "Public":
                    procedure.Scope = "public";
                    status = true;
                    break;

                default:
                    procedure.Scope = "private";
                    status = true;
                    break;
            }

            if (status)
            {
                // property
                position++;
                tempString = GetWord(line, ref position);
            }

            // procedure type
            switch (tempString)
            {
                case "Sub":
                    procedure.Type = ProcedureType.Subroutine;
                    break;

                case "Function":
                    procedure.Type = ProcedureType.Function;
                    break;

                case "Event":
                    procedure.Type = ProcedureType.Event;
                    break;
            }

            // next is name
            position++;
            start = position;
            position = line.IndexOf("(", start);
            procedure.Name = line.Substring(start, position - start);

            // next possible parameters
            position++;
            start = position;
            position = line.IndexOf(")", start);

            if (position - start > 0)
            {
                tempString = line.Substring(start, position - start);
                List<Parameter> ParameterList = new List<Parameter>();
                // process parametres
                ParseParametries(ParameterList, tempString);
                procedure.ParameterList = ParameterList;
            }

            // and return type of function
            if (procedure.Type == ProcedureType.Function)
            {
                // as
                position++;
                tempString = GetWord(line, ref position);
                // function return type
                position++;
                procedure.ReturnType = GetWord(line, ref position);
            }
        }

        // generate result file
        // OutPath for pictures
        private String GenerateOutput(DirectoryInfo basePath, DirectoryInfo outPath)
        {
            // Setup output files.
            StringBuilder result = new StringBuilder();
            StringBuilder design = new StringBuilder();

            // convert source to target
            _targetModule = new Module();
            _tools.ParseModule(_sourceModule, _targetModule);

            // Try to figure out namespace
            String baseFullName = outPath.Parent.Parent.FullName;
            String outputFullName = outPath.FullName;
            String projectName = outputFullName.Substring(baseFullName.Length);
            projectName = projectName.Replace(Path.DirectorySeparatorChar, '.');
            projectName = projectName.Replace(Path.AltDirectorySeparatorChar, '.');
            if (projectName.StartsWith("."))
                projectName = projectName.Substring(1);

            // ********************************************************
            // common class
            // ********************************************************
            result.AppendLine("using Microsoft.VisualBasic;");
            result.AppendLine("using System;");

            design.AppendLine("using Microsoft.VisualBasic;");
            design.AppendLine("using System;");

            // ********************************************************
            // only form class
            // ********************************************************
            if (_targetModule.Type == FileType.Form)
            {
                design.AppendLine("using System.Drawing;");
                design.AppendLine("using System.Collections;");
                design.AppendLine("using System.ComponentModel;");
                design.AppendLine("using System.Windows.Forms;");
            }

            // standard header
            result.AppendLine();
            result.AppendLine($"namespace {projectName}");
            // start namepsace region
            result.AppendLine("{");
            result.AppendLine(Indent2 + "/// <summary>");
            result.AppendLine(Indent2 + "/// Summary description for " + _sourceModule.Name + ".");
            result.AppendLine(Indent2 + "/// </summary>");

            // and for desing
            design.AppendLine();
            design.AppendLine($"namespace {projectName}");
            // start namepsace region
            design.AppendLine("{");
            design.AppendLine(Indent2 + "/// <summary>");
            design.AppendLine(Indent2 + "/// Summary description for " + _sourceModule.Name + ".");
            design.AppendLine(Indent2 + "/// </summary>");

            switch (_targetModule.Type)
            {
                case FileType.Form:
                    design.AppendLine(Indent2 + "public partial class " + _sourceModule.Name + " : System.Windows.Forms.Form");
                    result.AppendLine(Indent2 + "public partial class " + _sourceModule.Name + " : System.Windows.Forms.Form");
                    break;

                case FileType.Module:
                    result.AppendLine(Indent2 + "public sealed class " + _sourceModule.Name);
                    // all procedures must be static
                    break;

                case FileType.Class:
                    result.AppendLine(Indent2 + "public class " + _sourceModule.Name);
                    break;
            }
            // start class region
            result.AppendLine(Indent2 + "{");
            design.AppendLine(Indent2 + "{");

            // ********************************************************
            // only form class
            // ********************************************************

            if (_targetModule.Type == FileType.Form)
            {
                // list of controls
                foreach (ControlType oControl in _targetModule.ControlList)
                {
                    if (!oControl.Valid)
                        design.Append("//");
                    design.AppendLine(Indent2 + " private System.Windows.Forms." + oControl.Type + " " + oControl.Name + ";");
                }

                design.Append(Indent4 + "/// <summary>\r\n");
                design.Append(Indent4 + "/// Required designer variable.\r\n");
                design.Append(Indent4 + "/// </summary>\r\n");
                design.Append(Indent4 + "private System.ComponentModel.Container components = null;\r\n");
                design.Append("\r\n");
                design.Append(Indent4 + "public " + _sourceModule.Name + "()\r\n");
                design.Append(Indent4 + "{\r\n");
                design.Append(Indent6 + "// Required for Windows Form Designer support\r\n");
                design.Append(Indent6 + "InitializeComponent();\r\n");
                design.Append("\r\n");
                design.Append(Indent6 + "// TODO: Add any constructor code after InitializeComponent call\r\n");
                design.Append(Indent4 + "}\r\n");

                design.Append(Indent4 + "/// <summary>\r\n");
                design.Append(Indent4 + "/// Clean up any resources being used.\r\n");
                design.Append(Indent4 + "/// </summary>\r\n");
                design.Append(Indent4 + "protected override void Dispose( bool disposing )\r\n");
                design.Append(Indent4 + "{\r\n");
                design.Append(Indent6 + "if( disposing )\r\n");
                design.Append(Indent6 + "{\r\n");
                design.Append(Indent6 + "  if (components != null)\r\n");
                design.Append(Indent6 + "  {\r\n");
                design.Append(Indent6 + "    components.Dispose();\r\n");
                design.Append(Indent6 + "  }\r\n");
                design.Append(Indent6 + "}\r\n");
                design.Append(Indent6 + "base.Dispose( disposing );\r\n");
                design.Append(Indent4 + "}\r\n");

                design.Append(Indent4 + "#region Windows Form Designer generated code\r\n");
                design.Append(Indent4 + "/// <summary>\r\n");
                design.Append(Indent4 + "/// Required method for Designer support - do not modify\r\n");
                design.Append(Indent4 + "/// the contents of this method with the code editor.\r\n");
                design.Append(Indent4 + "/// </summary>\r\n");
                design.Append(Indent4 + "private void InitializeComponent()\r\n");
                design.Append(Indent4 + "{\r\n");

                // if form contain images
                if (_targetModule.ImagesUsed)
                    // System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(Form1));
                    design.Append(Indent6 + "System.Resources.ResourceManager resources = " +
                      "new System.Resources.ResourceManager(typeof(" + _targetModule.Name + "));\r\n");

                foreach (ControlType oControl in _targetModule.ControlList)
                {
                    if (!oControl.Valid)
                        design.Append("//");
                    design.Append(Indent6 + "this." + oControl.Name
                      + " = new System.Windows.Forms." + oControl.Type
                      + "();\r\n");
                }

                // SuspendLayout part
                design.Append(Indent6 + "this.SuspendLayout();\r\n");
                // this.Frame1.ResumeLayout(false);
                // resume layout for each container
                foreach (ControlType control in _targetModule.ControlList)
                    // check if control is container
                    // !! for menu controls
                    if (control.Container && !(control.Type == "MenuItem") && !(control.Type == "MainMenu"))
                    {
                        if (!control.Valid)
                            design.Append("//");
                        design.Append(Indent6 + "this." + control.Name + ".SuspendLayout();\r\n");
                    }

                // each controls and his property
                foreach (ControlType control in _targetModule.ControlList)
                {
                    design.Append(Indent6 + "//\r\n");
                    design.Append(Indent6 + "// " + control.Name + "\r\n");
                    design.Append(Indent6 + "//\r\n");

                    // unsupported control
                    if (!control.Valid)
                        design.Append("/*");
                    // ImageList, Timer, Menu has't name property
                    if (control.Type != "ImageList" && control.Type != "Timer"
                      && control.Type != "MenuItem" && control.Type != "MainMenu")
                        // control name
                        design.Append(Indent6 + "this." + control.Name + ".Name = "
                          + (Char)34 + control.Name + (Char)34 + ";\r\n");

                    // write properties
                    foreach (ControlProperty oProperty in control.PropertyList)
                        GetPropertyRow(design, control.Type, control.Name, oProperty, outPath);

                    // if control is container for other controls
                    String temp = String.Empty;
                    foreach (ControlType control1 in _targetModule.ControlList)
                        // all controls ownered by current control
                        if (control1.Owner == control.Name && !control1.InvisibleAtRuntime)
                            temp = temp + Indent6 + Indent6 + "this." + control1.Name + ",\r\n";
                    if (temp != String.Empty)
                    {
                        // exception for menu controls
                        if (control.Type == "MainMenu" || control.Type == "MenuItem")
                            // this.mainMenu1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[]
                            design.Append(Indent6 + "this." + control.Name
                              + ".MenuItems.AddRange(new System.Windows.Forms.MenuItem[]\r\n");
                        else
                            // this. + oControl.Name + .Controls.AddRange(new System.Windows.Forms.Control[]
                            design.Append(Indent6 + "this." + control.Name
                              + ".Controls.AddRange(new System.Windows.Forms.Control[]\r\n");

                        design.Append(Indent6 + "{\r\n");
                        design.Append(temp);
                        // remove last comma, keep CRLF
                        design.Remove(design.Length - 3, 1);
                        // close addrange part
                        design.Append(Indent6 + "});\r\n");
                    }
                    // unsupported control
                    if (!control.Valid)
                        design.Append("*/");
                }

                design.Append(Indent6 + "//\r\n");
                design.Append(Indent6 + "// " + _sourceModule.Name + "\r\n");
                design.Append(Indent6 + "//\r\n");
                design.Append(Indent6 + "this.Controls.AddRange(new System.Windows.Forms.Control[]\r\n");
                design.Append(Indent6 + "{\r\n");

                // add control range to form
                foreach (ControlType oControl in _targetModule.ControlList)
                {
                    if (!oControl.Valid)
                        design.Append("//");
                    // all controls ownered by main form
                    if (oControl.Owner == _sourceModule.Name && !oControl.InvisibleAtRuntime)
                        design.Append(Indent6 + "      this." + oControl.Name + ",\r\n");
                }

                // remove last comma, keep CRLF
                //design.Remove(result.Length - 3, 1);
                // close addrange part
                design.Append(Indent6 + "});\r\n");

                // form name
                design.Append(Indent6 + "this.Name = " + (Char)34 + _targetModule.Name + (Char)34 + ";\r\n");
                // exception for menu
                // this.Menu = this.mainMenu1;
                if (_targetModule.MenuUsed)
                    foreach (ControlType control in _targetModule.ControlList)
                        if (control.Type == "MainMenu")
                            design.Append(Indent6 + "      this.Menu = " + control.Name + ";\r\n");
                // form properties
                foreach (ControlProperty property in _targetModule.FormPropertyList)
                {
                    if (!property.Valid)
                        design.Append("//");
                    GetPropertyRow(design, _targetModule.Type.ToString(), "", property, outPath);
                }

                // this.CancelButton = this.cmdExit;

                // this.Frame1.ResumeLayout(false);
                // resume layout for each container
                foreach (ControlType control in _targetModule.ControlList)
                    // check if control is container
                    if (control.Container && !(control.Type == "MenuItem") && !(control.Type == "MainMenu"))
                    {
                        if (!control.Valid)
                            design.Append("//");
                        design.Append(Indent6 + "this." + control.Name + ".ResumeLayout(false);\r\n");
                    }
                // form
                design.Append(Indent6 + "this.ResumeLayout(false);\r\n");

                design.Append(Indent4 + "}\r\n");
                design.Append(Indent4 + "#endregion\r\n");
            } // if (mTargetModule.Type = "form")

            // ********************************************************
            // enums
            // ********************************************************

            if (_targetModule.EnumList.Count > 0)
            {
                result.Append("\r\n");
                foreach (EnumType oEnum in _targetModule.EnumList)
                {
                    // public enum VB_FILE_TYPE
                    result.Append(Indent4 + oEnum.Scope + " enum " + oEnum.Name + "\r\n");
                    result.Append(Indent4 + "{\r\n");

                    foreach (EnumItem oEnumItem in oEnum.ItemList)
                    {
                        // name
                        result.Append(Indent6 + oEnumItem.Name);

                        if (oEnumItem.Value != String.Empty)
                            result.Append(" = " + oEnumItem.Value);
                        // enum items delimiter
                        result.Append(",\r\n");
                    }
                    // remove last comma, keep CRLF
                    result.Remove(result.Length - 3, 1);
                    // end enum
                    result.Append(Indent4 + "};\r\n");
                }
            }

            // ********************************************************
            //  variables for al module types
            // ********************************************************

            if (_targetModule.VariableList.Count > 0)
            {
                result.Append("\r\n");

                foreach (Variable variable in _targetModule.VariableList)
                    // string Result = null;
                    result.Append(Indent4 + variable.Scope + " " + variable.Type + " " + variable.Name + ";\r\n");
            }

            // ********************************************************
            // properties has only forms and classes
            // ********************************************************

            if (_targetModule.Type == FileType.Form || _targetModule.Type == FileType.Class)
                // properties
                if (_targetModule.PropertyList.Count > 0)
                {
                    // new line
                    result.Append("\r\n");
                    //public string Comment
                    //{
                    //  get { return mComment; }
                    //  set { mComment = value; }
                    //}
                    foreach (Property property in _targetModule.PropertyList)
                    {
                        // possible comment
                        result.Append(property.Comment + ";\r\n");
                        // string Result = null;
                        result.Append(Indent4 + property.Scope + " " + property.Type + " " + property.Name + ";\r\n");
                        result.Append(Indent4 + "{\r\n");
                        result.Append(Indent6 + "get { return ; }\r\n");
                        result.Append(Indent6 + "set {  = value; }\r\n");

                        // lines
                        foreach (String Line in property.LineList)
                        {
                            String temp = Line.Trim();
                            if (temp.Length > 0)
                                result.Append(Indent6 + temp + ";\r\n");
                            else
                                result.Append("\r\n");
                        }
                        result.Append(Indent4 + "}\r\n");
                    }
                }

            // ********************************************************
            // procedures
            // ********************************************************

            if (_targetModule.ProcedureList.Count > 0)
            {
                result.Append("\r\n");
                foreach (Procedure oProcedure in _targetModule.ProcedureList)
                {
                    // private void WriteResX ( ArrayList mImageList, string OutPath, string ModuleName )
                    result.Append(Indent4 + oProcedure.Scope + " ");
                    switch (oProcedure.Type)
                    {
                        case ProcedureType.Subroutine:
                            result.Append("void");
                            break;

                        case ProcedureType.Function:
                            result.Append(oProcedure.ReturnType);
                            break;

                        case ProcedureType.Event:
                            result.Append("void");
                            break;
                    }
                    // name
                    result.Append(" " + oProcedure.Name);
                    // parametres
                    if (oProcedure.ParameterList.Count > 0)
                    {
                    }
                    else
                        result.Append("()\r\n");

                    // start body
                    result.Append(Indent4 + "{\r\n");

                    foreach (String Line in oProcedure.LineList)
                    {
                        String temp = Line.Trim();
                        if (temp.Length > 0)
                            result.Append(Indent6 + temp + ";\r\n");
                        else
                            result.Append("\r\n");
                    }
                    // end procedure
                    result.Append(Indent4 + "}\r\n");
                }
            }

            // end class
            result.AppendLine(Indent2 + "}");
            design.AppendLine(Indent2 + "}");

            // end namespace
            result.AppendLine("}");
            design.AppendLine("}");

            // save result
            FileInfo outFileName = outPath.PathCombineFile(_targetModule.FileName);
            File.WriteAllText(outFileName.FullName, result.ToString());

            // save design file
            if (_targetModule.Type == FileType.Form)
            {
                String designFile = Path.GetFileNameWithoutExtension(_targetModule.FileName);
                designFile = $"{designFile}.Designer.cs";
                FileInfo designFilePath = outPath.PathCombineFile(designFile);
                File.WriteAllText(designFilePath.FullName, design.ToString());
            }

            // generate resx file if source form contain any images
            if (_targetModule.ImagesUsed)
                WriteResX(_targetModule.ImageList, outPath, _targetModule.Name);

            // return result
            return result.ToString();
        }

        private void WriteResX(List<String> imageList, DirectoryInfo outPath, String moduleName)
        {
            if (imageList.Count > 0)
            {
                // resx name
                FileInfo resxName = outPath.PathCombineFile($"{moduleName}.resx");
                // open file
                ResXResourceWriter rsxw = new ResXResourceWriter(resxName.FullName);

                foreach (String resourceName in imageList)
                    try
                    {
                        DirectoryInfo resourcePath = outPath.Parent.PathCombineDirectory("Resources");
                        if (!resourcePath.Exists)
                            resourcePath.Create();

                        Image img = Image.FromFile(resourcePath.PathCombineFile(resourceName).FullName);
                        rsxw.AddResource(resourceName, img);
                        img.Dispose();
                    }
                    catch
                    {
                    }
                // rsxw.Generate();
                rsxw.Close();

                foreach (String ResourceName in imageList)
                    outPath.PathCombineFile(ResourceName).Delete();
            }
        }

        private Boolean WriteImage(Module sourceModule, String resourceName, String value, DirectoryInfo outPath)
        {
            String Temp = String.Empty;
            Int32 Offset = 0;
            String FrxFile = String.Empty;
            String sResxName = String.Empty;
            Int32 Position = 0;

            Position = value.IndexOf(":", 0);
            // "Form1.frx":0000;
            // old vb3 code has name without ""
            // CONTROL.FRX:0000

            if (sourceModule.Version == "5.00")
                FrxFile = value.Substring(1, Position - 2);
            else
                FrxFile = value.Substring(0, Position);

            Temp = value.Substring(Position + 1, value.Length - Position - 1);
            Offset = Convert.ToInt32("0x" + Temp, 16);
            // exist file ?

            // get image

            _tools.GetFRXImage(Path.GetDirectoryName(sourceModule.FileName) + @"\" + FrxFile, Offset, out Byte[] imageString);

            if (imageString.GetLength(0) - 8 > 0)
            {
                DirectoryInfo resourceDir = outPath.Parent.PathCombineDirectory("Resources");
                if (!resourceDir.Exists)
                {
                    resourceDir.Create();
                }
                FileInfo resourcePath = resourceDir.PathCombineFile(resourceName);
                if (resourcePath.Exists)
                {
                    resourcePath.Delete();
                }
                using FileStream Stream = resourcePath.Create();
                BinaryWriter Writer = new BinaryWriter(Stream);
                Writer.Write(imageString, 8, imageString.GetLength(0) - 8);
                Stream.Close();
                Writer.Close();

                // write
                _targetModule.ImageList.Add(resourceName);
                return true;
            }
            else
                return false;
            // save it to resx
            //      Debug.WriteLine(ModuleName + ", " + ResourceName + ", " + Temp + ", " + oImage.Width.ToString() );
            //
            //      ResXResourceWriter oWriter;
            //
            //      sResxName = @"C:\temp\test\" + ModuleName + ".resx";
            //      oWriter = new ResXResourceWriter(sResxName);
            //     // oImage = Image.FromFile(myRow[sField].ToString());
            //      oWriter.AddResource(ResourceName, oImage);
            //      oWriter.Generate();
            //      oWriter.Close();

            //      MemoryStream s = new MemoryStream();
            //      oImage.Save(s);
            //
            //      byte[] b = s.ToArray();
            //      String strContents = System.Security.Cryptography.EncodeAsBase64.EncodeBuffer(b);
            //      myXmlTextNode.Value = strContents;

            /* Add Convert it back using.... */
            //
            //byte[]  b =
            //System.Security.Cryptography.DecodeBase64.DecodeBuffer(myXmlTextNode.Value);

            //      ResourceWriter oWriter;
            //
            //      sResxName = @"c:\temp\" + ModuleName + ".resx";
            //      oWriter = new ResourceWriter(sResxName);
            //     // oImage = Image.FromFile(myRow[sField].ToString());
            //      oWriter.AddResource(ResourceName, oImage);
            //      oWriter.Close();
        }

        private String GetWord(String line, ref Int32 position)
        {
            String Result = null;
            Int32 End = 0;

            if (position < line.Length)
            {
                // seach for first space
                End = line.IndexOf(" ", position);
                if (End > -1)
                {
                    Result = line.Substring(position, End - position);
                    position = End;
                }
                else
                    Result = line.Substring(position);
            }
            return Result;
        }

        private void GetPropertyRow(StringBuilder result, String type,
                                    String name, ControlProperty property, DirectoryInfo outPath)
        {
            // exception for images
            if (property.Name == "Icon" || property.Name == "Image" || property.Name == "BackgroundImage")
            {
                // generate resx file and write there image extracted from VB6 frx file
                String ResourceName = String.Empty;

                //.BackgroundImage = ((System.Drawing.Bitmap)(resources.GetObject("$this.BackgroundImage")));
                //.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
                //.Image = ((System.Drawing.Bitmap)(resources.GetObject("Command1.Image")));

                switch (property.Name)
                {
                    case "BackgroundImage":
                        ResourceName = "$this.BackgroundImage";
                        break;

                    case "Icon":
                        ResourceName = "$this.Icon";
                        break;

                    case "Image":
                        ResourceName = name + ".Image";
                        break;
                }
                if (WriteImage(_sourceModule, ResourceName, property.Value, outPath))
                    switch (property.Name)
                    {
                        case "BackgroundImage":
                            result.Append(Indent6 + "this."
                              + property.Name + " = ((System.Drawing.Bitmap)(resources.GetObject("
                              + (Char)34 + "$this.BackgroundImage" + (Char)34 + ")));\r\n");
                            break;

                        case "Icon":
                            result.Append(Indent6 + "this."
                              + property.Name + " = ((System.Drawing.Icon)(resources.GetObject("
                              + (Char)34 + "$this.Icon" + (Char)34 + ")));\r\n");
                            break;

                        case "Image":
                            result.Append(Indent6 + "this." + name + "."
                              + property.Name + " = ((System.Drawing.Bitmap)(resources.GetObject("
                              + (Char)34 + name + ".Image" + (Char)34 + ")));\r\n");
                            break;
                    }
            }
            else
            {
                // unsupported property
                if (!property.Valid)
                    result.Append("//");
                if (type == "Form")
                    // form properties
                    result.Append(Indent6 + "this."
                      + property.Name + " = " + property.Value + ";\r\n");
                else
                    // control properties
                    result.Append(Indent6 + "this." + name + "."
                      + property.Name + " = " + property.Value + ";\r\n");
            }
        }
    }
}
