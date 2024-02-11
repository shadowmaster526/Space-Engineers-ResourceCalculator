using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using VRageMath;
using System.Linq;
using System.Text;
using System;
using static IngameScript.Turbo.Program;
using static IngameScript.Scripts.Airlock.Program;
using System.Diagnostics;
using EmptyKeys.UserInterface.Generated.DataTemplatesStoreBlock_Bindings;
using Sandbox.Game.Debugging;
using Sandbox.Common.ObjectBuilders;

namespace IngameScript.Scripts.Airlock
{
    internal class Program : MyGridProgram
    {
        public enum Status
        {
            Depressurized,
            Pressurized,
            Pressurizing,
            Depressurizing
        }

        public class Airlock : MyGridProgram
        {
            public string airlockName = "";
            public IMyDoor doorOut; // door to get out
            public IMyDoor doorIn;  // door to get in
            public IMyAirVent airvent;
            public IMyTextSurface statusLCD;
            public bool isHangar;
            IMyDoor[] hangerDoors = null;

            public bool isOverriden = false;

            readonly string[] progressionDial = { "[|]", "[/]", "[-]", "[\\]" };

            int index = 0;

            int progression = 0;
            readonly int maxProgression = 10;

            Status currentStatus = Status.Depressurized;

            public Airlock(string airlockName, IMyDoor doorOut, IMyDoor doorIn, IMyAirVent airvent, IMyTextSurface statusLCD, IMyDoor[] hangerDoors, bool isHangar)
            {
                this.airlockName = airlockName;
                this.doorOut = doorOut;
                this.doorIn = doorIn;
                this.airvent = airvent;
                this.statusLCD = statusLCD;
                this.hangerDoors = hangerDoors;
                this.isHangar = isHangar;
                (statusLCD as IMyTerminalBlock).CustomData = CUSTOMDATA;
                CycleAirlock();
            }

            string Progression
            {
                get
                {
                    if (airvent == null) return ProgressionBarAnimation();
                    progression = MathHelper.RoundToInt((airvent.GetOxygenLevel() * 10));
                    return ProgressionBarAnimation();
                }
            }

            public bool Depressurize => currentStatus == Status.Depressurizing;

            public string GetStatus
            {
                get
                {
                    if (isOverriden) return "Door Overide";
                    return currentStatus.ToString();
                }
            }

            public void CycleAirlock()
            {
                if (!AirlockFailure())
                {
                    if (currentStatus == Status.Pressurized)
                        currentStatus = Status.Depressurizing;

                    else if (currentStatus == Status.Depressurized)
                        currentStatus = Status.Pressurizing;
                }
            }

            public string ValidateBlocks()
            {
                string str = $"AIRLOCK: {airlockName}\nReferences Status: \n";

                string str_doorOut;
                if (!isHangar) str_doorOut = doorOut == null ? "NO" : "YES";
                else str_doorOut = hangerDoors == null ? "NO" : "YES";
                
                string str_doorIn = doorIn == null ? "NO" : "YES";
                string str_airvent = airvent == null ? "NO" : "YES";
                string str_statusLCD = statusLCD == null ? "NO" : "YES";

                str += $"Air vent = {str_airvent}\n";
                str += $"LCD Panel = {str_statusLCD}\n";
                str += $"Door In = {str_doorIn}\n";
                str += $"Door Out = {str_doorOut}\n";
                if (isHangar) str += "is a hanger bay \n";

                return str;
            }

            public void ProcessAirlock()
            {
                if (!AirlockFailure() && !isOverriden)
                {
                    ChangePressure(doorOut, doorIn, Status.Pressurizing, Status.Pressurized, _hA: isHangar);
                    ChangePressure(doorIn, doorOut, Status.Depressurizing, Status.Depressurized, _hB: isHangar);
                    AirlockDisplay();
                }
            }

            private void ChangePressure(IMyDoor _a, IMyDoor _b, Status _check, Status _set, bool _hA = false, bool _hB = false)
            {
                if (currentStatus == _check)
                {
                    if (!_hA) _a.CloseDoor();
                    else
                    {
                        for (int i = 0; i < hangerDoors.Length; i++)
                        {
                            IMyDoor hangerDoor = hangerDoors[i];
                            hangerDoor.CloseDoor();
                        }
                    }

                    if (_a.OpenRatio == 0) PressureChangeState(_a, _b, _set, _hA, _hB);
                }
            }

