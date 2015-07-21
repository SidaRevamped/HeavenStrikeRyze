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
    class Program
    {
        private static Obj_AI_Hero Player { get { return ObjectManager.Player; } }

        private static Orbwalking.Orbwalker _orbwalker;

        private static Spell _q, _q2, _w, _e, _r;

        private static Menu _menu;

        private static bool IsCharged { get { return Player.HasBuff("ryzepassivecharged"); } }

        private static int PassiveStack
        {
            get
            {
                var buff = Player.Buffs.Find(b => b.DisplayName == "RyzePassiveStack");
                return buff != null ? buff.Count : 0;
            }
        }
        private static bool waitQ { get { return _q.IsReady(250); } }
        private static bool waitW { get { return _w.IsReady(250); } }
        private static bool waitE { get { return _e.IsReady(250); } }
        private static bool mustwaitQ = false;
        private static int timewaitQ;
        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            //Verify Champion
            if (Player.ChampionName != "Ryze")
                return;

            //Spells
            _q = new Spell(SpellSlot.Q, 900);
            _q2 = new Spell(SpellSlot.Q, 900);
            _w = new Spell(SpellSlot.W, 600);
            _e = new Spell(SpellSlot.E, 600);
            _r = new Spell(SpellSlot.R);

            _q.SetSkillshot(0.26f, 50f, 1700f, true, SkillshotType.SkillshotLine);
            _q2.SetSkillshot(0.26f, 50f, 1700f, false, SkillshotType.SkillshotLine);
            _q.MinHitChance = HitChance.Medium;
            _q2.MinHitChance = HitChance.Medium;

            //Menu instance
            _menu = new Menu(Player.ChampionName, Player.ChampionName, true);
            //Orbwalker
            Menu orbwalkerMenu = new Menu("Orbwalker", "Orbwalker");
            _orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);
            //Targetsleector
            _menu.AddSubMenu(orbwalkerMenu);
            Menu ts = _menu.AddSubMenu(new Menu("Target Selector", "Target Selector")); ;
            TargetSelector.AddToMenu(ts);
            //spell menu
            Menu spellMenu = _menu.AddSubMenu(new Menu("Spells", "Spells"));
            //Combo
            Menu Combo = spellMenu.AddSubMenu(new Menu("Combo", "Combo"));
            Combo.AddItem(new MenuItem("StackQ", "Stack Passive Until Can Burst").SetValue(true));
            Combo.AddItem(new MenuItem("BlockAA", "Block AutoAttack").SetValue(true));
            //Combo.AddItem(new MenuItem("RHC", "R if will hit").SetValue(new Slider(2, 1, 5)));
            //auto
            Menu Auto = spellMenu.AddSubMenu(new Menu("Auto", "Auto"));
            Auto.AddItem(new MenuItem("Wantigap", "W anti gap").SetValue(true));
            Auto.AddItem(new MenuItem("Winterrupt", "W interrupt").SetValue(true));
            Auto.AddItem(new MenuItem("KeepUP", "Keep Passive STack Up").SetValue(true));
            //Clear
            Menu JungClear = spellMenu.AddSubMenu(new Menu("JC", "Jungle Clear"));
            JungClear.AddItem(new MenuItem("QJC", "use Q Jungle Clear").SetValue(true));
            JungClear.AddItem(new MenuItem("WJC", "use W Jungle Clear").SetValue(true));
            JungClear.AddItem(new MenuItem("EJC", "use E Jungle Clear").SetValue(true));
            JungClear.AddItem(new MenuItem("RJC", "use R Jungle Clear").SetValue(true));
            JungClear.AddItem(new MenuItem("ManaJC", "Min Mana Jung Clear").SetValue(new Slider(40, 0, 100)));
            Menu LaneClear = spellMenu.AddSubMenu(new Menu("LC", "Lane Clear"));
            LaneClear.AddItem(new MenuItem("QLC", "use Q Lane Clear").SetValue(true));
            LaneClear.AddItem(new MenuItem("WLC", "use w Lane Clear").SetValue(true));
            LaneClear.AddItem(new MenuItem("ELC", "use e Lane Clear").SetValue(true));
            LaneClear.AddItem(new MenuItem("RLC", "use R Lane Clear").SetValue(true));
            LaneClear.AddItem(new MenuItem("ManaLC", "Min Mana Lane Clear").SetValue(new Slider(40, 0, 100)));
            Menu LastHit = spellMenu.AddSubMenu(new Menu("LH", "Last Hit"));
            LastHit.AddItem(new MenuItem("QLH", "use Q Last Hit").SetValue(true));
            LastHit.AddItem(new MenuItem("ManaLH", "Min Mana Last Hit").SetValue(new Slider(40, 0, 100)));


            //Drawing
            Menu Draw = _menu.AddSubMenu(new Menu("Drawing", "Drawing"));
            Draw.AddItem(new MenuItem("DQ", "Draw Q").SetValue(true));
            Draw.AddItem(new MenuItem("DW", "Draw W").SetValue(true));
            Draw.AddItem(new MenuItem("DE", "Draw E").SetValue(true));
            Draw.AddItem(new MenuItem("DB", "Draw Burst Status").SetValue(true));

            //Attach to root
            _menu.AddToMainMenu();

            Notifications.AddNotification(notifyselected);

            //Listen to events
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnGameUpdate;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Obj_AI_Base.OnProcessSpellCast += oncast;

        }
        private static bool stackQcombo { get { return _menu.Item("StackQ").GetValue<bool>(); } }
        private static bool blockAAcombo { get { return _menu.Item("BlockAA").GetValue<bool>(); } }
        private static bool wAntiGap { get { return _menu.Item("Wantigap").GetValue<bool>(); } }
        private static bool wInterrupt { get { return _menu.Item("Winterrupt").GetValue<bool>(); } }
        private static bool QjungClear { get { return _menu.Item("QJC").GetValue<bool>(); } }
        private static bool WjungClear { get { return _menu.Item("WJC").GetValue<bool>(); } }
        private static bool EjungClear { get { return _menu.Item("EJC").GetValue<bool>(); } }
        private static bool RjungClear { get { return _menu.Item("RJC").GetValue<bool>(); } }
        private static bool QlaneClear { get { return _menu.Item("QLC").GetValue<bool>(); } }
        private static bool WlaneClear { get { return _menu.Item("WLC").GetValue<bool>(); } }
        private static bool ElaneClear { get { return _menu.Item("ELC").GetValue<bool>(); } }
        private static bool RlaneClear { get { return _menu.Item("RLC").GetValue<bool>(); } }
        private static int ManaJungClear { get { return _menu.Item("ManaJC").GetValue<Slider>().Value; } }
        private static int ManaLaneClear { get { return _menu.Item("ManaLC").GetValue<Slider>().Value; } }
        private static bool QlastHit { get { return _menu.Item("QLH").GetValue<bool>(); } }
        private static int ManaLastHit { get { return _menu.Item("ManaLH").GetValue<Slider>().Value; } }
        private static bool DrawQ { get { return _menu.Item("DQ").GetValue<bool>(); } }
        private static bool DrawW { get { return _menu.Item("DW").GetValue<bool>(); } }
        private static bool DrawE { get { return _menu.Item("DE").GetValue<bool>(); } }
        private static bool DrawBurstStatus { get { return _menu.Item("DB").GetValue<bool>(); } }
        private static bool Keeppassiveup { get { return _menu.Item("KeepUP").GetValue<bool>(); } }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Player.IsDead) return;
            if (DrawQ)
                Render.Circle.DrawCircle(Player.Position, _q.Range, Color.Aqua);
            if (DrawW)
                Render.Circle.DrawCircle(Player.Position, _w.Range, Color.Purple);
            if (DrawE)
                Render.Circle.DrawCircle(Player.Position, _e.Range, Color.Yellow);
        }

        public static void oncast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            var spell = args.SData;
            if (!sender.IsMe)
            {
                return;
            }
            //Game.Say(spell.Name);
            if (spell.Name.ToLower().Contains("ryzeq"))
            {
                mustwaitQ = false;
                qcount = Utils.GameTimeTickCount;
            }
            if (spell.Name.ToLower().Contains("ryzew"))
            {
                if (PassiveStack == 4 || IsCharged)
                {
                    mustwaitQ = true;
                    timewaitQ = Utils.GameTimeTickCount;
                }
                wcount = Utils.GameTimeTickCount;
            }
            if (spell.Name.ToLower().Contains("ryzee"))
            {
                ecount = Utils.GameTimeTickCount;
            }
            if (spell.Name.ToLower().Contains("ryzer"))
            {
                rcount = Utils.GameTimeTickCount;
            }
        }

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            // use W against gap closer
            var target = gapcloser.Sender;
            if (_w.IsReady() && target.IsValidTarget(_w.Range) && wAntiGap && _orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo)
            {
                _w.Cast(target);
            }
        }

        private static void Interrupter2_OnInterruptableTarget(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            // interrupt with W
            if (_w.IsReady() && sender.IsValidTarget(_w.Range) && !sender.IsZombie && wInterrupt && _orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo)
            {
                _w.Cast(sender);
            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            DrawBurst();

            if (mustwaitQ == true && Utils.GameTimeTickCount >= timewaitQ + 500 && !_q.IsReady())
            {
                mustwaitQ = false;
            }
            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && blockAAcombo)
            {
                if (Player.Mana > _q.Instance.ManaCost + _w.Instance.ManaCost + _e.Instance.ManaCost && !(_q.IsReady(500) || _w.IsReady(500) || _e.IsReady(500)))
                    _orbwalker.SetAttack(false);
                else
                    _orbwalker.SetAttack(true);
            }
            else
            {
                _orbwalker.SetAttack(true);
            }
            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                Combo();
                stackQcombomode();
            }
            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                JungClear();
                LaneClear();
            }
            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit)
            {
                LastHit();
            }
            //Game.Say(PassiveStack.ToString());
        }

        private static void Combo()
        {
            int waitcombo = 1000;
            bool waitcombobool = true;
            if (Player.Mana >= 3 * _q.Instance.ManaCost + _w.Instance.ManaCost + _e.Instance.ManaCost)
            {
                var target = TargetSelector.GetTarget(600, TargetSelector.DamageType.Magical);
                var target2 = TargetSelector.GetTarget(_q.Range, TargetSelector.DamageType.Magical);
                if (IsCharged)
                {
                    if (mustwaitQ == true)
                    {
                        if (_q.IsReady())
                        {
                            if (target.IsValidTarget() && !target.IsZombie)
                                _q2.Cast(target);
                            else if (target2.IsValidTarget() && !target.IsZombie)
                                _q2.Cast(target2);
                        }

                    }
                    else if (target.IsValidTarget() && ! target.IsZombie)
                    {
                         if (_r.IsReady() && Player.CountEnemiesInRange(800) >=2)
                        {
                            if (target.IsValidTarget() && !target.IsZombie)
                                _r.Cast();
                        }
                        else if (_w.IsReady())
                        {
                            if (target.IsValidTarget() && !target.IsZombie)
                                _w.Cast(target);
                        }
                        else if (_q.IsReady())
                        {
                            if (target.IsValidTarget() && !target.IsZombie)
                                _q2.Cast(target);
                            else if (target2.IsValidTarget() && !target.IsZombie)
                                _q2.Cast(target2);
                        }
                        else if (_e.IsReady())
                        {
                            if (target.IsValidTarget() && !target.IsZombie)
                                _e.Cast(target);
                        }

                        else if (_r.IsReady())
                        {
                            if (target.IsValidTarget() && !target.IsZombie)
                                _r.Cast();
                        }
                    }
                    else
                    {
                        if (_q.IsReady())
                        {
                            if (target2.IsValidTarget() && !target.IsZombie)
                                _q2.Cast(target2);
                        }
                    }
                }
                else if (PassiveStack == 4)
                {
                    if (candocombo() && waitcombobool)
                    {
                        if (_w.IsReady())
                        {
                            if (target.IsValidTarget() && !target.IsZombie)
                                _w.Cast(target);
                        }
                        else if (_q.IsReady())
                        {
                            if (target.IsValidTarget() && !target.IsZombie)
                                _q2.Cast(target);
                            else if (target2.IsValidTarget() && !target.IsZombie)
                                _q2.Cast(target2);
                        }
                        else if (_e.IsReady())
                        {
                            if (target.IsValidTarget() && !target.IsZombie)
                                _e.Cast(target);
                        }

                        else if (_r.IsReady())
                        {
                            if (target.IsValidTarget() && !target.IsZombie)
                                _r.Cast();
                        }
                    }

                }
                else if (PassiveStack == 3)
                {
                    if (candocombo())
                    {
                        if(CountSpellReady >2)
                        {
                            if (_w.IsReady())
                            {
                                if (target.IsValidTarget() && !target.IsZombie)
                                    _w.Cast(target);
                            }
                            else if (_q.IsReady())
                            {
                                if (target.IsValidTarget() && !target.IsZombie)
                                    _q2.Cast(target);
                                else if (target2.IsValidTarget() && !target.IsZombie)
                                    _q2.Cast(target2);
                            }
                        }
                        else
                        {
                            if (_w.IsReady() && _r.IsReady())
                            {
                                if (target.IsValidTarget() && !target.IsZombie)
                                    _w.Cast(target);
                            }
                            else
                            {
                                if (_q.IsReady())
                                {
                                    if (target.IsValidTarget() && !target.IsZombie)
                                        _q2.Cast(target);
                                    else if (target2.IsValidTarget() && !target.IsZombie)
                                        _q2.Cast(target2);
                                }
                                else if (_e.IsReady())
                                {
                                    if (target.IsValidTarget() && !target.IsZombie)
                                        _e.Cast(target);
                                }
                            }

                        }

                    }
                    else
                    {
                        if (candowaitcombo(waitcombo) && waitcombobool)
                        {
                            if(CountSpellWait(waitcombo) > 2)
                            {
                                if (_w.IsReady())
                                {
                                    if (target.IsValidTarget() && !target.IsZombie)
                                        _w.Cast(target);
                                }
                                else if (_q.IsReady())
                                {
                                    if (target.IsValidTarget() && !target.IsZombie)
                                        _q2.Cast(target);
                                    else if (target2.IsValidTarget() && !target.IsZombie)
                                        _q2.Cast(target2);
                                }
                                else if (_e.IsReady())
                                {
                                    if (target.IsValidTarget() && !target.IsZombie)
                                        _e.Cast(target);
                                }

                                else if (_r.IsReady())
                                {
                                    if (target.IsValidTarget() && !target.IsZombie)
                                        _r.Cast();
                                }
                            }
                            else
                            {
                                if (_w.IsReady(waitcombo) && _r.IsReady(waitcombo))
                                {
                                    if (_w.IsReady())
                                    {
                                        if (target.IsValidTarget() && !target.IsZombie)
                                            _w.Cast(target);
                                    }
                                    else if (_r.IsReady())
                                    {
                                        if (target.IsValidTarget() && !target.IsZombie)
                                            _r.Cast();
                                    }
                                }
                                else
                                {
                                    if (_q.IsReady())
                                    {
                                        if (target.IsValidTarget() && !target.IsZombie)
                                            _q2.Cast(target);
                                        else if (target2.IsValidTarget() && !target.IsZombie)
                                            _q2.Cast(target2);
                                    }
                                    else if (_e.IsReady())
                                    {
                                        if (target.IsValidTarget() && !target.IsZombie)
                                            _e.Cast(target);
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (_w.IsReady())
                            {
                                if (target.IsValidTarget() && !target.IsZombie)
                                    _w.Cast(target);
                            }
                            if (_q.IsReady())
                            {
                                if (target.IsValidTarget() && !target.IsZombie)
                                    _q.Cast(target);
                                else if (target2.IsValidTarget() && !target.IsZombie)
                                    _q.Cast(target2);
                            }
                            if (_e.IsReady())
                            {
                                if (target.IsValidTarget() && !target.IsZombie)
                                    _e.Cast(target);
                            }
                        }
                    }
                }
                else if (PassiveStack == 2)
                {
                    if (candocombo())
                    {
                        if (_w.IsReady() && _r.IsReady())
                        {
                            if (target.IsValidTarget() && !target.IsZombie)
                                _w.Cast(target);
                        }
                        else
                        {
                            if (_q.IsReady())
                            {
                                if (target.IsValidTarget() && !target.IsZombie)
                                    _q2.Cast(target);
                                else if (target2.IsValidTarget() && !target.IsZombie)
                                    _q2.Cast(target2);
                            }
                            else if (_e.IsReady())
                            {
                                if (target.IsValidTarget() && !target.IsZombie)
                                    _e.Cast(target);
                            }
                        }
                    }
                    else
                    {
                        if (candowaitcombo(waitcombo) && waitcombobool)
                        {
                            if (_w.IsReady() && _r.IsReady())
                            {
                                if (target.IsValidTarget() && !target.IsZombie)
                                    _w.Cast(target);
                            }
                            else
                            {
                                if (_q.IsReady())
                                {
                                    if (target.IsValidTarget() && !target.IsZombie)
                                        _q2.Cast(target);
                                    else if (target2.IsValidTarget() && !target.IsZombie)
                                        _q2.Cast(target2);
                                }
                                else if (_e.IsReady())
                                {
                                    if (target.IsValidTarget() && !target.IsZombie)
                                        _e.Cast(target);
                                }
                            }
                        }
                        else
                        {
                            if (_w.IsReady())
                            {
                                if (target.IsValidTarget() && !target.IsZombie)
                                    _w.Cast(target);
                            }
                            if (_q.IsReady())
                            {
                                if (target.IsValidTarget() && !target.IsZombie)
                                    _q.Cast(target);
                                else if (target2.IsValidTarget() && !target.IsZombie)
                                    _q.Cast(target2);
                            }
                            if (_e.IsReady())
                            {
                                if (target.IsValidTarget() && !target.IsZombie)
                                    _e.Cast(target);
                            }
                        }
                    }
                }
                else if (PassiveStack == 1)
                {
                    if (candocombo())
                    {
                        if (_w.IsReady())
                        {
                            if (target.IsValidTarget() && !target.IsZombie)
                                _w.Cast(target);
                        }
                    }
                    else
                    {
                        if (candowaitcombo(waitcombo) && waitcombobool)
                        {
                            if (_w.IsReady())
                            {
                                if (target.IsValidTarget() && !target.IsZombie)
                                    _w.Cast(target);
                            }
                            else if (_q.IsReady())
                            {
                                if (target.IsValidTarget() && !target.IsZombie)
                                    _q2.Cast(target);
                                else if (target2.IsValidTarget() && !target.IsZombie)
                                    _q2.Cast(target2);
                            }
                            else if (_e.IsReady())
                            {
                                if (target.IsValidTarget() && !target.IsZombie)
                                    _e.Cast(target);
                            }
                        }
                        else
                        {
                            if (_w.IsReady())
                            {
                                if (target.IsValidTarget() && !target.IsZombie)
                                    _w.Cast(target);
                            }
                            if (_q.IsReady())
                            {
                                if (target.IsValidTarget() && !target.IsZombie)
                                    _q.Cast(target);
                                else if (target2.IsValidTarget() && !target.IsZombie)
                                    _q.Cast(target2);
                            }
                            if (_e.IsReady())
                            {
                                if (target.IsValidTarget() && !target.IsZombie)
                                    _e.Cast(target);
                            }
                        }
                    }
                }
                else
                {
                    if (_w.IsReady())
                    {
                        if (target.IsValidTarget() && !target.IsZombie)
                            _w.Cast(target);
                    }
                    if (_q.IsReady())
                    {
                        if (target.IsValidTarget() && !target.IsZombie)
                            _q.Cast(target);
                        else if (target2.IsValidTarget() && !target.IsZombie)
                            _q.Cast(target2);
                    }
                    if (_e.IsReady())
                    {
                        if (target.IsValidTarget() && !target.IsZombie)
                            _e.Cast(target);
                    }
                }
            }
            else
            {
                var target = TargetSelector.GetTarget(600, TargetSelector.DamageType.Magical);
                var target2 = TargetSelector.GetTarget(_q.Range, TargetSelector.DamageType.Magical);
                if (_q.IsReady())
                {
                    if (target.IsValidTarget() && !target.IsZombie)
                        _q.Cast(target);
                    else if (target2.IsValidTarget() && !target.IsZombie)
                        _q.Cast(target2);
                }
                if (_e.IsReady())
                {
                    if (target.IsValidTarget() && !target.IsZombie)
                        _e.Cast(target);
                }
                if (_w.IsReady())
                {
                    if (target.IsValidTarget() && !target.IsZombie)
                        _w.Cast(target);
                }
            }
        }
        private static void JungClear()
        {
            if (Player.Mana * 100 / Player.MaxMana > ManaJungClear)
            {
                var targetq = MinionManager.GetMinions(Player.Position, _q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.Health).FirstOrDefault();
                var target = MinionManager.GetMinions(Player.Position, 600, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.Health).FirstOrDefault();
                if (_q.IsReady() && QjungClear)
                {
                    if (targetq != null)
                    {
                        _q.Cast(targetq);
                    }
                }
                else if (_e.IsReady() && EjungClear)
                {
                    if (targetq != null)
                    {
                        _e.Cast(targetq);
                    }
                }
                else if (_w.IsReady() && WjungClear)
                {
                    if (target != null)
                    {
                        _w.Cast(targetq);
                    }
                }
                else if (_r.IsReady() && RjungClear && PassiveStack == 4)
                {
                    if (targetq != null)
                    {
                        _r.Cast();
                    }
                }
            }
        }
        private static void LaneClear()
        {
            if (Player.Mana * 100 / Player.MaxMana > ManaLaneClear)
            {
                var targetq = MinionManager.GetMinions(Player.Position, _q.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health).FirstOrDefault();
                var target = MinionManager.GetMinions(Player.Position, 600, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health).FirstOrDefault();
                if (_q.IsReady() && QlaneClear)
                {
                    if (targetq != null)
                    {
                        _q.Cast(targetq);
                    }
                }
                else if (_e.IsReady() && ElaneClear)
                {
                    if (targetq != null)
                    {
                        _e.Cast(targetq);
                    }
                }
                else if (_w.IsReady() && WlaneClear)
                {
                    if (target != null)
                    {
                        _w.Cast(targetq);
                    }
                }
                else if (_r.IsReady() && RlaneClear && PassiveStack == 4)
                {
                    if (targetq != null)
                    {
                        _r.Cast();
                    }
                }
            }
        }
        private static void LastHit()
        {
            if (Player.Mana*100/Player.MaxMana > ManaLastHit)
            {
                var targetq = MinionManager.GetMinions(Player.Position, _q.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health).FirstOrDefault();
                var target = MinionManager.GetMinions(Player.Position, 600, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health).FirstOrDefault();
                if (_q.IsReady() && QlastHit && targetq != null && Orbwalking.CanMove(100) && target.Health < _q.GetDamage(targetq))
                {
                    _q.Cast(targetq);
                }
            }
        }
        private static int CountSpellReady
        {
            get
            {
                int x = 0 ;
                if (_q.IsReady())
                    x += 1;
                if (_w.IsReady())
                    x += 1;
                if (_e.IsReady())
                    x += 1;
                if (_r.IsReady())
                    x += 1;
                return x;
            }
        }
        private static int CountSpellWait(int timewait)
        {
            int x = 0;
            if (_q.IsReady(timewait))
                x += 1;
            if (_w.IsReady(timewait))
                x += 1;
            if (_e.IsReady(timewait))
                x += 1;
            if (_r.IsReady(timewait))
                x += 1;
            return x;
        }
        private static bool candocombo()
        {
            if (PassiveStack == 4)
            {
                if (CountSpellReady >=2)
                {
                    return true;
                }
                else if (CountSpellReady == 1)
                {
                    if ( _w.IsReady() || _r.IsReady())
                        {
                            return true;
                        }
                    else
                    {
                        if (_q.IsReady())
                        {
                            if (_w.IsReady((int)_q.Instance.Cooldown) || _e.IsReady((int)_q.Instance.Cooldown) || _r.IsReady((int)_q.Instance.Cooldown))
                            {
                                return true;
                            }
                            else
                                return false;
                        }
                        else if (_e.IsReady())
                        {
                            if (_w.IsReady((int)_q.Instance.Cooldown * 2) || _r.IsReady((int)_q.Instance.Cooldown * 2))
                            {
                                return true;
                            }
                            else
                                return false;
                        }
                        else
                            return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            else if (PassiveStack == 3)
            {
                if (CountSpellReady > 2)
                {
                    return true;
                }
                else if (CountSpellReady == 2)
                {
                    if ( _w.IsReady() || _r.IsReady())
                    {
                        return true;
                    }
                    else
                    {
                        if (_w.IsReady((int)_q.Instance.Cooldown * 2) || _r.IsReady((int)_q.Instance.Cooldown * 2))
                        {
                            return true;
                        }
                        else
                            return false;
                    }
                }
                else
                    return false;
            }
            else if (PassiveStack == 2)
            {
                if (CountSpellReady >=3)
                    return true;
                else
                    return false;
            }
            else if (PassiveStack == 1)
            {
                if (CountSpellReady == 4)
                    return true;
                else
                    return false;
            }
            else return false;
        }
        private static bool candowaitcombo(int timewait)
        {
            if (PassiveStack == 4)
            {
                if (CountSpellWait(timewait) >= 2)
                {
                    return true;
                }
                else if (CountSpellWait(timewait) == 1)
                {
                    if (_w.IsReady(timewait) || _r.IsReady(timewait))
                    {
                        return true;
                    }
                    else
                    {
                        if (_q.IsReady(timewait))
                        {
                            if (_w.IsReady((int)_q.Instance.Cooldown + timewait) || _e.IsReady((int)_q.Instance.Cooldown + timewait) || _r.IsReady((int)_q.Instance.Cooldown + timewait))
                            {
                                return true;
                            }
                            else
                                return false;
                        }
                        else if (_e.IsReady(timewait))
                        {
                            if (_w.IsReady((int)_q.Instance.Cooldown * 2 + timewait) || _r.IsReady((int)_q.Instance.Cooldown * 2 + timewait) )
                            {
                                return true;
                            }
                            else
                                return false;
                        }
                        else
                            return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            else if (PassiveStack == 3)
            {
                if (CountSpellWait(timewait) > 2)
                {
                    return true;
                }
                else if (CountSpellWait(timewait) == 2)
                {
                    if (_w.IsReady(timewait) || _r.IsReady(timewait))
                    {
                        return true;
                    }
                    else
                    {
                        if (_w.IsReady((int)_q.Instance.Cooldown * 2 +timewait) || _r.IsReady((int)_q.Instance.Cooldown * 2 + timewait))
                        {
                            return true;
                        }
                        else
                            return false;
                    }
                }
                else
                    return false;
            }
            else if (PassiveStack == 2)
            {
                if (CountSpellWait(timewait) >=3 )
                    return true;
                else
                    return false;
            }
            else if (PassiveStack == 1)
            {
                if (CountSpellWait(timewait) == 4)
                    return true;
                else
                    return false;
            }
            else return false;
        }
        private static void stackQcombomode()
        {
            if (!IsCharged && !candocombo() && !candowaitcombo(1000) && stackQcombo && _q.IsReady())
            {
                if (Player.CountEnemiesInRange(600) == 0)
                {
                    var target = TargetSelector.GetTarget(_q.Range,TargetSelector.DamageType.Magical);
                    if (target.IsValidTarget())
                    {
                        _q.Cast(target);
                    }
                    else
                    {
                        _q.Cast(Player.Position);
                    }
                }
            }
        }
        private static void DrawBurst()
        {
            // keep passive up
            if (Keeppassiveup && !IsCharged && !Player.IsRecalling())
            {
                var buff = Player.Buffs.Find(b => b.DisplayName == "RyzePassiveStack");
                if (buff == null)
                {
                    if (_q.IsReady())
                    {
                        _q.Cast(Player.Position);
                    }
                }
                else if (buff != null)
                {
                    if (PassiveStack == 4)
                    {
                        return;
                    }
                    else
                    {
                        int x = 0;
                        x = qcount > x ? qcount : x;
                        x = wcount > x ? wcount : x;
                        x = ecount > x ? ecount : x;
                        x = rcount > x ? rcount : x;
                        if (Utils.GameTimeTickCount - x >= 9250 && _q.IsReady())
                        {
                            _q.Cast(Player.Position);
                        }
                    }
                }
            }
            //draw burst
            if (DrawBurstStatus)
            {
                if (candocombo())
                {
                    if (notifyselected.Text == "Burst Ready")
                    {
                        return;
                    }
                    else
                    {
                        Notifications.RemoveNotification(notifyselected);
                        notifyselected = new Notification("Burst Ready");
                        Notifications.AddNotification(notifyselected);
                    }
                }
                else if (candowaitcombo(1000))
                {
                    if (notifyselected.Text == "Burst Ready in 1s")
                    {
                        return;
                    }
                    else
                    {
                        Notifications.RemoveNotification(notifyselected);
                        notifyselected = new Notification("Burst Ready in 1s");
                        Notifications.AddNotification(notifyselected);
                    }
                }
                else
                {
                    if (notifyselected.Text == "Burst Not Ready")
                    {
                        return;
                    }
                    else
                    {
                        Notifications.RemoveNotification(notifyselected);
                        notifyselected = new Notification("Burst Not Ready");
                        Notifications.AddNotification(notifyselected);
                    }
                }
            }
            else
            {
                Notifications.RemoveNotification(notifyselected);
            }
        }
        private static Notification notifyselected = new Notification("Burst Not Ready");
        private static int qcount, ecount, wcount, rcount;
    }
}
