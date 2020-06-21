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
        private PropertyObject HeroFixEquipmentProperty = null;

        public HeroFixBehavior()
        {
            HeroFixEquipmentProperty = new PropertyObject("zenDzeeMods_fix_equipment");
            PropertyObject tmp = ZenDzeeCompatibilityHelper.RegisterPresumedObject(HeroFixEquipmentProperty);
            if (tmp != null)
            {
                HeroFixEquipmentProperty = tmp;
            }
            HeroFixEquipmentProperty.Initialize(new TextObject("zenDzeeMods_fix_equipment"),
                new TextObject("Non-zero value - equipment is fixed."));
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnGivenBirthEvent.AddNonSerializedListener(this, OnGivenBirth);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        private void OnHeroGrows(Hero hero)
        {
            HeroFixHelper.FixHeroStats(hero);

            if (!hero.CharacterObject.IsOriginalCharacter
                && hero.HeroDeveloper.GetPropertyValue(HeroFixEquipmentProperty) == 0)
            {
                hero.HeroDeveloper.SetPropertyValue(HeroFixEquipmentProperty, 1);
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

        private void OnDailyTick()
        {
            foreach (Hero hero in Hero.All.Where(h => !h.IsTemplate && !h.IsMinorFactionHero && h.IsNoble))
            {
                if ((int)hero.BirthDay.ElapsedDaysUntilNow % CampaignTime.DaysInYear == 0)
                {
                    OnHeroGrows(hero);
                }
            }
        }
    }
}