using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace VisualBasicUpgradeAssistant.Core.Extensions
{
    public static class DirectoryInfoExtensions
    {
        public static void Clear(this DirectoryInfo obj)
        {
            FileInfo[] files = obj.GetFiles();
            for (Int32 i = 0; i < files.Length; i++)
            {
                files[i].Delete();
            }
            DirectoryInfo[] directories = obj.GetDirectories();
            for (Int32 i = 0; i < directories.Length; i++)
            {
                directories[i].Delete(recursive: true);
            }
        }

        public static void CopyTo(this DirectoryInfo obj, String destDirName)
        {
            obj.CopyTo(destDirName, "*.*", SearchOption.TopDirectoryOnly);
        }

        public static void CopyTo(this DirectoryInfo obj, String destDirName, String searchPattern)
        {
            obj.CopyTo(destDirName, searchPattern, SearchOption.TopDirectoryOnly);
        }

        public static void CopyTo(this DirectoryInfo obj, String destDirName, SearchOption searchOption)
        {
            obj.CopyTo(destDirName, "*.*", searchOption);
        }

        public static void CopyTo(this DirectoryInfo obj, String destDirName, String searchPattern, SearchOption searchOption)
        {
            FileInfo[] files = obj.GetFiles(searchPattern, searchOption);
            foreach (FileInfo fileInfo in files)
            {
                String text = destDirName + fileInfo.FullName.Substring(obj.FullName.Length);
                DirectoryInfo directory = new FileInfo(text).Directory;
                if (directory == null)
                {
                    throw new Exception("The directory cannot be null.");
                }
                if (!directory.Exists)
                {
                    directory.Create();
                }
                fileInfo.CopyTo(text);
            }
            DirectoryInfo[] directories = obj.GetDirectories(searchPattern, searchOption);
            foreach (DirectoryInfo directoryInfo in directories)
            {
                DirectoryInfo directoryInfo2 = new DirectoryInfo(destDirName + directoryInfo.FullName.Substring(obj.FullName.Length));
                if (!directoryInfo2.Exists)
                {
                    directoryInfo2.Create();
                }
            }
        }

        public static void CopyTo(this DirectoryInfo sourceDirectory, DirectoryInfo targetDirectory)
        {
            if (!targetDirectory.Exists)
            {
                targetDirectory.Create();
            }
            DirectoryInfo[] directories = sourceDirectory.GetDirectories();
            foreach (DirectoryInfo directoryInfo in directories)
            {
                directoryInfo.CopyTo(Path.Combine(targetDirectory.FullName, directoryInfo.Name));
            }
            FileInfo[] files = sourceDirectory.GetFiles();
            foreach (FileInfo fileInfo in files)
            {
                fileInfo.CopyTo(Path.Combine(targetDirectory.FullName, fileInfo.Name));
            }
        }

        public static DirectoryInfo CreateAllDirectories(this DirectoryInfo @this)
        {
            return Directory.CreateDirectory(@this.FullName);
        }

        public static void DeleteDirectoriesWhere(this DirectoryInfo obj, Func<DirectoryInfo, Boolean> predicate)
        {
            EnumerableExtensions.ForEach(obj.GetDirectories().Where(predicate), (Action<DirectoryInfo>)delegate (DirectoryInfo x)
            {
                x.Delete();
            });
        }

        public static void DeleteDirectoriesWhere(this DirectoryInfo obj, SearchOption searchOption, Func<DirectoryInfo, Boolean> predicate)
        {
            EnumerableExtensions.ForEach(obj.GetDirectories("*.*", searchOption).Where(predicate), (Action<DirectoryInfo>)delegate (DirectoryInfo x)
            {
                x.Delete();
            });
        }

        public static void DeleteFilesWhere(this DirectoryInfo obj, Func<FileInfo, Boolean> predicate)
        {
            foreach (FileInfo item in obj.GetFiles().Where(predicate))
            {
                item.Delete();
            }
        }

        public static void DeleteFilesWhere(this DirectoryInfo obj, SearchOption searchOption, Func<FileInfo, Boolean> predicate)
        {
            foreach (FileInfo item in obj.GetFiles("*.*", searchOption).Where(predicate))
            {
                item.Delete();
            }
        }

        public static void DeleteOlderThan(this DirectoryInfo obj, TimeSpan timeSpan)
        {
            DateTime minDate = DateTime.Now.Subtract(timeSpan);
            (from x in obj.GetFiles()
             where x.LastWriteTime < minDate
             select x).ToList().ForEach(delegate (FileInfo x)
             {
                 x.Delete();
             });
            (from x in obj.GetDirectories()
             where x.LastWriteTime < minDate
             select x).ToList().ForEach(delegate (DirectoryInfo x)
             {
                 x.Delete();
             });
        }

        public static void DeleteOlderThan(this DirectoryInfo obj, SearchOption searchOption, TimeSpan timeSpan)
        {
            DateTime minDate = DateTime.Now.Subtract(timeSpan);
            (from x in obj.GetFiles("*.*", searchOption)
             where x.LastWriteTime < minDate
             select x).ToList().ForEach(delegate (FileInfo x)
             {
                 x.Delete();
             });
            (from x in obj.GetDirectories("*.*", searchOption)
             where x.LastWriteTime < minDate
             select x).ToList().ForEach(delegate (DirectoryInfo x)
             {
                 x.Delete();
             });
        }

        public static DirectoryInfo EnsureDirectoryExists(this DirectoryInfo @this)
        {
            return Directory.CreateDirectory(@this.FullName);
        }

        public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo @this)
        {
            return EnumerableExtensions.Select(Directory.EnumerateDirectories(@this.FullName), (String x) => new DirectoryInfo(x));
        }

        public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo @this, String searchPattern)
        {
            return EnumerableExtensions.Select(Directory.EnumerateDirectories(@this.FullName, searchPattern), (String x) => new DirectoryInfo(x));
        }

        public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo @this, String searchPattern, SearchOption searchOption)
        {
            return EnumerableExtensions.Select(Directory.EnumerateDirectories(@this.FullName, searchPattern, searchOption), (String x) => new DirectoryInfo(x));
        }

        public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo @this, String[] searchPatterns)
        {
            return searchPatterns.SelectMany(@this.GetDirectories).Distinct();
        }

        public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo @this, String[] searchPatterns, SearchOption searchOption)
        {
            return searchPatterns.SelectMany((String x) => @this.GetDirectories(x, searchOption)).Distinct();
        }

        public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo @this)
        {
            return EnumerableExtensions.Select(Directory.EnumerateFiles(@this.FullName), (String x) => new FileInfo(x));
        }

        public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo @this, String searchPattern)
        {
            return EnumerableExtensions.Select(Directory.EnumerateFiles(@this.FullName, searchPattern), (String x) => new FileInfo(x));
        }

        public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo @this, String searchPattern, SearchOption searchOption)
        {
            return EnumerableExtensions.Select(Directory.EnumerateFiles(@this.FullName, searchPattern, searchOption), (String x) => new FileInfo(x));
        }

        public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo @this, String[] searchPatterns)
        {
            return searchPatterns.SelectMany(@this.GetFiles).Distinct();
        }

        public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo @this, String[] searchPatterns, SearchOption searchOption)
        {
            return searchPatterns.SelectMany((String x) => @this.GetFiles(x, searchOption)).Distinct();
        }

        public static IEnumerable<String> EnumerateFileSystemEntries(this DirectoryInfo @this)
        {
            return Directory.EnumerateFileSystemEntries(@this.FullName);
        }

        public static IEnumerable<String> EnumerateFileSystemEntries(this DirectoryInfo @this, String searchPattern)
        {
            return Directory.EnumerateFileSystemEntries(@this.FullName, searchPattern);
        }

        public static IEnumerable<String> EnumerateFileSystemEntries(this DirectoryInfo @this, String searchPattern, SearchOption searchOption)
        {
            return Directory.EnumerateFileSystemEntries(@this.FullName, searchPattern, searchOption);
        }

        public static IEnumerable<String> EnumerateFileSystemEntries(this DirectoryInfo @this, String[] searchPatterns)
        {
            return searchPatterns.SelectMany((String x) => Directory.EnumerateFileSystemEntries(@this.FullName, x)).Distinct();
        }

        public static IEnumerable<String> EnumerateFileSystemEntries(this DirectoryInfo @this, String[] searchPatterns, SearchOption searchOption)
        {
            return searchPatterns.SelectMany((String x) => Directory.EnumerateFileSystemEntries(@this.FullName, x, searchOption)).Distinct();
        }

        public static FileInfo FindFileRecursive(this DirectoryInfo directory, String pattern)
        {
            FileInfo[] files = directory.GetFiles(pattern);
            if (files.Length != 0)
            {
                return files[0];
            }
            DirectoryInfo[] directories = directory.GetDirectories();
            for (Int32 i = 0; i < directories.Length; i++)
            {
                FileInfo fileInfo = directories[i].FindFileRecursive(pattern);
                if (fileInfo != null)
                {
                    return fileInfo;
                }
            }
            return null;
        }

        public static FileInfo FindFileRecursive(this DirectoryInfo directory, Func<FileInfo, Boolean> predicate)
        {
            FileInfo[] files = directory.GetFiles();
            foreach (FileInfo fileInfo in files)
            {
                if (predicate(fileInfo))
                {
                    return fileInfo;
                }
            }
            DirectoryInfo[] directories = directory.GetDirectories();
            for (Int32 i = 0; i < directories.Length; i++)
            {
                FileInfo fileInfo2 = directories[i].FindFileRecursive(predicate);
                if (fileInfo2 != null)
                {
                    return fileInfo2;
                }
            }
            return null;
        }

        public static FileInfo[] FindFilesRecursive(this DirectoryInfo directory, String pattern)
        {
            List<FileInfo> list = new List<FileInfo>();
            FindFilesRecursive(directory, pattern, list);
            return list.ToArray();
        }

        private static void FindFilesRecursive(DirectoryInfo directory, String pattern, List<FileInfo> foundFiles)
        {
            foundFiles.AddRange(directory.GetFiles(pattern));
            EnumerableExtensions.ForEach((IEnumerable<DirectoryInfo>)directory.GetDirectories(), (Action<DirectoryInfo>)delegate (DirectoryInfo d)
            {
                FindFilesRecursive(d, pattern, foundFiles);
            });
        }

        public static FileInfo[] FindFilesRecursive(this DirectoryInfo directory, Func<FileInfo, Boolean> predicate)
        {
            List<FileInfo> list = new List<FileInfo>();
            FindFilesRecursive(directory, predicate, list);
            return list.ToArray();
        }

        private static void FindFilesRecursive(DirectoryInfo directory, Func<FileInfo, Boolean> predicate, List<FileInfo> foundFiles)
        {
            foundFiles.AddRange(directory.GetFiles().Where(predicate));
            EnumerableExtensions.ForEach((IEnumerable<DirectoryInfo>)directory.GetDirectories(), (Action<DirectoryInfo>)delegate (DirectoryInfo d)
            {
                FindFilesRecursive(d, predicate, foundFiles);
            });
        }

        public static DirectoryInfo[] GetDirectories(this DirectoryInfo @this, String[] searchPatterns)
        {
            return searchPatterns.SelectMany(@this.GetDirectories).Distinct().ToArray();
        }

        public static DirectoryInfo[] GetDirectories(this DirectoryInfo @this, String[] searchPatterns, SearchOption searchOption)
        {
            return searchPatterns.SelectMany((String x) => @this.GetDirectories(x, searchOption)).Distinct().ToArray();
        }

        public static DirectoryInfo[] GetDirectoriesWhere(this DirectoryInfo @this, Func<DirectoryInfo, Boolean> predicate)
        {
            return EnumerableExtensions.Select(Directory.EnumerateDirectories(@this.FullName), (String x) => new DirectoryInfo(x)).Where(predicate).ToArray();
        }

        public static DirectoryInfo[] GetDirectoriesWhere(this DirectoryInfo @this, String searchPattern, Func<DirectoryInfo, Boolean> predicate)
        {
            return EnumerableExtensions.Select(Directory.EnumerateDirectories(@this.FullName, searchPattern), (String x) => new DirectoryInfo(x)).Where(predicate).ToArray();
        }

        public static DirectoryInfo[] GetDirectoriesWhere(this DirectoryInfo @this, String searchPattern, SearchOption searchOption, Func<DirectoryInfo, Boolean> predicate)
        {
            return EnumerableExtensions.Select(Directory.EnumerateDirectories(@this.FullName, searchPattern, searchOption), (String x) => new DirectoryInfo(x)).Where(predicate).ToArray();
        }

        public static DirectoryInfo[] GetDirectoriesWhere(this DirectoryInfo @this, String[] searchPatterns, Func<DirectoryInfo, Boolean> predicate)
        {
            return searchPatterns.SelectMany(@this.GetDirectories).Distinct().Where(predicate)
                .ToArray();
        }

        public static DirectoryInfo[] GetDirectoriesWhere(this DirectoryInfo @this, String[] searchPatterns, SearchOption searchOption, Func<DirectoryInfo, Boolean> predicate)
        {
            return searchPatterns.SelectMany((String x) => @this.GetDirectories(x, searchOption)).Distinct().Where(predicate)
                .ToArray();
        }

        public static FileInfo[] GetFiles(this DirectoryInfo @this, String[] searchPatterns)
        {
            return searchPatterns.SelectMany(@this.GetFiles).Distinct().ToArray();
        }

        public static FileInfo[] GetFiles(this DirectoryInfo @this, String[] searchPatterns, SearchOption searchOption)
        {
            return searchPatterns.SelectMany((String x) => @this.GetFiles(x, searchOption)).Distinct().ToArray();
        }

        public static FileInfo[] GetFilesWhere(this DirectoryInfo @this, Func<FileInfo, Boolean> predicate)
        {
            return EnumerableExtensions.Select(Directory.EnumerateFiles(@this.FullName), (String x) => new FileInfo(x)).Where(predicate).ToArray();
        }

        public static FileInfo[] GetFilesWhere(this DirectoryInfo @this, String searchPattern, Func<FileInfo, Boolean> predicate)
        {
            return EnumerableExtensions.Select(Directory.EnumerateFiles(@this.FullName, searchPattern), (String x) => new FileInfo(x)).Where(predicate).ToArray();
        }

        public static FileInfo[] GetFilesWhere(this DirectoryInfo @this, String searchPattern, SearchOption searchOption, Func<FileInfo, Boolean> predicate)
        {
            return EnumerableExtensions.Select(Directory.EnumerateFiles(@this.FullName, searchPattern, searchOption), (String x) => new FileInfo(x)).Where(predicate).ToArray();
        }

        public static FileInfo[] GetFilesWhere(this DirectoryInfo @this, String[] searchPatterns, Func<FileInfo, Boolean> predicate)
        {
            return searchPatterns.SelectMany(@this.GetFiles).Distinct().Where(predicate)
                .ToArray();
        }

        public static FileInfo[] GetFilesWhere(this DirectoryInfo @this, String[] searchPatterns, SearchOption searchOption, Func<FileInfo, Boolean> predicate)
        {
            return searchPatterns.SelectMany((String x) => @this.GetFiles(x, searchOption)).Distinct().Where(predicate)
                .ToArray();
        }

        public static String[] GetFileSystemEntries(this DirectoryInfo @this)
        {
            return Directory.EnumerateFileSystemEntries(@this.FullName).ToArray();
        }

        public static String[] GetFileSystemEntries(this DirectoryInfo @this, String searchPattern)
        {
            return Directory.EnumerateFileSystemEntries(@this.FullName, searchPattern).ToArray();
        }

        public static String[] GetFileSystemEntries(this DirectoryInfo @this, String searchPattern, SearchOption searchOption)
        {
            return Directory.EnumerateFileSystemEntries(@this.FullName, searchPattern, searchOption).ToArray();
        }

        public static String[] GetFileSystemEntries(this DirectoryInfo @this, String[] searchPatterns)
        {
            return searchPatterns.SelectMany((String x) => Directory.EnumerateFileSystemEntries(@this.FullName, x)).Distinct().ToArray();
        }

        public static String[] GetFileSystemEntries(this DirectoryInfo @this, String[] searchPatterns, SearchOption searchOption)
        {
            return searchPatterns.SelectMany((String x) => Directory.EnumerateFileSystemEntries(@this.FullName, x, searchOption)).Distinct().ToArray();
        }

        public static String[] GetFileSystemEntriesWhere(this DirectoryInfo @this, Func<String, Boolean> predicate)
        {
            return Directory.EnumerateFileSystemEntries(@this.FullName).Where(predicate).ToArray();
        }

        public static String[] GetFileSystemEntriesWhere(this DirectoryInfo @this, String searchPattern, Func<String, Boolean> predicate)
        {
            return Directory.EnumerateFileSystemEntries(@this.FullName, searchPattern).Where(predicate).ToArray();
        }

        public static String[] GetFileSystemEntriesWhere(this DirectoryInfo @this, String searchPattern, SearchOption searchOption, Func<String, Boolean> predicate)
        {
            return Directory.EnumerateFileSystemEntries(@this.FullName, searchPattern, searchOption).Where(predicate).ToArray();
        }

        public static String[] GetFileSystemEntriesWhere(this DirectoryInfo @this, String[] searchPatterns, Func<String, Boolean> predicate)
        {
            return searchPatterns.SelectMany((String x) => Directory.EnumerateFileSystemEntries(@this.FullName, x)).Distinct().Where(predicate)
                .ToArray();
        }

        public static String[] GetFileSystemEntriesWhere(this DirectoryInfo @this, String[] searchPatterns, SearchOption searchOption, Func<String, Boolean> predicate)
        {
            return searchPatterns.SelectMany((String x) => Directory.EnumerateFileSystemEntries(@this.FullName, x, searchOption)).Distinct().Where(predicate)
                .ToArray();
        }

        public static Int64 GetSize(this DirectoryInfo @this)
        {
            return @this.GetFiles("*.*", SearchOption.AllDirectories).Sum((FileInfo x) => x.Length);
        }

        public static String PathCombine(this DirectoryInfo @this, params String[] paths)
        {
            List<String> list = paths.ToList();
            list.Insert(0, @this.FullName);
            return Path.Combine(list.ToArray());
        }

        public static DirectoryInfo PathCombineDirectory(this DirectoryInfo @this, params String[] paths)
        {
            List<String> list = paths.ToList();
            list.Insert(0, @this.FullName);
            return new DirectoryInfo(Path.Combine(list.ToArray()));
        }

        public static FileInfo PathCombineFile(this DirectoryInfo info, params String[] paths)
        {
            List<String> list = paths.ToList();
            list.Insert(0, info.FullName);
            return new FileInfo(Path.Combine(list.ToArray()));
        }
    }
}
