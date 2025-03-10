using System.Net;
using System.Net.Sockets;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics.Tracing;
// Uncomment this line to pass the first stage

// Wait for user input

string[] AllCommands = { "echo", "type", "exit", "pwd", "cd" };
string[] Paths = Environment.GetEnvironmentVariable("PATH")?.Split(":") ?? [""];
string WorkingDirectory = Environment.CurrentDirectory;
// need to replace single quotes and double quotes with unique characters so regex won't try to keep literal value when matching
const string SingleQuotesEscaped = "[sq]";
const string DoubleQuotesEscaped = "[dq]";

while (true)
{
    Console.Write("$ "); 
    List<string> userInput = HandleUserInput(Console.ReadLine() ?? "");
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
                var allContent = userInput.Select(File.ReadAllText);
                Console.Write(string.Join("", allContent));
                break; 
            }
        default:
            {
                CheckForProgram(userInput);
                break;
            }
    }
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
    var fullPath = CheckFilePathExist(userInput[0]);
    if (!string.IsNullOrWhiteSpace(fullPath))
    {
        ProcessStartInfo startInfo = new ProcessStartInfo(userInput[0], userInput[1]);
        Process process = new Process() { StartInfo = startInfo };
        process.Start();
        process.WaitForExit();
        return;
    }
    Console.WriteLine($"{userInput[0]}: command not found");
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
    var regexQuotes = new Regex("^[\"'](.*?[^\"']+)[\"']$"); // removing quotes before returning list
    var regexEscapeCharacter = new Regex("\\\\(?=(?:[^\"']*(?:\"[^\"']*\"|'[^\"']*'))*[^\"']*$)"); // removes escape characters outside of quotes
    foreach (Match match in matches)
    {
        var removedEscapeCharacters = regexEscapeCharacter.Replace(match.Value, "");
        filteredInput.Add(regexQuotes.Replace(removedEscapeCharacters, "$1").Replace(DoubleQuotesEscaped, "\"").Replace(SingleQuotesEscaped, "\'"));
    }
    return filteredInput;
}