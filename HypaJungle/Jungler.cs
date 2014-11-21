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
    abstract class Jungler
    {
        //Ty tc-crew
        private enum PotionType
        {
            Health = 2003,
            Mana = 2004,
            Biscuit = 2009,
            CrystalFlask = 2041,
        }

        internal class ItemToShop
        {
            public int goldReach;
            public List<int> itemIds;
            public List<int> itemsMustHave;
        }

        public static bool canBuyItems = true;

        private SpellSlot smite = player.GetSpellSlot("summonersmite");

        

        public static Obj_AI_Hero player = ObjectManager.Player;

        public static Spellbook sBook = player.Spellbook;

        public SpellDataInst Qdata = sBook.GetSpell(SpellSlot.Q);
        public SpellDataInst Wdata = sBook.GetSpell(SpellSlot.W);
        public SpellDataInst Edata = sBook.GetSpell(SpellSlot.E);
        public SpellDataInst Rdata = sBook.GetSpell(SpellSlot.R);
        public Spell Q;//Emp 1470
        public Spell W;
        public Spell E;
        public Spell R;
        public Spell recall;
        public Spell[] levelUpSeq;
        public List<ItemToShop> buyThings;

        public ItemToShop nextItem;

        public float dpsFix = 0;
        public int buffPriority = 7;
        public bool gotMana = true;

        public abstract void setUpSpells();
        public abstract void setUpItems();

        public abstract void UseQ(Obj_AI_Minion minion);
        public abstract void UseW(Obj_AI_Minion minion);
        public abstract void UseE(Obj_AI_Minion minion);
        public abstract void UseR(Obj_AI_Minion minion);

        public abstract void attackMinion(Obj_AI_Minion minion);
        public abstract void castWhenNear(JungleCamp camp);

        public abstract void doWhileRunningIdlin();


        public abstract float getDPS(Obj_AI_Minion minion);

        public abstract bool canMove();


        public Jungler()
        {
            setupSmite();
        }

        public void setupSmite()
        {
            if (player.SummonerSpellbook.GetSpell(SpellSlot.Summoner1).SData.Name.ToLower().Contains("smite"))
                smite = SpellSlot.Summoner1;
            else if (player.SummonerSpellbook.GetSpell(SpellSlot.Summoner2).SData.Name.ToLower().Contains("smite"))
                smite =SpellSlot.Summoner2;
        }

        private void doSmite(Obj_AI_Base target)
        {
            if (player.SummonerSpellbook.CanUseSpell(smite) == SpellState.Ready)
            {
                player.SummonerSpellbook.CastSpell(smite, target);
            }
        }

        public void startAttack(Obj_AI_Minion minion)
        {
            usePots();

            if (minion == null || !minion.IsValid || !minion.IsVisible)
                return;
            if (HypaJungle.Config.Item("smiteToKill").GetValue<bool>())
            {
                if (player.GetSummonerSpellDamage(minion, Damage.SummonerSpell.Smite)>=minion.Health)
                    if (((!HypaJungle.jTimer._jungleCamps.Where(cp => cp.isBuff).Any()) && minion.MaxHealth >= 800) ||
                        (JungleClearer.focusedCamp.isBuff && minion.MaxHealth >= 1400))
                    {
                        doSmite(minion);
                    }
            }
            else
            {
                if (minion.Health / getDPS(minion) > ((!HypaJungle.jTimer._jungleCamps.Where(cp => cp.isBuff).Any()) ? 7 : 4) || (JungleClearer.focusedCamp.isBuff && minion.MaxHealth >= 1400))
                    doSmite(minion);
            }
           


            attackMinion(minion);
        }

        public void usePots()
        {
            if (player.Health / player.MaxHealth <= 0.6f && !player.HasBuff("Health Potion"))
                CastPotion(PotionType.Health);

            // Mana Potion
            if(!gotMana) return;

            if (player.Mana / player.MaxMana <= 0.3f && !player.HasBuff("Mana Potion"))
                CastPotion(PotionType.Mana);
        }

        private static void CastPotion(PotionType type)
        {
            try
            {
                player.InventoryItems.First(
                    item =>
                        item.Id == (type == PotionType.Health ? (ItemId) 2003 : (ItemId) 2004) ||
                        (item.Id == (ItemId) 2010) || (item.Id == (ItemId) 2041 && item.Charges > 0)).UseItem();
            }
            catch (Exception)
            {
                
            }
        }

        public void setFirstLvl()
        {
            sBook.LevelUpSpell(levelUpSeq[0].Slot);
            buyItems();
        }

        public void levelUp(Obj_AI_Base sender, CustomEvents.Unit.OnLevelUpEventArgs args)
        {
            if (sender.NetworkId == player.NetworkId)
            {
                    sBook.LevelUpSpell(levelUpSeq[args.NewLevel - 1].Slot);
            }
        }

        public bool canKill(JungleCamp camp)
        {
            if (dpsFix == 0 || camp.dps == 0)
                return true;
            float secToKill = camp.health/dpsFix;
            if (secToKill*camp.dps > (player.Health))
                return false;
            return true;
        }

        public void checkItems()
        {
            if (!canBuyItems)
                return;
            for (int i = buyThings.Count - 1; i >= 0; i--)
            {
                bool hasThemAll = buyThings[i].itemsMustHave.All(item => Items.HasItem(item));
                if (hasThemAll)
                {
                    nextItem = buyThings[i];
                    if (i == buyThings.Count - 1)
                    {
                        canBuyItems = false;
                    }

                    return;
                }
            }
        }

        public void buyItems()
        {
            if (inSpwan())
            {
                foreach (var item in nextItem.itemIds)
                {
                    if (!Items.HasItem(item))
                    {
                        Packet.C2S.BuyItem.Encoded(new Packet.C2S.BuyItem.Struct(item, ObjectManager.Player.NetworkId))
                            .Send();
                    }
                }
            }
            checkItems();
        }

        public bool inSpwan()
        {
            Vector3 spawnPos1 = new Vector3(14286f, 14382f, 172f);
            Vector3 spawnPos0 = new Vector3(416f, 468f, 182f);
            if (player.Distance(spawnPos1) < 600 || player.Distance(spawnPos0) < 600)
                return true;
            return false;
        }
        
    }
}
