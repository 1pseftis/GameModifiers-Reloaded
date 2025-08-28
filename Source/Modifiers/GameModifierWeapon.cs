
using System;
using System.Collections.Generic;
using System.Linq;

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;

namespace GameModifiers.Modifiers;

public abstract class GameModifierWeapon : GameModifierBase
{
    protected readonly List<uint> CachedAppliedWeapons = new();

    public override void Enabled()
    {
        base.Enabled();

        if (Core != null)
        {
            Core.RegisterEventHandler<EventItemEquip>(OnItemEquip);
            Core.RegisterEventHandler<EventEntityKilled>(OnEntityKilled, HookMode.Pre);
        }

        Utilities.GetPlayers().ForEach(TryApplyWeaponModifier);
    }

    public override void Disabled()
    {
        if (Core != null)
        {
            Core.DeregisterEventHandler<EventItemEquip>(OnItemEquip);
            Core.DeregisterEventHandler<EventEntityKilled>(OnEntityKilled, HookMode.Pre);
        }

        foreach (uint weaponIndex in CachedAppliedWeapons)
        {
            CBasePlayerWeapon? weapon = Utilities.GetEntityFromIndex<CBasePlayerWeapon>((int)weaponIndex);
            if (weapon != null && weapon.IsValid)
            {
                RemoveWeaponModifier(weapon);
            }
        }

        CachedAppliedWeapons.Clear();

        base.Disabled();
    }

    private void TryApplyWeaponModifier(CCSPlayerController? player)
    {
        List<CBasePlayerWeapon?> weapons = GameModifiersUtils.GetWeapons(player);
        if (!weapons.Any())
        {
            return;
        }

        foreach (var weapon in weapons)
        {
            if (weapon == null || !weapon.IsValid)
            {
                continue;
            }

            if (!CachedAppliedWeapons.Contains(weapon.Index) && ApplyWeaponModifier(weapon))
            {
                CachedAppliedWeapons.Add(weapon.Index);
            }
        }
    }

    protected virtual bool ApplyWeaponModifier(CBasePlayerWeapon? weapon)
    {
        // Implement in child class...
        // Should return true if it was applied successfully.
        return false;
    }

    protected virtual void RemoveWeaponModifier(CBasePlayerWeapon? weapon)
    {
        // Implement in child class...
    }

    private HookResult OnItemEquip(EventItemEquip @event, GameEventInfo info)
    {
        TryApplyWeaponModifier(@event.Userid);
        return HookResult.Continue;
    }

    private HookResult OnEntityKilled(EventEntityKilled @event, GameEventInfo info)
    {
        var index = (uint)@event.EntindexKilled;
        if (CachedAppliedWeapons.Contains(index))
        {
            CBasePlayerWeapon? removedWeapon = Utilities.GetEntityFromIndex<CBasePlayerWeapon>((int)index);
            RemoveWeaponModifier(removedWeapon);
            CachedAppliedWeapons.Remove(index);
        }

        return HookResult.Continue;
    }
}

public class GameModifierOnePerMag : GameModifierWeapon
{
    public override string Name => "OnePerReload";
    public override string Description => "1 bullet per reload";
    public override bool SupportsRandomRounds => true;
    public override HashSet<string> IncompatibleModifiers =>
    [
        GameModifiersUtils.GetModifierName<GameModifierOneInTheChamber>(),
        "InfiniteAmmo"
    ];
    private readonly Dictionary<string, int> _cachedMaxClip1 = new();

    protected override bool ApplyWeaponModifier(CBasePlayerWeapon? weapon)
    {
        if (weapon == null || !weapon.IsValid || !GameModifiersUtils.IsRangedWeapon(weapon))
        {
            return false;
        }

        CCSWeaponBaseVData? weaponVData = weapon.As<CCSWeaponBase>().VData;
        if (weaponVData == null)
        {
            return false;
        }

        if (!_cachedMaxClip1.ContainsKey(weapon.DesignerName))
        {
            _cachedMaxClip1.Add(weapon.DesignerName, weaponVData.MaxClip1);
            
            Server.NextFrame(() =>
            {
                weaponVData.MaxClip1 = 1;
            });
        }

        Server.NextFrame(() =>
        {
            weapon.Clip1 = 1;
            Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_iClip1");
        });

        return true;
    }

