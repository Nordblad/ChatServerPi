using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace ChatServerPi
{
    class Program
    {
        private static List<ChatUser> clientList = new List<ChatUser>();

        static void Main(string[] args)
        {
            TcpListener listener = new TcpListener(IPAddress.Any, 5000);
            listener.Start();
            Console.WriteLine("Server up!");

            while (true)
            {
                //Acceptera nya klienter
                TcpClient client = listener.AcceptTcpClient(); //Kommer fastna där, står och väntar.
                NetworkStream stream = client.GetStream();
                //Få användarnamn
                byte[] recievedData = new byte[256];
                stream.Read(recievedData, 0, recievedData.Length);
                //Spara den nyuppkopplade användaren
                ChatUser user = new ChatUser { UserName = Encoding.Unicode.GetString(recievedData).TrimEnd('\0'), Client = client };
                clientList.Add(user);
                Console.WriteLine("User " + user.UserName + " joined.");
                Broadcast("", user.UserName + " joined the chat.");
                user.StartListening();
            }
            Console.ReadKey();
        }

        internal static void Broadcast (string userName, string message)
        {
            foreach (ChatUser client in clientList)
            {
                NetworkStream stream = client.Client.GetStream();
                byte[] responseMessage = Encoding.Unicode.GetBytes(userName + "$" + message);
                stream.Write(responseMessage, 0, responseMessage.Length);
            }
        }

        internal static void DisconnectUser (ChatUser user)
        {
            Console.WriteLine("Client " + user.UserName + " disconnected.");
            user.Client.Close();
            clientList.Remove(user);
            user = null;
        }
    }
}
