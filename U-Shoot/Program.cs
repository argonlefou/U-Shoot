using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using UsCore.Config;

namespace U_Shoot
{
    internal class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AttachConsole(int dwProcessId);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool FreeConsole();
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetConsoleWindow();
        [DllImport("user32.dll", SetLastError = true)]
        static extern int SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        const uint WM_CHAR = 0x0102;
        const int VK_ENTER = 0x0D;
        const int ATTACH_PARENT_PROCESS = -1;

        static IntPtr ConsoleHwnd = IntPtr.Zero;

        static string strGame = string.Empty;
        static string strFullGameConfigPath = string.Empty;


        /// <summary>
        /// Point d'entrée principal de l'application.
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
            //Stop program if Demulshooter already running
            //Process[] p = Process.GetProcessesByName("U-SHoot");
            //if (p.Length > 1)
            //{
            //    MessageBox.Show("Another instance of " + Properties.Resources.APP_NAME + " is already running.\nPlease terminate it before launching a new one", Properties.Resources.APP_NAME, MessageBoxButtons.OK, MessageBoxIcon.Error);
            //    return 0;
            //}

            // Attach to the parent process via AttachConsole SDK call
            AttachConsole(ATTACH_PARENT_PROCESS);
            ConsoleHwnd = GetConsoleWindow();

            string strFullGameConfigPath = string.Empty;
            if (args.Length > 0)
            {
                foreach (string sOption in args)
                {
                    if (sOption.ToLower().Equals("-h") || sOption.ToLower().Equals("--help") || sOption.ToLower().Equals("-?"))
                    {
                        PrintConsoleHeader();
                        Console.WriteLine("");
                        Console.WriteLine("usage : " + Properties.Resources.APP_NAME + ".exe -game=[Game config file] [options]");
                        Console.WriteLine("");
                        Console.WriteLine("");
                        Console.WriteLine("Supported [options] :");
                        Console.WriteLine(" -config=[ConfigFile]\tspecify a config file name for U-Shoot.exe to load");
                        Console.WriteLine(" -? -h --help\t\tShow this help");
                        Console.WriteLine(" -v --verbose\t\tEnable output to log file");

                        ExitConsole();
                    }
                    else if (sOption.ToLower().StartsWith("-config"))
                    {
                        if (sOption.Split('=').Length > 1)
                            ProgramConfig.CustomConfigFilename = (sOption.Split('='))[1].Trim();
                    }
                    else if (sOption.ToLower().StartsWith("-game"))
                    {
                        if (sOption.ToLower().Split('=').Length < 2)
                        {
                            PrintConsoleHeader();
                            Console.WriteLine("");
                            Console.WriteLine("Error: No [game] parameter specified.");
                            ExitConsole();
                        }

                        strGame = (sOption.ToLower().Split('='))[1].Trim();
                        if (string.IsNullOrEmpty(strGame))
                        {
                            PrintConsoleHeader();
                            Console.WriteLine("");
                            Console.WriteLine("Error: No [game] parameter specified.");
                            ExitConsole();
                        }

                        if (!strGame.ToLower().EndsWith(".xml"))
                            strGame += ".xml";

                        strFullGameConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Properties.Resources.DATA_TEMPLATES_FOLDER, strGame);
                        if (!File.Exists(strFullGameConfigPath))
                        {
                            PrintConsoleHeader();
                            Console.WriteLine("");
                            Console.WriteLine("Error : Game file not found : \"" + strFullGameConfigPath);
                            ExitConsole();
                        }
                    }
                }

                if (!string.IsNullOrEmpty(strFullGameConfigPath))
                {
                    //Force Config.xml check/creation
                    ProgramConfig p = ProgramConfig.GetInstance();
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new UShoot_Program(strFullGameConfigPath));
                }

                PrintConsoleHeader();
                Console.WriteLine("");
                Console.WriteLine("Error : Invalid command or missing arguments. See " + Properties.Resources.APP_NAME + ".exe -h for help.");
            }
            else
            {
                //Force Config.xml check/creation
                ProgramConfig p = ProgramConfig.GetInstance();
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Wnd_Main());
            }

            return 0;
        }
        
        static private string GetDaySuffix(int day)
        {
            switch (day)
            {
                case 1:
                case 21:
                case 31:
                    return "st";
                case 2:
                case 22:
                    return "nd";
                case 3:
                case 23:
                    return "rd";
                default:
                    return "th";
            }
        }

        static void PrintConsoleHeader()
        {
            /* For DateTime (Builtin.CompileTime), add this in the .csproj file :
            
            <Target Name="Date" BeforeTargets="BeforeBuild">
                <WriteLinesToFile File="$(IntermediateOutputPath)gen.cs" Lines="static partial class Builtin { public static long CompileTime = $([System.DateTime]::UtcNow.Ticks) %3B }" Overwrite="true" />
                <ItemGroup>
                    <Compile Include="$(IntermediateOutputPath)gen.cs" />
                </ItemGroup>
            </Target>

            */
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine(Properties.Resources.APP_NAME + " v" + System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString());
            DateTime CompileTime = new DateTime(Builtin.CompileTime, DateTimeKind.Utc);
            string CompileDate = CompileTime.ToString("MMMM d", new CultureInfo("en-US"));
            CompileDate = string.Format("{0}{1}, {2}", CompileDate, GetDaySuffix(CompileTime.Day), CompileTime.ToString("yyyy"));
            Console.WriteLine("Build date : " + CompileDate);
        }

        static private void ExitConsole()
        {
            FreeConsole();
            //Send a {ENTER} key message to the attached console to show prompt
            SendMessage(ConsoleHwnd, WM_CHAR, (IntPtr)VK_ENTER, IntPtr.Zero);
            Environment.Exit(0);
        }

    }
}
