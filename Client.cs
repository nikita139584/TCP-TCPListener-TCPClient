using System.Net;
using System.Net.Sockets;
using System.Text;

class Client
{
    private const int DEFAULT_BUFLEN = 512;
    private const int DEFAULT_PORT = 27015;

    static async Task Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        await client.ConnectAsync(new IPEndPoint(IPAddress.Loopback, DEFAULT_PORT));

        Console.WriteLine("Підключено до сервера");

        var sendTask = Task.Run(async () =>
        {
            while (true)
            {
                Console.Write("Введіть валюти (USD EUR): ");
                var msg = Console.ReadLine();
                if (msg == "exit") break;

                var data = Encoding.UTF8.GetBytes(msg!);
                await client.SendAsync(data);
            }

            client.Shutdown(SocketShutdown.Send);
        });

        var receiveTask = Task.Run(async () =>
        {
            var buffer = new byte[DEFAULT_BUFLEN];

            while (true)
            {
                int bytes = await client.ReceiveAsync(buffer);
                if (bytes == 0) break;

                var response = Encoding.UTF8.GetString(buffer, 0, bytes);
                Console.WriteLine($"Курс: {response}");
            }
        });

        await Task.WhenAll(sendTask, receiveTask);

        client.Close();
    }
}