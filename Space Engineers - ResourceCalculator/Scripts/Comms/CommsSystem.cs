using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using VRageMath;
using System;
using System.Linq;
using System.Text;


namespace IngameScript.Scripts.Comms
{
    internal class Program : MyGridProgram
    {
        const string LCDPREFFIX = "[COMMS]";

        IMyTextSurface lcdPanel = null;

        int runCount = 0;
        string broadcastTag = "MDK IGC EXAMPLE 1";
        IMyBroadcastListener myBroadcastListener;

        public Program()
        {
            Echo("Broadcasting....");
            myBroadcastListener = IGC.RegisterBroadcastListener(broadcastTag);
            myBroadcastListener.SetMessageCallback(broadcastTag);

            List<IMyTextSurface> lcds = new List<IMyTextSurface>();
            GridTerminalSystem.GetBlocksOfType(lcds);

            for (int i = 0; i < lcds.Count; i++)
            {
                if ((lcds[i] as IMyTerminalBlock).CustomName.Contains(LCDPREFFIX))
                {
                    lcdPanel = lcds[i];
                    break;
                }
            }
        }

        public void Save()
        {
            // Called when the program needs to save its state. Use
            // this method to save your state to the Storage field
            // or some other means. 
            // 
            // This method is optional and can be removed if not
            // needed.
        }

        public void Main(string argument, UpdateType updateSource)
        {
            runCount++;
            Echo(runCount.ToString() + ":" + updateSource.ToString());

            if ((updateSource & (UpdateType.Trigger | UpdateType.Terminal)) > 0
                || (updateSource & (UpdateType.Mod)) > 0
                || (updateSource & (UpdateType.Script)) > 0)
            {
                if (argument != "")
                {
                    IGC.SendBroadcastMessage(broadcastTag, argument);
                    Echo("Sending message:\n" + argument);
                }
            }

            if ((updateSource & UpdateType.IGC) > 0)
            {
                while (myBroadcastListener.HasPendingMessage)
                {
                    MyIGCMessage myIGCMessage = myBroadcastListener.AcceptMessage();
                    if (myIGCMessage.Tag == broadcastTag)
                    {
                        if (myIGCMessage.Data is string)
                        {
                            string str = myIGCMessage.Data.ToString();
                            str += "\n\nReceived IGC Public Message";
                            str += "\nTag=" + myIGCMessage.Tag;
                            str += "\nData=" + myIGCMessage.Data.ToString();
                            str += "\nSource=" + myIGCMessage.Source.ToString("X");
                            lcdPanel.WriteText(str);
                        }
                        else // if(msg.Data is XXX)
                        {
                        }
                    }
                    else
                    {
                    }
                }
            }
        }
    }
}
