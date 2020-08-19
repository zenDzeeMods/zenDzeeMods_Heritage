using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using zenDzeeMods;

namespace zenDzeeMods_Heritage
{
    internal class HeritageBehavior : CampaignBehaviorBase
    {
        private Hero newLeader = null;
        private Hero leader = null;

        private Hero characterWindowHero = null;
        private MobileParty characterWindowParty = null;
        private List<string> characterWindowEvents = null;
        private Settlement characterWindowSettlement = null;


        public override void SyncData(IDataStore dataStore)
        {
        }
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        private void OnSessionLaunched(CampaignGameStarter campaignStarter)
        {
            campaignStarter.AddPlayerLine("zendzee_legacy_start_leader", "companion_role", "zendzee_legacy_leader_pretalk",
                "{=zeeD75C6D0E}I want you to lead our clan.",
                ConditionChangeClanLeader, null, 110);
            campaignStarter.AddDialogLine("zendzee_legacy_candidate_pretalk", "zendzee_legacy_leader_pretalk", "zendzee_legacy_leader_need_confirm",
                "{=zee4E3D805F}Are you sure? This is a serious change...",
                null, null);
            campaignStarter.AddPlayerLine("zendzee_legacy_pretalk_confirm", "zendzee_legacy_leader_need_confirm", "zendzee_legacy_leader_confirm",
                "{=zee1E20B79B}Yes. I am absolutely sure.",
                null, null);
            campaignStarter.AddPlayerLine("zendzee_legacy_pretalk_nevermind", "zendzee_legacy_leader_need_confirm", "companion_okay",
                "{=mdNRYlfS}Nevermind.",
                null, null);
            campaignStarter.AddDialogLine("zendzee_legacy_candidate_accept", "zendzee_legacy_leader_confirm", "hero_main_options",
                "{=5hhxQBTj}I will be honored.[rb:positive]",
                null, ConsequenceChangeClanLeader);

            campaignStarter.AddPlayerLine("zendzee_charcter_window_open", "companion_role", "zendzee_charcter_window_active",
                "{=zee9065692B}Let me check your progress.",
                ConditionOpenCharacterWindow, ConsequenceCharacterWindowOpen, 110);
            campaignStarter.AddPlayerLine("zendzee_inventory_window_open", "companion_role", "zendzee_charcter_window_active",
                "{=zeeA3B242F4}Do you need new equipment?",
                ConditionOpenCharacterWindow, ConsequenceInventoryWindowOpen, 110);
            campaignStarter.AddDialogLine("zendzee_charcter_window_reply1", "zendzee_charcter_window_active", "zendzee_charcter_window_close",
                "{=zee376CA300}Is everything alright?",
                null, null);
            campaignStarter.AddPlayerLine("zendzee_charcter_window_reply2", "zendzee_charcter_window_close", "companion_okay",
                "{=zee5F9A2B79}Yes.",
                null, ConsequenceCharacterWindowClose);
        }

        private static bool ConditionOpenCharacterWindow()
        {
            return Hero.OneToOneConversationHero != null
                && Clan.PlayerClan != null
                && Hero.OneToOneConversationHero.Clan == Clan.PlayerClan
                && Hero.MainHero != null
                && MobileParty.MainParty != null
                && MobileParty.MainParty != Hero.OneToOneConversationHero.PartyBelongedTo;
        }

        private void PrepareCharacterWindow_Internal()
        {
            characterWindowSettlement = characterWindowHero.StayingInSettlementOfNotable;

            characterWindowEvents = new List<string>(characterWindowHero.GetHeroOccupiedEvents());
            string evt;
            while ((evt = characterWindowHero.GetHeroOccupiedEvents().FirstOrDefault()) != default)
            {
                characterWindowHero.RemoveEventFromOccupiedHero(evt);
            }

            characterWindowParty = characterWindowHero.PartyBelongedTo;
            if (characterWindowParty != MobileParty.MainParty)
            {
                if (characterWindowParty != null)
                {
                    characterWindowParty.MemberRoster.AddToCounts(characterWindowHero.CharacterObject, -1);
                }
                MobileParty.MainParty.MemberRoster.AddToCounts(characterWindowHero.CharacterObject, 1);
            }
        }

        private void ConsequenceCharacterWindowOpen()
        {
            characterWindowHero = Hero.OneToOneConversationHero;
            if (Hero.MainHero != null && MobileParty.MainParty != null && characterWindowHero != null)
            {
                PrepareCharacterWindow_Internal();

                Game.Current.GameStateManager.PushState(Game.Current.GameStateManager.CreateState<CharacterDeveloperState>(), 0);
            }
        }

        private void ConsequenceInventoryWindowOpen()
        {
            characterWindowHero = Hero.OneToOneConversationHero;
            if (Hero.MainHero != null && MobileParty.MainParty != null && characterWindowHero != null)
            {
                PrepareCharacterWindow_Internal();

                InventoryManager.OpenScreenAsInventory(null);
            }
        }

        private void ConsequenceCharacterWindowClose()
        {
            if (Hero.MainHero != null && MobileParty.MainParty != null && characterWindowHero != null)
            {
                if (characterWindowParty != MobileParty.MainParty)
                {
                    MobileParty.MainParty.MemberRoster.AddToCounts(characterWindowHero.CharacterObject, -1);
                    if (characterWindowParty != null)
                    {
                        characterWindowParty.MemberRoster.AddToCounts(characterWindowHero.CharacterObject, 1);
                        characterWindowParty.ChangePartyLeader(characterWindowHero.CharacterObject);
                    }
                }

                foreach (string e in characterWindowEvents)
                {
                    characterWindowHero.AddEventForOccupiedHero(e);
                }

                characterWindowHero.StayingInSettlementOfNotable = characterWindowSettlement;
            }

            characterWindowHero = null;
            characterWindowParty = null;
            characterWindowEvents = null;
        }

