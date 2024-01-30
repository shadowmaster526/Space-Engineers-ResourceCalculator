using Sandbox.Definitions;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Screens;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Interfaces.Terminal;
using SpaceEngineers.Game.Entities.Blocks;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Resources;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Graphics;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRageMath;
using IMyTextSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;

namespace IngameScript.Turbo
{
    partial class Program : MyGridProgram
    {
        public enum DisplayState
        {
            TurboLiftSelectionMenu,
            TurboLiftModifyMenu
        }

        const double DISTTHRESHOLD = 3;
        const string DOORPREFIX         = "[tdoor]";
        const string TURBOLIFTPREFIX    = "[tLift]";
        const string LCDPREFIX          = "[tLCD]";

        public struct LCD
        {
            public IMyTextSurface Panel;
            public IMyTerminalBlock Block;
            public string CustomData;

            public string StrippedCustomData(string _customData)
            {
                string[] split = _customData.Split('\n');

                if(split.Length > 0) { 
                    string[] split2 = split[0].Split(':');
                    if(split?.Length > 0)
                        return split2[1];
                }

                return "NULL";
            }

            public LCD(IMyTextSurface panel, string customData)
            {
                Panel = panel;
                CustomData = customData;
                Block = panel as IMyTerminalBlock;
                Block.CustomData = CustomData;
            }
        }
        public struct TurboLift
        {
            public IMyButtonPanel Lift;
            public Dictionary<int, TurboLiftButton> Buttons;
            public IMyDoor LiftDoor;
            public int id;

            public TurboLift(IMyButtonPanel lift, Dictionary<int,TurboLiftButton> Buttons, IMyDoor liftDoor)
            {
                Lift = lift;
                this.Buttons = Buttons;
                LiftDoor = liftDoor;
                id = -1;
            }

            public void TeleportTo(int _buttonID)
            {
                Lift.GetActionWithName("Activate").Apply(Buttons[_buttonID].To);
                LiftDoor?.CloseDoor();
            }
            public bool ButtonExists(int _buttonID) => Buttons.ContainsKey(_buttonID);
            internal void NewButton(int _buttonID, IMyButtonPanel _lift = null)
            {
                if (!ButtonExists(_buttonID))
                    Buttons.Add(_buttonID, new TurboLiftButton(_lift));

                else
                {
                    TurboLiftButton button = Buttons[_buttonID];
                    button.To = _lift;
                    Buttons[_buttonID] = button;
                }

                LiftDoor?.CloseDoor();
            }
        }
        public struct TurboLiftButton
        {
            public IMyButtonPanel To;

            public TurboLiftButton(IMyButtonPanel to)
            {
                To = to;
            }
        }

        readonly Dictionary<string,TurboLift> turboLifts = new Dictionary<string, TurboLift>();
        readonly List<IMyButtonPanel> activeTurboLifts = new List<IMyButtonPanel>();
        readonly List<IMyDoor> activeDoors = new List<IMyDoor>();
        readonly List<LCD> activeLCDs = new List<LCD>();
        DisplayState currentState = DisplayState.TurboLiftSelectionMenu;


