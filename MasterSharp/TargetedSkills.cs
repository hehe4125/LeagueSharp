using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;

namespace MasterSharp
{
    class TargetedSkills
    {
        internal class TargSkill
        {
            public string sName;
            public int useQ;
            public int useW;
            public int danger;
            public int delay = 250;
            public GameObjectProcessSpellCastEventArgs spell;

            public TargSkill(string name, int q, int w,int d)
            {
                sName = name;
                useQ = q;
                useW = w;
                danger = d;
            }

            public TargSkill(string name, int q, int w, int d,int del)
            {
                sName = name;
                useQ = q;
                useW = w;
                danger = d;
                delay = del;
            }
        }

        public static List<TargSkill> targetedSkillsAll = new List<TargSkill>();

        public static List<String> dagerousBuffs = new List<String>()
        {
            "timebombenemybuff",
            "karthusfallenonetarget"
        };



        public static void setUpSkills()
        {
            // name of spellName, Q use, W use --- 2-prioritize more , 1- prioritize less 0 dont use
            targetedSkillsAll.Add(new TargSkill("SyndraR", 0, 1, 1));
            targetedSkillsAll.Add(new TargSkill("VayneCondemn", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("Dazzle", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("Overload", 2, 1, 0));
            targetedSkillsAll.Add(new TargSkill("IceBlast", 2, 1, 0));
            targetedSkillsAll.Add(new TargSkill("LeblancChaosOrb", 2, 1, 0));
            targetedSkillsAll.Add(new TargSkill("JudicatorReckoning", 2, 1, 0));
            targetedSkillsAll.Add(new TargSkill("KatarinaQ", 2, 1, 0));
            targetedSkillsAll.Add(new TargSkill("NullLance", 2, 1, 0));
            targetedSkillsAll.Add(new TargSkill("FiddlesticksDarkWind", 2, 1, 0));
            targetedSkillsAll.Add(new TargSkill("CaitlynHeadshotMissile", 2, 1, 1));
            targetedSkillsAll.Add(new TargSkill("BrandWildfire", 2, 1, 1));
            targetedSkillsAll.Add(new TargSkill("Disintegrate", 2, 1, 0));
            targetedSkillsAll.Add(new TargSkill("Frostbite", 2, 1, 0));
            targetedSkillsAll.Add(new TargSkill("AkaliMota", 2, 1, 0));
            //infiniteduresschannel  InfiniteDuress
            targetedSkillsAll.Add(new TargSkill("InfiniteDuress", 2, 0, 1,0));
        }

    }
}
