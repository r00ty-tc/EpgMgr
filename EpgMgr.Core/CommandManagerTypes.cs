using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EpgMgr.Plugins;

namespace EpgMgr
{
    public delegate string CommandMethodHandler(Core core, ref FolderEntry context, string command, string[] args);

    public class CommandReference
    {
        public string CommandString { get; set; }
        public int? RequiredArgs { get; set; }
        public bool IsGlobal { get; set; }
        public FolderEntry? Context { get; set; }
        public CommandMethodHandler Method { get; set; }
        public Plugin? Plugin { get; set; }

        public CommandReference(string commandString, CommandMethodHandler method, Plugin? plugin = null,
            FolderEntry? context = null, int? requiredArgs = null)
        {
            CommandString = commandString;
            IsGlobal = context == null;
            Method = method;
            RequiredArgs = requiredArgs;
            Context = context;
        }
    }

    public class FolderEntry
    {
        public string FolderName { get; set; }
        public string FolderPath { get; set; }
        public FolderEntry? ParentFolder;
        public List<FolderEntry> ChildFolders { get; set; }

        public FolderEntry(string folderName, FolderEntry? parentFolder)
        {
            FolderName = folderName;
            ChildFolders = new List<FolderEntry>();
            ParentFolder = parentFolder;
            FolderPath = getFullPath();
        }

        public FolderEntry AddChild(string folderName)
        {
            var folder = new FolderEntry(folderName, this);
            ChildFolders.Add(folder);
            return folder;
        }

        private string getFullPath()
        {
            var currentPath = this;
            var tempPath = FolderName;
            while (currentPath.ParentFolder != null)
            {
                currentPath = currentPath.ParentFolder;
                tempPath = $"{currentPath.FolderName}/{tempPath}";
            }

            return tempPath.Replace("//", "/");
        }

        public FolderEntry FindEntryByPath(string path)
        {
            if (FolderPath.Equals(path, StringComparison.CurrentCultureIgnoreCase))
                return this;

            foreach (var child in ChildFolders)
            {
                if (child.ChildFolders.Any())
                    child.FindEntryByPath(path);
            }

            return null;
        }
    }
}
