using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace HypaJungle
{
    class JungleClearer
    {
        public enum JungleCleanState
        {
            AttackingMinions,
            WaitingMinions,
            RunningToCamp,
            SearchingBestCamp,
            GoingToShop,
            DoingDragon,
            DoSomeHealing
        }

        public static List<String> supportedChamps = new List<string> { "MasterYi", "Udyr", "Warwick", "Shyvana", "LeeSin", "Amumu", "Rengar" }; 


        public static Obj_AI_Hero player = ObjectManager.Player;

        public static JungleCamp focusedCamp;

        public static bool recalCasted = false;


        public static JungleCamp skipCamp;

        public static JungleCleanState jcState = JungleCleanState.GoingToShop;

        public static Jungler jungler = new MasterYi();

        public static void setUpJCleaner()
        {
            switch (player.ChampionName.ToLower())
            {
                case("warwick"):
                    jungler = new Warwick();
                    Game.PrintChat("Warwick loaded");
                    break;
                case "masteryi":
                    jungler = new MasterYi();
                    Game.PrintChat("MasterYi loaded");
                    break;
                case "udyr":
                    jungler = new Udyr();
                    Game.PrintChat("Udyr loaded");
                    break;
                case "shyvana":
                    jungler = new Shyvana();
                    Game.PrintChat("Shyvana loaded");
                    break;
                case "leesin":
                    jungler = new LeeSin();
                    Game.PrintChat("LeeSin loaded");
                    break;
                case "amumu":
                    jungler = new Amumu();
                    Game.PrintChat("Amumu loaded");
                    break;
                case "rengar":
                    jungler = new Rengar();
                    Game.PrintChat("Rengar loaded");
                    break;
            }

            Game.PrintChat("Other junglers coming soon!");

        }

        public static void updateJungleCleaner()
        {
            if (jcState == JungleCleanState.SearchingBestCamp)
            {
                focusedCamp = getBestCampToGo();
                if (focusedCamp != null)
                    jcState = JungleCleanState.RunningToCamp;
            }

            if (jcState == JungleCleanState.RunningToCamp)
            {
                if (focusedCamp.State != JungleCampState.Dead && focusedCamp.team != 3)
                    jungler.castWhenNear(focusedCamp);
                jungler.checkItems();
                logicRunToCamp();
            }

            if (jcState == JungleCleanState.RunningToCamp && (HypaJungle.player.Position.Distance(focusedCamp.Position) < 200 || isCampVisible()))
            {
                jcState = JungleCleanState.WaitingMinions;
            }

            if (jcState == JungleCleanState.WaitingMinions)
            {
                doWhileIdling();
            }

            if (jcState == JungleCleanState.WaitingMinions && (isCampVisible()))
            {
                jcState = JungleCleanState.AttackingMinions;
            }

            if (jcState == JungleCleanState.AttackingMinions)
            {
                attackCampMinions();
            }

            if (jcState == JungleCleanState.AttackingMinions && isCampFinished())
            {
                if(HypaJungle.Config.Item("autoBuy").GetValue<bool>())
                    jcState = JungleCleanState.GoingToShop;
                else
                    jcState = JungleCleanState.SearchingBestCamp;
            }

            if (jcState == JungleCleanState.GoingToShop)
            {
                if (!HypaJungle.Config.Item("autoBuy").GetValue<bool>())
                    jcState = JungleCleanState.SearchingBestCamp;

                if (jungler.inSpwan())
                    jungler.setupSmite();

                if (jungler.inSpwan() && player.IsChanneling)
                {
                    Vector3 stopRecPos = new Vector3(6, 30, 2);
                    player.IssueOrder(GameObjectOrder.MoveTo, player.Position + stopRecPos);
                }

                if (jungler.nextItem != null && player.GoldCurrent >= jungler.nextItem.goldReach )
                {
                    Console.WriteLine(player.GoldCurrent + "   " + jungler.nextItem.goldReach);
                    if (jungler.recall.IsReady() && !player.IsChanneling && !jungler.inSpwan() && !recalCasted)
                    {
                        jungler.recall.Cast();
                        recalCasted = true;
                    }
                }
                else
                {
                    Console.WriteLine("fuk up shop");
                    if (jungler.inSpwan() && player.Health > player.MaxHealth * 0.9f && (!jungler.gotMana || player.Mana > player.MaxMana * 0.9f))
                        jcState = JungleCleanState.SearchingBestCamp;
                    if(!player.IsChanneling && !jungler.inSpwan())
                        jcState = JungleCleanState.SearchingBestCamp;

                }
            }
            else
            {
                recalCasted = false;
            }

            if (jcState == JungleCleanState.GoingToShop && jungler.inSpwan())
            {
                jungler.buyItems();
                if (player.Health > player.MaxHealth * 0.9f && player.Mana > player.MaxMana * 0.9f)
                    jcState = JungleCleanState.SearchingBestCamp;
            }
        }

        public static bool noEnemiesAround()
        {
            return (MinionManager.GetMinions(player.Position, 500).Count == 0);
        }



        public static bool isCampVisible()
        {
            getJungleMinionsManualy();

            foreach (var min in focusedCamp.Minions)
            {
                if (min.Unit != null && min.Unit.IsVisible)
                {
                    return true;
                }
            }

            return false;
        }

        //will need to impliment all shortcuts here
        public static void logicRunToCamp()
        {
            jungler.doWhileRunningIdlin();

            if (!jungler.canMove())
            {
                jcState = JungleCleanState.SearchingBestCamp;
                return;
            }

            if ( !HypaJungle.player.IsMoving || HypaJungle.player.Path.Count() ==0 
                || HypaJungle.player.Path.Last().Distance(focusedCamp.Position) > 50)
            {
                HypaJungle.player.IssueOrder(GameObjectOrder.MoveTo, focusedCamp.Position);
            }
        }

        public static void attackCampMinions()
        {
            List<JungleMinion> campMinions = focusedCamp.Minions.Where(min => min.Unit != null && !min.Dead).OrderByDescending(min => ((Obj_AI_Minion)min.Unit).MaxHealth).ToList();
            /*if (campMinions.Count() ==0)
            {
                getJungleMinionsManualy();
            }
            else//do all attacking
            {*/
                jungler.startAttack((Obj_AI_Minion)campMinions.FirstOrDefault().Unit);
            //}

        }

        public static void getJungleMinionsManualy()
        {
            List<Obj_AI_Base> jungles = MinionManager.GetMinions(HypaJungle.player.Position, 500, MinionTypes.All,MinionTeam.Neutral).ToList();
            foreach (var jun in jungles)
            {
                HypaJungle.jTimer.setUpMinionsPlace((Obj_AI_Minion)jun);
            }
        }

        public static bool isCampFinished()
        {
            if (focusedCamp.State == JungleCampState.Dead && focusedCamp.Minions.All(min => min == null || min.Dead))
                return true;
            return false;
            // return focusedCamp.Minions.All(min => min == null || min.Dead);
        }

        public static void doWhileIdling()
        {
            jungler.doWhileRunningIdlin();
        }
        /*
         *  is buff +5
         *  is in way of needed buff +5
         *  is close priority +10 +8 +6
         *  is spawning till get + 5 sec +4
         *  if smite ebtter get to buff then other camps
         * 
         */

        public static JungleCamp getBestCampToGo()
        {
            float minPriority = getPriorityNumber(HypaJungle.jTimer._jungleCamps.First());
            JungleCamp bestCamp = null;
            foreach (var jungleCamp in HypaJungle.jTimer._jungleCamps)
            {
                if(skipCamp != null && skipCamp.campId == jungleCamp.campId)
                    continue;
                int piro = getPriorityNumber(jungleCamp);
                if (minPriority > piro)
                {                   
                    bestCamp = jungleCamp;
                    minPriority = piro;
                }
            }
            skipCamp = null;
            return bestCamp;
        }

        public static int getPriorityNumber(JungleCamp camp)
        {
            if (camp.isDragBaron)
                return 99999;

            if (((camp.team == 0 && HypaJungle.player.Team == GameObjectTeam.Chaos)
                || (camp.team == 1 && HypaJungle.player.Team == GameObjectTeam.Order)) && !HypaJungle.Config.Item("enemyJung").GetValue<bool>())
                return 999;

            if (camp.team == 3 && !HypaJungle.Config.Item("doCrabs").GetValue<bool>())
                return 999;

            int priority = 0;



            var distTillCamp = getPathLenght(HypaJungle.player.GetPath(camp.Position));
            var timeToCamp = distTillCamp / HypaJungle.player.MoveSpeed;
            var spawnTime = (Game.Time < camp.SpawnTime.TotalSeconds) ? camp.SpawnTime.TotalSeconds : camp.RespawnTimer.TotalSeconds;

            float revOn = camp.ClearTick + (float)spawnTime;
            float timeTillSpawn = (camp.State == JungleCampState.Dead)?((revOn - Game.Time > 0) ? (revOn - Game.Time) : 0):0;

            if (!jungler.canKill(camp))
                priority += 999;
            priority -= camp.bonusPrio;
            priority += (int)timeToCamp;
            priority += (int) timeTillSpawn;
            priority -= (camp.isBuff) ? jungler.buffPriority : 0;
            //if(!camp.isBuff)
              //  priority -= (isInBuffWay(camp)) ? 10 : 0;

            return (int)priority;
        }

        public static bool isInBuffWay(JungleCamp camp)
        {
            
            JungleCamp bestBuff = getBestBuffCamp();
            if (bestBuff == null)
                return false;
            float distTobuff = bestBuff.Position.Distance(HypaJungle.player.Position,true);
            float distToCamp = camp.Position.Distance(HypaJungle.player.Position,true);
            float distCampToBuff = camp.Position.Distance(bestBuff.Position,true);
            if (distTobuff > distToCamp + 800 && distTobuff > distCampToBuff)
                return true;
            return false;

        }

        public static JungleCamp getBestBuffCamp()
        {
            if (HypaJungle.jTimer._jungleCamps.Where(cp => cp.isBuff).Count() == 0)
                return null;

            JungleCamp bestCamp = HypaJungle.jTimer._jungleCamps.Where(cp => cp.isBuff).OrderByDescending(cp => getPriorityNumber(cp)).First();
            return bestCamp;
        }


        public static float getPathLenght(Vector3[] vecs)
        {
            float dist = 0;
            Vector3 from = vecs[0];
            foreach (var vec in vecs)
            {
                dist += Vector3.Distance(from, vec);
                from = vec;
            }
            return dist;
        }

    }
}
