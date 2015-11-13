using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using xSLx_Orbwalker;
using Color = System.Drawing.Color;
using Menu = LeagueSharp.Common.Menu;
using MenuItem = LeagueSharp.Common.MenuItem;

namespace Ultimate_Carry_Prevolution.Plugin
{
    class Syndra : Champion
    {
        public Syndra()
        {
            SetSpells();
            LoadMenu();
        }

        //in progress

        private Spell QE;

        private void SetSpells()
        {
            Q = new Spell(SpellSlot.Q, 800);
            Q.SetSkillshot(0, 130f, 2000f, false, SkillshotType.SkillshotCircle);

            W = new Spell(SpellSlot.W, 925);
            W.SetSkillshot(0, 140f, 2000f, false, SkillshotType.SkillshotCircle);

            E = new Spell(SpellSlot.E, 700);
            E.SetSkillshot(250, (float)(45 * 0.5), 2500f, false, SkillshotType.SkillshotCircle);

            R = new Spell(SpellSlot.R, 675);

            QE = new Spell(SpellSlot.Q, 1250);
            QE.SetSkillshot(400, 60f, 1000f, false, SkillshotType.SkillshotCircle);

        }

        private void LoadMenu()
        {
            var champMenu = new Menu("Syndra Plugin", "Syndra");
            {
                var SpellMenu = new Menu("SpellMenu", "SpellMenu");
                {
                    var qMenu = new Menu("QMenu", "QMenu");
                    {
                        qMenu.AddItem(new MenuItem("Q_Auto_Immobile", "Auto Q on Immobile").SetValue(true));
                        SpellMenu.AddSubMenu(qMenu);
                    }

                    var wMenu = new Menu("WMenu", "WMenu");
                    {
                        wMenu.AddItem(new MenuItem("W_Only_Orb", "Only Pick Up Orb").SetValue(false));
                        SpellMenu.AddSubMenu(wMenu);
                    }
                    var rMenu = new Menu("RMenu", "RMenu");
                    {
                        rMenu.AddItem(new MenuItem("R_Overkill_Check", "Overkill Check").SetValue(true));

                        rMenu.AddSubMenu(new Menu("Don't use R on", "Dont_R"));
                        foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != MyHero.Team)
                        )
                            rMenu.SubMenu("Dont_R")
                                .AddItem(new MenuItem("Dont_R" + enemy.BaseSkinName, enemy.BaseSkinName).SetValue(false));

                        SpellMenu.AddSubMenu(rMenu);
                    }

                    champMenu.AddSubMenu(SpellMenu);
                }

                var comboMenu = new Menu("Combo", "Combo");
                {
                    AddSpelltoMenu(comboMenu, "Q", true);
                    AddSpelltoMenu(comboMenu, "QE", true, "Use QE");
                    AddSpelltoMenu(comboMenu, "W", true);
                    AddSpelltoMenu(comboMenu, "E", true);
                    AddSpelltoMenu(comboMenu, "R", true);
                    AddSpelltoMenu(comboMenu, "Ignite", true, "Use Ignite");
                    champMenu.AddSubMenu(comboMenu);
                }

                var harassMenu = new Menu("Harass", "Harass");
                {
                    AddSpelltoMenu(harassMenu, "Q", true);
                    AddSpelltoMenu(harassMenu, "QE", true, "Use QE");
                    AddSpelltoMenu(harassMenu, "W", true);
                    AddSpelltoMenu(harassMenu, "E", true);
                    AddManaManagertoMenu(harassMenu, 30);
                    champMenu.AddSubMenu(harassMenu);
                }

                var laneClearMenu = new Menu("LaneClear", "LaneClear");
                {
                    AddSpelltoMenu(laneClearMenu, "Q", true);
                    AddSpelltoMenu(laneClearMenu, "W", true);
                    AddSpelltoMenu(laneClearMenu, "E", true);
                    AddManaManagertoMenu(laneClearMenu, 30);
                    champMenu.AddSubMenu(laneClearMenu);
                }

