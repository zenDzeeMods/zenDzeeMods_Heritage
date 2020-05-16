using Helpers;
using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace zenDzeeMods
{
    internal class HeroFixHelper
    {
        private static List<CharacterAttributesEnum> attrList = new List<CharacterAttributesEnum> {
            CharacterAttributesEnum.Vigor,
            CharacterAttributesEnum.Control,
            CharacterAttributesEnum.Endurance,
            CharacterAttributesEnum.Cunning,
            CharacterAttributesEnum.Social,
            CharacterAttributesEnum.Intelligence,
        };

        public static void FixHeroStats(Hero hero)
        {
            if (hero.IsDead || hero.IsDisabled)
            {
                return;
            }

            bool corrected = false;

            // attr
            int attrSum = hero.HeroDeveloper.UnspentAttributePoints;
            foreach (CharacterAttributesEnum attr in attrList)
            {
                attrSum += hero.GetAttributeValue(attr);
            }

            int attrExpected = 2 * 6 + 6; // min * attrs + backstory
            attrExpected += hero.Level / Campaign.Current.Models.CharacterDevelopmentModel.LevelsPerAttributePoint;

            int diff_attr = Math.Max(0, attrExpected - attrSum);

            if (diff_attr > 0)
            {
                hero.HeroDeveloper.UnspentAttributePoints += diff_attr;
                FixAttribute(hero);
                corrected = true;
            }


            // focus
            int focusSum = hero.HeroDeveloper.UnspentFocusPoints;
            int skillSum = 0;
            foreach (SkillObject skill in Game.Current.SkillList)
            {
                focusSum += hero.HeroDeveloper.GetFocus(skill);
                skillSum += hero.GetSkillValue(skill);
            }

            int focusExpected = 2 * 6; // backstory
            focusExpected += hero.Level / Campaign.Current.Models.CharacterDevelopmentModel.FocusPointsPerLevel;

            int diff_skill = Math.Max(0, focusExpected * 20 + 1 - skillSum);
            if (diff_skill > 0)
            {
                FixSkills(hero, diff_skill);
                corrected = true;
            }

            int diff_focus = Math.Max(0, focusExpected - focusSum);

            if (diff_focus > 0)
            {
                hero.HeroDeveloper.UnspentFocusPoints += diff_focus;
            }

            if (corrected)
            {
                //InformationManager.DisplayMessage(new InformationMessage("FIXED: " + hero.Name + " added attributes: " + diff_attr + ", skills: " + diff_skill));
            }

            if ((hero.IsFemale && hero.Mother != null) || (!hero.IsFemale && hero.Father != null))
            {
                StaticBodyProperties sbpHero = hero.BodyProperties.StaticProperties;
                StaticBodyProperties sbpOrig;
                if (hero.IsFemale)
                {
                    sbpOrig = hero.Mother.BodyProperties.StaticProperties;
                }
                else
                {
                    sbpOrig = hero.Father.BodyProperties.StaticProperties;
                }

                if (CompareBodySliders(sbpOrig, sbpHero))
                {
                    RandomizeAppearance(hero);
                    //InformationManager.DisplayMessage(new InformationMessage("FIXED: " + hero.Name + " appearance randomized"));
                }
            }
        }

        private static void FixSkills(Hero hero, double diffSkill)
        {
            int val;
            double skillAdd = Game.Current.SkillList.Count > 0 ? Math.Ceiling(diffSkill / Game.Current.SkillList.Count) : 1;
            foreach (SkillObject skill in Game.Current.SkillList)
            {
                val = hero.GetSkillValue(skill);
                val += (int)skillAdd;
                hero.SetSkillValue(skill, val);
                float xp = hero.HeroDeveloper.GetPropertyValue(skill);
                float xpNeeded = Campaign.Current.Models.CharacterDevelopmentModel.GetXpRequiredForSkillLevel(val);
                if (xpNeeded > xp)
                {
                    hero.HeroDeveloper.SetPropertyValue(skill, xpNeeded);
                }
            }
        }

        private static void FixAttribute(Hero hero)
        {
            double sumOfParentsAttributes = 0;
            double val;
            int cur;

            foreach (CharacterAttributesEnum attr in attrList)
            {
                sumOfParentsAttributes += Math.Max(2, (hero.Father != null) ? hero.Father.GetAttributeValue(attr) : 0);
                sumOfParentsAttributes += Math.Max(2, (hero.Mother != null) ? hero.Mother.GetAttributeValue(attr) : 0);

                cur = hero.GetAttributeValue(attr);
                if (cur < 2 && hero.HeroDeveloper.UnspentAttributePoints > 0)
                {
                    AddAttribute(hero, attr, Math.Min(2 - cur, hero.HeroDeveloper.UnspentAttributePoints));
                }
            }

            double u = hero.HeroDeveloper.UnspentAttributePoints;
            foreach (CharacterAttributesEnum attr in attrList)
            {
                val = 0;
                val += Math.Max(2, (hero.Father != null) ? hero.Father.GetAttributeValue(attr) : 0);
                val += Math.Max(2, (hero.Mother != null) ? hero.Mother.GetAttributeValue(attr) : 0);
                val = (int)Math.Max(0, Math.Round(val * u / sumOfParentsAttributes));

                if (!hero.HeroDeveloper.IsHeroAtMaxAttribute())
                {
                    AddAttribute(hero, attr, Math.Min((int)val, hero.HeroDeveloper.UnspentAttributePoints));
                }
            }

            while (hero.HeroDeveloper.UnspentAttributePoints > 0 && !hero.HeroDeveloper.IsHeroAtMaxAttribute())
            {
                AddAttribute(hero, attrList.GetRandomElement(), 1);
            }
        }

        private static void AddAttribute(Hero hero, CharacterAttributesEnum attr, int val)
        {
            if (val > 0 && hero.HeroDeveloper.UnspentAttributePoints >= val)
            {
                int c = hero.GetAttributeValue(attr);
                hero.SetAttributeValue(attr, c + val);
                int diff = hero.GetAttributeValue(attr) - c;
                hero.HeroDeveloper.UnspentAttributePoints -= diff;
            }
        }

        private static void RandomizeAppearance(Hero hero)
        {
            // one of the parents should be not null
            Hero mother = hero.Mother != null ? hero.Mother : hero.Father;
            Hero father = hero.Father != null ? hero.Father : hero.Mother;

            StaticBodySliders motherBodySliders = new StaticBodySliders(mother.BodyProperties.StaticProperties);
            StaticBodySliders fatherBodySliders = new StaticBodySliders(father.BodyProperties.StaticProperties);
            StaticBodySliders heroBodySliders;

            // Hero's StaticProperties are broken, we have to use parents StaticProperties
            if (hero.IsFemale)
            {
                heroBodySliders = new StaticBodySliders(mother.BodyProperties.StaticProperties);
            }
            else
            {
                heroBodySliders = new StaticBodySliders(father.BodyProperties.StaticProperties);
            }

            heroBodySliders.FaceAsymmetry = GetRandomSliderValue(motherBodySliders.FaceAsymmetry, fatherBodySliders.FaceAsymmetry);
            heroBodySliders.FaceCenterHeight = GetRandomSliderValue(motherBodySliders.FaceCenterHeight, fatherBodySliders.FaceCenterHeight);
            heroBodySliders.FaceCheekboneDepth = GetRandomSliderValue(motherBodySliders.FaceCheekboneDepth, fatherBodySliders.FaceCheekboneDepth);
            heroBodySliders.FaceCheekboneHeight = GetRandomSliderValue(motherBodySliders.FaceCheekboneHeight, fatherBodySliders.FaceCheekboneHeight);
            heroBodySliders.FaceCheekboneWidth = GetRandomSliderValue(motherBodySliders.FaceCheekboneWidth, fatherBodySliders.FaceCheekboneWidth);
            heroBodySliders.FaceDepth = GetRandomSliderValue(motherBodySliders.FaceDepth, fatherBodySliders.FaceDepth);
            heroBodySliders.FaceEarSize = GetRandomSliderValue(motherBodySliders.FaceEarSize, fatherBodySliders.FaceEarSize);
            heroBodySliders.FaceEyeSocketSize = GetRandomSliderValue(motherBodySliders.FaceEyeSocketSize, fatherBodySliders.FaceEyeSocketSize);
            heroBodySliders.FaceRatio = GetRandomSliderValue(motherBodySliders.FaceRatio, fatherBodySliders.FaceRatio);
            heroBodySliders.FaceSharpness = GetRandomSliderValue(motherBodySliders.FaceSharpness, fatherBodySliders.FaceSharpness);
            heroBodySliders.FaceTempleWidth = GetRandomSliderValue(motherBodySliders.FaceTempleWidth, fatherBodySliders.FaceTempleWidth);
            heroBodySliders.FaceWeight = GetRandomSliderValue(motherBodySliders.FaceWeight, fatherBodySliders.FaceWeight);
            heroBodySliders.FaceWidth = GetRandomSliderValue(motherBodySliders.FaceWidth, fatherBodySliders.FaceWidth);
            heroBodySliders.EyeAsymmetry = GetRandomSliderValue(motherBodySliders.EyeAsymmetry, fatherBodySliders.EyeAsymmetry);
            heroBodySliders.EyeBrowInnerHeight = GetRandomSliderValue(motherBodySliders.EyeBrowInnerHeight, fatherBodySliders.EyeBrowInnerHeight);
            heroBodySliders.EyeBrowMiddleHeight = GetRandomSliderValue(motherBodySliders.EyeBrowMiddleHeight, fatherBodySliders.EyeBrowMiddleHeight);
            heroBodySliders.EyeDepth = GetRandomSliderValue(motherBodySliders.EyeDepth, fatherBodySliders.EyeDepth);
            heroBodySliders.EyeEyebrowDepth = GetRandomSliderValue(motherBodySliders.EyeEyebrowDepth, fatherBodySliders.EyeEyebrowDepth);
            heroBodySliders.EyeEyelidHeight = GetRandomSliderValue(motherBodySliders.EyeEyelidHeight, fatherBodySliders.EyeEyelidHeight);
            heroBodySliders.EyeInnerHeight = GetRandomSliderValue(motherBodySliders.EyeInnerHeight, fatherBodySliders.EyeInnerHeight);
            heroBodySliders.EyeMonolidEyes = GetRandomSliderValue(motherBodySliders.EyeMonolidEyes, fatherBodySliders.EyeMonolidEyes);
            heroBodySliders.EyeOuterHeight = GetRandomSliderValue(motherBodySliders.EyeOuterHeight, fatherBodySliders.EyeOuterHeight);
            heroBodySliders.EyePosition = GetRandomSliderValue(motherBodySliders.EyePosition, fatherBodySliders.EyePosition);
            heroBodySliders.EyeSize = GetRandomSliderValue(motherBodySliders.EyeSize, fatherBodySliders.EyeSize, 1);
            heroBodySliders.EyeToEyeDistance = GetRandomSliderValue(motherBodySliders.EyeToEyeDistance, fatherBodySliders.EyeToEyeDistance, 1);
            heroBodySliders.NoseAngle = GetRandomSliderValue(motherBodySliders.NoseAngle, fatherBodySliders.NoseAngle, 1);
            heroBodySliders.NoseAsymmetry = GetRandomSliderValue(motherBodySliders.NoseAsymmetry, fatherBodySliders.NoseAsymmetry, 1);
            heroBodySliders.NoseBridge = GetRandomSliderValue(motherBodySliders.NoseBridge, fatherBodySliders.NoseBridge, 1);
            heroBodySliders.NoseBump = GetRandomSliderValue(motherBodySliders.NoseBump, fatherBodySliders.NoseBump, 1);
            heroBodySliders.NoseDefenition = GetRandomSliderValue(motherBodySliders.NoseDefenition, fatherBodySliders.NoseDefenition, 1);
            heroBodySliders.NoseLength = GetRandomSliderValue(motherBodySliders.NoseLength, fatherBodySliders.NoseLength, 1);
            heroBodySliders.NoseNostrilHeight = GetRandomSliderValue(motherBodySliders.NoseNostrilHeight, fatherBodySliders.NoseNostrilHeight, 1);
            heroBodySliders.NoseNostrilSize = GetRandomSliderValue(motherBodySliders.NoseNostrilSize, fatherBodySliders.NoseNostrilSize, 1);
            heroBodySliders.NoseSize = GetRandomSliderValue(motherBodySliders.NoseSize, fatherBodySliders.NoseSize, 1);
            heroBodySliders.NoseTipHeight = GetRandomSliderValue(motherBodySliders.NoseTipHeight, fatherBodySliders.NoseTipHeight, 1);
            heroBodySliders.NoseWidth = GetRandomSliderValue(motherBodySliders.NoseWidth, fatherBodySliders.NoseWidth, 1);
            heroBodySliders.MouthChinForward = GetRandomSliderValue(motherBodySliders.MouthChinForward, fatherBodySliders.MouthChinForward, 2);
            heroBodySliders.MouthChinLength = GetRandomSliderValue(motherBodySliders.MouthChinLength, fatherBodySliders.MouthChinLength, 2);
            heroBodySliders.MouthForward = GetRandomSliderValue(motherBodySliders.MouthForward, fatherBodySliders.MouthForward, 2);
            heroBodySliders.MouthFrowSmile = GetRandomSliderValue(motherBodySliders.MouthFrowSmile, fatherBodySliders.MouthFrowSmile);
            heroBodySliders.MouthJawHeight = GetRandomSliderValue(motherBodySliders.MouthJawHeight, fatherBodySliders.MouthJawHeight);
            heroBodySliders.MouthJawLine = GetRandomSliderValue(motherBodySliders.MouthJawLine, fatherBodySliders.MouthJawLine, 2);
            heroBodySliders.MouthLipsConcaveConvex = GetRandomSliderValue(motherBodySliders.MouthLipsConcaveConvex, fatherBodySliders.MouthLipsConcaveConvex);
            heroBodySliders.MouthLipThickness = GetRandomSliderValue(motherBodySliders.MouthLipThickness, fatherBodySliders.MouthLipThickness);
            heroBodySliders.MouthPosition = GetRandomSliderValue(motherBodySliders.MouthPosition, fatherBodySliders.MouthPosition, 2);
            heroBodySliders.MouthTeethType = GetRandomSliderValue(motherBodySliders.MouthTeethType, fatherBodySliders.MouthTeethType);
            heroBodySliders.MouthWidth = GetRandomSliderValue(motherBodySliders.MouthWidth, fatherBodySliders.MouthWidth);

            heroBodySliders.EyeColor = ChoseRandomSliderValue(motherBodySliders.EyeColor, fatherBodySliders.EyeColor);
            heroBodySliders.EyeShape = ChoseRandomSliderValue(motherBodySliders.EyeShape, fatherBodySliders.EyeShape);
            heroBodySliders.FaceEarShape = ChoseRandomSliderValue(motherBodySliders.FaceEarShape, fatherBodySliders.FaceEarShape);
            heroBodySliders.MouthBottomLipShape = ChoseRandomSliderValue(motherBodySliders.MouthBottomLipShape, fatherBodySliders.MouthBottomLipShape);
            heroBodySliders.MouthChinShape = ChoseRandomSliderValue(motherBodySliders.MouthChinShape, fatherBodySliders.MouthChinShape);
            heroBodySliders.MouthJawShape = ChoseRandomSliderValue(motherBodySliders.MouthJawShape, fatherBodySliders.MouthJawShape);
            heroBodySliders.MouthTopLipShape = ChoseRandomSliderValue(motherBodySliders.MouthTopLipShape, fatherBodySliders.MouthTopLipShape);
            heroBodySliders.NoseShape = ChoseRandomSliderValue(motherBodySliders.NoseShape, fatherBodySliders.NoseShape);
            heroBodySliders.HairColor = ChoseRandomSliderValue(motherBodySliders.HairColor, fatherBodySliders.HairColor);
            heroBodySliders.SkinColor = ChoseRandomSliderValue(motherBodySliders.SkinColor, fatherBodySliders.SkinColor);

            heroBodySliders.MarkingsColor = 0;
            heroBodySliders.MarkingsType = 0;

            // note: current version of the game supports DynamicBodyProperties only if character's age is 22+
            // If character under 22 years old, then he cannot be changed via Character CUstomization GUI.
            BodyProperties newBodyProperties = new BodyProperties(hero.DynamicBodyProperties, heroBodySliders.GetStaticBodyProperties());

            BasicCharacterObject tmp = Game.Current.PlayerTroop;
            Game.Current.PlayerTroop = hero.CharacterObject;
            hero.CharacterObject.UpdatePlayerCharacterBodyProperties(newBodyProperties, hero.IsFemale);
            Game.Current.PlayerTroop = tmp;
        }

        private static byte GetRandomSliderValue(byte v1, byte v2, byte extraRandomization = 4, byte maxValue = 0xF)
        {
            float diff = Math.Abs(v1 - v2) + extraRandomization + extraRandomization;
            float vx = diff * MBRandom.RandomFloat - extraRandomization;
            vx += Math.Min(v1, v2);
            return (byte)Math.Max(0, Math.Min(maxValue, vx));
        }

        private static byte ChoseRandomSliderValue(byte v1, byte v2)
        {
            return (MBRandom.RandomFloat > 0.5) ? v1 : v2;
        }

        private static bool CompareBodySliders(StaticBodyProperties sbp1, StaticBodyProperties sbp2)
        {
            StaticBodySliders sbp1bs = new StaticBodySliders(sbp1);
            StaticBodySliders sbp2bs = new StaticBodySliders(sbp2);

            if (sbp1bs.FaceAsymmetry != sbp2bs.FaceAsymmetry) return false;
            if (sbp1bs.FaceCenterHeight != sbp2bs.FaceCenterHeight) return false;
            if (sbp1bs.FaceCheekboneDepth != sbp2bs.FaceCheekboneDepth) return false;
            if (sbp1bs.FaceCheekboneHeight != sbp2bs.FaceCheekboneHeight) return false;
            if (sbp1bs.FaceCheekboneWidth != sbp2bs.FaceCheekboneWidth) return false;
            if (sbp1bs.FaceDepth != sbp2bs.FaceDepth) return false;
            if (sbp1bs.FaceEarShape != sbp2bs.FaceEarShape) return false;
            if (sbp1bs.FaceEarSize != sbp2bs.FaceEarSize) return false;
            if (sbp1bs.FaceEyeSocketSize != sbp2bs.FaceEyeSocketSize) return false;
            if (sbp1bs.FaceRatio != sbp2bs.FaceRatio) return false;
            if (sbp1bs.FaceSharpness != sbp2bs.FaceSharpness) return false;
            if (sbp1bs.FaceTempleWidth != sbp2bs.FaceTempleWidth) return false;
            if (sbp1bs.FaceWeight != sbp2bs.FaceWeight) return false;
            if (sbp1bs.FaceWidth != sbp2bs.FaceWidth) return false;
            if (sbp1bs.EyeAsymmetry != sbp2bs.EyeAsymmetry) return false;
            if (sbp1bs.EyeBrowInnerHeight != sbp2bs.EyeBrowInnerHeight) return false;
            if (sbp1bs.EyeBrowMiddleHeight != sbp2bs.EyeBrowMiddleHeight) return false;
            if (sbp1bs.EyeBrowOuterHeight != sbp2bs.EyeBrowOuterHeight) return false;
            if (sbp1bs.EyeColor != sbp2bs.EyeColor) return false;
            if (sbp1bs.EyeDepth != sbp2bs.EyeDepth) return false;
            if (sbp1bs.EyeEyebrowDepth != sbp2bs.EyeEyebrowDepth) return false;
            if (sbp1bs.EyeEyebrowType != sbp2bs.EyeEyebrowType) return false;
            if (sbp1bs.EyeEyelidHeight != sbp2bs.EyeEyelidHeight) return false;
            if (sbp1bs.EyeInnerHeight != sbp2bs.EyeInnerHeight) return false;
            if (sbp1bs.EyeMonolidEyes != sbp2bs.EyeMonolidEyes) return false;
            if (sbp1bs.EyeOuterHeight != sbp2bs.EyeOuterHeight) return false;
            if (sbp1bs.EyePosition != sbp2bs.EyePosition) return false;
            if (sbp1bs.EyeShape != sbp2bs.EyeShape) return false;
            if (sbp1bs.EyeSize != sbp2bs.EyeSize) return false;
            if (sbp1bs.EyeToEyeDistance != sbp2bs.EyeToEyeDistance) return false;
            if (sbp1bs.NoseAngle != sbp2bs.NoseAngle) return false;
            if (sbp1bs.NoseAsymmetry != sbp2bs.NoseAsymmetry) return false;
            if (sbp1bs.NoseBridge != sbp2bs.NoseBridge) return false;
            if (sbp1bs.NoseBump != sbp2bs.NoseBump) return false;
            if (sbp1bs.NoseDefenition != sbp2bs.NoseDefenition) return false;
            if (sbp1bs.NoseLength != sbp2bs.NoseLength) return false;
            if (sbp1bs.NoseNostrilHeight != sbp2bs.NoseNostrilHeight) return false;
            if (sbp1bs.NoseNostrilSize != sbp2bs.NoseNostrilSize) return false;
            if (sbp1bs.NoseShape != sbp2bs.NoseShape) return false;
            if (sbp1bs.NoseSize != sbp2bs.NoseSize) return false;
            if (sbp1bs.NoseTipHeight != sbp2bs.NoseTipHeight) return false;
            if (sbp1bs.NoseWidth != sbp2bs.NoseWidth) return false;
            if (sbp1bs.MouthBottomLipShape != sbp2bs.MouthBottomLipShape) return false;
            if (sbp1bs.MouthChinForward != sbp2bs.MouthChinForward) return false;
            if (sbp1bs.MouthChinLength != sbp2bs.MouthChinLength) return false;
            if (sbp1bs.MouthChinShape != sbp2bs.MouthChinShape) return false;
            if (sbp1bs.MouthForward != sbp2bs.MouthForward) return false;
            if (sbp1bs.MouthFrowSmile != sbp2bs.MouthFrowSmile) return false;
            if (sbp1bs.MouthJawHeight != sbp2bs.MouthJawHeight) return false;
            if (sbp1bs.MouthJawLine != sbp2bs.MouthJawLine) return false;
            if (sbp1bs.MouthJawShape != sbp2bs.MouthJawShape) return false;
            if (sbp1bs.MouthLipsConcaveConvex != sbp2bs.MouthLipsConcaveConvex) return false;
            if (sbp1bs.MouthLipThickness != sbp2bs.MouthLipThickness) return false;
            if (sbp1bs.MouthPosition != sbp2bs.MouthPosition) return false;
            if (sbp1bs.MouthTeethType != sbp2bs.MouthTeethType) return false;
            if (sbp1bs.MouthTopLipShape != sbp2bs.MouthTopLipShape) return false;
            if (sbp1bs.MouthWidth != sbp2bs.MouthWidth) return false;

            return true;
        }

        internal static void FixEquipment(Hero hero)
        {
            CharacterObject characterObject = null;
            if (hero.IsFemale)
            {
                if (hero.Mother != null)
                {
                    characterObject = hero.Mother.CharacterObject;
                }
            }
            else if (hero.Father != null)
            {
                characterObject = hero.Father.CharacterObject;
            }
            if (characterObject == null)
            {
                characterObject = hero.CharacterObject;
            }


            Equipment battleEquipment = characterObject.BattleEquipments.GetRandomElement<Equipment>();
            EquipmentHelper.AssignHeroEquipmentFromEquipment(hero, battleEquipment);

            Equipment civilianEquipment = characterObject.CivilianEquipments.GetRandomElement<Equipment>();
            EquipmentHelper.AssignHeroEquipmentFromEquipment(hero, civilianEquipment);
        }
    }
}
