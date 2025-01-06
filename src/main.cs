using System.Net;
using System.Net.Sockets;
using System;
// Uncomment this line to pass the first stage

// Wait for user input
 string[] AllCommands = { "echo", "type", "exit" };

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
                if (AllCommands.Contains(inputText))
                    Console.Write($"{inputText} is a shell builtin");
                else
                    Console.Write($"{inputText}: not found");
                break;
            }
        default:
            {
                Console.Write($"{userInput}: command not found");
                break;
            }
    }
    Console.Write('\n');
}
