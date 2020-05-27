using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MenuAPI;
using Newtonsoft.Json;
using CitizenFX.Core;
using static CitizenFX.Core.UI.Screen;
using static CitizenFX.Core.Native.API;
using static vMenuClient.CommonFunctions;
using static vMenuShared.PermissionsManager;

namespace vMenuClient
{
    public class OnlinePlayers
    {
        public List<int> PlayersWaypointList = new List<int>();

        // Menu variable, will be defined in CreateMenu()
        private Menu menu;

        Menu playerMenu = new Menu("Joueurs en ligne", "Joueurs:");
        Player currentPlayer = new Player(Game.Player.Handle);


        /// <summary>
        /// Creates the menu.
        /// </summary>
        private void CreateMenu()
        {
            // Create the menu.
            menu = new Menu("Administration", "Joueurs en ligne") { };
            menu.CounterPreText = "Joueurs : ";

            MenuController.AddSubmenu(menu, playerMenu);

            MenuItem sendMessage = new MenuItem("Envoyer un message privé", "");
            MenuItem teleport = new MenuItem("Téléportation au joueur", "");
            MenuItem teleportVeh = new MenuItem("Téléportation au véhicule du joueur", "");
            MenuItem summon = new MenuItem("Téléporte le joueur sur vous", "");
            MenuItem toggleGPS = new MenuItem("Point GPS", "");
            MenuItem spectate = new MenuItem("Mode Spectateur", "");
            MenuItem printIdentifiers = new MenuItem("Affiche Identifiers", "");
            //MenuItem freezePlayer = new MenuItem("Freeze le joueur", "");
            MenuItem kill = new MenuItem("~r~Tue le joueur", "Kill this player, note they will receive a notification saying that you killed them. It will also be logged in the Staff Actions log.");
            MenuItem kick = new MenuItem("~r~Kick le joueur ", "Kick the player from the server.");
            MenuItem ban = new MenuItem("~r~Bannir le joueur permanent", "Ban this player permanently from the server. Are you sure you want to do this? You can specify the ban reason after clicking this button.");
            MenuItem tempban = new MenuItem("~r~Bannir le joueur temporairement", "Give this player a tempban of up to 30 days (max). You can specify duration and ban reason after clicking this button.");

            // always allowed
            playerMenu.AddMenuItem(sendMessage);
            // permissions specific
            if (IsAllowed(Permission.OPTeleport))
            {
                playerMenu.AddMenuItem(teleport);
                playerMenu.AddMenuItem(teleportVeh);
            }
            if (IsAllowed(Permission.OPSummon))
            {
                playerMenu.AddMenuItem(summon);
            }
            if (IsAllowed(Permission.OPSpectate))
            {
                playerMenu.AddMenuItem(spectate);
            }
            if (IsAllowed(Permission.OPWaypoint))
            {
                playerMenu.AddMenuItem(toggleGPS);
            }
            if (IsAllowed(Permission.OPIdentifiers))
            {
                playerMenu.AddMenuItem(printIdentifiers);
            }
            if (IsAllowed(Permission.OPKill))
            {
                playerMenu.AddMenuItem(kill);
            }
            if (IsAllowed(Permission.OPKick))
            {
                playerMenu.AddMenuItem(kick);
            }
            if (IsAllowed(Permission.OPTempBan))
            {
                playerMenu.AddMenuItem(tempban);
            }
            if (IsAllowed(Permission.OPPermBan))
            {
                playerMenu.AddMenuItem(ban);
                ban.LeftIcon = MenuItem.Icon.WARNING;
            }

            playerMenu.OnMenuClose += (sender) =>
            {
                playerMenu.RefreshIndex();
                ban.Label = "";
            };

            playerMenu.OnIndexChange += (sender, oldItem, newItem, oldIndex, newIndex) =>
            {
                ban.Label = "";
            };

            // handle button presses for the specific player's menu.
            playerMenu.OnItemSelect += async (sender, item, index) =>
            {
                // send message
                if (item == sendMessage)
                {
                    //if (MainMenu.MiscSettingsMenu != null && !MainMenu.MiscSettingsMenu.MiscDisablePrivateMessages)
                    //{
                        string message = await GetUserInput($"Message privé à {currentPlayer.Name}", 200);
                        if (string.IsNullOrEmpty(message))
                        {
                            Notify.Error(CommonErrors.InvalidInput);
                        }
                        else
                        {
                            TriggerServerEvent("vMenu:SendMessageToPlayer", currentPlayer.ServerId, message);
                            PrivateMessage(currentPlayer.ServerId.ToString(), message, true);
                        }
                    //}
                    //else
                    //{
                    //    Notify.Error("You can't send a private message if you have private messages disabled yourself. Enable them in the Misc Settings menu and try again.");
                    //}



                }
                // teleport (in vehicle) button
                else if (item == teleport || item == teleportVeh)
                {
                    if (Game.Player.Handle != currentPlayer.Handle)
                        TeleportToPlayer(currentPlayer.Handle, item == teleportVeh); // teleport to the player. optionally in the player's vehicle if that button was pressed.
                    else
                        Notify.Error("You can not teleport to yourself!");
                }
                // summon button
                else if (item == summon)
                {
                    if (Game.Player.Handle != currentPlayer.Handle)
                        SummonPlayer(currentPlayer);
                    else
                        Notify.Error("You can't summon yourself.");
                }
                // spectating
                else if (item == spectate)
                {
                    SpectatePlayer(currentPlayer);
                }
                // kill button
                else if (item == kill)
                {
                    KillPlayer(currentPlayer);
                }
                // manage the gps route being clicked.
                else if (item == toggleGPS)
                {
                    bool selectedPedRouteAlreadyActive = false;
                    if (PlayersWaypointList.Count > 0)
                    {
                        if (PlayersWaypointList.Contains(currentPlayer.Handle))
                        {
                            selectedPedRouteAlreadyActive = true;
                        }
                        foreach (int playerId in PlayersWaypointList)
                        {
                            int playerPed = GetPlayerPed(playerId);
                            if (DoesEntityExist(playerPed) && DoesBlipExist(GetBlipFromEntity(playerPed)))
                            {
                                int oldBlip = GetBlipFromEntity(playerPed);
                                SetBlipRoute(oldBlip, false);
                                RemoveBlip(ref oldBlip);
                                Notify.Custom($"~g~GPS route to ~s~<C>{GetSafePlayerName(currentPlayer.Name)}</C>~g~ is now disabled.");
                            }
                        }
                        PlayersWaypointList.Clear();
                    }

                    if (!selectedPedRouteAlreadyActive)
                    {
                        if (currentPlayer.Handle != Game.Player.Handle)
                        {
                            int ped = GetPlayerPed(currentPlayer.Handle);
                            int blip = GetBlipFromEntity(ped);
                            if (DoesBlipExist(blip))
                            {
                                SetBlipColour(blip, 58);
                                SetBlipRouteColour(blip, 58);
                                SetBlipRoute(blip, true);
                            }
                            else
                            {
                                blip = AddBlipForEntity(ped);
                                SetBlipColour(blip, 58);
                                SetBlipRouteColour(blip, 58);
                                SetBlipRoute(blip, true);
                            }
                            PlayersWaypointList.Add(currentPlayer.Handle);
                            Notify.Custom($"~g~GPS route to ~s~<C>{GetSafePlayerName(currentPlayer.Name)}</C>~g~ is now active, press the ~s~Toggle GPS Route~g~ button again to disable the route.");
                        }
                        else
                        {
                            Notify.Error("You can not set a waypoint to yourself.");
                        }
                    }
                }
                else if (item == printIdentifiers)
                {
                    Func<string, string> CallbackFunction = (data) =>
                    {
                        Debug.WriteLine(data);
                        string ids = "~s~";
                        foreach (string s in JsonConvert.DeserializeObject<string[]>(data))
                        {
                            ids += "~n~" + s;
                        }
                        Notify.Custom($"~y~<C>{GetSafePlayerName(currentPlayer.Name)}</C>~g~ Identifiers: {ids}", false);
                        return data;
                    };
                    BaseScript.TriggerServerEvent("vMenu:GetPlayerIdentifiers", currentPlayer.ServerId, CallbackFunction);
                }
                // kick button
                else if (item == kick)
                {
                    if (currentPlayer.Handle != Game.Player.Handle)
                        KickPlayer(currentPlayer, true);
                    else
                        Notify.Error("You cannot kick yourself!");
                }
                // temp ban
                else if (item == tempban)
                {
                    BanPlayer(currentPlayer, false);
                }
                // perm ban
                else if (item == ban)
                {
                    if (ban.Label == "Are you sure?")
                    {
                        ban.Label = "";
                        UpdatePlayerlist();
                        playerMenu.GoBack();
                        BanPlayer(currentPlayer, true);
                    }
                    else
                    {
                        ban.Label = "Are you sure?";
                    }
                }
            };

            // handle button presses in the player list.
            menu.OnItemSelect += (sender, item, index) =>
                {
                    if (MainMenu.PlayersList.ToList().Any(p => p.ServerId.ToString() == item.Label.Replace(" →→→", "").Replace("Server #", "")))
                    {
                        currentPlayer = MainMenu.PlayersList.ToList().Find(p => p.ServerId.ToString() == item.Label.Replace(" →→→", "").Replace("Server #", ""));
                        playerMenu.MenuSubtitle = $"~s~Joueur: ~y~{GetSafePlayerName(currentPlayer.Name)}";
                        playerMenu.CounterPreText = $"[ID: ~y~{currentPlayer.ServerId}~s~] ";
                    }
                    else
                    {
                        playerMenu.GoBack();
                    }
                };
        }

        /// <summary>
        /// Updates the player items.
        /// </summary>
        public void UpdatePlayerlist()
        {
            menu.ClearMenuItems();

            foreach (Player p in MainMenu.PlayersList)
            {
                MenuItem pItem = new MenuItem($"{GetSafePlayerName(p.Name)}", $"Affiche les options du joueur. ID: {p.ServerId}. Local ID: {p.Handle}.")
                {
                    Label = $"Server #{p.ServerId} →→→"
                };
                menu.AddMenuItem(pItem);
                MenuController.BindMenuItem(menu, playerMenu, pItem);
            }

            menu.RefreshIndex();
            //menu.UpdateScaleform();
            playerMenu.RefreshIndex();
            //playerMenu.UpdateScaleform();
        }

        /// <summary>
        /// Checks if the menu exists, if not then it creates it first.
        /// Then returns the menu.
        /// </summary>
        /// <returns>The Online Players Menu</returns>
        public Menu GetMenu()
        {
            if (menu == null)
            {
                CreateMenu();
                return menu;
            }
            else
            {
                UpdatePlayerlist();
                return menu;
            }
        }
    }
}
