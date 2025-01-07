using System.Net;
using System.Net.Sockets;
using System;
using System.Diagnostics;
// Uncomment this line to pass the first stage

// Wait for user input
string[] AllCommands = { "echo", "type", "exit" };
string[]? Paths = Environment.GetEnvironmentVariable("PATH")?.Split(":"); 

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
                Console.Write(text);
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

        default:
            {
                CheckForProgram(userInput);
                break;
            }
    }
    Console.Write('\n');
}
void CheckCommandPathExists(string inputText) 
{
    if (AllCommands.Contains(inputText))
    {
        Console.Write($"{inputText} is a shell builtin");
        return;
    }    
    var fullPath = CheckFilePathExist(inputText);
    if (!string.IsNullOrWhiteSpace(fullPath))
        Console.Write($"{inputText} is {fullPath}");

    Console.Write($"{inputText}: not found");
}

void CheckForProgram(string userInput)
{
    var splitArgs = userInput.Split(" ");
    var fullPath = CheckFilePathExist(splitArgs[0]);
    if (!string.IsNullOrWhiteSpace(fullPath))
    {
        ProcessStartInfo startInfo = new ProcessStartInfo(fullPath, splitArgs[1]);
        Process.Start(startInfo);
        return;
    }
    Console.Write($"{userInput}: command not found");
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