﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Root;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Localization;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem;
using Kingmaker.UI.Common;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using static Kingmaker.UnitLogic.ActivatableAbilities.ActivatableAbilityResourceLogic;
using static Kingmaker.UnitLogic.Commands.Base.UnitCommand;

namespace CallOfTheWild
{
    partial class Shaman
    {
        internal class LifeSpirit
        {
            internal static BlueprintFeature spirit_ability;
            internal static BlueprintFeature greater_spirit_ability;
            internal static BlueprintFeature true_spirit_ability;
            internal static BlueprintFeature manifestation;
            internal static BlueprintFeature curse_of_suffereing;
            internal static BlueprintFeature enhanced_cures;
            internal static BlueprintFeature life_link;
            internal static BlueprintFeature life_sight;
            internal static BlueprintAbility[] spells;
            internal static BlueprintFeature[] hexes;

            internal static BlueprintFeature extra_channel;
            internal static BlueprintAbility heal_living;
            internal static BlueprintAbility harm_undead;


            internal static Spirit create()
            {
                createSpiritAbility();
                createGreaterSpiritAbility();
                createTrueSpiritAbility();
                createManifestation();

                spells = new BlueprintAbility[9]
                {
                    library.Get<BlueprintAbility>("f6f95242abdfac346befd6f4f6222140"), //remove sickness
                    library.Get<BlueprintAbility>("e84fc922ccf952943b5240293669b171"), //lesser restoration
                    SpellDuplicates.addDuplicateSpell("e7240516af4241b42b2cd819929ea9da", "ShamanLifeSpiritNeutralizePoison", ""), //neutralzie poison
                    library.Get<BlueprintAbility>("f2115ac1148256b4ba20788f7e966830"), //restoration
                    library.Get<BlueprintAbility>("d5847cad0b0e54c4d82d6c59a3cda6b0"), //breath of life
                    SpellDuplicates.addDuplicateSpell("5da172c4c89f9eb4cbb614f3a67357d3", "ShamanLifeSpiritHeal", ""), //heal
                    library.Get<BlueprintAbility>("fafd77c6bfa85c04ba31fdc1c962c914"), //greater restoration
                    SpellDuplicates.addDuplicateSpell("867524328b54f25488d371214eea0d90", "shamanLifeSpiritMassHeal", ""), //mass heal
                    SpellDuplicates.addDuplicateSpell("80a1a388ee938aa4e90d427ce9a7a3e9", "shamanLifeSpiritResurrection", ""), //resurrection
                };

                curse_of_suffereing = BattleSpirit.curse_of_suffering_hex;
                enhanced_cures = hex_engine.createEnchancedCures("ShamanEnhancedCures",
                                                                  "Enhanced Cures",
                                                                  "When the shaman casts a cure spell, the maximum number of hit points healed is based on her shaman level, not the limit imposed by the spell. For example an 11th-level shaman with this hex can cast cure light wounds to heal 1d8+11 hit points instead of the normal 1d8+5 maximum."
                                                                  );

                life_link = hex_engine.createLifeLink("ShamanLifeLink",
                                                  "Life Link",
                                                  "The shaman creates a bond between herself and another creature within 30 feet. Each round at the start of the shaman’s turn, if the bonded creature is wounded for 5 or more hit points below its maximum hit points it heals 5 hit points and the shaman takes 5 points of damage. The shaman can have one bond active per shaman level. The bond continues until the bonded creature dies, the shaman dies, the distance between her and the bonded creature exceeds 40 feet, or the shaman ends it as an immediate action. If the shaman has multiple bonds active, she can end as many as she wants with the same immediate action."
                                                  );

                life_sight = hex_engine.createLifeSight("ShamanLifeSight",
                                                            "Life Sight",
                                                            "Shaman is able to sense all nearby living creatures; this functions similar to blindsight, but only for living creatures within 30 feet of her. The shaman can use this ability a number of rounds per day equal to her shaman level, but these rounds do not need to consecutive."
                                                           );

                hexes = new BlueprintFeature[]
                {
                    curse_of_suffereing,
                    enhanced_cures,
                    life_sight,
                    life_link,
                };

                return new Spirit("Life",
                                  "Life",
                                  "A shaman who selects the life spirit appears more vibrant than most mortals. Her skin seems to glow, and her teeth are a pearly white. When she calls upon one of this spirit’s abilities, her eyes and hair shimmer in the light.",
                                  manifestation.Icon,
                                  "",
                                  spirit_ability,
                                  greater_spirit_ability,
                                  true_spirit_ability,
                                  manifestation,
                                  hexes,
                                  spells);
            }