    protected override void RemoveWeaponModifier(CBasePlayerWeapon? weapon)
    {
        if (weapon == null || !weapon.IsValid)
        {
            return;
        }

        if (_cachedMaxClip1.ContainsKey(weapon.DesignerName) == false)
        {
            GameModifiersUtils.ResetWeaponAmmo(weapon);
            return;
        }

        int originalMaxClip1 = _cachedMaxClip1[weapon.DesignerName];
        _cachedMaxClip1.Remove(weapon.DesignerName);

        Server.NextFrame(() =>
        {
            CCSWeaponBaseVData? weaponVData = weapon.As<CCSWeaponBase>().VData;
            if (weaponVData == null)
            {
                Console.WriteLine($"[GameModifierOnePerMag::RemoveWeaponModifier] WARNING: Cannot reset weapon VData for {weapon.DesignerName}!");
            }
            else
            {
                weaponVData.MaxClip1 = originalMaxClip1;
            }

            GameModifiersUtils.ResetWeaponAmmo(weapon);
        });
    }
}

// TeamReload Modifier WIP
// public class GameModifierTeamReload : GameModifierWeapon
// {
//     public override string Name => "TeamReload";
//     public override string Description => "When one player reloads, their whole team reloads too!";
//     public override bool SupportsRandomRounds => false;

//     public override void Enabled()
//     {
//         base.Enabled();

//         if (Core != null)
//         {
//             Core.RegisterEventHandler<EventWeaponReload>(OnWeaponReload);
//         }
//     }

//     public override void Disabled()
//     {
//         if (Core != null)
//         {
//             Core.DeregisterEventHandler<EventWeaponReload>(OnWeaponReload);
//         }

//         base.Disabled();
//     }

//     private HookResult OnWeaponReload(EventWeaponReload ev, GameEventInfo info)
//     {
//         CCSPlayerController? reloader = ev.Userid;
//         if (reloader == null || !reloader.IsValid || !reloader.PawnIsAlive)
//             return HookResult.Continue;

//         // Get team of reloader
//         var team = reloader.TeamNum;

//         // Apply reload to teammates
//         foreach (var teammate in Utilities.GetPlayers().Where(p => p.TeamNum == team && p != reloader))
//         {
//             ForceReload(teammate);
//         }

//         return HookResult.Continue;
//     }

//     private void ForceReload(CCSPlayerController player)
//     {
//         var pawn = player.PlayerPawn.Value;
//         if (pawn == null || !pawn.IsValid) return;

//         var weapon = pawn.WeaponServices?.ActiveWeapon.Value;
//         if (weapon == null || !weapon.IsValid) return;

//         // Get weapon VData for clip information
//         CCSWeaponBaseVData? weaponVData = weapon.As<CCSWeaponBase>().VData;
//         if (weaponVData == null) return;

//         // Skip if already full clip or no reserve ammo
//         if (weapon.Clip1 >= weaponVData.MaxClip1) return;
//         if (weapon.ReserveAmmo[0] <= 0) return;

//         Server.NextFrame(() =>
//         {
//             // Calculate how much ammo we need to fill the clip
//             int neededAmmo = weaponVData.MaxClip1 - weapon.Clip1;
//             int availableAmmo = weapon.ReserveAmmo[0];
//             int ammoToUse = Math.Min(neededAmmo, availableAmmo);

//             // Refill the clip
//             weapon.Clip1 += ammoToUse;
//             weapon.ReserveAmmo[0] -= ammoToUse;

//             // Update state
//             Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_iClip1");
//             Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_pReserveAmmo");
//         });
//     }
// }

public class GameModifierOneInTheChamber : GameModifierWeapon
{
    public override string Name => "OneInTheChamber";
    public override string Description => "1 bullet per kill, pistols one hit";
    public override bool SupportsRandomRounds => true;
    public override HashSet<string> IncompatibleModifiers =>
    [
        GameModifiersUtils.GetModifierName<GameModifierOnePerMag>(),
        "InfiniteAmmo"
    ];

    public override void Enabled()
    {
        base.Enabled();

        if (Core != null)
        {
            Core.RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
        }
        
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Pre);
    }

    public override void Disabled()
    {
        if (Core != null)
        {
            Core.DeregisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
        }
        
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(OnTakeDamage, HookMode.Pre);

        base.Disabled();
    }

    protected override bool ApplyWeaponModifier(CBasePlayerWeapon? weapon)
    {
        if (weapon == null || !weapon.IsValid || !GameModifiersUtils.IsRangedWeapon(weapon))
        {
            return false;
        }

        Server.NextFrame(() =>
        {
            weapon.Clip1 = 1;
            weapon.Clip2 = 0;
            weapon.ReserveAmmo[0] = 0;

            Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_iClip1");
            Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_iClip2");
            Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_pReserveAmmo");
        });

        return true;
    }

    protected override void RemoveWeaponModifier(CBasePlayerWeapon? weapon)
    {
        GameModifiersUtils.ResetWeaponAmmo(weapon);
    }

    private void AddBullet(CCSPlayerController? player, string weaponName)
    {
        CBasePlayerWeapon? foundWeapon = GameModifiersUtils.GetWeapon(player, weaponName);
        if (foundWeapon == null || !foundWeapon.IsValid || !GameModifiersUtils.IsRangedWeapon(foundWeapon))
        {
            Console.WriteLine($"add bullet: CANNOT FIND WEAPON {weaponName}");
            return;
        }

        Server.NextFrame(() =>
        {
            foundWeapon.ReserveAmmo[0] += 1;

            Utilities.SetStateChanged(foundWeapon, "CBasePlayerWeapon", "m_pReserveAmmo");
        });
    }

    private HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        AddBullet(@event.Attacker, @event.Weapon);
        return HookResult.Continue;
    }
    
    private HookResult OnTakeDamage(DynamicHook hook)
    {
        CTakeDamageInfo damageInfo = hook.GetParam<CTakeDamageInfo>(1);
        damageInfo.Damage *= 10.0f;
        return HookResult.Continue;
    }
}

