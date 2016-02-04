using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ChatServerPi
{
    class Program
    {
        private static List<ChatClient> clientList = new List<ChatClient>();
        private static string serverName = null;

        static void Main(string[] args)
        {
            serverName = args.Length > 0 ? args[0] : "Default";
            TcpListener listener = new TcpListener(IPAddress.Any, 5000);
            listener.Start();
            Console.WriteLine("Server '" + serverName +  "' up and running!");

            while (true)
            {
                //Acceptera nya klienter
                TcpClient client = listener.AcceptTcpClient(); //Kommer fastna där, står och väntar.
                NetworkStream stream = client.GetStream();
                //Få användarnamn
                byte[] recievedData = new byte[512];
                stream.Read(recievedData, 0, recievedData.Length);
                //Spara den nyuppkopplade användaren
                ChatClient user = new ChatClient { UserName = Encoding.Unicode.GetString(recievedData).TrimEnd('\0'), Client = client };
                clientList.Add(user);
                Console.WriteLine("*** User " + user.UserName + " joined. ***");
                SendMessage(user.Client, "", "Welcome to " + serverName + "! Online users: " + (clientList.Count-1) + ".");
                Broadcast("", user.UserName + " joined the chat.");
                user.StartListening();
            }
            //Console.ReadKey();
        }

        internal static void Broadcast (string userName, string message)
        {
            //byte[] responseMessage = Encoding.Unicode.GetBytes(userName + "$" + message);
            ////FELSÖKNINGFELSÖKNINGFELSÖKNING
            //Console.WriteLine("Broadcasting to users: " + clientList.Count);
            //foreach (ChatUser client in clientList)
            //{
            //    try
            //    {
            //        NetworkStream stream = client.Client.GetStream();
            //        stream.Write(responseMessage, 0, responseMessage.Length);
            //    }
            //    catch
            //    {
            //        Console.WriteLine("ERROR VID BROADCAST! " + client.UserName + ", " + clientList.Count);
            //        Console.WriteLine("Message:[" + message + "], user: [" + userName + "]");
            //    }
            //}
            foreach (ChatClient user in clientList)
            {
                SendMessage(user.Client, userName, message);
            }
        }

        internal static void SendMessage (TcpClient user, string userName, string message)
        {
            if (user == null || !user.Connected)
            {
                Console.WriteLine("TRIED SENDING TO OFFLINE CLIENT!!");
                return;
            }
            try
            {
                byte[] byteMessage = Encoding.Unicode.GetBytes(userName + "$" + message);
                NetworkStream stream = user.GetStream();
                stream.Write(byteMessage, 0, byteMessage.Length);
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR IN SENDING! " + e.Message);
            }
        }

        internal static void DisconnectUser (ChatClient user)
        {
            Console.WriteLine("*** Client " + user.UserName + " disconnected. ***");
            user.Client.Close();
            clientList.Remove(user);
            Broadcast("", user.UserName + " left the chat.");

            //PLACERA OM!
            //user = null;
        }
    }
    class ChatClient
    {
        public string UserName { get; set; }
        public TcpClient Client { get; set; }

        public void StartListening()
        {
            Thread ListenThread = new Thread(new ThreadStart(ListenToClient));
            ListenThread.Start();
        }

        private void ListenToClient()
        {
            NetworkStream stream = Client.GetStream();

            while (true)
            {
                byte[] messageBuffer = new byte[512];
                try
                {
                    int messageSize = stream.Read(messageBuffer, 0, messageBuffer.Length);
                    Console.WriteLine("SIZE: " + messageSize);
                    if (messageSize <= 0)
                    {
                        stream.Close();
                        Program.DisconnectUser(this); //KAN INTE TAS BORT MEDANS DEN KÖRS FRÅGETECKEN?
                        break;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("ERROR 1: " + e.Message);
                    stream.Close();
                    Program.DisconnectUser(this); //KAN INTE TAS BORT MEDANS DEN KÖRS FRÅGETECKEN?
                    break;
                }
                string msg = Encoding.Unicode.GetString(messageBuffer, 0, messageBuffer.Length).TrimEnd('\0');
                Console.WriteLine(UserName + " wrote: " + msg);
                Program.Broadcast(UserName, msg);
            }
        }
    }
}
