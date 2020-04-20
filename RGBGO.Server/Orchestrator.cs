using MSIGS.Server;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Drawing;
using static System.Linq.Enumerable;
using MSIRGB;
using System.Windows.Media;

namespace MSIGS.Server
{
    public class LedOrchestrator
    {
        private Lighting _lighting;

        public LedOrchestrator(IGameState gameState)
        {
            this.GameState = gameState;

            gameState.OnStateChanged += OnGameStateChanged;

            TryInitializeDll(true);
        }

        private void TryInitializeDll(bool ignoreMbCheck = false)
        {
            try
            {
                _lighting = new Lighting(ignoreMbCheck);
            }
            catch (Lighting.Exception exc)
            {
                if (exc.GetErrorCode() == Lighting.ErrorCode.MotherboardModelNotSupported)
                {
                    Console.WriteLine("WARNING: Your motherboard is not on the list of supported motherboards");
                    TryInitializeDll(true);
                    return;
                }
                else
                {
                    throw new NotSupportedException(string.Format("Motherboard not supported or unable to load driver, error code {0}", exc.GetErrorCode()));
                }
            }
        }

        public void GetCurrentColors(ref List<Color> colours,
                             out ushort stepDuration,
                             out bool breathingEnabled,
                             out FlashingSpeed flashingSpeed)
        {
            foreach (byte index in Range(1, 8))
            {
                Color c = _lighting.GetColour(index).Value;
                c.R *= 0x11; // Colour is exposed as 12-bit depth, but colour picker expects 24-bit depth
                c.G *= 0x11;
                c.B *= 0x11;
                colours.Add(c);
            }

            stepDuration = _lighting.GetStepDuration();

            breathingEnabled = _lighting.IsBreathingModeEnabled();

            flashingSpeed = (FlashingSpeed)_lighting.GetFlashingSpeed();
        }

        public void ApplyColor(List<Color> colours,
                                ushort stepDuration,
                                bool breathingEnabled,
                                FlashingSpeed flashingSpeed)
        {
            _lighting.BatchBegin();

            foreach (byte index in Range(1, 8))
            {
                Color c = colours[index - 1];
                c.R /= 0x11; // Colour must be passed with 12-bit depth
                c.G /= 0x11;
                c.B /= 0x11;
                _lighting.SetColour(index, c);
            }

            _lighting.SetStepDuration(stepDuration);

            // Since breathing mode can't be enabled if flashing was previously enabled
            // we need to set the new flashing speed setting before trying to change breathing mode state
            _lighting.SetFlashingSpeed((Lighting.FlashingSpeed)flashingSpeed);

            _lighting.SetBreathingModeEnabled(breathingEnabled);

            _lighting.BatchEnd();
        }

        public void DisableLighting()
        {
            _lighting.SetLedEnabled(false);
        }

        private void OnGameStateChanged(object sender, EventArgs e)
        {
            

            GameStateDictionary states = GameState.States;

            if(states["player.activity"].Equals("playing"))
            {
                if (states.HasChanged("player.state.health") && !states["player.state.health"].Equals("100"))
                {
                    Console.WriteLine("AUTS!");
                }

                if (states.HasChanged("player.state.flashed") && !states["player.state.health"].Equals("0"))
                {
                    Console.WriteLine("Flash!");
                }

                if (states.HasChanged("player.state.smoked") && !states["player.state.health"].Equals("0"))
                {
                    Console.WriteLine("Smoke!");
                }

                if (states.HasChanged("player.state.burning") && !states["player.state.health"].Equals("0"))
                {
                    Console.WriteLine("FIRE!");
                }
            }

            if (states.HasChanged("round.bomb") && states["round.bomb"].Equals("exploded"))
            {
                Console.WriteLine("BOOOOOOOOOOOM!!!");
            }

            if (states.HasChanged("round.phase") && states["round.phase"].Equals("over"))
            {
                if (states["round.win_team"].Equals("CT"))
                    Console.WriteLine("Counter terrorists win!");

                if (states["round.win_team"].Equals("T"))
                    Console.WriteLine("Terrorists win!");
            }

            //foreach(string key in states.Keys)
            //{
            //    Console.WriteLine(string.Format("{0}:{1}", key, states[key]));
            //}

            //if(GameState.States.Changed.Count > 0)
            //    Console.WriteLine("Has changed -->");

            //foreach (string key in GameState.States.Changed)
            //{
            //    Console.WriteLine(string.Format("{0}", key));
            //}

        }

        public IGameState GameState { get; private set; }
    }

    public enum FlashingSpeed
    {
        Disabled = Lighting.FlashingSpeed.Disabled,
        Speed1 = Lighting.FlashingSpeed.Speed1,
        Speed2 = Lighting.FlashingSpeed.Speed2,
        Speed3 = Lighting.FlashingSpeed.Speed3,
        Speed4 = Lighting.FlashingSpeed.Speed4,
        Speed5 = Lighting.FlashingSpeed.Speed5,
        Speed6 = Lighting.FlashingSpeed.Speed6,
    }
}
