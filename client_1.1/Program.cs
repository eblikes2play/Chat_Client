
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using ChatProtocolController;
using ConnectionManager;
using ChatManager;
using HeartBeat;
using System.Runtime.InteropServices;




namespace Client
{
    class client
    {
        static void Main(string[] args)
        {
            Console.WindowWidth = 60;


            Connection.FirstConnect();
            //connect to server
            Socket clientSocket = Connection.cliSocket;
            
            while (true)
            {
                //default state
                FirstUI();

                string command = GetCommand();
                //login state
                if (command.Equals("LOGIN"))
                {
                    //try login
                    LogIn(clientSocket);

                    bool loginResult = LogInAccept(clientSocket);

                    if (loginResult)
                    {
                        string lobbyCommand = String.Empty;


                        HeartBeatThread hb = new HeartBeatThread();
                       // hb.StartListen();
                        //login success
                        do
                        {
                            //lobby state
                            LobbyUI();

                            lobbyCommand = GetCommand();

                            switch (lobbyCommand)
                            {
                                case "LIST":
                                    GetList();
                      
                                    break;

                                case "JOIN":

                                    Console.Write("Select Room Number :");
                                    int roomNum = Convert.ToInt32(Console.ReadLine());

                                    JoinRoom(clientSocket, roomNum);
                                    
                                        break;
                                    
                                case "CREATE":

                                    int roomNumber = CreateRoom(clientSocket);

                                    if (roomNumber == -1)
                                    {
                                        Console.WriteLine("Can not create new room");
                                    }
                                    else
                                    {
                                        Console.WriteLine("Create Room " + roomNumber);
                                        JoinRoom(clientSocket, roomNumber);
                                    }

                                    break;

                                case "LOGOUT":
                                     LogOut(clientSocket);
                                    //logout success

                                    Console.WriteLine("Logout");
                                    break;

                                default:
                                    Console.WriteLine("Invalid Command : "+lobbyCommand);
                                    break;
                            }
                        } while (!lobbyCommand.Equals("LOGOUT"));
                    }
                    else
                    {
                        continue;
                    }

                }//login state
                else if (command.Equals("EXIT"))
                {
                    clientSocket.Close();
                    Environment.Exit(0);
                }//exit
                else
                {
                    Console.WriteLine("Invalid Command");
                } //Login State End
            } //Application End
        }//main ends

        public static RoomInfoDatum ByteToRoomInfoDatum(byte[] input)
        {
            RoomInfoDatum ro = new RoomInfoDatum();
            ro.roomNumber = input[0];
            byte[] temp = new byte[Marshal.SizeOf(typeof(RoomInfoDatum)) - 2];
            Array.ConstrainedCopy(input, 1, temp, 0, temp.Length);
            ro.roomTitle = Encoding.UTF8.GetString(temp);
            ro.userCount = input[input.Length - 1];

            return ro;
        }


        public static void GetList()
        {
            ChatProtocol roomListInfo;
            ushort dataLength;
            byte part = 0;
            byte numberPart = 0;
            byte[] temp = new byte[22];
            RoomInfoDatum roomInfoTemp = new RoomInfoDatum();

            do
            {
                roomListInfo = Connection.ServerToClient(Connection.cliSocket);

                if (roomListInfo.command == PacketMaker.CommandCode.ROOM_LIST_REQUEST)
                {
                    dataLength = roomListInfo.fixedLengthField;
                    byte[] roomData = roomListInfo.variableLengthField;
                    part = roomData[0];
                    numberPart = roomData[1];

                    for (int i = 2; i <= (dataLength + 2); i++)
                    {
                        if (i < (dataLength + 2))
                        {
                            temp[(i - 2) / 22] = roomData[i];
                        }
                        if ((i - 2) % 22 == 0)
                        {
                            if (i != 2)
                            {
                                roomInfoTemp = ByteToRoomInfoDatum(temp);
                                Console.Write("Room: " + roomInfoTemp.roomNumber + "\t" + roomInfoTemp.roomTitle + "\t" + roomInfoTemp.userCount + " user(s).");
                            }
                        }
                    }
                }
                else
                {
                    //error
                }

            } while (part != numberPart);
        }


       

        public static string GetCommand()
        {
            string command = Console.ReadLine().ToUpper();
            return command;
        }


        public static void FirstUI()
        {
            Console.WriteLine("============================================================");
            Console.WriteLine("=================Welcome to the POCKETCHAT!!================");
            Console.WriteLine("============================================================");
            Console.WriteLine("==================LOGIN==================EXIT===============");
        }



        public static void LogIn(Socket s)
        {
            string id = String.Empty;
            string pw = String.Empty;
            int idMaxSize = 12;

            //Get correct login info
            do
            {
                Console.Write("Enter your ID : ");
                id = Console.ReadLine();
            }
            while (!IsValidID(id, idMaxSize));

            do
            {
                Console.Write("Enter your PW : ");
                pw = Console.ReadLine();
            }
            while (!IsValidPW(pw, idMaxSize));

            //Send login info to server
            string loginInfo = id + "#" + pw;

            byte[] loginData = PacketMaker.CreatePacket(PacketMaker.CommandCode.LOGIN, Convert.ToUInt16(loginInfo.Length), Connection.StringToByte(loginInfo));

            Connection.ClientToServer(s, loginData);
        }

