using Life;
using Life.Network;
using Life.UI;
using ModKit.Helper;
using ModKit.Interfaces;
using System.Collections.Generic;
using _menu = AAMenu.Menu;
using mk = ModKit.Helper.TextFormattingHelper;

namespace JobImpound
{
    public class JobImpound : ModKit.ModKit
    {

        public JobImpound(IGameAPI api) : base(api)
        {
            PluginInformations = new PluginInformations(AssemblyHelper.GetName(), "1.0.0", "Aarnow");
        }

        public override void OnPluginInit()
        {
            base.OnPluginInit();
        
            ModKit.Internal.Logger.LogSuccess($"{PluginInformations.SourceName} v{PluginInformations.Version}", "initialisé");
        }
    }
}