            private void PressureChangeState(IMyDoor _a, IMyDoor _b, Status _set, bool _hA, bool _hB)
            {
                if(!_hA) _a.Enabled = false;
                airvent.Depressurize = Depressurize;

                if (CheckProgression())
                {
                    currentStatus = _set;
                    if (!_hB)
                    {
                        _b.Enabled = true;
                        _b.OpenDoor();
                    }
                    else
                    {
                        for (int i = 0; i < hangerDoors.Length; i++)
                        {
                            IMyDoor hangerDoor = hangerDoors[i];
                            hangerDoor.OpenDoor();
                        }
                    }
                }
            }

            private bool CheckProgression()
            {
                if(currentStatus == Status.Pressurizing) return progression > 9;
                else return progression == 0;
            }

            public bool AirlockFailure()
            {
                bool null1 = doorIn == null;
                bool null2 = doorOut == null;   
                bool null3 = airvent == null;
                bool null4 = statusLCD == null;

                return null1 || null2 || null3 || null4;
            }

            private void AirlockDisplay()
            {
                string str = $"{airlockName}\n\n";
                str += $"Status: {currentStatus}\n\n";

                if (currentStatus == Status.Pressurizing || currentStatus == Status.Depressurizing)
                    str += ProgressDialAnimation() + "\n\n";

                str += "Door Out: Closed\n\n";
                str += "Door In: Opened\n\n";
                str += $"Pressure Level: {Progression}";
                statusLCD?.WriteText(str);
            }

            private string ProgressionBarAnimation()
            {
                StringBuilder bar = new StringBuilder("[");
                bar.Append('=', progression);
                bar.Append('-', (maxProgression - progression));
                bar.Append("]");
                return bar.ToString();
            }

            string ProgressDialAnimation()
            {
                string str = progressionDial[index];
                index++;
                if (index > progressionDial.Length - 1) index = 0;
                return str;
            }

            internal bool CanCycle(ref string _overrideKey)
            {
                if (isOverriden) return false;

                string customData = (statusLCD as IMyTerminalBlock).CustomData;

                if (!customData.Equals(CUSTOMDATA)) _overrideKey = airlockName;

                return currentStatus == Status.Pressurizing || currentStatus == Status.Depressurizing || progression < 10;
            }

            internal void DisableOverride()
            {
                (statusLCD as IMyTerminalBlock).CustomData = CUSTOMDATA;
                isOverriden = false;
                SetPressurized();
            }

            internal void OverrideDoors()
            {
                isOverriden = true;
                doorIn.Enabled = true;
                doorOut.Enabled = true;

                doorIn.OpenDoor();
                doorOut.OpenDoor();
            }

            internal void SetPressurized() => currentStatus = Status.Pressurizing;
        }

        private static string[] ONES = {
        "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine",
        "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen",};

        const char DELIM = ',';

        const string OVERRIDECOMMAND = "Override";
        const string DISABLEOVERRIDECOMMAND = "Reset";

        private const string CUSTOMDATA
            = "Welcome to airlock os\n" +
              "if you are experiencing trouble with breathing,\n" +
              "please consult the air\n" +
              "For emergency use you can enable the emergency override below\n\n" +
              "[Manual Override] = False";

        private const string USAGEDATA
            = "Welcome to the Airlock System\n\n" +
            "To use create an airlock with: \n" +
              " -  2 doors\n" +
              " -  1 vent\n" +
              " -  1 lcd\n" +
              " -  1 button panel\n\n" +
              
            "Naming conventions: \n" +
              "number = the number of this airlock, \nand needs to be the same for each block of the airlock\n\n" +
              "Door to outside  ->  [ADOUT],{number}\n" +
              "Door to inside   ->  [ADIN],{number}\n" +
              "Vent             ->  [AVENT],{number}\n" +
              "LCD              ->  [ALCD],{number}\n\n" +
              "Button panel needs to have a button setup to run the programmble block,\n" +
              "the run argument needs to be Cycle,{number}";

        // For the airlock
        const string DOOROUTPREFIX         =  "[ADOUT]";
        const string DOORINPREFIX          =  "[ADIN]";
        const string AIRVENTPREFIX         =  "[AVENT]";
        const string LCDPREFIX             =  "[ALCD]";
        const string HANGERPREFIX          =  "[AHANGER]";
        const string MAINLCDPREFIX         =  "[AMLCD]";
        const string HANGERIDENTIFIER      =  "[H]";

        IMyTextSurface mainLCD;

