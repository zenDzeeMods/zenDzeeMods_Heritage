using System.Collections.Generic;
using System.Linq;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Barterables;
using System.Text.RegularExpressions;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;

namespace zenDzeeMods_Heritage
{
    internal class MarriageFixBehavior : CampaignBehaviorBase
    {
        private bool shouldJoinPartyAfterOffer = false;

        private Hero playerRelative = null;
        private Hero proposedSpouseForPlayerRelative = null;

        // converstion tags
        private int lord_propose_marriage_to_clan_leader_options = 0;
        private int lord_propose_marriage_to_clan_leader_response = 0;
        private int lord_propose_marriage_to_clan_leader_response_self = 0;
        private int lord_start_courtship_response = 0;
        private int lord_propose_marriage_to_clan_leader_response_other = 0;
        private int lord_propose_marriage_to_clan_leader_confirm = 0;
        private int lord_talk_speak_diplomacy_2 = 0;

        public override void SyncData(IDataStore dataStore)
        {
        }
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.RomanticStateChanged.AddNonSerializedListener(this, OnRomanticStateChanged);
        }

        private void OnRomanticStateChanged(Hero hero1, Hero hero2, Romance.RomanceLevelEnum toWhat)
        {
            if (((playerRelative == hero1 && proposedSpouseForPlayerRelative == hero2)
                || (playerRelative == hero2 && proposedSpouseForPlayerRelative == hero1))
                && toWhat == Romance.RomanceLevelEnum.Marriage)
            {
                shouldJoinPartyAfterOffer = false;
            }
        }

        private void OnSessionLaunched(CampaignGameStarter gameStarter)
        {
            Campaign.Current.ConversationManager.ConsequenceRunned += OnConsequenceRunnedEvent;

            lord_propose_marriage_to_clan_leader_options = Campaign.Current.ConversationManager.GetStateIndex("lord_propose_marriage_to_clan_leader_options");
            lord_propose_marriage_to_clan_leader_response = Campaign.Current.ConversationManager.GetStateIndex("lord_propose_marriage_to_clan_leader_response");
            lord_propose_marriage_to_clan_leader_response_self = Campaign.Current.ConversationManager.GetStateIndex("lord_propose_marriage_to_clan_leader_response_self");
            lord_start_courtship_response = Campaign.Current.ConversationManager.GetStateIndex("lord_start_courtship_response");
            lord_propose_marriage_to_clan_leader_response_other = Campaign.Current.ConversationManager.GetStateIndex("lord_propose_marriage_to_clan_leader_response_other");
            lord_propose_marriage_to_clan_leader_confirm = Campaign.Current.ConversationManager.GetStateIndex("lord_propose_marriage_to_clan_leader_confirm");
            lord_talk_speak_diplomacy_2 = Campaign.Current.ConversationManager.GetStateIndex("lord_talk_speak_diplomacy_2");

            gameStarter.AddDialogLine("zendzee_lord_refuses_barter", "lord_propose_marriage_to_clan_leader", "lord_start",
                "{=3L8xN9uC}I believe it hasn't been long since we've last bartered.",
                ConditionRefuseBarterDueCoolDown, null, 200, null);
        }

        private bool ConditionRefuseBarterDueCoolDown()
        {
            return !BarterManager.Instance.CanPlayerBarterWithHero(Hero.OneToOneConversationHero);
        }

        private static void RemoveDeadClanMembers(ICollection<Hero> heroes)
        {
            if (heroes != null)
            {
                Hero del;
                while ((del = heroes.FirstOrDefault(h => h.IsDead)) != null)
                {
                    heroes.Remove(del);
                }
            }
        }

