using Common.Contract;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Common
{
    public sealed class Client : IDisposable
    {
        private readonly CancellationTokenSource _source;
        private readonly TcpClient _client;
        private readonly string _username;
        private NetworkStream _stream;

        public Client()
        {
            _source = new CancellationTokenSource();
            _client = new TcpClient();

            Console.Write("Username: ");
            _username = Console.ReadLine();
        }


        public void Dispose()
        {
            _client.Dispose();
        }

        public async Task ConnectAsync()
        {
            await _client.ConnectAsync(Constant.Endpoint.Address, Constant.Endpoint.Port);
            _stream = _client.GetStream();

            var handshake = new HandShake(_username);
            await _stream.WriteAsync(handshake.Buffer);

            var awaiter = AwaitMessageAsync();
            while (!_source.IsCancellationRequested)
            {
                Console.Write("#>");
                var message = new Message(Console.ReadLine());
                await _stream.WriteAsync(message.Buffer);
            }

            Task.WaitAll(awaiter);
            Console.WriteLine("Exited");
        }


        private async Task AwaitMessageAsync()
        {
            var buffer = new byte[_client.ReceiveBufferSize];
            while (!_source.IsCancellationRequested)
            {
                await Task.Delay(50);
                var count = await _stream.ReadAsync(buffer);
                if (count == 0)
                {
                    continue;
                }

                var message = Encoding.ASCII.GetString(buffer, 0, count);
                Console.Write(message);
                Console.Write("#>");
            }
        }
    }
}
