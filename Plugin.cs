using BepInEx;
using BepInEx.IL2CPP;
using HarmonyLib;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using SteamworksNative;
using GameLoop = MonoBehaviourPublicObInLi1GagasmLi1GaUnique;
using PlayerMovement = MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique;
using SteamManager = MonoBehaviourPublicObInUIgaStCSBoStcuCSUnique;
using GameManager = MonoBehaviourPublicDi2UIObacspDi2UIObUnique;
using LobbyManager = MonoBehaviourPublicCSDi2UIInstObUIloDiUnique;
using ServerSend = MonoBehaviourPublicInInUnique;
using PlayerManager = MonoBehaviourPublicCSstReshTrheObplBojuUnique;
using GameModeTag = GameModePublicLi1UIUnique;
using GameServer = MonoBehaviourPublicObInCoIE85SiAwVoFoCoUnique;
using LobbySettings = MonoBehaviourPublicObjomaOblogaTMObseprUnique;
using GameModeManager = MonoBehaviourPublicGadealGaLi1pralObInUnique;
using Chatbox = MonoBehaviourPublicRaovTMinTemeColoonCoUnique;
using ServerConfig = ObjectPublicInSiInInInInInInInInUnique;
using Client = ObjectPublicBoInBoCSItBoInSiBySiUnique;
using MapManager = MonoBehaviourPublicObInMamaLi1plMadeMaUnique;
using ServerHandle = MonoBehaviourPublicPlVoUI9GaVoUI9UsPlUnique;

