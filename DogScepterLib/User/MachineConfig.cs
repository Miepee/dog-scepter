﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DogScepterLib.User
{
    // Configuration for a specific machine (i.e., references to files)
    public class MachineConfig
    {
        public const int MaxRecentProjects = 8;

        public Dictionary<string, ProjectConfig> Projects { get; set; } = new Dictionary<string, ProjectConfig>();
        public List<string> RecentProjects { get; set; } = new List<string>(MaxRecentProjects);

        public readonly static JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public static void Save(MachineConfig config)
        {
            Storage.Config.WriteAllBytes("config.json", JsonSerializer.SerializeToUtf8Bytes(config, JsonOptions));
        }

        public static MachineConfig Load()
        {
            byte[] bytes = Storage.Config.ReadAllBytes("config.json");
            if (bytes == null)
                return new MachineConfig();
            try
            {
                return JsonSerializer.Deserialize<MachineConfig>(bytes, JsonOptions);
            }
            catch
            {
                return new MachineConfig();
            }
        }

        public void AddNewProject(string projectFile, ProjectConfig config)
        {
            Projects[projectFile] = config;

            // Update recent projects list
            int ind = RecentProjects.IndexOf(projectFile);
            if (ind != -1)
                RecentProjects.RemoveAt(ind);
            else if (RecentProjects.Count == MaxRecentProjects)
                RecentProjects.RemoveAt(MaxRecentProjects - 1);
            RecentProjects.Insert(0, projectFile);
        }

        public void Clear()
        {
            Projects.Clear();
            RecentProjects.Clear();
        }
    }
}
