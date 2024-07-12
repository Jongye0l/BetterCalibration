using JALib.Bootstrap;
using UnityModManagerNet;

namespace BetterCalibration {
    public class Bootstrap {
        public static void Setup(UnityModManager.ModEntry modEntry) => JABootstrap.Load(modEntry);
    }
}