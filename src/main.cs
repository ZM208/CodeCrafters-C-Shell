using System.Net;
using System.Net.Sockets;
using System;
// Uncomment this line to pass the first stage

// Wait for user input
while (true)
{
    Console.Write("$ ");
    string userInput = Console.ReadLine() ?? "";
    Console.Write($"{userInput}: command not found\n");
    if (userInput.Contains("exit"))
    {
        Environment.Exit(0);
    }
}
