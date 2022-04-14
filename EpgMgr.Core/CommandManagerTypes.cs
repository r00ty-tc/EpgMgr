using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using EpgMgr.Plugins;

namespace EpgMgr
{
    public delegate string CommandMethodHandler(Core core, ref FolderEntry context, string command, string[] args);

    public delegate void ValueSetterGetter(FolderEntry context, string valueName, ValueType type, ref dynamic? value);

    public class CommandReference
    {
        public string CommandString { get; set; }
        public int? RequiredArgs { get; set; }
        public bool IsGlobal { get; set; }
        public FolderEntry? Context { get; set; }
        public CommandMethodHandler Method { get; set; }
        public Plugin? Plugin { get; set; }
        public string? UsageText { get; set; }

        public CommandReference(string commandString, CommandMethodHandler method, string? usageText = null,
            Plugin? plugin = null, FolderEntry? context = null, int? requiredArgs = null)
        {
            CommandString = commandString;
            IsGlobal = context == null;
            Method = method;
            RequiredArgs = requiredArgs;
            Context = context;
            UsageText = usageText;
            Plugin = plugin;
        }
    }

    public class FolderEntry
    {
        public string FolderName { get; set; }
        public string FolderPath { get; set; }
        public FolderEntry? ParentFolder;
        public List<FolderEntry> ChildFolders { get; set; }
        public List<FolderValue> ChildValues { get; set; }

        public FolderEntry(string folderName, FolderEntry? parentFolder)
        {
            FolderName = folderName;
            ChildFolders = new List<FolderEntry>();
            ChildValues = new List<FolderValue>();
            ParentFolder = parentFolder;
            FolderPath = getFullPath();
        }

        public FolderEntry AddChildFolder(string folderName)
        {
            var folder = new FolderEntry(folderName, this);
            ChildFolders.Add(folder);
            return folder;
        }

        public FolderValue AddChildValue(string valueId, ValueSetterGetter setget, ValueType type)
        {
            var value = new FolderValue(this, valueId, setget, type);
            ChildValues.Add(value);
            return value;
        }

        public void AddChildValue(FolderValue value) => ChildValues.Add(value);

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

        public FolderEntry? FindEntryByPath(string path)
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

    public class FolderValue
    {
        public FolderEntry FolderRef { get; set; }
        public string ValueId { get; set; }

        public ValueType Type { get; set; }
        public ValueSetterGetter SetGet { get; set; }

        public dynamic? Value
        {
            get
            {
                dynamic? result = null;
                SetGet(FolderRef, ValueId, Type, ref result);
                return result;
            }
            set => SetGet(FolderRef, ValueId, Type, ref value);
        }

        public FolderValue(FolderEntry context, string valueId, ValueSetterGetter setget, ValueType type)
        {
            FolderRef = context;
            ValueId = valueId;
            Type = type;
            SetGet = setget;
        }

        public static FolderValue GetNewValue(FolderEntry context, string valueId, ValueSetterGetter setget, ValueType type)
        {
            var result = new FolderValue(context, valueId, setget, type);
            context.AddChildValue(result);
            return result;
        }
    }

    public class ConsoleControl
    {
        private const string EscapePattern = "❛❜";
        private const string SetFGColourString = "SFG";
        private const string SetBGColourString = "SBG";

        public static string SetFG(ConsoleColor colour) => EscapePattern + SetFGColourString + ((int)colour).ToString() + EscapePattern;
        public static string SetBG(ConsoleColor colour) => EscapePattern + SetBGColourString + ((int)colour).ToString() + EscapePattern;

        public static void Write(string text)
        {
            var initialFGColour = Console.ForegroundColor;
            var initialBGColour = Console.BackgroundColor;
            var splits = text.Split(EscapePattern).ToList();
            while (splits.Count > 0)
            {
                var printText = splits.FirstOrDefault();
                if (text != null)
                {
                    Console.Write(printText);
                    splits.RemoveAt(0);
                }

                var commandText = splits.FirstOrDefault();
                if (commandText == null)
                    continue;
                splits.RemoveAt(0);
                if (commandText.StartsWith(SetFGColourString))
                {
                    if (!int.TryParse(commandText.Replace(SetFGColourString, ""), out var value)) continue;
                    var colour = (ConsoleColor)value;
                    Console.ForegroundColor = colour;
                }
                else if (commandText.StartsWith(SetBGColourString))
                {
                    if (!int.TryParse(commandText.Replace(SetBGColourString, ""), out var value)) continue;
                    var colour = (ConsoleColor)value;
                    Console.BackgroundColor = colour;
                }
            }

            Console.ForegroundColor = initialFGColour;
            Console.BackgroundColor = initialBGColour;
        }

        public static void WriteLine(string text) => Write(text + Environment.NewLine);
    }
}
