using BepInEx;
using GameBase.Manager;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using UnityEngine;
using UnityPlugin_BepInEx.Core;
using UnityPlugin_BepInEx_IniFile;

namespace BepInEx_USoot_Plugin
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class UShoot_Plugin : BaseUnityPlugin
    {
        public const String pluginGuid = "argonlefou.ushoot.topgun";
        public const String pluginName = "TopGun_BepInEx_Ushoot_Plugin";
        public const String pluginVersion = "1.0.0.0";
        public const String pluginConfigFile = "UShoot_Plugin.ini";

        public static BepInEx.Logging.ManualLogSource MyLogger;

        public static UShoot_Plugin Instance = null;

        //------ Custom Input Data ------//
        //-                             -//
        public static readonly int MAX_PLAYERS = 1;
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
        public static readonly float ORIGINAL_HEIGHT = 1920.0f;
        public static bool ScaleToHorizontal = false;

        public static Process OutputLoggerProcess = null;
        public static SyFireComm SyFireComm_Instance = null;
        public static MethodInfo mRecvCoin = null;
        public static MethodInfo mRecvKey = null;
        public static MethodInfo mRecvDoorState = null;
        public static MethodInfo mRecvGunInCameraSpace = null;
        public static MethodInfo mRecvGunInScreenSpace = null;
        public static MethodInfo mRecvTickets = null;
        byte bKeyStates = 0;
        byte bPreviousKeyStates = 0;

        public static bool SaveToGameFolder = false;

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
            GameButtons.Add("COIN", GameButtons.Count);
            GameButtons.Add("RIGHT", GameButtons.Count);
            GameButtons.Add("LEFT", GameButtons.Count);
            GameButtons.Add("SELECT", GameButtons.Count);
            GameButtons.Add("REFILL", GameButtons.Count);
            GameButtons.Add("EXIT", GameButtons.Count);
            GameController = new VirtualGameController(GameButtons);

            //Virtual Player Controllers
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
                    Plugin_IniFile.IniReadBoolValue("SYSTEM", "LOCAL_SAVE", ref SaveToGameFolder);

                    Plugin_IniFile.IniReadIntValue("VIDEO", "WIDTH", ref ScreenWidth);
                    Plugin_IniFile.IniReadIntValue("VIDEO", "HEIGHT", ref ScreenHeight);

                    Plugin_IniFile.IniReadBoolValue("VIDEO", "FULLSCREEN", ref Fullscreen);
                    Plugin_IniFile.IniReadBoolValue("VIDEO", "FORCE_RESOLUTION", ref ForceResolution);
                    Plugin_IniFile.IniReadBoolValue("VIDEO", "HORIZONTAL_SCREEN", ref ScaleToHorizontal);

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
            if (ForceResolution && ScaleToHorizontal)
                ScaleDisplay();

            //Embedded Keyboard code, disabled by the programl but still existing if we call it
            /*SingletonComponent<GameManager>.Instance.IOManager.GetGameIOComm<SYFireAndroidIO>().positionInCamSpace[0].x = Input.mousePosition.x;
            SingletonComponent<GameManager>.Instance.IOManager.GetGameIOComm<SYFireAndroidIO>().positionInCamSpace[0].y = Screen.height - Input.mousePosition.y;
            SingletonComponent<GameManager>.Instance.IOManager.OnKeyboard();*/

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

            //Coin
            if (GameController.GetButtonDown("COIN"))
            {
                byte bPlayer = 0;    //Stick to single player cab, no LINK play with 2P
                byte bCoinIn = 1;
                mRecvCoin.Invoke(SyFireComm_Instance, new object[] { new byte[] { 0, 0, 0, 0, bPlayer, bCoinIn } });
            }

            //KEYS
            if (GameController.GetButtonDown("TEST"))       //MENU
                bKeyStates |= 0x01;
            if (GameController.GetButtonDown("LEFT"))      //MENU UP
                bKeyStates |= 0x02;
            if (GameController.GetButtonDown("SELECT"))  //MENU SELECT | REPLENISH
                bKeyStates |= 0x04;
            if (GameController.GetButtonDown("RIGHT"))    //MENU DOWN
                bKeyStates |= 0x08;
            if (GameController.GetButtonDown("REFILL"))      //REFILL
                bKeyStates |= 0x80;
            //UNUSED ?
            //if (Input.GetKeyDown(KeyCode.Keypad4))
            //    bKeyStates |= 0x10;
            //if (Input.GetKeyDown(KeyCode.Keypad5))
            //    bKeyStates |= 0x20;
            //if (Input.GetKeyDown(KeyCode.Keypad6))
            //    bKeyStates |= 0x40;


            if (GameController.GetButtonUp("TEST"))
                bKeyStates &= 0xFE;
            if (GameController.GetButtonUp("LEFT"))
                bKeyStates &= 0xFD;
            if (GameController.GetButtonUp("SELECT"))
                bKeyStates &= 0xFB;
            if (GameController.GetButtonUp("RIGHT"))
                bKeyStates &= 0xF7;
            if (GameController.GetButtonUp("REFILL"))
                bKeyStates &= 0x7F;
            //if (Input.GetKeyUp(KeyCode.Keypad4))
            //    bKeyStates &= 0xEF;
            //if (Input.GetKeyUp(KeyCode.Keypad5))
            //    bKeyStates &= 0xDF;
            //if (Input.GetKeyUp(KeyCode.Keypad6))
            //    bKeyStates &= 0xBF;

            //Checking for a change in inputs to send or not
            if (bKeyStates != bPreviousKeyStates)
                mRecvKey.Invoke(SyFireComm_Instance, new object[] { new byte[] { 0, 0, 0, 0, 0, bKeyStates } });

            //Gun position and shoot
            if (PlayerControllers[0].GetButtonDown("TRIGGER"))
            {
                Vector3 vGunPos = PlayerControllers[0].GetAimingPosition();
                //Sending (0,0) should display the "shoot in the screen" message
                if (vGunPos.x < 0 || vGunPos.y < 0 || vGunPos.x > Screen.width || vGunPos.y > Screen.height)
                {
                    vGunPos.x = 0;
                    vGunPos.y = 0;
                }
                byte[] bPosArray = new byte[9];
                bPosArray[4] = (byte)SingletonComponent<GameManager>.Instance.currentPlayingID;
                bPosArray[5] = (byte)((int)vGunPos.x & 0xFF);
                bPosArray[6] = (byte)((int)vGunPos.x >> 8 & 0xFF);
                bPosArray[7] = (byte)((Screen.height - (int)vGunPos.y) & 0xFF);
                bPosArray[8] = (byte)((Screen.height - (int)vGunPos.y) >> 8 & 0xFF);
                mRecvGunInScreenSpace.Invoke(SyFireComm_Instance, new object[] { bPosArray });
            }

            //Door States
            //Bitwise operation on Data[5]
            //Bit 0 : Door1
            //Bit 1 : Door2
            //Bit 5 : Door3
            //Bit 6 : Door4
            if (Input.GetKeyDown(KeyCode.Alpha6))
            {
                byte States = 0x33;  //All Doors
                mRecvDoorState.Invoke(SyFireComm_Instance, new object[] { new byte[] { 0, 0, 0, 0, 0, States } });
            }
            if (Input.GetKeyUp(KeyCode.Alpha6))
            {
                byte States = 0x00;
                mRecvDoorState.Invoke(SyFireComm_Instance, new object[] { new byte[] { 0, 0, 0, 0, 0, States } });
            }

            //Ticket SENSOR
            if (Input.GetKeyDown(KeyCode.Space))
            {
                UShoot_Plugin.MyLogger.LogMessage("Ticket Return test Pushed");
                byte bPlayer = (byte)SingletonComponent<GameManager>.Instance.currentPlayingID;
                byte bTicket = 1;

                mRecvTickets.Invoke(SyFireComm_Instance, new object[] { new byte[] { 0, 0, 0, 0, bPlayer, bTicket } });
            }

            //Getting Ammo
            for (int i = 0; i < SingletonComponent<GameManager>.Instance.playerManager.list.GetPlayerNum(); i++)
            {
                Player p = SingletonComponent<GameManager>.Instance.playerManager.list.GetPlayerFromIndex(i);
                if (p.GetType() == typeof(FirePlayer))
                {
                    OutputData.Ammo = ((FirePlayer)p).bulletNumber;
                }
            }

            //Getting credits
            OutputData.Credits = SingletonComponent<GameManager>.Instance.dataManager.playerData[0].data[0];

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
            bPreviousKeyStates = bKeyStates;
            if (Input.GetKeyDown(KeyCode.Space))
                Screen.SetResolution(1080, 1900, false);
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
        /// If the game is not developped to be scaled with window size, using that function every frame
        /// will check the root canvas and, if the scale setting is not set, will scale it to the current window size
        /// </summary>
        private void ScaleDisplay()
        {
            float fScale = (float)Screen.height / ORIGINAL_HEIGHT;

            Canvas[] allCanvas = FindObjectsOfType<Canvas>();
            foreach (Canvas c in allCanvas)
            {
                c.scaleFactor = fScale;
            }
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
                                    GameController.SetRawButtonState("COIN", _InputData.Coin);
                                    GameController.SetRawButtonState("MENU_UP", _InputData.MenuUp);
                                    GameController.SetRawButtonState("MENU_DOWN", _InputData.MenuDown);
                                    GameController.SetRawButtonState("MENU_SELECT", _InputData.MenuSelect);
                                    GameController.SetRawButtonState("REFILL", _InputData.Refill);
                                    for (int i = 0; i < MAX_PLAYERS; i++)
                                    {
                                        PlayerControllers[i].SetAimingValues(new Vector3(_InputData.Axis_X[i], _InputData.Axis_Y[i]));
                                        PlayerControllers[i].SetRawButtonState("TRIGGER", _InputData.Trigger[i]);
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
                        OutputData.Recoil = 0;
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

