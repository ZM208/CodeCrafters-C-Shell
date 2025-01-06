using System.Net;
using System.Net.Sockets;
using System;
// Uncomment this line to pass the first stage

// Wait for user input
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
        default:
            {
                Console.Write($"{userInput}: command not found\n");
                break;
            }
    }
}
