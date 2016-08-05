using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.IO;
using ChatProtocolController;



namespace ConnectionManager
{
    public static class Connection
    {
        public static Socket cliSocket;
        public static void FirstConnect()
        {
            string initIP = SelectServer();
            cliSocket =  ConnectToServer(initIP); 
        }
        
        public static Socket ConnectToServer(string serverIP)
        {

            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Console.WriteLine("Create Socket");

            IPAddress serip = IPAddress.Parse(serverIP);
            IPEndPoint serep = new IPEndPoint(serip, 50001);

            try
            {
                sock.Connect(serep);
            }
            catch (SocketException se)
            {
                Console.WriteLine(se.HResult +" : " + se.Message);
                sock.Close();
            }catch(ObjectDisposedException de)
            {
                Console.WriteLine(de.HResult + " : " + de.Message);
                Console.WriteLine("Connection already closed");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.HResult + " : " + e.Message);
                sock.Close();
            }
            
            Console.WriteLine("Connect to Server : " + sock.RemoteEndPoint);

            return sock;
        }

        public static string SelectServer()
        {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "config.txt");
                string[] lines = System.IO.File.ReadAllLines(path);
                Random rand = new Random();

                return lines[rand.Next(lines.Length)]; 
        }

        public static void ClientToServer(Socket s, byte[] data)
        {
            
            s.Send(data, 0, data.Length, SocketFlags.None);
          
        }


        public static ChatProtocol ServerToClient(Socket s)
        {
            ChatProtocol pt;
            byte[] data = new byte[Marshal.SizeOf(typeof(ChatProtocol))];

            s.Receive(data, data.Length, SocketFlags.None);
          

            if (!PacketMaker.TryDePacket(data, out pt))
            {
                Console.WriteLine("DePacket Error!");
            }
         
            return pt;
        }

        public static byte[] StringToByte(string str)
        {
            byte[] message = new byte[1024];    //max message size
            Array.Copy(Encoding.UTF8.GetBytes(str), message, Encoding.UTF8.GetBytes(str).Length);
            return message;
        }

    }
}