        public static void LogOut(Socket s)
        {
           byte[] logoutData = PacketMaker.CreatePacket(PacketMaker.CommandCode.LOGOUT, 0, Connection.StringToByte("0"));
           Connection.ClientToServer(s, logoutData);
        }

        public static bool LogInAccept(Socket s)
        {
            ChatProtocol result = Connection.ServerToClient(s);

            if (result.command == PacketMaker.CommandCode.LOGIN_RESULT)
            {
                if (BitConverter.ToInt32(result.variableLengthField, 0) == 1)
                {
                    Console.WriteLine("Welcome!");
                    return true;
                }
                else if (BitConverter.ToInt32(result.variableLengthField, 0) == -1)
                {
                    Console.WriteLine("LogIn Failed");
                    return false;
                }
                else
                {
                    Console.WriteLine(BitConverter.ToInt32(result.variableLengthField, 0));
                    Console.WriteLine("valueB error.");
                    return false;
                }
            }
            else
            {
                Console.WriteLine("login command error.");
                return false;
            }
        }

        public static bool IsValidID(string info, int idMaxSize)
        {
            if (Encoding.UTF8.GetBytes(info).Length > idMaxSize)
            {
                Console.WriteLine("길이는 최대 12byte를 넘길 수 없습니다. (영어 12자, 한글 4자)");
                return false;
            }
            else
            {
                string cleanString = Regex.Replace(info, @"[^a-zA-Z0-9가-힣]", String.Empty, RegexOptions.Singleline);

                if (info.CompareTo(cleanString) != 0)
                {
                    Console.WriteLine("특수문자 또는 공백을 포함할 수 없습니다.");
                    return false;
                }
            }
            return true;
        }


        public static bool IsValidPW(string info, int idMaxSize)
        {
            if (Encoding.UTF8.GetBytes(info).Length > idMaxSize)
            {
                Console.WriteLine("길이는 최대 12byte를 넘길 수 없습니다. (영숫자 혼합 12자)");
                return false;
            }
            else
            {
                string cleanString = Regex.Replace(info, @"[^a-zA-Z0-9]", String.Empty, RegexOptions.Singleline);

                if (info.CompareTo(cleanString) != 0)
                {
                    Console.WriteLine("한글 또는 특수문자 또는 공백을 포함할 수 없습니다.");
                    return false;
                }
            }
            return true;
        }

        public static void LobbyUI()
        {
            Console.WriteLine("============================================================");
            Console.WriteLine("===================POCKETCHAT BETA ver.1.0==================");
            Console.WriteLine("============================================================");
            Console.WriteLine("=========LIST========JOIN=======CREATE========LOGOUT========");
        }


        public static bool JoinRoom(Socket s, int roomNum)
        {
            byte[] joinRoomRequest
                = PacketMaker.CreatePacket(PacketMaker.CommandCode.JOIN_ROOM, Convert.ToUInt16(roomNum), Connection.StringToByte("0"));

            Connection.ClientToServer(s, joinRoomRequest);
            ChatProtocol joinResult = Connection.ServerToClient(s);

            if (joinResult.command == PacketMaker.CommandCode.JOIN_ROOM_RESULT)
            {
                if (BitConverter.ToInt32(joinResult.variableLengthField, 0) == 1)
                {
                    Console.WriteLine("Join to Room # : " + roomNum);
                    Chat.StartChat(roomNum);
                    return true;
                }
                else if (BitConverter.ToInt32(joinResult.variableLengthField, 0) == -1)
                {
                    Console.WriteLine("Join Room Failed");
                    return false;
                }
            }
            else
            {
                Console.WriteLine("Invalid Message from Server");
                return false;
            }
            return false;
        }


        public static int CreateRoom(Socket s)
        {
            int roomNumber = -1;
            int maxRoomNameLength = 20;
            string roomName;

            do
            {
                Console.WriteLine("Enter Room Name");
                roomName = Console.ReadLine();

                if (roomName.Length > maxRoomNameLength)
                {
                    Console.WriteLine("방 이름이 너무 길어양");
                }
            } while (roomName.Length > maxRoomNameLength);

            ushort roomNameLength = Convert.ToUInt16(roomName.Length);
           
            byte[] newRoomRequest = PacketMaker.CreatePacket(PacketMaker.CommandCode.CREATE_ROOM, roomNameLength, Connection.StringToByte(roomName));
            Connection.ClientToServer(s, newRoomRequest);
            
            Console.WriteLine(Chat.KeyInputController.wasStopChatted);
            ChatProtocol newRoom = Connection.ServerToClient(s);
            
            if (newRoom.command == PacketMaker.CommandCode.CREATE_ROOM_RESULT)
            {
                roomNumber = BitConverter.ToInt32(newRoom.variableLengthField,0);
            }
            else
            {
                Console.WriteLine("command error");
            }

            return roomNumber;
        }


       

       


      


        



    
        }
    

    
}