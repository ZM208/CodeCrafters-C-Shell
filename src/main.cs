using System.Net;
using System.Net.Sockets;
using System;
using System.Diagnostics;
// Uncomment this line to pass the first stage

// Wait for user input
string[] AllCommands = { "echo", "type", "exit", "pwd", "cd" };
string[]? Paths = Environment.GetEnvironmentVariable("PATH")?.Split(":");
string workingDirectory = Environment.CurrentDirectory;
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
                Console.Write(text + "\n");
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
                var requestDirectory = userInput.Split(' ')[1];
                if (Path.Exists(requestDirectory))
                    workingDirectory = requestDirectory;
                else
                    Console.WriteLine($"cd: {requestDirectory}: No such file or directory");
                break;
            }
        case "pwd":
            {
                Console.WriteLine(workingDirectory);
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
        Console.Write($"{inputText} is a shell builtin \n");
        return;
    }    
    var fullPath = CheckFilePathExist(inputText);
    if (!string.IsNullOrWhiteSpace(fullPath))
        Console.Write($"{inputText} is {fullPath} \n");
    else 
        Console.Write($"{inputText}: not found \n");
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
    Console.Write($"{userInput}: command not found \n");
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