﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;
using System.Drawing;
using SharpDX;

namespace Yasuo_Sharpino
{
    class Yasuo
    {



        public static Obj_AI_Hero Player = ObjectManager.Player;

        public static Vector3 test = new Vector3();

        public static Spellbook sBook = Player.Spellbook;

        public static SpellDataInst Qdata = sBook.GetSpell(SpellSlot.Q);
        public static SpellDataInst Wdata = sBook.GetSpell(SpellSlot.W);
        public static SpellDataInst Edata = sBook.GetSpell(SpellSlot.E);
        public static SpellDataInst Rdata = sBook.GetSpell(SpellSlot.R);
        public static Spell Q = new Spell(SpellSlot.Q, 475);
        public static Spell QEmp = new Spell(SpellSlot.Q, 900);
        public static Spell QCir = new Spell(SpellSlot.Q, 375);
        public static Spell W = new Spell(SpellSlot.W, 400);
        public static Spell E = new Spell(SpellSlot.E, 475);
        public static Spell R = new Spell(SpellSlot.R, 1200);


        public static Vector3 point1 = new Vector3();
        public static Vector3 point2 = new Vector3();

        public static void setSkillShots()
        {
            Q.SetSkillshot(0.25f, 50f, 1800f, false, Prediction.SkillshotType.SkillshotLine);
            QEmp.SetSkillshot(0.25f, 50f, 1200f, false, Prediction.SkillshotType.SkillshotLine);
            //QCir.SetSkillshot(0.25f, 50f, 1200f, false, Prediction.SkillshotType.SkillshotCircle);
        }

        public static void doCombo(Obj_AI_Hero target)
        {
            if (target == null) return;

            
            if (!useESmart(target))
            {
                List<Obj_AI_Hero> ign = new List<Obj_AI_Hero>();
                ign.Add(target);
                gapCloseE(target.Position.To2D(), ign);
            }
            useQSmart(target);
        }

        public static void doLastHit(Obj_AI_Hero target)
        {
            if (!Orbwalking.CanAttack())
                return;
            var minions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range + 50);
            foreach ( var minion in minions.Where( minion => minion.IsValidTarget(Q.Range)))
            {
                if (Player.Distance(minion) < Orbwalking.GetRealAutoAttackRange(minion) && minion.Health < DamageLib.CalcPhysicalMinionDmg((double)(Player.BaseAttackDamage + Player.FlatPhysicalDamageMod), (Obj_AI_Minion)minion, true))
                    return;
                if (YasuoSharp.Config.Item("useElh").GetValue<bool>() && minion.Health < DamageLib.getDmg(minion, DamageLib.SpellType.E))
                    useENormal(minion);

                if (YasuoSharp.Config.Item("useQlh").GetValue<bool>() && !isQEmpovered() && minion.Health < DamageLib.getDmg(minion, DamageLib.SpellType.Q))
                    if (!(target != null && isQEmpovered() && Player.Distance(target) < 1050))
                        Q.CastIfWillHit(minion,1, false);
            }
        }

        public static void doLaneClear(Obj_AI_Hero target)
        {
            
            List<Obj_AI_Base> minions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range + 50);

