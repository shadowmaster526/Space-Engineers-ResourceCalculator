using Sandbox.Definitions;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.Screens;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
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
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;
using IMyTextSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        enum States
        {
            QueueSelection,
            DisplaySelection,
        }

        IMyTextSurface lcd;
        List<IMyCargoContainer> cargoContainers = new List<IMyCargoContainer>();
        List<IMyRefinery> refineries = new List<IMyRefinery>();
        List<IMyAssembler> assemblers = new List<IMyAssembler>();
        List<string> options = new List<string>();
        private int padAmount = 10;

        private States currentState = States.QueueSelection;

        Component DEFAULT = new Component(new List<RequiredMaterial>(), "NULL");

        Dictionary<string, Component> blueprints = new Dictionary<string, Component>();
        private int currentOption = 0;

        private struct Component
        {
            public string subtypeid;
            public List<RequiredMaterial> requiredMaterials;

            public Component(List<RequiredMaterial> _materials, string _subtype = "")
            {
                requiredMaterials = _materials;
                subtypeid = _subtype;
            }
        }

        private struct RequiredMaterial
        {
            public string Name;
            public double Amount;

            public RequiredMaterial(string _name,  double _amount)
            {
                Name = _name;
                Amount = _amount;
            }
        }

        public Program()
        {
            // The constructor, called only once every session and
            // always before any other method is called. Use it to
            // initialize your script. 
            //     
            // The constructor is optional and can be removed if not
            // needed.
            // 
            // It's recommended to set Runtime.UpdateFrequency 
            // here, which will allow your script to run itself without a 
            // timer block.

            Runtime.UpdateFrequency = UpdateFrequency.Update10;

            string err = "Missing [display] LCD\n";

            try { lcd = GridTerminalSystem.GetBlockWithName("[display] LCD") as IMyTextSurface; } 
            catch { }

            try
            {
                cargoContainers = GetCargo();
                refineries = GetRefineries();
                assemblers = GetAssemblers();
                CreateBlueprints();
                WriteCustomData();
            } catch { Echo(err + "Missing Refineries, Cargo and assemblers"); }
        }

        private void CreateBlueprints()
        {
            blueprints.Clear();

            // Used to create blueprint recipes in space engineers.
            /*
                Format for blue print 
                
                blueprints.Add("[Name of blueprint]", new Component(new List<RequiredMaterial>
                {
                    new RequiredMaterial("[ingot]", [amount]),
                }, "[SubID]"));

             */

            blueprints.Add("Superconductor", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Gold", 0.67),
                new RequiredMaterial("Silver", 0.67),
                new RequiredMaterial("Titanium", 2.67),
                new RequiredMaterial("Copper", 1.67)
            }, "Superconductor"));
            blueprints.Add("Thruster", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Platinum", 0.13),
                new RequiredMaterial("Gold", 0.33),
                new RequiredMaterial("Cobalt", 1.67),
                new RequiredMaterial("Copper", 1.67),
                new RequiredMaterial("Titanium", 3.33),
                new RequiredMaterial("Iron", 3.33)
            }, "ThrustComponent"));
            blueprints.Add("Reactor", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Silver", 1.67),
                new RequiredMaterial("Copper", 1.67),
                new RequiredMaterial("Titanium", 5.00),
                new RequiredMaterial("Aluminium", 1.67),
                new RequiredMaterial("Iron", 3.33)
            }, "ReactorComponent"));
            blueprints.Add("Tyre", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("SyntheticRubber", 3.33),
                new RequiredMaterial("SteelWire", 0.17)
            }, "RubberTyre"));
            blueprints.Add("TorpedoThruster", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Tritanium", 0.11),
                new RequiredMaterial("Duranium", 0.11),
                new RequiredMaterial("Aluminum", 0.08),
                new RequiredMaterial("Iron", 0.08),
                new RequiredMaterial("Nickel", 0.03),
                new RequiredMaterial("Silicon", 0.03),
                new RequiredMaterial("MagnesiumPowder", 0.33),
                new RequiredMaterial("Cobalt", 0.33),
                new RequiredMaterial("Silver", 0.17),
                new RequiredMaterial("Gold", 0.17)
            }, "TorpedoThruster"));
            blueprints.Add("TransparentAluminumPlate", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Aluminum", 10.0),
                new RequiredMaterial("Silver", 6.67)
            }, "TransparentAluminumPlate"));
            blueprints.Add("TorpedoWarhead", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Tritanium", 0.17),
                new RequiredMaterial("Duranium", 0.17),
                new RequiredMaterial("Aluminum", 0.11),
                new RequiredMaterial("Iron", 0.11),
                new RequiredMaterial("Nickel", 0.08),
                new RequiredMaterial("Silicon", 0.08),
                new RequiredMaterial("MagnesiumPowder", 0.03),
                new RequiredMaterial("Cobalt", 0.03),
                new RequiredMaterial("Silver", 0.33),
                new RequiredMaterial("Gold", 0.33),
            }, "TorpedoWarhead"));
            blueprints.Add("SteelWire", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Iron", 0.4)
            }, "GalvanisedWire"));
            blueprints.Add("SteelPlate", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Iron", 7.0)
            }, "SteelPlate"));
            blueprints.Add("SteelGirder", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Steel", 2.0)
            }, "SteelGirder"));
            blueprints.Add("SolarCell", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Nickel", 0.67),
                new RequiredMaterial("Aluminium", 0.67),
                new RequiredMaterial("Silicon", 1.67),
                new RequiredMaterial("Copper", 0.67),
            }, "SolarCell"));
            blueprints.Add("SmallSteelTube", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Iron", 1.67)
            }, "SmallTube"));
            blueprints.Add("SiTyre", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Silicon", 6.67),
                new RequiredMaterial("SteelWire", 1.67)

            }, "SiliconeTyre"));
            blueprints.Add("FieldEmitter", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Platinum", 2.67),
                new RequiredMaterial("Iron", 26.67),
                new RequiredMaterial("Silicon", 6.67),
                new RequiredMaterial("Gold", 5.00)

            }, "ShieldComponentBP"));
            blueprints.Add("RadioComponent", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Silicon", 0.33),
                new RequiredMaterial("Iron", 2.00),
                new RequiredMaterial("Copper", 1.67)

            }, "RadioCommunication"));
            blueprints.Add("PowerCell", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Iron", 1.67),
                new RequiredMaterial("Silicon", 0.33),
                new RequiredMaterial("Nickel", 2.67),
                new RequiredMaterial("Copper", 0.67)

            }, "PowerCell"));
            blueprints.Add("Polycarbonate", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Plastic", 1.67),
                new RequiredMaterial("Silicon", 0.67)
            }, "Polycarbonate"));
            blueprints.Add("Magnet", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Iron", 1.33),
                new RequiredMaterial("Nickel", 1.33),
                new RequiredMaterial("Cobalt", 1.33)
            }, "Electromagnet"));
            blueprints.Add("LiSCell", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Aluminium", 0.67),
                new RequiredMaterial("Lithium", 0.67),
                new RequiredMaterial("Sulfur", 0.67),
                new RequiredMaterial("Cobalt", 0.33),
                new RequiredMaterial("GoldWire", 0.07),
            }, "LithiumSulfurPowerCell"));
            blueprints.Add("LiFePo4Cell", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Aluminium", 0.67),
                new RequiredMaterial("Lithium", 1.33),
                new RequiredMaterial("Cobalt", 0.33),
                new RequiredMaterial("CopperWire", 0.07),
            }, "LithiumIonPowerCell"));
            blueprints.Add("LightEmittingDiode", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Gallium", 0.67),
                new RequiredMaterial("Plastic", 1.33),
                new RequiredMaterial("CopperWire", 0.33),
            }, "LightEmittingDiode"));
            blueprints.Add("LargeSteelTube", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Iron", 10.00)
            }, "LargeTube"));
            blueprints.Add("InteriorPlate", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Iron", 0.67),
                new RequiredMaterial("Aluminium", 0.33)
            }, "InteriorPlate"));
            blueprints.Add("Heatsink", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Copper", 0.33),
                new RequiredMaterial("Aluminium", 0.67)
            }, "Heatsink"));
            blueprints.Add("HackingChip", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Platinum", 0.30),
                new RequiredMaterial("Silver", 0.33),
                new RequiredMaterial("Gold", 0.53),
                new RequiredMaterial("Silicon", 0.90)
            }, "HackingChip"));
            blueprints.Add("GravityComp", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Silver", 1.67),
                new RequiredMaterial("Gold", 3.33),
                new RequiredMaterial("Copper", 13.33),
                new RequiredMaterial("Aluminium", 16.67),
                new RequiredMaterial("Cobalt", 16.67),
                new RequiredMaterial("Iron", 66.67),
                new RequiredMaterial("Titanium", 100.00)
            }, "GravityGeneratorComponent"));
            blueprints.Add("GoldWire", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Gold", 0.33),
                new RequiredMaterial("Plastic", 0.01),
            }, "GoldWire"));
            blueprints.Add("TemperedGlass", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Silicon", 5.0)
            }, "TemperedGlass"));
            blueprints.Add("Circuit", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Silicon", 1.33),
                new RequiredMaterial("Plastic", 1.33),
                new RequiredMaterial("GoldWire", 0.07),
            }, "AdvancedCircuit"));
            blueprints.Add("TitaniumCompositePlate", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Titanium", 1.0),
                new RequiredMaterial("DepletedUranium", 0.07),
            }, "TitaniumCompositePlate"));
            blueprints.Add("Computer", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Iron", 0.07),
                new RequiredMaterial("Silicon", 0.13),
                new RequiredMaterial("Copper", 0.07),
            }, "Computer"));
            blueprints.Add("ConstructionComp", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Iron", 2.67),
            }, "Construction"));
            blueprints.Add("CopperWire", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Copper", 0.33),
                new RequiredMaterial("Plastic", 0.01),
            }, "CopperWire"));
            blueprints.Add("CPU", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Gallium", 0.67),
                new RequiredMaterial("Copper", 0.33),
                new RequiredMaterial("Aluminium", 0.67)
            }, "10GHzCPU"));
            blueprints.Add("DetectorComponent", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Iron", 1.67),
                new RequiredMaterial("Copper", 2.67),
                new RequiredMaterial("Nickel", 2.67)
            }, "DetectionComp"));
            blueprints.Add("DilithiumMatrix", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Dilithium", 1.0)
            }, "DilithiumMatrix"));
            blueprints.Add("Display", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Aluminium", 0.67),
                new RequiredMaterial("Copper", 0.67),
                new RequiredMaterial("Silicon", 1.67)
            }, "Display"));
            blueprints.Add("DuraniumGrid", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Duranium", 0.33)
            }, "DuraniumGrid"));
            blueprints.Add("Explosives", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Lithium", 0.33),
                new RequiredMaterial("Gunpowder", 0.33),
                new RequiredMaterial("Uranium", 0.07),
                new RequiredMaterial("Plutonium", 0.07),
            }, "Explosives"));
            blueprints.Add("Flashpowder", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("PotassiumPowder", 0.10),
                new RequiredMaterial("ChlorinePowder", 0.10),
                new RequiredMaterial("Aluminium", 0.07),
                new RequiredMaterial("MagnesiumPowder", 0.07),
            }, "FlashPowder"));
            blueprints.Add("GaAsSolarCell", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Gallium", 1.0),
                new RequiredMaterial("Aluminium", 0.67),
                new RequiredMaterial("Silicon", 1.33),
                new RequiredMaterial("Copper", 0.67),
            }, "GaAsSolarCell"));
            blueprints.Add("Girder", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Iron", 2.0)
            }, "Girder"));
            blueprints.Add("NeuralTransceiver", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Nickel", 5.0),
                new RequiredMaterial("Gold", 1.0),
                new RequiredMaterial("Silicon", 1.0),
                new RequiredMaterial("Silver", 2.0),
                new RequiredMaterial("Gravel", 6.0),
                new RequiredMaterial("Iron", 33.33)
            }, "NeuralTransceiver"));
            blueprints.Add("Motor", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Iron", 1.67),
                new RequiredMaterial("Nickel", 4.0),
                new RequiredMaterial("Copper", 3.33)
            }, "Motor"));
            blueprints.Add("MetalGrid", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Iron", 4.0),
                new RequiredMaterial("Nickel", 1.67),
                new RequiredMaterial("Cobalt", 1.0)
            }, "MetalGrid"));
            blueprints.Add("MedicalComp", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Copper", 6.67),
                new RequiredMaterial("Aluminium", 3.33),
                new RequiredMaterial("Lithium", 3.33),
                new RequiredMaterial("Silver", 6.67),
                new RequiredMaterial("Iron", 13.33),
                new RequiredMaterial("Nickel", 13.33)
            }, "Medical"));
	    blueprints.Add("TritaniumPlate", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Tritanium", 3.33),
            }, "TritaniumPlate"));
	    blueprints.Add("WeaponPaintGun", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Iron", 1.33),
				new RequiredMaterial("Nickel", 0.33),
				new RequiredMaterial("Silicon", 0.67),
            }, "PaintGun"));
		blueprints.Add("PaintGunMag", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Gravel", 0.01),
            }, "PaintGunMag"));
		blueprints.Add("WeaponConcreteTool", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Iron", 1.67),
				new RequiredMaterial("Nickel", 0.67),
				new RequiredMaterial("Silicon", 1),
            }, "ConcreteTool"));
		blueprints.Add("ConcreteMix", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Gravel", 3.33),
            }, "ConcreteMix"));
	    blueprints.Add("Petroleum", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Oil", 1.2),
            }, "Petrol_Gasoline"));
	    blueprints.Add("Plastic", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Oil", 1.2),
            }, "Plastic"));
	    blueprints.Add("Rubber", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Oil", 1),
                new RequiredMaterial("Silicon", 0.2),
                new RequiredMaterial("Carbon", 0.2),
            }, "SyntheticRubber"));
	    blueprints.Add("DepletedUranium", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("SpentFuel", 1.5),
                new RequiredMaterial("Ice", 2),
                new RequiredMaterial("Sulfur", 1),
            }, "DepletedUranium_Plutonium"));
	    blueprints.Add("Gunpowder", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Nitre", 0.17),
                new RequiredMaterial("Carbon", 0.1),
                new RequiredMaterial("Sulfur", 0.07),
            }, "Gunpowder"));
	    blueprints.Add("Position0100_Welder2", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Iron", 1.67),
                new RequiredMaterial("Nickel", 0.33),
                new RequiredMaterial("Cobalt", 0.07),
                new RequiredMaterial("Silicon", 0.67),
            }, "EnhancedWelder"));
	    blueprints.Add("Position0110_Welder3", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Iron", 1.67),
                new RequiredMaterial("Nickel", 0.33),
                new RequiredMaterial("Cobalt", 0.07),
                new RequiredMaterial("Silver", 0.67),
            }, "ProficientWelder"));
	    blueprints.Add("Position0020_AngleGrinder2", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Iron", 1),
                new RequiredMaterial("Nickel", 0.33),
                new RequiredMaterial("Cobalt", 0.67),
                new RequiredMaterial("Silicon", 2),
            }, "EnhancedGrinder"));
	    blueprints.Add("Position0030_AngleGrinder3", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Iron", 1),
                new RequiredMaterial("Nickel", 0.33),
                new RequiredMaterial("Cobalt", 0.33),
                new RequiredMaterial("Silicon", 2),
                new RequiredMaterial("Silver", 0.67),
            }, "ProficientGrinder"));
	    blueprints.Add("Position0060_HandDrill2", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Iron", 6.67),
                new RequiredMaterial("Nickel", 1),
                new RequiredMaterial("Silicon", 1.67),
            }, "EnhancedHandDrill"));
	    blueprints.Add("Position0070_HandDrill3", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Iron", 6.67),
                new RequiredMaterial("Nickel", 1),
                new RequiredMaterial("Silicon", 1),
                new RequiredMaterial("Silver", 0.67),
            }, "ProficientHandDrill"));
	    blueprints.Add("Position0010_OxygenBottle", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Iron", 26.67),
                new RequiredMaterial("Nickel", 10),
                new RequiredMaterial("Silicon", 3.33),
            }, "OxygenBottle"));
	    blueprints.Add("Position0020_HydrogenBottle", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Iron", 26.67),
                new RequiredMaterial("Nickel", 10),
                new RequiredMaterial("Silicon", 3.33),
            }, "HydrogenBottle"));
		blueprints.Add("TranswarpCoil", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Iron", 0.33),
                new RequiredMaterial("Nickel", 0.33),
                new RequiredMaterial("Silver", 0.33),
                new RequiredMaterial("Gold", 0.33),
                new RequiredMaterial("Cobalt", 0.33),
                new RequiredMaterial("Silicon", 0.33),
                new RequiredMaterial("MagnesiumPowder", 0.33),
                new RequiredMaterial("Platinum", 0.33),
                new RequiredMaterial("Uranium", 0.33),
            }, "TranswarpCoil"));
            blueprints.Add("Torpedo_Casing", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Tritanium", 3.33),
				new RequiredMaterial("Duranium", 0.33),
				new RequiredMaterial("Aluminium", 0.17),
				new RequiredMaterial("Iron", 0.17),
				new RequiredMaterial("Nickel", 0.17),
				new RequiredMaterial("Silicon", 0.11),
				new RequiredMaterial("MagnesiumPowder", 0.08),
				new RequiredMaterial("Cobalt", 0.08),
				new RequiredMaterial("Silver", 0.03),
				new RequiredMaterial("Gold", 0.03),
            }, "Torpedo_Casing"));
	    blueprints.Add("Torpedo_Fuel_Cell", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Tritanium", 3.33),
				new RequiredMaterial("Duranium", 0.03),
				new RequiredMaterial("Aluminium", 0.33),
				new RequiredMaterial("Iron", 0.33),
				new RequiredMaterial("Nickel", 0.17),
				new RequiredMaterial("Silicon", 0.17),
				new RequiredMaterial("MagnesiumPowder", 0.11),
				new RequiredMaterial("Cobalt", 0.11),
				new RequiredMaterial("Silver", 0.08),
				new RequiredMaterial("Gold", 0.08),
            }, "Torpedo_Fuel_Cell"));
	    blueprints.Add("Torpedo_Guidance", new Component(new List<RequiredMaterial>
            {
                new RequiredMaterial("Tritanium", 3.33),
				new RequiredMaterial("Duranium", 0.08),
				new RequiredMaterial("Aluminium", 0.03),
				new RequiredMaterial("Iron", 0.03),
				new RequiredMaterial("Nickel", 0.33),
				new RequiredMaterial("Silicon", 0.33),
				new RequiredMaterial("MagnesiumPowder", 0.17),
				new RequiredMaterial("Cobalt", 0.17),
				new RequiredMaterial("Silver", 0.11),
				new RequiredMaterial("Gold", 0.11),
            }, "Torpedo_Guidance"));
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
            // The main entry point of the script, invoked every time
            // one of the programmable block's Run actions are invoked,
            // or the script updates itself. The updateSource argument
            // describes where the update came from. Be aware that the
            // updateSource is a  bitfield  and might contain more than 
            // one update type.
            // 
            // The method itself is required, but the arguments above
            // can be removed if not needed.

            if((updateType & UpdateType.Terminal) != 0)
            {
                RunCommand(argument);
            }

            if ((updateType & UpdateType.Update10) != 0)
            {
                try
                {
                    string m = "";

                    switch (currentState)
                    {
                        case States.QueueSelection:
                            m += SelectOption();
                            break;
                        case States.DisplaySelection:
                            m += CalculateNeedResources(GetOption()) + '\n' + '\n';
                            m += "to go back use custom data";
                            break;
                    }

                    ReadChoice();
                    lcd.WriteText(m);
                }
                catch { lcd?.WriteText("Waiting for refineries\n, assemblers and cargo blocks"); }
            }
        }

        private string GetOption()
        {
            try
            {
                int amount = 0;
                string[] split = options[currentOption].Split(',');
                string option_ = split[0];
                try { amount = int.Parse(split[1].Trim()); } catch { }

                foreach (IMyAssembler assem in assemblers)
                {
                    if (assem.CustomName.Contains("[Main]"))
                    {
                        List<MyProductionItem> productionItems = new List<MyProductionItem>();
                        assem.GetQueue(productionItems);

                        amount = (int)productionItems[currentOption].Amount;
                    }
                }
                
                return option_ + ',' + amount;
            }
            catch
            {
                WriteCustomData(false);
                currentOption = 0;
                currentState = States.QueueSelection;
                return " ";
            }
        }

        private string customData = "";
        private bool Changed(string _updatedData)
        {
            return !customData.Equals(_updatedData);
        }

        private void WriteCustomData(bool back = false)
        {
            IMyTerminalBlock lcd_tb = lcd as IMyTerminalBlock;
            string message = "Welcome to ResourceCalculatorOS\nEnjoy your stay :)\n\n";
            if(!back) message += "Select Option: \n";
            if(back) message += "Go back? (Y/N): \n";
            lcd_tb.CustomData = message;
            customData = message;
        }

        private void ReadChoice()
        {
            IMyTerminalBlock lcd_tb = lcd as IMyTerminalBlock;
            string data = lcd_tb.CustomData;

            if(Changed(data))
            {
                string[] lines = data.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                int option = 0;
                bool flag = false;


                foreach (string line in lines) 
                {
                    string o = "";
                    
                    try { o = line.Split(':')[1]; }
                    catch { }

                    try
                    {
                        if (line.Contains("Select Option:"))
                            option = int.Parse(o);

                        else if (line.Contains("Go back? (Y/N):"))
                            flag = o.ToLower().Contains("y");
                    }
                    catch { Echo("Failure to parse"); }
                }

                if(!flag)
                {
                    WriteCustomData(true);
                    currentOption = option;
                    currentState = States.DisplaySelection;
                }
                else
                {
                    WriteCustomData(false);
                    currentOption = 0;
                    currentState = States.QueueSelection;
                }

                Echo(option.ToString() + " " + flag + " " + currentState.ToString());
                Echo("Changes Made!");
            }
        }

        private string SelectOption()
        {
            options.Clear();
            string m = "Please Select an Option in Custom Data: \n\n";

            //CalculateNeedResources(blueprint_cached);
            foreach (IMyAssembler assem in assemblers)
            {
                if (assem.CustomName.Contains("[Main]"))
                {
                    List<MyProductionItem> productionItems = new List<MyProductionItem>();
                    assem.GetQueue(productionItems);
                    for (int i = 0; i < productionItems.Count; i++)
                    {
                        MyProductionItem productionItem = productionItems[i];
                        string product = productionItem.BlueprintId.SubtypeName;

                        string blueprint = product + ',' + productionItem.Amount;

                        m += $"Option: {i} {HandleEdgeCases(SearchForBlueprint(product).subtypeid)}\n";

                        options.Add(blueprint);

                        Echo(product);
                    }
                }
            }

            return m;
        }

        private List<IMyCargoContainer> GetCargo()
        {
            List<IMyCargoContainer> cargo_ = new List<IMyCargoContainer>();
            GridTerminalSystem.GetBlocksOfType(cargo_);
            return cargo_;
        }
        private List<IMyRefinery> GetRefineries()
        {
            List<IMyRefinery> refinery_ = new List<IMyRefinery>();
            GridTerminalSystem.GetBlocksOfType(refinery_);
            return refinery_;
        }
        private List<IMyAssembler> GetAssemblers()
        {
            List<IMyAssembler> assemblers_ = new List<IMyAssembler>();
            GridTerminalSystem.GetBlocksOfType(assemblers_);
            return assemblers_;
        }

        private void RunCommand(string _arg)
        {
            //blueprint_cached = _arg;
            //lcd.WriteText(CalculateNeedResources(_arg));
        }

        private string CalculateNeedResources(string blueprint_)
        {
            try
            {
                // superconductor,10
                string[] split = blueprint_.Split(',');
                string first = split[0];
                double value = double.Parse(split[1]);

                Component blueprint = SearchForBlueprint(first);

                string message = $"Resources need for {HandleEdgeCases(blueprint.subtypeid)} x {value}\n\n";

                string mat = "Material".PadRight(padAmount);
                string req = "Required".PadRight(padAmount);
                string ned = "Needed".PadRight(padAmount);

                message += mat + " |  " + req + "|   " + ned + '\n';
                message += "-----------|------------|-----------\n";

                foreach (RequiredMaterial mat_ in blueprint.requiredMaterials)
                {
                    // becuase keen is weird
                    double required_ = (mat_.Amount * value) + 10;
                    string name = mat_.Name;
                    double stored = 0;

                    foreach (IMyCargoContainer container in cargoContainers)
                    {
                        IMyInventory inventory = container.GetInventory();
                        stored += (double)inventory.GetItemAmount(MyItemType.MakeIngot(name));
                    }

                    foreach (IMyRefinery refinery in refineries)
                    {
                        IMyInventory inventory = refinery.OutputInventory;
                        stored += (double)inventory.GetItemAmount(MyItemType.MakeIngot(name));
                    }

                    foreach (IMyAssembler assembler in assemblers)
                    {
                        IMyInventory inventory = assembler.InputInventory;
                        stored += (double)inventory.GetItemAmount(MyItemType.MakeIngot(name));
                    }

                    double needed = required_ - stored;

                    needed = Math.Round(needed, 2);
                    required_ = Math.Round(required_, 2);

                    needed = needed < 0 ? 0 : needed;

                    string needed_, required;

                    required = ConvertToThousandths(required_);

                    needed_ = ConvertToThousandths(needed);

                    message += FormatRow(TruncateString(name), required, needed_);
                }

                return message; //lcd.WriteText(message);
            }

            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        private string HandleEdgeCases(string name)
        {
            switch(name.ToLower())
            {
                case "shieldcomponentbp":
                    return "FieldEmitter";
                case "gravitygeneratorcomponent":
                    return "GravityComp";
                case "reactorcomponent":
                    return "ReactorComp";
                case "thrustcomponent":
                    return "Thruster";
            }

            return name;
        }

        private string TruncateString(string _str)
        {
            try
            {
                if (_str.Length >= padAmount)
                {
                    int splitIndex = _str.ToList().FindIndex(1, char.IsUpper);

                    string first = _str.Substring(0, splitIndex).Substring(0, 3);

                    string second = _str.Substring(splitIndex);

                    return first + second.Substring(0, second.Length - 4) + ".";
                }

                return _str;
            }
            catch { return _str; }
        }

        private string ConvertToThousandths(double number)
        {
            string suffix = number >= 1000 ? "K" : "";
            number = number >= 1000 ? number / 1000 : number;
            return $"{number:0.00}{suffix}";
        }

        private string FormatRow(string material, string required, string needed)
        {
            // Pad the material name to the left, and numbers to the right
            material = material.PadRight(padAmount);
            required = required.PadLeft(padAmount);
            needed = needed.PadLeft(padAmount);
            return $"{material} | {required} | {needed}\n";
        }

        private Component SearchForBlueprint(string _name)
        {
            string nameSplit = _name;
            try
            {
                if (!_name.ToLower().Contains("torpedo"))
                {
                    int splitIndex = _name.ToList().FindIndex(1, char.IsUpper);
                    nameSplit = _name.Substring(0, splitIndex);
                    string nameSplit2 = _name.Substring(splitIndex).ToLower();
                    if (nameSplit.ToLower().Contains("steel") && (nameSplit2.Contains("plate") || nameSplit2.Contains("girder")))
                        nameSplit = nameSplit.ToLower() + nameSplit2;

                }
                else
                {
                    string[] str = _name.Split('_');
                    string newName = "";
                    foreach (string str2 in str)
                        newName += str2;

                    nameSplit = newName.ToLower();
                }
            }
            catch { }

            string nameLower = nameSplit.ToLower();
            string[] searchTerms = nameLower.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            Component bestMatch = DEFAULT; // Initialize with a default value
            int maxMatches = 0;
            bool isMatchFound = false; // Flag to track if a match is found

            foreach (var blueprint in blueprints)
            {
                string keyLower = blueprint.Key.ToLower();
                string valueLower = blueprint.Value.subtypeid.ToLower();

                int matches = searchTerms.Count(term => keyLower.Contains(term) || valueLower.Contains(term));

                if (matches > maxMatches)
                {
                    bestMatch = blueprint.Value;
                    maxMatches = matches;
                    isMatchFound = true; // Set flag to true when a match is found
                }
            }

            if (isMatchFound)
            {
                Echo($"Best match: {bestMatch.subtypeid}");
                return bestMatch;
            }

            return DEFAULT; // Return a default Component if no match is found
        }
    }
}
