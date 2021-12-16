using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace EztransServer.Core
{
    internal class ConcurrentBuffer<T>
    {

        private class Client
        {
            public readonly TaskCompletionSource<T> Waiting;
            public readonly CancellationTokenRegistration Cancelling;

            public Client(CancellationToken token)
            {
                Waiting = new TaskCompletionSource<T>();
                Cancelling = token.Register(() => Waiting.TrySetCanceled(token));
            }
        }

        private readonly ConcurrentQueue<T> _dataQueue = new ConcurrentQueue<T>();
        private readonly ConcurrentQueue<Client> _clients = new ConcurrentQueue<Client>();

        public int PendingSize => _dataQueue.Count;

        public int HungerSize => _clients.Count;

        public Task<T> ReceiveAsync()
        {
            return ReceiveAsync(CancellationToken.None);
        }

        public Task<T> ReceiveAsync(CancellationToken token)
        {
            if (_dataQueue.TryDequeue(out T res))
            {
                return Task.FromResult(res);
            }
            else
            {
                var client = new Client(token);
                _clients.Enqueue(client);
                return client.Waiting.Task;
            }
        }

        public void Enqueue(T value)
        {
            while (_clients.TryDequeue(out Client client))
            {
                client.Cancelling.Dispose();
                if (client.Waiting.TrySetResult(value))
                {
                    return;
                }
            }
            _dataQueue.Enqueue(value);
        }

        public void Abort()
        {
            while (_clients.TryDequeue(out Client client))
            {
                client.Cancelling.Dispose();
                client.Waiting.TrySetCanceled();
            }
        }
    }
}
