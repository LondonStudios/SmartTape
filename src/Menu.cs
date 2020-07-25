using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using NativeUI;

namespace Delta
{
    public static class Menu
    {
        private static MenuPool menuPool;
        private static UIMenu mainMenu;

        // Made by London Studios
        static Menu()
        {
            menuPool = new MenuPool()
            {
                MouseEdgeEnabled = false,
                ControlDisablingEnabled = false
            };
            mainMenu = new UIMenu("Smart Tape", "Manage tape")
            {
                MouseControlsEnabled = false,
                ControlDisablingEnabled = false
            };
            menuPool.Add(mainMenu);
            TapeMenu(mainMenu);
            menuPool.RefreshIndex();
        }

        internal static async void Toggle()
        {
            if (menuPool.IsAnyMenuOpen())
            {
                menuPool.CloseAllMenus();
            }
            else
            {
                mainMenu.Visible = true;
                while (menuPool.IsAnyMenuOpen())
                {
                    menuPool.ProcessMenus();
                    await BaseScript.Delay(0);   
                }
            }
        }

        private static void TapeMenu(UIMenu menu)
        {
            var newTape = menuPool.AddSubMenu(menu, "Create new tape");
            newTape.MouseControlsEnabled = false;
            newTape.ControlDisablingEnabled = false;
            var length = new UIMenuSliderProgressItem("Tape Length", 35, 10);
            newTape.AddItem(length);

            var policeTape = new UIMenuItem(Main.tape1Name, $"Place {Main.tape1Name} tape down");
            newTape.AddItem(policeTape);

            var innerCordonTape = new UIMenuItem(Main.tape2Name, $"Place {Main.tape2Name} tape down");
            newTape.AddItem(innerCordonTape);

            var fireTape = new UIMenuItem(Main.tape3Name, $"Place {Main.tape3Name} tape down");
            newTape.AddItem(fireTape);

            var manageTapes = menuPool.AddSubMenu(menu, "Manage tapes");

            var moveTape = new UIMenuItem("Move Tape", "Move a nearby tape");
            manageTapes.AddItem(moveTape);

            var deleteTape = new UIMenuItem("Delete Tape", "Delete a nearby tape");
            manageTapes.AddItem(deleteTape);

            manageTapes.MouseControlsEnabled = false;

            manageTapes.OnItemSelect += (sender, item, index) =>
            {
                if (item == moveTape)
                {
                    Main.MoveTape();
                }
                else if (item == deleteTape)
                {
                    Main.DeleteTape();
                }
                Toggle();
            };

            newTape.OnItemSelect += (sender, item, index) =>
            {
                if (item == policeTape)
                {
                    Main.startPosition = GetEntityCoords(PlayerPedId(), true);
                    Main.CalculateTape(Main.objects.Count(), GetHashKey("p_clothtarp_s"), length.Value);
                }
                else if (item == fireTape)
                {
                    Main.startPosition = GetEntityCoords(PlayerPedId(), true);
                    Main.CalculateTape(Main.objects.Count(), GetHashKey("prop_fire_tape"), length.Value);
                }
                else if (item == innerCordonTape)
                {
                    Main.startPosition = GetEntityCoords(PlayerPedId(), true);
                    Main.CalculateTape(Main.objects.Count(), GetHashKey("prop_cordon_tape"), length.Value);
                }
                Toggle();
            };
        }
    }
}
