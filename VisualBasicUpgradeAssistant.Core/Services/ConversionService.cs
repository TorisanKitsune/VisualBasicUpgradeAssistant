using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using VisualBasicUpgradeAssistant.Core.Extensions;
using VisualBasicUpgradeAssistant.Core.Model;

namespace VisualBasicUpgradeAssistant.Core.Services
{
    internal class ConversionService
    {
        public void Convert(FileInfo source, DirectoryInfo dest)
        {
            String basename = Path.GetFileNameWithoutExtension(source.Name);

            CreateProject(dest, basename);
            ConvertFiles(source, dest, basename);
            // fill the files.
        }

        private void ConvertFiles(FileInfo project, DirectoryInfo dest, String basename)
        {
            DirectoryInfo sourceDir = project.Directory;

            DirectoryInfo winformsPath = dest.PathCombineDirectory(basename, $"{basename}.Desktop", "UserInterface");
            DirectoryInfo modulesPath = dest.PathCombineDirectory(basename, $"{basename}.Core", "Helpers");
            DirectoryInfo classPath = dest.PathCombineDirectory(basename, $"{basename}.Core", "Services");

            winformsPath.Create();
            modulesPath.Create();
            classPath.Create();

            ConvertCode convertCode = new ConvertCode();
            using (TextReader reader = project.OpenText())
            {
                while (reader.Peek() > 0)
                {
                    String line = reader.ReadLine();
                    if (String.IsNullOrEmpty(line))
                        return;

                    String[] kvm = line.Split('=');
                    switch (kvm.FirstOrDefault())
                    {
                        case "Class": // Last part Only
                            String classFile = kvm.Last().Split(';').Last().Trim();
                            FileInfo classFilePath = sourceDir.PathCombineFile(classFile);
                            Debug.WriteLine($"class:{classFile}");
                            convertCode.ParseFile(classFilePath, dest, classPath);
                            break;

                        case "Module": // Last part only
                            String moduleFile = kvm.Last().Split(';').Last().Trim();
                            FileInfo moduleFilePath = sourceDir.PathCombineFile(moduleFile);
                            Debug.WriteLine($"module:{moduleFile}");
                            convertCode.ParseFile(moduleFilePath, dest, modulesPath);
                            break;

                        case "Form":
                            // Form files go to desktop
                            String formFile = kvm[1];
                            FileInfo formFilePath = sourceDir.PathCombineFile(formFile);
                            Debug.WriteLine($"Form:{formFile}");
                            convertCode.ParseFile(formFilePath, dest, winformsPath);
                            break;
                    }
                }
            }
        }

        private static void CreateProject(DirectoryInfo dest, String basename)
        {
            // Setup output Dir
            DirectoryInfo outputDir = new DirectoryInfo(Path.Combine(dest.FullName, basename));
            if (outputDir.Exists)
                outputDir.Delete(true);

            // setup for initial project
            ProcessStartInfo startinfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = dest.FullName,
                FileName = "dotnet.exe",
                Arguments = $"new sln -o {basename}",
            };

            // setup base project
            Process.Start(startinfo).WaitForExit();

            // Add library
            startinfo.WorkingDirectory = outputDir.FullName;
            startinfo.Arguments = $"new classlib --langVersion 8.0 -o {basename}.Core";
            Process.Start(startinfo).WaitForExit();

            // Add Test
            startinfo.Arguments = $"new mstest -o {basename}.CoreTests";
            Process.Start(startinfo).WaitForExit();

            // Add Test
            startinfo.Arguments = $"add {basename}.CoreTests/{basename}.CoreTests.csproj reference {basename}.Core/{basename}.Core.csproj";
            Process.Start(startinfo).WaitForExit();

            // Add Winforms
            startinfo.Arguments = $"new winforms --langVersion 8.0 -o {basename}.Desktop";
            Process.Start(startinfo).WaitForExit();

            // Add Test
            startinfo.Arguments = $"add {basename}.Desktop/{basename}.Desktop.csproj reference {basename}.Core/{basename}.Core.csproj";
            Process.Start(startinfo).WaitForExit();

            // Combine into sln
            startinfo.Arguments = $"sln {basename}.sln add {basename}.Core {basename}.CoreTests {basename}.Desktop";
            Process.Start(startinfo).WaitForExit();
        }
    }
}
