// See https://aka.ms/new-console-template for more information

using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using EpgMgr;
using Console = System.Console;

#if SIGNED
[DllImport("mscoree.dll", CharSet = CharSet.Unicode)]
static extern bool StrongNameSignatureVerificationEx(string wszFilePath, bool fForceVerification, ref bool pfWasVerified);
var publicKey =
    @"ACQAAASAAACUAAAABgIAAAAkAABSU0ExAAQAAAEAAQCBwN/koxj5XuQ/5ht39fl7o51gd32MBvB4i5oC409tCewSageo2alerW+9RrKVIv9hi/k5bBcfwRCA3R4hXDHQJz2Ixzhzr6lG8w9PE3GseA6RyJzDd6gQcrmWAaAEptuJwB2iLdL49eaD1x3xBr7Q+23zlnVsc7/lHLCdZ1HB2g==";

// Validate components
bool wasChecked = false;
var consoleSigned = StrongNameSignatureVerificationEx(@"E:\Development\VS_Projects\EpgMgr.Console\EpgMgr.Console\bin\Debug\net6.0\Console.dll", true, ref wasChecked);
if (!wasChecked)
    throw new Exception("Failed to validate Console.exe component");
var coreSigned = StrongNameSignatureVerificationEx("Core.Dll", true, ref wasChecked);
if (!wasChecked)
    throw new Exception("Failed to validate Core.dll component");

var consoleDllInfo = new FileInfo("Console.DLL");
var consoleAssembly = Assembly.LoadFile(consoleDllInfo.FullName);

var consoleKey = System.Convert.ToBase64String(consoleAssembly.GetName().GetPublicKey());
if (!consoleSigned || !consoleKey.Equals(publicKey))
    throw new Exception("Failed to validate Console.exe component");

var coreDllInfo = new FileInfo("Core.DLL");
var coreAssembly = Assembly.LoadFile(coreDllInfo.FullName);

var coreKey = System.Convert.ToBase64String(coreAssembly.GetName().GetPublicKey());
if (!coreSigned || !coreKey.Equals(publicKey))
    throw new Exception("Failed to validate Console.exe component");
#endif

var lastStatus = string.Empty;
var progressMode = false;
var core = new Core(UpdateFeedback);

Console.OutputEncoding = Encoding.UTF8;
var context = core.CommandMgr.RootFolder;
ConsoleControl.WriteLine($"EpgMgr Console {Assembly.GetExecutingAssembly().GetName().Version}");
var useConsole = false;
if (args.Length != 0)
{
    useConsole = ProcessArgs(args.ToList());
}

if (useConsole)
{
    ShowPrompt(context);
    while (true)
    {
        ProcessCommand();
    }
}

core.Dispose();
System.Environment.Exit(0);

bool ProcessArgs(List<string> args)
{
    var useConsole = false;
    var willRun = false;
    // Currently just looking for -run or -file
    while (args.Count > 0)
    {
        var arg = TakeArg(args);
        if (arg == null)
            break;

        switch (arg.ToLower())
        {
            case "-file":
                {
                    var file = TakeArg(args);
                    if (file == null)
                    {
                        ConsoleControl.WriteLine($"{ConsoleControl.ErrorColour}Invalid parameter. -file requires filename parameter");
                        core.Dispose();
                        Environment.Exit(1);
                    }

                    var info = new FileInfo(file);
                    if (info.Directory is { Exists: true })
                    {
                        ConsoleControl.WriteLine($"{ConsoleControl.ErrorColour}Folder for {file} doesn't exist");
                        core.Dispose();
                        Environment.Exit(1);
                    }

                    core.Config.XmlTvConfig.Filename = file;
                    break;
                }
            case "-console":
                useConsole = true;
                break;
            case "-run":
                {
                    useConsole = false;
                    willRun = true;
                    break;
                }
            case "-help":
            case "-?":
                ConsoleControl.WriteLine("Usage: Console [-file <filename>] [-run]. No arguments will start the console -file will set the xmltv filename to use and -run will bypass console and create an xmltv file");
                core.Dispose();
                Environment.Exit(0);
                break;
            default:
                ConsoleControl.WriteLine($"{ConsoleControl.ErrorColour}Invalid argument {arg}");
                core.Dispose();
                Environment.Exit(1);
                break;
        }
    }

    if (useConsole && willRun)
    {
        ConsoleControl.WriteLine($"{ConsoleControl.ErrorColour}Cannot set console and run modes at the same time");
        core.Dispose();
        Environment.Exit(1);
    }

    if (willRun)
        core.MakeXmlTV();

    return useConsole;
}

static string? TakeArg(List<string> args)
{
    var arg = args.FirstOrDefault();
    if (arg == null)
        return null;
    args.RemoveAt(0);
    return arg;
}

void ProcessCommand()
{
    var command = Console.ReadLine();
    var result = core.HandleCommand(ref context, command ?? string.Empty);
    if (result.StartsWith("**END**"))
    {
        ConsoleControl.WriteLine("Exiting..");
        System.Environment.Exit(0);
    }

    ConsoleControl.Write(result);
    ShowPrompt(context);
}

void ShowPrompt(FolderEntry context)
{
    ConsoleControl.Write($"{ConsoleControl.SetFG(ConsoleColor.Cyan)}{context.FolderPath} :: {ConsoleControl.SetFG(ConsoleColor.White)}");
}

void UpdateFeedback(object? sender, FeedbackEventArgs eventArgs)
{
    if (eventArgs.Info.CurrentItem.Equals(0) && eventArgs.Info.MaxItems.Equals(0))
    {
        // Handle straightforward message update
        if (progressMode)
        {
            ConsoleControl.WriteLine("");
            progressMode = false;
        }

        ConsoleControl.WriteLine(eventArgs.Info.Status);
    }
    else
    {
        // Otherwise handle progress bar
        progressMode = true;
        var progress = Math.Min((int)Math.Round(eventArgs.Info.Percent / 4, 0, MidpointRounding.AwayFromZero), 25);
        var statusString = $"{eventArgs.Info.Status,-35}[{new string('▓', progress)}{new string('░', 25 - progress)}] {eventArgs.Info.CurrentItem}/{eventArgs.Info.MaxItems} ({Math.Round(eventArgs.Info.Percent, 2):0.00}%)";
        if (!lastStatus.Equals(eventArgs.Info.Status))
            ConsoleControl.Write(Environment.NewLine);
        ConsoleControl.Write(statusString + new string('\b', statusString.Length));
        lastStatus = eventArgs.Info.Status;
    }
}