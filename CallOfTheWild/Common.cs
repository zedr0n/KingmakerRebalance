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
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Localization;
using Kingmaker.RuleSystem;
using Kingmaker.UI.Common;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using Kingmaker.Blueprints.Items;
using static Kingmaker.UnitLogic.ActivatableAbilities.ActivatableAbilityResourceLogic;
using static Kingmaker.UnitLogic.Commands.Base.UnitCommand;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.UI.Log;
using Kingmaker.Blueprints.Root.Strings.GameLog;
using Kingmaker;
using UnityEngine;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.ElementsSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.Designers.Mechanics.WeaponEnchants;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;
using Kingmaker.Designers.Mechanics.EquipmentEnchants;
using Kingmaker.ResourceLinks;

namespace CallOfTheWild
{

    public static partial class Extensions
    {
        public static bool checkSpellbook(this BlueprintSpellbook spellbook, bool is_divine, bool is_arcane, bool is_alchemist)
        {
            if (spellbook == null)
            {
                return false;
            }

            return (spellbook.IsAlchemist && is_alchemist) || (spellbook.IsArcane && is_arcane) || (!spellbook.IsArcane && !spellbook.IsAlchemist && is_divine);
        }

        public static T CloneObject<T>(this T obj) where T : class
        {
            if (obj == null) return null;
            System.Reflection.MethodInfo inst = obj.GetType().GetMethod("MemberwiseClone",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (inst != null)
                return (T)inst.Invoke(obj, null);
            else
                return null;
        }
    }



    public class Common
    {
        public static BlueprintFeature undead = library.Get<BlueprintFeature>("734a29b693e9ec346ba2951b27987e33");
        public static BlueprintFeature construct = library.Get<BlueprintFeature>("fd389783027d63343b4a5634bd81645f");
        public static BlueprintFeature elemental = library.Get<BlueprintFeature>("198fd8924dabcb5478d0f78bd453c586");

        static readonly Type ParametrizedFeatureData = Harmony12.AccessTools.Inner(typeof(AddParametrizedFeatures), "Data");
        static readonly Type ContextActionSavingThrow_ConditionalDCIncrease = Harmony12.AccessTools.Inner(typeof(ContextActionSavingThrow), "ConditionalDCIncrease");

