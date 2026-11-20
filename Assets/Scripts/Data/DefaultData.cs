using System.Collections.Generic;
using UnityEngine;

namespace RpgBattle
{
    /// Builds the prototype's content from the brief as runtime ScriptableObjects.
    /// Assign a BattleConfig asset on BattleBootstrap to replace all of this without touching code.
    public static class DefaultData
    {
        static EffectEntry Fx(EffectType type, float value = 0f, float chance = 1f, StatusType status = StatusType.None,
            StatKey stat = StatKey.STR, int duration = 3, bool onCaster = false)
            => new EffectEntry { type = type, value = value, chance = chance, status = status, stat = stat, duration = duration, onCaster = onCaster };

        static AbilityData Ability(string name, string desc, ActionCategory cat, DamageType dt, TargetShape shape, TargetSide side,
            float recovery, Presentation pres, Color col, params EffectEntry[] fx)
        {
            var a = ScriptableObject.CreateInstance<AbilityData>();
            a.name = name; a.displayName = name; a.description = desc;
            a.category = cat; a.damageType = dt; a.shape = shape; a.side = side;
            a.recoveryUnits = recovery; a.presentation = pres; a.vfxColor = col;
            a.effects = new List<EffectEntry>(fx);
            return a;
        }

        static UnitData Enemy(string name, ModelStyle style, bool ranged, AbilityData atk, int hp, int str, int def, int ats, int adf,
            int spd, int mov, Color tint, float scale)
        {
            var u = ScriptableObject.CreateInstance<UnitData>();
            u.name = name; u.displayName = name; u.team = Team.Enemy; u.modelStyle = style; u.ranged = ranged;
            u.maxHp = hp; u.maxEp = 0; u.startSp = 0; u.maxSp = 0;
            u.str = str; u.def = def; u.ats = ats; u.adf = adf; u.spd = spd; u.mov = mov;
            u.maxDamagePerHit = 30; u.basicAttack = atk; u.tint = tint; u.scale = scale;
            return u;
        }

        static AbilityData EnemyAttack(string name, DamageType dt, int range, Presentation pres, Color col, float recovery)
        {
            var a = Ability(name, "Enemy attack.", ActionCategory.Attack, dt, TargetShape.Single, TargetSide.Enemy, recovery, pres, col, Fx(EffectType.Damage, 1f));
            a.castRange = range; a.animKey = "Attack"; a.hitDelay = 0.35f; a.totalDuration = 0.8f;
            a.sfxKey = pres == Presentation.MeleeSlash ? "sword_swing" : "magic_shot";
            return a;
        }

