using MailKit.Security;
using MailLibrary.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace MailLibrary.Controller
{
    /// <summary>
    /// Логика пользователя.
    /// </summary>
    public class UserController
    {
        private ApplicationContext db;

        /// <summary>
        /// Список пользователей.
        /// </summary>
        public ObservableCollection<User> Users { get; }

        public ObservableCollection<Message> Messages { get; private set; }

        public ObservableCollection<Folder> Folders { get; private set; }

        /// <summary>
        /// Пользователей в чьем аккаунте мы сейчас.
        /// </summary>
        public User CurrentUser { get; private set; }

        /// <summary>
        /// Imap.
        /// </summary>
        private ImapController imap;

        /// <summary>
        /// Поток для получения писем.
        /// </summary>
        private Task task1;

        /// <summary>
        /// Контекст для отображения писем
        /// </summary>
        private SynchronizationContext uiContext;

        /// <summary>
        ///  CancellationTokenSource для отмены процесса.
        /// </summary>
        private static CancellationTokenSource cts;

        /// <summary>
        /// CancellationToken для отмены процесса.
        /// </summary>
        private CancellationToken token;

        private bool hasInternet = true;

        private string culture;

        public UserController()
        {
            db = new ApplicationContext();
            GetUsersFromFile();
            Users = new ObservableCollection<User>(db.Users.ToList());
            CurrentUser = Users[0];
        }

        /// <summary>
        /// Конструктор для получения пользователей.
        /// </summary>
        public UserController(int userNumber)
        {
            uiContext = SynchronizationContext.Current;
            Folders = new ObservableCollection<Folder>();
            Messages = new ObservableCollection<Message>();
            cts = new CancellationTokenSource();
            token = cts.Token;
            db = new ApplicationContext();
            GetUsersFromFile();
            Users = new ObservableCollection<User>(db.Users.ToList());
            if (userNumber > Users.Count - 1)
                throw new Exception("No correct user");
            CurrentUser = Users[userNumber];
            try
            {
                imap = new ImapController(CurrentUser);
            }
            catch (Exception e)
            {
                if (e.Message.Contains("No Internet"))
                    hasInternet = false;
                else
                    throw new Exception(e.Message);
            }
            culture = CultureInfo.CurrentCulture.Name;
            SetFolders();
            SetMessages("INBOX");
        }

        /// <summary>
        /// Смена пользователей.
        /// </summary>
        /// <param name="numberUser"></param>
        public void SwitchUser(int numberUser)
        {
            if ((task1 != null) && (!task1.IsCompleted))
            {
                cts.Cancel();
                task1.Wait();
                cts = new CancellationTokenSource();
                token = cts.Token;
            }

            imap?.Disconnection();
            CurrentUser = Users[numberUser];
            try
            {
                imap = new ImapController(CurrentUser);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
            SetFolders();
            SetMessages("INBOX");
        }

        /// <summary>
        /// Установка папок.
        /// </summary>
        private async void SetFolders()
        {
            Folders = new ObservableCollection<Folder>(db.Folders.Where(x => x.UserId == CurrentUser.Id).ToList());
            if ((Folders.Count == 0) && (hasInternet))
            {
                if (db.Folders.Where(x => x.UserId == CurrentUser.Id).ToList().Count <= 0)
                {
                    var folders = GetFolders();
                    foreach (var folder in folders)
                    {
                        db.Folders.Add(folder);
                    }
                    db.SaveChanges();
                }
                Folders = new ObservableCollection<Folder>(db.Folders.Where(x => x.UserId == CurrentUser.Id).ToList());
            }
        }

        /// <summary>
        /// Получение папок с почтового ящика
        /// </summary>
        /// <returns> Возвращает полученные папки</returns>
        private ObservableCollection<Folder> GetFolders()
        {
            ObservableCollection<Folder> folders = new ObservableCollection<Folder>();
            var nameFolders = imap.GetFolders();
            var nameFolder = nameFolders.Where(x => !x.Attributes.ToString().Contains("NonExistent")).Select(x => x.Name).ToList();
            foreach (var name in nameFolder)
                folders.Add(new Folder(name, CurrentUser.Id));
            return folders;
        }

        /// <summary>
        /// Установка сообщение
        /// </summary>
        /// <param name="nameFolder"> Имя папки откуда брать сообщения</param>
        public void SetMessages(string nameFolder)
        {
            if ((task1 != null) && (!task1.IsCompleted))
            {
                cts.Cancel();
                task1.Wait();
                cts = new CancellationTokenSource();
                token = cts.Token;
            }
            Messages.Clear();

            task1 = Task.Run(() => MessagesControl(nameFolder));
        }

        private void MessagesControl(string nameFolder)
        {
            List<Message> allMessages = new List<Message>();
            if (culture == "ru-RU")
                allMessages = db.Messages.Where(x => (x.UserId == CurrentUser.Id) && (x.FolderId == Folders.Where(y => y.Name == nameFolder).ToList()[0].Id)).OrderByDescending(x => Convert.ToDateTime(x.Date)).ToList();
            if (culture == "en-US")
                allMessages = db.Messages.Where(x => (x.UserId == CurrentUser.Id) && (x.FolderId == Folders.Where(y => y.Name == nameFolder).ToList()[0].Id)).OrderByDescending(x => Convert.ToDateTime(DateTime.ParseExact(x.Date, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture).ToString("MM/d/yyyy hh:mm:ss tt zzz"))).ToList();

            foreach (var item in allMessages)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }
                uiContext.Send(x => Messages.Add(item), null);
            }
            if (hasInternet)
            {
                int countMessages = allMessages.Count();
                if (countMessages > 0)
                {
                    GetNewMessages(nameFolder, token, allMessages);
                    if (culture == "ru-RU")
                        allMessages = db.Messages.Where(x => (x.UserId == CurrentUser.Id) && (x.FolderId == Folders.Where(y => y.Name == nameFolder).ToList()[0].Id)).OrderByDescending(x => Convert.ToDateTime(x.Date)).ToList();
                    if (culture == "en-US")
                        allMessages = db.Messages.Where(x => (x.UserId == CurrentUser.Id) && (x.FolderId == Folders.Where(y => y.Name == nameFolder).ToList()[0].Id)).OrderByDescending(x => Convert.ToDateTime(DateTime.ParseExact(x.Date, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture).ToString("MM/d/yyyy hh:mm:ss tt zzz"))).ToList();
                }
                GetMessages(nameFolder, token, allMessages);
            }
        }

        private void GetNewMessages(string nameFolder, CancellationToken token, List<Message> allMessages)
        {
            int i = 0, j = allMessages.Count();
            DateTime oldMessageDate = DateTime.MinValue;
            if (culture == "ru-RU")
                oldMessageDate = Convert.ToDateTime(allMessages[0].Date);
            if (culture == "en-US")
                oldMessageDate = Convert.ToDateTime(DateTime.ParseExact(allMessages[0].Date, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture).ToString("MM/d/yyyy hh:mm:ss tt zzz"));

            while (true)
            {
                List<Message> messages = imap.GetNewMessages(nameFolder, CurrentUser, Folders.Where(y => y.Name == nameFolder).ToList()[0], token, i, oldMessageDate);
                if (messages.Count() == 0)
                    break;
                db.Messages.AddRange(messages);
                db.SaveChanges();
                if (culture == "ru-RU")
                    allMessages = db.Messages.Where(x => (x.UserId == CurrentUser.Id) && (x.FolderId == Folders.Where(y => y.Name == nameFolder).ToList()[0].Id)).OrderByDescending(x => Convert.ToDateTime(x.Date)).ToList();
                if (culture == "en-US")
                    allMessages = db.Messages.Where(x => (x.UserId == CurrentUser.Id) && (x.FolderId == Folders.Where(y => y.Name == nameFolder).ToList()[0].Id)).OrderByDescending(x => Convert.ToDateTime(DateTime.ParseExact(x.Date, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture).ToString("MM/d/yyyy hh:mm:ss tt zzz"))).ToList();

                if (token.IsCancellationRequested)
                {
                    return;
                }
                foreach (var item in messages)
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }
                    uiContext.Send(x => Messages.Insert(i, item), null);
                }
                i = allMessages.Count() - j;
            }
        }

        private void GetMessages(string nameFolder, CancellationToken token, List<Message> allMessages)
        {
            int i = allMessages.Count();
            while (true)
            {
                List<Message> messages = new List<Message>();
                messages = imap.GetMessages(nameFolder, CurrentUser, Folders.Where(y => y.Name == nameFolder).ToList()[0], token, i);
                if (messages.Count() == 0)
                    break;
                db.Messages.AddRange(messages);
                db.SaveChanges();
                if (culture == "ru-RU")
                    allMessages = db.Messages.Where(x => (x.UserId == CurrentUser.Id) && (x.FolderId == Folders.Where(y => y.Name == nameFolder).ToList()[0].Id)).OrderByDescending(x => Convert.ToDateTime(x.Date)).ToList();
                if (culture == "en-US")
                    allMessages = db.Messages.Where(x => (x.UserId == CurrentUser.Id) && (x.FolderId == Folders.Where(y => y.Name == nameFolder).ToList()[0].Id)).OrderByDescending(x => Convert.ToDateTime(DateTime.ParseExact(x.Date, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture).ToString("MM/d/yyyy hh:mm:ss tt zzz"))).ToList();
                messages = allMessages.Skip(i).Take(messages.Count()).ToList();

                foreach (var item in messages)
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }
                    uiContext.Send(x => Messages.Add(item), null);
                }
                i = allMessages.Count();
            }
        }

        public void SwitchFolder(string NameFolder, int index)
        {
            if ((task1 != null) && (!task1.IsCompleted))
            {
                cts.Cancel();
                task1.Wait();
                cts = new CancellationTokenSource();
                token = cts.Token;
            }
            task1 = Task.Run(() =>
            {
                imap.MoveToFolder(Messages[index], NameFolder);
                var message = db.Messages.Where(x => x.Id == Messages[index].Id).FirstOrDefault();
                message.FolderId = db.Folders.Where(x => x.Name == NameFolder && CurrentUser.Id == x.UserId).FirstOrDefault().Id;
                db.SaveChanges();
                uiContext.Send(x => Messages.RemoveAt(index), null);
            });
        }

        /// <summary>
        /// Получение пользователей из xml-файлов и запись их в бд.
        /// </summary>
        private void GetUsersFromFile()
        {
            XmlDocument xml = new XmlDocument();
            string path = $"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\\configuration.xml";
            xml.Load(path);
            string login = null, password = null, imap = null;
            int port = 0;
            XmlElement element = xml.DocumentElement;
            foreach (XmlNode xnode in element)
            {
                if (xnode.Name == "mailboxes")
                {
                    foreach (XmlNode mailbox in xnode.ChildNodes)
                    {
                        foreach (XmlNode childMailBox in mailbox.ChildNodes)
                        {
                            if (childMailBox.Name == "login")
                                login = childMailBox.InnerText;

                            if (childMailBox.Name == "password")
                                password = childMailBox.InnerText;

                            if (childMailBox.Name == "hostname")
                                imap = childMailBox.InnerText;

                            if ((childMailBox.Name == "port") && (int.TryParse(childMailBox.InnerText, out int x)))
                                port = int.Parse(childMailBox.InnerText);
                        }
                        if (login == null)
                            throw new ArgumentNullException("Логин в файле не заполнен!", nameof(login));

                        if (password == null)
                            throw new ArgumentNullException("Пароль в файле не заполнен!", nameof(password));

                        if (imap == null)
                            throw new ArgumentNullException("Хост в файле не заполнен!", nameof(imap));
                        if (db.Users.Where(x => x.Login == login).Count() == 0)
                            db.Users.Add(new User(login, password, imap, port));
                        if (db.Users.Where(x => x.Login == login).Count() == 1)
                        {
                            var user = db.Users.Where(x => x.Login == login).First();
                            user.Password = password;
                        }
                    }
                }

            }
            db.SaveChanges();
        }
    }
}
