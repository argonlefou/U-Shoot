using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using UnityEngine;
using UnityPlugin_BepInEx.Core;
using UnityPlugin_BepInEx_IniFile;

namespace BepInEx_UShoot_Plugin
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class UShoot_Plugin : BaseUnityPlugin
    {
        public const String pluginGuid = "argonlefou.ushoot.mechatornado";
        public const String pluginName = "MechaTornado_BepInEx_Ushoot_Plugin";
        public const String pluginVersion = "1.0.0.0";
        public const String pluginConfigFile = "UShoot_Plugin.ini";

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

        public static int PlayerNum = 2;
        public static bool EnableTriggerWaterDelay = false;

        //Custom data        
        public static int[] BufferCoin = new int[4];
        public static int[] BufferTicket = new int[4];

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
            GameButtons.Add("MENU_LEFT", GameButtons.Count);
            GameButtons.Add("MENU_RIGHT", GameButtons.Count);
            GameButtons.Add("EXIT", GameButtons.Count);
            GameController = new VirtualGameController(GameButtons);

            //Virtual Player Controllers
            PlayerButtons.Add("COIN", PlayerButtons.Count);
            PlayerButtons.Add("START", PlayerButtons.Count);
            PlayerButtons.Add("TRIGGER", PlayerButtons.Count);
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
                    Plugin_IniFile.IniReadIntValue("System", "PLAYERS", ref PlayerNum);
                    Plugin_IniFile.IniReadBoolValue("System", "USE_WATERGUN", ref EnableTriggerWaterDelay);

                    Plugin_IniFile.IniReadIntValue("Video", "WIDTH", ref ScreenWidth);
					Plugin_IniFile.IniReadIntValue("Video", "HEIGHT", ref ScreenHeight);

					Plugin_IniFile.IniReadBoolValue("Video", "FULLSCREEN", ref Fullscreen);
					Plugin_IniFile.IniReadBoolValue("Video", "FORCE_RESOLUTION", ref ForceResolution);

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
                    PlayerControllers[i].SetRawButtonState("COIN", Input.GetKey((KeyCode)PlayerControllers[i].GetButtonKeyCode("COIN")) ? (byte)1 : (byte)0);
                    PlayerControllers[i].SetRawButtonState("START", Input.GetKey((KeyCode)PlayerControllers[i].GetButtonKeyCode("START")) ? (byte)1 : (byte)0);
                    PlayerControllers[i].SetRawButtonState("TRIGGER", Input.GetMouseButton(0) ? (byte)1 : (byte)0);
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


            if (Input.GetKeyDown(KeyCode.H))
            {
                Type t = AccessTools.TypeByName("IniFile.IniParser");
                if (t == null)
                {
                    UShoot_Plugin.MyLogger.LogError("Error");
                }
                else
                {
                    foreach (ConstructorInfo ci in AccessTools.TypeByName("IniFile.IniParser").GetConstructors())
                    {
                        if (ci.GetParameters().Length == 1)
                        {
                            UShoot_Plugin.MyLogger.LogWarning(ci.Name + ", " + ci.GetParameters().Length);
                        }
                    }
                }
            }

            //COIN input
            for (int i = 0; i < MAX_PLAYERS; i++)
            {
                if (PlayerControllers[i].GetButtonDown("COIN"))
                    BufferCoin[i] += 1;
            }

            //Fetching some outputs
            for (int i = 0; i < MAX_PLAYERS; i++)
            {
                OutputData.IsPlaying[i] = (byte)gamedata.m_playerdata[i].gamestate;
                OutputData.Life[i] = gamedata.m_playerdata[i].life;
                OutputData.Credits[i] = BufferCoin[i];
            }

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
                                    GameController.SetRawButtonState("MENU_LEFT", _InputData.MenuLeft);
                                    GameController.SetRawButtonState("MENU_RIGHT", _InputData.MenuRight);
                                    for (int i = 0; i < MAX_PLAYERS; i++)
                                    {
                                        PlayerControllers[i].SetAimingValues(new Vector3(_InputData.Axis_X[i], _InputData.Axis_Y[i]));
                                        PlayerControllers[i].SetRawButtonState("COIN", _InputData.Coin[i]);
                                        PlayerControllers[i].SetRawButtonState("TRIGGER", _InputData.Trigger[i]);
                                        PlayerControllers[i].SetRawButtonState("START", _InputData.Start[i]);
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
    }
}

