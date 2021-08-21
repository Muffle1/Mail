using System;
using MailLibrary.Controller;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using Hardcodet.Wpf.TaskbarNotification;
using MailLibrary.Model;
using MailKit.Security;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Globalization;

namespace Mail_
{
    public delegate void ShowNotify(string userName);
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public event ShowNotify showNotify;
        private UserController userController;
        private SendMessageWindow SendMessageWindow = new SendMessageWindow();
        Process[] localAll = Process.GetProcesses();
        private bool hasCorrectUser = true;
        private string culture;
        /// <summary>
        /// Конструктор.
        /// </summary>
        public MainWindow()
        {
            int userNumber = 0;
            culture = CultureInfo.CurrentCulture.Name;
            ProcessCheck();
            InitializeComponent();
            while (true)
            {
                try
                {
                    userController = new UserController(userNumber);
                    break;
                }
                catch (Exception e)
                {
                    if (e.Message.Contains("Invalid credentials"))
                    {
                        ShowError(e.Message.Replace("Invalid credentials: ", ""));
                        userNumber++;
                    }
                    if (e.Message.Contains("No correct user"))
                    {
                        userController = new UserController();
                        hasCorrectUser = false;
                        break;
                    }
                }
            }
            BindingData();
            showNotify += OnShowNotify;
            Idle();
        }

        public void Idle()
        {
            foreach (var i in userController.Users)
            {
                if (!TextBlockError.Text.Contains(i.Login))
                {
                    Task.Run(() =>
                    {
                        StartIdle(i);
                    });
                }
            }
        }

        /// <summary>
        /// Уведомление о новом сообщение.
        /// </summary>
        public void OnShowNotify(string userName)
        {
            string title = "Уведомление";
            string text = $"Новое сообщение для пользователя: {userName}";
            myNotifyIcon.ShowBalloonTip(title, text, BalloonIcon.Info);
            myNotifyIcon.ShowBalloonTip(title, text, myNotifyIcon.Icon);
            myNotifyIcon.HideBalloonTip();
            Application.Current.Dispatcher.Invoke(() =>
            {
                userController.SetMessages(folderName1.Text);
            });
        }

        public void StartIdle(User user, SecureSocketOptions SslOptions = SecureSocketOptions.Auto)
        {
            using (var client1 = new IdleClient(user, SslOptions, this.showNotify))
            {
                var idleTask = client1.RunAsync();

                client1.Exit();
                idleTask.GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// Привязка данных
        /// </summary>
        private void BindingData()
        {
            Accounts.ItemsSource = userController.Users.Select(x => x.Login).ToList();
            if (hasCorrectUser)
            {
                Folders.ItemsSource = userController.Folders;
                listBoxMessages.ItemsSource = userController.Messages;
                MoveFolders.ItemsSource = userController.Folders.Select(x => x.Name);
            }
        }

        private void Folders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Folders.SelectedIndex >= 0 && folderName1.Text != userController.Folders[Folders.SelectedIndex].Name)
            {
                folderName1.Text = userController.Folders[Folders.SelectedIndex].Name;
                folderName2.Text = userController.Folders[Folders.SelectedIndex].Name;
                userController.SetMessages(folderName1.Text);
                listBoxMessages.ItemsSource = userController.Messages;
            }
        }

        private void ProcessCheck()
        {
            if (localAll.Where(i => i.ProcessName == "Mail_").Count() > 1)
            {
                Environment.Exit(0);
                myNotifyIcon.Dispose();
            }
        }


        private void Accounts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (userController.CurrentUser.Login != userController.Users[Accounts.SelectedIndex].Login)
            {
                try
                {
                    userController.SwitchUser(Accounts.SelectedIndex);
                    BindingData();
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("Invalid credentials"))
                        return;
                }
            }
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((sender as ListBox).SelectedIndex != -1) && (sender is ListBox))
            {
                ListBox listBox = sender as ListBox;
                TitleMessage.Text = userController.Messages[listBox.SelectedIndex].Subject;
                FromDisplayName.Text = userController.Messages[listBox.SelectedIndex].FromName;
                if (culture == "ru-RU")
                    DateMessage.Text = Convert.ToDateTime(userController.Messages[listBox.SelectedIndex].Date).ToString("dddd, dd MMMM, HH:mm");
                if (culture == "en-US")
                    DateMessage.Text = Convert.ToDateTime(DateTime.ParseExact(userController.Messages[listBox.SelectedIndex].Date, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture).ToString("MM/d/yyyy hh:mm:ss tt zzz")).ToString("dddd, dd MMMM, HH:mm");
                BodyMessage.NavigateToString($"<!DOCTYPE html><html><meta content='text/html;charset=UTF-8'><div>{userController.Messages[listBox.SelectedIndex].HtmlBody}</div>");
                MessageFull.Visibility = Visibility.Visible;
            }
        }

        private void ButtonSend_Click(object sender, RoutedEventArgs e)
        {
            (MessagesGrid.Children[1] as ListBox).SelectedIndex = -1;
            if (!SendMessageWindow.IsLoaded)
                SendMessageWindow = new SendMessageWindow();
            SendMessageWindow.Show();
        }


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            SendMessageWindow.Hide();
            Hide();
        }


        private void MenuItemSave_Click(object sender, RoutedEventArgs e)
        {
            Show();
        }


        private void MenuItemExit_Click(object sender, RoutedEventArgs e)
        {
            myNotifyIcon.Dispose();
            Environment.Exit(0);
        }


        private void MoveFolders_Click(object sender, RoutedEventArgs e)
        {
            if ((e.OriginalSource as MenuItem).Header.ToString() != folderName1.Text && listBoxMessages.SelectedIndex > -1)
                userController.SwitchFolder((e.OriginalSource as MenuItem).Header.ToString(), listBoxMessages.SelectedIndex);
        }

        private void ShowError(string login)
        {
            if (GridMail.RowDefinitions.Count == 1)
            {
                RowDefinition row = new RowDefinition()
                {
                    Height = new GridLength(50)
                };
                GridMail.RowDefinitions.Add(row);
                Grid.SetRow(GridError, 1);
                TextBlockError.Text += $"{login}";
            }
            else
                TextBlockError.Text += $", {login}";
            GridError.Visibility = Visibility.Visible;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            GridError.Visibility = Visibility.Hidden;
            GridMail.RowDefinitions.RemoveAt(1);
        }
    }
}