        static public string[] roman_id = { "", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X" };


        static public BlueprintFeatureSelection EldritchKnightSpellbookSelection = Main.library.Get<BlueprintFeatureSelection>("dc3ab8d0484467a4787979d93114ebc3");
        static public BlueprintFeatureSelection ArcaneTricksterSelection = Main.library.Get<BlueprintFeatureSelection>("ae04b7cdeb88b024b9fd3882cc7d3d76");
        static public BlueprintFeatureSelection DragonDiscipleSpellbookSelection = Main.library.Get<BlueprintFeatureSelection>("8c1ba14c0b6dcdb439c56341385ee474");
        static public BlueprintFeatureSelection MysticTheurgeArcaneSpellbookSelection = Main.library.Get<BlueprintFeatureSelection>("97f510c6483523c49bc779e93e4c4568");
        static public BlueprintFeatureSelection MysticTheurgeDivineSpellbookSelection = Main.library.Get<BlueprintFeatureSelection>("7cd057944ce7896479717778330a4933");
        static LibraryScriptableObject library => Main.library;

        public enum DomainSpellsType
        {
            NoSpells = 1,
            SpecialList = 2,
            NormalList = 3
        }

        public struct SpellId
        {
            public readonly string guid;
            public readonly int level;
            public SpellId(string spell_guid, int spell_level)
            {
                guid = spell_guid;
                level = spell_level;
            }
        }

        public class ExtraSpellList
        {
            SpellId[] spells;

            public ExtraSpellList(params SpellId[] list_spells)
            {
                spells = list_spells;
            }


            public ExtraSpellList(params string[] list_spell_guids)
            {
                spells = new SpellId[list_spell_guids.Length];
                for (int i = 0; i < list_spell_guids.Length; i++)
                {
                    spells[i] = new SpellId(list_spell_guids[i], i + 1);
                }
            }


            public Kingmaker.Blueprints.Classes.Spells.BlueprintSpellList createSpellList(string name, string guid)
            {
                var spell_list = Helpers.Create<Kingmaker.Blueprints.Classes.Spells.BlueprintSpellList>();
                spell_list.name = name;
                library.AddAsset(spell_list, guid);
                spell_list.SpellsByLevel = new SpellLevelList[10];
                for (int i = 0; i < spell_list.SpellsByLevel.Length; i++)
                {
                    spell_list.SpellsByLevel[i] = new SpellLevelList(i);
                }
                foreach (var s in spells)
                {
                    if (!s.guid.Empty())
                    {
                        var spell = library.Get<BlueprintAbility>(s.guid);
                        spell.AddToSpellList(spell_list, s.level);
                    }
                }
                return spell_list;
            }


            public Kingmaker.UnitLogic.FactLogic.LearnSpellList createLearnSpellList(string name, string guid, BlueprintCharacterClass character_class, BlueprintArchetype archetype = null)
            {
                Kingmaker.UnitLogic.FactLogic.LearnSpellList learn_spell_list = Helpers.Create<Kingmaker.UnitLogic.FactLogic.LearnSpellList>();
                learn_spell_list.Archetype = archetype;
                learn_spell_list.CharacterClass = character_class;
                learn_spell_list.SpellList = createSpellList(name, guid);
                return learn_spell_list;
            }

        }


        public static BlueprintFeature createCantrips(string name, string display_name, string description, UnityEngine.Sprite icon, string guid, BlueprintCharacterClass character_class,
                                       StatType stat, BlueprintAbility[] spells)
        {
            var learn_spells = Helpers.Create<LearnSpells>();
            learn_spells.CharacterClass = character_class;
            learn_spells.Spells = spells;

            var bind_spells = Helpers.CreateBindToClass(character_class, stat, spells);
            bind_spells.LevelStep = 1;
            bind_spells.Cantrip = true;
            return Helpers.CreateFeature(name,
                                  display_name,
                                  description,
                                  guid,
                                  icon,
                                  FeatureGroup.None,
                                  Helpers.CreateAddFacts(spells),
                                  learn_spells,
                                  bind_spells
                                  );
        }

        public static Kingmaker.UnitLogic.Mechanics.Actions.ContextActionConditionalSaved createContextSavedApplyBuff(BlueprintBuff buff, DurationRate duration_rate,
                                                                                                                        AbilityRankType rank_type = AbilityRankType.Default,
                                                                                                                        bool is_from_spell = true, bool is_permanent = false, bool is_child = false,
                                                                                                                        bool on_failed_save = true, bool is_dispellable = true)
        {
            var context_saved = Helpers.Create<Kingmaker.UnitLogic.Mechanics.Actions.ContextActionConditionalSaved>();

            var apply_buff = Helpers.Create<Kingmaker.UnitLogic.Mechanics.Actions.ContextActionApplyBuff>();
            apply_buff.IsFromSpell = is_from_spell;
            apply_buff.AsChild = is_child;
            apply_buff.Permanent = is_permanent;
            apply_buff.Buff = buff;
            apply_buff.IsNotDispelable = !is_dispellable;
            var bonus_value = Helpers.CreateContextValue(rank_type);
            bonus_value.ValueType = ContextValueType.Rank;
            apply_buff.DurationValue = Helpers.CreateContextDuration(bonus: bonus_value,
                                                                           rate: duration_rate);
            if (on_failed_save)
            {
                context_saved.Succeed = new Kingmaker.ElementsSystem.ActionList();
                context_saved.Failed = Helpers.CreateActionList(apply_buff);
            }
            else
            {
                context_saved.Failed = new Kingmaker.ElementsSystem.ActionList();
                context_saved.Succeed = Helpers.CreateActionList(apply_buff);
            }
            return context_saved;
        }


        public static Kingmaker.UnitLogic.Mechanics.Actions.ContextActionConditionalSaved createContextSavedApplyBuff(BlueprintBuff buff, ContextDurationValue duration, bool is_from_spell = false,
                                                                                                                  bool is_child = false, bool is_permanent = false, bool is_dispellable = true)
        {
            var context_saved = Helpers.Create<Kingmaker.UnitLogic.Mechanics.Actions.ContextActionConditionalSaved>();
            context_saved.Succeed = new Kingmaker.ElementsSystem.ActionList();
            var apply_buff = Helpers.Create<Kingmaker.UnitLogic.Mechanics.Actions.ContextActionApplyBuff>();
            apply_buff.IsFromSpell = true;
            apply_buff.Buff = buff;
            apply_buff.DurationValue = duration;
            apply_buff.IsFromSpell = is_from_spell;
            apply_buff.AsChild = is_child;
            apply_buff.Permanent = is_permanent;
            apply_buff.IsNotDispelable = !is_dispellable;
            context_saved.Failed = Helpers.CreateActionList(apply_buff);
            return context_saved;
        }


        static public Kingmaker.UnitLogic.Mechanics.Components.DeathActions createDeathActions(Kingmaker.ElementsSystem.ActionList action_list,
                                                                                                 BlueprintAbilityResource resource = null)
        {
            var a = Helpers.Create<Kingmaker.UnitLogic.Mechanics.Components.DeathActions>();
            a.Actions = action_list;
            a.CheckResource = (resource != null);
            a.Resource = resource;
            return a;
        }


        static public Kingmaker.Designers.Mechanics.Facts.CriticalConfirmationACBonus createCriticalConfirmationACBonus(int bonus)
        {
            var c = Helpers.Create<CriticalConfirmationACBonus>();
            c.Bonus = bonus;
            return c;
        }


        static public CriticalConfirmationBonus createCriticalConfirmationBonus(int bonus)
        {
            var c = Helpers.Create<CriticalConfirmationBonus>();
            c.Bonus = bonus;
            return c;
        }


        public static ContextActionSpawnFx createContextActionSpawnFx(Kingmaker.ResourceLinks.PrefabLink prefab)
        {
            var c = Helpers.Create<ContextActionSpawnFx>();
            c.PrefabLink = prefab;
            return c;
        }


        public static ContextActionSavingThrow createContextActionSavingThrow(SavingThrowType saving_throw, Kingmaker.ElementsSystem.ActionList action)
        {
            var c = Helpers.Create<ContextActionSavingThrow>();
            c.Type = saving_throw;
            c.Actions = action;
            return c;
        }


        public static Kingmaker.UnitLogic.Mechanics.Components.ContextCalculateAbilityParamsBasedOnClass createContextCalculateAbilityParamsBasedOnClass(BlueprintCharacterClass character_class,
                                                                                                                                                    StatType stat, bool use_kineticist_main_stat = false)
        {
            var c = Helpers.Create<ContextCalculateAbilityParamsBasedOnClass>();
            c.CharacterClass = character_class;
            c.StatType = stat;
            c.UseKineticistMainStat = use_kineticist_main_stat;
            return c;
        }


        public static NewMechanics.ContextCalculateAbilityParamsBasedOnClasses createContextCalculateAbilityParamsBasedOnClasses(BlueprintCharacterClass[] character_classes,
                                                                                                                                            StatType stat)
        {
            var c = Helpers.Create<NewMechanics.ContextCalculateAbilityParamsBasedOnClasses>();
            c.CharacterClasses = character_classes;
            c.StatType = stat;
            return c;
        }


        public static Kingmaker.UnitLogic.FactLogic.AddSecondaryAttacks createAddSecondaryAttacks(params Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon[] weapons)
        {
            var c = Helpers.Create<Kingmaker.UnitLogic.FactLogic.AddSecondaryAttacks>();
            c.Weapon = weapons;
            return c;
        }


        public static Kingmaker.UnitLogic.Mechanics.Components.AddIncomingDamageTrigger createIncomingDamageTrigger(params Kingmaker.ElementsSystem.GameAction[] actions)
        {
            var c = Helpers.Create<Kingmaker.UnitLogic.Mechanics.Components.AddIncomingDamageTrigger>();
            c.Actions = Helpers.CreateActionList(actions);
            return c;
        }


        static public Kingmaker.UnitLogic.Mechanics.Actions.ContextActionApplyBuff createContextActionApplyBuff(BlueprintBuff buff, ContextDurationValue duration, bool is_from_spell = false,
                                                                                                                  bool is_child = false, bool is_permanent = false, bool dispellable = true,
                                                                                                                  int duration_seconds = 0)
        {
            var apply_buff = Helpers.Create<Kingmaker.UnitLogic.Mechanics.Actions.ContextActionApplyBuff>();
            apply_buff.IsFromSpell = is_from_spell;
            apply_buff.Buff = buff;
            apply_buff.Permanent = is_permanent;
            apply_buff.DurationValue = duration;
            apply_buff.IsNotDispelable = !dispellable;
            apply_buff.UseDurationSeconds = duration_seconds > 0;
            apply_buff.DurationSeconds = duration_seconds;
            apply_buff.AsChild = is_child;
            return apply_buff;
        }


        static public Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRandomize createContextActionRandomize(params Kingmaker.ElementsSystem.ActionList[] actions)
        {
            var c = Helpers.Create<Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRandomize>();
            Type m_Action_type = Helpers.GetField(c, "m_Actions").GetType().GetElementType();
            var y = Array.CreateInstance(m_Action_type, actions.Length);
            var field = m_Action_type.GetField("Action");
            for (int i = 0; i < actions.Length; i++)
            {
                var yi = m_Action_type.GetConstructor(new System.Type[0]).Invoke(new object[0]);
                field.SetValue(yi, actions[i]);
                y.SetValue(yi, i);
            }
            Helpers.SetField(c, "m_Actions", y);
            return c;
        }


        static public BuffDescriptorImmunity createBuffDescriptorImmunity(SpellDescriptor descriptor)
        {
            var b = Helpers.Create<BuffDescriptorImmunity>();
            b.Descriptor = descriptor;
            return b;
        }


        static public SpellImmunityToSpellDescriptor createSpellImmunityToSpellDescriptor(SpellDescriptor descriptor)
        {
            var b = Helpers.Create<SpellImmunityToSpellDescriptor>();
            b.Descriptor = descriptor;
            return b;
        }


        static public SpecificBuffImmunity createSpecificBuffImmunity(BlueprintBuff buff)
        {
            var b = Helpers.Create<SpecificBuffImmunity>();
            b.Buff = buff;
            return b;
        }


        static public NewMechanics.SpecificBuffImmunityExceptCaster createSpecificBuffImmunityExceptCaster(BlueprintBuff buff, bool except_caster = true)
        {
            var b = Helpers.Create<NewMechanics.SpecificBuffImmunityExceptCaster>();
            b.Buff = buff;
            b.except_caster = except_caster;
            return b;
        }

        static public Blindsense createBlindsense(int range)
        {
            var b = Helpers.Create<Blindsense>();
            b.Range = range.Feet();
            return b;
        }


        static public Blindsense createBlindsight(int range)
        {
            var b = Helpers.Create<Blindsense>();
            b.Range = range.Feet();
            b.Blindsight = true;
            return b;
        }


        static public Kingmaker.Designers.Mechanics.Facts.AddFortification createAddFortification(int bonus = 0, ContextValue value = null)
        {
            var a = Helpers.Create<AddFortification>();
            a.Bonus = bonus;
            a.UseContextValue = value == null ? false : true;
            a.Value = value;
            return a;
        }


        static public Kingmaker.Designers.Mechanics.Buffs.BuffStatusCondition createBuffStatusCondition(UnitCondition condition, SavingThrowType save_type = SavingThrowType.Unknown,
                                                                                                           bool save_each_round = true)
        {
            var c = Helpers.Create<Kingmaker.Designers.Mechanics.Buffs.BuffStatusCondition>();
            c.SaveType = save_type;
            c.SaveEachRound = save_each_round;
            c.Condition = condition;
            return c;
        }

        static public Kingmaker.UnitLogic.Buffs.Conditions.BuffConditionCheckRoundNumber createBuffConditionCheckRoundNumber(int round_number, bool not = false)
        {
            var c = Helpers.Create<Kingmaker.UnitLogic.Buffs.Conditions.BuffConditionCheckRoundNumber>();
            c.RoundNumber = round_number;
            c.Not = not;
            return c;
        }


        static public ContextValue createSimpleContextValue(int value)
        {
            var v = new ContextValue();
            v.Value = value;
            v.ValueType = ContextValueType.Simple;
            return v;
        }


        static public Kingmaker.UnitLogic.FactLogic.SpontaneousSpellConversion createSpontaneousSpellConversion(BlueprintCharacterClass character_class, params BlueprintAbility[] spells)
        {
            var sc = Helpers.Create<Kingmaker.UnitLogic.FactLogic.SpontaneousSpellConversion>();
            sc.CharacterClass = character_class;
            sc.SpellsByLevel = spells;
            return sc;
        }


        static public Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteAlignment createPrerequisiteAlignment(Kingmaker.UnitLogic.Alignments.AlignmentMaskType alignment)
        {
            var p = Helpers.Create<Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteAlignment>();
            p.Alignment = alignment;
            return p;
        }


        static public Kingmaker.Designers.Mechanics.Facts.AddCasterLevelForAbility createAddCasterLevelToAbility(BlueprintAbility spell, int bonus)
        {
            var a = Helpers.Create<Kingmaker.Designers.Mechanics.Facts.AddCasterLevelForAbility>();
            a.Bonus = bonus;
            a.Spell = spell;
            return a;
        }

        static public PrerequisiteArchetypeLevel createPrerequisiteArchetypeLevel(BlueprintCharacterClass character_class, BlueprintArchetype archetype, int level, bool any = false)
        {
            var p = Helpers.Create<PrerequisiteArchetypeLevel>();
            p.CharacterClass = character_class;
            p.Archetype = archetype;
            p.Level = level;
            p.Group = any ? Prerequisite.GroupType.Any : Prerequisite.GroupType.All;
            return p;
        }


        static public Kingmaker.Designers.Mechanics.Facts.ArcaneArmorProficiency createArcaneArmorProficiency(params Kingmaker.Blueprints.Items.Armors.ArmorProficiencyGroup[] armor)
        {
            var p = Helpers.Create<Kingmaker.Designers.Mechanics.Facts.ArcaneArmorProficiency>();
            p.Armor = armor;
            return p;
        }


        static public Kingmaker.Blueprints.Classes.Spells.SpellsLevelEntry createSpellsLevelEntry(params int[] count)
        {
            var s = new Kingmaker.Blueprints.Classes.Spells.SpellsLevelEntry();
            s.Count = count;
            return s;
        }

        static public Kingmaker.Blueprints.Classes.Spells.BlueprintSpellsTable createSpellsTable(string name, string guid, params Kingmaker.Blueprints.Classes.Spells.SpellsLevelEntry[] levels)
        {
            var t = Helpers.Create<Kingmaker.Blueprints.Classes.Spells.BlueprintSpellsTable>();
            t.name = name;
            library.AddAsset(t, guid);
            t.Levels = levels;
            return t;
        }


        static public Kingmaker.Blueprints.Classes.Spells.BlueprintSpellsTable createSpontaneousHalfCasterSpellsPerDay(string name, string guid)
        {
            return createSpellsTable(name, guid,
                                       Common.createSpellsLevelEntry(),  //0
                                       Common.createSpellsLevelEntry(),  //1
                                       Common.createSpellsLevelEntry(),  //2
                                       Common.createSpellsLevelEntry(),  //3
                                       Common.createSpellsLevelEntry(0, 1), //4
                                       Common.createSpellsLevelEntry(0, 1), //5
                                       Common.createSpellsLevelEntry(0, 1), //6
                                       Common.createSpellsLevelEntry(0, 1, 1), //7
                                       Common.createSpellsLevelEntry(0, 1, 1), //8
                                       Common.createSpellsLevelEntry(0, 2, 1), //9
                                       Common.createSpellsLevelEntry(0, 2, 1, 1), //10
                                       Common.createSpellsLevelEntry(0, 2, 1, 1), //11
                                       Common.createSpellsLevelEntry(0, 2, 2, 1), //12
                                       Common.createSpellsLevelEntry(0, 3, 2, 1, 1), //13
                                       Common.createSpellsLevelEntry(0, 3, 2, 1, 1), //14
                                       Common.createSpellsLevelEntry(0, 3, 2, 2, 1), //15
                                       Common.createSpellsLevelEntry(0, 3, 3, 2, 1), //16
                                       Common.createSpellsLevelEntry(0, 4, 3, 2, 1), //17
                                       Common.createSpellsLevelEntry(0, 4, 4, 2, 2), //18
                                       Common.createSpellsLevelEntry(0, 4, 3, 3, 2), //19
                                       Common.createSpellsLevelEntry(0, 4, 4, 3, 2) //20
                                       );
        }


        static public Kingmaker.Blueprints.Classes.Spells.BlueprintSpellsTable createEmptySpellTable(string name, string guid)
        {
            return createSpellsTable(name, guid, Enumerable.Repeat(Common.createSpellsLevelEntry(), 21).ToArray());
        }

        static public Kingmaker.Blueprints.Classes.Spells.BlueprintSpellsTable createOneSpellSpellTable(string name, string guid)
        {
            var spell_array = new int[] { 0, 1, 1, 1, 1, 1, 1, 1, 1, 1 };

            var table_array = new SpellsLevelEntry[21];

            for (int i = 0; i < 21; i++)
            {
                table_array[i] = Common.createSpellsLevelEntry(spell_array.Take(Math.Min((i + 3) / 2, spell_array.Length)).ToArray());
            }

            return createSpellsTable(name, guid, table_array);
        }


        static public Kingmaker.Blueprints.Classes.Spells.BlueprintSpellsTable createSpontaneousHalfCasterSpellsKnown(string name, string guid)
        {
            return createSpellsTable(name, guid,
                                       Common.createSpellsLevelEntry(),  //0
                                       Common.createSpellsLevelEntry(),  //1
                                       Common.createSpellsLevelEntry(),  //2
                                       Common.createSpellsLevelEntry(),  //2
                                       Common.createSpellsLevelEntry(0, 2), //4
                                       Common.createSpellsLevelEntry(0, 3), //5
                                       Common.createSpellsLevelEntry(0, 4), //6
                                       Common.createSpellsLevelEntry(0, 4, 2), //7
                                       Common.createSpellsLevelEntry(0, 4, 3), //8
                                       Common.createSpellsLevelEntry(0, 5, 4), //9
                                       Common.createSpellsLevelEntry(0, 5, 4, 2), //10
                                       Common.createSpellsLevelEntry(0, 5, 4, 3), //11
                                       Common.createSpellsLevelEntry(0, 6, 5, 4), //12
                                       Common.createSpellsLevelEntry(0, 6, 5, 4, 2), //13
                                       Common.createSpellsLevelEntry(0, 6, 5, 4, 3), //14
                                       Common.createSpellsLevelEntry(0, 6, 6, 5, 4), //15
                                       Common.createSpellsLevelEntry(0, 6, 6, 5, 4), //16
                                       Common.createSpellsLevelEntry(0, 6, 6, 5, 4), //17
                                       Common.createSpellsLevelEntry(0, 6, 6, 6, 5), //18
                                       Common.createSpellsLevelEntry(0, 6, 6, 6, 5), //19
                                       Common.createSpellsLevelEntry(0, 6, 6, 6, 5) //20
                                       );
        }


        static public Kingmaker.Designers.Mechanics.Buffs.EmptyHandWeaponOverride createEmptyHandWeaponOverride(BlueprintItemWeapon weapon)
        {
            var c = Helpers.Create<Kingmaker.Designers.Mechanics.Buffs.EmptyHandWeaponOverride>();
            c.Weapon = weapon;
            return c;
        }


        static public RemoveFeatureOnApply createRemoveFeatureOnApply(BlueprintFeature feature)
        {
            var c = Helpers.Create<RemoveFeatureOnApply>();
            c.Feature = feature;
            return c;
        }


        static public void addContextActionApplyBuffOnFactsToActivatedAbilityBuff(BlueprintBuff target_buff, BlueprintBuff buff_to_add, Kingmaker.ElementsSystem.GameAction[] pre_actions,
                                                                                      params BlueprintUnitFact[] facts)
        {
            /*if (target_buff.GetComponent<AddFactContextActions>() == null)
            {
                target_buff.AddComponent(Helpers.CreateEmptyAddFactContextActions());
            }*/
            var condition = new Kingmaker.UnitLogic.Mechanics.Conditions.ContextConditionHasFact[facts.Length];
            for (int i = 0; i < facts.Length; i++)
            {
                condition[i] = Helpers.CreateConditionHasFact(facts[i]);
            }
            var action = Helpers.CreateConditional(condition, pre_actions.AddToArray(Common.createContextActionApplyBuff(buff_to_add, Helpers.CreateContextDuration(),
                                                                                     dispellable: false, is_child: true, is_permanent: true)));
            addContextActionApplyBuffOnConditionToActivatedAbilityBuff(target_buff, buff_to_add, action);
        }

        static public void addContextActionApplyBuffOnConditionToActivatedAbilityBuff(BlueprintBuff target_buff, BlueprintBuff buff_to_add, Conditional conditional_action)
        {
            if (target_buff.GetComponent<AddFactContextActions>() == null)
            {
                target_buff.AddComponent(Helpers.CreateEmptyAddFactContextActions());
            }

            var activated = target_buff.GetComponent<Kingmaker.UnitLogic.Mechanics.Components.AddFactContextActions>().Activated;
            activated.Actions = activated.Actions.AddToArray(conditional_action);
            var deactivated = target_buff.GetComponent<Kingmaker.UnitLogic.Mechanics.Components.AddFactContextActions>().Deactivated;
            var remove_buff = Helpers.Create<Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuff>();
            remove_buff.Buff = buff_to_add;
            deactivated.Actions = deactivated.Actions.AddToArray(remove_buff);
        }



        static public void addContextActionApplyBuffOnFactsToActivatedAbilityBuffNoRemove(BlueprintBuff target_buff, BlueprintBuff buff_to_add, Kingmaker.ElementsSystem.GameAction[] pre_actions,
                                                                              params BlueprintUnitFact[] facts)
        {
            /*if (target_buff.GetComponent<AddFactContextActions>() == null)
            {
                target_buff.AddComponent(Helpers.CreateEmptyAddFactContextActions());
            }*/
            var condition = new Kingmaker.UnitLogic.Mechanics.Conditions.ContextConditionHasFact[facts.Length];
            for (int i = 0; i < facts.Length; i++)
            {
                condition[i] = Helpers.CreateConditionHasFact(facts[i]);
            }
            var action = Helpers.CreateConditional(condition, pre_actions.AddToArray(Common.createContextActionApplyBuff(buff_to_add, Helpers.CreateContextDuration(),
                                                                                     dispellable: false, is_child: true, is_permanent: true)));
            addContextActionApplyBuffOnConditionToActivatedAbilityBuff(target_buff, action);
        }


        static public void addContextActionApplyBuffOnConditionToActivatedAbilityBuff(BlueprintBuff target_buff, Conditional conditional_action)
        {
            if (target_buff.GetComponent<AddFactContextActions>() == null)
            {
                var context_actions = new BlueprintComponent[] { Helpers.CreateEmptyAddFactContextActions() };
                target_buff.ComponentsArray = context_actions.AddToArray(target_buff.ComponentsArray);
            }

            var activated = target_buff.GetComponent<Kingmaker.UnitLogic.Mechanics.Components.AddFactContextActions>().Activated;
            activated.Actions = activated.Actions.AddToArray(conditional_action);
        }


        static public NewMechanics.WeaponTypeSizeChange createWeaponTypeSizeChange(int size_change, params BlueprintWeaponType[] types)
        {
            var w = Helpers.Create<NewMechanics.WeaponTypeSizeChange>();
            w.SizeCategoryChange = size_change;
            w.WeaponTypes = types;
            return w;
        }


        static public void addContextActionApplyBuffOnFactsToActivatedAbilityBuff(BlueprintBuff target_buff, BlueprintBuff buff_to_add, params BlueprintUnitFact[] facts)
        {
            addContextActionApplyBuffOnFactsToActivatedAbilityBuff(target_buff, buff_to_add, new Kingmaker.ElementsSystem.GameAction[0], facts);
        }


        static public void addContextActionApplyBuffOnFactsToActivatedAbilityBuffNoRemove(BlueprintBuff target_buff, BlueprintBuff buff_to_add, params BlueprintUnitFact[] facts)
        {
            addContextActionApplyBuffOnFactsToActivatedAbilityBuffNoRemove(target_buff, buff_to_add, new Kingmaker.ElementsSystem.GameAction[0], facts);
        }


        static public Kingmaker.UnitLogic.Buffs.Components.AddAreaEffect createAddAreaEffect(BlueprintAbilityAreaEffect area_effect)
        {
            var a = Helpers.Create<Kingmaker.UnitLogic.Buffs.Components.AddAreaEffect>();
            a.AreaEffect = area_effect;
            return a;
        }


        static public AddInitiatorAttackWithWeaponTrigger createAddInitiatorAttackWithWeaponTrigger(Kingmaker.ElementsSystem.ActionList action, bool only_hit = true, bool critical_hit = false,
                                                                                                      bool check_weapon_range_type = false, bool reduce_hp_to_zero = false,
                                                                                                      bool on_initiator = false,
                                                                                                      AttackTypeAttackBonus.WeaponRangeType range_type = AttackTypeAttackBonus.WeaponRangeType.Melee,
                                                                                                      bool wait_for_attack_to_resolve = false, bool only_first_hit = false)
        {
            var t = Helpers.Create<AddInitiatorAttackWithWeaponTrigger>();
            t.Action = action;
            t.OnlyHit = only_hit;
            t.CriticalHit = critical_hit;
            t.CheckWeaponRangeType = check_weapon_range_type;
            t.RangeType = range_type;
            t.ReduceHPToZero = reduce_hp_to_zero;
            t.ActionsOnInitiator = on_initiator;
            t.WaitForAttackResolve = wait_for_attack_to_resolve;
            t.OnlyOnFirstAttack = only_first_hit;
            return t;
        }


        static public AddInitiatorAttackWithWeaponTrigger createAddInitiatorAttackWithWeaponTriggerWithCategory(Kingmaker.ElementsSystem.ActionList action, bool only_hit = true, bool critical_hit = false,
                                                                                              bool check_weapon_range_type = false, bool reduce_hp_to_zero = false,
                                                                                              bool on_initiator = false,
                                                                                              AttackTypeAttackBonus.WeaponRangeType range_type = AttackTypeAttackBonus.WeaponRangeType.Melee,
                                                                                              bool wait_for_attack_to_resolve = false, bool only_first_hit = false,
                                                                                              WeaponCategory weapon_category = WeaponCategory.UnarmedStrike)
        {
            var t = Helpers.Create<AddInitiatorAttackWithWeaponTrigger>();
            t.Action = action;
            t.OnlyHit = only_hit;
            t.CriticalHit = critical_hit;
            t.CheckWeaponRangeType = check_weapon_range_type;
            t.RangeType = range_type;
            t.ReduceHPToZero = reduce_hp_to_zero;
            t.ActionsOnInitiator = on_initiator;
            t.WaitForAttackResolve = wait_for_attack_to_resolve;
            t.OnlyOnFirstAttack = only_first_hit;
            t.CheckWeaponCategory = true;
            t.Category = weapon_category;
            return t;
        }


        static public Kingmaker.UnitLogic.FactLogic.AddOutgoingPhysicalDamageProperty createAddOutgoingAlignment(DamageAlignment alignment, bool check_range = false, bool is_ranged = false)
        {
            var a = Helpers.Create<AddOutgoingPhysicalDamageProperty>();
            a.AddAlignment = true;
            a.Alignment = alignment;
            a.CheckRange = check_range;
            a.IsRanged = is_ranged;
            return a;
        }


        static public Kingmaker.UnitLogic.FactLogic.AddOutgoingPhysicalDamageProperty createAddOutgoingMaterial(PhysicalDamageMaterial matrerial, bool check_range = false, bool is_ranged = false)
        {
            var a = Helpers.Create<AddOutgoingPhysicalDamageProperty>();
            a.AddMaterial = true;
            a.Material = matrerial;
            a.CheckRange = check_range;
            a.IsRanged = is_ranged;
            return a;
        }


        static public Kingmaker.UnitLogic.FactLogic.AddOutgoingPhysicalDamageProperty createAddOutgoingMagic(bool check_range = false, bool is_ranged = false)
        {
            var a = Helpers.Create<AddOutgoingPhysicalDamageProperty>();
            a.AddMagic = true;
            a.CheckRange = check_range;
            a.IsRanged = is_ranged;
            return a;
        }


        static public Kingmaker.UnitLogic.FactLogic.AddOutgoingPhysicalDamageProperty createAddOutgoingGhost(bool check_range = false, bool is_ranged = false)
        {
            var a = Helpers.Create<AddOutgoingPhysicalDamageProperty>();
            a.CheckRange = check_range;
            a.IsRanged = is_ranged;
            a.AddReality = true;
            a.Reality = DamageRealityType.Ghost;
            return a;
        }



        static public NewMechanics.ContextWeaponTypeDamageBonus createContextWeaponTypeDamageBonus(ContextValue bonus, params BlueprintWeaponType[] weapon_types)
        {
            var c = Helpers.Create<NewMechanics.ContextWeaponTypeDamageBonus>();
            c.Value = bonus;
            c.weapon_types = weapon_types;
            return c;
        }

        public static BlueprintFeatureSelection copyRenameSelection(string original_selection_guid, string name_prefix, string description, string selection_guid)
        {
            var old_selection = library.Get<BlueprintFeatureSelection>(original_selection_guid);
            var new_selection = library.CopyAndAdd<BlueprintFeatureSelection>(original_selection_guid, name_prefix + old_selection, selection_guid);

            new_selection.SetDescription(description);

            BlueprintFeature[] new_features = new BlueprintFeature[old_selection.AllFeatures.Length];

            var old_features = old_selection.AllFeatures;

            for (int i = 0; i < old_features.Length; i++)
            {
                new_features[i] = library.CopyAndAdd<BlueprintFeature>(old_features[i].AssetGuid, name_prefix + old_features[i].name, "");
                new_features[i].SetDescription(description);
            }
            new_selection.AllFeatures = new_features;
            return new_selection;
        }





        public static BlueprintFeature createSmite(string name, string display_name, string description, string guid, string ability_guid, UnityEngine.Sprite icon,
                                                     BlueprintCharacterClass[] classes, AlignmentComponent smite_alignment)
        {
            var new_context_rank_config = Helpers.CreateContextRankConfig(baseValueType: ContextRankBaseValueType.ClassLevel, classes: classes);

            return createSmite(name, display_name, description, guid, ability_guid, icon, new_context_rank_config, smite_alignment);
        }


        public static BlueprintFeature createSmite(string name, string display_name, string description, string guid, string ability_guid, UnityEngine.Sprite icon,
                                             ContextRankConfig new_context_rank_config, AlignmentComponent smite_alignment)
        {
            var smite_ability = library.CopyAndAdd<BlueprintAbility>("7bb9eb2042e67bf489ccd1374423cdec", name + "Ability", ability_guid);
            var smite_feature = library.CopyAndAdd<BlueprintFeature>("3a6db57fce75b0244a6a5819528ddf26", name + "Feature", guid);


            smite_feature.SetName(display_name);
            smite_feature.SetDescription(description);
            smite_feature.SetIcon(icon);

            smite_feature.ReplaceComponent<Kingmaker.UnitLogic.FactLogic.AddFacts>(Helpers.CreateAddFact(smite_ability));
            


            smite_ability.SetName(smite_feature.Name);
            smite_ability.SetDescription(smite_feature.Description);
            smite_ability.SetIcon(icon);
            smite_ability.RemoveComponent(smite_ability.GetComponent<Kingmaker.UnitLogic.Abilities.Components.CasterCheckers.AbilityCasterAlignment>());
            var old_context_rank_config = smite_ability.GetComponents<Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig>().Where(a => a.Type == AbilityRankType.DamageBonus).ElementAt(0);
            Helpers.SetField(new_context_rank_config, "m_Type", AbilityRankType.DamageBonus);
            smite_ability.ReplaceComponent(old_context_rank_config, new_context_rank_config);

            var smite_action = smite_ability.GetComponent<Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction>();
                   
            var old_conditional = (Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional)smite_action.Actions.Actions[0];
            var conditions = new Kingmaker.ElementsSystem.Condition[] { Helpers.CreateContextConditionAlignment(smite_alignment, false, false), old_conditional.ConditionsChecker.Conditions[1] };

            var smite_buff = ((ContextActionApplyBuff)old_conditional.IfTrue.Actions[0]).Buff;
            //make buff take icon and name from parent ability
            smite_buff.SetIcon(null);
            smite_buff.SetName("");
            var new_smite_action =  Helpers.CreateConditional(conditions, old_conditional.IfTrue.Actions, old_conditional.IfFalse.Actions);
            smite_ability.ReplaceComponent(smite_action, Helpers.CreateRunActions(new_smite_action));
            smite_feature.GetComponent<AddAbilityResources>().RestoreAmount = true;
            return smite_feature;
        }


        public static void addConditionToResoundingBlow(ContextCondition new_condtion)
        {
            //add it to resounding blow
            var resounding_blow_buff = library.Get<BlueprintBuff>("06173a778d7067a439acffe9004916e9");
            foreach (var attack_trigger in resounding_blow_buff.GetComponents<AddInitiatorAttackWithWeaponTrigger>())
            {
                var cnd = (attack_trigger.Action.Actions[0] as Conditional);
                cnd.ConditionsChecker.Conditions = cnd.ConditionsChecker.Conditions.AddToArray(new_condtion);

            }
            
        }


        public static PrerequisiteNoArchetype prerequisiteNoArchetype(BlueprintCharacterClass character_class, BlueprintArchetype archetype, bool any = false)
        {
            var p = Helpers.Create<PrerequisiteNoArchetype>();
            p.Archetype = archetype;
            p.CharacterClass = character_class;
            p.Group = any ? Prerequisite.GroupType.Any : Prerequisite.GroupType.All;
            return p;
        }

        public static BlueprintFeature createSpellResistance(string name, string display_name, string description, string guid, BlueprintCharacterClass character_class, int start_value)
        {
            var spell_resistance = library.CopyAndAdd<BlueprintFeature>("01182bcee8cb41640b7fa1b1ad772421", //monk diamond soul
                                                                        name,
                                                                        guid);
            spell_resistance.SetName(display_name);
            spell_resistance.SetDescription(description);
            spell_resistance.Groups = new FeatureGroup[0];
            spell_resistance.RemoveComponent(spell_resistance.GetComponent<Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteClassLevel>());
            var context_rank_config = spell_resistance.GetComponent<Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig>().CreateCopy();
            Helpers.SetField(context_rank_config, "m_StepLevel", start_value);
            Helpers.SetField(context_rank_config, "m_Class", new BlueprintCharacterClass[] { character_class });
            spell_resistance.ReplaceComponent<ContextRankConfig>(context_rank_config);
            return spell_resistance;
        }


        public static SpellResistanceAgainstSpellDescriptor createSpellResistanceAgainstSpellDescriptor(ContextValue value, SpellDescriptor descriptor)
        {
            var sr = Helpers.Create<SpellResistanceAgainstSpellDescriptor>();
            sr.SpellDescriptor = descriptor;
            sr.Value = value;
            return sr;
        }


        public static Kingmaker.UnitLogic.FactLogic.AddDamageResistancePhysical createAlignmentDR(int dr_value, DamageAlignment alignment)
        {
            var feat = Helpers.Create<Kingmaker.UnitLogic.FactLogic.AddDamageResistancePhysical>();
            feat.Alignment = alignment;
            feat.BypassedByAlignment = true;
            feat.Value.ValueType = ContextValueType.Simple;
            feat.Value.Value = dr_value;

            return feat;
        }


        public static Kingmaker.UnitLogic.FactLogic.AddDamageResistancePhysical createMatrerialDR(int dr_value, PhysicalDamageMaterial material)
        {
            var feat = Helpers.Create<Kingmaker.UnitLogic.FactLogic.AddDamageResistancePhysical>();
            feat.Material = material;
            feat.BypassedByMaterial = true;
            feat.Value.ValueType = ContextValueType.Simple;
            feat.Value.Value = dr_value;

            return feat;
        }



        public static Kingmaker.UnitLogic.FactLogic.AddDamageResistancePhysical createPhysicalDR(int dr_value)
        {
            var feat = Helpers.Create<Kingmaker.UnitLogic.FactLogic.AddDamageResistancePhysical>();
            feat.BypassedByMaterial = false;
            feat.Value.ValueType = ContextValueType.Simple;
            feat.Value.Value = dr_value;
            return feat;
        }


        public static Kingmaker.UnitLogic.FactLogic.AddDamageResistancePhysical createContextPhysicalDR(ContextValue value)
        {
            var feat = Helpers.Create<Kingmaker.UnitLogic.FactLogic.AddDamageResistancePhysical>();
            feat.BypassedByMaterial = false;
            feat.Value = value;
            return feat;
        }


        public static Kingmaker.UnitLogic.FactLogic.AddDamageResistancePhysical createContextFormDR(ContextValue value, PhysicalDamageForm form)
        {
            var feat = Helpers.Create<Kingmaker.UnitLogic.FactLogic.AddDamageResistancePhysical>();
            feat.BypassedByForm = true;
            feat.Form = form;
            feat.Value = value;
            return feat;
        }


        public static Kingmaker.UnitLogic.FactLogic.AddDamageResistancePhysical createMaterialDR(ContextValue value, PhysicalDamageMaterial material)
        {
            var feat = Helpers.Create<Kingmaker.UnitLogic.FactLogic.AddDamageResistancePhysical>();
            feat.Material = material;
            feat.BypassedByMaterial = true;
            feat.Value = value;

            return feat;
        }


        public static Kingmaker.UnitLogic.FactLogic.AddDamageResistancePhysical createMagicDR(ContextValue dr_value)
        {
            var feat = Helpers.Create<Kingmaker.UnitLogic.FactLogic.AddDamageResistancePhysical>();
            feat.BypassedByMagic = true;
            feat.Value = dr_value;

            return feat;
        }


        public static AddCondition createAddCondition(UnitCondition condition)
        {
            var a = Helpers.Create<AddCondition>();
            a.Condition = condition;
            return a;
        }


        public static Kingmaker.UnitLogic.FactLogic.AddDamageResistancePhysical createAlignmentDRContextRank(DamageAlignment alignment, AbilityRankType rank = AbilityRankType.StatBonus)
        {
            var feat = Helpers.Create<Kingmaker.UnitLogic.FactLogic.AddDamageResistancePhysical>();
            feat.Alignment = alignment;
            feat.BypassedByAlignment = true;
            feat.Value = Helpers.CreateContextValueRank(rank);
            return feat;
        }


        public static Kingmaker.UnitLogic.FactLogic.AddDamageResistanceEnergy createEnergyDR(int dr_value, DamageEnergyType energy)
        {
            var feat = Helpers.Create<Kingmaker.UnitLogic.FactLogic.AddDamageResistanceEnergy>();
            feat.Type = energy;
            feat.Value.ValueType = ContextValueType.Simple;
            feat.Value.Value = dr_value;

            return feat;
        }


        public static Kingmaker.UnitLogic.FactLogic.AddDamageResistanceEnergy createEnergyDRContextRank(DamageEnergyType energy, AbilityRankType rank = AbilityRankType.StatBonus, int multiplier = 1)
        {
            var feat = Helpers.Create<Kingmaker.UnitLogic.FactLogic.AddDamageResistanceEnergy>();
            feat.Type = energy;
            feat.Value = Helpers.CreateContextValueRank(rank);
            feat.UseValueMultiplier = multiplier != 1;
            feat.ValueMultiplier = Common.createSimpleContextValue(multiplier);
            return feat;
        }


        public static void addClassToDomains(BlueprintCharacterClass class_to_add, BlueprintArchetype[] archetypes_to_add, DomainSpellsType spells_type, BlueprintFeatureSelection domain_selection)
        {
            var domains = domain_selection.AllFeatures;
            foreach (var domain_feature in domains)
            {

                BlueprintProgression domain = (BlueprintProgression)domain_feature;
                domain.Classes = domain.Classes.AddToArray(class_to_add);
                domain.Archetypes = domain.Archetypes.AddToArray(archetypes_to_add);

                foreach (var entry in domain.LevelEntries)
                {
                    foreach (var feat in entry.Features)
                    {
                        addClassToFeat(class_to_add, archetypes_to_add, spells_type, feat);
                    }
                }

                var spell_list = domain.GetComponent<Kingmaker.UnitLogic.FactLogic.LearnSpellList>()?.SpellList;
                if (spells_type == DomainSpellsType.NormalList && spell_list != null)
                {
                    if (archetypes_to_add.Empty())
                    {
                        var learn_spells_fact = Helpers.Create<Kingmaker.UnitLogic.FactLogic.LearnSpellList>();
                        learn_spells_fact.SpellList = spell_list;
                        learn_spells_fact.CharacterClass = class_to_add;
                        domain.AddComponent(learn_spells_fact);

                    }
                    else
                    {
                        foreach (var ar_type in archetypes_to_add)
                        {
                            var learn_spells_fact = Helpers.Create<Kingmaker.UnitLogic.FactLogic.LearnSpellList>();
                            learn_spells_fact.SpellList = spell_list;
                            learn_spells_fact.CharacterClass = class_to_add;
                            learn_spells_fact.Archetype = ar_type;
                            domain.AddComponent(learn_spells_fact);
                        }
                    }
                }
            }
        }


        static void addClassToContextRankConfig(BlueprintCharacterClass class_to_add, ContextRankConfig c)
        {
            BlueprintCharacterClass cleric_class = library.Get<BlueprintCharacterClass>("67819271767a9dd4fbfd4ae700befea0");
            var classes = Helpers.GetField<BlueprintCharacterClass[]>(c, "m_Class");
            if (classes.Contains(cleric_class))
            {
                classes = classes.AddToArray(class_to_add);
                Helpers.SetField(c, "m_Class", classes);
            }
        }


        static void addClassToAreaEffect(BlueprintCharacterClass class_to_add, Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbilityAreaEffect a)
        {
            var components = a.ComponentsArray;
            foreach (var c in components)
            {
                if (c is ContextRankConfig)
                {
                    var c_typed = (Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig)c;
                    addClassToContextRankConfig(class_to_add, c_typed);
                }
                else if (c is Kingmaker.UnitLogic.Abilities.Components.AreaEffects.AbilityAreaEffectBuff)
                {
                    var c_typed = (Kingmaker.UnitLogic.Abilities.Components.AreaEffects.AbilityAreaEffectBuff)c;
                    addClassToBuff(class_to_add, c_typed.Buff);
                }
            }
        }


        static void addClassToBuff(BlueprintCharacterClass class_to_add, BlueprintBuff b)
        {
            var components = b.ComponentsArray;
            foreach (var c in components)
            {
                if (c is ContextRankConfig)
                {
                    var c_typed = (Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig)c;
                    addClassToContextRankConfig(class_to_add, c_typed);
                }
                else if (c is Kingmaker.UnitLogic.Buffs.Components.AddAreaEffect)
                {
                    var c_typed = (Kingmaker.UnitLogic.Buffs.Components.AddAreaEffect)c;
                    addClassToAreaEffect(class_to_add, c_typed.AreaEffect);
                }
                else if (c is NewMechanics.ContextCalculateAbilityParamsBasedOnClasses)
                {
                    (c as NewMechanics.ContextCalculateAbilityParamsBasedOnClasses).CharacterClasses = (c as NewMechanics.ContextCalculateAbilityParamsBasedOnClasses).CharacterClasses.AddToArray(class_to_add);
                }
            }
        }


        static void addClassToAbility(BlueprintCharacterClass class_to_add, BlueprintAbility a)
        {
            var components = a.ComponentsArray;
            foreach (var c in components)
            {
                if (c is Kingmaker.UnitLogic.Abilities.Components.AbilityVariants)
                {
                    var c_typed = (Kingmaker.UnitLogic.Abilities.Components.AbilityVariants)c;
                    foreach (var v in c_typed.Variants)
                    {
                        addClassToAbility(class_to_add, v);
                    }
                }
                else if (c is Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig)
                {
                    var c_typed = (Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig)c;
                    addClassToContextRankConfig(class_to_add, c_typed);
                }
                else if (c is AbilityEffectRunAction)
                {
                    var c_typed = (AbilityEffectRunAction)c;
                    foreach (var aa in c_typed.Actions.Actions)
                    {
                        if (aa == null)
                        {
                            continue;
                        }
                        if (aa is Kingmaker.UnitLogic.Mechanics.Actions.ContextActionApplyBuff)
                        {
                            var aa_typed = (Kingmaker.UnitLogic.Mechanics.Actions.ContextActionApplyBuff)aa;
                            addClassToBuff(class_to_add, aa_typed.Buff);
                        }
                    }
                }
                else if (c is NewMechanics.ContextCalculateAbilityParamsBasedOnClasses)
                {
                    (c as NewMechanics.ContextCalculateAbilityParamsBasedOnClasses).CharacterClasses = (c as NewMechanics.ContextCalculateAbilityParamsBasedOnClasses).CharacterClasses.AddToArray(class_to_add);
                }

            }
        }


        static void addClassToFact(BlueprintCharacterClass class_to_add, BlueprintArchetype[] archetypes_to_add, DomainSpellsType spells_type, BlueprintUnitFact f)
        {
            if (f is Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility)
            {
                var f_typed = (Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility)f;
                addClassToAbility(class_to_add, f_typed);
            }
            else if (f is Kingmaker.UnitLogic.ActivatableAbilities.BlueprintActivatableAbility)
            {
                var f_typed = (Kingmaker.UnitLogic.ActivatableAbilities.BlueprintActivatableAbility)f;
                addClassToBuff(class_to_add, f_typed.Buff);
            }
        }


        static void addClassToResource(BlueprintCharacterClass class_to_add, BlueprintArchetype[] archetypes_to_add, BlueprintAbilityResource rsc)
        {
            BlueprintCharacterClass cleric_class = library.Get<BlueprintCharacterClass>("67819271767a9dd4fbfd4ae700befea0");
            var amount = ExtensionMethods.getMaxAmount(rsc);
            var classes = Helpers.GetField<BlueprintCharacterClass[]>(amount, "Class");
            var archetypes = Helpers.GetField<BlueprintArchetype[]>(amount, "Archetypes");

            if (classes.Contains(cleric_class))
            {
                classes = classes.AddToArray(class_to_add);
                archetypes = archetypes.AddToArray(archetypes_to_add);
                Helpers.SetField(amount, "Class", classes);
                Helpers.SetField(amount, "Archetypes", archetypes);
                ExtensionMethods.setMaxAmount(rsc, amount);
            }

        }


        static void addClassToFeat(BlueprintCharacterClass class_to_add, BlueprintArchetype[] archetypes_to_add, DomainSpellsType spells_type, BlueprintFeatureBase feat)
        {
            foreach (var c in feat.ComponentsArray)
            {
                if (c is Kingmaker.Designers.Mechanics.Buffs.IncreaseSpellDamageByClassLevel)
                {
                    var c_typed = (Kingmaker.Designers.Mechanics.Buffs.IncreaseSpellDamageByClassLevel)c;
                    c_typed.AdditionalClasses = c_typed.AdditionalClasses.AddToArray(class_to_add);
                    c_typed.Archetypes = c_typed.Archetypes.AddToArray(archetypes_to_add);
                }
                else if (c is Kingmaker.Designers.Mechanics.Facts.AddFeatureOnClassLevel)
                {
                    var c_typed = (Kingmaker.Designers.Mechanics.Facts.AddFeatureOnClassLevel)c;
                    if (c_typed.Feature.ComponentsArray.Length > 0
                          && c_typed.Feature.ComponentsArray[0] is Kingmaker.UnitLogic.FactLogic.AddSpecialSpellList)
                    {
                        if (spells_type == DomainSpellsType.SpecialList)
                        {
                            //TODO: will need to make a copy of feature and replace CharacterClass in component with class_to_add
                        }
                        else
                        {
                            continue;
                        }
                    }
                    c_typed.AdditionalClasses = c_typed.AdditionalClasses.AddToArray(class_to_add);
                    c_typed.Archetypes = c_typed.Archetypes.AddToArray(archetypes_to_add);
                    addClassToFeat(class_to_add, archetypes_to_add, spells_type, c_typed.Feature);
                }
                else if (c is Kingmaker.UnitLogic.FactLogic.AddSpecialSpellList && spells_type == DomainSpellsType.SpecialList)
                {
                    /*var c_typed = (Kingmaker.UnitLogic.FactLogic.AddSpecialSpellList)c;
                    if (c_typed.CharacterClass != class_to_add)
                    {
                        var c2 = Helpers.Create<Kingmaker.UnitLogic.FactLogic.AddSpecialSpellList>();
                        c2.CharacterClass = class_to_add;
                        c2.SpellList = c_typed.SpellList;
                        feat.AddComponent(c2);
                    }*/
                }
                else if (c is Kingmaker.UnitLogic.FactLogic.AddFacts)
                {
                    var c_typed = (Kingmaker.UnitLogic.FactLogic.AddFacts)c;
                    foreach (var f in c_typed.Facts)
                    {
                        addClassToFact(class_to_add, archetypes_to_add, spells_type, f);
                    }
                }
                else if (c is Kingmaker.Designers.Mechanics.Facts.AddAbilityResources)
                {
                    var c_typed = (Kingmaker.Designers.Mechanics.Facts.AddAbilityResources)c;
                    addClassToResource(class_to_add, archetypes_to_add, c_typed.Resource);
                }
                else if (c is Kingmaker.Designers.Mechanics.Facts.FactSinglify)
                {
                    var c_typed = (Kingmaker.Designers.Mechanics.Facts.FactSinglify)c;
                    foreach (var f in c_typed.NewFacts)
                    {
                        addClassToFact(class_to_add, archetypes_to_add, spells_type, f);
                    }
                }
                else if (c is Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig)
                {
                    var c_typed = (Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig)c;
                    addClassToContextRankConfig(class_to_add, c_typed);
                }


            }
        }


        static public Kingmaker.UnitLogic.FactLogic.AddConditionImmunity createAddConditionImmunity(UnitCondition condition)
        {
            Kingmaker.UnitLogic.FactLogic.AddConditionImmunity c = Helpers.Create<Kingmaker.UnitLogic.FactLogic.AddConditionImmunity>();
            c.Condition = condition;
            return c;
        }


        static public Kingmaker.Designers.Mechanics.Facts.SavingThrowBonusAgainstDescriptor createSavingThrowBonusAgainstDescriptor(int bonus, ModifierDescriptor descriptor, SpellDescriptor spell_descriptor)
        {
            Kingmaker.Designers.Mechanics.Facts.SavingThrowBonusAgainstDescriptor c = Helpers.Create<Kingmaker.Designers.Mechanics.Facts.SavingThrowBonusAgainstDescriptor>();
            c.Value = bonus;
            c.Bonus = createSimpleContextValue(0);
            c.ModifierDescriptor = descriptor;
            c.SpellDescriptor = spell_descriptor;
            return c;
        }


        static public SavingThrowBonusAgainstAlignment createSavingThrowBonusAgainstAlignment(int bonus, ModifierDescriptor descriptor, AlignmentComponent alignment)
        {
            var c = Helpers.Create<SavingThrowBonusAgainstAlignment>();
            c.Value = bonus;
            c.Descriptor = descriptor;
            c.Alignment = alignment;
            return c;
        }


        static public Kingmaker.Designers.Mechanics.Facts.SavingThrowContextBonusAgainstDescriptor createContextSavingThrowBonusAgainstDescriptor(ContextValue value, ModifierDescriptor descriptor, SpellDescriptor spell_descriptor)
        {
            var c = Helpers.Create<Kingmaker.Designers.Mechanics.Facts.SavingThrowContextBonusAgainstDescriptor>();
            c.ModifierDescriptor = descriptor;
            c.SpellDescriptor = spell_descriptor;
            c.Value = value;
            return c;
        }


        static public SavingThrowBonusAgainstSchool createSavingThrowBonusAgainstSchool(int bonus, ModifierDescriptor descriptor, SpellSchool school)
        {
            var c = Helpers.Create<SavingThrowBonusAgainstSchool>();
            c.School = school;
            c.ModifierDescriptor = descriptor;
            c.Value = bonus;
            return c;
        }


        static public Kingmaker.UnitLogic.FactLogic.BuffEnchantWornItem createBuffEnchantWornItem(Kingmaker.Blueprints.Items.Ecnchantments.BlueprintItemEnchantment enchantment)
        {
            var b = Helpers.Create<BuffEnchantWornItem>();
            b.Enchantment = enchantment;
            return b;
        }


        static public Kingmaker.UnitLogic.FactLogic.AddEnergyDamageImmunity createAddEnergyDamageImmunity(DamageEnergyType energy_type)
        {
            var a = Helpers.Create<Kingmaker.UnitLogic.FactLogic.AddEnergyDamageImmunity>();
            a.EnergyType = energy_type;
            return a;
        }


        static public Kingmaker.UnitLogic.ActivatableAbilities.ActivatableAbilityUnitCommand createActivatableAbilityUnitCommand(CommandType command_type)
        {
            var a = Helpers.Create<ActivatableAbilityUnitCommand>();
            a.Type = command_type;
            return a;
        }


        static public Kingmaker.Designers.Mechanics.Facts.AttackTypeAttackBonus createAttackTypeAttackBonus(ContextValue value, AttackTypeAttackBonus.WeaponRangeType attack_type,
                                                                                                              ModifierDescriptor descriptor)
        {
            var a = Helpers.Create<AttackTypeAttackBonus>();
            a.AttackBonus = 1;
            a.Type = attack_type;
            a.Value = value;
            a.Descriptor = descriptor;
            return a;
        }



        static public BlueprintActivatableAbility createSwitchActivatableAbilityOnlyBuff(string name, string switch_guid, string ability_guid,
                                                            BlueprintBuff effect, BlueprintBuff target_buff, Kingmaker.ElementsSystem.GameAction[] pre_actions,
                                                            UnityEngine.AnimationClip animation,
                                                            ActivatableAbilityGroup group = ActivatableAbilityGroup.None, int weight = 1,
                                                            CommandType command_type = CommandType.Free, CommandType unit_command = CommandType.Free)

        {
            effect.SetBuffFlags(BuffFlags.HiddenInUi | effect.GetBuffFlags());
            var switch_buff = Helpers.CreateBuff(name + "SwitchBuff",
                                              effect.Name,
                                              effect.Description,
                                              switch_guid,
                                              effect.Icon,
                                              null,
                                              Helpers.CreateEmptyAddFactContextActions());

            Common.addContextActionApplyBuffOnFactsToActivatedAbilityBuff(switch_buff, effect, pre_actions, target_buff);
            Common.addContextActionApplyBuffOnFactsToActivatedAbilityBuff(target_buff, effect, pre_actions, switch_buff);

            var ability = Helpers.CreateActivatableAbility(name + "ToggleAbility",
                                                                        effect.Name,
                                                                        effect.Description,
                                                                        ability_guid,
                                                                        effect.Icon,
                                                                        switch_buff,
                                                                        AbilityActivationType.Immediately,
                                                                        command_type,
                                                                        animation
                                                                        );
            if (unit_command != CommandType.Free)
            {
                ability.AddComponent(Common.createActivatableAbilityUnitCommand(unit_command));
            }
            ability.Group = group;
            ability.WeightInGroup = weight;

            return ability;
        }


        static public BlueprintFeature createSwitchActivatableAbilityBuff(string name, string switch_guid, string ability_guid, string feature_guid,
                                                                    BlueprintBuff effect, BlueprintBuff target_buff, Kingmaker.ElementsSystem.GameAction[] pre_actions,
                                                                    UnityEngine.AnimationClip animation,
                                                                    ActivatableAbilityGroup group = ActivatableAbilityGroup.None, int weight = 1,
                                                                    CommandType command_type = CommandType.Free, CommandType unit_command = CommandType.Free)

        {
            var ability = createSwitchActivatableAbilityOnlyBuff(name, switch_guid, ability_guid,
                                                                    effect, target_buff, pre_actions,
                                                                    animation,
                                                                    group, weight,
                                                                    command_type, unit_command);
            var feature = Helpers.CreateFeature(name + "Feature",
                                                effect.Name,
                                                effect.Description,
                                                feature_guid,
                                                effect.Icon,
                                                FeatureGroup.None,
                                                CallOfTheWild.Helpers.CreateAddFact(ability));

            return feature;
        }


        static public BlueprintFeature createSwitchActivatableAbilityBuff(string name, string switch_guid, string ability_guid, string feature_guid,
                                                                            BlueprintBuff effect, BlueprintBuff target_buff, UnityEngine.AnimationClip animation,
                                                                            ActivatableAbilityGroup group = ActivatableAbilityGroup.None, int weight = 1,
                                                                            CommandType command_type = CommandType.Free, CommandType unit_command = CommandType.Free)

        {
            return createSwitchActivatableAbilityBuff(name, switch_guid, ability_guid, feature_guid, effect, target_buff, new Kingmaker.ElementsSystem.GameAction[0],
                                                      animation, group, weight, command_type, unit_command);
        }


        static public ContextActionRemoveBuffsByDescriptor createContextActionRemoveBuffsByDescriptor(SpellDescriptor descriptor, bool not_self = true)
        {
            var r = Helpers.Create<ContextActionRemoveBuffsByDescriptor>();
            r.SpellDescriptor = descriptor;
            r.NotSelf = not_self;
            return r;
        }


        static public NewMechanics.AddContextEffectFastHealing createAddContextEffectFastHealing(ContextValue value, int multiplier = 1)
        {
            var a = Helpers.Create<NewMechanics.AddContextEffectFastHealing>();
            a.Value = value;
            a.Multiplier = multiplier;
            return a;
        }


        static public Kingmaker.Designers.Mechanics.Facts.AuraFeatureComponent createAuraFeatureComponent(BlueprintBuff buff)
        {
            var a = Helpers.Create<Kingmaker.Designers.Mechanics.Facts.AuraFeatureComponent>();
            a.Buff = buff;
            return a;
        }


        static public Kingmaker.UnitLogic.Mechanics.Actions.ContextActionHealTarget createContextActionHealTarget(ContextDiceValue value)
        {
            var c = Helpers.Create<ContextActionHealTarget>();
            c.Value = value;
            return c;
        }


        static public Kingmaker.UnitLogic.FactLogic.AddProficiencies createAddArmorProficiencies(params ArmorProficiencyGroup[] armor)
        {
            var a = Helpers.Create<Kingmaker.UnitLogic.FactLogic.AddProficiencies>();
            a.ArmorProficiencies = armor;
            return a;
        }

        static public Kingmaker.UnitLogic.FactLogic.AddProficiencies createAddWeaponProficiencies(params WeaponCategory[] weapons)
        {
            var a = Helpers.Create<Kingmaker.UnitLogic.FactLogic.AddProficiencies>();
            a.WeaponProficiencies = weapons;
            return a;
        }


        static public AddEnergyVulnerability createAddEnergyVulnerability(DamageEnergyType energy)
        {
            var a = Helpers.Create<AddEnergyVulnerability>();
            a.Type = energy;
            return a;
        }


        static public AbilityCasterHasNoFacts createAbilityCasterHasNoFacts(params BlueprintUnitFact[] facts)
        {
            var a = Helpers.Create<AbilityCasterHasNoFacts>();
            a.Facts = facts;
            return a;
        }

        static public AbilityCasterHasFacts createAbilityCasterHasFacts(params BlueprintUnitFact[] facts)
        {
            var a = Helpers.Create<AbilityCasterHasFacts>();
            a.Facts = facts;
            return a;
        }


        static public AddGenericStatBonus createAddGenericStatBonus(int bonus, ModifierDescriptor descriptor, StatType stat)
        {
            var a = Helpers.Create<AddGenericStatBonus>();
            a.Stat = stat;
            a.Value = bonus;
            a.Descriptor = descriptor;
            return a;
        }


        static public ChangeUnitSize createChangeUnitSize(Size size)
        {
            var c = Helpers.Create<ChangeUnitSize>();
            c.Size = size;
            Helpers.SetField(c, "m_Type", 1);
            return c;
        }


        static public void addReplaceSpellbook(BlueprintFeatureSelection selection, BlueprintSpellbook spellbook, string name, params BlueprintComponent[] components)
        {
            addReplaceSpellbook(selection, spellbook, name, spellbook.Name, components);
        }

        static public void addReplaceSpellbook(BlueprintFeatureSelection selection, BlueprintSpellbook spellbook, string name, string display_name, params BlueprintComponent[] components)
        {
            var feature = Helpers.Create<BlueprintFeatureReplaceSpellbook>();
            feature.name = name;
            feature.Groups = new FeatureGroup[] { selection.Group };
            feature.IsClassFeature = true;
            feature.SetName(display_name);
            feature.SetDescription(selection.Description);
            feature.ComponentsArray = components;
            feature.Spellbook = spellbook;
            
            library.AddAsset(feature, Helpers.GuidStorage.hasStoredGuid(feature.name) ? "" : Helpers.MergeIds(selection.AssetGuid, spellbook.AssetGuid)); //fix to ensure that classes of other mods will also work
            feature.Groups = new FeatureGroup[] { selection.Group };
            selection.AllFeatures = selection.AllFeatures.AddToArray(feature);
        }


        static public void addMTDivineSpellbookProgression(BlueprintCharacterClass @class, BlueprintSpellbook spellbook, string name, params BlueprintComponent[] components)
        {
            var mt_divine_spellbook_selection = library.Get<BlueprintFeatureSelection>("7cd057944ce7896479717778330a4933");
            BlueprintProgression cleric_progression = mt_divine_spellbook_selection.AllFeatures[0] as BlueprintProgression;
            BlueprintFeature cleric_lvlup = cleric_progression.LevelEntries[0].Features[0] as BlueprintFeature;

            var progression = library.CopyAndAdd<BlueprintProgression>(cleric_progression.AssetGuid, name + "Progression", "");
            var lvl_up = library.CopyAndAdd<BlueprintFeature>(cleric_lvlup.AssetGuid, name + "LevelUpFeature", "");
            lvl_up.ReplaceComponent<AddSpellbookLevel>(a => a.Spellbook = spellbook);
            var mt_spellbook = progression.GetComponent<MysticTheurgeSpellbook>().CreateCopy();
            mt_spellbook.CharacterClass = @class;
            progression.ComponentsArray = components.AddToArray(mt_spellbook);
            progression.SetName(spellbook.Name);

            List<BlueprintFeature> sp_features = new List<BlueprintFeature>();
            if (spellbook.Spontaneous)
            {
                sp_features = CreateSpontaneousDivineCasterSpellSelectionForMt(name, @class, spellbook);
            }

            for (int i = 0; i < progression.LevelEntries.Length; i++)
            {
                progression.LevelEntries[i] = Helpers.LevelEntry(progression.LevelEntries[i].Level, lvl_up);
                progression.LevelEntries[i].Features.AddRange(sp_features);
            }
            mt_divine_spellbook_selection.AllFeatures = mt_divine_spellbook_selection.AllFeatures.AddToArray(progression);
        }


        static public List<BlueprintFeature> CreateSpontaneousDivineCasterSpellSelectionForMt(string name, BlueprintCharacterClass @class, BlueprintSpellbook spellbook)
        {
            List<BlueprintFeature> features1 = new List<BlueprintFeature>();
            List<BlueprintFeature> features2 = new List<BlueprintFeature>();
            int max_spell_level = spellbook.SpellsKnown.Levels.Last().Count.Length - 1;
            for (int i = 0; i < max_spell_level; i++)
            {
                for (int j = 0; j < 2; j++) //we need to create 2 copies of each since at some levels sp casters can get 2 spells
                {
                    var parametrize_feature = library.CopyAndAdd<BlueprintParametrizedFeature>("bcd757ac2aeef3c49b77e5af4e510956", name + $"SpellSelection{j + 1}_{i + 1}ParametrizedFeature", ""); //from inquisitor
                    parametrize_feature.SpellLevel = i + 1;
                    string level_ext = i == 0 ? "st" : (i == 1 ? "nd" : (i == 2 ? "rd" : "th"));
                    parametrize_feature.SetName($"{@class.Name} Spell ({i + 1}{level_ext} Level)");
                    parametrize_feature.SetDescription($"You can select new known {@class.Name.ToLower()} spell when you gain a new level in mystic theurge.");
                    parametrize_feature.ReplaceComponent<LearnSpellParametrized>(l =>
                    {
                        l.SpellLevel = i + 1;
                        l.SpellcasterClass = @class;
                        l.SpecificSpellLevel = spellbook.SpellList;
                    });
                    parametrize_feature.SpellcasterClass = @class;
                    parametrize_feature.SpellList = spellbook.SpellList;
                    parametrize_feature.SetIcon(null);
                    var can_learn = Helpers.Create<NewMechanics.PrerequisiteKnownSpellAquired>();
                    can_learn.num_spells_will_learn = j + 1;
                    can_learn.spell_level = i + 1;
                    can_learn.spellbook = spellbook;
                    parametrize_feature.AddComponent(can_learn);

                    if (j == 0)
                    {
                        BlueprintFeatureSelection selection = library.CopyAndAdd<BlueprintFeatureSelection>("8ae18c62c0fbfeb4ea77f877883947fd", name + $"SpellSelection{i + 1}FeatureSelection", ""); //from inquisitor
                        selection.AllFeatures = new BlueprintFeature[] { parametrize_feature };
                        selection.Features = selection.AllFeatures;
                        selection.SetName(parametrize_feature.Name);
                        selection.SetDescription(parametrize_feature.Description);
                        selection.SetIcon(null);
                        features1.Add(selection);
                    }
                    else
                    {
                        features2.Add(parametrize_feature);
                    }
                }
            }

            BlueprintFeatureSelection selection2 = library.CopyAndAdd<BlueprintFeatureSelection>("8ae18c62c0fbfeb4ea77f877883947fd", name + $"2nsSpellSelectionFeatureSelection", ""); //from inquisitor
            selection2.AllFeatures = features2.ToArray();
            selection2.Features = selection2.AllFeatures;
            selection2.SetName($"Extra {@class.Name.ToLower()} Spell Selection");
            selection2.SetDescription($"You can select new known {@class.Name.ToLower()} spell when you gain a new level in mystic theurge.");
            selection2.SetIcon(null);
            features1.Add(selection2);

            return features1;
            
        }


        static public PrerequisiteClassSpellLevel createPrerequisiteClassSpellLevel(BlueprintCharacterClass character_class, int spell_level)
        {
            var p = Helpers.Create<PrerequisiteClassSpellLevel>();
            p.CharacterClass = character_class;
            p.RequiredSpellLevel = spell_level;
            return p;
        }


        static public ContextActionRemoveBuff createContextActionRemoveBuff(BlueprintBuff buff)
        {
            var r = Helpers.Create<ContextActionRemoveBuff>();
            r.Buff = buff;
            return r;
        }


        static public NewMechanics.SavingThrowBonusAgainstSpecificSpells createSavingThrowBonusAgainstSpecificSpells(int bonus, ModifierDescriptor descriptor, params BlueprintAbility[] spells)
        {
            var s = Helpers.Create<NewMechanics.SavingThrowBonusAgainstSpecificSpells>();
            s.Spells = spells;
            s.ModifierDescriptor = descriptor;
            s.Value = bonus;
            return s;
        }


        static public AbilityTargetHasFact createAbilityTargetHasFact(bool inverted, params BlueprintUnitFact[] facts)
        {

            var a = Helpers.Create<AbilityTargetHasFact>();
            a.CheckedFacts = facts;
            a.Inverted = inverted;
            return a;
        }


        static public NewMechanics.AbilityTargetHasNoFactUnlessBuffsFromCaster createAbilityTargetHasNoFactUnlessBuffsFromCaster(BlueprintBuff[] target_buffs,
                                                                                                          params BlueprintBuff[] alternative_buffs)
        {
            var h = Helpers.Create<NewMechanics.AbilityTargetHasNoFactUnlessBuffsFromCaster>();
            h.CheckedBuffs = target_buffs;
            h.AlternativeBuffs = alternative_buffs;
            return h;
        }


        static public Kingmaker.UnitLogic.Abilities.Components.TargetCheckers.AbilityTargetIsPartyMember createAbilityTargetIsPartyMember(bool val = false)
        {
            var a = Helpers.Create<AbilityTargetIsPartyMember>();
            a.Not = !val;
            return a;
        }


        static public AbilityShowIfCasterHasFact createAbilityShowIfCasterHasFact(BlueprintUnitFact fact)
        {
            var a = Helpers.Create<AbilityShowIfCasterHasFact>();
            a.UnitFact = fact;
            return a;
        }


        static public NewMechanics.AbilityShowIfCasterHasFacts createAbilityShowIfCasterHasFacts(params BlueprintUnitFact[] facts)
        {
            var a = Helpers.Create<NewMechanics.AbilityShowIfCasterHasFacts>();
            a.UnitFacts = facts;
            return a;
        }


        static public ContextConditionHasFact createContextConditionHasFact(BlueprintUnitFact fact, bool has = true)
        {
            var c = Helpers.Create<ContextConditionHasFact>();
            c.Fact = fact;
            c.Not = !has;
            return c;
        }


        static public ContextConditionCasterHasFact createContextConditionCasterHasFact(BlueprintUnitFact fact, bool has = true)
        {
            var c = Helpers.Create<ContextConditionCasterHasFact>();
            c.Fact = fact;
            c.Not = !has;
            return c;
        }


        public static void AddBattleLogMessage(string message, object tooltip = null, Color? color = null)
        {
            var data = new LogDataManager.LogItemData(message, color ?? GameLogStrings.Instance.DefaultColor, tooltip, PrefixIcon.None);
            if (Game.Instance.UI.BattleLogManager)
            {
                Game.Instance.UI.BattleLogManager.LogView.AddLogEntry(data);
            }
        }


        static public ClassLevelsForPrerequisites createClassLevelsForPrerequisites(BlueprintCharacterClass fake_class, BlueprintCharacterClass actual_class, double modifier = 1.0, int summand = 0)
        {
            var c = Helpers.Create<ClassLevelsForPrerequisites>();
            c.ActualClass = actual_class;
            c.FakeClass = fake_class;
            c.Modifier = modifier;
            c.Summand = summand;
            return c;
        }


        static public ACBonusAgainstFactOwner createACBonusAgainstFactOwner(int bonus, ModifierDescriptor descriptor, BlueprintUnitFact fact)
        {
            var a = Helpers.Create<ACBonusAgainstFactOwner>();
            a.Bonus = bonus;
            a.Descriptor = descriptor;
            a.CheckedFact = fact;
            return a;
        }


        static public void addTemworkFeats(BlueprintFeature feat, bool share = true)
        {
            var tactical_leader_feat_share_buff = library.Get<BlueprintBuff>("a603a90d24a636c41910b3868f434447");
            var monster_tactics_buff = library.Get<BlueprintBuff>("81ddc40b935042844a0b5fb052eeca73");
            var sh_teamwork_share = library.Get<BlueprintFeature>("e1f437048db80164792155102375b62c");
            var teamwork_feat = library.Get<BlueprintFeatureSelection>("d87e2f6a9278ac04caeb0f93eff95fcb");
            var teamwork_feat_vanguard = library.Get<BlueprintFeatureSelection>("90b882830b3988446ae681c6596460cc");

            if (share)
            {
                sh_teamwork_share.GetComponent<ShareFeaturesWithCompanion>().Features = sh_teamwork_share.GetComponent<ShareFeaturesWithCompanion>().Features.AddToArray(feat);
                monster_tactics_buff.GetComponent<AddFactsFromCaster>().Facts = monster_tactics_buff.GetComponent<AddFactsFromCaster>().Facts.AddToArray(feat);
                //Hunter.hunter_tactics.GetComponent<ShareFeaturesWithCompanion>().Features = Hunter.hunter_tactics.GetComponent<ShareFeaturesWithCompanion>().Features.AddToArray(feats); - same as inquisitor
                tactical_leader_feat_share_buff.GetComponent<AddFactsFromCaster>().Facts = tactical_leader_feat_share_buff.GetComponent<AddFactsFromCaster>().Facts.AddToArray(feat);
            }
            teamwork_feat.AllFeatures = teamwork_feat.AllFeatures.AddToArray(feat);
            Hunter.hunter_teamwork_feat.AllFeatures = teamwork_feat.AllFeatures;
            teamwork_feat_vanguard.AllFeatures = teamwork_feat_vanguard.AllFeatures.AddToArray(feat);

            VindicativeBastard.teamwork_feat.AllFeatures = VindicativeBastard.teamwork_feat.AllFeatures.AddToArray(feat);



            //update vanguard features
            var vanguard_variants = library.Get<BlueprintAbility>("00af3b5f43aa7ae4c87bcfe4e129f6e8").GetComponent<AbilityVariants>();

            var buff = library.CopyAndAdd<BlueprintBuff>("9de63078d422dcc46a86ba0920b4991e", "VanguardTactician" + feat.name + "Buff", "");
            var add_fact = buff.GetComponent<AddFactsFromCaster>().CreateCopy();
            add_fact.Facts = new BlueprintUnitFact[] { feat };
            buff.ReplaceComponent<AddFactsFromCaster>(add_fact);
            buff.SetName("Vanguard Tactician — " + feat.Name);
            buff.SetDescription(feat.Description);


            var ability = library.CopyAndAdd<BlueprintAbility>("53f4d8597163db24f8309462aadc4348", "VanguardTactician" + feat.name + "Ability", "");
            ability.ReplaceComponent<AbilityShowIfCasterHasFact>(Common.createAbilityShowIfCasterHasFact(feat));

            var condition_apply_buff = (Conditional)ability.GetComponent<AbilityEffectRunAction>().Actions.Actions[0];
            var context_apply_buff = ((ContextActionApplyBuff)condition_apply_buff.IfTrue.Actions[0]).CreateCopy();
            context_apply_buff.Buff = buff;
            var run_action = Helpers.CreateRunActions(Helpers.CreateConditional(Common.createContextConditionHasFact(feat, false), context_apply_buff));

            ability.ReplaceComponent<AbilityEffectRunAction>(run_action);
            ability.SetName(buff.Name);
            ability.SetDescription(buff.Description);

            if (share)
            {
                vanguard_variants.Variants = vanguard_variants.Variants.AddToArray(ability);
            }

        }


        static public AddFeatureIfHasFact createAddFeatureIfHasFact(BlueprintUnitFact fact, BlueprintUnitFact feature, bool not = false)
        {
            var a = Helpers.Create<AddFeatureIfHasFact>();
            a.CheckedFact = fact;
            a.Feature = feature;
            a.Not = not;
            return a;
        }


        static public NewMechanics.AddFeatureIfHasFactAndNotHasFact createAddFeatureIfHasFactAndNotHasFact(BlueprintUnitFact has_fact, BlueprintUnitFact not_has_fact, BlueprintUnitFact feature)
        {
            var a = Helpers.Create<NewMechanics.AddFeatureIfHasFactAndNotHasFact>();
            a.HasFact = has_fact;
            a.NotHasFact = not_has_fact;
            a.Feature = feature;
            return a;
        }


        static public BuffExtraAttack createBuffExtraAttack(int num, bool haste)
        {
            var b = Helpers.Create<BuffExtraAttack>();
            b.Number = num;
            b.Haste = haste;
            return b;
        }


        static public ContextConditionIsCaster createContextConditionIsCaster(bool not = false)
        {
            var c = Helpers.Create<ContextConditionIsCaster>();
            c.Not = not;
            return c;
        }


        static public AddWearinessHours createAddWearinessHours(int hours)
        {
            var a = Helpers.Create<AddWearinessHours>();
            a.Hours = hours;
            return a;
        }


        static public AbilityScoreCheckBonus createAbilityScoreCheckBonus(ContextValue bonus, ModifierDescriptor descriptor, StatType stat)
        {
            var a = Helpers.Create<AbilityScoreCheckBonus>();
            a.Bonus = bonus;
            a.Descriptor = descriptor;
            a.Stat = stat;
            return a;
        }


        static public ContextActionResurrect createContextActionResurrect(float result_health, bool full_restore = false)
        {
            var c = Helpers.Create<ContextActionResurrect>();
            c.ResultHealth = result_health;
            c.FullRestore = full_restore;
            return c;
        }


        static public NewMechanics.ContextActionRemoveBuffFromCaster createContextActionRemoveBuffFromCaster(BlueprintBuff buff, int delay = 0)
        {
            var c = Helpers.Create<NewMechanics.ContextActionRemoveBuffFromCaster>();
            c.Buff = buff;
            c.remove_delay_seconds = delay;
            return c;
        }


        static public BlueprintActivatableAbility convertPerformance(BlueprintActivatableAbility base_ability, BlueprintBuff effect_buff, string prefix)
        {
            var ability = library.CopyAndAdd<BlueprintActivatableAbility>(base_ability.AssetGuid, prefix + "Ability", "");
            var ability_buff = library.CopyAndAdd<BlueprintBuff>(base_ability.Buff.AssetGuid, prefix + "Buff", "");
            var area = library.CopyAndAdd<BlueprintAbilityAreaEffect>(ability_buff.GetComponent<AddAreaEffect>().AreaEffect.AssetGuid, prefix + "Area", "");

            area.ReplaceComponent<AbilityAreaEffectBuff>(c => { c.Buff = effect_buff; });

            ability_buff.ReplaceComponent<AddAreaEffect>(c => { c.AreaEffect = area; });
            ability_buff.SetName(effect_buff.Name);
            ability_buff.SetDescription(effect_buff.Description);
            ability_buff.SetIcon(effect_buff.Icon);


            ability.SetName(effect_buff.Name);
            ability.SetDescription(effect_buff.Description);
            ability.SetIcon(effect_buff.Icon);
            ability.Buff = ability_buff;

            return ability;
        }



        static public BlueprintActivatableAbility convertPerformance(BlueprintActivatableAbility base_ability, BlueprintAbilityAreaEffect area, string prefix,
                                                                                                                                     UnityEngine.Sprite icon, string display_name, string description)
        {
            var ability = library.CopyAndAdd<BlueprintActivatableAbility>(base_ability.AssetGuid, prefix + "Ability", "");
            var ability_buff = library.CopyAndAdd<BlueprintBuff>(base_ability.Buff.AssetGuid, prefix + "Buff", "");

            ability_buff.ReplaceComponent<AddAreaEffect>(c => { c.AreaEffect = area; });
            ability_buff.SetName(display_name);
            ability_buff.SetDescription(description);
            ability_buff.SetIcon(icon);


            ability.SetName(display_name);
            ability.SetDescription(description);
            ability.SetIcon(icon);
            ability.Buff = ability_buff;

            return ability;
        }


        static public ContextActionDispelMagic createContextActionDispelMagic(SpellDescriptor spell_descriptor, SpellSchool[] schools, RuleDispelMagic.CheckType check_type,
                                                                                 ContextValue max_spell_level = null, ContextValue max_caster_level = null)
        {
            var c = Helpers.Create<ContextActionDispelMagic>();
            c.Descriptor = spell_descriptor;
            c.Schools = schools;
            var spell_level = max_spell_level == null ? createSimpleContextValue(9) : max_spell_level;
            Helpers.SetField(c, "m_MaxSpellLevel", spell_level);
            if (max_caster_level == null)
            {
                Helpers.SetField(c, "m_UseMaxCasterLevel", false);
            }
            else
            {
                Helpers.SetField(c, "m_UseMaxCasterLevel", true);
                Helpers.SetField(c, "m_MaxCasterLevel", max_caster_level);
            }
            Helpers.SetField(c, "m_CheckType", check_type);
            return c;
        }


        static public NewMechanics.CrowdAlliesACBonus createCrowdAlliesACBonus(int min_num_allies_around, ContextValue value, int radius = 2 /* in meters ~ roughly 7 feets*/)
        {
            var c = Helpers.Create<NewMechanics.CrowdAlliesACBonus>();
            c.num_allies_around = min_num_allies_around;
            c.value = value;
            c.Radius = radius;
            return c;
        }


        static public BlueprintFeature AbilityToFeature(BlueprintAbility ability, bool hide = true, string guid = "")
        {
            var feature = Helpers.CreateFeature(ability.name + "Feature",
                                                     ability.Name,
                                                     ability.Description,
                                                     guid,
                                                     ability.Icon,
                                                     FeatureGroup.None
                                                     );
            feature.AddComponent(Common.createAddFeatureIfHasFact(ability, ability, not: true));
            if (hide)
            {
                feature.HideInCharacterSheetAndLevelUp = true;
                feature.HideInUI = true;
            }
            return feature;
        }


        static public BlueprintFeature ActivatableAbilityToFeature(BlueprintActivatableAbility ability, bool hide = true, string guid = "")
        {
            var feature = Helpers.CreateFeature(ability.name + "Feature",
                                                     ability.Name,
                                                     ability.Description,
                                                     guid,
                                                     ability.Icon,
                                                     FeatureGroup.None,
                                                     Helpers.CreateAddFact(ability)
                                                     );
            if (hide)
            {
                feature.HideInCharacterSheetAndLevelUp = true;
                feature.HideInUI = true;
            }
            return feature;
        }


        static public NewMechanics.ComeAndGetMe createComeAndGetMe()
        {
            var c = Helpers.Create<NewMechanics.ComeAndGetMe>();
            return c;
        }


        public static BlueprintAbility[] CreateAbilityVariantsReplace(BlueprintAbility parent, string prefix, Action<BlueprintAbility> action, bool as_duplicates, 
                                                                      params BlueprintAbility[] variants)
        {
            var clear_variants = variants.Distinct().ToArray();
            List<BlueprintAbility> processed_spells = new List<BlueprintAbility>();

            foreach (var v in clear_variants)
            {
                
                var variants_comp = v.GetComponent<AbilityVariants>();

                if (variants_comp != null)
                {
                    var variant_spells = CreateAbilityVariantsReplace(parent, prefix, action, as_duplicates, variants_comp.Variants);
                    processed_spells = processed_spells.Concat(variant_spells).ToList();
                }
                else
                {
                    BlueprintAbility processed_spell = null;
                    if (!as_duplicates)
                    {
                        processed_spell = library.CopyAndAdd<BlueprintAbility>(v.AssetGuid, prefix + v.name, Helpers.MergeIds(parent.AssetGuid, v.AssetGuid));
                    }
                    else
                    {
                        processed_spell = SpellDuplicates.addDuplicateSpell(v, prefix + v.name, Helpers.MergeIds(parent.AssetGuid, v.AssetGuid));
                    }
                    if (action != null)
                    {
                        action(processed_spell);
                    }
                    processed_spell.Parent = parent;
                    processed_spell.RemoveComponents<SpellListComponent>();
                    processed_spells.Add(processed_spell);
                }
            }
            return processed_spells.ToArray();
        }


        static public void addToFactInContextConditionHasFact(BlueprintBuff buff, BlueprintUnitFact inner_buff_to_locate = null,
                                                       Condition condition_to_add = null)
        {
            var component = buff.GetComponent<AddFactContextActions>();
            if (component == null)
            {
                return;
            }

            var activated_actions = component.Activated.Actions;

            for (int i = 0; i < activated_actions.Length; i++)
            {
                if (activated_actions[i] is Conditional)
                {
                    var c_action = (Conditional)activated_actions[i].CreateCopy();
                    for (int j = 0; j < c_action.ConditionsChecker.Conditions.Length; j++)
                    {
                        if (c_action.ConditionsChecker.Conditions[j] is ContextConditionHasFact)
                        {
                            var condition_entry = (ContextConditionHasFact)c_action.ConditionsChecker.Conditions[j];
                            var fact = condition_entry.Fact;
                            if (fact == inner_buff_to_locate)
                            {
                                c_action.ConditionsChecker.Conditions = c_action.ConditionsChecker.Conditions.AddToArray(condition_to_add);
                                c_action.ConditionsChecker.Operation = Kingmaker.ElementsSystem.Operation.Or;
                                activated_actions[i] = c_action;
                                break;
                            }
                        }
                    }
                }
            }
        }



        static public NewMechanics.ContextWeaponDamageBonus createContextWeaponDamageBonus(ContextValue bonus, bool apply_to_melee = true, bool apply_to_ranged = false, bool apply_to_thrown = true,
                                                                                             bool scale_2h = true)
        {
            var c = Helpers.Create<NewMechanics.ContextWeaponDamageBonus>();
            c.apply_to_melee = apply_to_melee;
            c.apply_to_ranged = apply_to_ranged;
            c.apply_to_thrown = apply_to_thrown;
            c.value = bonus;
            c.scale_for_2h = scale_2h;
            return c;
        }


        static public VitalStrikeMechanics.VitalStrikeScalingDamage createVitalStrikeScalingDamage(ContextValue value, int multiplier = 1)
        {
            var v = Helpers.Create<VitalStrikeMechanics.VitalStrikeScalingDamage>();
            v.Value = value;
            v.multiplier = multiplier;
            return v;
        }


        static public SavingThrowBonusAgainstAbilityType createSavingThrowBonusAgainstAbilityType(int base_value, ContextValue bonus, AbilityType ability_type)
        {
            var b = Helpers.Create<SavingThrowBonusAgainstAbilityType>();
            b.Value = base_value;
            b.Bonus = bonus;
            b.AbilityType = ability_type;
            return b;
        }

        static public PrerequisiteParametrizedFeature createPrerequisiteParametrizedFeatureWeapon(BlueprintParametrizedFeature feature, WeaponCategory category, bool any = false)
        {
            var p = Helpers.Create<PrerequisiteParametrizedFeature>();
            p.Feature = feature;
            p.ParameterType = FeatureParameterType.WeaponCategory;
            p.WeaponCategory = category;
            p.Group = any ? Prerequisite.GroupType.Any : Prerequisite.GroupType.All;
            return p;
        }


        static public NewMechanics.ForbidSpellCastingUnlessHasClass createForbidSpellCastingUnlessHasClass(bool forbid_magic_items, params BlueprintCharacterClass[] classes)
        {
            var f = Helpers.Create<NewMechanics.ForbidSpellCastingUnlessHasClass>();
            f.allowed_classes = classes;
            f.ForbidMagicItems = forbid_magic_items;
            return f;
        }


        static public NewMechanics.EnchantmentMechanics.WeaponDamageStatReplacement createWeaponDamageStatReplacementEnchantment(StatType stat)
        {
            var w = Helpers.Create<NewMechanics.EnchantmentMechanics.WeaponDamageStatReplacement>();
            w.Stat = stat;
            return w;
        }



        static public BlueprintWeaponEnchantment createWeaponEnchantment(string name, string display_name, string description, string prefix, string suffix, string guid, int identify_dc, GameObject fx_prefab, params BlueprintComponent[] components)
        {
            var e = Helpers.Create<BlueprintWeaponEnchantment>();
            Helpers.SetField(e, "m_IdentifyDC", identify_dc);
            e.name = name;

            Helpers.SetField(e, "m_EnchantName", Helpers.CreateString($"{name}.DisplayName", display_name));
            Helpers.SetField(e, "m_Description", Helpers.CreateString($"{name}.Description", description));
            Helpers.SetField(e, "m_Prefix", Helpers.CreateString($"{name}.Prefix", prefix));
            Helpers.SetField(e, "m_Suffix", Helpers.CreateString($"{name}.Suffix", suffix));
            e.AddComponents(components);
            e.WeaponFxPrefab = fx_prefab;
            library.AddAsset(e, guid);

            return e;
        }


        static public BlueprintArmorEnchantment createArmorEnchantment(string name, string display_name, string description, string prefix, string suffix, string guid, int identify_dc, int cost, params BlueprintComponent[] components)
        {
            var e = Helpers.Create<BlueprintArmorEnchantment>();
            Helpers.SetField(e, "m_IdentifyDC", identify_dc);
            e.name = name;

            Helpers.SetField(e, "m_EnchantName", Helpers.CreateString($"{name}.DisplayName", display_name));
            Helpers.SetField(e, "m_Description", Helpers.CreateString($"{name}.Description", description));
            Helpers.SetField(e, "m_Prefix", Helpers.CreateString($"{name}.Prefix", prefix));
            Helpers.SetField(e, "m_Suffix", Helpers.CreateString($"{name}.Suffix", suffix));
            Helpers.SetField(e, "m_EnchantmentCost", cost);
            e.AddComponents(components);
            library.AddAsset(e, guid);

            return e;
        }

        static public NewMechanics.EnchantmentMechanics.WeaponAttackStatReplacement createWeaponAttackStatReplacementEnchantment(StatType stat)
        {
            var w = Helpers.Create<NewMechanics.EnchantmentMechanics.WeaponAttackStatReplacement>();
            w.Stat = stat;
            return w;
        }

        static public void addEnchantment(BlueprintItemWeapon weapon, params BlueprintWeaponEnchantment[] enchantments)
        {
            BlueprintWeaponEnchantment[] original_enchantments = Helpers.GetField<BlueprintWeaponEnchantment[]>(weapon, "m_Enchantments");
            Helpers.SetField(weapon, "m_Enchantments", original_enchantments.AddToArray(enchantments));
        }

        static public DamageTypeDescription createEnergyDamageDescription(DamageEnergyType energy)
        {
            var d = new DamageTypeDescription();
            d.Energy = energy;
            d.Type = DamageType.Energy;
            return d;
        }

        static public DamageTypeDescription createForceDamageDescription()
        {
            var d = new DamageTypeDescription();
            d.Type = DamageType.Force;
            return d;
        }


        static public NewMechanics.EnchantmentMechanics.BuffContextEnchantPrimaryHandWeapon createBuffContextEnchantPrimaryHandWeapon(ContextValue value,
                                                                                                                   bool only_non_magical, bool lock_slot,
                                                                                                                   BlueprintWeaponType[] allowed_types,
                                                                                                                   params BlueprintWeaponEnchantment[] enchantments)
        {
            var b = Helpers.Create<NewMechanics.EnchantmentMechanics.BuffContextEnchantPrimaryHandWeapon>();
            b.only_non_magical = only_non_magical;
            b.allowed_types = allowed_types;
            b.lock_slot = lock_slot;
            b.enchantments = enchantments;
            b.value = value;
            return b;
        }

        static public NewMechanics.EnchantmentMechanics.BuffContextEnchantPrimaryHandWeapon createBuffContextEnchantPrimaryHandWeapon(ContextValue value,
                                                                                                           bool only_non_magical, bool lock_slot,
                                                                                                           params BlueprintWeaponEnchantment[] enchantments)
        {
            return createBuffContextEnchantPrimaryHandWeapon(value, only_non_magical, lock_slot, new BlueprintWeaponType[0], enchantments);
        }


        static public NewMechanics.EnchantmentMechanics.BuffContextEnchantArmor createBuffContextEnchantArmor(ContextValue value,
                                                                                                           bool only_non_magical, bool lock_slot,
                                                                                                           params BlueprintArmorEnchantment[] enchantments)
        {
            var b = Helpers.Create<NewMechanics.EnchantmentMechanics.BuffContextEnchantArmor > ();
            b.only_non_magical = only_non_magical;
            b.lock_slot = lock_slot;
            b.enchantments = enchantments;
            b.value = value;
            return b;
        }


        static public AbilityCasterMainWeaponCheck createAbilityCasterMainWeaponCheck(params WeaponCategory[] category)
        {
            var a = Helpers.Create<AbilityCasterMainWeaponCheck>();
            a.Category = category;
            return a;
        }


        static public NewMechanics.AbilitTargetMainWeaponCheck createAbilitTargetMainWeaponCheck(params WeaponCategory[] category)
        {
            var a = Helpers.Create<NewMechanics.AbilitTargetMainWeaponCheck>();
            a.Category = category;
            return a;
        }


        static public NewMechanics.EnchantmentMechanics.BuffContextEnchantPrimaryHandWeaponIfHasMetamagic createBuffContextEnchantPrimaryHandWeaponIfHasMetamagic(Metamagic metamagic, bool only_non_magical, bool lock_slot,
                                                                                                                            BlueprintWeaponType[] allowed_types, BlueprintWeaponEnchantment enchantment)
        {
            var b = Helpers.Create<NewMechanics.EnchantmentMechanics.BuffContextEnchantPrimaryHandWeaponIfHasMetamagic>();
            b.allowed_types = allowed_types;
            b.enchantment = enchantment;
            b.only_non_magical = only_non_magical;
            b.lock_slot = lock_slot;
            b.metamagic = metamagic;
            return b;
        }


        static public AddParametrizedFeatures createAddParametrizedFeatures(BlueprintParametrizedFeature feature, WeaponCategory category)
        {
            var data = Activator.CreateInstance(ParametrizedFeatureData);
            Helpers.SetField(data, "Feature", feature);
            Helpers.SetField(data, "ParamWeaponCategory", category);

            var data_array = Array.CreateInstance(ParametrizedFeatureData, 1);
            data_array.SetValue(data, 0);

            var a = Helpers.Create<AddParametrizedFeatures>();
            Helpers.SetField(a, "m_Features", data_array);
            return a;
        }

        static public IncreaseActivatableAbilityGroupSize createIncreaseActivatableAbilityGroupSize(ActivatableAbilityGroup group)
        {
            var i = Helpers.Create<IncreaseActivatableAbilityGroupSize>();
            i.Group = group;
            return i;
        }


        static public ReplaceStatForPrerequisites createReplace34BabWithClassLevel(BlueprintCharacterClass character_class)
        {
            var r = Helpers.Create<ReplaceStatForPrerequisites>();
            r.Policy = ReplaceStatForPrerequisites.StatReplacementPolicy.MagusBaseAttack;
            r.CharacterClass = character_class;
            r.OldStat = StatType.BaseAttackBonus;
            return r;
        }


        static public NewMechanics.ContextWeaponDamageDiceReplacement createContextWeaponDamageDiceReplacement(BlueprintParametrizedFeature[] required_parametrized_features,
                                                                                                                 ContextValue value, params DiceFormula[] dice_formulas)
        {
            var c = Helpers.Create<NewMechanics.ContextWeaponDamageDiceReplacement>();
            c.required_parametrized_features = required_parametrized_features;
            c.value = value;
            c.dice_formulas = dice_formulas;
            return c;
        }


        static public NewMechanics.EnchantmentMechanics.BuffRemainingGroupsSizeEnchantPrimaryHandWeapon createBuffRemainingGroupsSizeEnchantPrimaryHandWeapon(ActivatableAbilityGroup group, bool only_non_magical,
                                                                                                                                       bool lock_slot, params BlueprintWeaponEnchantment[] enchants)
        {
            var b = Helpers.Create<NewMechanics.EnchantmentMechanics.BuffRemainingGroupsSizeEnchantPrimaryHandWeapon>();
            b.allowed_types = new BlueprintWeaponType[0];
            b.enchantments = enchants;
            b.lock_slot = lock_slot;
            b.only_non_magical = only_non_magical;
            b.group = group;
            return b;
        }


        static public NewMechanics.EnchantmentMechanics.BuffRemainingGroupSizetEnchantArmor createBuffRemainingGroupSizetEnchantArmor(ActivatableAbilityGroup group, bool only_non_magical,
                                                                                                                                       bool lock_slot, params BlueprintArmorEnchantment[] enchants)
        {
            var b = Helpers.Create<NewMechanics.EnchantmentMechanics.BuffRemainingGroupSizetEnchantArmor>();
            b.enchantments = enchants;
            b.group = group;
            b.lock_slot = lock_slot;
            b.only_non_magical = only_non_magical;
            b.shift_with_current_enchantment = true;
            return b;
        }


        static public WeaponGroupAttackBonus createWeaponGroupAttackBonus(int bonus, ModifierDescriptor descriptor, WeaponFighterGroup group)
        {
            WeaponGroupAttackBonus w = Helpers.Create<WeaponGroupAttackBonus>();
            w.AttackBonus = bonus;
            w.Descriptor = descriptor;
            w.WeaponGroup = group;
            return w;
        }


        static public NewMechanics.RunActionsDependingOnContextValue createRunActionsDependingOnContextValue(ContextValue value, params ActionList[] actions)
        {
            var r = Helpers.Create<NewMechanics.RunActionsDependingOnContextValue>();
            r.value = value;
            r.actions = actions;
            return r;
        }


        static public WeaponDamageAgainstAlignment createWeaponDamageAgainstAlignment(DamageEnergyType energy, DamageAlignment damage_alignment, AlignmentComponent enemy_alignment,
                                                                                        ContextDiceValue value)
        {
            var w = Helpers.Create<WeaponDamageAgainstAlignment>();
            w.DamageType = energy;
            w.WeaponAlignment = damage_alignment;
            w.EnemyAlignment = enemy_alignment;
            w.Value = value;
            return w;
        }


        static public NewMechanics.ContextActionSpendResource createContextActionSpendResource(BlueprintAbilityResource resource, int amount, params BlueprintUnitFact[] cost_reducing_facts)
        {
            var c = Helpers.Create<NewMechanics.ContextActionSpendResource>();
            c.amount = amount;
            c.resource = resource;
            c.cost_reducing_facts = cost_reducing_facts;
            return c;
        }


        static public WeaponEnergyDamageDice weaponEnergyDamageDice(DamageEnergyType energy, DiceFormula dice_formula)
        {
            var w = Helpers.Create<WeaponEnergyDamageDice>();
            w.Element = energy;
            w.EnergyDamageDice = dice_formula;
            return w;
        }


        static public EvasionAgainstDescriptor createEvasionAgainstDescriptor(SpellDescriptor descriptor, SavingThrowType save_type)
        {
            var e = Helpers.Create<EvasionAgainstDescriptor>();
            e.SpellDescriptor = descriptor;
            e.SavingThrow = save_type;
            return e;
        }


        static public NewMechanics.AddEnergyDamageDurability createAddEnergyDamageDurability(DamageEnergyType energy, float scaling_factor)
        {
            var a = Helpers.Create<NewMechanics.AddEnergyDamageDurability>();
            a.scaling = scaling_factor;
            a.Type = energy;
            return a;
        }


        static public NewMechanics.AbilityTargetCompositeOr createAbilityTargetCompositeOr(bool not, params IAbilityTargetChecker[] checkers)
        {
            var c = Helpers.Create<NewMechanics.AbilityTargetCompositeOr>();
            c.ability_checkers = checkers;
            c.Not = not;
            return c;
        }


        static public AbilityTargetHasCondition createAbilityTargetHasCondition(UnitCondition condition, bool not = false)
        {
            var c = Helpers.Create<AbilityTargetHasCondition>();
            c.Condition = condition;
            c.Not = not;
            return c;
        }


        static public bool isPersonalSpell(AbilityData spell)
        {
            if (spell?.Spellbook == null)
            {
                return false;
            }

            return spell.Blueprint.CanTargetSelf && !spell.Blueprint.HasAreaEffect() && !spell.Blueprint.CanTargetPoint;
        }



        static public BlueprintFeature createMonkFeatureUnlock(BlueprintFeature feature, bool no_weapon)
        {
            var feature_unlock = Helpers.CreateFeature(feature.name + "Unlock",
                                                        feature.Name,
                                                        feature.Description,
                                                        "",
                                                        feature.Icon,
                                                        FeatureGroup.None);
            if (no_weapon)
            {
                feature_unlock.AddComponent(Helpers.Create<MonkNoArmorAndMonkWeaponFeatureUnlock>(c => c.NewFact = feature));
            }
            else
            {
                feature_unlock.AddComponent(Helpers.Create<MonkNoArmorFeatureUnlock>(c => c.NewFact = feature));
            }
            return feature_unlock;
        }


        public static void replaceDomainSpell(BlueprintProgression domain_progression, BlueprintAbility new_spell, int level)
        {
            var base_feature = domain_progression.LevelEntries[0].Features[0]; //should be very first feature
            var spell_list = domain_progression.GetComponent<LearnSpellList>().SpellList;
            var spells = spell_list.SpellsByLevel.First(s => s.SpellLevel == level).Spells;
            var old_spell = spells[0];

            var new_description = domain_progression.Description.Replace(old_spell.Name, new_spell.Name);
            var old_description = domain_progression.Description;
            var blueprints = library.GetAllBlueprints().OfType<BlueprintProgression>().Where(f => f.Description == old_description).ToArray();
            foreach (var b in blueprints)
            {
                b.SetDescription(new_description);
            }
            base_feature.SetDescription(base_feature.Description.Replace(old_spell.Name, new_spell.Name));

            spells.Clear();
            spells.Add(new_spell);
        }


        public static RestrictionHasFact createActivatableAbilityRestrictionHasFact(BlueprintUnitFact fact, bool not = false)
        {
            var r = Helpers.Create<RestrictionHasFact>();
            r.Feature = fact;
            r.Not = not;
            return r;
        }


        public static NewMechanics.ContextConditionHasFacts createContextConditionHasFacts(bool all, params BlueprintUnitFact[] facts)
        {
            var c = Helpers.Create<NewMechanics.ContextConditionHasFacts>();
            c.all = all;
            c.Facts = facts;
            return c;
        }

        public static void addFeatureToEnchantment(BlueprintItemEnchantment enchantment, BlueprintFeature feature)
        {
            var c = enchantment.GetComponent<AddUnitFeatureEquipment>();
            if (c == null)
            {
                c = Helpers.Create<Kingmaker.Designers.Mechanics.EquipmentEnchants.AddUnitFeatureEquipment>();
                var enchant_feature = Helpers.CreateFeature(enchantment.name + "Feature",
                                                        "",
                                                        "",
                                                        "",
                                                        null,
                                                        FeatureGroup.None);
                enchant_feature.HideInCharacterSheetAndLevelUp = true;
                c.Feature = enchant_feature;
                enchantment.AddComponent(c);
            }
            c.Feature.AddComponent(Helpers.CreateAddFact(feature));
        }

        public static ContextConditionHasBuffFromCaster createContextConditionHasBuffFromCaster(BlueprintBuff buff, bool not = false)
        {
            var c = Helpers.Create<ContextConditionHasBuffFromCaster>();
            c.Buff = buff;
            c.Not = not;
            return c;
        }


        public static NewMechanics.AddWeaponEnergyDamageDice createAddWeaponEnergyDamageDiceBuff(ContextDiceValue dice_value, DamageEnergyType energy, params AttackType[] attack_types)
        {
            var a = Helpers.Create<NewMechanics.AddWeaponEnergyDamageDice>();
            a.dice_value = dice_value;
            a.Element = energy;
            a.range_types = attack_types;
            return a;
        }


        public static NewMechanics.AddWeaponEnergyDamageDiceIfHasFact createAddWeaponEnergyDamageDiceBuffIfHasFact(ContextDiceValue dice_value, DamageEnergyType energy, BlueprintUnitFact checked_fact, 
                                                                                                                    params AttackType[] attack_types)
        {
            var a = Helpers.Create<NewMechanics.AddWeaponEnergyDamageDiceIfHasFact>();
            a.dice_value = dice_value;
            a.Element = energy;
            a.range_types = attack_types;
            a.checked_fact = checked_fact;
            return a;
        }


        public static void setAsFullRoundAction(BlueprintAbility spell)
        {
            Helpers.SetField(spell, "m_IsFullRoundAction", true);
        }


        public static void addFeaturePrerequisiteOr(BlueprintFeature feature,  BlueprintFeature prerequisite)
        {
            var features_from_list = feature.GetComponent<PrerequisiteFeaturesFromList>();
            if (features_from_list == null)
            {
                features_from_list = Helpers.PrerequisiteFeaturesFromList(prerequisite);
                feature.AddComponent(features_from_list);
            }

            if (!features_from_list.Features.Contains(prerequisite))
            {
                features_from_list.Features = features_from_list.Features.AddToArray(prerequisite);
            }
        }

        public static void addFeaturePrerequisiteAny(BlueprintFeature feature, BlueprintFeature prerequisite)
        {
            var features_prereq = feature.GetComponents<PrerequisiteFeature>().Where(f => f.Group == Prerequisite.GroupType.Any);
            foreach (var fp in features_prereq)
            {
                if (fp.Feature == prerequisite)
                {
                    return;
                }
            }

            feature.AddComponent(Helpers.PrerequisiteFeature(prerequisite, any: true));
        }


        public static AddTargetAttackWithWeaponTrigger createAddTargetAttackWithWeaponTrigger(ActionList action_self = null, ActionList action_attacker = null, WeaponCategory[] categories = null,
                                                                                             bool only_hit = true, bool not_reach = true, bool only_melee = true, bool not = false,
                                                                                             bool wait_for_attack_to_resolve = false, bool only_critical_hit = false)
        {
            var a = Helpers.Create<AddTargetAttackWithWeaponTrigger>();

            a.ActionOnSelf = action_self != null ? action_self : Helpers.CreateActionList();
            a.ActionsOnAttacker = action_attacker != null ? action_attacker : Helpers.CreateActionList();
            a.OnlyHit = only_hit;
            a.NotReach = not_reach;
            a.OnlyMelee = only_melee;
            a.CheckCategory = categories != null;
            a.Categories = categories;
            a.Not = not;
            a.WaitForAttackResolve = wait_for_attack_to_resolve;
            a.CriticalHit = only_critical_hit;
            return a;
        }


        public static ManeuverBonus createManeuverBonus(CombatManeuver maneuver_type, int bonus)
        {
            var m = Helpers.Create<ManeuverBonus>();
            m.Bonus = bonus;
            m.Type = maneuver_type;
            return m;
        }


        public static ManeuverDefenceBonus createManeuverDefenseBonus(CombatManeuver maneuver_type, int bonus)
        {
            var m = Helpers.Create<ManeuverDefenceBonus>();
            m.Bonus = bonus;
            m.Type = maneuver_type;
            return m;
        }


        public static NewMechanics.ContextManeuverDefenceBonus createContextManeuverDefenseBonus(CombatManeuver maneuver_type, ContextValue bonus)
        {
            var m = Helpers.Create<NewMechanics.ContextManeuverDefenceBonus>();
            m.Bonus = bonus;
            m.Type = maneuver_type;
            return m;
        }


        public static NewMechanics.ContextSavingThrowBonusAgainstFact createContextSavingThrowBonusAgainstFact(BlueprintFeature fact, AlignmentComponent alignment, ContextValue value, ModifierDescriptor descriptor)
        {
            var c = Helpers.Create<NewMechanics.ContextSavingThrowBonusAgainstFact>();
            c.CheckedFact = fact;
            c.Alignment = alignment;
            c.Descriptor = descriptor;
            c.Bonus = value;
            return c;
        }


        public static NewMechanics.ContextACBonusAgainstFactOwner createContextACBonusAgainstFactOwner(BlueprintFeature fact, AlignmentComponent alignment, ContextValue value, ModifierDescriptor descriptor)
        {
            var c = Helpers.Create<NewMechanics.ContextACBonusAgainstFactOwner>();
            c.CheckedFact = fact;
            c.Alignment = alignment;
            c.Descriptor = descriptor;
            c.Bonus = value;
            return c;
        }


        public static NewMechanics.AttackBonusOnAttacksOfOpportunity createAttackBonusOnAttacksOfOpportunity(ContextValue value, ModifierDescriptor descriptor)
        {
            var a = Helpers.Create<NewMechanics.AttackBonusOnAttacksOfOpportunity>();
            a.Value = value;
            a.Descriptor = descriptor;
            return a;
        }


        public static ACBonusAgainstAttacks createACBonussOnAttacksOfOpportunity(ContextValue value, ModifierDescriptor descriptor)
        {
            var a = Helpers.Create<ACBonusAgainstAttacks>();
            a.Value = value;
            a.Descriptor = descriptor;
            a.OnlyAttacksOfOpportunity = true;
            return a;
        }


        public static PrerequisiteCasterTypeSpellLevel createPrerequisiteCasterTypeSpellLevel(bool is_arcane, int spell_level, bool any = false)
        {
            var p = Helpers.Create<PrerequisiteCasterTypeSpellLevel>();
            p.IsArcane = is_arcane;
            p.RequiredSpellLevel = spell_level;
            p.Group = any ? Prerequisite.GroupType.Any : Prerequisite.GroupType.All;
            return p;
        }


        public static NewMechanics.ReduceDRForFactOwner createReduceDRForFactOwner(int reduce_value, BlueprintFeature fact, params AttackType[] attack_types)
        {
            var r = Helpers.Create<NewMechanics.ReduceDRForFactOwner>();
            r.Reduction = reduce_value;
            r.CheckedFact = fact;
            r.attack_types = attack_types;
            return r;
        }


        

        public static void addSpellbooksToSpellSelection(string name, int spell_level,
                                                        BlueprintFeatureSelection spellbook_selection, bool divine = true, bool arcane = true, bool alchemist = true)
        {
            var wizard = library.Get<BlueprintCharacterClass>("ba34257984f4c41408ce1dc2004e342e");
            var thassilonian_shools = library.Get<BlueprintFeatureSelection>("f431178ec0e2b4946a34ab504bb46285").AllFeatures;
            var thassilonian_specialist = library.Get<BlueprintArchetype>("55a8ce15e30d71547a44c69bb2e8a84f");

            foreach (var c in Helpers.classes)
            {
                var alternative_spellbook_archetypes = c.Archetypes.Where(a => a.ReplaceSpellbook != null || a.RemoveSpellbook).ToArray();
                if (c == wizard)
                {
                    alternative_spellbook_archetypes =  alternative_spellbook_archetypes.AddToArray(thassilonian_specialist);
                }
                List<BlueprintComponent> components = new List<BlueprintComponent>();
                components.Add(Common.createPrerequisiteClassSpellLevel(c, spell_level));

                if (c.Spellbook.checkSpellbook(divine, arcane, alchemist))
                {
                    foreach (var a in alternative_spellbook_archetypes)
                    {
                        components.Add(Common.prerequisiteNoArchetype(c, a));
                    }
                    Common.addReplaceSpellbook(spellbook_selection, c.Spellbook, name + c.name + "SpellbookFeature",
                                               components.ToArray());
                }

                foreach (var a in alternative_spellbook_archetypes)
                {
                    if (a.ReplaceSpellbook.checkSpellbook(divine, arcane, alchemist))
                    {
                        Common.addReplaceSpellbook(spellbook_selection, a.ReplaceSpellbook, name + a.name + "SpellbookFeature", a.Name,
                                                Common.createPrerequisiteArchetypeLevel(c, a, 1),
                                                components[0]);
                    }
                }
            }

            if (!arcane)
            {
                return;
            }


            foreach (var s in thassilonian_shools)
            {
                var spellbook = (s as BlueprintFeatureReplaceSpellbook).Spellbook;
                Common.addReplaceSpellbook(spellbook_selection, spellbook, name + spellbook.name + "Feature",
                                           "Thassilonian Specialist: " + s.Name,
                                            Common.createPrerequisiteClassSpellLevel(wizard, spell_level),
                                            Common.createPrerequisiteArchetypeLevel(wizard, thassilonian_specialist, 1));
            }
        }


        static public NewMechanics.ContextActionAttack createContextActionAttack(ActionList action_on_hit = null, ActionList action_on_miss = null)
        {
            var c = Helpers.Create<NewMechanics.ContextActionAttack>();
            c.action_on_success = action_on_hit;
            c.action_on_miss = action_on_miss;
            return c;
        }


        static public NewMechanics.ContextActionForceAttack createContextActionForceAttack()
        {
            return Helpers.Create<NewMechanics.ContextActionForceAttack>(); ;
        }


        public static BlueprintAbility createVariantWrapper(string name, string guid, params BlueprintAbility[] variants)
        {
            var wrapper = library.CopyAndAdd<BlueprintAbility>(variants[0].AssetGuid, name, guid);

            //wrapper.SetDescription("");
            List<BlueprintComponent> components = new List<BlueprintComponent>();
            components.Add(Helpers.CreateAbilityVariants(wrapper, variants));
            wrapper.ComponentsArray = components.ToArray();

            return wrapper;
        }


        public static AbilityExecuteActionOnCast createAbilityExecuteActionOnCast(ActionList actions, ConditionsChecker  condition = null)
        {
            var a = Helpers.Create<AbilityExecuteActionOnCast>();
            if (condition != null)
            {
                a.Conditions = condition;
            }
            else
            {
                a.Conditions = new ConditionsChecker();
            }
            a.Actions = actions;
            return a;
        }


        public static GameAction[] addMatchingAction<T>(GameAction[] action_list, params GameAction[] actions_to_add) where T : GameAction
        {
            //we assume that only possible actions are actual actions, conditionals, ContextActionSavingThrow or ContextActionConditionalSaved
            var actions = action_list.ToList();
            int num_actions = actions.Count();
            for (int i = 0; i < num_actions; i++)
            {
                if (actions[i] == null)
                {
                    continue;
                }
                else if (actions[i] is T)
                {
                    actions.AddRange(actions_to_add);
                }

                if (actions[i] is Conditional)
                {
                    actions[i] = actions[i].CreateCopy();
                    (actions[i] as Conditional).IfTrue = Helpers.CreateActionList(addMatchingAction<T>((actions[i] as Conditional).IfTrue.Actions, actions_to_add));
                    (actions[i] as Conditional).IfFalse = Helpers.CreateActionList(addMatchingAction<T>((actions[i] as Conditional).IfFalse.Actions, actions_to_add));
                }
                else if (actions[i] is ContextActionConditionalSaved)
                {
                    actions[i] = actions[i].CreateCopy();
                    (actions[i] as ContextActionConditionalSaved).Succeed = Helpers.CreateActionList(addMatchingAction<T>((actions[i] as ContextActionConditionalSaved).Succeed.Actions, actions_to_add));
                    (actions[i] as ContextActionConditionalSaved).Failed = Helpers.CreateActionList(addMatchingAction<T>((actions[i] as ContextActionConditionalSaved).Failed.Actions, actions_to_add));
                }
                else if (actions[i] is ContextActionSavingThrow)
                {
                    actions[i] = actions[i].CreateCopy();
                    (actions[i] as ContextActionSavingThrow).Actions = Helpers.CreateActionList(addMatchingAction<T>((actions[i] as ContextActionSavingThrow).Actions.Actions, actions_to_add));
                }
            }

            return actions.ToArray();
        }


        public static GameAction[] changeAction<T>(GameAction[] action_list, Action<T> change) where T : GameAction
        {
            //we assume that only possible actions are actual actions, conditionals, ContextActionSavingThrow or ContextActionConditionalSaved
            var actions = action_list.ToList();
            int num_actions = actions.Count();
            for (int i = 0; i < num_actions; i++)
            {
                if (actions[i] == null)
                {
                    continue;
                }
                else if (actions[i] is T)
                {
                    actions[i] = actions[i].CreateCopy();
                    change(actions[i] as T);
                }

                if (actions[i] is Conditional)
                {
                    actions[i] = actions[i].CreateCopy();
                    (actions[i] as Conditional).IfTrue = Helpers.CreateActionList(changeAction<T>((actions[i] as Conditional).IfTrue.Actions, change));
                    (actions[i] as Conditional).IfFalse = Helpers.CreateActionList(changeAction<T>((actions[i] as Conditional).IfFalse.Actions, change));
                }
                else if (actions[i] is ContextActionConditionalSaved)
                {
                    actions[i] = actions[i].CreateCopy();
                    (actions[i] as ContextActionConditionalSaved).Succeed = Helpers.CreateActionList(changeAction<T>((actions[i] as ContextActionConditionalSaved).Succeed.Actions, change));
                    (actions[i] as ContextActionConditionalSaved).Failed = Helpers.CreateActionList(changeAction<T>((actions[i] as ContextActionConditionalSaved).Failed.Actions, change));
                }
                else if (actions[i] is ContextActionSavingThrow)
                {
                    actions[i] = actions[i].CreateCopy();
                    (actions[i] as ContextActionSavingThrow).Actions = Helpers.CreateActionList(changeAction<T>((actions[i] as ContextActionSavingThrow).Actions.Actions, change));
                }
            }

            return actions.ToArray();
        }


        static public ContextActionOnContextCaster createContextActionOnContextCaster(params GameAction[] actions)
        {
            var c = Helpers.Create<ContextActionOnContextCaster>();
            c.Actions = Helpers.CreateActionList(actions);
            return c;
        }


        public static WeaponVisualParameters replaceProjectileInWeaponVisualParameters(WeaponVisualParameters wp, BlueprintProjectile projectile)
        {
            WeaponVisualParameters new_wp = wp.CloneObject();

            Helpers.SetField(new_wp, "m_Projectiles", new BlueprintProjectile[] { projectile });
            return new_wp;

        }


        public static ContextActionSkillCheck createContextActionSkillCheck(StatType skill, ActionList success = null, ActionList failure = null, ContextValue custom_dc = null)
        {
            var c = Helpers.Create<ContextActionSkillCheck>();
            c.UseCustomDC = custom_dc != null;
            c.CustomDC = custom_dc;
            c.Success = success == null ? Helpers.CreateActionList() : success;
            c.Failure = failure == null ? Helpers.CreateActionList() : failure;
            c.Stat = skill;
            return c;

        }

   
        public static ContextActionSpawnAreaEffect createContextActionSpawnAreaEffect(BlueprintAbilityAreaEffect area_effect, ContextDurationValue duration)
        {
            return Helpers.Create<ContextActionSpawnAreaEffect>(c => { c.AreaEffect = area_effect; c.DurationValue = duration; });
        }


        public static NewMechanics.ContextActionSpawnAreaEffectMultiple createContextActionSpawnAreaEffectMultiple(BlueprintAbilityAreaEffect area_effect, ContextDurationValue duration, params Vector2[] points)
        {
            return Helpers.Create<NewMechanics.ContextActionSpawnAreaEffectMultiple>(c => { c.AreaEffect = area_effect; c.DurationValue = duration; c.points_around_target = points; });
        }


        public static void addConditionalDCIncrease(ContextActionSavingThrow context_action_savingthrow, ConditionsChecker condition, ContextValue value)
        {
            var data = Activator.CreateInstance(ContextActionSavingThrow_ConditionalDCIncrease);
            Helpers.SetField(data, "Condition", condition);
            Helpers.SetField(data, "Value", value);

            var data_array = Array.CreateInstance(ContextActionSavingThrow_ConditionalDCIncrease, 1);
            data_array.SetValue(data, 0);
            Helpers.SetField(context_action_savingthrow, "m_ConditionalDCIncrease", data_array);
        }


        public static PrefabLink createPrefabLink(string asset_id)
        {
            var link = new PrefabLink();
            link.AssetId = asset_id;
            return link;
        }


        public static AbilitySpawnFx createAbilitySpawnFx(string asset_id, AbilitySpawnFxAnchor position_anchor = AbilitySpawnFxAnchor.None, 
                                                                           AbilitySpawnFxAnchor orientation_anchor = AbilitySpawnFxAnchor.None,
                                                                           AbilitySpawnFxAnchor anchor = AbilitySpawnFxAnchor.None)
        {
            var a = Helpers.Create<AbilitySpawnFx>();
            a.PrefabLink = createPrefabLink(asset_id);
            a.PositionAnchor = position_anchor;
            a.OrientationAnchor = orientation_anchor;
            a.Anchor = anchor;
            return a;
        }


        static public AbilityDeliverProjectile createAbilityDeliverProjectile(AbilityProjectileType type, BlueprintProjectile projectile, Feet length, Feet width)
        {
            var a = Helpers.Create<AbilityDeliverProjectile>();
            a.Type = type;
            a.Projectiles = new BlueprintProjectile[] { projectile };
            a.Length = length;
            a.LineWidth = width;

            return a;
        }


        static public NewMechanics.AddStackingStatBonusToModifierFromFact createAddStackingStatBonusToModifierFromFact(ContextDiceValue dice_value, StatType stat, ModifierDescriptor descriptor,
                                                                                                                       BlueprintUnitFact fact, int max_value = 0, bool set = false)
        {
            var a = Helpers.Create<NewMechanics.AddStackingStatBonusToModifierFromFact>();
            a.stat = stat;
            a.dice_value = dice_value;
            a.Descriptor = descriptor;
            a.storing_fact = fact;
            a.max_value = max_value;
            a.set = set;

            return a;
        }


        static public NewMechanics.AddStackingStatBonusToModifierFromFact createAddStackingStatBonusToModifierFromFact(ContextValue value, StatType stat, ModifierDescriptor descriptor,
                                                                                                               BlueprintUnitFact fact, int max_value = 0, bool set = false)
        {
            var a = Helpers.Create<NewMechanics.AddStackingStatBonusToModifierFromFact>();
            a.stat = stat;
            a.dice_value = Helpers.CreateContextDiceValue(DiceType.Zero, 0, value);
            a.Descriptor = descriptor;
            a.storing_fact = fact;
            a.max_value = max_value;
            a.set = set;

            return a;
        }


        public static BlueprintAbility replaceCureInflictSpellParameters(BlueprintAbility spell, string new_name, string new_display_name, string new_description, AbilityType new_type,
                                                                     ContextRankConfig new_context_rank_config, ContextDiceValue new_dice, bool spell_resistance,
                                                                     string guid_ability, string guid_primary, string guid_buff, string guid_secondary)
        {

            var new_ability = library.CopyAndAdd<BlueprintAbility>(spell, new_name, guid_ability);
            new_ability.Type = new_type;
            new_ability.SpellResistance = spell_resistance;
            new_ability.SetNameDescription(new_display_name, new_description);
            
            var primary_ability = library.CopyAndAdd<BlueprintAbility>(spell.StickyTouch.TouchDeliveryAbility, new_name + "Cast", guid_primary);
            new_ability.ReplaceComponent<AbilityEffectStickyTouch>(s => s.TouchDeliveryAbility = primary_ability);
            primary_ability.SetNameDescription(new_display_name, new_description);
            primary_ability.Type = new_type;
            primary_ability.ReplaceComponent<ContextRankConfig>(new_context_rank_config);
            Common.replaceDamageOrHealDice(primary_ability, new_dice, true, true);
            BlueprintAbility secondary_spell = null;

            var new_actions = Common.changeAction<ContextActionCastSpell>(primary_ability.GetComponent<AbilityEffectRunAction>().Actions.Actions,
                                                                            a =>
                                                                            {
                                                                                secondary_spell = library.CopyAndAdd<BlueprintAbility>(a.Spell, new_name + "Secondary", guid_secondary);
                                                                                secondary_spell.Type = new_type;
                                                                                secondary_spell.ReplaceComponent<ContextRankConfig>(new_context_rank_config);
                                                                                Common.replaceDamageOrHealDice(secondary_spell, new_dice, true, true);
                                                                                a.Spell = secondary_spell;
                                                                                secondary_spell.SetNameDescription(new_display_name, new_description);
                                                                            }
                                                                            );

            new_actions = Common.changeAction<ContextActionApplyBuff>(new_actions,
                                                                        a =>
                                                                        {
                                                                            var buff = library.CopyAndAdd<BlueprintBuff>(a.Buff, new_name + "CasterBuff", guid_buff);
                                                                            buff.GetComponent<ReplaceAbilityParamsWithContext>().Ability = secondary_spell;
                                                                            a.Buff = buff;
                                                                        }
                                                                        );
            primary_ability.ReplaceComponent<AbilityEffectRunAction>(Helpers.CreateRunActions(new_actions));
            new_ability.StickyTouch.TouchDeliveryAbility = primary_ability;
            new_ability.SpellResistance = spell_resistance;
            primary_ability.SpellResistance = spell_resistance;
            secondary_spell.SpellResistance = spell_resistance;

            return new_ability;
        }


        public static void replaceDamageOrHealDice(BlueprintAbility ability, ContextDiceValue new_dice, bool heal, bool damage)
        {
            if (new_dice == null)
            {
                return;
            }
            if (heal)
            {
                ability.ReplaceComponent<AbilityEffectRunAction>(a => a.Actions = Helpers.CreateActionList(Common.changeAction<ContextActionHealTarget>(a.Actions.Actions,
                                                                                                                                                        c =>
                                                                                                                                                        {
                                                                                                                                                            c.Value = new_dice;
                                                                                                                                                        })
                                                                                                          )
                                                                );
            }
            if (damage)
            {
                ability.ReplaceComponent<AbilityEffectRunAction>(a => a.Actions = Helpers.CreateActionList(Common.changeAction<ContextActionDealDamage>(a.Actions.Actions,
                                                                                                                                                        c =>
                                                                                                                                                        {
                                                                                                                                                            c.Value = new_dice;
                                                                                                                                                        })
                                                                                                          )
                                                                );
            }
        }



        public static BlueprintActivatableAbility CreateMetamagicAbility(BlueprintFeature feat, String name, String display_name, Metamagic metamagic, SpellDescriptor descriptor, 
                                                                  String buff_id, String ability_id)
        {
            var buff = Helpers.CreateBuff($"{feat.name}{name}Buff", display_name, feat.Description,
                buff_id, feat.Icon, null,
                Helpers.Create<AutoMetamagic>(a => { a.Metamagic = metamagic; a.Descriptor = descriptor; }));

            var ability = Helpers.CreateActivatableAbility($"{feat.name}{name}ToggleAbility", display_name, feat.Description,
                ability_id, feat.Icon, buff, AbilityActivationType.Immediately,
                CommandType.Free, null);
            ability.DeactivateImmediately = true;
            return ability;
        }


        static public Kingmaker.Blueprints.Classes.Spells.BlueprintSpellList createSpellList(string name, string guid)
        {
            var spell_list = Helpers.Create<Kingmaker.Blueprints.Classes.Spells.BlueprintSpellList>();
            spell_list.name = name;
            library.AddAsset(spell_list, guid);
            spell_list.SpellsByLevel = new SpellLevelList[10];
            for (int i = 0; i < spell_list.SpellsByLevel.Length; i++)
            {
                spell_list.SpellsByLevel[i] = new SpellLevelList(i);
            }
            return spell_list;
        }


        static public OutgoingConcealementMechanics.AddOutgoingConcealment createOutgoingConcelement(AddConcealment self_concealement)
        {
            var o = Helpers.Create<OutgoingConcealementMechanics.AddOutgoingConcealment>();
            o.Descriptor = self_concealement.Descriptor;
            o.Concealment = self_concealement.Concealment;
            o.CheckDistance = self_concealement.CheckDistance;
            o.CheckWeaponRangeType = self_concealement.CheckWeaponRangeType;
            o.DistanceGreater = self_concealement.DistanceGreater;
            o.OnlyForAttacks = self_concealement.OnlyForAttacks;
            o.RangeType = self_concealement.RangeType;
            return o;
        }


        static public BlueprintAbility[] getSpellsFromSpellList(BlueprintSpellList spell_list)
        {
            List<BlueprintAbility> list = new List<BlueprintAbility>();
            foreach (var sl in spell_list.SpellsByLevel)
            {
                list.AddRange(sl.Spells);
            }

            return list.ToArray();
        }

        public static BlueprintAbility createAttackAbility(string name, string display_name, string description, string guid, UnityEngine.Sprite icon, UnitCommand.CommandType action_type, params BlueprintComponent[] components)
        {

            var ability = Helpers.CreateAbility(name,
                                               display_name,
                                               description,
                                               guid,
                                               icon,
                                               AbilityType.Special,
                                               action_type,
                                               AbilityRange.Weapon,
                                               "",
                                               "",
                                               Helpers.CreateRunActions(Common.createContextActionAttack(null, null)),
                                               Helpers.Create<NewMechanics.AttackAnimation>()
                                               );

            ability.AddComponents(components);
            ability.NeedEquipWeapons = true;
            ability.setMiscAbilityParametersSingleTargetRangedHarmful(works_on_allies: true);

            return ability;                                   
        }


        public static WeaponCategory[] getRangedWeaponCategories()
        {
            return new WeaponCategory[] {WeaponCategory.Longbow, WeaponCategory.Shortbow, WeaponCategory.HandCrossbow, WeaponCategory.HeavyCrossbow, WeaponCategory.LightCrossbow, WeaponCategory.Javelin,
                                        WeaponCategory.ThrowingAxe, WeaponCategory.Ray, WeaponCategory.Sling, WeaponCategory.SlingStaff};
        }


    }
}
