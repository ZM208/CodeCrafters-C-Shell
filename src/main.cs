using System.Net;
using System.Net.Sockets;

// Uncomment this line to pass the first stage

// Wait for user input
while (true)
{
    Console.Write("$ ");
    string userInput = Console.ReadLine();
    Console.Write($"{userInput}: command not found\n");
}
