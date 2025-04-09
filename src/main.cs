using System.Net;
using System.Net.Sockets;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics.Tracing;
using System.Text;
using System.Net.WebSockets;
using System.IO;
// Uncomment this line to pass the first stage

// Wait for user input

string[] AllCommands = { "echo", "type", "exit", "pwd", "cd" };
string[] Paths = Environment.GetEnvironmentVariable("PATH")?.Split(":") ?? [""];
string WorkingDirectory = Environment.CurrentDirectory;
// need to replace single quotes and double quotes with unique characters so regex won't try to keep literal value when matching
const string SingleQuotesEscaped = "[sq]";
const string DoubleQuotesEscaped = "[dq]";
FileStream Fs = null;
StreamWriter Writer = null; 
TextWriter DefaultOutput = Console.Out;
char[] EscapedSpecialCharacters = { '\"', '\'', '\\', 'n' };

while (true)
{
    Console.Write("$ "); 
    List<string> userInput = HandleUserInput(Console.ReadLine() ?? "");
    if (userInput.Contains(">"))
        BeginRedirectOutput(userInput);
    string command = userInput[0];
    switch (command)
    {
        case "echo":
            {
                userInput.RemoveRange(0,2);
                string text = string.Join("", userInput);
                Console.WriteLine(text);
                
                break;
            }
        case "exit":
            {
                Environment.Exit(0);
                break;
            }
        case "type":
            {
                var inputText = userInput[2];
                CheckCommandPathExists(inputText);
                break;
            }
        case "cd":
            {
                ChangeDirectory(userInput[2]);
                break;
            }
        case "pwd":
            {
                Console.WriteLine(WorkingDirectory);
                break;
            }
        case "cat":
            {
                userInput.RemoveAt(0);
                userInput = userInput.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
                var invalidPath = userInput.FirstOrDefault(x => !File.Exists(x));
                if (invalidPath != null)
                {
                    Console.SetOut(DefaultOutput); // test fix for cat errors. Refactoring if works for proper fix
                    Console.WriteLine($"cat: {invalidPath}: No such file or directory");
                    break;
                }
                var allContent = userInput.Select(File.ReadAllText);
                Console.WriteLine(string.Join("", allContent).Trim());
                break; 
            }
        default:
            {
                CheckForProgram(userInput);
                break;
            }
    }
    if (Fs != null)
        EndRedirectOutput();
}

void BeginRedirectOutput(List<string> userInput)
{
    Fs = new FileStream(userInput[userInput.Count - 1], FileMode.Create);
    Writer = new StreamWriter(Fs, new UTF8Encoding(true)) { AutoFlush = true };
    Console.SetOut(Writer);
    var symbolIndex = userInput.IndexOf(">");
    userInput.RemoveRange(symbolIndex, userInput.Count - symbolIndex);
}

void EndRedirectOutput()
{
    Console.SetOut(DefaultOutput);
    Writer.Close();
    Fs.Close();
    Fs = null;
    Writer = null; 
}
void CheckCommandPathExists(string inputText)
{
    if (AllCommands.Contains(inputText))
    {
        Console.WriteLine($"{inputText} is a shell builtin");
        return;
    }
    var fullPath = CheckFilePathExist(inputText);
    if (!string.IsNullOrWhiteSpace(fullPath))
        Console.WriteLine($"{inputText} is {fullPath}");
    else
        Console.WriteLine($"{inputText}: not found");
}

void CheckForProgram(List<string> userInput)
{
    userInput = userInput.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
    var fileName = userInput[0];
    userInput.RemoveAt(0);
    var fullPath = CheckFilePathExist(fileName);

    if (!string.IsNullOrWhiteSpace(fullPath))
    {
        StartProcess(fullPath, string.Join(" ", userInput));
        return;
    }
    Console.WriteLine($"{fileName}: command not found");
}

