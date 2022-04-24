using EpgMgr.Plugins;

namespace EpgMgr
{
    /// <summary>
    /// Delegate for command handler methods.
    /// </summary>
    public delegate string CommandMethodHandler(Core core, ref FolderEntry context, string command, string[] args);

    /// <summary>
    /// Delegate for a method that will handle setting/getting values (for configuration). If value is null it will get a value, if not it will set the value
    /// </summary>
    public delegate void ValueSetterGetter(FolderEntry context, string valueName, ValueType type, ref dynamic? value);

    /// <summary>
    /// Structure class for a command reference entry. Contains information about the command, context and callback delagate.
    /// </summary>
    public class CommandReference
    {
        /// <summary>
        /// Command String (the command entered in trhe console to trigger this logic)
        /// </summary>
        public string CommandString { get; set; }
        /// <summary>
        /// Number of required arguments, if null any arguments are allowed
        /// </summary>
        public int? RequiredArgs { get; set; }
        /// <summary>
        /// True if command is global, otherwise false if command only valid in specific folder
        /// </summary>
        public bool IsGlobal { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public FolderEntry? Context { get; set; }
        /// <summary>
        /// If specified the command will only trigger logic if run from this context folder.
        /// </summary>
        public CommandMethodHandler Method { get; set; }
        /// <summary>
        /// Reference to plugin object if command created by plugin
        /// </summary>
        public Plugin? Plugin { get; set; }
        /// <summary>
        /// Text that will describe to the user how to use the command. Returned by "help command"
        /// </summary>
        public string? UsageText { get; set; }

        /// <summary>
        /// Create a new Command Reference object. If context is null, it will be global, if not it will be local to that folder.
        /// </summary>
        /// <param name="commandString"></param>
        /// <param name="method"></param>
        /// <param name="usageText"></param>
        /// <param name="plugin"></param>
        /// <param name="context"></param>
        /// <param name="requiredArgs"></param>
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

    /// <summary>
    /// 
    /// </summary>
    public class FolderEntry
    {
        /// <summary>
        /// Folder Name
        /// </summary>
        public string FolderName { get; set; }
        /// <summary>
        /// Folder Path
        /// </summary>
        public string FolderPath { get; set; }
        /// <summary>
        /// Parent folder context entyr
        /// </summary>
        public FolderEntry? ParentFolder;
        /// <summary>
        /// List of child folders from this folder's context
        /// </summary>
        public List<FolderEntry> ChildFolders { get; set; }
        /// <summary>
        /// List of child values from this folder's context
        /// </summary>
        public List<FolderValue> ChildValues { get; set; }

        /// <summary>
        /// Create new instance of a folder entry
        /// </summary>
        /// <param name="folderName"></param>
        /// <param name="parentFolder"></param>
        public FolderEntry(string folderName, FolderEntry? parentFolder)
        {
            FolderName = folderName;
            ChildFolders = new List<FolderEntry>();
            ChildValues = new List<FolderValue>();
            ParentFolder = parentFolder;
            FolderPath = getFullPath();
        }

        /// <summary>
        /// Add a child folder to the current object's folder and return a reference to the new folder
        /// </summary>
        /// <param name="folderName"></param>
        /// <returns></returns>
        public FolderEntry AddChildFolder(string folderName)
        {
            var folder = new FolderEntry(folderName, this);
            ChildFolders.Add(folder);
            return folder;
        }

        /// <summary>
        /// Add a value to the current folder object and return a reference to the value.
        /// </summary>
        /// <param name="valueId"></param>
        /// <param name="setget"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public FolderValue AddChildValue(string valueId, ValueSetterGetter setget, ValueType type)
        {
            var value = new FolderValue(this, valueId, setget, type);
            ChildValues.Add(value);
            return value;
        }

        /// <summary>
        /// Add an already created value to the current folder object
        /// </summary>
        /// <param name="value"></param>
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

        /// <summary>
        /// Try to find a path relative to the current path. If found, return a reference to the folder entry for that path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
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

    /// <summary>
    /// Folder value: Stores information about a value. Folder reference, Id, Type and a method that will be called to get/set a value
    /// </summary>
    public class FolderValue
    {
        /// <summary>
        /// Reference to the folder this value belongs to.
        /// </summary>
        public FolderEntry FolderRef { get; set; }
        /// <summary>
        /// ID for the value. This will be listed as the variable name in the console
        /// </summary>
        public string ValueId { get; set; }

        /// <summary>
        /// Data type for the value. Used to decide now to handle the value and is used in serialization
        /// </summary>
        public ValueType Type { get; set; }
        /// <summary>
        /// Method called to set or get the value.
        /// </summary>
        public ValueSetterGetter SetGet { get; set; }

        /// <summary>
        /// The actual value. Allows direct setting/getting via calls to the set/get method
        /// </summary>
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

        /// <summary>
        /// Create new instance of a folder value. setget should refer to a method that will be used to set or get the value
        /// </summary>
        /// <param name="context"></param>
        /// <param name="valueId"></param>
        /// <param name="setget"></param>
        /// <param name="type"></param>
        public FolderValue(FolderEntry context, string valueId, ValueSetterGetter setget, ValueType type)
        {
            FolderRef = context;
            ValueId = valueId;
            Type = type;
            SetGet = setget;
        }

        /// <summary>
        /// Static method to create a new folder value with the given parameters and add it to the folder entry passed in. Returns a reference to the value object
        /// </summary>
        /// <param name="context"></param>
        /// <param name="valueId"></param>
        /// <param name="setget"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static FolderValue GetNewValue(FolderEntry context, string valueId, ValueSetterGetter setget, ValueType type)
        {
            var result = new FolderValue(context, valueId, setget, type);
            context.AddChildValue(result);
            return result;
        }
    }

    /// <summary>
    /// ConsoleControl: A series of static methods used to access the console but allow setting colours dynamically.
    /// </summary>
    public class ConsoleControl
    {
        private const string EscapePattern = "❛❜";
        private const string SetFGColourString = "SFG";
        private const string SetBGColourString = "SBG";

        /// <summary>
        /// Return control characters used to set the console forground colour to the specified colour.
        /// </summary>
        /// <param name="colour"></param>
        /// <returns></returns>
        public static string SetFG(ConsoleColor colour) => EscapePattern + SetFGColourString + ((int)colour).ToString() + EscapePattern;
        /// <summary>
        /// Return control characters used to set the console background colour to the specified colour.
        /// </summary>
        /// <param name="colour"></param>
        /// <returns></returns>
        public static string SetBG(ConsoleColor colour) => EscapePattern + SetBGColourString + ((int)colour).ToString() + EscapePattern;
        /// <summary>
        /// A shortcut to return the control characters to set the text to the error colour (red)
        /// </summary>
        public static string ErrorColour => SetFG(ConsoleColor.Red);

        /// <summary>
        /// Wrapper around Console.Write that handles the colour control characters. Will return to the original colour on exit
        /// </summary>
        /// <param name="text"></param>
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

        /// <summary>
        /// Wrapper around ConsoleControl.Write to also add a NewLine at the end (to function like Console.WriteLine)
        /// </summary>
        /// <param name="text"></param>
        public static void WriteLine(string text) => Write(text + Environment.NewLine);
    }
}
