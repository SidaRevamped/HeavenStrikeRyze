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
    public static class Lane
    {
        private static Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        public static void BadaoActivate()
        {
            Game.OnUpdate += Game_OnUpdate;
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (Program._orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.LaneClear)
                return;

            if (Player.Mana * 100 / Player.MaxMana > Program.ManaLaneClear)
            {
                var targetq = MinionManager.GetMinions(Player.Position, Program._q.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health).FirstOrDefault();
                var target = MinionManager.GetMinions(Player.Position, 600, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health).FirstOrDefault();
                if (Program._q2.IsReady() && Program.QlaneClear)
                {
                    if (targetq != null)
                    {
                        Program._q2.Cast(targetq);
                    }
                }
                else if (Program._e.IsReady() && Program.ElaneClear)
                {
                    if (target != null)
                    {
                        Program._e.Cast(target);
                    }
                }
                else if (Program._w.IsReady() && Program.WlaneClear)
                {
                    if (target != null)
                    {
                        Program._w.Cast(targetq);
                    }
                }
            }
        }
    }
}
