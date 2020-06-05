using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using zenDzeeMods;

namespace zenDzeeMods_Heritage
{
    internal class HeroFixBehavior : CampaignBehaviorBase
    {
        public override void SyncData(IDataStore dataStore)
        {
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnGivenBirthEvent.AddNonSerializedListener(this, OnGivenBirth);
            //CampaignEvents.HeroReachesTeenAgeEvent.AddNonSerializedListener(this, OnHeroGrows);
            //CampaignEvents.HeroGrowsOutOfInfancyEvent.AddNonSerializedListener(this, OnHeroGrows);
            //CampaignEvents.HeroComesOfAgeEvent.AddNonSerializedListener(this, OnHeroGrows);

            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        private void OnChildGrows(Hero hero)
        {
            HeroFixHelper.FixHeroStats(hero);
            HeroFixHelper.FixEquipment(hero);
        }
        
        private void OnGivenBirth(Hero mother, List<Hero> children, int arg3)
        {
            foreach (Hero child in children)
            {
                if (child.IsAlive)
                {
                    HeroFixHelper.FixHeroStats(child);

                    if (!child.Mother.IsNoble && child.Father.IsNoble)
                    {
                        child.IsNoble = true;
                        child.Clan = child.Father.Clan;
                    }
                    else if (child.Mother.IsNoble && !child.Father.IsNoble)
                    {
                        child.IsNoble = true;
                        child.Clan = child.Mother.Clan;
                    }
                }
            }
        }

        private void OnDailyTick()
        {
            foreach (Hero hero in Hero.All.Where(h => !h.IsTemplate && !h.IsMinorFactionHero && h.IsNoble))
            {
                if ((int)hero.BirthDay.ElapsedDaysUntilNow % CampaignTime.DaysInYear == 0)
                {
                    OnChildGrows(hero);
                }
            }
        }
    }
}