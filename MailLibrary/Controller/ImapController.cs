using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailLibrary.Model;
using MimeKit.Text;
using System;
using System.Windows;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using MailKit.Net.Proxy;
using MailKit.Security;

namespace MailLibrary.Controller
{
    public class ImapController : IServerClient
    {
        private ImapClient сlient;
        private ApplicationContext db;

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="user">Пользователь для устанвки соединения</param>
        public ImapController(User user)
        {
            try
            {
                Connection(user);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
            db = new ApplicationContext();
        }

        /// <summary>
        /// Получение сообщений
        /// </summary>
        /// <param name="nameFolder">Из какой папки брать сообщения</param>
        /// <param name="user"> Пользователь которому передавать полученные сообщения</param>
        /// <param name="token">Токен для прерывания процесса получения.</param>
        /// <param name="uiThread">Передача контекста.</param>
        /// <param name="newMessageLoad">Дата последнего нового сообщения.</param>
        public List<Message> GetMessages(string nameFolder, User user, Folder myFolder, CancellationToken token, int messageId)
        {
            List<Message> messages = new List<Message>();
            var folders = GetFolders();
            string textBody = null;
            foreach (var folder in folders)
            {
                if (folder.Name.ToUpper() == nameFolder.ToUpper())
                {
                    folder.Open(FolderAccess.ReadOnly);
                    List<UniqueId> idMessages = folder.Search(SearchQuery.All).Reverse().ToList();
                    int countMessages = idMessages.Count();
                    if (messageId == countMessages - 1)
                        break;
                    for (int j = messageId; j < messageId + 5 && j < countMessages; j++)
                    {
                        textBody = null;
                        var message = folder.GetMessage(idMessages[j]);
                        if (message.TextBody != null)
                            textBody = message.GetTextBody(TextFormat.Text).ToString();

                        messages.Add(new Message(message.From.ToString(), myFolder.Id, user.Id, message.Date.ToString(), message.HtmlBody, idMessages[j].Id.ToString(), message.Subject, textBody));

                        if (token.IsCancellationRequested)
                        {
                            folder.Close();
                            break;
                        }
                    }
                    return messages;
                }
            }
            return messages;
        }

        public List<Message> GetNewMessages(string nameFolder, User user, Folder myFolder, CancellationToken token, int messageId, DateTime oldMessageDate)
        {
            List<Message> messages = new List<Message>();
            var folders = GetFolders();
            string textBody;
            foreach (var folder in folders)
            {
                if (folder.Name.ToUpper() == nameFolder.ToUpper())
                {
                    folder.Open(FolderAccess.ReadOnly);
                    List<UniqueId> idMessages = folder.Search(SearchQuery.All).Reverse().ToList();
                    int countMessages = idMessages.Count();
                    if (messageId == countMessages - 1)
                        break;
                    for (int j = messageId; j < messageId + 5 && j < countMessages; j++)
                    {
                        textBody = null;
                        var message = folder.GetMessage(idMessages[j]);
                        if (message.TextBody != null)
                            textBody = message.GetTextBody(TextFormat.Text).ToString();

                        if (message.Date > oldMessageDate)
                            messages.Add(new Message(message.From.ToString(), myFolder.Id, user.Id, message.Date.ToString(), message.HtmlBody, idMessages[j].Id.ToString(), message.Subject, textBody));

                        if (token.IsCancellationRequested)
                        {
                            folder.Close();
                            break;
                        }
                    }
                    return messages;
                }
            }
            return messages;
        }

        /// <summary>
        /// Получение папок поьзователя.
        /// </summary>
        /// <returns>Возвращает полученные папки</returns>
        public ObservableCollection<IMailFolder> GetFolders()
        {
            return new ObservableCollection<IMailFolder>(сlient.GetFolders(сlient.PersonalNamespaces.First()).ToList());
        }

        public async void MoveToFolder(Message message, string nameNewFolder)
        {
            var folders = GetFolders();
            foreach (var folder in folders)
            {
                if (folder.Name.ToUpper() == message.Folder.Name.ToUpper())
                {
                    var folderName = folders.Where(x => x.Name.ToUpper() == nameNewFolder.ToUpper()).ToList()[0];
                    folder.Open(FolderAccess.ReadWrite);
                    await folder.MoveToAsync(new UniqueId(Convert.ToUInt32(message.Uid)), folderName);
                    folder.Close();
                    break;
                }
            }
        }

        public async void IDLE()
        {
            CancellationTokenSource done;
            done = new CancellationTokenSource(new TimeSpan(0, 9, 0));
            await сlient.IdleAsync(done.Token);
        }

        public void Connection(User user)
        {
            try
            {
                сlient = new ImapClient();
                сlient.Connect(user.Imap, user.Port, true);
                сlient.Authenticate(user.Login, user.Password);
            }
            catch (Exception e)
            {
                if (e.ToString().ToUpper().Contains("INVALID CREDENTIALS"))
                    throw new Exception($"Invalid credentials: {user.Login}");
                if (e.Message.Contains("Этот хост неизвестен"))
                    throw new Exception("No Internet");

            }
        }

        public void Disconnection()
        {
            сlient.Disconnect(true);
        }
    }
}