            static void createSpiritAbility()
            {
                var resource = Helpers.CreateAbilityResource("ShamanChannelResource", "", "", "", null);
                resource.SetIncreasedByStat(1, StatType.Charisma);

                var positive_energy_feature = library.Get<BlueprintFeature>("a79013ff4bcd4864cb669622a29ddafb");
                var context_rank_config = Helpers.CreateContextRankConfig(baseValueType: ContextRankBaseValueType.ClassLevel, progression: ContextRankProgression.StartPlusDivStep,
                                                                                      type: AbilityRankType.Default, classes: getShamanArray(), startLevel: 1, stepLevel: 2);
                var dc_scaling = Common.createContextCalculateAbilityParamsBasedOnClasses(getShamanArray(), StatType.Charisma);
                spirit_ability = Helpers.CreateFeature("ShamanLifeSpiritChannelPositiveEnergyFeature",
                                                       "Channel Positive Energy",
                                                       "Shaman channels positive energy and can choose to deal damage to undead creatures or to heal living creatures.\nChanneling energy causes a burst that either heals all living creatures or damages all undead creatures in a 30-foot radius centered on the shaman. The amount of damage dealt or healed is equal to 1d6 points of damage plus 1d6 points of damage for every two shaman levels beyond 1st (2d6 at 3rd, 3d6 at 5th, and so on). Creatures that take damage from channeled energy receive a Will save to halve the damage. The DC of this save is equal to 10 + 1/2 the shaman's level + the shaman's Charisma modifier. Creatures healed by channel energy cannot exceed their maximum hit point total—all excess healing is lost. A shaman may channel energy a number of times per day equal to 1 + her Charisma modifier. This is a standard action that does not provoke an attack of opportunity. A shaman can choose whether or not to include herself in this effect.",
                                                       "", 
                                                       positive_energy_feature.Icon,
                                                       FeatureGroup.None);

                heal_living = ChannelEnergyEngine.createChannelEnergy(ChannelEnergyEngine.ChannelType.PositiveHeal,
                                                                          "ShamanLifeSpiritChannelEnergyHealLiving",
                                                                          "",
                                                                          "Channeling positive energy causes a burst that heals all living creatures in a 30 - foot radius centered on the shaman. The amount of damage healed is equal to 1d6 plus 1d6 for every two shaman levels beyond first.",
                                                                          "",
                                                                          context_rank_config,
                                                                          dc_scaling,
                                                                          Helpers.CreateResourceLogic(resource));
                harm_undead = ChannelEnergyEngine.createChannelEnergy(ChannelEnergyEngine.ChannelType.PositiveHarm,
                                                              "ShamanLifeSpiritChannelEnergyHarmUndead",
                                                              "",
                                                              "Channeling energy causes a burst that damages all undead creatures in a 30 - foot radius centered on the shaman. The amount of damage dealt is equal to 1d6 plus 1d6 for every two shaman levels beyond first. Creatures that take damage from channeled energy receive a Will save to halve the damage. The DC of this save is equal to 10 + 1 / 2 the shaman's level + the shaman's Charisma modifier.",
                                                              "",
                                                              context_rank_config,
                                                              dc_scaling,
                                                              Helpers.CreateResourceLogic(resource));

                var heal_living_base = Common.createVariantWrapper("ShamanLifeSpiritPositiveHealBase", "", heal_living);
                var harm_undead_base = Common.createVariantWrapper("ShamanLifeSpiritPositiveHarmBase", "", harm_undead);

                ChannelEnergyEngine.storeChannel(heal_living, spirit_ability, ChannelEnergyEngine.ChannelType.PositiveHeal);
                ChannelEnergyEngine.storeChannel(harm_undead, spirit_ability, ChannelEnergyEngine.ChannelType.PositiveHarm);

                spirit_ability.AddComponent(Helpers.CreateAddFacts(heal_living_base, harm_undead_base));
                spirit_ability.AddComponent(Helpers.CreateAddAbilityResource(resource));
                extra_channel = ChannelEnergyEngine.createExtraChannelFeat(heal_living, spirit_ability, "ExtraChannelShamanLifeSpirit", "Extra Channel (Shaman Life Spirit)", "");
            }


            static void createGreaterSpiritAbility()
            {
                var icon = library.Get<BlueprintAbility>("f6f95242abdfac346befd6f4f6222140").Icon;
                greater_spirit_ability = Helpers.CreateFeature("ShamanHealersTouchFeature",
                                                               "Healer's Touch",
                                                               "The shaman gains a +4 bonus to religion skill.",
                                                               "",
                                                               icon,
                                                               FeatureGroup.None,
                                                               Helpers.CreateAddStatBonus(StatType.SkillLoreReligion, 4, ModifierDescriptor.UntypedStackable)
                                                               );
            }


