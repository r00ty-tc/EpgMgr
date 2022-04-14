// See https://aka.ms/new-console-template for more information

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using EpgMgr;
using EpgMgr.Plugins;
using Console = System.Console;

[DllImport("mscoree.dll", CharSet = CharSet.Unicode)]
static extern bool StrongNameSignatureVerificationEx(string wszFilePath, bool fForceVerification, ref bool pfWasVerified);

#if SIGNED
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

var context = core.CommandMgr.RootFolder;
ConsoleControl.WriteLine($"EpgMgr Console {Assembly.GetExecutingAssembly().GetName().Version}");
ShowPrompt(context);
while (true)
{
    ProcessCommand();
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
            progressMode = false;
        ConsoleControl.WriteLine(eventArgs.Info.Status);
    }
    else
    {
        // Otherwise handle progress bar
        progressMode = true;
        int progress = (int)Math.Round(eventArgs.Info.Percent / 4, 0, MidpointRounding.AwayFromZero);
        var statusString = $"{eventArgs.Info.Status,-35}[{new string('▓', progress)}{new string('░', 25 - progress)}] {eventArgs.Info.CurrentItem}/{eventArgs.Info.MaxItems} ({Math.Round(eventArgs.Info.Percent, 2):0.00}%)";
        if (!lastStatus.Equals(eventArgs.Info.Status))
            ConsoleControl.Write(Environment.NewLine);
        ConsoleControl.Write(statusString + new string('\b', statusString.Length));
        lastStatus = eventArgs.Info.Status;
    }
}