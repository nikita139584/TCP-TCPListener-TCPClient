using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

class Server
{
    private const int DEFAULT_BUFLEN = 512;
    private const int DEFAULT_PORT = 27015;

    private static ConcurrentQueue<(Socket client, byte[] data, int length)> messageQueue
        = new ConcurrentQueue<(Socket, byte[], int)>();

    private static Dictionary<string, double> rates = new Dictionary<string, double>()
    {
        { "USD_EUR", 0.9 },
        { "EUR_USD", 1.1 },
        { "USD_UAH", 40 },
        { "UAH_USD", 0.025 }
    };

    static async Task Main()
    {

        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("Сервер запущено");

        var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(new IPEndPoint(IPAddress.Any, DEFAULT_PORT));
        listener.Listen(10);

        var client = await listener.AcceptAsync();
        Console.WriteLine($"Клієнт підключився: {client.RemoteEndPoint} | {DateTime.Now}");

        _ = ProcessMessages();

        var buffer = new byte[DEFAULT_BUFLEN];

        while (true)
        {
            int bytes = await client.ReceiveAsync(buffer);
            if (bytes == 0) break;

            var data = new byte[bytes];
            Buffer.BlockCopy(buffer, 0, data, 0, bytes);

            messageQueue.Enqueue((client, data, bytes));
        }

        Console.WriteLine($"Клієнт відключився: {DateTime.Now}");
        client.Close();
    }

    private static async Task ProcessMessages()
    {
        while (true)
        {
            if (messageQueue.TryDequeue(out var item))
            {
                var (client, data, length) = item;
                var message = Encoding.UTF8.GetString(data, 0, length);

                Console.WriteLine($"Запит: {message}");

                var parts = message.Split(' ');

                string response;

                if (parts.Length == 2)
                {
                    string key = $"{parts[0]}_{parts[1]}";

                    if (rates.ContainsKey(key))
                        response = rates[key].ToString();
                    else
                        response = "Немає курсу";
                }
                else
                {
                    response = "Формат: USD EUR";
                }

                var bytes = Encoding.UTF8.GetBytes(response);
                await client.SendAsync(bytes);
            }

            await Task.Delay(50);
        }
    }
}