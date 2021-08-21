using MailKit;
using MailLibrary.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MailLibrary
{
    interface IServerClient
    {
        void Connection(User user);

        void Disconnection();

        List<Message> GetMessages(string nameFolder, User user, Folder myFolder, CancellationToken token, int messageId);

        ObservableCollection<IMailFolder> GetFolders();
    }
}
