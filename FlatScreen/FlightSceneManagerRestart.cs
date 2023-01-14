using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony;

namespace Triquetra.FlatScreen
{
    [HarmonyPatch(typeof(FlightSceneManager), "InstantScenarioRestartRoutine")]
    internal class FlightSceneManagerRestart
    {
        static void Postfix()
        {
            FlatScreenMonoBehaviour.Instance?.Reclean();
        }
    }
}
