using System;
using System.IO;
using System.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;
using MinoriEditorShell.Services;
using MvvmCross.Commands;
using VisualBasicUpgradeAssistant.Core.Services;

namespace VisualBasicUpgradeAssistant.Core.ViewModels
{
    public class ProjectConverterViewModel : MesDocument
    {
        public String SourceFolder => Properties.Settings.Default.LastSource;
        public String DestinationFolder => Properties.Settings.Default.LastDest;

        public IMvxCommand SourceCommand => new MvxCommand(() => SetSourceProject());
        public IMvxCommand DestinationCommand => new MvxCommand(() => SetDestinationFolder());
        public IMvxCommand ConvertCommand => new MvxCommand(() => StartConversion());

        public ProjectConverterViewModel()
        {
            DisplayName = "Convert to CS Project";
        }

        // Init and Start are important parts of MvvmCross' CIRS ViewModel lifecycle
        // Learn how to use Init and Start at https://github.com/MvvmCross/MvvmCross/wiki/view-model-lifecycle
        public void Init()
        {
        }

        public override void Start()
        {
        }

        /// <summary>
        /// Setup for source folder
        /// </summary>
        private void SetSourceProject()
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.Filters.Add(new CommonFileDialogFilter("VB6 Project (*.vbp)", "*.vbp"));
            dialog.Filters.Add(new CommonFileDialogFilter("All files (*.*)", "*.*"));

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                String project = dialog.FileName;

                if (File.Exists(project))
                {
                    Properties.Settings.Default.LastSource = project;
                    Properties.Settings.Default.Save();

                    RaiseAllPropertiesChanged();
                }
            }
        }

        /// <summary>
        /// Setup for dest folder
        /// </summary>
        private void SetDestinationFolder()
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                String project = dialog.FileName;

                if (Directory.Exists(project))
                {
                    Properties.Settings.Default.LastDest = project;
                    Properties.Settings.Default.Save();

                    RaiseAllPropertiesChanged();
                }
            }
        }

        private void StartConversion()
        {
            FileInfo source = new FileInfo(SourceFolder);
            DirectoryInfo destFolder = new DirectoryInfo(DestinationFolder);

            if (source.Exists && destFolder.Exists)
            {
                try
                {
                    ConversionService conversionService = new ConversionService();
                    conversionService.Convert(source, destFolder);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Conversion Failed with {ex.Message}", "Conversion Failed");
                }
            }
            else
            {
                MessageBox.Show("One of the files do not exist. Check them and try again", "Input errors");
            }
        }
    }
}