            if (Q.IsReady() && !isDashing() && YasuoSharp.Config.Item("useQlc").GetValue<bool>())
            {
                if (isQEmpovered() && !(target != null && Player.Distance(target) < 1050))
                {
                    List<Vector2> minionPs = MinionManager.GetMinionsPredictedPositions(minions, 0.25f, 50f, 1200f, Player.ServerPosition, 900f, false, Prediction.SkillshotType.SkillshotLine);
                    MinionManager.FarmLocation farm = QEmp.GetLineFarmLocation(minionPs); //MinionManager.GetBestLineFarmLocation(minionPs, 50f, 900f);
                    //Console.WriteLine(farm.MinionsHit);
                    if (farm.MinionsHit >= 3)
                        QEmp.Cast(farm.Position, false);
                }
                else
                {

                    if (!Orbwalking.CanAttack())
                        return;
                    List<Vector2> minionPs = MinionManager.GetMinionsPredictedPositions(minions, 0.25f, 30f, 1800f, Player.ServerPosition, 475, false, Prediction.SkillshotType.SkillshotLine);
                    //MinionManager.FarmLocation farm = Q.GetLineFarmLocation(minionPs); //MinionManager.GetBestLineFarmLocation(minionPs, 50f, 900f);
                    Vector2 clos = Geometry.Closest(Player.ServerPosition.To2D(), minionPs);
                    //if (farm.MinionsHit > 1)
                    if(Player.Distance(clos)<475)
                        Q.Cast(clos, false);
                }
            }
            if(YasuoSharp.Config.Item("useWlc").GetValue<bool>())
                foreach (var minion in minions.Where(minion => minion.IsValidTarget(Q.Range)))
                {
                    if (minion.Health < DamageLib.getDmg(minion, DamageLib.SpellType.E))
                    {
                        useENormal(minion);
                        return;
                    }
                }
        }

        public static void doHarass(Obj_AI_Hero target)
        {
            useQSmart(target);
        }


        public static bool inTowerRange(Vector2 pos)
        {
            if (!YasuoSharp.Config.Item("djTur").GetValue<bool>())
                return false;
            foreach (Obj_AI_Turret tur in ObjectManager.Get<Obj_AI_Turret>().Where(tur => tur.IsEnemy && tur.Health > 0))
            {
                if (pos.Distance(tur.Position.To2D()) < (775+Player.BoundingRadius))
                    return true;
            }
            return false;
        }

        public static Vector3 getDashEndPos()
        {
            Vector2 dashPos2 = Player.GetDashInfo().EndPos;
            return new Vector3(dashPos2, Player.ServerPosition.Z);
        }

        public static bool isQEmpovered()
        {
            return Player.HasBuff("yasuoq3w", true);
        }

        public static bool isDashing()
        {
            return Player.IsDashing();
        }

        public static bool canCastFarQ()
        {
            return !Player.IsDashing();
        }

        public static bool canCastCircQ()
        {
            return Player.IsDashing();
        }

        public static void useQSmart(Obj_AI_Hero target)
        {
            if (!Q.IsReady())
                return;

            if (isQEmpovered())
            {
                if (canCastFarQ())
                {
                    Prediction.PredictionOutput po = Prediction.GetBestAOEPosition(target, 0.25f, 50f, 1800f, Player.ServerPosition, 900f, false, Prediction.SkillshotType.SkillshotLine); //QEmp.GetPrediction(target, true);
                    if (po.HitChance >= Prediction.HitChance.HighHitchance && Player.Distance(po.CastPosition) < 900)
                    {
                        Q.Cast(po.CastPosition);
                    }
                }
                else//dashing
                {
                    float trueRange = QCir.Range-10;
                    Vector3 endPos = getDashEndPos();
                    if (Player.Distance(endPos) < 10 && target.Distance(endPos) < trueRange)
                    {
                        QCir.Cast(target.Position);
                    }
                } 
            }
            else
            {
                if (canCastFarQ())
                {
                    Q.CastIfWillHit(target, 1);
                }
                else//dashing
                {
                    float trueRange = QCir.Range-10;
                    Vector3 endPos = getDashEndPos();
                    if (Player.Distance(endPos) < 5 && target.Distance(endPos) < trueRange)
                    {
                        QCir.Cast(target.Position);
                    }
                }
            }
        }

        public static void useWSmart(Obj_SpellMissile missle)
        {
           // Missle mis = new Missle(missle, obj);
           // if (Player.Distance(missle.Position) > 400)
           // {
            //Console.WriteLine("izsauca");
            if (missle.SpellCaster is Obj_AI_Hero && missle.IsEnemy)
            {
                Obj_AI_Base enemHero = missle.SpellCaster;
                float dmg = (enemHero.BaseAttackDamage + enemHero.FlatPhysicalDamageMod);
                if (missle.SData.Name.Contains("Crit"))
                    dmg *= 2;

                //if(dmg!=0)
               // Console.WriteLine("Dmg: " + Player.PercentArmorMod);

                if (!missle.SData.Name.Contains("Attack") || (enemHero.CombatType == GameObjectCombatType.Ranged && dmg > Player.MaxHealth / 8))
                {
                    if (YasMath.DistanceFromPointToLine(missle.SpellCaster.Position.To2D(), missle.EndPosition.To2D(), Yasuo.Player.ServerPosition.To2D()) < (Player.BoundingRadius + missle.BoundingRadius+20))
                    {
                        Vector3 blockWhere = missle.Position;//Player.Position + Vector3.Normalize(missle.SpellCaster.Position- Player.Position)*15;
                        //Console.WriteLine(missle.SData.LineWidth + Player.BoundingRadius);
                        if (Player.Distance(missle.Position) < 420)
                        {
                            //Console.WriteLine(missle.IsEnemy);
                            if (W.IsReady())
                            {
                                W.Cast(blockWhere, true);
                                YasuoSharp.skillShots.Clear();
                            }
                        }
                    }
                }
            }
           // }
        }

        public static void useENormal(Obj_AI_Base target)
        {
            Vector2 pPos = Player.ServerPosition.To2D();
            Vector2 posAfterE = pPos + (Vector2.Normalize(target.Position.To2D() - pPos) * E.Range);
            if(!inTowerRange(posAfterE))
                E.Cast(target, false);
        }

        public static bool useESmart(Obj_AI_Hero target,List<Obj_AI_Hero> ignore = null)
        {
            if (!E.IsReady())
                return false;
            float trueAARange = Player.AttackRange + target.BoundingRadius;
            float trueERange = target.BoundingRadius + E.Range;

            float dist = Player.Distance(target);
            Vector2 dashPos = new Vector2();
            if (target.IsMoving)
            {
                Vector2 tpos = target.Position.To2D() ;
                Vector2 path = target.Path[0].To2D() - tpos;
                path.Normalize();
                dashPos = tpos + (path * 100);
            }
            float targ_ms = (target.IsMoving && Player.Distance(dashPos) > dist) ? target.MoveSpeed : 0;
            float msDif = (Player.MoveSpeed - targ_ms) == 0 ? 0.0001f : (Player.MoveSpeed - targ_ms);
            float timeToReach = (dist-trueAARange) / msDif;
            //Console.WriteLine(timeToReach);
            if (dist > trueAARange && dist < trueERange)
            {
                if (timeToReach > 1.7f || timeToReach<0.0f)
                {
                    useENormal(target);
                    return true;
                }
            }
            return false;
        }

        public static void gapCloseE(Vector2 pos, List<Obj_AI_Hero> ignore = null)
        {
            if (!E.IsReady())
                return;
            Vector2 pPos = Player.ServerPosition.To2D();
            Obj_AI_Base bestEnem = null;
            Vector2 bestLoc = pPos + (Vector2.Normalize(pos - pPos) * (Player.MoveSpeed * 0.35f));
            float bestDist = pos.Distance(bestLoc);
            foreach (Obj_AI_Base enemy in ObjectManager.Get<Obj_AI_Base>().Where(enemy => enemyIsJumpable(enemy, ignore)))
            {
                float trueRange = E.Range + enemy.BoundingRadius;
                float distToEnem = Player.Distance(enemy);
                if (distToEnem < trueRange && distToEnem>15)
                {
                    Vector2 posAfterE = pPos + (Vector2.Normalize(enemy.Position.To2D() - pPos) * E.Range);
                    float distE = pos.Distance(posAfterE);
                    if (distE < bestDist)
                    {
                        bestLoc = posAfterE;
                        bestDist = distE;
                        bestEnem = enemy;
                    }
                }
            }
            if (bestEnem != null)
                useENormal(bestEnem);

        }

        public static bool enemyIsJumpable(Obj_AI_Base enemy, List<Obj_AI_Hero> ignore = null)
        {
            if(enemy.IsValid && enemy.IsEnemy && !enemy.IsInvulnerable && !enemy.MagicImmune && !enemy.IsDead)
            {
            
                 if (ignore != null )
                     foreach (Obj_AI_Hero ign in ignore)
                     {
                         if(ign.NetworkId == enemy.NetworkId)
                             return false;
                     }
                   

                 foreach(BuffInstance buff in enemy.Buffs)
                 {
                     if (buff.Name == "YasuoDashWrapper")
                         return false;
                 }
                 return true;
            }
            return false;
        }

        public static float getSpellCastTime(Spell spell)
        {
            return sBook.GetSpell(spell.Slot).SData.SpellCastTime;
        }

        public static float getSpellCastTime(SpellSlot slot)
        {
            return sBook.GetSpell(slot).SData.SpellCastTime;
        }
    }
}
