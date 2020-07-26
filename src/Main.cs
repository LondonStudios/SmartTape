using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using SharpConfig;

namespace Delta
{
    public class Main : BaseScript
    {
        public static Dictionary<int, List<int>> objects = new Dictionary<int, List<int>> { };

        public static Dictionary<int, Vector3> locations = new Dictionary<int, Vector3> { };

        public static Vector3 startPosition;
        public static Vector3 endPosition;

        public static float direction;

        public static string tape1Name = "Police Tape";
        public static string tape2Name = "Inner Cordon Tape";
        public static string tape3Name = "Fire Tape";

        // Made by London Studios
        public Main()
        {
            Request(GetHashKey("p_clothtarp_s"));
            Request(GetHashKey("prop_fire_tape"));
            Request(GetHashKey("prop_cordon_tape"));
            TriggerEvent("chat:addSuggestion", "/tape", "Opens the tape management menu");
            ReadConfig();
            RegisterKeyMapping("tape", "Opens the tape management menu", "KEYBOARD", "F10");
        }

        private async void Request(int model)
        {
            while (!HasModelLoaded((uint)model))
            {
                RequestModel((uint)model);
                await Delay(0);
            }
        }

        [Command("tape")]
        private void PoliceTape()
        {
            Menu.Toggle();
        }

        private void ReadConfig()
        {
            var data = LoadResourceFile(GetCurrentResourceName(), "config.ini");
            if (Configuration.LoadFromString(data).Contains("SmartTape") == true)
            {
                Configuration loaded = Configuration.LoadFromString(data);

                tape1Name = loaded["SmartTape"]["Tape1"].StringValue;
                tape2Name = loaded["SmartTape"]["Tape2"].StringValue;
                tape3Name = loaded["SmartTape"]["Tape3"].StringValue;
            }
        }

        public static void CalculateTape(int id, int model, int amount = 0)
        {
            
            float groundZ = 0f;
            GetGroundZFor_3dCoord(startPosition.X, startPosition.Y, startPosition.Z, ref groundZ, false);
            var old = startPosition;
            startPosition = new Vector3(startPosition.X, startPosition.Y, (groundZ + 3f));

            if (amount == 0)
            {
                amount = Amount(old, endPosition);

            }
            objects.Add(id, new List<int> { });
            locations.Add(id, startPosition);
            if (amount > 35)
            {

            }
            else
            {
                var oldObject = NewObject(id, model);
                for (int i = 0; i < amount - 1; i++)
                {
                    var newObject = NewObject(id, model);
                    AttachObjects(newObject, oldObject);
                    oldObject = newObject;
                }
                MoveTape(id);
                var networkId = ObjToNet(objects[id][0]);
                SetNetworkIdExistsOnAllMachines(networkId, true);
            }  
        }

        public static async void MoveTape(int index = -1)
        {
            var position = GetEntityCoords(PlayerPedId(), true);
            foreach(var location in locations)
            {
                if (Vdist(position.X, position.Y, position.Z, location.Value.X, location.Value.Y, location.Value.Z) < 10.0f)
                {
                    if (index == -1)
                    {
                        index = location.Key;
                    }
                    DisplayTopNotification("Press ~INPUT_PICKUP~ to stop moving the tape.");
                    var entity = objects[index][0];
                    var coords = new Vector3(0, 0, 0);
                    var height = 0.2f;
                    var rotation = GetEntityHeading(PlayerPedId());
                    DisableControlAction(1, 27, true);
                    DisableControlAction(1, 173, true);
                    DisableControlAction(1, 174, true);
                    DisableControlAction(1, 175, true);

                    var length = objects[index].Count();
                    var right = -(length * 0.423f / 2f);
                    while (!IsControlJustPressed(1, 51))
                    {
                        position = GetEntityCoords(PlayerPedId(), true);
                        rotation = GetEntityHeading(PlayerPedId());
                        coords = GetOffsetFromEntityInWorldCoords(PlayerPedId(), right, 2f, 0f);
                        SetEntityCoords(entity, coords.X, coords.Y, position.Z + height, true, true, true, false);
                        SetEntityRotation(entity, rotation - 90f, 90.0f, 180f, 1, true);
                        await Delay(0);

                        if (IsControlJustPressed(1, 27))
                        {
                            height = height + 0.15f;
                        }
                        else if (IsControlJustPressed(1, 173))
                        {
                            height = height - 0.15f;
                        }
                        else if (IsControlJustPressed(1, 174))
                        {
                            rotation = rotation + 10f;
                        }
                        else if (IsControlJustPressed(1, 175))
                        {
                            rotation = rotation - 10f;
                        }
                    }
                    DisableControlAction(1, 27, false);
                    DisableControlAction(1, 173, false);
                    DisableControlAction(1, 174, false);
                    DisableControlAction(1, 175, false);
                    coords = GetOffsetFromEntityInWorldCoords(PlayerPedId(), right, 1.8f, 0f);
                    SetEntityCoords(entity, coords.X, coords.Y, position.Z + height, true, true, true, false);
                    var oldLocation = locations[index];
                    locations[index] = coords;
                    DisplayTopNotification("Tape moved.");

                    break;
                }
            }
            await Delay(0);
        }
        public static void DeleteTape(int index = -1)
        {
            var position = GetEntityCoords(PlayerPedId(), true);
            foreach (var location in locations)
            {
                if (Vdist(position.X, position.Y, position.Z, location.Value.X, location.Value.Y, location.Value.Z) < 10.0f)
                {
                    if (index == -1)
                    {
                        index = location.Key;
                    }

                    var entity = objects[index][0];

                    SetEntityVisible(entity, false, false);
                    DeleteEntity(ref entity);
                    DisplayTopNotification("Tape deleted");
                    objects.Remove(index);
                    locations.Remove(index);
                    break;
                }      
            }
        }

        private static void DisplayTopNotification(string text)
        {
            BeginTextCommandDisplayHelp("STRING");
            AddTextComponentSubstringPlayerName(text);
            SetNotificationTextEntry("STRING");
            EndTextCommandDisplayHelp(0, false, true, -1);
        }

        public static int NewObject(int id, int modelHash)
        {
            RequestModel((uint)modelHash);
            int objHandle = CreateObject(modelHash, startPosition.X, startPosition.Y, startPosition.Z, true, true, true);
            FreezeEntityPosition(objHandle, true);
            objects[id].Add(objHandle);
            SetEntityCompletelyDisableCollision(objHandle, true, true);
            SetEntityCollision(objHandle, false, true);
            
            return objHandle;
        }

        private static void AttachObjects(int firstEntity, int newEntity)
        { 
            //-0.423
            AttachEntityToEntity(firstEntity, newEntity, 0, 0.0f, -0.42f, 0, 0, 0, 0, false, false, false, false, 0, true);
            SetEntityRotation(newEntity, direction, 90.0f, 180f, 1, true);
        }

        private static int Amount(Vector3 start, Vector3 end)
        {
            var distance = Vdist(start.X, start.Y, start.Z, end.X, end.Y, end.Z);
            var rounded = Convert.ToInt32(Math.Round((distance / 0.423), 0));
            return rounded;
        }
    }
}