                var miscMenu = new Menu("Misc", "Misc");
                {
                    miscMenu.AddItem(new MenuItem("Misc_QE_Mouse", "QE to mouse").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
                    miscMenu.AddItem(new MenuItem("QE_Interrupt", "Use QE to Interrupt").SetValue(true));
                    miscMenu.AddItem(new MenuItem("E_Gap_Closer", "Use E On Gap Closer").SetValue(true));
                    champMenu.AddSubMenu(miscMenu);
                }

                var drawMenu = new Menu("Drawing", "Drawing");
                {
                    drawMenu.AddItem(new MenuItem("Draw_Disabled", "Disable All").SetValue(false));
                    drawMenu.AddItem(new MenuItem("Draw_Q", "Draw Q").SetValue(true));
                    drawMenu.AddItem(new MenuItem("Draw_QE", "Draw QE").SetValue(true));
                    drawMenu.AddItem(new MenuItem("Draw_W", "Draw W").SetValue(true));
                    drawMenu.AddItem(new MenuItem("Draw_E", "Draw E").SetValue(true));
                    drawMenu.AddItem(new MenuItem("Draw_R", "Draw R").SetValue(true));
                    drawMenu.AddItem(new MenuItem("Draw_R_Killable", "Draw R Mark on Killable").SetValue(true));

                    MenuItem drawComboDamageMenu = new MenuItem("Draw_ComboDamage", "Draw Combo Damage").SetValue(true);
                    drawMenu.AddItem(drawComboDamageMenu);
                    Utility.HpBarDamageIndicator.DamageToUnit = GetComboDamage;
                    Utility.HpBarDamageIndicator.Enabled = drawComboDamageMenu.GetValue<bool>();
                    drawComboDamageMenu.ValueChanged +=
                        delegate(object sender, OnValueChangeEventArgs eventArgs)
                        {
                            Utility.HpBarDamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
                        }; 

                    champMenu.AddSubMenu(drawMenu);
                }
            }

            Menu.AddSubMenu(champMenu);
            Menu.AddToMainMenu();
        }

        private IEnumerable<SpellSlot> GetSpellCombo()
        {
            var spellCombo = new List<SpellSlot>();
            if (Q.IsReady())
                spellCombo.Add(SpellSlot.Q);
            if (W.IsReady())
                spellCombo.Add(SpellSlot.W);
            if (E.IsReady())
                spellCombo.Add(SpellSlot.E);
            return spellCombo;
        }

        private float GetComboDamage(Obj_AI_Base target)
        {
            double comboDamage = (float)ObjectManager.Player.GetComboDamage(target, GetSpellCombo());

            comboDamage += Get_Ult_Dmg(target);

            if (Ignite != SpellSlot.Unknown && MyHero.SummonerSpellbook.CanUseSpell(Ignite) == SpellState.Ready)
                comboDamage += MyHero.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);