public abstract class GameModifierFireRateBase : GameModifierBase
{
    public virtual float FireRateMultiplier { get; protected set; } = 1.0f;

    public override void Enabled()
    {
        base.Enabled();

        if (Core != null)
        {
            Core.RegisterEventHandler<EventWeaponFire>(OnWeaponFire);
        }
    }

    public override void Disabled()
    {
        if (Core != null)
        {
            Core.DeregisterEventHandler<EventWeaponFire>(OnWeaponFire);
        }

        base.Disabled();
    }

    private HookResult OnWeaponFire(EventWeaponFire @event, GameEventInfo info)
    {
        if (Math.Abs(FireRateMultiplier - 1.0f) < 0.01f)
        {
            return HookResult.Continue;
        }

        CCSPlayerController? player = @event.Userid;
        if (player == null || !player.IsValid)
        {
            return HookResult.Continue;
        }

        CBasePlayerWeapon? weapon = GameModifiersUtils.GetWeapon(player, @event.Weapon);
        if (weapon == null || !weapon.IsValid || !GameModifiersUtils.IsRangedWeapon(weapon))
        {
            return HookResult.Continue;
        }

        Server.NextFrame(() =>
        {
            float fireRateTicks = (weapon.NextPrimaryAttackTick - Server.TickCount) / FireRateMultiplier;
            weapon.NextPrimaryAttackTick = Server.TickCount + + Convert.ToInt32(fireRateTicks);

            Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_nNextPrimaryAttackTick");
        });

        return HookResult.Continue;
    }
}

public class GameModifierNoSpread : GameModifierBase
{
    public override string Name => "NoSpread";
    public override string Description => "Weapons have perfect aim";
    public override bool SupportsRandomRounds => true;
    public override HashSet<string> IncompatibleModifiers =>
    [
        "IncreasedSpread"
    ];

    public override void Enabled()
    {
        base.Enabled();

        if (Core != null)
        {
            Core.RegisterEventHandler<EventWeaponFire>(OnWeaponFire);
        }
    }

    public override void Disabled()
    {
        if (Core != null)
        {
            Core.DeregisterEventHandler<EventWeaponFire>(OnWeaponFire);
        }

        base.Disabled();
    }

    private HookResult OnWeaponFire(EventWeaponFire @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;
        if (player == null || !player.IsValid)
        {
            return HookResult.Continue;
        }

        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn == null || !playerPawn.IsValid)
        {
            return HookResult.Continue;
        }

        Server.NextFrame(() =>
        {
            playerPawn.AimPunchTickFraction = 0;
            playerPawn.AimPunchTickBase = -1;
            playerPawn.AimPunchAngle.X = 0;
            playerPawn.AimPunchAngle.Y = 0;
            playerPawn.AimPunchAngle.Z = 0;
            playerPawn.AimPunchAngleVel.X = 0;
            playerPawn.AimPunchAngleVel.Y = 0;
            playerPawn.AimPunchAngleVel.Z = 0;
        });

        return HookResult.Continue;
    }
}

public class GameModifierFastFireRate : GameModifierFireRateBase
{
    public override string Name => "FastFireRate";
    public override string Description => "Fire rate is 2 times faster";
    public override bool SupportsRandomRounds => true;
    public override HashSet<string> IncompatibleModifiers =>
    [
        GameModifiersUtils.GetModifierName<GameModifierSlowFireRate>()
    ];
    public override float FireRateMultiplier { get; protected set; } = 2.0f;
}

public class GameModifierSlowFireRate : GameModifierFireRateBase
{
    public override string Name => "SlowFireRate";
    public override string Description => "Fire rate is 2 times slower";
    public override bool SupportsRandomRounds => true;
    public override HashSet<string> IncompatibleModifiers =>
    [
        GameModifiersUtils.GetModifierName<GameModifierFastFireRate>()
    ];
    public override float FireRateMultiplier { get; protected set; } = 0.5f;
}