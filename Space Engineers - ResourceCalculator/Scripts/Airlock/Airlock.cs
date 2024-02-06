using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using VRageMath;
using System;
using static VRage.Profiler.MyProfilerBlock;
using System.Linq;
using System.Text;

namespace IngameScript.Scripts.Airlock
{
    internal class Program : MyGridProgram
    {
        enum Status
        {
            Depressurized,
            Pressurized,
            Pressurizing,
            Depressurizing
        }

        const string DOOROUTPREFIX  =  "[airdoorout]";
        const string DOORINPREFIX   =  "[airdoorin]";
        const string AIRVENTPREFIX  =  "[airvent]";
        const string LCDPREFIX      =  "[airlcd]";

        string Progression
        {
            get
            {
                if (airvent == null) return ProgressionBarAnimation();
                progression = MathHelper.RoundToInt((airvent.GetOxygenLevel() * 10));
                return ProgressionBarAnimation();
            }
        }

        string[] progressionDial = { "[|]", "[/]", "[-]", "[\\]" };
        int index = 0;

        int progression = 0;
        int maxProgression = 10;
        bool reverse = false;

        IMyTextSurface lcd;
        IMyAirVent airvent;
        IMyDoor doorOut;
        IMyDoor doorIn;

        Status currentStatus = Status.Depressurized;


        public Program()
        {
            Echo("Starting Airlock Procedures....");

            Runtime.UpdateFrequency = UpdateFrequency.Update10;

            List<IMyTextSurface> lcdBlocks = new List<IMyTextSurface>();
            List<IMyAirVent> airventBlocks = new List<IMyAirVent>();
            List<IMyDoor> doorBlocks = new List<IMyDoor>();
            
            GridTerminalSystem.GetBlocksOfType(lcdBlocks);
            GridTerminalSystem.GetBlocksOfType(airventBlocks);
            GridTerminalSystem.GetBlocksOfType(doorBlocks);

            lcd = lcdBlocks.FirstOrDefault(n => (n as IMyTerminalBlock).CustomName.Contains(LCDPREFIX));
            airvent = airventBlocks.FirstOrDefault(n => n.CustomName.Contains(AIRVENTPREFIX));
            doorOut = doorBlocks.FirstOrDefault(n => n.CustomName.Contains(DOOROUTPREFIX));
            doorIn = doorBlocks.FirstOrDefault(n => n.CustomName.Contains(DOORINPREFIX));

            if (airvent.GetOxygenLevel() > 0)
            {
                currentStatus = Status.Pressurized;
                doorOut.Enabled = false;
            }
            else
            {
                currentStatus = Status.Depressurized;
                doorIn.Enabled = false;
            }
        }

        public void Save()
        {
            
        }

        public void Main(string argument, UpdateType updateType)
        {
            if (argument.Contains("Cycle")) CycleAirlock();

            Depressurisation();

            Repressurisation();

            AirlockDisplay();
        }

        private void CycleAirlock()
        {
            if(currentStatus == Status.Pressurized)
                currentStatus = Status.Depressurizing;
         
            else if(currentStatus == Status.Depressurized)
                currentStatus = Status.Pressurizing;
        }

        private void Repressurisation()
        {
            if (currentStatus == Status.Pressurizing)
            {
                doorOut.CloseDoor();
                if (doorOut.OpenRatio == 0)
                {
                    // start depressure
                    doorOut.Enabled = false;
                    airvent.Depressurize = false;

                    if (progression > 5)
                    {
                        currentStatus = Status.Pressurized;
                        doorIn.Enabled = true;
                        doorIn.OpenDoor();
                    }
                }
            }
        }

        private void Depressurisation()
        {
            if (currentStatus == Status.Depressurizing)
            {
                doorIn.CloseDoor();
                if (doorIn.OpenRatio == 0)
                {
                    // start depressure
                    doorIn.Enabled = false;
                    airvent.Depressurize = true;
                    if (progression == 0)
                    {
                        currentStatus = Status.Depressurized;
                        doorOut.Enabled = true;
                        doorOut.OpenDoor();
                    }
                }
            }
        }

        private void AirlockDisplay()
        {
            string str = "Airlock One\n\n";
            str += $"Status: {currentStatus}\n\n";

            if (currentStatus == Status.Pressurizing || currentStatus == Status.Depressurizing)
                str += ProgressDialAnimation() + "\n\n";

            str += "Door Out: Closed\n\n";
            str += "Door In: Opened\n\n";
            str += $"Pressure Level: {Progression}";
            lcd?.WriteText(str);
        }

        private string ProgressionBarAnimation()
        {
            StringBuilder bar = new StringBuilder();
            string progressionBar = "[";
            bar.Append('=', progression);
            bar.Append('-', (maxProgression - progression));
            progressionBar += (bar.ToString() + "]");

            //if (!reverse) progression++;
            //else progression--;

            //if (progression > maxProgression)
            //{
            //    progression = maxProgression;
            //    reverse = true;
            //}
            //else if (progression < 0)
            //{
            //    progression = 0;
            //    reverse = false;
            //}
            return progressionBar;
        }
        string ProgressDialAnimation()
        {
            string str = progressionDial[index];
            index++;
            if (index > progressionDial.Length - 1) index = 0;
            return str;
        }
    }
}