            return (float)(comboDamage + ObjectManager.Player.GetAutoAttackDamage(target));
        }

        private float Get_Ult_Dmg(Obj_AI_Base enemy)
        {
            var damage = 0d;

            if (DFG.IsReady())
                damage += MyHero.GetItemDamage(enemy, Damage.DamageItems.Dfg) / 1.2;

            if (R.IsReady())
                damage += (3 + getOrbCount()) * MyHero.GetSpellDamage(enemy, SpellSlot.R, 1) - 20;

            return (float)damage * (DFG.IsReady() ? 1.2f : 1);
        }

        public override void OnDraw()
        {
            if (Menu.Item("Draw_Disabled").GetValue<bool>())
            {
                xSLxOrbwalker.DisableDrawing();
                return;
            }
            xSLxOrbwalker.EnableDrawing();
            if (Menu.Item("Draw_Q").GetValue<bool>())
                if (Q.Level > 0)
                    Utility.DrawCircle(MyHero.Position, Q.Range, Q.IsReady() ? Color.Green : Color.Red);
            if (Menu.Item("Draw_QE").GetValue<bool>())
                if (Q.Level > 0 && E.Level >0)
                    Utility.DrawCircle(MyHero.Position, QE.Range, Q.IsReady() && E.IsReady() ? Color.Green : Color.Red);
            if (Menu.Item("Draw_W").GetValue<bool>())
                if (W.Level > 0)
                    Utility.DrawCircle(MyHero.Position, W.Range, W.IsReady() ? Color.Green : Color.Red);

            if (Menu.Item("Draw_E").GetValue<bool>())
                if (E.Level > 0)
                    Utility.DrawCircle(MyHero.Position, E.Range, E.IsReady() ? Color.Green : Color.Red);

            if (Menu.Item("Draw_R").GetValue<bool>())
                if (R.Level > 0)
                    Utility.DrawCircle(MyHero.Position, R.Range, R.IsReady() ? Color.Green : Color.Red);

            if (Menu.Item("Draw_R_Killable").GetValue<bool>() && R.IsReady())
            {
                foreach (var unit in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsValidTarget(2000) && !x.IsDead && x.IsEnemy).OrderBy(x => x.Health))
                {
                    var health = unit.Health + unit.HPRegenRate + 10;
                    if (Get_Ult_Dmg(unit) > health)
                    {
                        Vector2 wts = Drawing.WorldToScreen(unit.Position);
                        Drawing.DrawText(wts[0] - 20, wts[1], Color.White, "KILL!!!");
                    }
                }
            }
        }

        public override void OnPassive()
        {

            if (Menu.Item("Misc_QE_Mouse").GetValue<KeyBind>().Active)
            {
                var vec = MyHero.ServerPosition + Vector3.Normalize(Game.CursorPos - MyHero.ServerPosition)*(E.Range - 50);
                QE.Cast(vec, UsePackets());
                QE.LastCastAttemptT = Environment.TickCount;
            }
                
            var Q_Target = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
            if (Menu.Item("Q_Auto_Immobile").GetValue<bool>() && Q_Target != null)
                if (Q.GetPrediction(Q_Target).Hitchance == HitChance.Immobile)
                    Q.Cast(Q_Target);
        }

        public override void OnCombo()
        {
            var Q_Target = SimpleTs.GetTarget(650, SimpleTs.DamageType.Magical);

            if (IsSpellActive("R") && R.IsReady())
                Cast_R();

            if (IsSpellActive("Q") && Q.IsReady())
                Cast_Q();


            if (IsSpellActive("W") && W.IsReady())
                Cast_W(true);

            if (Q_Target != null)
            if (GetComboDamage(Q_Target) >= Q_Target.Health && IsSpellActive("Ignite") && Ignite != SpellSlot.Unknown  && MyHero.Distance(Q_Target) < 650 &&
                    MyHero.SummonerSpellbook.CanUseSpell(Ignite) == SpellState.Ready)
                Use_Ignite(Q_Target);

            if (IsSpellActive("QE") && E.IsReady() && Q.IsReady())
                Cast_QE();

            if (IsSpellActive("E") && E.IsReady())
                Cast_E();


        }

        public override void OnHarass()
        {
            if (ManaManagerAllowCast())
            {
                if (IsSpellActive("Q") && Q.IsReady())
                    Cast_Q();
                if (IsSpellActive("W") && W.IsReady())
                    Cast_W(true);
                if (IsSpellActive("E") && E.IsReady())
                    Cast_E();
                if (IsSpellActive("QE") && E.IsReady() && Q.IsReady())
                    Cast_QE();
            }
        }

        public override void OnLaneClear()
        {
            if (ManaManagerAllowCast())
            {
                if (IsSpellActive("Q") && Q.IsReady())
                    Cast_BasicSkillshot_AOE_Farm(Q);
                if (IsSpellActive("W") && W.IsReady())
                    Cast_W(false);
                if (IsSpellActive("E") && E.IsReady())
                {
                    Cast_BasicSkillshot_AOE_Farm(E, 100);
                    W.LastCastAttemptT = Environment.TickCount + 500;
                }
            }
        }

        public override void OnGapClose(ActiveGapcloser gapcloser)
        {
            if (!Menu.Item("E_Gap_Closer").GetValue<bool>()) return;

            if (E.IsReady() && gapcloser.Sender.IsValidTarget(E.Range))
            {
                E.Cast(gapcloser.Sender, UsePackets());
                W.LastCastAttemptT = Environment.TickCount + 500;
            }
        }

     
        public override void OnProcessSpell(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs spell)
        {
            if (unit.IsMe && E.IsReady() && (spell.SData.Name == "SyndraQ") && Environment.TickCount - QE.LastCastAttemptT < 300)
            {
                E.Cast(spell.End, UsePackets());
                W.LastCastAttemptT = Environment.TickCount + 500;
            }
        }
        public override void OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (spell.DangerLevel < InterruptableDangerLevel.Medium || unit.IsAlly)
                return;

            if (Menu.Item("QE_Interrupt").GetValue<bool>() && unit.IsValidTarget(QE.Range))
                Cast_QE(unit);
        }

        private void Cast_Q()
        {
            var Q_Target = SimpleTs.GetTarget(Q.Range + 100, SimpleTs.DamageType.Magical);

            Q.UpdateSourcePosition();
            if (Q.IsReady() && Q_Target != null && Q.GetPrediction(Q_Target).Hitchance >= HitChance.High)
                Q.Cast(Q_Target, UsePackets());
        }

        private void Cast_W(bool mode)
        {
            if (mode)
            {
                var W_Target = SimpleTs.GetTarget(W.Range + 100, SimpleTs.DamageType.Magical);

                var Grabbable_Obj = Get_Nearest_orb();
                var W_Toggle_State = MyHero.Spellbook.GetSpell(SpellSlot.W).ToggleState;

                if (W_Target != null)
                {
                    if (W_Toggle_State == 1 && Environment.TickCount - W.LastCastAttemptT > Game.Ping && W.IsReady() &&
                        Grabbable_Obj != null)
                    {
                        W.Cast(Grabbable_Obj.ServerPosition);
                        W.LastCastAttemptT = Environment.TickCount + 500;
                        return;
                    }

                    W.UpdateSourcePosition(Get_Current_Orb().ServerPosition, Get_Current_Orb().ServerPosition);
                    if (MyHero.Distance(W_Target) < E.Range)
                    {
                        if (W_Toggle_State != 1 && W.IsReady() && W.GetPrediction(W_Target).Hitchance >= HitChance.High &&
                            Environment.TickCount - W.LastCastAttemptT > Game.Ping + 100)
                        {
                            W.Cast(W_Target);
                        }
                    }

                    if (W_Toggle_State != 1 && W.IsReady() && W.GetPrediction(W_Target).Hitchance >= HitChance.High)
                    {
                        W.Cast(W_Target);
                    }
                }
            }
            else
            {
                var allMinionsW = MinionManager.GetMinions(MyHero.ServerPosition, W.Range + W.Width + 20, MinionTypes.All, MinionTeam.NotAlly);

                var Grabbable_Obj = Get_Nearest_orb();
                var W_Toggle_State = MyHero.Spellbook.GetSpell(SpellSlot.W).ToggleState;

                if (W_Toggle_State == 1 && Environment.TickCount - W.LastCastAttemptT > Game.Ping && W.IsReady() &&
                        Grabbable_Obj != null)
                {
                    W.Cast(Grabbable_Obj.ServerPosition);
                    W.LastCastAttemptT = Environment.TickCount + 1000;
                    return;
                }

                W.UpdateSourcePosition(Get_Current_Orb().ServerPosition, Get_Current_Orb().ServerPosition);

                var farmLocation = W.GetCircularFarmLocation(allMinionsW);

                if (farmLocation.MinionsHit > 2)
                    W.Cast(farmLocation.Position);
            }
        }

        private void Cast_E()
        {
            if (getOrbCount() > 0)
            {
                var target = SimpleTs.GetTarget(QE.Range + 100, SimpleTs.DamageType.Magical);

                foreach (var orb in getOrb().Where(x => MyHero.Distance(x) < E.Range))
                {
                    var Start_Pos = orb.ServerPosition;
                    var End_Pos = MyHero.ServerPosition + (Start_Pos - MyHero.ServerPosition)*QE.Range;

                    E.UpdateSourcePosition();
                    var Target_Pos = QE.GetPrediction(target);

                    var projection = Geometry.ProjectOn(Target_Pos.UnitPosition.To2D(), Start_Pos.To2D(), End_Pos.To2D());

                    if (projection.IsOnSegment && E.IsReady() && Target_Pos.Hitchance >= HitChance.High && 
                        projection.LinePoint.Distance(Target_Pos.UnitPosition.To2D()) < QE.Width + target.BoundingRadius)
                    {
                        E.Cast(orb.ServerPosition, UsePackets());
                        W.LastCastAttemptT = Environment.TickCount + 500;
                        return;
                    }
                }
            }
            /*For when riot makes it to where i can stun enemy by knocking them into ball!
            foreach (var enemy in AllHerosEnemy.Where(x => MyHero.Distance(x) < E.Range))
            {
                foreach (var orb in getOrb().Where(x => enemy.Distance(x) < E.Range))
                {
                    var Start_Pos = enemy.ServerPosition;
                    var End_Pos = MyHero.ServerPosition + (enemy.ServerPosition - MyHero.ServerPosition) * Q.Range;
                    
                    var projection = Geometry.ProjectOn(orb.ServerPosition.To2D(), Start_Pos.To2D(), End_Pos.To2D());

                    if (projection.IsOnSegment &&
                            projection.LinePoint.Distance(orb.ServerPosition.To2D()) < EQ.Width + target.BoundingRadius)
                        E.Cast(enemy, UsePackets());
                }
            }*/
        }

        private void Cast_R()
        {
            var R_Target = SimpleTs.GetTarget(R.Range + 100, SimpleTs.DamageType.Magical);

            if (R_Target != null)
            {
                if (Menu.Item("Dont_R" + R_Target.BaseSkinName) != null)
                {
                    if (!Menu.Item("Dont_R" + R_Target.BaseSkinName).GetValue<bool>())
                    {
                        if (Menu.Item("R_Overkill_Check").GetValue<bool>())
                        {
                            if (MyHero.GetSpellDamage(R_Target, SpellSlot.Q) > R_Target.Health)
                            {

                            }
                            else if (Get_Ult_Dmg(R_Target) > R_Target.Health + 20 && R_Target.Distance(MyHero) < R.Range)
                            {
                                if (DFG.IsReady())
                                    Use_DFG(R_Target);

                                R.CastOnUnit(R_Target, UsePackets());
                            }
                        }
                        else if (Get_Ult_Dmg(R_Target) > R_Target.Health - 20 && R_Target.Distance(MyHero) < R.Range)
                        {
                            if (DFG.IsReady())
                                Use_DFG(R_Target);

                            R.CastOnUnit(R_Target, UsePackets());
                        }
                    }
                }
            }
        }

        private void Cast_QE(Obj_AI_Base target = null)
        {
            var QE_Target = SimpleTs.GetTarget(QE.Range, SimpleTs.DamageType.Magical);

            if (target != null)
                QE_Target = (Obj_AI_Hero) target;

            if (QE_Target != null)
            {
                QE.UpdateSourcePosition();

                var QE_Pred = QE.GetPrediction(QE_Target);

                var Pred_Vec = MyHero.ServerPosition + Vector3.Normalize(QE_Pred.UnitPosition - MyHero.ServerPosition)*(E.Range - 100);

                //Utility.DrawCircle(Pred_Vec, 50, Color.Red);

                if (QE_Pred.Hitchance >= HitChance.High && Q.IsReady() && E.IsReady())
                {
                    Q.Cast(Pred_Vec, UsePackets());
                    QE.LastCastAttemptT = Environment.TickCount;
                }
            }
        }

        private int getOrbCount()
        {
            return
                ObjectManager.Get<Obj_AI_Minion>().Count(obj => obj.IsValid && obj.Team == ObjectManager.Player.Team && obj.Name == "Seed");
        }

        private IEnumerable<Obj_AI_Minion> getOrb()
        {
            return ObjectManager.Get<Obj_AI_Minion>().Where(obj => obj.IsValid && obj.Team == ObjectManager.Player.Team && obj.Name == "Seed").ToList();
        }

        private Obj_AI_Minion Get_Nearest_orb()
        {
            var Orb =
                ObjectManager.Get<Obj_AI_Minion>()
                    .Where(obj => obj.IsValid && obj.Team == ObjectManager.Player.Team && obj.Name == "Seed")
                    .ToList()
                    .OrderBy(x => MyHero.Distance(x))
                    .FirstOrDefault();
            if(Orb != null)
                return Orb;

            if (!Menu.Item("W_Only_Orb").GetValue<bool>())
            {
                var minion = ObjectManager.Get<Obj_AI_Minion>().FirstOrDefault(x => x.IsValidTarget(W.Range));

                if (minion != null)
                    return minion;
            }
            return null;
        }

        private Obj_AI_Base Get_Current_Orb()
        {
            var orb = ObjectManager.Get<Obj_AI_Minion>().FirstOrDefault(x => x.Team == MyHero.Team && x.Name == "Seed" && !x.IsTargetable);

            if (orb != null)
                return orb;

            var minion = ObjectManager.Get<Obj_AI_Minion>().FirstOrDefault(x => x.IsInvulnerable && x.Name != "Seed" && x.Name != "k");

            if(minion != null)
                return minion;

            return null;
        }

    }
}