            static void createTrueSpiritAbility()
            {
                var healing_spells = new BlueprintAbility[]
                {
                    library.Get<BlueprintAbility>("5590652e1c2225c4ca30c4a699ab3649"),
                    library.Get<BlueprintAbility>("6b90c773a6543dc49b2505858ce33db5"),
                    library.Get<BlueprintAbility>("3361c5df793b4c8448756146a88026ad"),
                    library.Get<BlueprintAbility>("41c9016596fe1de4faf67425ed691203"),
                    library.Get<BlueprintAbility>("5d3d689392e4ff740a761ef346815074"),
                    library.Get<BlueprintAbility>("571221cc141bc21449ae96b3944652aa"),
                    library.Get<BlueprintAbility>("1f173a16120359e41a20fc75bb53d449"),
                    library.Get<BlueprintAbility>("0cea35de4d553cc439ae80b3a8724397")
                };

                ChannelEnergyEngine.createSwiftPositiveChannel();
                var icon = library.Get<BlueprintAbility>("867524328b54f25488d371214eea0d90").Icon; //mass heal
                var buff = Helpers.CreateBuff("ShamanQuickHealingBuff",
                                                "Quick Healing",
                                                "The shaman calls upon her spirit to enhance the speed of her healing abilities. This ability allows her to channel positive energy or cast a cure spell as a swift action. The shaman can use this ability a number of times per day equal to her Charisma modifier.",
                                                "",
                                                icon,
                                                null,
                                                Helpers.Create<NewMechanics.MetamagicMechanics.MetamagicOnSpellList>(a =>
                                                                                                                    {
                                                                                                                        a.resource = ChannelEnergyEngine.swift_positve_channel_resource;
                                                                                                                        a.amount = 1;
                                                                                                                        a.spell_list = healing_spells;
                                                                                                                        a.Metamagic = Kingmaker.UnitLogic.Abilities.Metamagic.Quicken;
                                                                                                                    }
                                                                                                                    )
                                              );

                var ability = Helpers.CreateActivatableAbility("ShamanQuickHealingActivatableAbility",
                                                               buff.Name,
                                                               buff.Description,
                                                               "",
                                                               buff.Icon,
                                                               buff,
                                                               AbilityActivationType.Immediately,
                                                               CommandType.Free,
                                                               null,
                                                               Helpers.CreateActivatableResourceLogic(ChannelEnergyEngine.swift_positve_channel_resource, ResourceSpendType.Never)
                                                               );
                ability.DeactivateImmediately = true;

                true_spirit_ability = Common.ActivatableAbilityToFeature(ability, hide: false);
                true_spirit_ability.AddComponent(Helpers.CreateAddAbilityResource(ChannelEnergyEngine.swift_positve_channel_resource));
                true_spirit_ability.AddComponent(Helpers.CreateAddFact(ChannelEnergyEngine.swift_positive_channel));
            }


            static void createManifestation()
            {
                var conditions = SpellDescriptor.Bleed | SpellDescriptor.Death | SpellDescriptor.Exhausted | SpellDescriptor.Fatigue | SpellDescriptor.Nauseated | SpellDescriptor.Sickened;

                manifestation = Helpers.CreateFeature("ShamanLifeManifestation",
                                                      "Manifestation",
                                                      "Upon reaching 20th level, the shaman becomes a perfect channel for life energy. She gains immunity to bleed, death attacks, and negative energy, as well as to the exhausted, fatigued, nauseated, and sickened conditions. Ability damage and drain cannot reduce her to below 1 in any ability score. She also receives diehard feat for free.",
                                                      "",
                                                      Helpers.GetIcon("be2062d6d85f4634ea4f26e9e858c3b8"), // cleanse
                                                      FeatureGroup.None,
                                                      Common.createBuffDescriptorImmunity(conditions),
                                                      Common.createSpellImmunityToSpellDescriptor(conditions),
                                                      UnitCondition.Exhausted.CreateImmunity(),
                                                      UnitCondition.Fatigued.CreateImmunity(),
                                                      UnitCondition.Nauseated.CreateImmunity(),
                                                      UnitCondition.Paralyzed.CreateImmunity(),
                                                      UnitCondition.Sickened.CreateImmunity(),
                                                      Helpers.Create<AddImmunityToEnergyDrain>(),
                                                      Helpers.Create<HealingMechanics.StatsCannotBeReducedBelow1>(),
                                                      Common.createAddFeatureIfHasFact(library.Get<BlueprintFeature>("c99f3405d1ef79049bd90678a666e1d7"), //diehard
                                                                                       library.Get<BlueprintFeature>("86669ce8759f9d7478565db69b8c19ad"),
                                                                                       not: true)
                                                      );
            }

        }
    }
}

