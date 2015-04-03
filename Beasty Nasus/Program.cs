using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using Color = System.Drawing.Color;

class Program
{

    private static Obj_AI_Hero Player { get { return ObjectManager.Player; } }

    private static Orbwalking.Orbwalker Orbwalker;

    private static Spell Q, W, E, R;

    private static Items.Item Hydra;
    private static Items.Item Tiamat;

    private static Menu Menu;


    static void Main(string[] args)
    {
        // Events http://msdn.microsoft.com/en-us/library/edzehd2t%28v=vs.110%29.aspx
        // OnGameLoad event, gets fired after loading screen is over
        CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
    }


    private static void Game_OnGameLoad(EventArgs args)
    {
        if (Player.ChampionName != "Nasus") 
            return; 


        Q = new Spell(SpellSlot.Q); 
        W = new Spell(SpellSlot.W, 600); 
        E = new Spell(SpellSlot.E, 650); 
        R = new Spell(SpellSlot.R);

        // Method Spell.SetSkillshot(float delay, float width, float speed, bool collision, SkillshotType type)
        // Q.SetSkillshot(0.25f, 80f, 1800f, false, SkillshotType.SkillshotLine);
        E.SetSkillshot(0.28f, 400f, float.MaxValue, false, SkillshotType.SkillshotCircle);

        Hydra = new Items.Item(3074, 500); 
        Hydra = new Items.Item((int)ItemId.Ravenous_Hydra_Melee_Only, 500);

        Tiamat = new Items.Item(3077, 500);
        Tiamat = new Items.Item((int)ItemId.Tiamat_Melee_Only, 500);


        Menu = new Menu("Beasty Nasus", Player.ChampionName, true);

        Menu orbwalkerMenu = Menu.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));

        Orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);

        Menu ts = Menu.AddSubMenu(new Menu("Target Selector", "Target Selector")); ;

        TargetSelector.AddToMenu(ts);

        Menu ComboMenu = Menu.AddSubMenu(new Menu("Combo Options", "ComboS"));
        Menu MixedMenu = Menu.AddSubMenu(new Menu("Mixed Mode", "MixedS"));
        Menu LaneMenu = Menu.AddSubMenu(new Menu("Lane Clear", "LaneS"));
        Menu LastMenu = Menu.AddSubMenu(new Menu("LastHit", "LastS"));
        Menu UltMenu = Menu.AddSubMenu(new Menu("Ultimate", "UltS"));
        Menu WMenu = Menu.AddSubMenu(new Menu("W Use ChampList", "WS"));
        Menu MiscMenu = Menu.AddSubMenu(new Menu("Misc", "MiscS"));
        Menu KsMenu = Menu.AddSubMenu(new Menu("Killing Steal", "KillS"));

        ComboMenu.AddItem(new MenuItem("useQ", "Use Q").SetValue(true));
        ComboMenu.AddItem(new MenuItem("useW", "Use W").SetValue(true));
        ComboMenu.AddItem(new MenuItem("useE", "Use E").SetValue(true));
        ComboMenu.AddItem(new MenuItem("LaughButton", "Get rekt m8").SetValue(new KeyBind(32, KeyBindType.Press)));
        ComboMenu.AddItem(new MenuItem("useTHC", "Use Tiamat/Hydra").SetValue(true));

        MixedMenu.AddItem(new MenuItem("harassQ", "Mixed Q LogicBeta").SetValue(true));
        MixedMenu.AddItem(new MenuItem("harassE", "Use E").SetValue(true));
        MixedMenu.AddItem(new MenuItem("harE", "E Mana Manager").SetValue(new Slider(50, 1, 100)));

        LaneMenu.AddItem(new MenuItem("laneQ", "Smart Q LaneClear").SetValue(true));
        LaneMenu.AddItem(new MenuItem("clearE", "Use E").SetValue(true));
        LaneMenu.AddItem(new MenuItem("laneE", "E Mana Manager").SetValue(new Slider(75, 1, 100)));
        LaneMenu.AddItem(new MenuItem("useTHL", "Use Tiamat/Hydra").SetValue(true));

        LastMenu.AddItem(new MenuItem("lastQ", "Smart Q LastHit").SetValue(true));

        KsMenu.AddItem(new MenuItem("KsE", "Use E for KS").SetValue(true));

        UltMenu.AddItem(new MenuItem("RLWop", "Disable Auto R Below HP??").SetValue(false));
        UltMenu.AddItem(new MenuItem("AutoRLW", "Auto R Below % HP").SetValue(new Slider(10, 1, 100)));
        UltMenu.AddItem(new MenuItem("REAop", "Disable Auto R if # Enemies Around??").SetValue(false));
        UltMenu.AddItem(new MenuItem("AutoREA", "Auto R if # enemies around").SetValue(new Slider(3, 1, 5)));

        WMenu.AddItem(new MenuItem("sep0", "Use W On:"));
        foreach (var Champ in ObjectManager.Get<Obj_AI_Hero>().Where(champ => champ.IsEnemy))
        {
            WMenu.AddItem(
                new MenuItem("noWon" + Champ.BaseSkinName, string.Format("W {0}", Champ.BaseSkinName)).SetValue(true));
        }
        
        MiscMenu.AddItem(new MenuItem("GapW", "Use W for AntiGapClosers").SetValue(true));

        Menu.AddToMainMenu();

        Drawing.OnDraw += Drawing_OnDraw;
        AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;

        Game.OnUpdate += Game_OnGameUpdate;

        Game.PrintChat("Beasty Nasus 1.01 By Bolsudo.");
        Game.PrintChat("Please post bugs in the thread. Thank you");
    }


    private static void Game_OnGameUpdate(EventArgs args)
    {

        if (Player.IsDead)
            return;


        if (!Menu.Item("REAop").GetValue<bool>())
        {
            AutoREnemies();
        }

        if (!Menu.Item("RLWop").GetValue<bool>())
        {
            AutoRHealth();
        }

        if (!Menu.Item("KsE").GetValue<bool>())
            return;
        {
            KillSteal();
        }

        // checks the current Orbwalker mode Combo/Mixed/LaneClear/LastHit
        if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
        {

            HydraCombo();
            TiamatCombo();
            ECombo();
            WCombo();
            QCombo();
        }

        if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit)
        {
            QLastHit2();
        }

        if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
        {
            EHarass();
            QHarass();
        }

        if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
        {
            QClear();
            EClear();
            HydraClear();
            TiamatClear();
        }
    }



    /// <summary>
    /// Main Draw Method
    /// </summary>
    private static void Drawing_OnDraw(EventArgs args)
    {

        if (Player.IsDead)
            return;


        if (E.IsReady())
        {
            // draw Aqua circle around the player
            Utility.Render.Circle(Player.Position, Q.Range, Color.Aqua);
        }
        else
        {
            // draw DarkRed circle around the player while on cd
            Utility.Render.Circle(Player.Position, Q.Range, Color.DarkRed);
        }
    }

    private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
    {
        if (Player.HasBuff("Recall") || Player.IsWindingUp) return;
        if (!Menu.Item("GapW").GetValue<bool>() && W.IsReady())
        {
            W.Cast((Obj_AI_Hero)gapcloser.Sender);
        }
    }


    
    public static void BuggedQ(Spell spell, bool skillshot = false)
    {
        if (spell.IsReady())
            return;
        var allMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, spell.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
        foreach (var minion in allMinions)
        {
            if (!minion.IsValidTarget())
                continue;
            var minionInRangeAa = Orbwalking.InAutoAttackRange(minion);
            var minionInRangeSpell = minion.Distance(ObjectManager.Player) <= spell.Range;
            var minionKillableAa = ObjectManager.Player.GetAutoAttackDamage(minion) >= minion.Health;
            var minionKillableSpell = ObjectManager.Player.GetSpellDamage(minion, SpellSlot.Q) >= minion.Health;
            var lastHit = Program.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit;
            var laneClear = Program.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear;

            if ((lastHit && minionInRangeSpell && minionKillableSpell) && ((minionInRangeAa && !minionKillableAa) || !minionInRangeAa))
                if (skillshot)
                    spell.Cast(minion.Position);
                else
                    spell.Cast(minion);
            else if ((laneClear && minionInRangeSpell && !minionKillableSpell) && ((minionInRangeAa && !minionKillableAa) || !minionInRangeAa))
                if (skillshot)
                    spell.Cast(minion.Position);
                else
                    spell.Cast(minion);
        }
    }

    private static void WUseNo()
    {

        foreach (var aChamp in from aChamp in ObjectManager.Get<Obj_AI_Hero>()
                               where (aChamp.IsEnemy) && (ObjectManager.Player.ServerPosition.Distance(aChamp.Position) < W.Range)
                               where Menu.Item("noWon" + aChamp.BaseSkinName).GetValue<bool>() && W.IsReady()
                               select aChamp)
        {
            W.CastOnUnit(aChamp);
        }
    }
    private static void QLastHit2()
    {
        if (!Menu.Item("lastQ").GetValue<bool>())
            return;

        // check if E ready
        var target = Program.Orbwalker.GetTarget();

        if (Q.IsReady() && target.IsValidTarget(300))
        {
            {
                Q.Cast();
            }
        }
    }




    private static void QClear()
    {
        var target = Program.Orbwalker.GetTarget();
        if (!Menu.Item("laneQ").GetValue<bool>() && Q.IsReady() && target.IsValidTarget(400))
            return;
        
            // check if E ready
            foreach (var enemy in ObjectManager.Get<Obj_AI_Minion>().Where(minion => minion.IsValidTarget() && minion.Health <= (ObjectManager.Player.GetSpellDamage(minion, SpellSlot.Q))))
            {

                Q.Cast();


            }
        
    }
    private static void EClear()
    {
        var Minions = MinionManager.GetMinions(Player.Position, E.Range, MinionTypes.All, MinionTeam.NotAlly);
        var ManaPercentage =  ((Player.Mana / Player.MaxMana)*100);
        int sliderValue = Menu.Item("laneE").GetValue<Slider>().Value;
        if (Minions.Count == 0) return;


        if (!Menu.Item("clearE").GetValue<bool>() && E.IsReady())
            return;

        // check if E ready
        var eposition =  MinionManager.GetBestCircularFarmLocation(Minions.Select(minion => minion.ServerPosition.To2D()).ToList(), E.Width, E.Range);

        if (E.IsReady() && ManaPercentage <= sliderValue)
        {
            {
                E.Cast(eposition.Position);
            }
        }
    }
    
    private static void KillSteal()
    {

        if (E.IsReady())
            
        {
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsValidTarget() && hero.Health <= (ObjectManager.Player.GetSpellDamage(hero, SpellSlot.E))))
            E.Cast(enemy);
        }

    }
    private static void HydraClear()
    {
    var target = Program.Orbwalker.GetTarget();
    if (!Menu.Item("useTHL").GetValue<bool>() && Hydra.IsReady() && target.IsValidTarget(700))
        return;
        {

                Hydra.Cast();
  
        }
    }
    private static void TiamatClear()
    {
        var target = Program.Orbwalker.GetTarget();
        if (Tiamat.IsReady() && target.IsValidTarget(700) && !Menu.Item("useTHL").GetValue<bool>())
            return;
        {

            Tiamat.Cast();

        }
    }
    private static void HydraCombo()
    {

        if (Hydra.IsReady() && !Menu.Item("useTHC").GetValue<bool>())
            return;
        {

            if (Utility.CountEnemiesInRange(ObjectManager.Player, (int)Hydra.Range) >= 1)
            {
                Hydra.Cast();
            }
        }
    }
    private static void TiamatCombo()
    {

        if (Tiamat.IsReady() && !Menu.Item("useTHC").GetValue<bool>())
            return;
        {

            if (Utility.CountEnemiesInRange(ObjectManager.Player, (int)Hydra.Range) >= 1)
            {
                Tiamat.Cast();
            }
        }
    }
    private static void AutoREnemies()
    {

        if (R.IsReady())

        {

            if (CountEnemiesInRange(ObjectManager.Player, (int)E.Range) >= Program.Menu.Item("AutoREA").GetValue<Slider>().Value)
            {
                R.Cast();
            }
        }
    }
    private static void AutoRHealth()
    {

        if (R.IsReady())
        {

            int sliderValue = Menu.Item("AutoRLW").GetValue<Slider>().Value;

            // calc current percent hp
            float healthPercent = Player.Health / Player.MaxHealth * 100;

            // check if we should ult
            if (healthPercent < sliderValue)
            {
                R.Cast();

            }
        }
    }

    private static void QCombo()
    {
        // check if the player wants to use E
        if (!Menu.Item("useQ").GetValue<bool>())
            return;

        // check if E ready
        Obj_AI_Hero target = TargetSelector.GetTarget(700, TargetSelector.DamageType.Physical);
        
        if (Q.IsReady() && target.IsValidTarget(300))
            {
                {
                    Q.Cast();
                }
            }
    }
    private static void WCombo()
    {
        
        if (!Menu.Item("useW").GetValue<bool>())
            return;

        // check if E ready
        foreach (var enemy2 in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsValidTarget()))
        if (W.IsReady() && Menu.Item("noWon" + enemy2.BaseSkinName).GetValue<bool>())

        {
            // check if we found a valid target in range
            {
                
                W.CastOnUnit(enemy2);        
            }
        }
    }

    private static void ECombo()
    {

        if (!Menu.Item("useE").GetValue<bool>())
            return;


        Obj_AI_Hero target = TargetSelector.GetTarget(700, TargetSelector.DamageType.Magical);

       // check if E ready
        if (E.IsReady())
        {

            if (target.IsValidTarget(E.Range))
            {

                E.CastOnUnit(target);
            }
        }


    }
 
    private static void EHarass()
    {
  
        if (!Menu.Item("harassE").GetValue<bool>())
            return;
        
        var ManaPercentage = ((Player.Mana / Player.MaxMana) * 100);
        int sliderValue = Menu.Item("harE").GetValue<Slider>().Value;

        Obj_AI_Hero target = TargetSelector.GetTarget(700, TargetSelector.DamageType.Magical);


        if (E.IsReady() && ManaPercentage <= sliderValue)
        {

            if (target.IsValidTarget(E.Range))
            {

                E.CastOnUnit(target);
            }
        }


    }
    private static void QHarass()
    {
        if (!Menu.Item("harassQ").GetValue<bool>())
            return;

        // check if E ready
        var target = Program.Orbwalker.GetTarget();

        if (Q.IsReady() && target.IsValidTarget(300))
        {
            {
                Q.Cast();
            }
        }
    }

}

