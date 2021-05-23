using Common;
using Common.Contract;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    class Program
    {
        private static int _currentMessageIndex = 0;
        // needs to rotate because of memory leak but just for demonstrate
        private static SortedList<int, byte[]> _messages;
        private static ConcurrentDictionary<EndPoint, Tuple<byte[], Socket>> _clients;
        private static ConcurrentDictionary<EndPoint, Task> _messageAwaiter;

        static async Task Main(string[] args)
        {
            _messages = new SortedList<int, byte[]>();
            _clients = new ConcurrentDictionary<EndPoint, Tuple<byte[], Socket>>();
            _messageAwaiter = new ConcurrentDictionary<EndPoint, Task>();

            var source = new CancellationTokenSource();
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(Constant.Endpoint);
            socket.Listen();
            Console.WriteLine("Started");

            var tasks = new Task[]
            {
                AwaitCommandAsync(source),
                AwaitClientsAsync(socket, source),
                AwaitMessageAsync(source)
            };

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Exited");
            }
            finally
            {
                socket.Dispose();
            }
        }


        private static async Task AwaitClientsAsync(Socket server, CancellationTokenSource source)
        {
            var tasks = new Task<Socket>[1];
            while (!source.IsCancellationRequested)
            {
                var client = tasks[0] = server.AcceptAsync();
                // to prevent unlim "accept" awaiter
                Task.WaitAny(tasks, source.Token);
                // obtain handshake
                await ReceiveMessageAsync(client.Result, source);
                // send last 30 messages
                var history = _messages.OrderByDescending(x => x.Key).Select(x => x.Value).Take(30).ToArray();
                await SendMessageAsync(client.Result, history);
            }
        }


        private static async Task SendMessageAsync(Socket recipient, byte[][] buffers)
        {
            // new line for each message
            var cntrl = new byte[] { 0x0A };
            foreach (var buffer in buffers)
            {
                await recipient.SendAsync(buffer, SocketFlags.None);
                await recipient.SendAsync(cntrl, SocketFlags.None);
            }
        }


        private static async Task ReceiveMessageAsync(Socket client, CancellationTokenSource source, bool loop = false)
        {
            var buffer = new byte[client.ReceiveBufferSize];
            while (!source.IsCancellationRequested)
            {
                var count = await client.ReceiveAsync(buffer, SocketFlags.None);
                if (count == 0)
                {
                    return;
                }

                var localBuffer = buffer.Take(count).ToArray();
                switch (localBuffer[0])
                {
                    case HandShake.HEADER:
                        var usernameBuffer = localBuffer.Skip(1).ToArray();
                        _clients.AddOrUpdate(client.RemoteEndPoint,
                            (_) => new Tuple<byte[], Socket>(usernameBuffer, client),
                            (_, __) => new Tuple<byte[], Socket>(usernameBuffer, client));

                        var username = Encoding.ASCII.GetString(usernameBuffer);
                        Console.WriteLine("[{0}] {1} connected", client.RemoteEndPoint, username);
                        break;

                    case Message.HEADER:
                        var authorBuffer = _clients[client.RemoteEndPoint].Item1;
                        var messageBuffer = localBuffer.Skip(1).ToArray();
                        var fullTextBuffer = ArrayExtensions.Join(new byte[][]
                        {
                            authorBuffer,
                            // :[space]
                            new byte[] { 0x3A, 0x20 },
                            messageBuffer
                        });

                        _messages.Add(_currentMessageIndex++, fullTextBuffer);
                        foreach (var c in _clients)
                        {
                            await SendMessageAsync(c.Value.Item2, new byte[][] { fullTextBuffer });
                        }

                        var message = Encoding.ASCII.GetString(fullTextBuffer);
                        Console.WriteLine(message);


                        break;
                }

                if (!loop)
                {
                    break;
                }
            }
        }


        private static async Task AwaitMessageAsync(CancellationTokenSource source)
        {
            while (!source.IsCancellationRequested)
            {
                await Task.Delay(50);
                if (_messageAwaiter.Count != _clients.Count)
                {
                    foreach (var client in _clients)
                    {
                        if (!_messageAwaiter.ContainsKey(client.Key))
                        {
                            _messageAwaiter.TryAdd(client.Key, ReceiveMessageAsync(client.Value.Item2, source, loop: true));
                        }
                    }
                }
            }
        }


        private static async Task AwaitCommandAsync(CancellationTokenSource source)
        {
            while (!source.IsCancellationRequested)
            {
                await Task.Delay(50);
                Console.Write("#>");
                var command = Console.ReadLine();
                if (string.IsNullOrEmpty(command))
                {
                    continue;
                }

                switch (command)
                {
                    case "exit":
                        source.Cancel();
                        break;
                    case "ls":
                        foreach (var client in _clients)
                        {
                            var username = Encoding.ASCII.GetString(client.Value.Item1);
                            Console.WriteLine("{0} [{1}]", username, client.Key);
                        }
                        break;
                    default:
                        Console.WriteLine("Unknown command");
                        continue;
                }
            }
        }
    }
}