        private void OnConsequenceRunnedEvent(ConversationSentence sentence)
        {
            if (sentence.OutputToken == lord_talk_speak_diplomacy_2)
            {
                // I have no choice but to remove the dead heroes, otherwise
                // the game will consider them as candidates for marriage.
                RemoveDeadClanMembers(Hero.OneToOneConversationHero.Clan.Lords as ICollection<Hero>);
                RemoveDeadClanMembers(Hero.MainHero.Clan.Lords as ICollection<Hero>);
            }

            if (sentence.Id == "lord_propose_marriage_conv_general_proposal_2"
                && sentence.InputToken == lord_propose_marriage_to_clan_leader_options
                && sentence.OutputToken == lord_propose_marriage_to_clan_leader_response)
            {
                CharacterObject characterObject = ConversationSentence.LastSelectedRepeatObject as CharacterObject;
                if (characterObject != null)
                {
                    playerRelative = characterObject.HeroObject;

                    // Bug in MarriageAction:
                    // if hero in party - party will be disbanded.
                    // Remove hero temporarily from the MainParty.
                    if (playerRelative.PartyBelongedTo != null
                        && playerRelative.PartyBelongedTo == MobileParty.MainParty)
                    {
                        MobileParty.MainParty.AddElementToMemberRoster(playerRelative.CharacterObject, -1);
                        shouldJoinPartyAfterOffer = true;
                    }
                }
            }
            else if (sentence.InputToken == lord_propose_marriage_to_clan_leader_response
                && sentence.OutputToken == lord_propose_marriage_to_clan_leader_response_self)
            {
                proposedSpouseForPlayerRelative = Hero.OneToOneConversationHero;
            }
            else if (sentence.InputToken == lord_propose_marriage_to_clan_leader_response_self
                && sentence.OutputToken == lord_start_courtship_response
                && playerRelative != null && proposedSpouseForPlayerRelative != null)
            {
                StartMarriageBarter();
            }
            else if (sentence.InputToken == lord_propose_marriage_to_clan_leader_response
                && sentence.OutputToken == lord_propose_marriage_to_clan_leader_response_other)
            {
                Regex reHref = new Regex("<a .* href=\"event:Hero-(.*)\">");
                var href = reHref.Match(sentence.Text.ToString());
                if (href.Success)
                {
                    string id = href.Groups[1].Captures[0].ToString();
                    proposedSpouseForPlayerRelative = Hero.OneToOneConversationHero.Clan.Heroes.FirstOrDefault(h => h.StringId == id);
                }
            }
            else if (sentence.InputToken == lord_propose_marriage_to_clan_leader_response_other
                && sentence.OutputToken == lord_propose_marriage_to_clan_leader_confirm
                && playerRelative != null && proposedSpouseForPlayerRelative != null)
            {
                StartMarriageBarter();
            }
            else
            {
                if (shouldJoinPartyAfterOffer
                    && playerRelative != null
                    && playerRelative.PartyBelongedTo == null)
                {
                    AddHeroToPartyAction.Apply(playerRelative, MobileParty.MainParty);
                    shouldJoinPartyAfterOffer = false;
                }

                proposedSpouseForPlayerRelative = null;
                playerRelative = null;
            }
        }

        private void StartMarriageBarter()
        {
            shouldJoinPartyAfterOffer = false;
            Hero offererHero = Hero.MainHero;
            MobileParty partyBelongedTo = offererHero.PartyBelongedTo;
            PartyBase offererParty = (partyBelongedTo != null) ? partyBelongedTo.Party : null;

            Hero otherHero = Hero.OneToOneConversationHero;
            partyBelongedTo = otherHero.PartyBelongedTo;
            PartyBase otherParty = (partyBelongedTo != null) ? partyBelongedTo.Party : null;

            Barterable barterable = new MarriageBarterable(otherHero, otherParty, proposedSpouseForPlayerRelative, playerRelative);
            barterable.SetIsOffered(true);
            List<Barterable> offer = new List<Barterable>() { barterable };

            BarterManager.Instance.StartBarterOffer(offererHero, otherHero, offererParty, otherParty, null, null, 0, false, offer);
        }
    }
}