        public Program()
        {
            Init();
            SetupDoors();
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
        
        public void Main(string argument, UpdateType updateType)
        {
            HandleTurbolift(argument);
            HandleLCDPanels();
        }

        private void HandleLCDPanels()
        {
            // loop through all lcds.
            // depending on the state check if the values have changed
            // if Selection Menu Check default value changed, as the selection menu
            // can update without input from the player;

            for(int i = 0; i < activeLCDs.Count; i++)
            {
                LCD currentLCD = activeLCDs[i];
                string customData = currentLCD.Block.CustomData;
                if(currentState == DisplayState.TurboLiftSelectionMenu)
                {
                    try
                    {
                        string strippedData = currentLCD.StrippedCustomData(customData);
                        string strippedData2 = currentLCD.StrippedCustomData(currentLCD.CustomData);

                        if (strippedData.Equals("NULL"))
                        {
                            customData = DisplayTurboLiftSelectionMenu();
                            return;
                        }

                        if (!strippedData.Equals(strippedData2))
                            customData = ChangeState(DisplayState.TurboLiftModifyMenu, strippedData);
                        else customData = DisplayTurboLiftSelectionMenu();
                    } catch (Exception e) { currentLCD.Panel.WriteText($"Error going from selection menu to modify menu\n{e}"); }
                }

                else if(currentState == DisplayState.TurboLiftModifyMenu)
                {
                    if (!customData.Equals(currentLCD.CustomData))
                        customData = ChangeState(DisplayState.TurboLiftSelectionMenu, customData);
                }

                currentLCD.Block.CustomData = customData;
                currentLCD.Panel.WriteText(customData);
                currentLCD.CustomData = customData;
                activeLCDs[i] = currentLCD; 
            }
        }

        private string ChangeState(DisplayState _newState, string _customData)
        {
            try
            {
                currentState = _newState;
                switch (_newState)
                {
                    case DisplayState.TurboLiftModifyMenu:
                        try { return DisplayTurboLiftModifyMenu(int.Parse(_customData)); }
                        catch { return ChangeState(DisplayState.TurboLiftSelectionMenu, _customData); }

                    case DisplayState.TurboLiftSelectionMenu:
                        ApplyModifications(_customData);
                        return DisplayTurboLiftSelectionMenu();
                }
            }
            catch (Exception e) { return $"{e}"; }

            return "NULL";
        }

        private void ApplyModifications(string _customData)
        {
            try
            {
                string[] strArray = _customData.Split('\n');

                int currentID = -1;
                for (int i = 0; i < strArray.Length; i++)
                {
                    string str = strArray[i];

                    if (str.Contains("tid"))
                        currentID = int.Parse(str.Split(':')[1]);

                    if (currentID < 0) continue;

                    if (str.ToLower().Contains("button"))
                    {
                        string str_button = str.Split('-')[1];
                        string[] IDs = str_button.Split(':');

                        int buttonID = int.Parse(IDs[0]);
                        int liftID = int.Parse(IDs[1]);

                        TurboLift t = turboLifts[activeTurboLifts[currentID].CustomName];
                        t.NewButton(buttonID, activeTurboLifts[liftID]);
                        turboLifts[activeTurboLifts[currentID].CustomName] = t;
                    }
                }
            }
            catch (Exception e) { Echo(e.ToString()); }
        }

        string teleportLog = "\n";
        void HandleTurbolift(string argument)
        {
            try
            {
                string[] args = argument.Split(',');

                if (args != null)
                {
                    if (args.Length > 1)
                    {
                        string name = args[0];
                        int id = int.Parse(args[1]);

                        Echo($"Searching for {name} : button {id}");
                        if (turboLifts.ContainsKey(name))
                        {
                            TurboLift currentTurboLift = turboLifts[name];
                            if (currentTurboLift.ButtonExists(id))
                            {
                                currentTurboLift.TeleportTo(id);
                                teleportLog += $"TO: {currentTurboLift.Buttons[id].To.CustomName} FROM: {currentTurboLift.Lift.CustomName}\n";
                            }
                            else currentTurboLift.NewButton(id);
                        }
                    }
                }
            }
            catch (Exception e) { activeLCDs[0].Panel.WriteText($"Error with Turbolifts {e}"); }
        }

        void Init()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType(blocks);

            for (int i = 0; i < blocks.Count; i++)
            {
                IMyTerminalBlock block = blocks[i];
                GetTurboLifts(block);
                GetDoors(block);
                GetLCDs(block);
            }
        }

