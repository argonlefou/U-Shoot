using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using BepInEx;
using HarmonyLib;
using SBK;
using UnityEngine;
using UnityPlugin_BepInEx.Core;
using UnityPlugin_BepInEx_IniFile;

namespace BepInEx_UShoot_Plugin
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class UShoot_Plugin : BaseUnityPlugin
    {
        public const String pluginGuid = "argonlefou.ushoot.tombraider";
        public const String pluginName = "TombRaider_BepInEx_UShoot_Plugin";
        public const String pluginVersion = "1.0.0.0";
        public const String pluginConfigFile = "USHoot_Plugin.ini";

        public static BepInEx.Logging.ManualLogSource MyLogger;

        public static UShoot_Plugin Instance = null;

        //------ Custom Input Data ------//
        //-                             -//
        public static readonly int MAX_PLAYERS = 4;
        private Dictionary<string, int> PlayerButtons = new Dictionary<string, int>();
        public static VirtualPlayerController[] PlayerControllers = new VirtualPlayerController[MAX_PLAYERS];
        public static bool EnableInputHack = false;         //By default, no input hack on the plugin. Enabled once U-Shoot is connected (without -noinput flag)

        private Dictionary<string, int> GameButtons = new Dictionary<string, int>();
        public static VirtualGameController GameController;
        //-                             -//
        //-------------------------------//

        //TCP server data for Inputs/Outputs
        private TcpListener _TcpListener;
        private Thread _TcpListenerThread;
        private TcpClient _TcpClient;
        private int _TcpPort = 33610;
        private static NetworkStream _TcpStream;

        public static TcpOutputData OutputData;
        private TcpOutputData _OutputDataBefore;
        private TcpInputData _InputData;

        public static bool CrossHairVisibility = true;

        //Custom resolution
        public static int ScreenWidth = 1920;
        public static int ScreenHeight = 1080;
        public static bool Fullscreen = true;
        public static bool ForceResolution = false;

        public static bool LocalSave = true;
        public static string GameLanguage = "EN";

        public void Awake()
        {
            Instance = this;

            MyLogger = Logger;
            MyLogger.LogMessage("Plugin Loaded");
            Harmony harmony = new Harmony(pluginGuid);

            MyLogger.LogMessage("Generating [InputData] elements....");
            _InputData = new TcpInputData(MAX_PLAYERS);
            MyLogger.LogMessage("Generating [OutputData] elements....");
            OutputData = new TcpOutputData(MAX_PLAYERS);
            MyLogger.LogMessage("Generating [OutputDataBefore] elements....");
            _OutputDataBefore = new TcpOutputData(MAX_PLAYERS);

            //Virtual Game Controller
            GameButtons.Add("TEST", GameButtons.Count);
            GameButtons.Add("MENU_UP", GameButtons.Count);
            GameButtons.Add("MENU_DOWN", GameButtons.Count);
            GameButtons.Add("MENU_SELECT", GameButtons.Count);
            GameButtons.Add("EXIT", GameButtons.Count);
            GameController = new VirtualGameController(GameButtons);

            //Virtual Player Controllers
            PlayerButtons.Add("START", PlayerButtons.Count);
            PlayerButtons.Add("TRIGGER", PlayerButtons.Count);
            PlayerButtons.Add("RELOAD", PlayerButtons.Count);
            PlayerButtons.Add("COIN", PlayerButtons.Count);
            for (int i = 0; i < MAX_PLAYERS; i++)
            {
                PlayerControllers[i] = new VirtualPlayerController(i, PlayerButtons);
            }

            // Start TcpServer	
            _TcpListenerThread = new Thread(new ThreadStart(TcpClientThreadLoop));
            _TcpListenerThread.IsBackground = true;
            _TcpListenerThread.Start();

            string PluginConfigFilePath = Path.Combine(BepInEx.Paths.ConfigPath, pluginConfigFile);
            MyLogger.LogMessage(Instance.GetType().Name + "." + MethodBase.GetCurrentMethod().Name + "(): Loading custom config : " + PluginConfigFilePath);
            INIFile Plugin_IniFile = new INIFile(PluginConfigFilePath);

            if (File.Exists(Plugin_IniFile.FInfo.FullName))
            {
                try
                {
                    //Can be buggy if folder does not exist already
                    //Plugin_IniFile.IniReadBoolValue("SYSTEM", "SAVE_TO_GAME_FOLDER", ref SaveToGameFolder);

                    Plugin_IniFile.IniReadIntValue("VIDEO", "WIDTH", ref ScreenWidth);
                    Plugin_IniFile.IniReadIntValue("VIDEO", "HEIGHT", ref ScreenHeight);

                    Plugin_IniFile.IniReadBoolValue("VIDEO", "FULLSCREEN", ref Fullscreen);
                    Plugin_IniFile.IniReadBoolValue("VIDEO", "FORCE_RESOLUTION", ref ForceResolution);

                    //Language
                    GameLanguage = Plugin_IniFile.IniReadValue("System", " LANGUAGE");
                    Plugin_IniFile.IniReadBoolValue("System", "LOCAL_SAVE", ref LocalSave);

                    for (int i = 0; i < MAX_PLAYERS; i++)
                    {
                        foreach (KeyValuePair<string, int> b in PlayerButtons)
                        {
                            string KeyName = "P" + (i + 1).ToString() + "_" + b.Key;
                            string KeyValue = Plugin_IniFile.IniReadValue("INPUT_KEYS", KeyName);
                            int iKeyCode = 0;
                            if (KeyValue != string.Empty)
                            {
                                if (int.TryParse(KeyValue, out iKeyCode))
                                {
                                    KeyCode k = (KeyCode)iKeyCode;
                                    MyLogger.LogMessage("--> Setting " + KeyName + " to " + k.ToString() + " Key");
                                }
                                else
                                {
                                    MyLogger.LogMessage("--> Setting " + KeyName + " to " + KeyValue);
                                }
                                PlayerControllers[i].SetButtonKeyCode(b.Key, KeyValue);
                            }
                        }
                    }

                    foreach (KeyValuePair<string, int> b in GameButtons)
                    {
                        string KeyValue = Plugin_IniFile.IniReadValue("INPUT_KEYS", b.Key);
                        int iKeyCode = 0;
                        if (KeyValue != string.Empty)
                        {
                            if (int.TryParse(KeyValue, out iKeyCode))
                            {
                                KeyCode k = (KeyCode)iKeyCode;
                                MyLogger.LogMessage("--> Setting " + b.Key + " to " + k.ToString() + " Key");
                            }
                            else
                            {
                                MyLogger.LogMessage("--> Setting " + b.Key + " to " + KeyValue);
                            }
                            GameController.SetButtonKeyCode(b.Key, KeyValue);
                        }
                    }
                }
                catch (Exception Ex)
                {
                    MyLogger.LogError(Instance.GetType().Name + "." + MethodBase.GetCurrentMethod().Name + "(): Error reading config file : " + Plugin_IniFile.FInfo.FullName);
                    MyLogger.LogError(Ex.Message.ToString());
                }
            }
            else
            {
                MyLogger.LogWarning(Instance.GetType().Name + "." + MethodBase.GetCurrentMethod().Name + "():" + Plugin_IniFile.FInfo.FullName + " not found");
            }

            MyLogger.LogMessage("Graphics Engine: " + SystemInfo.graphicsDeviceVersion);
            MyLogger.LogMessage("Persistent Data Path: " + UnityEngine.Application.persistentDataPath);

            harmony.PatchAll();
        }

        public void Start()
        {}

        public void Update()
        {
            //Updating controls if U-SHoot is not running
            if (!EnableInputHack)
            {
                foreach (PluginControllerButton b in GameController.Buttons)
                {
                    b.SetRawState(Input.GetKey((KeyCode)b.KeyCode));
                }

                for (int i = 0; i < MAX_PLAYERS; i++)
                {
                    PlayerControllers[i].SetAimingValues(Input.mousePosition);
                    PlayerControllers[i].SetRawButtonState("START", Input.GetKey((KeyCode)PlayerControllers[i].GetButtonKeyCode("START")) ? (byte)1 : (byte)0);
                    PlayerControllers[i].SetRawButtonState("TRIGGER", Input.GetMouseButton(0) ? (byte)1 : (byte)0);
                    PlayerControllers[i].SetRawButtonState("RELOAD", Input.GetMouseButton(1) ? (byte)1 : (byte)0);
                    PlayerControllers[i].SetRawButtonState("COIN", Input.GetKey((KeyCode)PlayerControllers[i].GetButtonKeyCode("COIN")) ? (byte)1 : (byte)0);
                }
            }

            //Setting Frame-BAsed buttons status and events
            GameController.UpdateFrameInputStates();
            for (int i = 0; i < MAX_PLAYERS; i++)
            {
                PlayerControllers[i].UpdateFrameInputStates();
            }

            //Quit
            if (GameController.GetButtonDown("EXIT"))
                UnityEngine.Application.Quit();

            //Fetching Outputs
            try
            {
                Player[] playerList = Singleton<PlayerManager>.Instance.m_PlayerList;
                for (int i = 0; i < MAX_PLAYERS; i++)
                {
                    if (!Singleton<GameplayManager>.Instance.IsDemo && playerList[i].IsAlive())
                        OutputData.IsPlaying[i] = 1;
                    else
                        OutputData.IsPlaying[i] = 0;

                    OutputData.Life[i] = playerList[i].Health > 0.0f ? playerList[i].Health : 0.0f;
                    OutputData.Ammo[i] = playerList[i].CurrentGun.ClipRemaining;
                    OutputData.Credits[i] = Singleton<KaboomManager>.Instance.GetCoinCount((ID)i);
                }
            }
            catch { }

            //Checking for a change in output to send or not
            byte[] bOutputData = OutputData.ToByteArray();
            byte[] bOutputDataBefore = _OutputDataBefore.ToByteArray();
            for (int i = 0; i < bOutputData.Length; i++)
            {
                if (bOutputData[i] != bOutputDataBefore[i])
                {
                    SendOutputs();
                    break;
                }
            }

            //Save current state
            _OutputDataBefore.Update(bOutputData);
        }

        public void OnDestroy()
        {
            MyLogger.LogMessage(Instance.GetType().Name + "." + MethodBase.GetCurrentMethod().Name + "()");
            _TcpListener.Server.Close();
        }

        private void HarmonyPatch(Harmony hHarmony, Type OriginalClass, String OriginalMethod, Type ReplacementClass, String ReplacementMethod)
        {
            MethodInfo original = AccessTools.Method(OriginalClass, OriginalMethod);
            MethodInfo patch = AccessTools.Method(ReplacementClass, ReplacementMethod);
            hHarmony.Patch(original, new HarmonyMethod(patch));
        }

        /// <summary> 	
        /// Runs in background TcpServerThread; Handles incomming TcpClient requests 	
        /// </summary> 	
        private void TcpClientThreadLoop()
        {
            try
            {
                // Create listener on localhost port 8052. 			
                _TcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), _TcpPort);
                _TcpListener.Start();
                MyLogger.LogMessage(Instance.GetType().Name + "." + MethodBase.GetCurrentMethod().Name + "(): TCP Server is listening on Port " + _TcpPort);

                Byte[] bytes = new Byte[1024];
                while (true)
                {
                    using (_TcpClient = _TcpListener.AcceptTcpClient())
                    {
                        MyLogger.LogMessage(Instance.GetType().Name + "." + MethodBase.GetCurrentMethod().Name + "(): TCP Client connected !");
                        using (_TcpStream = _TcpClient.GetStream())
                        {
                            //Send outputs at connection, if DemulShooter connects during game, between events
                            SendOutputs();
                            while (true)
                            {
                                int Length = 0;
                                try
                                {
                                    Length = _TcpStream.Read(bytes, 0, bytes.Length);
                                    //If Tcpclient gets disconnected, Read should return 0 bytes, so we can handle disconnection to allow a new connection
                                    if (Length == 0)
                                        break;
                                    byte[] InputBuffer = new byte[Length];
                                    Array.Copy(bytes, 0, InputBuffer, 0, Length);
                                    _InputData.Update(InputBuffer);
                                    //- Debug ONLY
                                    //MyLogger.LogMessage(Instance.GetType().Name + "." + MethodBase.GetCurrentMethod().Name + "(): client message received as: " + _InputData.ToString());
                                    //- Debug ONLY

                                    //lock (MutexLocker_Inputs)
                                    //{
                                    GameController.SetRawButtonState("EXIT", _InputData.Exit);
                                    GameController.SetRawButtonState("TEST", _InputData.Test);
                                    GameController.SetRawButtonState("MENU_UP", _InputData.MenuUp);
                                    GameController.SetRawButtonState("MENU_DOWN", _InputData.MenuDown);
                                    GameController.SetRawButtonState("MENU_SELECT", _InputData.MenuSelect);

                                    for (int i = 0; i < MAX_PLAYERS; i++)
                                    {
                                        PlayerControllers[i].SetAimingValues(new Vector3(_InputData.Axis_X[i], _InputData.Axis_Y[i]));
                                        PlayerControllers[i].SetRawButtonState("COIN", _InputData.Coin[i]);
                                        PlayerControllers[i].SetRawButtonState("START", _InputData.Start[i]);
                                        PlayerControllers[i].SetRawButtonState("TRIGGER", _InputData.Trigger[i]);
                                        PlayerControllers[i].SetRawButtonState("RELOAD", _InputData.Reload[i]);
                                        
                                    }

                                    CrossHairVisibility = _InputData.HideCrosshairs == 1 ? false : true;
                                    EnableInputHack = _InputData.EnableInputsHack == 1 ? true : false;
                                    //}
                                }
                                catch
                                {
                                    //Connnection Error ?
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch (SocketException socketException)
            {
                MyLogger.LogMessage(Instance.GetType().Name + "." + MethodBase.GetCurrentMethod().Name + "(): SocketException " + socketException.ToString());
            }
        }

        /// <summary>
        /// Send output data over the TCP connection
        /// When TcpClient is disconnected, _TcpClient is Disposed and can't be acces to check if null or not => need Try/Catch
        /// </summary>
        public static void SendOutputs()
        {
            try
            {
                if (Instance._TcpClient == null)
                    return;

                if (_TcpStream == null)
                    return;

                // Get a stream object for writing. 			
                if (_TcpStream.CanWrite)
                {
                    TcpPacket p = new TcpPacket(OutputData.ToByteArray(), TcpPacket.PacketHeader.Outputs);
                    byte[] Buffer = p.GetFullPacket();
                    //- Debug ONLY
                    //MyLogger.LogMessage(Instance.GetType().Name + "." + MethodBase.GetCurrentMethod().Name + "():  Sending data : " + p.ToString());
                    //- Debug ONLY
                    //lock (MutexLocker_Outputs)
                    //{
                    //Resetting event flags for next packets                    
                    for (int i = 0; i < MAX_PLAYERS; i++)
                    {
                        OutputData.Recoil[i] = 0;
                        OutputData.Damaged[i] = 0;
                    }
                    //}
                    _TcpStream.Write(Buffer, 0, Buffer.Length);
                }
            }
            catch (Exception Ex)
            {
                MyLogger.LogMessage(Instance.GetType().Name + "." + MethodBase.GetCurrentMethod().Name + "(): Socket exception: " + Ex);
            }
        }

        /// <summary>
        /// For deebug, printing StackTrace to trace calls to API we're looking for
        /// </summary>
        public static void PrintStackTrace()
        {
            MyLogger.LogMessage(System.Environment.StackTrace);
        }

        /// <summary>
        /// For deebug, printing StackTrace to trace calls to API we're looking for
        /// </summary>
        public static void PrintMethodInfo()
        {
            MyLogger.LogMessage(System.Environment.StackTrace);
            
        }
    }
}
