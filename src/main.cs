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
StringBuilder RedirectOuput = null;
string OutputFile = null;
FileMode OutputMode = FileMode.Create; 
StringBuilder RedirectError = null;
string ErrorFile = null;
FileMode ErrorMode = FileMode.Create;

char[] EscapedSpecialCharacters = { '\"', '\'', '\\', 'n' };

while (true)
{
    Console.Write("$ "); 
    List<string> userInput = HandleUserInput(Console.ReadLine() ?? "");
    userInput = CheckForRedirection(userInput);
    string command = userInput[0];
    switch (command)
    {
        case "echo":
            {
                userInput.RemoveRange(0,2);
                string text = string.Join("", userInput);
                WriteLine(text);
                
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
                WriteLine(WorkingDirectory);
                break;
            }
        case "cat":
            {
                userInput.RemoveAt(0);
                userInput = userInput.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
                var invalidPaths = userInput.Where(x => !File.Exists(x));
                var validPaths = userInput.Where(File.Exists);
                if (validPaths.Any())
                {
                    var allContent = validPaths.Select(File.ReadAllText);
                    WriteLine(string.Join("", allContent).Trim());
                }
                foreach (var path in invalidPaths)
                {
                    WriteLine($"cat: {path}: No such file or directory", isError: true);
                    break;
                } 
                break; 
            }
        default:
            {
                CheckForProgram(userInput);
                break;
            }
    }
    EndRedirectOutput();
}
void WriteLine(string text, bool isError = false)
{
    if (isError && RedirectError != null)
        RedirectError.AppendLine(text);
    else if (RedirectOuput != null && !isError)
        RedirectOuput.AppendLine(text);
    else 
        Console.WriteLine(text);
}
List<string> CheckForRedirection(List<string> userInputs)
{
    var redirectOutput = userInputs.IndexOf(">");
    if (redirectOutput != -1)
        OutputMode = FileMode.Create;
    var redirectOutputExisting = userInputs.IndexOf(">>");
    if (redirectOutputExisting != -1)
    {
        OutputMode = FileMode.Open;
        redirectOutput = redirectOutputExisting;
    }
    if (redirectOutput != -1)
    {
        RedirectOuput = new StringBuilder();
        OutputFile = userInputs[redirectOutput + 2];
        userInputs.RemoveRange(redirectOutput, userInputs.Count - redirectOutput);
    }
    var redirectError = userInputs.IndexOf("2>");
    if (redirectError != -1)
        OutputMode = FileMode.Create;
    var redirectErrorExisting = userInputs.IndexOf("2>>");
    if (redirectErrorExisting != -1)
    {
        OutputMode = FileMode.Open;
        redirectError = redirectErrorExisting;
    }
    if (redirectError != -1)
    {
        RedirectError = new StringBuilder();
        ErrorFile = userInputs[redirectError + 2];
        userInputs.RemoveRange(redirectError, userInputs.Count - redirectError);
    }
    return userInputs;
}

async Task EndRedirectOutput()
{
    if (RedirectError != null)
    {
        Writer = new StreamWriter(ErrorFile, append: ErrorMode == FileMode.Open) { AutoFlush = true };
        await Writer.WriteAsync(RedirectError.ToString().TrimEnd());
        Writer.Close();
        ErrorFile = null;
        RedirectError = null;
    }
    if (RedirectOuput != null)
    {
        Writer = new StreamWriter(OutputFile, append: OutputMode == FileMode.Open) { AutoFlush = true };
        await Writer.WriteAsync(RedirectOuput.ToString().TrimEnd());
        Writer.Close();
        OutputFile = null;
        RedirectOuput = null;
    }
    Fs = null;
    Writer = null; 
}
void CheckCommandPathExists(string inputText)
{
    if (AllCommands.Contains(inputText))
    {
        WriteLine($"{inputText} is a shell builtin");
        return;
    }
    var fullPath = CheckFilePathExist(inputText);
    if (!string.IsNullOrWhiteSpace(fullPath))
        WriteLine($"{inputText} is {fullPath}");
    else
        WriteLine($"{inputText}: not found", isError: true);
}

void CheckForProgram(List<string> userInput)
{
    userInput = userInput.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
    var fileName = userInput[0];
    userInput.RemoveAt(0);
    var fullPath = CheckFilePathExist(fileName, includeFileName: false);

    if (!string.IsNullOrWhiteSpace(fullPath))
    {
        StartProcess(fullPath, fileName, string.Join(" ", userInput));
        return;
    }
    WriteLine($"{fileName}: command not found", isError: true);
}

string CheckFilePathExist(string inputText, bool includeFileName = true)
{
    foreach (var path in Paths)
    {
        var fullPath = Path.Combine(path, inputText);
        if (Path.Exists(fullPath))
        {
            return includeFileName ? fullPath : path;
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
            WriteLine(directory);
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
        WriteLine($"cd: {newDirectory}: No such file or directory", isError: true);
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
    userInput = userInput.Replace(DoubleQuotesEscaped, "\"")
                         .Replace(SingleQuotesEscaped, "\'")
                         .Replace("1>", ">");
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

void StartProcess(string filePath, string fileName, string args)
{
    ProcessStartInfo startInfo = new ProcessStartInfo(fileName, args);
    Process process = new Process() { StartInfo = startInfo };
    process.StartInfo.FileName = fileName;
    process.StartInfo.WorkingDirectory = filePath; 
    process.StartInfo.UseShellExecute = false;
    process.StartInfo.RedirectStandardOutput = true;
    process.StartInfo.RedirectStandardError = true;
    StringBuilder output = new StringBuilder();
    StringBuilder errors = new StringBuilder();
    process.OutputDataReceived += (_, dataReceived) => output.AppendLine(dataReceived.Data);
    process.ErrorDataReceived += (_, dataReceived) => errors.AppendLine(dataReceived.Data);
    process.Start();
    process.BeginOutputReadLine();
    process.BeginErrorReadLine();
    process.WaitForExit();
    if (!string.IsNullOrWhiteSpace(output.ToString()))
        WriteLine(output.ToString().Trim());
    if (!string.IsNullOrWhiteSpace(errors.ToString()))
        WriteLine(errors.ToString().Trim(), isError: true);
    return;
}
