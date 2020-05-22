using System.Collections.Generic;
using System.Linq;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Barterables;
using System.Text.RegularExpressions;

namespace zenDzeeMods_Heritage
{
    internal class MarriageFixBehavior : CampaignBehaviorBase
    {
        private Hero playerProposalHero = null;
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

        private void OnConsequenceRunnedEvent(ConversationSentence sentence)
        {
            if (sentence.OutputToken == lord_talk_speak_diplomacy_2)
            {
                // I have no choice but to remove the dead heroes, otherwise
                // the game will consider them as candidates for marriage.
                ICollection<Hero> heroes = Hero.OneToOneConversationHero.Clan.Heroes as ICollection<Hero>;
                if (heroes != null)
                {
                    Hero del;
                    while ((del = heroes.FirstOrDefault(h => h.IsDead)) != null)
                    {
                        heroes.Remove(del);
                    }
                }
            }
            
            if (sentence.Id == "lord_propose_marriage_conv_general_proposal_2"
                && sentence.InputToken == lord_propose_marriage_to_clan_leader_options
                && sentence.OutputToken == lord_propose_marriage_to_clan_leader_response)
            {
                CharacterObject characterObject = ConversationSentence.LastSelectedRepeatObject as CharacterObject;
                if (characterObject != null)
                {
                    playerProposalHero = characterObject.HeroObject;
                }
            }
            else if (sentence.InputToken == lord_propose_marriage_to_clan_leader_response
                && sentence.OutputToken == lord_propose_marriage_to_clan_leader_response_self)
            {
                proposedSpouseForPlayerRelative = Hero.OneToOneConversationHero;
            }
            else if (sentence.InputToken == lord_propose_marriage_to_clan_leader_response_self
                && sentence.OutputToken == lord_start_courtship_response
                && playerProposalHero != null && proposedSpouseForPlayerRelative != null)
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
                && playerProposalHero != null && proposedSpouseForPlayerRelative != null)
            {
                StartMarriageBarter();
            }
            else
            {
                proposedSpouseForPlayerRelative = null;
                playerProposalHero = null;
            }
        }

        private void StartMarriageBarter()
        {
            Hero offererHero = Hero.MainHero;
            MobileParty partyBelongedTo = offererHero.PartyBelongedTo;
            PartyBase offererParty = (partyBelongedTo != null) ? partyBelongedTo.Party : null;

            Hero otherHero = Hero.OneToOneConversationHero;
            partyBelongedTo = otherHero.PartyBelongedTo;
            PartyBase otherParty = (partyBelongedTo != null) ? partyBelongedTo.Party : null;

            Barterable barterable = new MarriageBarterable(otherHero, otherParty, proposedSpouseForPlayerRelative, playerProposalHero);
            barterable.SetIsOffered(true);
            List<Barterable> offer = new List<Barterable>() { barterable };

            BarterManager.Instance.StartBarterOffer(offererHero, otherHero, offererParty, otherParty, null, null, 0, false, offer);
        }
    }
}