        private void ConsequenceChangeClanLeader()
        {
            newLeader = Hero.OneToOneConversationHero;
            leader = Hero.MainHero;

            if (newLeader.PartyBelongedTo == null || newLeader != newLeader.PartyBelongedTo.LeaderHero)
            {
                MobilePartyHelper.CreateNewClanMobileParty(newLeader, leader.Clan, out _);
            }

            if (leader.PartyBelongedTo != null && leader.PartyBelongedTo.CurrentSettlement != null)
            {
                EnterSettlementAction.ApplyForParty(newLeader.PartyBelongedTo, leader.PartyBelongedTo.CurrentSettlement);
                LeaveSettlementAction.ApplyForParty(leader.PartyBelongedTo);
            }

            CampaignEvents.TickEvent.AddNonSerializedListener(this, OnChangeClanLeader);
        }

        private void OnChangeClanLeader(float dt)
        {
            MobileParty leaderParty = leader.PartyBelongedTo;
            MobileParty newLeaderParty = newLeader.PartyBelongedTo;

            if (leader == null || newLeader == null || leaderParty == null || newLeaderParty == null || leaderParty == newLeaderParty)
            {
                return;
            }

            if (newLeader.GovernorOf != null)
            {
                ChangeGovernorAction.ApplyByGiveUpCurrent(newLeader);
            }
            if (leader.GovernorOf != null)
            {
                ChangeGovernorAction.ApplyByGiveUpCurrent(leader);
            }

            var qm = Campaign.Current.QuestManager;
            if (qm != null)
            {
                QuestBase q;
                while ((q = qm.Quests.FirstOrDefault(s => !s.IsSpecialQuest)) != null)
                {
                    q.CompleteQuestWithFail();
                }
            }

            string evt;
            while ((evt = newLeader.GetHeroOccupiedEvents().FirstOrDefault()) != default)
            {
                newLeader.RemoveEventFromOccupiedHero(evt);
            }
            while ((evt = leader.GetHeroOccupiedEvents().FirstOrDefault()) != default)
            {
                leader.RemoveEventFromOccupiedHero(evt);
            }

            GiveGoldAction.ApplyBetweenCharacters(leader, newLeader, leader.Gold, true);

            leader.Clan.SetLeader(newLeader);
            ChangePlayerCharacterAction.Apply(newLeader);

            TroopRosterElement t;
            while (leaderParty.MemberRoster.Count(x => x.Character != leader.CharacterObject) > 0)
            {
                t = leaderParty.MemberRoster.First(x => x.Character != leader.CharacterObject);
                leaderParty.MemberRoster.AddToCounts(t.Character, -t.Number);
                newLeaderParty.MemberRoster.AddToCounts(t.Character, t.Number);
            }

            while (leaderParty.PrisonRoster.Count() > 0)
            {
                t = leaderParty.PrisonRoster.First();
                leaderParty.PrisonRoster.AddToCounts(t.Character, -t.Number);
                newLeaderParty.PrisonRoster.AddToCounts(t.Character, t.Number);
            }

            ItemRosterElement i;
            while (leaderParty.ItemRoster.Count() > 0)
            {
                i = leaderParty.ItemRoster.First();
                leaderParty.ItemRoster.AddToCounts(i.EquipmentElement, -i.Amount);
                newLeaderParty.ItemRoster.AddToCounts(i.EquipmentElement, i.Amount);
            }

            leaderParty.RemoveParty();
            AddHeroToPartyAction.Apply(leader, newLeaderParty);

            newLeader.Clan.Influence = Math.Max(0, newLeader.Clan.Influence - 100);

            HeroFixHelper.FixHeroStats(newLeader);
            HeroFixHelper.FixEquipment(newLeader);

            newLeader = null;
            leader = null;
            CampaignEvents.RemoveListeners(this);
        }

        private bool ConditionChangeClanLeader()
        {
            return Hero.OneToOneConversationHero != null
                && Clan.PlayerClan != null
                && newLeader == null
                && Hero.OneToOneConversationHero.Clan == Clan.PlayerClan
                && Hero.MainHero == Clan.PlayerClan.Leader
                && Hero.MainHero.PartyBelongedTo != null
                && (IsFamilyMember(Hero.MainHero, Hero.OneToOneConversationHero) || IsMarried(Hero.MainHero, Hero.OneToOneConversationHero) || IsBloodRelated(Hero.MainHero, Hero.OneToOneConversationHero));
        }

        private static bool IsFamilyMember(Hero familyHero, Hero hero)
        {
            if (familyHero.Clan.Lords == null
                || familyHero.Clan.Lords.IsEmpty())
            {
                return false;
            }

            return familyHero.Clan.Lords.Contains(hero);
        }

        private static bool IsBloodRelated(Hero hero1, Hero hero2)
        {
            return hero1.Siblings.Contains(hero2) || IsChildOf(hero1, hero2) || IsChildOf(hero2, hero1) || HaveSameParent(hero1, hero2);
        }

        private static bool IsChildOf(Hero hero1, Hero hero2)
        {
            foreach (Hero child in hero1.Children)
            {
                if (child == hero2) return true;

                if (IsChildOf(child, hero2)) return true;
            }
            return false;
        }

        private static bool IsMarried(Hero hero1, Hero hero2)
        {
            return (hero1.Spouse == hero2) || (hero2.Spouse == hero1);
        }

        private static bool HaveSameParent(Hero hero1, Hero hero2)
        {
            return (hero1.Father != null && (hero1.Father == hero2.Father)) || (hero1.Mother != null && (hero1.Mother == hero2.Mother));
        }
    }
}