        Dictionary<string, Airlock> airlocks = new Dictionary<string, Airlock>();
        private bool overidden = false;

        public Program()
        {
            Me.CustomData = USAGEDATA;

            Echo("Starting Airlock Procedures....");

            List<IMyTextSurface> surfaces = new List<IMyTextSurface>();
            GridTerminalSystem.GetBlocksOfType(surfaces);

            mainLCD = surfaces.FirstOrDefault(lcd => (lcd as IMyTerminalBlock).CustomName.Contains(MAINLCDPREFIX));

            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            GetNewAirlocks();

            airlockNames = airlocks.Keys.ToArray();
            ControlMainLCD(OPTION);
        }

        public void Save()
        {
            
        }

        public void Main(string argument, UpdateType updateType)
        {
            if(!overidden) ControlAirlocks(argument, updateType);
            ResetEmergencyOverride(argument);
            EmergencyOverride(argument);
            ControlMainLCD(argument);
        }

        // AIRLOCK CONTROL METHODS

        private void ResetEmergencyOverride(string argument)
        {
            if (argument.Contains(DISABLEOVERRIDECOMMAND))
            {
                string[] str = argument.Split(DELIM);

                if (str.Length > 1)
                {
                    Airlock airlock = airlocks[str[1]];
                    if (!airlock.AirlockFailure())
                        airlock.DisableOverride();
                }
                else
                {
                    foreach (Airlock airlock in airlocks.Values)
                        if (!airlock.AirlockFailure())
                            airlock.DisableOverride();
                }
            }
        }

        private void EmergencyOverride(string argument)
        {
            if (argument.Contains(OVERRIDECOMMAND))
            {
                string[] str = argument.Split(DELIM);

                if (str.Length > 1)
                {
                    Airlock airlock = airlocks[str[1]];
                    if (!airlock.AirlockFailure())
                        airlock.OverrideDoors();
                }
                else
                {
                    foreach (Airlock airlock in airlocks.Values)
                        if (!airlock.AirlockFailure())
                            airlock.OverrideDoors();
                }

                //ControlMainLCD("Option");
            }
        }

        private void ControlAirlocks(string argument, UpdateType updateType)
        {
            if (argument.Contains("Cycle"))
                airlocks[argument.Split(DELIM)[1]].CycleAirlock();

            if (updateType == UpdateType.Update10)
            {
                foreach (var airlock in airlocks.Values)
                {
                    string o = "";
                    if (airlock.CanCycle(ref o))
                    {
                        airlock.ProcessAirlock();
                        ControlMainLCD("Option");
                    }
                    if(!string.IsNullOrEmpty(o)) EmergencyOverride($"{OVERRIDECOMMAND}{DELIM}{o}");
                }
            }
        }

        // AIRLOCK CREATION METHODS

