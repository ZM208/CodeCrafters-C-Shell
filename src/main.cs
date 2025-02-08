using System.Net;
using System.Net.Sockets;
using System;
using System.Diagnostics;
// Uncomment this line to pass the first stage

// Wait for user input

string[] AllCommands = { "echo", "type", "exit", "pwd", "cd" };
string[] Paths = Environment.GetEnvironmentVariable("PATH")?.Split(":") ?? [""];
string WorkingDirectory = Environment.CurrentDirectory;

while (true)
{
    Console.Write("$ ");
    string userInput = Console.ReadLine() ?? "";
    string command = userInput.Split(' ')[0];

    switch (command)
    {
        case "echo":
            {
                var text = userInput.Replace("echo ", "");
                if (text.Contains("'"))
                    text = text.Replace("'", "");
                else
                    text = text.Trim(' ');
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
                var inputText = userInput.Split(' ')[1];
                CheckCommandPathExists(inputText);
                break;
            }
        case "cd":
            {
                ChangeDirectory(userInput.Split(' ')[1]);
                break;
            }
        case "pwd":
            {
                Console.WriteLine(WorkingDirectory);
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

void CheckForProgram(string userInput)
{
    var splitArgs = userInput.Split(" ");
    var fullPath = CheckFilePathExist(splitArgs[0]);
    if (!string.IsNullOrWhiteSpace(fullPath))
    {
        ProcessStartInfo startInfo = new ProcessStartInfo(splitArgs[0], splitArgs[1]);
        Process process = new Process() { StartInfo = startInfo };
        process.Start();
        process.WaitForExit();
        return;
    }
    Console.WriteLine($"{userInput}: command not found");
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