string CheckFilePathExist(string inputText)
{
    foreach (var path in Paths)
    {
        var fullPath = Path.Combine(path, inputText);
        if (Path.Exists(fullPath))
        {
            return fullPath;
        }
    }
    return "";
}

void ChangeDirectory(string requestDirectory)
{
    var newDirectory = WorkingDirectory;
    if (requestDirectory.Contains("../"))
    {
        var backAmount = requestDirectory.Split("../").Count();
        for (int i = 1; i < backAmount; i++)
        {
            var lastPath = newDirectory.LastIndexOf('/');
            newDirectory = newDirectory.Substring(0, lastPath);
        }
    }
    else if (requestDirectory.Contains("./dir"))
    {
        var listAllDirectories = Directory.GetDirectories(newDirectory);
        foreach (var directory in listAllDirectories)
        {
            Console.WriteLine(directory);
        }
    }
    else if (requestDirectory.Contains("./")) {
        requestDirectory = requestDirectory.Replace(".", "");
        newDirectory = newDirectory + requestDirectory;
    }
    else if (requestDirectory.Contains('/'))
    {
        newDirectory = requestDirectory;
    }
    else if (requestDirectory.Contains('~'))
    {
        newDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }
    if (Path.Exists(newDirectory))
        WorkingDirectory = newDirectory;
    else
        Console.WriteLine($"cd: {newDirectory}: No such file or directory");
}

List<string> HandleUserInput(string userInput)
{
    userInput = userInput.Replace("\\\"", "\\" + DoubleQuotesEscaped).Replace("\\\'", "\\" + SingleQuotesEscaped);
    string pattern = @"([""'""])(.+?)\1|\S+|\s(?!\s)"; // main pattern to split up into aruguments
    List<string> filteredInput = [];
    MatchCollection matches = Regex.Matches(userInput, pattern);
    bool catMode = matches[0].Value == "cat" || !AllCommands.Contains(matches[0].Value); // won't replace escape chars in double quotes for files related commands

    var regexQuotes = new Regex("^[\"'](.*)[\"']$"); // removing quotes before returning list
    foreach (Match match in matches)
    {
        var removedEscapeCharacters = FilterUserInput(match.Value, catMode);
        filteredInput.Add(regexQuotes.Replace(removedEscapeCharacters, "$1"));
    }
    return filteredInput;
}
 
string FilterUserInput(string userInput, bool catMode)
{
    userInput = userInput.Replace(DoubleQuotesEscaped, "\"").Replace(SingleQuotesEscaped, "\'").Replace("1>", ">");
    string result = "";
    bool doubleQuotes = false;
    bool singleQuotes = false;
    bool escapeQuotes = false;
    foreach (var character in userInput)
    { 
        if (escapeQuotes)
        {
            escapeQuotes = !escapeQuotes;
            if ((!EscapedSpecialCharacters.Contains(character)) || catMode)
                result += '\\';
            result += character;
            continue;
        }
        else if (character == '\'')
            singleQuotes = !singleQuotes;
        
        else if (character == '\\' && !singleQuotes)
        {
            escapeQuotes = !escapeQuotes;
            continue;
        }
        else if(character == '"' && !singleQuotes)
            doubleQuotes = !doubleQuotes;
        result += character;
    }
    return result;
}

void StartProcess(string fileName, string args)
{
    ProcessStartInfo startInfo = new ProcessStartInfo(fileName, args);
    Process process = new Process() { StartInfo = startInfo };
    process.StartInfo.UseShellExecute = false;
    process.StartInfo.RedirectStandardOutput = true;
    process.StartInfo.RedirectStandardError = false;
    StringBuilder output = new StringBuilder();
    process.OutputDataReceived += (_, dataReceived) => output.AppendLine(dataReceived.Data);

    process.Start();
    process.BeginOutputReadLine();
    process.WaitForExit();
    Console.Write(output.ToString().Trim());
    return;
}
