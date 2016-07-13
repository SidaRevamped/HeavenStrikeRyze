using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using ItemData = LeagueSharp.Common.Data.ItemData;
using SharpDX;
using Color = System.Drawing.Color;

namespace HeavenStrikeRyze
{
    public static class Combo
    {
        public static void BadaoActivate()
        {
            Game.OnUpdate += Game_OnUpdate;
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
        }

        private static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (Program._orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo)
                return;

        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (Program._orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo)
                return;

            var target = TargetSelector.GetTarget(600, TargetSelector.DamageType.Magical);
            if (target.IsValidTarget() && !target.IsZombie)
            {
                if (!Helper.CanShield())
                {
                    if(Program._q.IsReady())
                        Helper.CastQTarget(target);
                    if (Program._w.IsReady())
                        Program._w.Cast(target);
                    if (Program._e.IsReady())
                        Program._e.Cast(target);
                }
                if (Helper.CanShield())
                {
                    if (Helper.Qstack() == 1)
                    {
                        if (Program._w.IsReady())
                            Program._w.Cast(target);
                        if (Program._e.IsReady())
                            Program._e.Cast(target);
                    }
                    if (Helper.Qstack() == 2)
                    {
                        if (Program._q.IsReady())
                            Helper.CastQTarget(target);
                        if (Program._w.IsReady())
                            Program._w.Cast(target);
                        if (Program._e.IsReady())
                            Program._e.Cast(target);
                    }
                }
            }
            else
            {
                var target1 = TargetSelector.GetTarget(Program._q.Range, TargetSelector.DamageType.Magical);
                if (target1.IsValidTarget() && !target1.IsZombie)
                {
                    Helper.CastQTarget(target1);
                }
            }
        }
    }
}
