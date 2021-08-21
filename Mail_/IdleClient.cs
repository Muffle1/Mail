using MailKit;
using MailKit.Net.Imap;
using MailKit.Security;
using MailLibrary.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mail_
{
    

    public class IdleClient : IDisposable
    {
        private readonly string host, username, password;
        private readonly SecureSocketOptions sslOptions;
        private readonly int port;
        List<IMessageSummary> messages;
        CancellationTokenSource cancel;
        CancellationTokenSource done;
        bool messagesArrived;
        ImapClient client;
        private ShowNotify showNotify;

        public IdleClient(User user, SecureSocketOptions sslOptions, ShowNotify showNotify)
        {
            this.client = new ImapClient(new ProtocolLogger(Console.OpenStandardError()));
            this.messages = new List<IMessageSummary>();
            this.cancel = new CancellationTokenSource();
            this.sslOptions = sslOptions;
            this.username = user.Login;
            this.password = user.Password;
            this.host = user.Imap;
            this.port = user.Port;
            this.showNotify = showNotify;
        }

        public void Reconnect()
        {
            if (!client.IsConnected)
                client.Connect(host, port, sslOptions, cancel.Token);

            if (!client.IsAuthenticated)
            {
                client.Authenticate(username, password, cancel.Token);
                client.Inbox.Open(FolderAccess.ReadOnly, cancel.Token);
            }
        }

        async Task FetchMessageSummariesAsync()
        {
            IList<IMessageSummary> fetched;
            while (true)
            {
                try
                {
                    int startIndex = messages.Count;

                    fetched = client.Inbox.Fetch(startIndex, -1, MessageSummaryItems.Full | MessageSummaryItems.UniqueId, cancel.Token);
                    break;
                }
                catch (ImapProtocolException)
                {
                    Reconnect();
                }
                catch (IOException)
                {
                    Reconnect();
                }
            }
            foreach (var message in fetched)
                messages.Add(message);
        }

        async Task WaitForNewMessagesAsync()
        {
            do
            {
                try
                {
                    if (client.Capabilities.HasFlag(ImapCapabilities.Idle))
                    {
                        done = new CancellationTokenSource(new TimeSpan(0, 9, 0));
                        try
                        {
                            client.Idle(done.Token, cancel.Token);
                        }
                        finally
                        {
                            done.Dispose();
                            done = null;
                        }
                    }
                    else
                    {
                        await Task.Delay(new TimeSpan(0, 1, 0), cancel.Token);
                        client.NoOp(cancel.Token);
                    }
                    break;
                }
                catch (ImapProtocolException)
                {
                    Reconnect();
                }
                catch (IOException)
                {
                    Reconnect();
                }
            } while (true);
        }

        async Task IdleAsync()
        {
            do
            {
                try
                {
                    await WaitForNewMessagesAsync();

                    if (messagesArrived)
                    {
                        this.showNotify(username);
                        await FetchMessageSummariesAsync();
                        messagesArrived = false;
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            } while (!cancel.IsCancellationRequested);
        }

        public async Task RunAsync()
        {
            try
            {
                Reconnect();
                await FetchMessageSummariesAsync();
            }
            catch (OperationCanceledException)
            {
                await client.DisconnectAsync(true);
                return;
            }
            var inbox = client.Inbox;
            inbox.CountChanged += OnCountChanged;
            inbox.MessageExpunged += OnMessageExpunged;

            await IdleAsync();

            inbox.MessageExpunged -= OnMessageExpunged;
            inbox.CountChanged -= OnCountChanged;

            await client.DisconnectAsync(true);
        }

        void OnCountChanged(object sender, EventArgs e)
        {
            var folder = (ImapFolder)sender;
            if (folder.Count > messages.Count)
            {
                int arrived = folder.Count - messages.Count;
                if (arrived > 1)
                    Console.WriteLine("\t{0} new messages have arrived.", arrived);
                if (arrived == 1)
                    Console.WriteLine("\t1 new message has arrived.");
                messagesArrived = true;
                done?.Cancel();
            }
        }

        void OnMessageExpunged(object sender, MessageEventArgs e)
        {
            if (e.Index < messages.Count)
            {
                messages.RemoveAt(e.Index); 
            }
        }

        public void Exit()
        {
            cancel.Cancel();
        }

        public void Dispose()
        {
            client.Dispose();
            cancel.Dispose();
        }
    }
}
