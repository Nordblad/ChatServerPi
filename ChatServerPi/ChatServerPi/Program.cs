﻿using System;
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
                byte[] recievedData = new byte[512];
                stream.Read(recievedData, 0, recievedData.Length);
                //Spara den nyuppkopplade användaren
                ChatUser user = new ChatUser { UserName = Encoding.Unicode.GetString(recievedData).TrimEnd('\0'), Client = client };
                clientList.Add(user);
                Console.WriteLine("*** User " + user.UserName + " joined. ***");
                Broadcast("", user.UserName + " joined the chat.");
                user.StartListening();
            }
            //Console.ReadKey();
        }

        internal static void Broadcast (string userName, string message)
        {
            byte[] responseMessage = Encoding.Unicode.GetBytes(userName + "$" + message);
            foreach (ChatUser client in clientList)
            {
                try
                {
                    NetworkStream stream = client.Client.GetStream();
                    stream.Write(responseMessage, 0, responseMessage.Length);
                }
                catch
                {
                    Console.WriteLine("ERROR VID BROADCAST! " + client.UserName + ", " + clientList.Count);
                }

            }
        }

        internal static void DisconnectUser (ChatUser user)
        {
            Console.WriteLine("*** Client " + user.UserName + " disconnected. ***");
            user.Client.Close();
            clientList.Remove(user);
            Broadcast("", user.UserName + " has left the chat.");
            //user.ListenThread.Abort();
            user = null;
        }
    }
    class ChatUser
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
                int messageSize = stream.Read(messageBuffer, 0, messageBuffer.Length);

                if (messageSize <= 0)
                {                    
                    //stream.Close();
                    Program.DisconnectUser(this);
                    break;
                }
                string msg = Encoding.Unicode.GetString(messageBuffer, 0, messageBuffer.Length).TrimEnd('\0');
                Console.WriteLine(UserName + " wrote: " + msg);
                Program.Broadcast(UserName, msg);
            }
        }
    }
}
