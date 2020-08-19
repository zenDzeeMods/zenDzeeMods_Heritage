using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using zenDzeeMods;

namespace zenDzeeMods_Heritage
{
    internal class HeroFixBehavior : CampaignBehaviorBase
    {
        public HeroFixBehavior()
        {
            PropertyObject FixEquipmentProperty = new PropertyObject("zenDzeeMods_fix_equipment");
            PropertyObject tmp = ZenDzeeCompatibilityHelper.RegisterPresumedObject(FixEquipmentProperty);
            if (tmp != null)
            {
                FixEquipmentProperty = tmp;
            }
            FixEquipmentProperty.Initialize(new TextObject("zenDzeeMods_fix_equipment"),
                new TextObject("Non-zero value - equipment is fixed."));

            HeroFixHelper.HeroFixEquipmentProperty = FixEquipmentProperty;
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnGivenBirthEvent.AddNonSerializedListener(this, OnGivenBirth);
            //CampaignEvents.DailyTickHeroEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        private void OnHeroGrows(Hero hero)
        {
            HeroFixHelper.FixHeroStats(hero);

            if (hero.Age > 6f
                && !hero.CharacterObject.IsOriginalCharacter)
            {
                HeroFixHelper.FixEquipment(hero);
            }
        }

        private static void OnGivenBirth(Hero mother, List<Hero> children, int arg3)
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

// Should be fixed by e1.4.3
#if false
        private void OnDailyTick(Hero hero)
        {
            if (!hero.IsTemplate && !hero.IsMinorFactionHero && hero.IsNoble)
            {
                if (hero.IsAlive && (hero.Age - hero.DynamicBodyProperties.Age) > 1f)
                {
                    DynamicBodyProperties dp = hero.DynamicBodyProperties;
                    dp.Age = hero.Age;
                    hero.DynamicBodyProperties = dp;
                }

                if ((int)hero.BirthDay.ElapsedDaysUntilNow % CampaignTime.DaysInYear == 0)
                {
                    OnHeroGrows(hero);
                }
            }
        }
#endif
    }
}
