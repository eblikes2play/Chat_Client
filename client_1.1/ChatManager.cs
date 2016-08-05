using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using ChatProtocolController;
using ConnectionManager;
using System.Text;
using System.Threading;

namespace ChatManager
{
    public static class Chat
    {
        static string message = String.Empty;
        static bool isChatOn=false;

        public static void StartChat(int roomNumber)
        {

            isChatOn = true;

            int chatTimer = 0;
            string message = String.Empty;
            //KeyInputController keyInputControl = new KeyInputController(input);
            KeyInputController.command = new StringBuilder("");
            KeyInputController.StartReading();
            ReceiveMessageController rcvMessageControl = new ReceiveMessageController();

           

            while (isChatOn)
            {
               
                //Set timer for redraw screen
                if (chatTimer <= 0)
                {

                    chatTimer = 1000;            //Get update every 1 sec
                }
                else
                {
                    chatTimer -= 200;
                }

                rcvMessageControl.StartRcvThread();
                //receiveListen
                //Logic for redraw:
                //If input was made OR chat timer cycled => redraw screen
                if (KeyInputController.wasCharWritten == true || chatTimer == 1000)
                {
                    message = KeyInputController.inputString;

                    if (KeyInputController.wasStringSended)
                    {
                        message = KeyInputController.inputString;
                        SendMessage();
                        //rcvMessageControl.KeepMessage(ReceiveMessage(s));
                        KeyInputController.wasStringSended = false;
                    } 

                    //Need to lock?
                    KeyInputController.wasCharWritten = false;

                    //Otherwise Redraw
                    Console.Clear();
                    //UI Top
                    Console.WriteLine("==========================================================");
                    Console.WriteLine("================Welcome to the POCKETCHAT!!===============");
                    Console.WriteLine("================" + "Room #" + roomNumber + "===============");
                    Console.WriteLine("==========================================================");

                    rcvMessageControl.PrintMessages();

                    Console.WriteLine("==========================================================");
                    Console.WriteLine("Enter 'LEAVE' to leave room.");
                    Console.WriteLine();
                    Console.Write("> " + KeyInputController.command.ToString());
                }

                Thread.Sleep(100);

                if (KeyInputController.wasStopChatted)
                {
                    
                    isChatOn = false;
                }

                 //Time to check for new input
            }
        }


        public class ReceiveMessageController
        {
            List<string> rcvList = new List<string>(10);

            public ReceiveMessageController()
            {
                for (int i = 0; i < 10; i++)
                {
                    rcvList.Add("  ");
                }
            }

            public void StartRcvThread()
            {
                Thread rcvThread = new Thread(KeepMessage);
                rcvThread.Start();
            }
            public void KeepMessage()
            {
                while (isChatOn)
                {
                   
                    if (rcvList.Count >= 10)
                    {
                        rcvList.RemoveAt(0);
                    }
                    string rcvMessage = ReceiveMessage();
                    rcvList.Add(rcvMessage); 
                }
            }

            public void PrintMessages()
            {
                foreach (string element in rcvList)
                {
                    Console.WriteLine(element);
                }
            }
        }

        public static class KeyInputController
        {
            public static StringBuilder command;
            public static string inputString = String.Empty;
            public static volatile bool wasCharWritten;
            public static volatile bool wasStopChatted;
            public static volatile bool wasStringSended;
            public static Thread keyInputReaderThread;

            //Initialization of a new thread for the connection
            public static void StartReading( )
            {
                //Create a new thread to handle the connection
                keyInputReaderThread = new Thread(BuildInput);
                //Start!
                keyInputReaderThread.Start();
            }

            public static void BuildInput()
            {
                ConsoleKeyInfo holdKey = new ConsoleKeyInfo();

                //command = null;

  
                do
                {
                    inputString = command.ToString();
                    command.Clear(); //When the command wasn't a 'stop', clear the request and continue collecting the new input

                    do
                    {
                        holdKey = Console.ReadKey();
                        switch (holdKey.KeyChar)
                        {
                            case '\r':
                                break;
                            case '\b':
                                if (command.Length > 0)
                                {
                                    command.Remove(command.Length - 1, 1);
                                }
                                break; 
                            default:
                                command.Append(holdKey.KeyChar);
                                break;
                        }
                        wasCharWritten = true;
                    } while (holdKey.KeyChar != '\r');

                    if (!command.ToString().ToUpper().Equals("LEAVE"))
                    {
                        wasStringSended = true;
                    }
                                                                                    //On a return key(=enter), stop the loop to check for a "LEAVE" command
                } while (!command.ToString().ToUpper().Equals("LEAVE"));    
                
                //Run this build input until a stop command is received
                
                wasStopChatted = true;//Need to lock
                byte[] leaveChat = PacketMaker.CreatePacket(PacketMaker.CommandCode.LEAVE_ROOM, 0, Connection.StringToByte("0"));
                Connection.ClientToServer(Connection.cliSocket, leaveChat);
                ChatProtocol leaveResult = Connection.ServerToClient(Connection.cliSocket);

               

                //signal that a stop command has been fired
                
            }
        }


        public static void SendMessage()
        {
            string inputMessage = String.Empty;
            ushort messageStringSize = 0;
            byte[] msgArray = new byte[1024];
            //create Packet
            inputMessage = KeyInputController.inputString;
            Array.Copy(Encoding.UTF8.GetBytes(inputMessage), msgArray, Encoding.UTF8.GetBytes(inputMessage).Length);
            messageStringSize = Convert.ToUInt16(Encoding.UTF8.GetBytes(inputMessage).Length);
            byte[] sendMessagePacket = PacketMaker.CreatePacket(PacketMaker.CommandCode.MESSAGE_TO_SERVER, messageStringSize, msgArray);

            //send Packet to server
            Connection.ClientToServer(Connection.cliSocket, sendMessagePacket);
        }

        public static string ReceiveMessage()
        {
            ushort messageLength;
            byte[] rcvPacket;
            string rcvMessage = String.Empty;
            ChatProtocol rcvCP;

            rcvCP = Connection.ServerToClient(Connection.cliSocket);

            //handling rcv message
            if (rcvCP.command == PacketMaker.CommandCode.MESSAGE_TO_CLIENT)
            {
                messageLength = rcvCP.fixedLengthField;
                rcvPacket = rcvCP.variableLengthField;
                rcvMessage = Encoding.UTF8.GetString(rcvPacket, 0, messageLength); 

                if (rcvMessage.Length != (int)messageLength)
                {
                    Console.WriteLine("Receive Message Size Error!");
                }
                //Console.WriteLine("Rcv:" + rcvMessage);
                return rcvMessage;

            }
            else
            {
                return ("Receive Error!");
                //handling send error message
            }
        }

    }//class Chat end
}