        private void GetNewAirlocks()
        {
            List<IMyDoor> doorBlocks = new List<IMyDoor>();
            List<IMyAirVent> airventBlocks = new List<IMyAirVent>();
            List<IMyTextSurface> lcdBlocks = new List<IMyTextSurface>();

            GetBlocks(ref lcdBlocks, ref airventBlocks, ref doorBlocks);

            CreateAirlocks(lcdBlocks, airventBlocks, doorBlocks);
        }
        private IMyTerminalBlock GetRequiredBlock<T>(List<T> _blocks, string _prefix, string _ventID) where T : IMyTerminalBlock
        {
            return _blocks.FirstOrDefault(n =>
            {
                if (n.CustomName.Contains(_prefix))
                {
                    string[] name = n.CustomName.Split(DELIM);
                    if (name.Length > 1) return name[1].Contains(_ventID);
                }
                return false;
            });
        }
        private void CreateAirlocks(List<IMyTextSurface> lcdBlocks, List<IMyAirVent> airventBlocks, List<IMyDoor> doorBlocks)
        {
            for (int i = 0; i < airventBlocks.Count; i++)
            {
                IMyAirVent airvent = airventBlocks[i];
                if (airvent.CustomName.Contains(AIRVENTPREFIX))
                {
                    string[] airventSplit = airvent.CustomName.Split(DELIM);

                    if (airventSplit.Length > 1)
                    {
                        string ventID = airventSplit[1];
                        string airlockName = $"Airlock {ONES[int.Parse(ventID)]}".ToUpper();

                        if (!airlocks.ContainsKey(airlockName))
                        {
                            IMyTextSurface lcd = null;
                            IMyDoor doorOut = null;
                            IMyDoor doorIn = null;

                            GetRequiredBlocks(lcdBlocks, doorBlocks, ventID, ref lcd, ref doorIn, ref doorOut);

                            bool isHanger = false;
                            IMyDoor[] doors = null;
                            if(doorOut.CustomName.Contains(HANGERIDENTIFIER))
                            {
                                IMyBlockGroup hangerGroup = GridTerminalSystem.GetBlockGroupWithName($"{HANGERPREFIX},{int.Parse(ventID)}");

                                if (hangerGroup != null)
                                {
                                    List<IMyDoor> h = new List<IMyDoor>();
                                    hangerGroup.GetBlocksOfType(h);
                                    doors = h.ToArray();
                                    isHanger = true;
                                }
                            }

                            airlocks.Add(airlockName, new Airlock(airlockName, doorOut, doorIn, airvent, lcd, doors, isHanger));

                            if (airlocks[airlockName] != null)
                            {
                                Airlock airlock = airlocks[airlockName];
                                Echo(airlock.ValidateBlocks());
                                Echo(" ");
                                Echo(" ");
                            }
                        }
                    }

                }
            }
        }
        private void GetRequiredBlocks(List<IMyTextSurface> lcdBlocks, List<IMyDoor> doorBlocks, string ventID, ref IMyTextSurface lcd, ref IMyDoor doorIn, ref IMyDoor doorOut)
        {
            // Find the matching text fucker
            lcd = lcdBlocks.FirstOrDefault(n =>
            {
                IMyTerminalBlock x = n as IMyTerminalBlock;
                if (x.CustomName.Contains(LCDPREFIX))
                {
                    string[] name = x.CustomName.Split(DELIM);
                    if (name.Length > 1) return name[1].Contains(ventID);
                }
                return false;
            });

            // Find the matching Door into base
            doorIn = GetRequiredBlock(doorBlocks, DOORINPREFIX, ventID) as IMyDoor;

            // Find the matching Door out of base
            doorOut = GetRequiredBlock(doorBlocks, DOOROUTPREFIX, ventID) as IMyDoor;
        }
        private void GetBlocks(ref List<IMyTextSurface> lcdBlocks, ref List<IMyAirVent> airventBlocks, ref List<IMyDoor> doorBlocks)
        {
            lcdBlocks = new List<IMyTextSurface>();
            airventBlocks = new List<IMyAirVent>();
            doorBlocks = new List<IMyDoor>();
            GridTerminalSystem.GetBlocksOfType(lcdBlocks);
            GridTerminalSystem.GetBlocksOfType(airventBlocks);
            GridTerminalSystem.GetBlocksOfType(doorBlocks);
        }

        // AIRLOCK LCD CONTROL
        
        string[] airlockNames = null;
        string[] airlockSelectionOptions = { "Cycle Airlock", "Override Airlock", "Reset Airlock" };
        string[] options = { "Override Doors", "New Airlocks", "Reset Doors", };

        int airlockIndex = 0;
        int optionIndex = 0;
        int selectionIndex = 0;

        const string OPTION  =  "Option";
        const string UP      =  "UP";
        const string DOWN    =  "DOWN";
        const string CONFIRM =  "CONFIRM";
        const string BACK    =  "BACK";

        bool optionSelected = false;
        bool isCommand = false;

        // Optimise the checks with a dictionary of methods

        private void ControlMainLCD(string _arg)
        {
            if(_arg.Contains(OPTION) && mainLCD != null)
            {
                string str = "";
                if (!optionSelected) str = SelectOptions(_arg);
                else str = HandleSelection(_arg);
                mainLCD.WriteText(str);
            }
        }

