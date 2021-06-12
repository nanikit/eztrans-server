using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace EZTransServer.Core
{
    public class ConcurrentBuffer<T>
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

        private readonly ConcurrentQueue<T> DataQueue = new ConcurrentQueue<T>();
        private readonly ConcurrentQueue<Client> Clients = new ConcurrentQueue<Client>();

        public int PendingSize => DataQueue.Count;

        public int HungerSize => Clients.Count;

        public Task<T> ReceiveAsync()
        {
            return ReceiveAsync(CancellationToken.None);
        }

        public Task<T> ReceiveAsync(CancellationToken token)
        {
            if (DataQueue.TryDequeue(out T res))
            {
                return Task.FromResult(res);
            }
            else
            {
                var client = new Client(token);
                Clients.Enqueue(client);
                return client.Waiting.Task;
            }
        }

        public void Enqueue(T value)
        {
            while (Clients.TryDequeue(out Client client))
            {
                client.Cancelling.Dispose();
                if (client.Waiting.TrySetResult(value))
                {
                    return;
                }
            }
            DataQueue.Enqueue(value);
        }

        public void Abort()
        {
            while (Clients.TryDequeue(out Client client))
            {
                client.Cancelling.Dispose();
                client.Waiting.TrySetCanceled();
            }
        }
    }
}
