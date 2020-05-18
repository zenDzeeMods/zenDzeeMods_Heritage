using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace zenDzeeMods_Heritage
{
    public class SubModule : MBSubModuleBase
    {
        protected override void OnGameStart(Game game, IGameStarter gameStarter)
        {
            if (game.GameType is Campaign)
            {
                CampaignGameStarter campaignStarter = (CampaignGameStarter)gameStarter;

                campaignStarter.AddBehavior(new HeritageBehavior());
                campaignStarter.AddBehavior(new HeroFixBehavior());
                campaignStarter.AddBehavior(new MarriageFixBehavior());
            }
        }
    }
}