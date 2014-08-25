﻿using System;

using LeagueSharp;
using LeagueSharp.Common;
/*
 * ToDo:
 * Q doesnt shoot much
 * Full combo burst
 * Useles gate <-- fixed
 * 
 * Add Fulldmg combo starting from hamer
 * 
 * kOCK ANY ENEMY UNDER TOWER
 * */
using SharpDX;
using Color = System.Drawing.Color;


namespace JayceSharp
{
    internal class JayceSharp
    {
        public const string CharName = "Jayce";

        public static Menu Config;

        public JayceSharp()
        {
            /* CallBAcks */
            CustomEvents.Game.OnGameLoad += onLoad;

        }

        private static void onLoad(EventArgs args)
        {

            Game.PrintChat("Jayce - Sharp by DeTuKs");
            Jayce.setSkillShots();
            try
            {

                Config = new Menu("Jayce - Sharp", "Jayce", true);
                //Orbwalker
                Config.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));
                Jayce.orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalker"));
                //TS
                Menu targetSelectorMenu = new Menu("Target Selector", "Target Selector");
                SimpleTs.AddToMenu(targetSelectorMenu);
                Config.AddSubMenu(targetSelectorMenu);
                //Combo
                Config.AddSubMenu(new Menu("Combo Sharp", "combo"));
                Config.SubMenu("combo").AddItem(new MenuItem("comboItems", "Use Items")).SetValue(true);
                Config.SubMenu("combo").AddItem(new MenuItem("fullDMG", "Do full damage")).SetValue(new KeyBind('A', KeyBindType.Press));
                Config.SubMenu("combo").AddItem(new MenuItem("injTarget", "Tower Injection")).SetValue(new KeyBind('G', KeyBindType.Press));

                //LastHit
                Config.AddSubMenu(new Menu("LastHit Sharp", "lHit"));

                //LaneClear
                Config.AddSubMenu(new Menu("LaneClear Sharp", "lClear"));

                //Harass
                Config.AddSubMenu(new Menu("Harass Sharp", "harass"));

                //Extra
                Config.AddSubMenu(new Menu("Extra Sharp", "extra"));
                Config.SubMenu("extra").AddItem(new MenuItem("shoot", "Shoot manual Q")).SetValue(new KeyBind('T', KeyBindType.Press));

                //Debug
                Config.AddSubMenu(new Menu("Debug", "debug"));
                Config.SubMenu("debug").AddItem(new MenuItem("db_targ", "Debug Target")).SetValue(new KeyBind('N', KeyBindType.Press));


                Config.AddToMainMenu();
                Drawing.OnDraw += onDraw;
                Game.OnGameUpdate += OnGameUpdate;

                GameObject.OnCreate += OnCreateObject;
                GameObject.OnDelete += OnDeleteObject;
                Obj_AI_Base.OnProcessSpellCast += OnProcessSpell;

            }
            catch
            {
                Game.PrintChat("Oops. Something went wrong with Jayce - Sharp");
            }

        }

        private static void OnGameUpdate(EventArgs args)
        {
            Jayce.checkForm();
            Jayce.processCDs();
            if (Config.Item("shoot").GetValue<KeyBind>().Active)//fullDMG
            {
                Jayce.shootQE(Game.CursorPos);
            }

            if(!Jayce.E1.IsReady())
                Jayce.castQon = new Vector3(0,0,0);
            else if (Jayce.castQon.X != 0)
                Jayce.shootQE(Jayce.castQon);

            if (Config.Item("fullDMG").GetValue<KeyBind>().Active)//fullDMG
            {
                Obj_AI_Hero target = SimpleTs.GetTarget(Jayce.getBestRange(), SimpleTs.DamageType.Physical);
                if (Jayce.lockedTarg == null)
                    Jayce.lockedTarg = target;
                Jayce.doFullDmg(Jayce.lockedTarg);
            }
            else
            {
                Jayce.lockedTarg = null;
            }

            if (Config.Item("injTarget").GetValue<KeyBind>().Active)//fullDMG
            {
                Obj_AI_Hero target = SimpleTs.GetTarget(Jayce.getBestRange(), SimpleTs.DamageType.Physical);
                if (Jayce.lockedTarg == null)
                    Jayce.lockedTarg = target;
                Jayce.doJayceInj(Jayce.lockedTarg);
            }
            else
            {
                Jayce.lockedTarg = null;
            }
           // Console.Clear();
           // Console.WriteLine(Jayce.isHammer +" "+Jayce.Qdata.SData.Name);

            if (Jayce.castEonQ != null && (Jayce.castEonQ.TimeSpellEnd-2) > Game.Time)
                Jayce.castEonQ = null;

            if (Jayce.orbwalker.ActiveMode.ToString() == "Combo")
            {
                
                Obj_AI_Hero target = SimpleTs.GetTarget(Jayce.getBestRange(), SimpleTs.DamageType.Physical);
                Jayce.doCombo(target);
            }

            if (Jayce.orbwalker.ActiveMode.ToString() == "Mixed")
            {

            }

            if (Jayce.orbwalker.ActiveMode.ToString() == "LaneClear")
            {

            }

          
        }

        private static void onDraw(EventArgs args)
        {

            //Obj_AI_Hero target = SimpleTs.GetTarget(1500, SimpleTs.DamageType.Physical);

          //  Utility.DrawCircle(Jayce.getBestPosToHammer(target), 70, Color.LawnGreen);
           // Utility.DrawCircle(Jayce.Player.Position, 400, Color.Violet);
            if (!Jayce.isHammer)
            {
                Utility.DrawCircle(Jayce.Player.Position, 1550, Color.Violet);
                Utility.DrawCircle(Jayce.Player.Position, 1100, Color.Red);
            }
            else
            {
                Utility.DrawCircle(Jayce.Player.Position, 600, Color.Red);
            }


            //Draw CD
            Jayce.drawCD();
        }

        private static void OnCreateObject(GameObject sender, EventArgs args)
        {
            Obj_SpellMissile missile = sender as Obj_SpellMissile;
            if (missile != null)
            {
                Obj_SpellMissile missle = missile;

                if (missle.SpellCaster.IsMe && missle.SData.Name == "JayceShockBlastMis")
                {
                   // Console.WriteLine("Created " +  missle.SData.Name );
                    Jayce.myCastedQ = missle;
                }
            }
        }

        private static void OnDeleteObject(GameObject sender, EventArgs args)
        {
            if (Jayce.myCastedQ != null && Jayce.myCastedQ.NetworkId == sender.NetworkId)
            {
                Jayce.myCastedQ = null;
                Jayce.castEonQ = null;
            }
        }

        public static void OnProcessSpell(Obj_AI_Base obj, GameObjectProcessSpellCastEventArgs arg)
        {
            if (obj.IsMe)
            {

                if (arg.SData.Name == "jayceshockblast")
                {
                    Jayce.castEonQ = arg;
                }
                else if (arg.SData.Name == "jayceaccelerationgate")
                {
                    Jayce.castEonQ = null;
                   // Console.WriteLine("Cast dat E on: " + arg.SData.Name);
                }

                Jayce.getCDs(arg);
            }
        }

    }
}