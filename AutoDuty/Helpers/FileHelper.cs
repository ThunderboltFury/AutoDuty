﻿using ECommons.DalamudServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using static AutoDuty.AutoDuty;

namespace AutoDuty.Helpers
{
    internal static class FileHelper
    {
        internal static Dictionary<uint, bool> PathFileExists = [];
        internal static readonly FileSystemWatcher FileSystemWatcher = new(Plugin.PathsDirectory.FullName)

        {
            NotifyFilter = NotifyFilters.Attributes
                                 | NotifyFilters.CreationTime
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Security
                                 | NotifyFilters.Size,

            Filter = "*.json",
            IncludeSubdirectories = false
        };

        public static byte[] CalculateMD5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    return md5.ComputeHash(stream);
                }
            }
        }

        internal static void OnStart()
        {
            //Move all the paths to the Paths folder on first install or update
            if (Plugin.AssemblyDirectoryInfo == null)
                return;
            try
            {
                int i = 0;
                var files = Plugin.AssemblyDirectoryInfo.EnumerateFiles("*.json", SearchOption.TopDirectoryOnly)
                .Where(s => s.Name.StartsWith('('));
                foreach (var file in files)
                {
                    if (!File.Exists($"{Plugin.PathsDirectory.FullName}/{file.Name}") || !BitConverter.ToString(CalculateMD5(file.FullName)).Replace("-", "").Equals(BitConverter.ToString(CalculateMD5($"{Plugin.PathsDirectory.FullName}/{file.Name}")).Replace("-", ""), StringComparison.InvariantCultureIgnoreCase))
                    {
                        file.MoveTo($"{Plugin.PathsDirectory.FullName}/{file.Name}", true);
                        Svc.Log.Info($"Moved: {file.Name}");
                        i++;
                    }
                }
                Svc.Log.Info($"Moved: {i} Paths to the Paths Folder: {Plugin.PathsDirectory.FullName}");
            }
            catch (Exception ex)
            {
                Svc.Log.Error($"Error copying paths from {Plugin.AssemblyDirectoryInfo.FullName} to {Plugin.PathsDirectory.FullName}\n{ex}");
            }
        }

        internal static void Init()
        {
            FileSystemWatcher.Changed += OnChanged;
            FileSystemWatcher.Created += OnCreated;
            FileSystemWatcher.Deleted += OnDeleted;
            FileSystemWatcher.Renamed += OnRenamed;
            FileSystemWatcher.EnableRaisingEvents = true;

            Update();
        }

        private static void Update() 
        {
            PathFileExists = [];
            foreach (var t in ContentHelper.DictionaryContent)
            {
                PathFileExists.TryAdd(t.Value.TerritoryType, File.Exists($"{Plugin.PathsDirectory.FullName}/({t.Value.TerritoryType}) {t.Value.Name}.json"));
            }
        }

        private static void OnChanged(object sender, FileSystemEventArgs e) => Update();

        private static void OnCreated(object sender, FileSystemEventArgs e) => Update();

        private static void OnDeleted(object sender, FileSystemEventArgs e) => Update();

        private static void OnRenamed(object sender, RenamedEventArgs e) => Update();
    }
}
