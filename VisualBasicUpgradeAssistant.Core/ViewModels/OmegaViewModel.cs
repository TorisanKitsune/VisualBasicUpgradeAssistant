using System;
using System.IO;
using System.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;
using MinoriEditorShell.Services;
using MvvmCross;
using MvvmCross.Commands;
using VisualBasicUpgradeAssistant.Core.Model;

namespace VisualBasicUpgradeAssistant.Core.ViewModels
{
    public class OmegaViewModel : MesDocument
    {
        private String? _vb6Text;
        private String? _fileName;
        private String? _outPath;
        private String? _csharpText;

        public String? CSharpText
        {
            get => _csharpText;
            private set => SetProperty(ref _csharpText, value);
        }

        public String? OutPath
        {
            get => _outPath;
            set => SetProperty(ref _outPath, value?.Trim());
        }

        public String? VB6Text
        {
            get => _vb6Text;
            private set => SetProperty(ref _vb6Text, value);
        }

        public IMvxCommand ConvertCommand => new MvxCommand(() =>
        {
            if (!String.IsNullOrWhiteSpace(_fileName))
            {
                // parse file
                ConvertCode ConvertObject = new ConvertCode();
                if (String.IsNullOrWhiteSpace(OutPath))
                {
                    MessageBox.Show("Fill out path !", "", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (!Directory.Exists(OutPath))
                {
                    MessageBox.Show("Out path not exists !", "", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (OutPath.Substring(OutPath.Length - 1, 1) != @"\")
                {
                    OutPath += @"\";
                }

                throw new NotImplementedException();
                //ConvertObject.ParseFile(new FileInfo(_fileName), new DirectoryInfo(OutPath));

                // show result
                //CSharpText = ConvertObject.OutSourceCode;
            }
        });

        public IMvxCommand LoadCommand => new MvxCommand(() =>
        {
            //String filter = "VB6 form (*.frm)|*.frm|VB6 module (*.bas)|*.bas|VB6 class (*.cls)|*.cls|All files (*.*)|*.*";

            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.Filters.Add(new CommonFileDialogFilter("VB6 form (*.frm)", "*.frm"));
            dialog.Filters.Add(new CommonFileDialogFilter("VB6 module (*.bas)", "*.bas"));
            dialog.Filters.Add(new CommonFileDialogFilter("VB6 class (*.cls)", "*.cls"));
            dialog.Filters.Add(new CommonFileDialogFilter("All files (*.*)", "*.*"));

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                _fileName = dialog.FileName;

                if (_fileName != null)
                {
                    // show content of file
                    StreamReader Reader = File.OpenText(_fileName);
                    VB6Text = Reader.ReadToEnd();
                    Reader.Close();
                }
            }
        });

        public IMvxCommand SelectOutPathCommand => new MvxCommand(() =>
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
            };

            CommonFileDialogResult result = dialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                OutPath = dialog.FileName;
            }
        });

        public IMvxCommand ExitCommand => new MvxCommand(() =>
        {
            // Don't write out to program directory
            //Program.Config.WriteString(Program.CONFIG_SETTING, Program.CONFIG_OUT_PATH, txtOutPath.Text);
            Mvx.IoCProvider.Resolve<IMesDocumentManager>().Documents.Remove(this);
        });

        //    private string FileSave()
        //    {
        //      string sFilter = "C# Files (*.cs)|*.cs" ;
        //      string sResult = null;
        //
        //      SaveFileDialog oDialog = new SaveFileDialog();
        //      oDialog.Filter = sFilter;
        //      if(oDialog.ShowDialog() != DialogResult.Cancel)
        //      {
        //        sResult = oDialog.FileName;
        //      }
        //      return sResult;
        //    }

        public OmegaViewModel()
        {
            DisplayName = "Omega Interface";
            OutPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        // Init and Start are important parts of MvvmCross' CIRS ViewModel lifecycle
        // Learn how to use Init and Start at https://github.com/MvvmCross/MvvmCross/wiki/view-model-lifecycle
        public void Init()
        {
        }

        public override void Start()
        {
        }
    }
}
