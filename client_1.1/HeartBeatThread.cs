using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ChatProtocolController;
using ConnectionManager;

namespace HeartBeat
{
     class HeartBeatThread
    {
        public HeartBeatThread()
        {

        }
        public  void StartListen()
        {
            ThreadStart threadStart = GetHeartBeat;
            Thread heartbeatThread = new Thread(GetHeartBeat);
            heartbeatThread.Start();
        }

        public void GetHeartBeat()
        {

            while (true) {

                ChatProtocol heartBeatPacket = Connection.ServerToClient(Connection.cliSocket);
                if (heartBeatPacket.command == 60)
                {
                    Console.Write("gotcha hb!");
                    byte[] heartBeatResponse = PacketMaker.CreatePacket(PacketMaker.CommandCode.HEARTBEAT_RESULT, 0, Connection.StringToByte("0"));
                    Connection.ClientToServer(Connection.cliSocket, heartBeatResponse);
                    Console.Write("response to hb");
                }
            }
        }
        
        
    }
}
