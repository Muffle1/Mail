using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailLibrary.Model
{
    public class Message
    {
        /// <summary>
        /// Тело сообщения.
        /// </summary>
        public string Body { get; set; }
        

        public string HtmlBody { get; set; }

        /// <summary>
        /// От кого сообщение.
        /// </summary>
        public string FromName { get; set; }

        /// <summary>
        /// Тема сообщения.
        /// </summary>
        public string Subject { get; set; }


        /// <summary>
        /// Дата сообщения.
        /// </summary>
        public string Date { get; set; }

        /// <summary>
        /// ID сообщения.
        /// </summary>
        public int Id { get; set; }

        public string Uid { get; set; }


        public int UserId { get; set; }

        public User User { get; set; }

        public int FolderId { get; set; }

        public Folder Folder { get; set; }

        /// <summary>
        /// Создание нового сообщения.
        /// </summary>
        /// <param name="from">От кого сообщение.</param>
        /// <param name="folder">Папка сообщения.</param>
        /// <param name="date">Дата сообщения.</param>
        /// <param name="subject">От кого сообщение.</param>
        /// <param name="body">Тело сообщения.</param>
        public Message(string from, int folderId, int userId, string date, string htmlbody, string uid, string subject = " ", string body = " ")
        {
            Body = body;
            FromName = from;
            Subject = subject;
            FolderId = folderId;
            UserId = userId;
            Date = Convert.ToDateTime(date).ToString("dd.MM.yyyy HH:mm:ss");
            HtmlBody = htmlbody;
            Uid = uid;
        }

        /// <summary>
        /// Конструктор для сообщений.
        /// </summary>
        public Message()
        {

        }
    }
}
