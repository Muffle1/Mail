using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailLibrary.Model
{
    /// <summary>
    /// Класс папок
    /// </summary>
    public class Folder
    {
        /// <summary>
        /// Название папки
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Id папки
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Кому папка пренадлежит
        /// </summary>
        public int UserId { get; set; }

        public User User { get; set; }

        /// <summary>
        /// Конструктор для БД.
        /// </summary>
        public Folder()
        {

        }

        /// <summary>
        /// Конструктор с параметрами
        /// </summary>
        /// <param name="name"> Название папки</param>
        /// <param name="usersName">К кому пренадлежит папка</param>
        public Folder(string name, int userId)
        {
            #region  Валидация данных
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException("Имя папки не может быть пустым", nameof(name));

            if (userId < 0)
                throw new Exception("UsersName не может быть пустым");
            #endregion
            
            Name = name;
            UserId = userId;
        }
    }
}
