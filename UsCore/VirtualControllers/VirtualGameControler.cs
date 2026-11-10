using System.Collections.Generic;
using UsCore.Config;
using UsCore.Logging;

namespace UsCore.VirtualControllers
{
    public class VirtualGameControler : VirtualController
    {
        private BaseConfig _Config;

        public List<VirtualPlayerController> PlayerControllers
        { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public VirtualGameControler(BaseConfig Config) : base()
        {
            _Config = Config;
            Logger.WriteLog("[" + this.GetType().Name + "] Binding Global Buttons :");
            BindButtons(_Config.System_Inputs);

            PlayerControllers = new List<VirtualPlayerController>();
            foreach (BaseConfig_Player Player in Config.Players_Inputs)
            {
                VirtualPlayerController PlayerController = new VirtualPlayerController(Player);
                PlayerControllers.Add(PlayerController);
            }
        }
    }
}