        public static BattleConfig CreateConfig()
        {
            var cfg = ScriptableObject.CreateInstance<BattleConfig>();
            cfg.name = "DefaultBattleConfig";
            cfg.blockedCells = new[]
            {
                new Vector2Int(9, 4), new Vector2Int(9, 11), new Vector2Int(13, 7), new Vector2Int(13, 8),
                new Vector2Int(16, 3), new Vector2Int(16, 12)
            };
            cfg.statuses = new[]
            {
                new StatusDef { type = StatusType.Blind, label = "Blind", duration = 2, tickPercent = 0f, color = new Color(0.7f, 0.7f, 0.75f) },
                new StatusDef { type = StatusType.Freeze, label = "Freeze", duration = 3, tickPercent = 0.10f, color = new Color(0.5f, 0.85f, 1f) },
                new StatusDef { type = StatusType.Poison, label = "Poison", duration = 3, tickPercent = 0.20f, color = new Color(0.6f, 0.9f, 0.3f) },
            };

            var white = new Color(0.9f, 0.95f, 1f);
            var gold = new Color(1f, 0.85f, 0.4f);

            // ---------- Fang ----------
            var fangAtk = Ability("Attack", "Approach the enemy and strike with his sword.", ActionCategory.Attack, DamageType.Physical,
                TargetShape.Single, TargetSide.Enemy, 50, Presentation.MeleeSlash, white, Fx(EffectType.Damage, 1f));
            fangAtk.castRange = 1; fangAtk.sfxKey = "sword_swing"; fangAtk.hitDelay = 0.5f; fangAtk.totalDuration = 0.95f;

            var flash = Ability("Crescent Flash", "A powerful piercing sword wave. Line S+. 40% Blind.", ActionCategory.Special, DamageType.Physical,
                TargetShape.Line, TargetSide.Enemy, 110, Presentation.GroundSlam, new Color(0.7f, 0.9f, 1f),
                Fx(EffectType.Damage, 1.8f), Fx(EffectType.ApplyStatus, 0f, 0.4f, StatusType.Blind));
            flash.spCost = 30; flash.castRange = 8; flash.lineLength = 8; flash.lineWidth = 3f;
            flash.animKey = "Special"; flash.sfxKey = "special"; flash.hitDelay = 1.0f; flash.totalDuration = 1.7f;

            var gale = Ability("Crescent Gale", "High-speed slashes across a large area. Area L.", ActionCategory.Special, DamageType.Physical,
                TargetShape.Circle, TargetSide.Enemy, 120, Presentation.BlinkStrike, new Color(0.6f, 1f, 0.9f),
                Fx(EffectType.Damage, 1.5f));
            gale.spCost = 60; gale.castRange = 4; gale.areaCenter = AreaCenter.Cursor; gale.radius = 1; gale.radiusAddsMov = true;
            gale.animKey = "Hold"; gale.sfxKey = "special"; gale.hitDelay = 0.2f; gale.totalDuration = 0.6f;

            var dawn = Ability("Valiant Dawn", "Eight Leaves technique that drives back the darkness. Resets enemy ATB and inflicts SPD Down.",
                ActionCategory.Special, DamageType.Physical, TargetShape.All, TargetSide.Enemy, 160, Presentation.ValiantDawn, gold,
                Fx(EffectType.Damage, 3f), Fx(EffectType.ResetAtb), Fx(EffectType.ApplyBuff, 0.7f, 1f, StatusType.None, StatKey.SPD, 3));
            dawn.spCost = 100; dawn.hidesUi = true; dawn.animKey = "Ultimate"; dawn.sfxKey = "ultimate"; dawn.hitDelay = 0.2f; dawn.totalDuration = 1.0f;

            var earthGuard = Ability("Earth Guard", "Earth shield that nullifies one incoming attack.", ActionCategory.Art, DamageType.None,
                TargetShape.Single, TargetSide.Ally, 80, Presentation.Buff, new Color(0.8f, 0.6f, 0.3f), Fx(EffectType.GrantShield, 1f));
            earthGuard.epCost = 20; earthGuard.castRange = 99; earthGuard.allowMovement = false;
            earthGuard.animKey = "Cast"; earthGuard.sfxKey = "art_cast"; earthGuard.hitDelay = 0.4f; earthGuard.totalDuration = 0.9f;

            var crest = Ability("Crest", "Earth protection. DEF +25% for 3 turns. Small area.", ActionCategory.Art, DamageType.None,
                TargetShape.Circle, TargetSide.Ally, 80, Presentation.Buff, new Color(0.8f, 0.6f, 0.3f),
                Fx(EffectType.ApplyBuff, 1.25f, 1f, StatusType.None, StatKey.DEF, 3));
            crest.epCost = 10; crest.castRange = 8; crest.allowMovement = false; crest.areaCenter = AreaCenter.Cursor; crest.radius = 2;
            crest.animKey = "Cast"; crest.sfxKey = "art_cast"; crest.hitDelay = 0.4f; crest.totalDuration = 0.9f;

            var fang = ScriptableObject.CreateInstance<UnitData>();
            fang.name = "Fang"; fang.displayName = "Fang"; fang.team = Team.Player; fang.modelStyle = ModelStyle.Fang;
            fang.maxHp = 500; fang.maxEp = 50; fang.startSp = 100; fang.maxSp = 150;
            fang.str = 120; fang.def = 60; fang.ats = 40; fang.adf = 35; fang.spd = 14; fang.mov = 4;
            fang.basicAttack = fangAtk; fang.arts = new List<AbilityData> { earthGuard, crest };
            fang.specials = new List<AbilityData> { flash, gale, dawn };
            fang.modelResourcePath = "Characters/Fang/fang"; fang.targetHeight = 1.9f; fang.portraitHeightFraction = 0.9f; fang.portraitSize = 0.32f;
            fang.textureRules = new List<TextureRule>
            {
                new TextureRule { materialNameContains = "sword", texturePath = "Characters/Fang/fang_weapon_albedo" },
                new TextureRule { materialNameContains = "", texturePath = "Characters/Fang/fang_body_albedo" },
            };
            fang.rigBones = new RigBones { upperArmR = "j_arm_r", lowerArmR = "j_forearm_r", handR = "j_hand_r", upperArmL = "j_arm_l", lowerArmL = "j_forearm_l", handL = "j_hand_l" };
            fang.clips = new List<ClipBinding>
            {
                new ClipBinding { key = "Idle", clipName = "dvl_mdl_guts_idle_00", loop = true, fade = 0.2f },
                new ClipBinding { key = "Attack", clipName = "dvl_mdl_guts_attack", speed = 1.6f, playLength = 1.5f },
                new ClipBinding { key = "Special", clipName = "dvl_mdl_guts_skill", speed = 1.2f, playLength = 2.0f },
                new ClipBinding { key = "Hold", clipName = "dvl_mdl_guts_attack", freezeAt = 0.9f },
                new ClipBinding { key = "Ultimate", clipName = "dvl_mdl_guts_spskill_00", speed = 1f, playLength = 3.17f },
                new ClipBinding { key = "Cast", pose = PoseKind.HandToChest, playLength = 1.0f },
                new ClipBinding { key = "Hurt", clipName = "dvl_mdl_guts_damage", speed = 1.5f, playLength = 1.2f },
                new ClipBinding { key = "Death", clipName = "dvl_mdl_guts_dead", holdLast = true },
            };

            // ---------- Xiangling ----------
            var xAtk = Ability("Attack", "Fire a magical projectile from her staff.", ActionCategory.Attack, DamageType.Magical,
                TargetShape.Single, TargetSide.Enemy, 50, Presentation.Projectile, gold, Fx(EffectType.Damage, 1f));
            xAtk.castRange = 4; xAtk.animKey = "Attack"; xAtk.sfxKey = "magic_shot"; xAtk.hitDelay = 0.4f; xAtk.totalDuration = 0.8f;

            var blessing = Ability("Argent Blessing", "Converts magical power into healing. Restores 10% HP, +40 SP. Area M.", ActionCategory.Special,
                DamageType.None, TargetShape.Circle, TargetSide.Ally, 100, Presentation.Heal, new Color(0.8f, 1f, 0.9f),
                Fx(EffectType.HealMaxHpPercent, 0.10f), Fx(EffectType.GrantSp, 40f, 1f, StatusType.None, StatKey.STR, 0, true));
            blessing.spCost = 20; blessing.areaCenter = AreaCenter.Caster; blessing.radius = 3; blessing.allowMovement = false; blessing.castRange = 0;
            blessing.animKey = "Cast"; blessing.sfxKey = "heal"; blessing.hitDelay = 0.5f; blessing.totalDuration = 1.2f;

            var spear = Ability("Argent Spear", "Penetrating arrow of lunar light. Line S. 40% Freeze, 40% Blind.", ActionCategory.Special,
                DamageType.Magical, TargetShape.Line, TargetSide.Enemy, 100, Presentation.PiercingBeam, new Color(0.85f, 0.95f, 1f),
                Fx(EffectType.Damage, 2f), Fx(EffectType.ApplyStatus, 0f, 0.4f, StatusType.Freeze), Fx(EffectType.ApplyStatus, 0f, 0.4f, StatusType.Blind));
            spear.spCost = 40; spear.castRange = 9; spear.lineLength = 9; spear.lineWidth = 1f;
            spear.animKey = "Special"; spear.sfxKey = "special"; spear.hitDelay = 0.5f; spear.totalDuration = 1.1f;

            var fate = Ability("Fate's Card", "A tarot card that may inflict Poison, Freeze, Blind or Instant Kill (25% each enemy).",
                ActionCategory.Special, DamageType.Magical, TargetShape.Circle, TargetSide.Enemy, 160, Presentation.FatesCard, new Color(0.75f, 0.45f, 1f),
                Fx(EffectType.RandomStatus, 0f, 0.75f));
            fate.spCost = 100; fate.castRange = 6; fate.radius = 4; fate.areaCenter = AreaCenter.Cursor; fate.hidesUi = true;
            fate.animKey = "Ultimate"; fate.sfxKey = "ultimate"; fate.hitDelay = 0.2f; fate.totalDuration = 1.0f;

            var heal = Ability("Heighten Heal", "Restore 30% max HP and cure status effects.", ActionCategory.Art, DamageType.None,
                TargetShape.Single, TargetSide.Ally, 80, Presentation.Heal, new Color(0.6f, 1f, 0.7f),
                Fx(EffectType.HealMaxHpPercent, 0.30f), Fx(EffectType.CureStatuses));
            heal.epCost = 30; heal.castRange = 99; heal.allowMovement = false;
            heal.animKey = "Cast"; heal.sfxKey = "art_cast"; heal.hitDelay = 0.4f; heal.totalDuration = 0.9f;

            var time = Ability("Heighten Timeframe", "SPD +30% for 3 turns. +20 SP. Area S.", ActionCategory.Art, DamageType.None,
                TargetShape.Circle, TargetSide.Ally, 80, Presentation.Buff, new Color(0.5f, 0.8f, 1f),
                Fx(EffectType.ApplyBuff, 1.3f, 1f, StatusType.None, StatKey.SPD, 3), Fx(EffectType.GrantSp, 20f, 1f, StatusType.None, StatKey.STR, 0, true));
            time.epCost = 30; time.areaCenter = AreaCenter.Caster; time.radius = 3; time.allowMovement = false; time.castRange = 0;
            time.animKey = "Cast"; time.sfxKey = "art_cast"; time.hitDelay = 0.4f; time.totalDuration = 0.9f;

            var force = Ability("Heighten Force", "STR +25%, DEF +25% for 3 turns. +20 SP.", ActionCategory.Art, DamageType.None,
                TargetShape.Single, TargetSide.Ally, 80, Presentation.Buff, new Color(1f, 0.6f, 0.4f),
                Fx(EffectType.ApplyBuff, 1.25f, 1f, StatusType.None, StatKey.STR, 3), Fx(EffectType.ApplyBuff, 1.25f, 1f, StatusType.None, StatKey.DEF, 3),
                Fx(EffectType.GrantSp, 20f, 1f, StatusType.None, StatKey.STR, 0, true));
            force.epCost = 30; force.castRange = 99; force.allowMovement = false;
            force.animKey = "Cast"; force.sfxKey = "art_cast"; force.hitDelay = 0.4f; force.totalDuration = 0.9f;

            var xiang = ScriptableObject.CreateInstance<UnitData>();
            xiang.name = "Xiangling"; xiang.displayName = "Xiangling"; xiang.team = Team.Player; xiang.modelStyle = ModelStyle.Xiangling;
            xiang.maxHp = 400; xiang.maxEp = 150; xiang.startSp = 100; xiang.maxSp = 150;
            xiang.str = 35; xiang.def = 30; xiang.ats = 130; xiang.adf = 70; xiang.spd = 12; xiang.mov = 4;
            xiang.basicAttack = xAtk; xiang.arts = new List<AbilityData> { heal, time, force };
            xiang.specials = new List<AbilityData> { blessing, spear, fate };
            xiang.modelResourcePath = "Characters/Xiangling/xiangling"; xiang.targetHeight = 2.0f; xiang.hoverHeight = 0.35f; xiang.hoverBob = 0.06f;
            xiang.portraitHeightFraction = 0.82f; xiang.portraitSize = 0.38f;
            xiang.rigBones = new RigBones { upperArmR = "upper_arm.R", lowerArmR = "lower_arm.R", handR = "hand.R", upperArmL = "upper_arm.L", lowerArmL = "lower_arm.L", handL = "hand.L" };
            xiang.clips = new List<ClipBinding>
            {
                new ClipBinding { key = "Idle", clipName = "*", loop = true, fade = 0.2f },
                new ClipBinding { key = "Attack", pose = PoseKind.PointForward, playLength = 0.9f },
                new ClipBinding { key = "Cast", pose = PoseKind.ArmsRaised, playLength = 1.0f },
                new ClipBinding { key = "Special", pose = PoseKind.PointForward, playLength = 1.3f },
                new ClipBinding { key = "Ultimate", pose = PoseKind.ArmsSpread, playLength = 3.2f, lift = 0.6f },
            };

            // ---------- Links ----------
            LinkAbilityData Link(LinkType t, string n, float chance, UnitData owner, UnitData partner, float value = 0f, AbilityData req = null)
            {
                var l = ScriptableObject.CreateInstance<LinkAbilityData>();
                l.name = n; l.type = t; l.displayName = n; l.chance = chance; l.owner = owner; l.partner = partner; l.value = value; l.requiredAbility = req;
                return l;
            }
            cfg.links = new[]
            {
                Link(LinkType.LinkAttack, "Link Attack", 0.5f, fang, xiang),
                Link(LinkType.Counterattack, "Counterattack", 0.5f, fang, xiang),
                Link(LinkType.SpiritAttunement, "Spirit Attunement", 0.5f, fang, xiang, 1.5f, spear),
                Link(LinkType.AutoCharge, "Auto-Charge", 0.5f, xiang, fang, 0.10f),
                Link(LinkType.AutoHeal, "Auto-Heal", 0.5f, xiang, fang, 0.10f),
                Link(LinkType.MagicCover, "Magic Cover", 0.2f, xiang, fang),
            };

            // ---------- Items ----------
            var balm = ScriptableObject.CreateInstance<ItemData>();
            balm.name = "Balm"; balm.displayName = "Balm"; balm.description = "Restores 250 HP."; balm.sfxKey = "heal";
            balm.effects.Add(Fx(EffectType.HealFlat, 250f));
            var epc = ScriptableObject.CreateInstance<ItemData>();
            epc.name = "EP Charge"; epc.displayName = "EP Charge"; epc.description = "Restores 30 EP."; epc.sfxKey = "buff"; epc.color = new Color(0.4f, 0.7f, 1f);
            epc.effects.Add(Fx(EffectType.RestoreEpFlat, 30f));
            cfg.items = new[] { new ItemStock { item = balm, quantity = 3 }, new ItemStock { item = epc, quantity = 3 } };

            // ---------- Enemies ----------
            var melee = EnemyAttack("Strike", DamageType.Physical, 1, Presentation.MeleeSlash, new Color(1f, 0.5f, 0.4f), 120);
            var arrow = EnemyAttack("Shot", DamageType.Physical, 5, Presentation.Projectile, new Color(1f, 0.8f, 0.5f), 130);
            var bolt = EnemyAttack("Hex Bolt", DamageType.Magical, 5, Presentation.Projectile, new Color(0.8f, 0.4f, 1f), 130);

            var red = new Color(0.75f, 0.18f, 0.15f);
            var steel = new Color(0.5f, 0.52f, 0.58f);
            var crimson = new Color(0.9f, 0.3f, 0.2f);
            var violet = new Color(0.45f, 0.25f, 0.6f);
            var spawns = new List<EnemySpawn>
            {
                new EnemySpawn { unit = Enemy("Soldier", ModelStyle.MeleeSoldier, false, melee, 900, 55, 40, 30, 40, 7, 3, red, 1f), cell = new Vector2Int(15, 5) },
                new EnemySpawn { unit = Enemy("Brute", ModelStyle.MeleeBrute, false, melee, 1150, 60, 55, 20, 35, 5, 3, crimson, 1.2f), cell = new Vector2Int(15, 7) },
                new EnemySpawn { unit = Enemy("Blader", ModelStyle.MeleeBlade, false, melee, 850, 50, 35, 30, 40, 9, 4, steel, 1f), cell = new Vector2Int(15, 9) },
                new EnemySpawn { unit = Enemy("Shieldbearer", ModelStyle.MeleeShield, false, melee, 1000, 45, 70, 30, 60, 6, 3, steel, 1.05f), cell = new Vector2Int(16, 10) },
                new EnemySpawn { unit = Enemy("Lancer", ModelStyle.MeleeLancer, false, melee, 900, 55, 40, 30, 40, 8, 3, red, 1.05f), cell = new Vector2Int(17, 6) },
                new EnemySpawn { unit = Enemy("Berserker", ModelStyle.MeleeBerserker, false, melee, 800, 65, 35, 20, 30, 8, 4, crimson, 1.1f), cell = new Vector2Int(17, 8) },
                new EnemySpawn { unit = Enemy("Archer", ModelStyle.RangedArcher, true, arrow, 700, 55, 30, 30, 40, 7, 3, red, 0.95f), cell = new Vector2Int(20, 4) },
                new EnemySpawn { unit = Enemy("Crossbowman", ModelStyle.RangedCrossbow, true, arrow, 750, 55, 35, 30, 40, 6, 3, steel, 1f), cell = new Vector2Int(20, 7) },
                new EnemySpawn { unit = Enemy("Mage", ModelStyle.RangedMage, true, bolt, 650, 25, 30, 60, 45, 6, 3, violet, 0.95f), cell = new Vector2Int(20, 9) },
                new EnemySpawn { unit = Enemy("Orb Caster", ModelStyle.RangedOrb, true, bolt, 650, 25, 30, 60, 45, 7, 3, violet, 0.95f), cell = new Vector2Int(20, 12) },
            };
            cfg.enemies = spawns.ToArray();
            cfg.party = new[]
            {
                new PartySpawn { unit = fang, cell = new Vector2Int(3, 7) },
                new PartySpawn { unit = xiang, cell = new Vector2Int(3, 9) },
            };
            return cfg;
        }
    }
}
