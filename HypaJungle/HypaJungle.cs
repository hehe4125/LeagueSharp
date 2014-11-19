using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace HypaJungle
{
    /*
     * Jungle
     * 
     * 
     * 
     * 
     * 
     * 
     * 
     * 
     * 
     */


    internal class HypaJungle
    {
        public static JungleTimers jTimer;

        public static Menu Config;

        public static Obj_AI_Hero player = ObjectManager.Player;

        public static float lastSkip = 0;

        public HypaJungle()
        {
          

            CustomEvents.Game.OnGameLoad += onLoad;

        }

        private static void onLoad(EventArgs args)
        {

            Game.PrintChat("HypaJungle by DeTuKs");
            try
            {
                ConfigLoader.setupFolders(JungleClearer.supportedChamps);

                if (!JungleClearer.supportedChamps.Contains(player.ChampionName))
                {
                    Game.PrintChat("Sory this champion is not supported yet! go vote for it in forum ;)");
                    return;
                }

                jTimer = new JungleTimers();

                Config = new Menu("HypeJungle", "hype", true);

                setChampMenu(player.ChampionName);

              //  Config.AddSubMenu(new Menu("Jungler Config", "junglerCon"));
               // Config.SubMenu("junglerCon").AddItem(new MenuItem("blabla", "Relead to work!")).SetValue(true);
              //  Config.SubMenu("junglerCon").AddItem(new MenuItem("useDefConf", "Use Default Config")).SetValue(true);
              //  Config.SubMenu("junglerCon").AddItem(new MenuItem("fileConfigHypa", "")).SetValue(ConfigLoader.getChampionConfigs(player.ChampionName));
                Config.AddSubMenu(new Menu("Jungler", "jungler"));
                Config.SubMenu("jungler").AddItem(new MenuItem("autoLVL", "Auto Level")).SetValue(true);
                Config.SubMenu("jungler").AddItem(new MenuItem("autoBuy", "Auto Buy")).SetValue(true);

                Config.AddSubMenu(new Menu("Debug stuff", "debug"));
                Config.SubMenu("debug").AddItem(new MenuItem("doJungle", "Do jungle")).SetValue(new KeyBind('J', KeyBindType.Toggle));
                Config.SubMenu("debug").AddItem(new MenuItem("debugOn", "Debug stuff")).SetValue(new KeyBind('A', KeyBindType.Press));
                Config.SubMenu("debug").AddItem(new MenuItem("skipSpawn", "Debug skip")).SetValue(new KeyBind('G', KeyBindType.Press));
                Config.SubMenu("debug").AddItem(new MenuItem("showPrio", "Show priorities")).SetValue(false);

                Config.AddToMainMenu();

               

                Game.OnGameUpdate += OnGameUpdate;
                Drawing.OnDraw += onDraw;
                CustomEvents.Unit.OnLevelUp += OnLevelUp;

                Game.OnGameProcessPacket += Game_OnGameProcessPacket;
                JungleClearer.setUpJCleaner();

                //Load custom stuff
                if (!Config.Item("useDefConf_"+player.ChampionName).GetValue<bool>())
                    ConfigLoader.loadNewConfigHypa(
                       Config.Item("fileConfigHypa2_" + player.ChampionName).GetValue<StringList>().SList[
                           Config.Item("fileConfigHypa2_"+player.ChampionName).GetValue<StringList>().SelectedIndex]);

                JungleClearer.jungler.setFirstLvl();


            }
            catch(Exception ex)
            {
                Game.PrintChat("Oops. Something went wrong with HypaJungle");
                Console.WriteLine(ex);
            }

        }

        public static void setChampMenu(string champ)
        {

            Config.AddSubMenu(new Menu(champ+" Config", "junglerCon" + champ));
            Config.SubMenu("junglerCon" + champ).AddItem(new MenuItem("blabla_" + champ, "Relead to work!")).SetValue(true);
            Config.SubMenu("junglerCon" + champ).AddItem(new MenuItem("useDefConf_" + champ, "Use Default Config")).SetValue(true);
            Config.SubMenu("junglerCon" + champ).AddItem(new MenuItem("fileConfigHypa2_" + champ, "")).SetValue(ConfigLoader.getChampionConfigs(player.ChampionName));

        }

        static void Game_OnGameProcessPacket(GamePacketEventArgs args)
        {
            if (args.PacketData[0] == Packet.S2C.EmptyJungleCamp.Header)
            {
                Packet.S2C.EmptyJungleCamp.Struct camp = Packet.S2C.EmptyJungleCamp.Decoded(args.PacketData);
                Console.WriteLine("disable camp: "+camp.CampId);
                jTimer.disableCamp((byte)camp.CampId);
            }

            if (args.PacketData[0] == 0xE9)
            {
                GamePacket gp = new GamePacket(args.PacketData);
                gp.Position = 21;
                byte campID = gp.ReadByte();
                Console.WriteLine("Enable camp: "+campID);
                jTimer.enableCamp(campID);

            }
        }

        private static void OnLevelUp(Obj_AI_Base sender, CustomEvents.Unit.OnLevelUpEventArgs args)
        {
            if (Config.Item("autoLVL").GetValue<bool>())
                JungleClearer.jungler.levelUp(sender,args);
        }

        private static void OnGameUpdate(EventArgs args)
        {
            if (Config.Item("skipSpawn").GetValue<KeyBind>().Active) //fullDMG
            {
                if (JungleClearer.focusedCamp != null && lastSkip+2<Game.Time)
                {
                    lastSkip = Game.Time;
                    JungleClearer.skipCamp = JungleClearer.focusedCamp;
                    jTimer.disableCamp(JungleClearer.focusedCamp.campId);
                }
            }

            if (Config.Item("debugOn").GetValue<KeyBind>().Active) //fullDMG
            {
                Console.WriteLine("BuyThings: "+JungleClearer.jungler.buyThings.First().goldReach);
            }
            if (Config.Item("doJungle").GetValue<KeyBind>().Active) //fullDMG
            {
                try
                {
                    JungleClearer.updateJungleCleaner();

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
            else
            {
                JungleClearer.jcState = JungleClearer.JungleCleanState.GoingToShop;
            }

        }

        private static void onDraw(EventArgs args)
        {
            Drawing.DrawText(200, 200, Color.Green, JungleClearer.jcState.ToString() +" : "+player.Position.X+ " : "+player.Position.Y+ " : "
                +player.Position.Z+ " : ");
            if (JungleClearer.jungler.nextItem != null)
                Drawing.DrawText(200, 250, Color.Green, "Gold: "+JungleClearer.jungler.nextItem.goldReach);
            if (JungleClearer.focusedCamp != null)
             Drawing.DrawCircle(JungleClearer.focusedCamp.Position,300,Color.BlueViolet);

            foreach (var min in MinionManager.GetMinions(HypaJungle.player.Position, 800, MinionTypes.All,MinionTeam.Neutral))
            {
                var pScreen = Drawing.WorldToScreen(min.Position);
                Drawing.DrawText(pScreen.X, pScreen.Y, Color.Red, min.Name+" : "+min.MaxHealth);
            }


            Drawing.DrawCircle(JungleClearer.getBestBuffCamp().Position, 500, Color.BlueViolet);

           /* foreach (var camp in jTimer._jungleCamps)
            {
                var pScreen = Drawing.WorldToScreen(camp.Position);

                if(JungleClearer.isInBuffWay(camp))
                    Drawing.DrawCircle(camp.Position, 200, Color.Red);
                   // Drawing.DrawText(pScreen.X, pScreen.Y, Color.Red, camp.State.ToString() + " : " + JungleClearer.getPriorityNumber(camp));

                //Order = 0 chaos =1
            }*/

            if (Config.Item("showPrio").GetValue<bool>()) //fullDMG
            {
                foreach (var camp in jTimer._jungleCamps)
                {
                    var pScreen = Drawing.WorldToScreen(camp.Position);

                    Drawing.DrawText(pScreen.X, pScreen.Y, Color.Red,
                        camp.State.ToString() + " : "+camp.team+ " : " + JungleClearer.getPriorityNumber(camp));

                    //Order = 0 chaos =1
                }
            }
        }

    }
}