        private string HandleSelection(string _arg)
        {
            string[] arr = _arg.Split(DELIM);
            string selection = GetSelection();

            if (arr.Length > 1)
            {
                // If back is parsed
                if (arr[1].Contains(BACK))
                {
                    return Reset();
                }

                if (arr[1].Contains(UP))
                {
                    selectionIndex--;
                    if (selectionIndex < 0) 
                        selectionIndex = 0;
                }

                else if (arr[1].Contains(DOWN))
                {
                    selectionIndex++;
                    if (selectionIndex > airlockSelectionOptions.Length - 1)
                        selectionIndex = airlockSelectionOptions.Length - 1;
                }

                if (arr[1].Contains(CONFIRM))
                {
                    RunAirlockCommand(airlockSelectionOptions[selectionIndex], airlockNames[airlockIndex]);
                    return Reset();
                }
            }

            if (!isCommand)
            {
                try
                {
                    var airlock = airlocks[airlockNames[airlockIndex]];
                    string str = $"NAME: {airlock.airlockName}\n";
                    str += $"Airlock Status => {airlock.GetStatus}\n\n";
                    str += "Airlock Options: \n";

                    for (int i = 0; i < airlockSelectionOptions.Length; i++)
                    {
                        string selected = "";
                        string a = airlockSelectionOptions[i];
                        if (i == selectionIndex) selected = "> ";
                        str += selected + a + "\n";
                    }

                    return str;
                }
                catch { return "ERROR WITH DISPLAYING INFORMATION FOR AIRLOCK"; } 
            }


            // if we are a command for example Override Doors, run command
            if (selection.Contains("Override Doors"))
            {
                EmergencyOverride(OVERRIDECOMMAND);
                return Reset();
            }

            if (selection.Contains("New Airlocks"))
            {
                GetNewAirlocks();
                airlockNames = airlocks.Keys.ToArray();
                return Reset();
            }

            if (selection.Contains("Reset Doors"))
            {
                ResetEmergencyOverride(DISABLEOVERRIDECOMMAND);
                return Reset();
            }

            return _arg;
        }

        private void RunAirlockCommand(string _command, string _key)
        {
            // "Cycle Airlock", "Override Airlock", "Reset Airlock"
            Airlock airlock = airlocks[_key];
            if (airlock.AirlockFailure()) return;

            if (_command.Equals("Cycle Airlock"))
            {
                ControlAirlocks($"Cycle{DELIM}{airlock.airlockName}", UpdateType.Script);
                return;
            }

            if (_command.Equals("Override Airlock"))
            {
                EmergencyOverride($"{OVERRIDECOMMAND}{DELIM}{airlock.airlockName}");
                return;
            }


            if (_command.Equals("Reset Airlock"))
            {
                ResetEmergencyOverride($"{DISABLEOVERRIDECOMMAND}{DELIM}{airlock.airlockName}");
                return;
            }
        }

        private string GetSelection()
        {
            // air lock selection
            if (airlockIndex <= airlockNames.Length - 1)
            {
                isCommand = false;
                return airlockNames[airlockIndex];
            }

            // command selection
            else
            {
                isCommand = true;
                return options[optionIndex];
            }
        }

        private string Reset()
        {
            // reset all values          
            if (!isCommand)
            {
                selectionIndex = 0;
                optionIndex = 0;
                airlockIndex = 0;
            }

            optionSelected = false;
            isCommand = false;

            // go back to options
            return SelectOptions(OPTION);
        }

        private string SelectOptions(string _arg)
        {
            string[] arr = _arg.Split(DELIM);
            if (arr.Length > 1) ChangeOptions(arr[1]);

            if (airlockIndex <= 0) airlockIndex = 0;

            string str = "Airlock System\n\nCurrent Airlocks:\n\n";
            for (int i = 0; i < airlockNames.Length; i++)
            {
                string selected = "";
                string key = airlockNames[i];
                string s = airlocks[key].GetStatus;
                if (i == airlockIndex) selected = "> ";
                str += selected + key.PadRight(15);
                str += " Status: " + s + '\n';
            }

            str += '\n';

            for (int i = 0; i < options.Length; i++)
            {
                string selectedFront = "";
                string selectedBack = "";

                if (i == optionIndex && airlockIndex >= airlockNames.Length)
                {
                    selectedFront = "[";
                    selectedBack = "]";
                }

                str += selectedFront + options[i] + selectedBack.PadRight(5);
            }

            if (arr.Length > 1)
            {
                if (arr[1].Contains(CONFIRM))
                {
                    optionSelected = true;
                    return HandleSelection(OPTION);
                }
            }
            return str;
        }

        private void ChangeOptions(string _direction)
        {
            if (_direction.Contains(UP))
            {
                if (airlockIndex <= airlockNames.Length - 1)
                    airlockIndex--;

                else
                {
                    optionIndex--;
                    if (optionIndex < 0)
                    {
                        airlockIndex--;
                        optionIndex = 0;
                    }
                }
            }

            if (_direction.Contains(DOWN))
            {
                if (airlockIndex <= airlockNames.Length - 1)
                    airlockIndex++;

                else
                {
                    optionIndex++;
                    if (optionIndex > options.Length - 1)
                        optionIndex = options.Length - 1;
                }
            }
        }
    }
}