namespace OneVOneMod
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, "1.1.0")]
    public class Plugin : BasePlugin
    {
        // Game state variables
        public static int gameState;
        public static int prevGameState;
        public static int prevMapID = 0;

        // Timer variables
        public static float gameTimer;
        public static int roundTime = 60;

        // Player variables
        public static List<ulong> alivePlayers = new();
        public static Dictionary<ulong, int> playerDictionary = new();
        public static ulong taggedPlayer;
        public static ulong firstTaggedPlayer;
        public static int scoreToWin = 5;

        // Messaging variables
        public static string lastServerMessage;
        public static float commandCoolDown = 0f;

        public override void Load()
        {
            Harmony.CreateAndPatchAll(typeof(Plugin));
            Log.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            Log.LogInfo("Mod created by JMac");
        }

        public static void CheckGameState()
        {
            /*if (LobbyManager.Instance == null || GameManager.Instance == null) return;

            int lobbyManagerState = (int)LobbyManager.Instance.state;
            int gameManagerState = (int)GameManager.Instance.gameMode.modeState;*/

            int lobbyManagerState = 0;
            int gameManagerState = 0;

            if (LobbyManager.Instance) lobbyManagerState = (int)LobbyManager.Instance.state;
            if (GameManager.Instance) gameManagerState = (int)GameManager.Instance.gameMode.modeState;

            if (lobbyManagerState == 0 && gameState != 0) //MainMenu
            {
                gameState = 0;
                prevGameState = 0;
                lastServerMessage = "";
            }
            else if (lobbyManagerState == 2 && gameManagerState == 0 && GetModeID() == 0 && gameState != 1) //Lobby
            {
                gameState = 1;
                prevGameState = 1;
            }
            else if (lobbyManagerState == 1 && alivePlayers.Count == 0 && gameState != 2) //Loading
            {
                gameState = 2;
                prevGameState = 2;
            }
            else if (lobbyManagerState == 2 && gameManagerState == 0 && GetModeID() != 0 && prevGameState != 1 && gameState != 3) //Frozen
            {
                gameState = 3;
                prevGameState = 3;
            }
            else if ((lobbyManagerState == 4 || lobbyManagerState == 2) && gameManagerState == 1 && gameState != 4) //Playing
            {
                gameState = 4;
                prevGameState = 4;
            }
            else if (lobbyManagerState == 2 && gameManagerState == 2 && gameState != 5) //Ended
            {
                gameState = 5;
                prevGameState = 5;
            }
            else if (lobbyManagerState == 4 && (gameManagerState == 2 || gameManagerState == 3) && gameState != 6) //GameOver
            {
                gameState = 6;
                prevGameState = 6;
            }

            //Debug.Log(gameState.ToString());
        }

        public static int GetMapID()
        {
            return LobbyManager.Instance.map.id;
        }

        public static int GetModeID()
        {
            return LobbyManager.Instance.gameMode.id;
        }

        public static int GetNumOfPlayers()
        {
            return GameManager.Instance.activePlayers.count + GameManager.Instance.spectators.count;
        }

        public static int GetPlayersAlive()
        {
            return GameManager.Instance.GetPlayersAlive();
        }

        public static ulong GetMyID()
        {
            return SteamManager.Instance.field_Private_CSteamID_0.m_SteamID;
        }

        public static ulong GetHostID()
        {
            return SteamManager.Instance.field_Private_CSteamID_1.m_SteamID;
        }

        public static Rigidbody GetPlayerRigidBody(ulong id)
        {
            if (id == GetMyID()) return PlayerMovement.prop_MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique_0.GetRb();
            else return GameManager.Instance.activePlayers[id].prop_MonoBehaviourPublicObVeSiVeRiSiAnVeanTrUnique_0.field_Private_Rigidbody_0;
        }

        public static Vector3 GetPlayerRotation(ulong id)
        {
            if (id == GetMyID()) return PlayerInput.Instance.cameraRot;
            else return new Vector3(GameManager.Instance.activePlayers[id].field_Private_MonoBehaviourPublicObVeSiVeRiSiAnVeanTrUnique_0.xRot, GameManager.Instance.activePlayers[id].field_Private_MonoBehaviourPublicObVeSiVeRiSiAnVeanTrUnique_0.yRot, 0f);
        }

        public static List<ulong> GetAlivePlayers()
        {
            List<ulong> list = new();
            foreach (var player in GameManager.Instance.activePlayers)
            {
                if (player == null || player.Value.dead) continue;
                list.Add(player.Key);
            }
            return list;
        }

        public static bool IsHost()
        {
            return SteamManager.Instance.IsLobbyOwner() && !LobbyManager.Instance.Method_Public_Boolean_0();
        }

        public static void CheckPosition(ulong id)
        {
            
            if (prevMapID == 36)// Small Beach
            {
                if (Vector3.Distance(GetPlayerRigidBody(id).position, new Vector3(20.8f, -1.1f, -15.8f)) < 1f)
                {
                    ServerSend.RespawnPlayer(id, new Vector3(19.2f, -1.1f, -17.3f));
                }
                else if (Vector3.Distance(GetPlayerRigidBody(id).position, new Vector3(-10.6f, -4.1f, 14.4f)) < 1f)
                {
                    ServerSend.RespawnPlayer(id, new Vector3(-14.4f, -4.1f, 14.4f));
                }
            }
        }

        public static void ChangeMap()
        {
            int randomMapID = MapManager.Instance.playableMaps[new System.Random().Next(0, MapManager.Instance.playableMaps.Count)].id;
            while (randomMapID == prevMapID && MapManager.Instance.playableMaps.Count > 1)
            {
                randomMapID = MapManager.Instance.playableMaps[new System.Random().Next(0, MapManager.Instance.playableMaps.Count)].id;
            }
            prevMapID = randomMapID;
            ServerSend.LoadMap(randomMapID, 4);
        }

        public static void CheckGameOver()
        {
            if (!IsHost() || gameState != 4) return;

            if (alivePlayers.Count <= 1 || GameManager.Instance.activePlayers.Count == 1)
            {
                ServerSend.GameOver(0);
                Debug.Log("Game Over");
            }
        }

        [HarmonyPatch(typeof(SteamManager), nameof(SteamManager.Start))]
        [HarmonyPostfix]
        public static void SteamManagerStart()
        {
            //__instance.serverNameField.text = "1v1Mod";
            //__instance.maxPlayers.slider.value = 2;

            MapManager.Instance.playableMaps.Clear();
            var defaultMaps = new int[] { 35, 36, 37, 38, 39, 40 };
            foreach (var mapIndex in defaultMaps) MapManager.Instance.playableMaps.Add(MapManager.Instance.maps[mapIndex]);

            GameModeManager.Instance.allPlayableGameModes.Clear();
            GameModeManager.Instance.allPlayableGameModes.Add(GameModeManager.Instance.allGameModes[4]);

            ServerConfig.field_Public_Static_Int32_5 = 5; // round start freeze
            ServerConfig.field_Public_Static_Int32_6 = 5; // round stop cinematic
            ServerConfig.field_Public_Static_Int32_7 = 4; // round end timeout
            ServerConfig.field_Public_Static_Int32_8 = 4; // game over timeout
            //ServerConfig.field_Public_Static_Int32_9 = 5; // load time before kicked
            //ServerConfig.field_Public_Static_Single_0 // speak after death time
        }

        [HarmonyPatch(typeof(SteamManager), nameof(SteamManager.Update))]
        [HarmonyPostfix]
        public static void SteamManagerUpdate()
        {
            CheckGameState();
            if (!IsHost()) return;

            if (gameState == 1)
            {
                foreach (var player in GameManager.Instance.activePlayers)
                {
                    if (player == null || player.Value.waitingReady == false) continue;
                    player.Value.waitingReady = false;
                }
            }

            if (gameState == 1 || gameState == 3 || gameState == 4 || gameState == 5 || gameState == 6)
            {
                commandCoolDown += Time.deltaTime;
            }

            /*if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                //GameLoop.Instance.StartGames();
                //ServerSend.LoadMap(39, 4);
                //ChangeMap();
            }*/
        }

        [HarmonyPatch(typeof(GameMode), nameof(GameMode.Update))]
        [HarmonyPostfix]
        public static void GameModeUpdate(GameMode __instance)
        {
            if (!IsHost()) return;
            alivePlayers = GetAlivePlayers();
            gameTimer = __instance.freezeTimer.field_Private_Single_0;

            if (gameState != 4) return;

            CheckGameOver();

            // Check If Player Is Trying To Glitch
            foreach (ulong id in playerDictionary.Keys)
            {
                CheckPosition(id);
            }
        }

        [HarmonyPatch(typeof(ServerHandle), nameof(ServerHandle.GameRequestToSpawn))]
        [HarmonyPrefix]
        public static void ServerHandleGameRequestToSpawn(ulong param_0)
        {
            if (!IsHost()) return;
            if (playerDictionary.ContainsKey(param_0)) LobbyManager.Instance.GetClient(param_0).field_Public_Boolean_0 = true; // active player
            else LobbyManager.Instance.GetClient(param_0).field_Public_Boolean_0 = false; // active player
        }

        [HarmonyPatch(typeof(GameMode), nameof(GameMode.Init))]
        [HarmonyPostfix]
        public static void GameModeInit()
        {
            if (!IsHost() || GetModeID() != 4) return;

            LobbyManager.Instance.gameMode.shortModeTime = roundTime;
            LobbyManager.Instance.gameMode.longModeTime = roundTime;
            LobbyManager.Instance.gameMode.mediumModeTime = roundTime;
        }

        [HarmonyPatch(typeof(GameLoop), nameof(GameLoop.CheckGameOver))]
        [HarmonyPrefix]
        public static bool GameLoopCheckGameOver()
        {
            if (!IsHost() || GetModeID() != 4) return true;
            return false;
        }

        [HarmonyPatch(typeof(GameModeTag), nameof(GameModeTag.CheckGameOver))]
        [HarmonyPrefix]
        public static bool GameModeTagCheckGameOver()
        {
            if (!IsHost() || GetModeID() != 4) return true;
            return false;
        }

        [HarmonyPatch(typeof(ServerSend), nameof(ServerSend.TagPlayer))]
        [HarmonyPostfix]
        public static void ServerSendTagPlayer(ulong param_1)
        {
            taggedPlayer = param_1;
        }

        [HarmonyPatch(typeof(GameModeTag), nameof(GameModeTag.OnFreezeOver))]
        [HarmonyPrefix]
        public static bool GameModeTagOnFreezeOver()
        {
            if (!IsHost() && GetModeID() != 4) return true;

            ulong randomPlayer = playerDictionary.ElementAt(new System.Random().Next(2)).Key;
            while (randomPlayer == firstTaggedPlayer) randomPlayer = playerDictionary.ElementAt(new System.Random().Next(2)).Key;
            ServerSend.TagPlayer(0, randomPlayer);
            GameServer.ForceGiveWeapon(randomPlayer, 10, 0);
            firstTaggedPlayer = randomPlayer;
            return false;
        }

        [HarmonyPatch(typeof(GameModeTag), nameof(GameModeTag.OnRoundOver))]
        [HarmonyPrefix]
        public static bool GameModeTagOnRoundOver()
        {
            if (!IsHost() || GetModeID() != 4) return true;

            if (!GameManager.Instance.activePlayers[playerDictionary.ElementAt(0).Key].dead && GameManager.Instance.activePlayers[playerDictionary.ElementAt(1).Key].dead)
            {
                playerDictionary[playerDictionary.ElementAt(0).Key]++;
            }
            else if (GameManager.Instance.activePlayers[playerDictionary.ElementAt(0).Key].dead && !GameManager.Instance.activePlayers[playerDictionary.ElementAt(1).Key].dead)
            {
                playerDictionary[playerDictionary.ElementAt(1).Key]++;
            }
            else if (!GameManager.Instance.activePlayers[playerDictionary.ElementAt(0).Key].dead && !GameManager.Instance.activePlayers[playerDictionary.ElementAt(1).Key].dead)
            {
                if (taggedPlayer != playerDictionary.ElementAt(0).Key) playerDictionary[playerDictionary.ElementAt(0).Key]++;
                else playerDictionary[playerDictionary.ElementAt(1).Key]++;
                GameServer.PlayerDied(taggedPlayer, 1, Vector3.zero);
            }

            ServerSend.SendChatMessage(1, GameManager.Instance.activePlayers[playerDictionary.ElementAt(0).Key].username.ToString() + " - " + playerDictionary.ElementAt(0).Value);
            ServerSend.SendChatMessage(1, GameManager.Instance.activePlayers[playerDictionary.ElementAt(1).Key].username.ToString() + " - " + playerDictionary.ElementAt(1).Value);

            if (playerDictionary.ElementAt(0).Value >= scoreToWin)
            {
                ServerSend.SendChatMessage(1, GameManager.Instance.activePlayers[playerDictionary.ElementAt(0).Key].username.ToString() + " wins");
            }
            else if (playerDictionary.ElementAt(1).Value >= scoreToWin)
            {
                ServerSend.SendChatMessage(1, GameManager.Instance.activePlayers[playerDictionary.ElementAt(1).Key].username.ToString() + " wins");
            }

            GameServer.ForceRemoveAllWeapons();
            return false;
        }

        [HarmonyPatch(typeof(ServerSend), nameof(ServerSend.SendWinner))]
        [HarmonyPrefix]
        public static bool ServerSendSendWinner()
        {
            if (!IsHost() || GetModeID() != 4) return true;

            if (playerDictionary.Count < 2 || playerDictionary.ElementAt(0).Value >= scoreToWin || playerDictionary.ElementAt(1).Value >= scoreToWin)
            {
                GameLoop.Instance.RestartLobby();
            }
            else
            {
                ChangeMap();
            }
            return false;
        }

        [HarmonyPatch(typeof(GameLoop), nameof(GameLoop.NextGame))]
        [HarmonyPrefix]
        public static bool GameLoopNextGame()
        {
            if (!IsHost() || GetModeID() != 4) return true;

            if (playerDictionary.Count < 2 || playerDictionary.ElementAt(0).Value >= scoreToWin || playerDictionary.ElementAt(1).Value >= scoreToWin)
            {
                GameLoop.Instance.RestartLobby();
            }
            else
            {
                ChangeMap();
            }
            return false;
        }

        [HarmonyPatch(typeof(GameLoop), nameof(GameLoop.RestartLobby))]
        [HarmonyPrefix]
        public static bool GameLoopRestartLobby()
        {
            if (!IsHost() || GetModeID() != 4) return true;
            if (playerDictionary.Count < 2) return true;

            if (gameState == 6 && playerDictionary.ElementAt(0).Value < scoreToWin && playerDictionary.ElementAt(1).Value < scoreToWin)
            {
                ChangeMap();
                return false;
            }
            else return true;
        }

        [HarmonyPatch(typeof(GameServer), nameof(GameServer.ForceGiveWeapon))]
        [HarmonyPrefix]
        public static bool GameServerForceGiveWeapon(int param_1)
        {
            if (IsHost() && param_1 == 9) return false;
            else return true;
        }

        /*[HarmonyPatch(typeof(Chatbox), nameof(Chatbox.AppendMessage))]
        [HarmonyPrefix]
        public static bool ChatboxAppendMessage(ulong param_1, string param_2, string param_3)
        {
            if (IsHost())
            {
                if (lastServerMessage == param_2) return false;
                if (param_1 == 1 && (param_2.Contains("joined the server") || param_2.Contains("left the server")) && param_3 == "")
                {
                    Debug.Log(param_2);
                    lastServerMessage = param_2;
                    ServerSend.SendChatMessage(1, param_2);
                }
                return true;
            }
            else return true;
        }*/

        [HarmonyPatch(typeof(ServerSend), nameof(ServerSend.SendChatMessage))]
        [HarmonyPostfix]
        public static void ServerSendSendChatMessage(ulong param_0, string param_1)
        {
            if (!IsHost() || !param_1.StartsWith("!") || (param_0 != GetHostID() && commandCoolDown < 1f)) return;
            if (param_0 != GetHostID() && commandCoolDown > 1f) commandCoolDown = 0f;

            string msg = param_1.ToLower();
            ulong hostID = GetHostID();

            if (msg == "!start") // !start
            {
                if (param_0 != hostID)
                {
                    ServerSend.SendChatMessage(1, "Not Authorized");
                    return;
                }

                if (gameState != 1)
                {
                    ServerSend.SendChatMessage(1, "You can only use this command while in the lobby");
                    return;
                }

                if (GameManager.Instance.activePlayers.count != 2)
                {
                    ServerSend.SendChatMessage(1, "You can only use this command if there's 2 people in the lobby");
                    ServerSend.SendChatMessage(1, "Use ![playernumber]v[playernumber] instead");
                    return;
                }

                firstTaggedPlayer = 0;
                playerDictionary.Clear();
                foreach (var player in GameManager.Instance.activePlayers)
                {
                    if (player == null) continue;
                    playerDictionary.Add(player.Key, 0);
                }
                ChangeMap();
            }
            else if (new Regex(@"^!\d+").IsMatch(msg)) // !1v2
            {
                if (param_0 != hostID)
                {
                    ServerSend.SendChatMessage(1, "Not Authorized");
                    return;
                }

                if (gameState != 1)
                {
                    ServerSend.SendChatMessage(1, "You can only use this command while in the lobby");
                    return;
                }

                Regex regex = new Regex(@"!(\d+)v(\d+)");
                Match match = regex.Match(msg);
                if (match.Success)
                {
                    int firstNumber = int.Parse(match.Groups[1].Value);
                    int secondNumber = int.Parse(match.Groups[2].Value);

                    if (firstNumber == secondNumber)
                    {
                        ServerSend.SendChatMessage(1, "Invalid Command - The player numbers cannont be the same");
                        return;
                    }

                    List<ulong> tempList = new();

                    foreach (var player in GameManager.Instance.activePlayers)
                    {
                        if (player == null) continue;
                        if (player.Value.playerNumber == firstNumber || player.Value.playerNumber == secondNumber)
                        {
                            tempList.Add(player.Key);
                        }
                    }

                    if (tempList.Count != 2)
                    {
                        ServerSend.SendChatMessage(1, "Invalid Command - A player number is not in the lobby");
                        return;
                    }

                    firstTaggedPlayer = 0;
                    playerDictionary.Clear();
                    playerDictionary.Add(tempList[0], 0);
                    playerDictionary.Add(tempList[1], 0);
                    ChangeMap();
                }
                else ServerSend.SendChatMessage(1, "Invalid Command Format - Use ![playernumber]v[playernumber]");
            }
            else if (msg == "!rematch") // !rematch
            {
                if (param_0 != hostID)
                {
                    ServerSend.SendChatMessage(1, "Not Authorized");
                    return;
                }

                if (gameState != 1)
                {
                    ServerSend.SendChatMessage(1, "You can only use this command while in the lobby");
                    return;
                }

                if (GameManager.Instance.activePlayers.ContainsKey(playerDictionary.Keys.ElementAt(0)) && GameManager.Instance.activePlayers.ContainsKey(playerDictionary.Keys.ElementAt(1)))
                {
                    firstTaggedPlayer = 0;
                    playerDictionary[playerDictionary.ElementAt(0).Key] = 0;
                    playerDictionary[playerDictionary.ElementAt(1).Key] = 0;
                    ChangeMap();
                }
                else
                {
                    ServerSend.SendChatMessage(1, "Can't rematch cause player(s) no longer in the lobby");
                }
            }
            else if (msg.StartsWith("!resume"))
            {
                if (param_0 != hostID)
                {
                    ServerSend.SendChatMessage(1, "Not Authorized");
                    return;
                }

                if (gameState != 1)
                {
                    ServerSend.SendChatMessage(1, "You can only use this command while in the lobby");
                    return;
                }

                Regex regex = new Regex(@"^!resume (\d+)v(\d+) (\d+)-(\d+)$");
                Match match = regex.Match(msg);
                if (match.Success)
                {
                    int playerOneNumber = int.Parse(match.Groups[1].Value);
                    int playerTwoNumber = int.Parse(match.Groups[2].Value);
                    int playerOneScore = int.Parse(match.Groups[3].Value);
                    int playerTwoScore = int.Parse(match.Groups[4].Value);

                    if (playerOneScore >= scoreToWin || playerTwoScore >= scoreToWin)
                    {
                        ServerSend.SendChatMessage(1, "Invalid Command - Can't resume with a score higher than or equal to the score to win");
                        return;
                    }
                    else if (playerOneScore < 0 || playerTwoScore < 0)
                    {
                        ServerSend.SendChatMessage(1, "Invalid Command - Can't resume with a score that's less than 0");
                        return;
                    }

                    Dictionary<ulong, int> tempDict = new();

                    foreach (var player in GameManager.Instance.activePlayers)
                    {
                        if (player == null) continue;

                        if (player.Value.playerNumber == playerOneNumber)
                        {
                            tempDict.Add(player.Key, playerOneScore);
                        }
                        else if(player.Value.playerNumber == playerTwoNumber)
                        {
                            tempDict.Add(player.Key, playerTwoScore);
                        }
                    }

                    if (tempDict.Count != 2)
                    {
                        ServerSend.SendChatMessage(1, "Invalid Command - A player number is not in the lobby");
                        return;
                    }

                    firstTaggedPlayer = 0;
                    playerDictionary.Clear();
                    playerDictionary = tempDict;
                    ChangeMap();
                }
                else ServerSend.SendChatMessage(1, "Invalid Command Format - Use !resume [playernumber]v[playernumber] [score]-[score]");

            }
            else if (msg == "!reset") // !reset
            {
                if (param_0 != hostID)
                {
                    ServerSend.SendChatMessage(1, "Not Authorized");
                    return;
                }

                //playerDictionary.Clear();
                firstTaggedPlayer = 0;
                GameLoop.Instance.RestartLobby();
            }
            else if (msg.StartsWith("!setscore")) // !setscore
            {
                if (param_0 != hostID)
                {
                    ServerSend.SendChatMessage(1, "Not Authorized");
                    return;
                }

                if (gameState != 1)
                {
                    ServerSend.SendChatMessage(1, "You can only use this command while in the lobby");
                    return;
                }

                Regex regex = new Regex(@"!setscore (\d+)");
                Match match = regex.Match(msg);
                if (match.Success)
                {
                    int temp = int.Parse(match.Groups[1].Value);
                    scoreToWin = temp;
                    ServerSend.SendChatMessage(1, "The score to win is now set to " + scoreToWin);
                }
                else ServerSend.SendChatMessage(1, "Invalid Command Format - Use !setscore [score]");
            }
            else if (msg.StartsWith("!settime")) // !settime
            {
                if (param_0 != hostID)
                {
                    ServerSend.SendChatMessage(1, "Not Authorized");
                    return;
                }

                if (gameState != 1)
                {
                    ServerSend.SendChatMessage(1, "You can only use this command while in the lobby");
                    return;
                }

                Regex regex = new Regex(@"!settime (\d+)");
                Match match = regex.Match(msg);
                if (match.Success)
                {
                    int temp = int.Parse(match.Groups[1].Value);
                    roundTime = temp;
                    ServerSend.SendChatMessage(1, "The round time is now set to " + roundTime);
                }
                else ServerSend.SendChatMessage(1, "Invalid Command Format - Use !settime [time]");
            }
            else if (msg == "!score") // !score
            {
                if (gameState == 3 || gameState == 4 || gameState == 5 || gameState == 6)
                {
                    ServerSend.SendChatMessage(1, GameManager.Instance.activePlayers[playerDictionary.ElementAt(0).Key].username.ToString() + " - " + playerDictionary[playerDictionary.ElementAt(0).Key]);
                    ServerSend.SendChatMessage(1, GameManager.Instance.activePlayers[playerDictionary.ElementAt(1).Key].username.ToString() + " - " + playerDictionary[playerDictionary.ElementAt(1).Key]);
                }
                else
                {
                    ServerSend.SendChatMessage(1, "Can't use that command while in the lobby");
                }

                // if in lobby send the last games score - if theres no last game score then send server message no last game score
            }
            else if (msg == "!help") // !help
            {
                if (param_0 == hostID)
                {
                    ServerSend.SendChatMessage(1, "- !start");
                    ServerSend.SendChatMessage(1, "- ![playernumber]v[playernumber]");
                    ServerSend.SendChatMessage(1, "- !rematch");
                    ServerSend.SendChatMessage(1, "- !resume [playernumber]v[playernumber] [score]-[score]");
                    ServerSend.SendChatMessage(1, "- !reset");
                    ServerSend.SendChatMessage(1, "- !setscore [score]");
                    ServerSend.SendChatMessage(1, "- !settime [time]");
                    ServerSend.SendChatMessage(1, "- !score");
                    //ServerSend.SendChatMessage(1, "- !creator");
                }
                else
                {
                    ServerSend.SendChatMessage(1, "- !score");
                    //ServerSend.SendChatMessage(1, "- !creator");
                }
            }
            else if (msg == "!creator") // !creator
            {
                ServerSend.SendChatMessage(1, "Mod created by JMac");
            }
            else
            {
                ServerSend.SendChatMessage(1, "Invalid Command");
            }
        }

        [HarmonyPatch(typeof(LobbyManager), nameof(LobbyManager.OnPlayerJoinLeaveUpdate))]
        [HarmonyPostfix]
        public static void LobbyManagerOnPlayerJoinLeave(CSteamID param_1, bool param_2)
        {
            if (IsHost() && GetModeID() == 4 && !param_2)
            {
                if (playerDictionary.ContainsKey(param_1.m_SteamID)) playerDictionary.Remove(param_1.m_SteamID);
            }
        }

        [HarmonyPatch(typeof(MonoBehaviourPublicGataInefObInUnique), "Method_Private_Void_GameObject_Boolean_Vector3_Quaternion_0")]
        [HarmonyPatch(typeof(MonoBehaviourPublicCSDi2UIInstObUIloDiUnique), "Method_Private_Void_0")]
        [HarmonyPatch(typeof(MonoBehaviourPublicVesnUnique), "Method_Private_Void_0")]
        [HarmonyPatch(typeof(MonoBehaviourPublicObjomaOblogaTMObseprUnique), "Method_Public_Void_PDM_2")]
        [HarmonyPatch(typeof(MonoBehaviourPublicTeplUnique), "Method_Private_Void_PDM_32")]
        [HarmonyPrefix]
        public static bool Prefix(System.Reflection.MethodBase __originalMethod)
        {
            return false;
        }
    }
}
