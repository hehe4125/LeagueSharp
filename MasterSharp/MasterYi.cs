using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace MasterSharp
{
    class MasterYi
    {
        public static Obj_AI_Hero player = ObjectManager.Player;

        public static SummonerItems sumItems = new SummonerItems(player);

        public static Spellbook sBook = player.Spellbook;

        public static SpellDataInst Qdata = sBook.GetSpell(SpellSlot.Q);
        public static SpellDataInst Wdata = sBook.GetSpell(SpellSlot.W);
        public static SpellDataInst Edata = sBook.GetSpell(SpellSlot.E);
        public static SpellDataInst Rdata = sBook.GetSpell(SpellSlot.R);
        public static Spell Q = new Spell(SpellSlot.Q, 600);
        public static Spell W = new Spell(SpellSlot.W, 0);
        public static Spell E = new Spell(SpellSlot.E, 0);
        public static Spell R = new Spell(SpellSlot.R, 0);


        public static Obj_AI_Base selectedTarget = null;

        public static void setSkillShots()
        {

        }


        public static void slayMaderDuker(Obj_AI_Base target)
        {
            try
            {
                if (target == null)
                    return;

                useHydra(target);
                if (target.Distance(player) < 500)
                {
                    sumItems.cast(SummonerItems.ItemIds.Ghostblade);
                }
                if (target.Distance(player) < 500 && (player.Health / player.MaxHealth) * 100 < 85)
                {
                    sumItems.cast(SummonerItems.ItemIds.BotRK, target);

                }
                useQSmart(target);
                useESmart(target);
                useRSmart(target);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public static void useHydra(Obj_AI_Base target)
        {

            if ((Items.CanUseItem(3074) || Items.CanUseItem(3074)) && target.Distance(player.ServerPosition) < (400 + target.BoundingRadius - 20))
            {
                Items.UseItem(3074, target);
                Items.UseItem(3077, target);
            }
        }
        public static void useQtoKill(Obj_AI_Base target)
        {
            if (Q.IsReady() && (target.Health <= Q.GetDamage(target) || iAmLow(0.20f)))
                Q.Cast(target, MasterSharp.Config.Item("packets").GetValue<bool>());
        }

        public static void useESmart(Obj_AI_Base target)
        {
            if (LXOrbwalker.InAutoAttackRange(target) && E.IsReady() && (aaToKill(target)>2 || iAmLow()))
                E.Cast(MasterSharp.Config.Item("packets").GetValue<bool>());
        }

        public static void useRSmart(Obj_AI_Base target)
        {
            if (LXOrbwalker.InAutoAttackRange(target) && R.IsReady() && aaToKill(target) > 5)
                R.Cast(MasterSharp.Config.Item("packets").GetValue<bool>());
        }

        public static bool useQSmart(Obj_AI_Base target)
        {
            try
            {
                if (!Q.IsReady())
                    return false;
                float trueAARange = player.AttackRange + target.BoundingRadius;

                float dist = player.Distance(target);
                Vector2 dashPos = new Vector2();
                if (!target.IsMoving || target.Path.Count() == 0)
                    return false;

                Vector2 tpos = target.Position.To2D();
                Vector2 path = target.Path[0].To2D() - tpos;
                path.Normalize();
                dashPos = tpos + (path * 20);
                float myMs = player.MoveSpeed;
                float targ_ms = (target.IsMoving && player.Distance(dashPos) > dist) ? target.MoveSpeed : 0;
                float msDif = (myMs - targ_ms) == 0 ? 0.0001f : (myMs - targ_ms);
                float timeToReach = (dist - trueAARange) / msDif;
                Console.WriteLine("TimeTO reach " + timeToReach);
                if (dist > trueAARange+50 && dist < 600)
                {
                    if (timeToReach > 1.7f || timeToReach < -1.0f)
                    {
                        Q.Cast(target, MasterSharp.Config.Item("packets").GetValue<bool>());
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return false;
        }

        public static bool iAmLow(float lownes = .25f)
        {
            return player.Health / player.MaxHealth < lownes;
        }

        public static int aaToKill(Obj_AI_Base target)
        {
            return 1+(int)(target.Health/player.GetAutoAttackDamage(target));
        }

        public static void evadeSkillShot(Skillshot sShot)
        {
            var sd = SpellDatabase.GetByMissileName(sShot.SpellData.MissileSpellName);
            if (LXOrbwalker.CurrentMode == LXOrbwalker.Mode.Combo && (MasterSharp.skillShotMustBeEvaded(sd.MenuItemName) || MasterSharp.skillShotMustBeEvadedW(sd.MenuItemName)))
            {
                float spellDamage = (float)sShot.Unit.GetSpellDamage(player, sd.SpellName);
                int procHp = (int)((spellDamage / player.MaxHealth) * 100);
                bool willKill = player.Health <= spellDamage;
                if (Q.IsReady() && (MasterSharp.skillShotMustBeEvaded(sd.MenuItemName) || willKill))
                {
                    useQonBest();
                    return;
                }
                else if ((!Q.IsReady(150) || !MasterSharp.skillShotMustBeEvaded(sd.MenuItemName)) && W.IsReady() && (MasterSharp.skillShotMustBeEvadedW(sd.MenuItemName) || willKill))
                {
                    LXOrbwalker.cantMoveTill = Environment.TickCount + 500;
                    W.Cast();
                    return;
                }
            }

            if (LXOrbwalker.CurrentMode != LXOrbwalker.Mode.Combo && (MasterSharp.skillShotMustBeEvadedAllways(sd.MenuItemName) || MasterSharp.skillShotMustBeEvadedWAllways(sd.MenuItemName)))
            {
                float spellDamage = (float)sShot.Unit.GetSpellDamage(player, sd.SpellName);
                int procHp = (int)((spellDamage / player.MaxHealth) * 100);
                bool willKill = player.Health <= spellDamage;
                if (Q.IsReady() && (MasterSharp.skillShotMustBeEvadedAllways(sd.MenuItemName) || willKill))
                {
                    useQonBest();
                    return;
                }
                else if ((!Q.IsReady(150) || !MasterSharp.skillShotMustBeEvadedAllways(sd.MenuItemName)) && W.IsReady() && (MasterSharp.skillShotMustBeEvadedWAllways(sd.MenuItemName) || willKill))
                {
                    LXOrbwalker.cantMoveTill = Environment.TickCount + 500;
                    W.Cast();
                    return;
                }
            }


        }

        public static void useQonBest()
        {
            try
            {
                if (!Q.IsReady() || selectedTarget == null)
                {
                    Console.WriteLine("Fuk uo here ");
                    return;
                }

                if (selectedTarget.Distance(player) < 600)
                {
                    Console.WriteLine("Q on targ ");
                    Q.Cast(selectedTarget);
                    return;
                }

                var bestOther =
                    ObjectManager.Get<Obj_AI_Base>()
                        .Where(
                            ob =>
                                ob.IsEnemy && (ob is Obj_AI_Minion || ob is Obj_AI_Hero) && ob.Distance(player) < 600 && !ob.IsDead)
                        .OrderBy(ob => ob.Distance(selectedTarget, true)).FirstOrDefault();
                Console.WriteLine("do shit? " + bestOther.Name);

                if (bestOther != null)
                {
                    Q.Cast(bestOther);
                }
                else
                {
                    Console.WriteLine("best cmnnn");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }


    }
}
