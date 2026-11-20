namespace RpgBattle
{
    public enum Team { Player, Enemy }
    public enum DamageType { Physical, Magical, None }
    public enum ActionCategory { Attack, Art, Special, Item }
    public enum StatusType { None, Blind, Freeze, Poison, InstantKill }
    public enum StatKey { STR, DEF, ATS, ADF, SPD, MOV }
    public enum TargetShape { Single, Line, Circle, All }
    public enum TargetSide { Enemy, Ally }
    public enum AreaCenter { Caster, Cursor }
    public enum EffectType
    {
        Damage, HealMaxHpPercent, HealFlat, RestoreEpFlat, RestoreEpPercent, ApplyStatus, ApplyBuff,
        GrantShield, GrantSp, ResetAtb, InstantKill, RandomStatus, CureStatuses
    }
    public enum LinkType { LinkAttack, Counterattack, SpiritAttunement, AutoCharge, AutoHeal, MagicCover }
    public enum Presentation { None, MeleeSlash, Projectile, PiercingBeam, CircleBurst, Cast, Heal, Buff, ValiantDawn, FatesCard, GroundSlam, BlinkStrike }
    public enum ModelStyle
    {
        Fang, Xiangling,
        MeleeSoldier, MeleeBrute, MeleeBlade, MeleeShield, MeleeLancer, MeleeBerserker,
        RangedArcher, RangedCrossbow, RangedMage, RangedOrb
    }
}
