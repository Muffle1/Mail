using MailKit;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace MailLibrary.Model
{
    /// <summary>
    /// Пользователь.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Логин пользователя.
        /// </summary>
        public string Login { get; set; }

        /// <summary>
        /// Пароль пользователя.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Хост пользователя.
        /// </summary>
        public string Imap { get; set; }

        /// <summary>
        /// Порт пользователя.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Id пользователя.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Создание нового Пользователя.
        /// </summary>
        /// <param name="login">Логин пользователя.</param>
        /// <param name="password">Пароль пользователя.</param>
        /// <param name="imap">Хост пользователя.</param>
        /// <param name="port">Порт пользователя.</param>
        /// <param name="id">Уникальный идентификатор пользователя.</param>
        public User(string login, string password, string imap, int port)
        {
            #region Валидация данных
            if (string.IsNullOrWhiteSpace(login))
                throw new ArgumentNullException("Логин не может быть пустым или NULL.", nameof(login));

            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentNullException("Пароль не может быть пустым или NULL.", nameof(password));

            if (string.IsNullOrWhiteSpace(imap))
                throw new ArgumentNullException("Хост не может быть пустым или NULL.", nameof(imap));

            if ((port != 993) && (port != 143))
                throw new ArgumentNullException("Указан не существующий порт.", nameof(port));
            #endregion

            Login = login;
            Password = password;
            Imap = imap;
            Port = port;
        }

        /// <summary>
        /// Конструктор для БД.
        /// </summary>
        public User()
        {

        }
    }
}