        void GetLCDs(IMyTerminalBlock block)
        {
            if (block.CustomName.Contains(LCDPREFIX))
            {
                activeLCDs.Add(new LCD(block as IMyTextSurface, DisplayTurboLiftSelectionMenu()));
                (block as IMyTextSurface).WriteText(DisplayTurboLiftSelectionMenu());
            }
        }
        void GetDoors(IMyTerminalBlock block)
        {
            if (block.CustomName.Contains(DOORPREFIX))
                activeDoors.Add(block as IMyDoor);
        }
        void GetTurboLifts(IMyTerminalBlock block)
        {
            if (block.CustomName.Contains(TURBOLIFTPREFIX))
            {
                activeTurboLifts.Add(block as IMyButtonPanel);
                turboLifts.Add(block.CustomName,
                    new TurboLift(block as IMyButtonPanel,
                    new Dictionary<int, TurboLiftButton>(), null));
            }
        }

        string DisplayTurboLiftSelectionMenu()
        {
            string text = "Choose Turbolift to modify: -1\n\n";
            for (int i = 0; i < activeTurboLifts.Count; i++)
            {
                IMyButtonPanel lifts = activeTurboLifts[i];
                text += $"{i} | {lifts.CustomName} : {turboLifts[lifts.CustomName].Buttons.Count}\n";
            }

            text += '\n';
            text += $"Doors Amount : {activeDoors.Count}{teleportLog}\n";

            return text;
        }

        string DisplayTurboLiftModifyMenu(int _index)
        {
            // swaps back to default state if out of bounds or some reason a negative number
            if (_index < 0 || _index > activeTurboLifts.Count)
            {
                currentState = DisplayState.TurboLiftSelectionMenu;
                return DisplayTurboLiftSelectionMenu();
            }

            // displays starting message, and gets the current turbo lift from the _index
            string message = "Change connections between turbolifts\n\n";
            TurboLift turboLift = turboLifts[activeTurboLifts[_index].CustomName];

            // Looks through the active turbo lifts
            for (int i = 0; i < activeTurboLifts.Count; i++)
            {
                var t = activeTurboLifts[i];

                // ignores current turbolift, add others to message
                if(t.CustomName != turboLift.Lift.CustomName)
                    message += $"ID: {i} TurboLift: {t.CustomName}\n"; 

                // used to set the ids, done here cos why not incase of a weird change
                TurboLift td = turboLifts[t.CustomName];
                td.id = i;
                turboLifts[t.CustomName] = td;
            }

            message += '\n';
            message += $"tid:{_index}\n"; // this just used to identify the active turbolift
            message += "Modify values below: \n";

            // loop through turbolift buttons
            foreach (int key in turboLift.Buttons.Keys)
            {
                // get the button we want to display
                TurboLiftButton button = turboLift.Buttons[key];
                int id = 0;

                // set the id to the currently stored id of the To turbolift
                if (button.To != null) id = turboLifts[button.To.CustomName].id;
                // display the key for this button + the id of the To
                message += $"Button -{key}: {id}\n";
            }

            message += "\nBack?: ";
            return message;
        }

        void SetupDoors()
        {
            for (int i = 0; i < turboLifts.Count; i++)
            {
                string message = $"TurboLift {i}\n\n"; 
                TurboLift lift = turboLifts[activeTurboLifts[i].CustomName];
                IMyTerminalBlock lift_block = lift.Lift;

                for (int j = 0; j < activeDoors.Count; j++)
                {
                    IMyTerminalBlock door_block = activeDoors[j];
                    Vector3D lift_position = lift_block.GetPosition();
                    Vector3D door_position = door_block.GetPosition();
                    
                    if(Vector3D.Distance(lift_position, door_position) < DISTTHRESHOLD)
                    {
                        message += $"Added" +
                            $"\nTurbolift: {lift.Lift.CustomName}" +
                            $"\nDoor: {door_block.CustomName}";

                        lift.LiftDoor = door_block as IMyDoor;
                        message += '\n';

                        lift.LiftDoor.CloseDoor();
                        break;
                    }
                }

                turboLifts[activeTurboLifts[i].CustomName] = lift; // <-- needed to add this
                Echo(message);
            }
        }
    }
}
