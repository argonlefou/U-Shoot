using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityPlugin_BepInEx.Core;
using UnityPlugin_BepInEx_IniFile;

namespace BepInEx_UShoot_Plugin
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class UShoot_Plugin : BaseUnityPlugin
    {
        public const String pluginGuid = "argonlefou.ushoot.nha2";
        public const String pluginName = "NightHunter2_BepInEx_UShoot_Plugin";
        public const String pluginVersion = "1.0.0.0";
        public const String pluginConfigFile = "UShoot_Plugin.ini";

        public static BepInEx.Logging.ManualLogSource MyLogger;

        public static UShoot_Plugin Instance = null;

        //------ Custom Input Data ------//
        //-                             -//
        public static readonly int MAX_PLAYERS = 2;
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
        public static bool GunVisibility = true;

        //Custom resolution
        public static int ScreenWidth = 1920;
        public static int ScreenHeight = 1080;
        public static bool Fullscreen = true;
        public static bool ForceResolution = false;

        private static readonly string SPRITE_1P_BLUE_FILE = "Assets\\image_1P_Blue.png";
        private static readonly string SPRITE_1P_RED_FILE = "Assets\\image_1P_Red.png";
        private static readonly string SPRITE_2P_BLUE_FILE = "Assets\\image_2P_Blue.png";
        private static readonly string SPRITE_2P_RED_FILE = "Assets\\image_2P_Red.png";
        public static Sprite Sprite_1P_Blue;
        public static Sprite Sprite_1P_Red;
        public static Sprite Sprite_2P_Blue;
        public static Sprite Sprite_2P_Red;

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
            GameButtons.Add("WHEEL_LEFT", GameButtons.Count);
            GameButtons.Add("WHEEL_RIGHT", GameButtons.Count);
            GameButtons.Add("MENU_DOWN", GameButtons.Count);
            GameButtons.Add("MENU_SELECT", GameButtons.Count);
            GameButtons.Add("EXIT", GameButtons.Count);
            GameController = new VirtualGameController(GameButtons);
            GameController.SetButtonKeyCode("WHEEL_LEFT", (int)KeyCode.LeftArrow);
            GameController.SetButtonKeyCode("WHEEL_RIGHT", (int)KeyCode.RightArrow);

            //Virtual Player Controllers
            PlayerButtons.Add("START", PlayerButtons.Count);
            PlayerButtons.Add("TRIGGER", PlayerButtons.Count);
            PlayerButtons.Add("ACTION", PlayerButtons.Count);
            PlayerButtons.Add("CHANGE_WEAPON", PlayerButtons.Count);
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

            Texture2D texture = LoadTextureFromFile(SPRITE_1P_BLUE_FILE);
            Sprite_1P_Blue = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
            texture = LoadTextureFromFile(SPRITE_1P_RED_FILE);
            Sprite_1P_Red = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
            texture = LoadTextureFromFile(SPRITE_2P_BLUE_FILE);
            Sprite_2P_Blue = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
            texture = LoadTextureFromFile(SPRITE_2P_RED_FILE);
            Sprite_2P_Red = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);

            harmony.PatchAll();
        }

        public void Start()
        { }

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
                    PlayerControllers[i].SetRawButtonState("CHANGE_WEAPON", Input.GetMouseButton(1) ? (byte)1 : (byte)0);
                    PlayerControllers[i].SetRawButtonState("ACTION", Input.GetMouseButton(2) ? (byte)1 : (byte)0);
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

            //Custom buttons handling
            if (GameController.GetButtonDown("TEST"))
            {
                //input_obj_goto_houtai object does not expose any static function to call for TEST menu
                //Instead, redoing what is done in it's work() function 
                if (zhichi_hanshu_game_run_manage.get_game_run_manage_status() == game_run_manage_normal.MANAGE_STATUS.LOADING || zhichi_hanshu_game_run_manage.get_game_run_manage_status() == game_run_manage_normal.MANAGE_STATUS.LOADING_ALL)
                {
                    //Do Nothing
                }
                else
                {
                    game_run_manage_normal.static_set_curr_manage_status(game_run_manage_normal.MANAGE_STATUS.HOUTAI);
                }
            }

            if (GameController.GetButtonDown("COIN"))
                input_obj_insert_coin.add_coin(1);

            for (int i = 0; i < MAX_PLAYERS; i++)
            {
                if (PlayerControllers[i].GetButtonDown("CHANGE_WEAPON"))
                    input_obj_change_bullet.change_weapon(i + 1); // PlayerNum is 1 or 2

                if (PlayerControllers[i].GetButtonDown("ACTION"))
                    input_obj_big_power.big_power_work(i + 1); // PlayerNum is 1 or 2

                if (PlayerControllers[i].GetButtonDown("START"))
                {
                    input_obj_start_game.start_game(i + 1); // PlayerNum is 1 or 2
                    input_obj_pass_movie.pass_movie();
                }
            }

            //Fetching Outputs
            try
            {
                OutputData.Credits = zhichi_hanshu_houtai.get_no_use_coins();
                for (int i = 0; i < MAX_PLAYERS; i++)
                {

                    OutputData.Life[i] = game_run_core.my_static_game_run.mygame_players.get_game_player(i + 1).get_curr_blood();
                    OutputData.GunType[i] = (int)zhichi_hanshu_gun_wheel_mark_manage.zchs_get_gun_type(i + 1);
                    OutputData.IsPlaying[i] = zhichi_hanshu_game_players.get_player_by_num(i + 1).is_living() ? (byte)1 : (byte)0;
                }
            } catch { }

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
                                    GameController.SetRawButtonState("COIN", _InputData.Coin);
                                    GameController.SetRawButtonState("WHEEL", _InputData.WheelLeft);
                                    GameController.SetRawButtonState("WHEEL_RIGHT", _InputData.WheelRight);
                                    GameController.SetRawButtonState("MENU_DOWN", _InputData.MenuDown);
                                    GameController.SetRawButtonState("MENU_SELECT", _InputData.MenuSelect);

                                    for (int i = 0; i < MAX_PLAYERS; i++)
                                    {
                                        PlayerControllers[i].SetAimingValues(new Vector3(_InputData.Axis_X[i], _InputData.Axis_Y[i]));
                                        PlayerControllers[i].SetRawButtonState("TRIGGER", _InputData.Trigger[i]);
                                        PlayerControllers[i].SetRawButtonState("CHANGE_WEAPON", _InputData.ChangeWeapon[i]);
                                        PlayerControllers[i].SetRawButtonState("ACTION", _InputData.Action[i]);
                                        PlayerControllers[i].SetRawButtonState("START", _InputData.Start[i]);
                                    }

                                    CrossHairVisibility = _InputData.HideCrosshairs == 1 ? false : true;
                                    EnableInputHack = _InputData.EnableInputsHack == 1 ? true : false;
                                    GunVisibility = _InputData.HideGuns == 1 ? false : true;
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

        private static Texture2D LoadTextureFromFile(string FileName)
        {
            string FilePath = Path.Combine(BepInEx.Paths.PluginPath, FileName);

            Texture2D tex = null;
            byte[] fileData;

            if (File.Exists(FilePath))
            {
                fileData = File.ReadAllBytes(FilePath);
                tex = new Texture2D(2, 2);
                tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
                MyLogger.LogMessage(string.Concat(new object[]
                {
                    "Texture created from ",
                    FilePath,
                    " :  width = ",
                    tex.width,
                    ", height =  ",
                    tex.height,
                    ",",
                    tex.dimension.ToString()
                }));
            }
            else
            {
                MyLogger.LogError("TokiPlugin.Awake() => File not found : " + FilePath);
            }
            return tex;
        }
    }
}
