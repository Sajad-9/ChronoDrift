using System;
using System.Collections.Generic;
using UnityEngine;
using ChronoDrift.Combat;
using ChronoDrift.Core;
using ChronoDrift.Player;

namespace ChronoDrift.Progression
{
    public class UpgradeOption
    {
        public string Id;
        public string Title;
        public string Description;
        public Color AccentColor;
        public Action Apply;
    }

    /// <summary>
    /// Roguelite Core Upgrade pool offered between waves.
    /// </summary>
    public static class UpgradeSystem
    {
        public static List<UpgradeOption> RollThreeOptions()
        {
            var pool = new List<UpgradeOption>
            {
                new UpgradeOption
                {
                    Id = "ricochet",
                    Title = "RICOCHET MASTERY",
                    Description = "+2 Bolt Bounces.\nEach bounce grants +50% damage.",
                    AccentColor = new Color(1f, 0.85f, 0.2f),
                    Apply = () => PlayerController.Instance?.AddRicochet(2)
                },
                new UpgradeOption
                {
                    Id = "split",
                    Title = "TWIN PRISM",
                    Description = "+2 Angled Split Bolts\nper primary shot.",
                    AccentColor = new Color(0.15f, 0.92f, 1f),
                    Apply = () => PlayerController.Instance?.AddSplitShot()
                },
                new UpgradeOption
                {
                    Id = "chain",
                    Title = "ARC LIGHTNING",
                    Description = "Bolts arc electricity to\n2 nearby enemies on hit.",
                    AccentColor = new Color(0.4f, 0.75f, 1f),
                    Apply = () => PlayerController.Instance?.AddChainLightning()
                },
                new UpgradeOption
                {
                    Id = "overclock",
                    Title = "OVERCLOCK CORE",
                    Description = "+22% Fire Rate and\n+12% Bolt Damage.",
                    AccentColor = new Color(1f, 0.45f, 0.2f),
                    Apply = () => PlayerController.Instance?.OverclockFireRate()
                },
                new UpgradeOption
                {
                    Id = "dash_emp",
                    Title = "PHASE SHOCKWAVE",
                    Description = "Dashing emits an EMP ring\nthat blasts nearby foes.",
                    AccentColor = new Color(0.25f, 1f, 0.75f),
                    Apply = () => PlayerController.Instance?.AddDashShockwave()
                },
                new UpgradeOption
                {
                    Id = "singularity",
                    Title = "VOID AMPLIFIER",
                    Description = "Larger Singularity radius,\n+30 dmg, faster cooldown.",
                    AccentColor = new Color(0.75f, 0.35f, 1f),
                    Apply = () => PlayerController.Instance?.AddSingularityUpgrade()
                },
                new UpgradeOption
                {
                    Id = "orbital",
                    Title = "ORBITAL AEGIS",
                    Description = "+2 Rotating Shield Orbs\nthat block enemy fire.",
                    AccentColor = new Color(0.3f, 1f, 0.55f),
                    Apply = () => PlayerController.Instance?.AddOrbitalShield()
                },
                new UpgradeOption
                {
                    Id = "vampiric",
                    Title = "CHRONO SIPHON",
                    Description = "Heal +1 HP on kill streaks &\n+35% Chrono Energy regen.",
                    AccentColor = new Color(1f, 0.25f, 0.55f),
                    Apply = () =>
                    {
                        PlayerController.Instance?.AddVampiric();
                        PlayerController.Instance?.Heal(1);
                        TimeDilationManager.Instance?.MultiplyRegen(1.35f);
                    }
                },
                new UpgradeOption
                {
                    Id = "blackhole",
                    Title = "RIFT CORE OVERCHARGE",
                    Description = "Upgrade Black Hole Rift:\n+Radius, +Damage, -18% Cooldown.",
                    AccentColor = new Color(0.5f, 0.05f, 1f),
                    Apply = () => PlayerController.Instance?.AddBlackHoleUpgrade()
                },
                new UpgradeOption
                {
                    Id = "maxhp",
                    Title = "HULL REINFORCEMENT",
                    Description = "+2 Maximum Hull Points.\nRestores +1 HP immediately.",
                    AccentColor = new Color(0.2f, 1f, 0.5f),
                    Apply = () =>
                    {
                        if (PlayerController.Instance == null) return;
                        PlayerController.Instance.maxHealth += 2;
                        PlayerController.Instance.Heal(1);
                    }
                }
            };

            // Fisher-Yates shuffle and take 3
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            return pool.GetRange(0, 3);
        }
    }
}
