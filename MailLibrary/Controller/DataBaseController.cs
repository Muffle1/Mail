using MailLibrary.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using System.Data;
using System.Data.SQLite;
using Dapper;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace MailLibrary.Controller
{
    /// <summary>
    /// Класс для работы с базой данных
    /// </summary>
    public class DataBaseController
    {
        #region Методы записи в БД.
        /// <summary>
        /// Запись пользователя в бд.
        /// </summary>
        /// <param name="user"> Пользователь который будет записан</param>
        /// <returns>Возвращает 1 если юзер был записан и -1 если юзер уже был записан там</returns>
        public static int SaveUser(User user)
        {
            if (!CheckExists("user", "login", user.Login))
            {
                using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
                {
                    cnn.Execute($"insert into user (login, password, imap, port) values (@login, @password, @imap, @port)", user);
                }
                CreateTableUsersMessages("user" + GetMaxId("user").ToString());
                return 1;
            }
            else
                return -1;
        }

        /// <summary>
        /// Запись сообщения в бд.
        /// </summary>
        /// <param name="NameTable"> Имя таблицы в которую записать</param>
        /// <param name="message"> Сообщение которое записать</param>
        /// <returns></returns>
        public static async Task SaveMessage(int id, Message message)
        {
            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                await cnn.ExecuteAsync($"insert into user{id} (Body, Date, Subject, FromName, Folder) values (@Body, @Date,@Subject, @FromName, @Folder)", message);
            }
        }

        /// <summary>
        ///Запись папок в БД.
        /// </summary>
        /// <param name="folders">Массив папок</param>
        public static async Task SaveFolders(ObservableCollection<Folder> folders)
        {
            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                await cnn.ExecuteAsync("insert into UserFolders (UserLogin, Name) values (@UserLogin, @Name)", folders);
            }
        }
        #endregion

        #region Методы чтения из БД.
        /// <summary>
        /// Загрузка сообщений из бд.
        /// </summary>
        public static async Task<ObservableCollection<Message>> LoadMessage(string NameTable, string folder)
        {
            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                var output = await cnn.QueryAsync<Message>($"select * from {NameTable} where Folder = '{folder}'", new DynamicParameters());
                return new ObservableCollection<Message>(output.ToList());
            }
        }

        /// <summary>
        /// Получение папок из БД.
        /// </summary>
        /// <param name="value">Имя пользователя чьи папки надо загрузить</param>
        /// <returns></returns>
        public static async Task<ObservableCollection<Folder>> LoadFolders(int value)
        {
            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                var output = await cnn.QueryAsync<Folder>($"select * from UserFolders where UserLogin ={value}", new DynamicParameters());
                return new ObservableCollection<Folder>(output.ToList());
            }
        }
        /// <summary>
        /// Полечение пользователей из БД.
        /// </summary>
        /// <returns></returns>
        public static ObservableCollection<User> LoadUsers()
        {
            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                var output = cnn.Query<User>("select * from user", new DynamicParameters());
                return new ObservableCollection<User>(output.ToList());
            }
        }
        #endregion

        /// <summary>
        /// Удаление записи из бд.
        /// </summary>
        /// <param name="NameTable">Имя таблицы из которой удалить</param>
        /// <param name="id">Id строки которые удалить</param>
        private static void DeleteRecord(string NameTable, int id)
        {
            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                //TODO: сделать удаление сообщение в базе данных
            }
        }

        /// <summary>
        /// Проверяет сущуствет ли запись с данным значением в данной таблице
        /// </summary>
        /// <param name="NameTable">Название таблицы</param>
        /// <param name="columnName">Название колонки где сравнимать значение</param>
        /// <param name="value">Значение которое проверяется записано ли</param>
        /// <returns>Возвращает true если есть такая запись иначе возвращает false</returns>
        public static bool CheckExists(string NameTable, string columnName, string value)
        {
            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                var count = cnn.ExecuteScalar($"select count(*) from {NameTable} where {columnName}='{value}'");
                if (Convert.ToInt32(count) > 0)
                    return true;
                else
                    return false;
            }
        }

        /// <summary>
        /// Получает маскимальный id
        /// </summary>
        /// <param name="NameTable">Таблица из которой получить макс ID</param>
        /// <returns>Возвращает максимальный id</returns>
        private static int GetMaxId(string NameTable)
        {
            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                var id = cnn.ExecuteScalar($"select max(id) from {NameTable}");
                return Convert.ToInt32(id);
            }
        }

        /// <summary>
        /// Создает таблицу для писем данного пользователя.
        /// </summary>
        /// <param name="NameTable">Название таблицы</param>
        private static void CreateTableUsersMessages(string NameTable)
        {
            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                cnn.Execute($"create table if not exists [{NameTable}] ([Id] INTEGER PRIMARY KEY,[Body] TEXT, [Date] TEXT, [Subject] TEXT, [FromName] TEXT, [Folder] TEXT)");
            }
        }



        /// <summary>
        /// Загружает строку подлючение к бд.
        /// </summary>
        /// <param name="id">ID строки подключения</param>
        /// <returns>Возвращает строку подключения</returns>
        private static string LoadConnectionString(string id = "DefaultConnection")
        {
            return ConfigurationManager.ConnectionStrings[id].ConnectionString;
        }
    }
}
