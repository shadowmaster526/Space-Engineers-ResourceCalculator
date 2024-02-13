using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using VRageMath;
using System;

namespace IngameScript.Scripts.AdvancedSorter
{
    internal class Program : MyGridProgram
    {
        // Will contain the main methods
        class LCDContainer
        {
            const string PROGBLOCK = "MyObjectBuilder_MyProgrammableBlock";
            const string CRYOCHAMBER = "MyObjectBuilder_CryoChamber";
            const string PROJECTOR = "MyObjectBuilder_Projector";
            const string COCKPIT = "MyObjectBuilder_Cockpit";

            public IMyTextSurface LCDTextSurface => lcd as IMyTextSurface;

            public IMyTextSurface LCDTextPanel
            {
                get
                {
                    string blockType = lcd.BlockDefinition.TypeIdString;
                    IMyTextSurface temp = null;
                    string type = CRYOCHAMBER;

                    if (blockType.Contains(type))
                    {
                        IMyCryoChamber cryo = lcd as IMyCryoChamber;
                        if (cryo != null)
                        {
                            temp = (lcd as IMyCryoChamber).GetSurface(0);
                        }
                    }

                    return temp;
                }
            }

            private readonly IMyTerminalBlock lcd;
            private readonly MyGridProgram mgp;

            public LCDContainer(IMyTerminalBlock lcd, MyGridProgram mgp)
            {
                this.lcd = lcd;
                this.mgp = mgp;
            }

            internal void WriteText(string _text)
            {
                LCDTextSurface?.WriteText(_text);
                LCDTextPanel?.WriteText(_text);
            }

            internal string GetBlockName() => lcd?.CustomName;
        }

        const string LCDPREFIX = "[cLCD]";

        readonly List<LCDContainer> LCDs = new List<LCDContainer>();

        public Program()
        {
            Echo("Starting up.....");

            try
            {
                Runtime.UpdateFrequency = UpdateFrequency.Update10;
                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                GridTerminalSystem?.GetBlocks(blocks);

                LCDContainer newLCD;
                for (int i = 0; i < blocks.Count; i++)
                {
                    if (blocks[i].CustomName.Contains(LCDPREFIX))
                    {
                        newLCD = new LCDContainer(blocks[i], this);
                        LCDs?.Add(newLCD);
                    }
                }
            }
            catch (Exception ex) { Echo($"Exception {ex.Message}"); }
        }

        public void Save()
        {

        }

        public void Main(string argument, UpdateType updateType)
        {
            string str = "";
            for (int i = 0; i < LCDs.Count; i++)
            {
                LCDContainer lcd = LCDs[i];
                string temp = lcd.GetBlockName() + '\n';
                string lcdSStatus = lcd.LCDTextSurface == null ? "missing" : "good";
                string lcdPStatus = lcd.LCDTextPanel == null ? "missing" : "good";
                temp += $"TextSurface Status: {lcdSStatus}\nTextPanel Status: {lcdPStatus}\n\n";
                str += temp;
            }

            for (int i = 0; i < LCDs.Count; i++)
            {
                LCDs[i].WriteText(str);
            }
        }
    }
}
