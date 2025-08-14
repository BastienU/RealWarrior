// Jeu d'aventure/survie en console - version évoluée avec capacités spéciales
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace JeuSurvieConsole
{

    public enum WeaponType { Sword, Greatsword, Spear, WizardWand }

    enum PotionType { Heal, SuperHeal, Luck, Damage, XP, Gold }

    enum SpecialAttackType { DoubleStrike, BerserkerSmash, StunningBlow }

    public enum ElementType { None, Fire, Ice }

    public static class CombatContext
    {
        public static WeaponType CurrentWeaponType { get; set; } = WeaponType.Sword;
    }

    class Program
    {
        static void Main(string[] args)
        {
            Game game = new Game();
            game.Start();
        }
    }

    class Game
    {
        Player player;
        Enemy currentEnemy;
        int waveNumber;
        Random random = new Random();
        Merchant merchant;



        public void Start()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            while (true)
            {
                player = new Player();
                waveNumber = 1;
                player.CurrentWave = waveNumber;

                Console.Clear();
                Console.WriteLine("Bienvenue dans cette nouvelle aventure !");
                Console.WriteLine("Appuyez sur une touche pour commencer...");
                Console.ReadKey(true);

                while (player.IsAlive)
                {
                    player.CurrentWave = waveNumber;

                    if (waveNumber % 10 == 0)
                        currentEnemy = EnemyFactory.CreateBoss(waveNumber);
                    else
                        currentEnemy = EnemyFactory.CreateEnemy(waveNumber);

                    CombatLoop();

                    if (!player.IsAlive)
                        break;

                    // Apparition du marchand toutes les 5 vagues, sauf à la vague 10 (boss)
                    if (waveNumber % 10 == 5)
                    {
                        merchant = new Merchant(waveNumber);
                        merchant.ShowShop(player);
                        if (random.NextDouble() < 0.5)
                        {
                            var darkMage = new DarkMage();
                            darkMage.OfferSpell(player);
                        }
                    }

                    // Apparition du forgeron toutes les 9 vagues, avant la vague 10 (boss)
                    if (waveNumber % 10 == 9)
                    {
                        Console.Clear();
                        Console.WriteLine("⚠️ Vous vous apprêtez à affronter un terrible boss !");
                        Console.WriteLine("Il serait sage d'améliorer vos armes pour sortir victorieux de ce combat !");
                        Console.WriteLine("\nAppuyez sur une touche pour aller voir le forgeron...");
                        Console.ReadKey(true);

                        Blacksmith.VisitBlacksmith(player);
                    }

                    waveNumber++;
                }

                Console.Clear();
                Console.WriteLine("\n\n💀 Vous êtes mort !\n");
                Console.WriteLine("Fécilications ! Vous êtes allé jusqu'à la vague " + waveNumber);
                Console.WriteLine("\nSouhaitez-vous rejouer ? (O/N)");
                var key = Console.ReadKey(true).Key;
                if (key != ConsoleKey.O) break;
            }
        }


        void CombatLoop()
        {
            while (currentEnemy.IsAlive && player.IsAlive)
            {
                // Appliquer les effets élémentaires sur l'ennemi AVANT l'action du joueur
                if (currentEnemy.CurrentElementStatus != null && currentEnemy.CurrentElementStatus.IsActive)
                {
                    switch (currentEnemy.CurrentElementStatus.Type)
                    {
                        case ElementType.Fire:
                            int burnDamage = 20;
                            currentEnemy.TakeDamage(burnDamage);
                            Console.WriteLine($"🔥 {currentEnemy.Name} subit {burnDamage} dégâts de brûlure !");
                            break;

                        case ElementType.Ice:
                            currentEnemy.FreezeTurns = currentEnemy.CurrentElementStatus.Duration;
                            Console.WriteLine($"❄️ {currentEnemy.Name} est gelé pendant {currentEnemy.FreezeTurns} tour(s) !");
                            break;
                    }

                    currentEnemy.CurrentElementStatus.Duration--;
                }

                // Si l’ennemi meurt de brûlure → loot et fin du combat
                if (!currentEnemy.IsAlive)
                {
                    GagnerRecompenses();
                    break;
                }

                DrawUI();

                // Reset de la défense au début du tour du joueur
                player.IsDefending = false;

                if (player.SkipNextTurn)
                {
                    // Si le joueur passe son tour à cause d’un gel ou autre
                    player.PassTurn();
                    player.SkipNextTurn = false;

                    if (currentEnemy.IsAlive)
                    {
                        currentEnemy.Act(player);
                        Console.ReadKey(true);
                    }
                    continue;
                }

                bool playerActed = false;
                ConsoleKey key = Console.ReadKey(true).Key;

                switch (key)
                {
                    case ConsoleKey.D1:
                        bool acted = player.Attack(currentEnemy); // true si une action a vraiment eu lieu
                        if (acted)
                        {
                            player.ReduceSpecialCooldown();
                            playerActed = true;
                        }
                        break;

                    case ConsoleKey.D2: // Défendre
                        player.Defend(currentEnemy);
                        playerActed = true;
                        break;

                    case ConsoleKey.D3: // Changer arme
                        player.ChangeWeapon();
                        break;

                    case ConsoleKey.D4: // Potion
                        player.ChoosePotion();
                        break;

                    case ConsoleKey.D5: // Inventaire
                        player.ShowInventory();
                        break;

                    case ConsoleKey.D6: // Passer tour volontairement
                        player.PassTurn();
                        playerActed = true;
                        break;

                    case ConsoleKey.D7: // Attaque spéciale
                        playerActed = player.UseSpecialAttack(currentEnemy);
                        break;

                    case ConsoleKey.D8: // Sort
                        if (player.LearnedSpells.Count == 0)
                        {
                            Console.WriteLine("❌ Vous ne connaissez aucun sort.");
                            Console.ReadKey(true);
                            break;
                        }
                        Console.Write("🔮 Entrez la formule magique : ");
                        var formula = Console.ReadLine()?.Trim().ToLower();
                        var spell = player.LearnedSpells.FirstOrDefault(s => s.Formula == formula);

                        if (spell != null)
                        {
                            if (player.SpendEssence(spell.EssenceCost))
                            {
                                Console.WriteLine($"✨ Vous lancez {spell.Name} !");
                                spell.Effect(player, currentEnemy);
                                playerActed = true;
                            }
                            Console.ReadKey(true);
                        }
                        else
                        {
                            Console.WriteLine("❌ Formule incorrecte ou sort inconnu.");
                            Console.ReadKey(true);
                        }
                        break;
                }

                // Si le joueur a agi, alors appliquer l’effet des éléments sur lui et décrémenter les buffs
                if (playerActed)
                {
                    player.UpdateElementStatus();
                    player.UpdateBuffs();
                }

                // Si l’ennemi est encore en vie, il joue
                if (playerActed && currentEnemy.IsAlive)
                {
                    if (!currentEnemy.HasAlreadyActedThisTurn)
                    {
                        currentEnemy.Act(player);
                        Console.ReadKey(true);
                    }
                    else
                    {
                        currentEnemy.HasAlreadyActedThisTurn = false;
                        Console.ReadKey(true);
                    }
                }

                // Fin du combat si l’ennemi meurt après l’action du joueur
                if (!currentEnemy.IsAlive)
                {
                    GagnerRecompenses();
                    break;
                }
            }
        }

        void GagnerRecompenses()
        {
            int baseGold = random.Next(5, 31);
            int gainedGold = player.GainGoldWithPotion(baseGold);

            int gainedXP = 0;
            if (currentEnemy.XPValue > 0)
            {
                gainedXP = player.GainXPWithPotion(currentEnemy.XPValue);
            }

            Console.WriteLine($"\n🏆 Vous gagnez {gainedGold} pièces d’or et {gainedXP} points d’expérience !");

            LootManager.DropPotion(player, currentEnemy, random);
            LootManager.DropElementStone(player, currentEnemy, random);
            if (currentEnemy is SorcererBoss)
            {
                player.ObtainWizardWand();

                const string FireStone = "Pierre enflammée";
                const string IceStone = "Pierre congelée";

                if (!player.ElementStones.ContainsKey(FireStone)) player.ElementStones[FireStone] = 0;
                if (!player.ElementStones.ContainsKey(IceStone)) player.ElementStones[IceStone] = 0;

                player.ElementStones[FireStone] += 1;
                player.ElementStones[IceStone] += 1;

                Console.WriteLine("🎁 Vous trouvez également une pierre enflammée et une pierre congelée !");
            }

            // Reset des effets élémentaires pour le prochain combat
            currentEnemy.CurrentElementStatus = new ElementStatus(ElementType.None, 0);
            Console.ReadKey(true);
        }

        void DrawUI()
        {
            string extra = player.LearnedSpells.Count > 0 ? "  [8] Sort" : "";

            Console.Clear();
            Console.WriteLine($"========== VAGUE {waveNumber} ==========");
            Console.WriteLine($"👤 Joueur : {player.Health}/{player.MaxHealth} PV | Arme : {player.CurrentWeapon.Name} | Atk: {player.TotalAttack()} | Or: {player.Gold} | XP: {player.XP}/{player.XPToNextLevel}");
            if (player.MagicUnlocked)
                Console.WriteLine($"🔮 Essence : {player.Essence}/{player.MaxEssence}");
            Console.WriteLine($"🧪 Buffs : {player.ListBuffs()} | Cooldown Spécial : {player.SpecialCooldown}/4");
            if (player.CurrentElementStatus.IsActive)
            {
                switch (player.CurrentElementStatus.Type)
                {
                    case ElementType.Fire:
                        Console.WriteLine($"🔥 Vous brûlez encore pendant {player.CurrentElementStatus.Duration} tour(s) !");
                        break;
                    case ElementType.Ice:
                        Console.WriteLine($"❄️ Vous êtes recouvert de glace pendant {player.CurrentElementStatus.Duration} tour(s) !");
                        break;
                }
            }
            if (player.XPBoostKillsRemaining > 0)
                Console.WriteLine($"📘 Potion d’XP active : encore {player.XPBoostKillsRemaining} ennemi(s) avec XP doublée !");
            if (player.GoldBoostKillsRemaining > 0)
                Console.WriteLine($"💰 Potion d’or active : encore {player.GoldBoostKillsRemaining} ennemi(s) avec or doublé !\n");
            string elementActuel = currentEnemy.CurrentElementStatus.IsActive
                ? currentEnemy.CurrentElementStatus.Type.ToString()
                : "Aucun";
            Console.WriteLine($"\n🦾 Ennemi : {currentEnemy.Name} - PV : {currentEnemy.Health}/{currentEnemy.MaxHealth} - Elément appliqué : {elementActuel}\n");
            Console.WriteLine("[1] Attaquer  [2] Se défendre  [3] Changer d'arme  [4] Potion  [5] Inventaire  [6] Passer  [7] Capacité spéciale" + extra + "\n");
        }
    }

    class SpecialAttack
    {
        public SpecialAttackType Type;
        public string Name;
        public string Description;

        public SpecialAttack(SpecialAttackType type)
        {
            Type = type;
            switch (type)
            {
                case SpecialAttackType.DoubleStrike:
                    Name = "Double Frappe";
                    Description = "Deux attaques consécutives.";
                    break;
                case SpecialAttackType.BerserkerSmash:
                    Name = "Fracas du Berserker";
                    Description = "Inflige des dégâts et augmente vos dégâts pour 3 attaques.";
                    break;
                case SpecialAttackType.StunningBlow:
                    Name = "Coup Étourdisant";
                    Description = "Inflige des dégâts et étourdit l'ennemi pendant 3 tours (sauf les boss).";
                    break;
            }
        }

        public void Execute(Player player, Enemy enemy)
        {
            switch (Type)
            {
                case SpecialAttackType.DoubleStrike:
                    DoubleStrike(player, enemy);
                    break;
                case SpecialAttackType.BerserkerSmash:
                    BerserkerSmash(player, enemy);
                    break;
                case SpecialAttackType.StunningBlow:
                    StunningBlow(player, enemy);
                    break;
            }
        }

        private void DoubleStrike(Player player, Enemy enemy)
        {
            int dmg1 = player.TotalAttack();
            int dmg2 = player.TotalAttack();
            Console.WriteLine($"Double Frappe ! Vous infligez {dmg1 + dmg2} dégâts !");
            enemy.TakeDamage(dmg1);
            enemy.TakeDamage(dmg2);
            if (enemy.IsAlive)
                player.GainEssence(5);
        }

        private void BerserkerSmash(Player player, Enemy enemy)
        {
            int dmg = player.TotalAttack() + 20;
            Console.WriteLine($"Fracas du Berserker ! Vous infligez {dmg} dégâts et augmentez vos dégâts pendant 3 attaques.");
            enemy.TakeDamage(dmg);
            player.SpecialAttackDamageBuffTurns += 3;
        }

        private void StunningBlow(Player player, Enemy enemy)
        {
            int dmg = player.TotalAttack();
            enemy.TakeDamage(dmg);

            if (enemy.IsBoss)
            {
                Console.WriteLine($"Coup Étourdisant ! {enemy.Name} reçoit {dmg} dégâts, mais résiste à l'étourdissement !");
            }
            else
            {
                enemy.StunTurns = 3;
                Console.WriteLine($"Coup Étourdisant ! {enemy.Name} est étourdi pendant 3 tours !");
            }
        }
    }



    class Player
    {
        public int CurrentWave { get; set; }
        private int health;
        private int maxHealth;

        public int Health
        {
            get => health;
            set => health = Math.Min(value, MaxHealth); // Health ne peut pas dépasser MaxHealth
        }

        public int MaxHealth
        {
            get => maxHealth;
            private set
            {
                maxHealth = value;
                // S'assurer que la vie ne dépasse pas le nouveau max
                if (Health > maxHealth) Health = maxHealth;
            }
        }

        public int XP = 0;
        public int Level = 1;
        public int XPToNextLevel => 100 * Level;
        public bool IsAlive => Health > 0;

        public int SpecialAttackDamageBuffTurns { get; internal set; }

        public Weapon CurrentWeapon;
        public List<Weapon> Weapons = new List<Weapon>();
        public Dictionary<PotionType, int> Inventory = new Dictionary<PotionType, int>();
        public Dictionary<string, int> ElementStones = new Dictionary<string, int>
        {
            { "Pierre enflammée", 0 },
            { "Pierre congelée", 0 }
        };
        public int DamageBuffTurns = 0;
        public int LuckBuffTurns = 0;
        public bool IsDefending = false;
        public bool SkipNextTurn { get; set; } = false;
        public ElementStatus CurrentElementStatus { get; private set; } = new ElementStatus(ElementType.None, 0);
        public int Gold = 0;
        public List<SpecialAttack> SpecialAttacks = new List<SpecialAttack>();
        public int SpecialCooldown = 0;
        public int Essence { get; private set; } = 0;
        public int MaxEssence { get; private set; } = 100;
        public List<Spell> LearnedSpells { get; } = new();
        public bool MagicUnlocked => LearnedSpells.Count > 0;
        public int ObsidianShieldTurns { get; set; } = 0;
        public int XPBoostKillsRemaining = 0;
        public int GoldBoostKillsRemaining { get; set; } = 0;

        public Player()
        {
            Weapons.Add(new Weapon("Épée", 50, WeaponType.Sword));
            Weapons.Add(new Weapon("Espadon", 65, WeaponType.Greatsword));
            Weapons.Add(new Weapon("Lance", 45, WeaponType.Spear));
            CurrentWeapon = Weapons[0];

            CombatContext.CurrentWeaponType = CurrentWeapon.Type;

            foreach (PotionType pt in Enum.GetValues(typeof(PotionType)))
                Inventory[pt] = 2;

            MaxHealth = 500;
            Health = MaxHealth;
        }

        public int TotalAttack()
        {
            // Prendre en compte le niveau de l’arme dans le calcul
            int baseDamage = CurrentWeapon.BaseDamage + (CurrentWeapon.Level * 5);

            int damageBuff = (DamageBuffTurns > 0) ? 20 : 0;

            int specialDamageBuff = (SpecialAttackDamageBuffTurns > 0) ? 20 : 0;

            return baseDamage + damageBuff + specialDamageBuff;
        }

        public bool Attack(Enemy enemy)
        {
            if (CurrentWeapon.Type == WeaponType.WizardWand)
            {
                return AttackWithWizardWand(enemy);
            }

            int dmg = TotalAttack();

            if (enemy.StunTurns > 0)
            {
                Console.WriteLine($"{enemy.Name} est étourdi et ne peut pas esquiver !");
                Console.WriteLine($"Vous attaquez {enemy.Name} avec votre {CurrentWeapon.Name} et infligez {dmg} dégâts !");
                bool hit = enemy.TakeDamage(dmg);
                if (hit)
                {
                    GainEssence(5);
                    if (!enemy.IsAlive)
                        GainEssence(10);
                }
                return true;
            }

            bool canDodge = !enemy.IsResting;
            double dodgeChance = 0.2;
            bool enemyDodged = canDodge && new Random().NextDouble() < dodgeChance;

            if (enemyDodged)
            {
                double counterAttackChance = 0.5;
                bool enemyCounterAttacks = new Random().NextDouble() < counterAttackChance;

                if (enemyCounterAttacks)
                {
                    Console.WriteLine($"{enemy.Name} contre-attaque !");

                    // Mini-jeu d'esquive AVANT que l'ennemi inflige des dégâts
                    if (QuickPressMiniGame("🌀 Esquive la contre-attaque !", 3000, out _))
                    {
                        Console.WriteLine("💨 Vous esquivez la contre-attaque avec succès !");
                        enemy.HasAlreadyActedThisTurn = true;

                        // Mini-jeu de contre-attaque
                        if (QuickPressMiniGame("⚔️ Contre-attaque !", 1500, out _))
                        {
                            int counterDmgMultiplier = CurrentWeapon.Name.ToLower() switch
                            {
                                "espadon" => 3,
                                "épée" => 5,
                                "lance" => 7,
                                _ => 2,
                            };

                            int counterDmg = TotalAttack() * counterDmgMultiplier;
                            Console.WriteLine($"⚡ Contre-attaque réussie ! Vous infligez {counterDmg} dégâts à {enemy.Name} !");
                            bool hitCounter = enemy.TakeDamage(counterDmg);
                            if (hitCounter)
                            {
                                GainEssence(5);
                                if (!enemy.IsAlive)
                                    GainEssence(10);
                            }
                        }
                        else
                        {
                            Console.WriteLine("❌ Vous avez raté la contre-attaque.");
                            Console.ReadKey(true);
                        }
                    }
                    else
                    {
                        Console.WriteLine("❌ Vous avez raté l'esquive !");
                        enemy.Attack(this);
                        enemy.HasAlreadyActedThisTurn = true;
                        Console.ReadKey(true);
                    }
                }
                else
                {
                    Console.WriteLine($"{enemy.Name} esquive votre attaque.");
                }
            }
            else
            {
                Console.WriteLine($"Vous attaquez {enemy.Name} avec votre {CurrentWeapon.Name} et infligez {dmg} dégâts !");
                bool hit = enemy.TakeDamage(dmg);
                if (hit)
                {
                    GainEssence(5);
                    if (!enemy.IsAlive)
                        GainEssence(10);
                }
            }
            return true; // le tour est consommé
        }

        // Retourne true si le tour est consommé, false si annulé / impossible (pas de pierres)
        private bool AttackWithWizardWand(Enemy enemy)
        {
            int fire = ElementStones["Pierre enflammée"];
            int ice = ElementStones["Pierre congelée"];

            if (fire <= 0 && ice <= 0)
            {
                Console.WriteLine("❌ Vous n’avez aucune pierre élémentaire pour utiliser la baguette !");
                Console.ReadKey(true);
                return false; // ne consomme pas le tour
            }

            while (true)
            {
                Console.Clear();
                Console.WriteLine("✨ Choisissez la pierre élémentaire à utiliser :");
                Console.WriteLine($"[1] Pierre enflammée ({fire})");
                Console.WriteLine($"[2] Pierre congelée ({ice})");
                Console.WriteLine("[0] Annuler");

                var key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.D0) return false; // annule -> ne consomme pas le tour

                ElementType chosen = ElementType.None;
                string stoneLabel = null;

                if (key == ConsoleKey.D1)
                {
                    if (fire <= 0) { Console.WriteLine("❌ Pas assez de pierres enflammées !"); Console.ReadKey(true); return false; }
                    chosen = ElementType.Fire;
                    stoneLabel = "Pierre enflammée";
                }
                else if (key == ConsoleKey.D2)
                {
                    if (ice <= 0) { Console.WriteLine("❌ Pas assez de pierres congelées !"); Console.ReadKey(true); return false; }
                    chosen = ElementType.Ice;
                    stoneLabel = "Pierre congelée";
                }
                else
                {
                    Console.WriteLine("❌ Choix invalide.");
                    Console.ReadKey(true);
                    continue;
                }

                // Consommation de la pierre
                ElementStones[stoneLabel]--;

                // Dégâts de la baguette
                int dmg = TotalAttack();

                Console.WriteLine($"✨ Vous canalisez une {stoneLabel} et projetez une attaque {chosen} !");
                Console.WriteLine($"💥 La frappe magique ne peut pas être esquivée. Dégâts infligés : {dmg}");

                // Pas d’esquive → on applique directement
                bool hit = enemy.TakeDamage(dmg);
                if (hit)
                {
                    GainEssence(5);
                    if (!enemy.IsAlive)
                        GainEssence(10);
                }

                // Application / réaction d’élément (durée 3 tours par défaut)
                enemy.ApplyElementStatus(chosen, 3);

                Console.ReadKey(true);
                return true;
            }
        }

        public bool UseSpecialAttack(Enemy enemy)
        {
            if (SpecialAttacks.Count == 0)
            {
                Console.WriteLine("❌ Vous ne possédez aucune capacité spéciale.");
                Console.ReadKey(true);
                return false;
            }

            if (SpecialCooldown > 0)
            {
                Console.WriteLine($"⏳ Capacité spéciale indisponible. (Recharge : {SpecialCooldown} attaques restantes)");
                Console.ReadKey(true);
                return false;
            }

            if (SpecialAttacks.Count == 1)
            {
                SpecialAttacks[0].Execute(this, enemy);
                GainEssence(5);
                if (!enemy.IsAlive)
                    GainEssence(10);
            }
            else
            {
                Console.Clear();
                Console.WriteLine("🌀 Choisissez une capacité spéciale :");
                for (int i = 0; i < SpecialAttacks.Count; i++)
                    Console.WriteLine($"[{i + 1}] {SpecialAttacks[i].Name} - {SpecialAttacks[i].Description}");

                var input = Console.ReadKey(true).KeyChar.ToString();
                if (int.TryParse(input, out int choice) && choice >= 1 && choice <= SpecialAttacks.Count)
                {
                    SpecialAttacks[choice - 1].Execute(this, enemy);
                    GainEssence(5);
                    if (!enemy.IsAlive)
                        GainEssence(10);
                }
                else
                {
                    Console.WriteLine("Choix invalide. Tour perdu...");
                    Console.ReadKey(true);
                    return false;
                }
            }

            SpecialCooldown = 4;
            return true;
        }

        public void Defend(Enemy enemy)
        {
            IsDefending = true;
            Console.WriteLine("🛡️ Vous vous protégez avec votre bouclier !");
            int chance = 30;
            int roll = new Random().Next(100);
            if (roll < chance && !enemy.IsBoss)
            {
                enemy.StunTurns = 2;
                Console.WriteLine($"✨ {enemy.Name} est étourdi !");
            }
        }

        public void ChangeWeapon()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("Choisissez une arme :");
                Console.WriteLine("[0] Annuler");

                for (int i = 0; i < Weapons.Count; i++)
                    Console.WriteLine($"[{i + 1}] {Weapons[i].Name}  (Dégâts : {Weapons[i].BaseDamage})");

                var key = Console.ReadKey(true).KeyChar;

                if (key == '0')
                    return;

                if (int.TryParse(key.ToString(), out int idx) &&
                    idx >= 1 && idx <= Weapons.Count)
                {
                    CurrentWeapon = Weapons[idx - 1];
                    CombatContext.CurrentWeaponType = CurrentWeapon.Type;
                    return;
                }

                Console.WriteLine("❌ Choix invalide. Appuyez sur une touche...");
                Console.ReadKey(true);
            }
        }

        public int GainXPWithPotion(int baseXP)
        {
            int finalXP = baseXP;

            if (XPBoostKillsRemaining > 0)
            {
                finalXP *= 2;
                XPBoostKillsRemaining--;
                Console.WriteLine("📘 Potion d’XP active : gain d’expérience doublé !");
            }

            GainXP(finalXP);
            return finalXP;
        }

        public int GainGoldWithPotion(int baseGold)
        {
            int finalGold = baseGold;

            if (GoldBoostKillsRemaining > 0)
            {
                finalGold *= 2;
                GoldBoostKillsRemaining--;
                Console.WriteLine("💰 Potion de richesse active : gain d’or doublé !");
            }

            GainGold(finalGold);
            return finalGold;
        }

        public void ChoosePotion()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("Quelle potion utiliser ?");
                Console.WriteLine("[0] Annuler");

                int n = 1;
                var map = new Dictionary<int, PotionType>();
                foreach (var kv in Inventory)
                {
                    Console.WriteLine($"[{n}] {kv.Key} : {kv.Value}");
                    map[n] = kv.Key;
                    n++;
                }

                var key = Console.ReadKey(true).KeyChar;

                if (key == '0')
                    return;

                if (int.TryParse(key.ToString(), out int choice) && map.ContainsKey(choice))
                {
                    UsePotion(map[choice]);
                    Console.ReadKey(true);
                    return;
                }

                Console.WriteLine("❌ Choix invalide. Appuyez sur une touche...");
                Console.ReadKey(true);
            }
        }

        public void UsePotion(PotionType type)
        {
            if (Inventory[type] > 0)
            {
                Inventory[type]--;
                switch (type)
                {
                    case PotionType.Heal:
                        Health += 100;
                        Console.WriteLine("+100 PV récupérés !");
                        break;
                    case PotionType.SuperHeal:
                        Health += 250;
                        Console.WriteLine("+250 PV récupérés !");
                        break;
                    case PotionType.Luck:
                        LuckBuffTurns = 5;
                        Console.WriteLine("🍀 Chance accrue pendant 5 tours !");
                        break;
                    case PotionType.Damage:
                        DamageBuffTurns = 10;
                        Console.WriteLine("💥 Dégâts augmentés de 20 pendant 10 tours !");
                        break;
                    case PotionType.XP:
                        XPBoostKillsRemaining = 2;
                        Console.WriteLine("📘 Les 2 prochains ennemis vaincus donneront le double d'XP !");
                        break;
                    case PotionType.Gold:
                        GoldBoostKillsRemaining = 2;
                        Console.WriteLine("💰 Potion d’or activée ! Vos 2 prochains gains d’or seront doublés.");
                        break;

                }
            }
            else Console.WriteLine("Potion indisponible.");
        }

        public void ShowInventory()
        {
            Console.Clear();
            Console.WriteLine("Inventaire des potions :");
            foreach (var kv in Inventory)
                Console.WriteLine($"{kv.Key} : {kv.Value}");

            Console.WriteLine("\nInventaire des pierres élémentaires :");
            foreach (var kv in ElementStones)
                Console.WriteLine($"{kv.Key} : {kv.Value}");

            Console.WriteLine("Appuyez sur une touche...");
            Console.ReadKey(true);
        }

        public string ListBuffs()
        {
            List<string> buffs = new List<string>();

            if (DamageBuffTurns > 0)
                buffs.Add($"Potion dégâts +20 ({DamageBuffTurns} tour{(DamageBuffTurns > 1 ? "s" : "")})");

            if (SpecialAttackDamageBuffTurns > 0)
                buffs.Add($"Fracas du berserker +20 ({SpecialAttackDamageBuffTurns} tour{(SpecialAttackDamageBuffTurns > 1 ? "s" : "")})");

            if (LuckBuffTurns > 0)
                buffs.Add($"Chance accrue ({LuckBuffTurns} tour{(LuckBuffTurns > 1 ? "s" : "")})");
            if (ObsidianShieldTurns > 0)
                buffs.Add($"Mur d'obsidienne ({ObsidianShieldTurns})");


            return buffs.Count > 0 ? string.Join(", ", buffs) : "Aucun";
        }

        public void UpdateBuffs()
        {
            if (DamageBuffTurns > 0) DamageBuffTurns--;
            if (LuckBuffTurns > 0) LuckBuffTurns--;
            if (SpecialAttackDamageBuffTurns > 0) SpecialAttackDamageBuffTurns--;
            if (ObsidianShieldTurns > 0) ObsidianShieldTurns--;
        }

        public void TakeDamage(int amount)
        {
            int warningThreshold = 100;
            if (ObsidianShieldTurns > 0)
            {
                int reduced = amount / 3;
                Console.WriteLine("🛡️ Mur d’obsidienne absorbe 66% des dégâts !");
                amount = reduced;
            }
            else if (IsDefending)
            {
                int reduced = amount / 2;
                Console.WriteLine("🛡️ Vous bloquez l'attaque avec votre bouclier !");
                amount = reduced;
            }

            Health -= amount;
            if (Health < 0) Health = 0;

            Console.WriteLine($"💥 Vous subissez {amount} dégâts ! PV restants : {Health}");
            if (CurrentWave >= 11 && CurrentWave <= 20)
                warningThreshold = 150;
            else if (CurrentWave >= 31)
                warningThreshold = 200;

            if (Health <= warningThreshold && Health > 0)
            {
                Console.ReadKey(true);
                Console.WriteLine("⚠️ Attention, vous êtes gravement blessé !");
            }
            IsDefending = false;
        }

        public void PassTurn()
        {
            Console.WriteLine("Vous ne pouvez pas agir ce tour !");
        }

        public void GainXP(int amount)
        {
            XP += amount;
            while (XP >= XPToNextLevel)
            {
                XP -= XPToNextLevel;
                Level++;
                IncreaseMaxHealth(50);
                Console.WriteLine($"🎉 Niveau {Level} atteint ! PV max augmenté de 50 !");
            }
        }

        public void GainGold(int amount)
        {
            Gold += amount;
        }

        public void ReduceSpecialCooldown()
        {
            if (SpecialCooldown > 0)
                SpecialCooldown--;
        }

        public void IncreaseMaxHealth(int amount)
        {
            MaxHealth += amount;
            Health = MaxHealth;
        }

        public void ModifyMaxHealth(int delta)
        {
            MaxHealth += delta;
            if (MaxHealth < 1) MaxHealth = 1;
            if (Health > MaxHealth) Health = MaxHealth;
        }


        // Méthode pour le mini-jeu pour l'esquive et la contre attaque.
        private bool QuickPressMiniGame(string promptMessage, int timeLimitMs, out ConsoleKey expectedKey)
        {
            var random = new Random();
            // Génère une lettre majuscule aléatoire
            char keyChar = (char)random.Next('A', 'Z' + 1);
            expectedKey = (ConsoleKey)Enum.Parse(typeof(ConsoleKey), keyChar.ToString());

            Console.WriteLine($"{promptMessage} Presse la touche '{keyChar}' dans les {timeLimitMs / 1000.0:F1} secondes !");

            DateTime startTime = DateTime.Now;
            while ((DateTime.Now - startTime).TotalMilliseconds < timeLimitMs)
            {
                if (Console.KeyAvailable)
                {
                    var keyInfo = Console.ReadKey(true);
                    if (keyInfo.Key == expectedKey)
                    {
                        Console.WriteLine("✔️ Succès !");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("❌ Mauvaise touche.");
                        return false;
                    }
                }
            }

            Console.WriteLine("⏰ Trop lent !");
            return false;
        }

        public void LearnSpell(Spell s)
        {
            if (!LearnedSpells.Any(ls => ls.Formula == s.Formula))
            {
                LearnedSpells.Add(s);
                if (LearnedSpells.Count == 1)
                {
                    Essence = MaxEssence;
                    Console.WriteLine("✨ Vous sentez l'Essence affluer en vous !");
                }
            }
        }

        public void GainEssence(int amount)
        {
            if (!MagicUnlocked) return;
            Essence = Math.Min(MaxEssence, Essence + amount);
            Console.WriteLine($"🔮 +{amount} Essence  ({Essence}/{MaxEssence})");
        }

        public bool SpendEssence(int amount)
        {
            if (Essence >= amount)
            {
                Essence -= amount;
                return true;
            }
            Console.WriteLine("❌ Pas assez d'Essence !");
            return false;
        }

        public void Heal(int amount)
        {
            Health = Math.Min(MaxHealth, Health + amount);
            Console.WriteLine($"💚 Vous récupérez {amount} PV (PV : {Health}/{MaxHealth})");
        }

        public void ObtainWizardWand()
        {
            if (!Weapons.Any(w => w.Type == WeaponType.WizardWand))
            {
                Weapons.Add(new Weapon("Baguette du sorcier", 40, WeaponType.WizardWand));
                Console.WriteLine("🪄 Vous obtenez la Baguette du sorcier ! Vous pourrez utiliser vos pierres élémentaires grâce à elle.");
            }
        }

        public void ApplyElementStatus(ElementType type, int duration)
        {
            if (type == ElementType.None) return;

            // Vérifier si la cible a déjà l'élément opposé
            bool isMeltReaction =
                (CurrentElementStatus.Type == ElementType.Fire && type == ElementType.Ice) ||
                (CurrentElementStatus.Type == ElementType.Ice && type == ElementType.Fire);

            if (isMeltReaction)
            {
                TriggerMeltReaction();
                // On retire l'effet élémentaire précédent
                CurrentElementStatus = new ElementStatus(ElementType.None, 0);
                return;
            }

            // Sinon comportement normal
            if (CurrentElementStatus.Type == type)
            {
                if (duration > CurrentElementStatus.Duration)
                    CurrentElementStatus.Duration = duration;
            }
            else
            {
                CurrentElementStatus = new ElementStatus(type, duration);
            }
        }

        private void TriggerMeltReaction()
        {
            // Effet de fonte : ici on inflige des dégâts fixes, mais on peut l’adapter
            int meltDamage = 100;
            TakeDamage(meltDamage);
            Console.WriteLine($"💥 Réaction de fonte ! Vous subissez {meltDamage} dégâts !");
        }

        public void UpdateElementStatus()
        {
            if (!CurrentElementStatus.IsActive)
                return;

            switch (CurrentElementStatus.Type)
            {
                case ElementType.Fire:
                    int burnDamage = 20;
                    TakeElementalDamage(burnDamage, ElementType.Fire);
                    break;
                case ElementType.Ice:
                    break;
            }

            Console.ReadKey(true);
            CurrentElementStatus.Duration--;

            if (!CurrentElementStatus.IsActive)
            {
                switch (CurrentElementStatus.Type)
                {
                    case ElementType.Fire:
                        Console.WriteLine("🔥 Le feu s'éteint.");
                        break;
                    case ElementType.Ice:
                        Console.WriteLine("❄️ La glace fond.");
                        break;
                }

                CurrentElementStatus = new ElementStatus(ElementType.None, 0);
            }
        }

        public void TakeElementalDamage(int amount, ElementType type)
        {
            Health -= amount;
            if (Health < 0) Health = 0;

            Console.WriteLine($"💥 Vous subissez {amount} dégâts de {type.ToString().ToLower()} ! PV restants : {Health}");

            if (health <= 100 && health > 0)
            {
                Console.ReadKey(true);
                Console.WriteLine("⚠️ Attention, vous êtes gravement blessé !");
            }
        }
    }


    class Weapon
    {
        public string Name;
        public int BaseDamage;
        public int Level;
        public static int MaxLevel = 5;
        public WeaponType Type;

        public Weapon(string name, int baseDamage, WeaponType type)
        {
            Name = name;
            BaseDamage = baseDamage;
            Level = 1;
            Type = type;
        }
    }

    class Enemy
    {
        public string Name;
        public int Health;
        public int MaxHealth;
        public int AttackPower;
        public int XPValue;
        public int GoldValue;
        public bool IsAlive => Health > 0;
        protected Random random = new Random();
        public bool IsBoss { get; set; } = false;
        protected int attackTurnsRemaining;
        protected int restTurnsRemaining;
        public bool HasAlreadyActedThisTurn { get; set; }
        protected bool isResting;
        public bool IsResting => isResting;
        public ElementType Element { get; protected set; } = ElementType.None;
        public ElementStatus CurrentElementStatus { get; set; } = new ElementStatus(ElementType.None, 0);
        public int FreezeTurns { get; set; } = 0;
        public List<EnemyAttack> Attacks { get; set; } = new();
        public int StunTurns = 0;

        public Enemy(string name, int hp, int atk)
        {
            Name = name;
            MaxHealth = hp;
            Health = hp;
            AttackPower = atk;

            // Génération des récompenses
            GenerateLoot();

            if (IsBoss)
            {
                attackTurnsRemaining = random.Next(1, 6); // 1 à 5 attaques
                restTurnsRemaining = 0;
                isResting = false;
            }
        }

        public virtual void Attack(Player player)
        {
            if (StunTurns > 0)
            {
                Console.WriteLine($"{Name} est étourdi et ne peut pas attaquer !");
                StunTurns--;
                return;
            }

            Console.WriteLine();
            Console.WriteLine($"{Name} attaque !");
            int dmg = AttackPower;
            player.TakeDamage(dmg);
        }

        private void GenerateLoot()
        {
            string lowerName = Name.ToLower();

            if (lowerName.Contains("gobelin"))
            {
                XPValue = random.Next(5, 11);
                GoldValue = random.Next(5, 15);
            }
            else if (lowerName.Contains("troll"))
            {
                XPValue = random.Next(12, 21);
                GoldValue = random.Next(10, 25);
            }
            else if (lowerName.Contains("orc"))
            {
                XPValue = random.Next(22, 31);
                GoldValue = random.Next(20, 35);
            }
            else
            {
                XPValue = random.Next(8, 16);
                GoldValue = random.Next(5, 30);
            }
        }

        public virtual bool TakeDamage(int amount)
        {
            if (amount <= 0)
            {
                Console.WriteLine($"{Name} ne subit aucun dégât !");
                return false;
            }

            Health -= amount;
            Console.WriteLine($"{Name} subit {amount} dégâts !");
            return true;
        }

        public virtual void Act(Player player)
        {
            if (StunTurns > 0)
            {
                Console.WriteLine($"{Name} est étourdi et ne peut pas attaquer !");
                StunTurns--;
                return;
            }

            if (IsBoss)
            {
                if (isResting)
                {
                    if (restTurnsRemaining > 0)
                    {
                        if (restTurnsRemaining == 2 || restTurnsRemaining == 3)
                            Console.WriteLine($"😮 {Name} se repose.");
                        else
                            Console.WriteLine($"😮 {Name} se repose encore.");
                        restTurnsRemaining--;
                        return;
                    }
                    else
                    {
                        isResting = false;
                        attackTurnsRemaining = random.Next(1, 6); // 1 à 5 attaques
                    }
                }

                PerformRandomAttack(player);
                attackTurnsRemaining--;

                if (attackTurnsRemaining <= 0)
                {
                    isResting = true;
                    restTurnsRemaining = random.Next(1, 4); // Repos de 1 à 3 tours
                }
            }
            else
            {
                Attack(player);
            }
        }

        // Méthode pour effectuer une attaque aléatoire parmi celles disponibles (boss uniquement)
        protected void PerformRandomAttack(Player player)
        {
            var rand = new Random();
            double roll = rand.NextDouble();

            double cumulative = 0;
            foreach (var attack in Attacks)
            {
                cumulative += attack.Chance;
                if (roll <= cumulative)
                {
                    attack.Execute(player);
                    return;
                }
            }

            // Fallback si aucune attaque n’est choisie (très rare)
            Console.WriteLine($"{Name} hésite... et ne fait rien.");
        }

        public void ApplyElementStatus(ElementType type, int duration)
        {
            bool isMeltReaction =
                (CurrentElementStatus.Type == ElementType.Fire && type == ElementType.Ice) ||
                (CurrentElementStatus.Type == ElementType.Ice && type == ElementType.Fire);

            if (isMeltReaction)
            {
                TriggerMeltReaction();
                CurrentElementStatus = new ElementStatus(ElementType.None, 0);
                return;
            }

            if (CurrentElementStatus.Type == type)
            {
                if (duration > CurrentElementStatus.Duration)
                    CurrentElementStatus.Duration = duration;
            }
            else
            {
                CurrentElementStatus = new ElementStatus(type, duration);
            }

            Console.WriteLine($"⚠️ {Name} subit l'effet {type} pendant {duration} tours !");
        }

        private void TriggerMeltReaction()
        {
            int meltDamage = AttackPower * 10;
            TakeDamage(meltDamage);
            Console.WriteLine($"💥 Réaction de fonte sur {Name} ! Il subit {meltDamage} dégâts !");
        }

        public void UpdateElementStatus()
        {
            if (CurrentElementStatus.IsActive)
            {
                switch (CurrentElementStatus.Type)
                {
                    case ElementType.Fire:
                        int burnDamage = 20;
                        TakeDamage(burnDamage);
                        Console.WriteLine($"🔥 {Name} subit {burnDamage} dégâts de brûlure.");
                        break;

                    case ElementType.Ice:
                        StunTurns = CurrentElementStatus.Duration;
                        Console.WriteLine($"❄️ {Name} est gelé ({StunTurns} tours restants).");
                        break;
                }
                CurrentElementStatus.Duration--;
            }
        }
    }

    class EnemyAttack
    {
        public string Name { get; }
        public int MinDamage { get; }
        public int MaxDamage { get; }
        public double Chance { get; }
        public Action<Player>? Effect { get; }

        public EnemyAttack(string name, int min, int max, double chance, Action<Player>? effect = null)
        {
            Name = name;
            MinDamage = min;
            MaxDamage = max;
            Chance = chance;
            Effect = effect;
        }

        public void Execute(Player player)
        {
            int dmg = new Random().Next(MinDamage, MaxDamage + 1);
            Console.WriteLine($"⚔️ L'ennemi utilise {Name} et inflige {dmg} dégâts !");
            player.TakeDamage(dmg);
            Effect?.Invoke(player);
        }
    }

    class FireElemental : Enemy
    {
        public FireElemental(int wave)
            : base("Élémentaire de Feu", 100 + wave * 20, 10 + wave * 2)
        {
            Element = ElementType.Fire;
            CurrentElementStatus = new ElementStatus(ElementType.Fire, int.MaxValue);
        }

        public override bool TakeDamage(int amount)
        {
            if (CombatContext.CurrentWeaponType == WeaponType.Sword ||
                CombatContext.CurrentWeaponType == WeaponType.Greatsword ||
                CombatContext.CurrentWeaponType == WeaponType.Spear)
            {
                amount = (int)(amount * 0.2);
            }

            return base.TakeDamage(amount);
        }

        public override void Act(Player player)
        {
            base.Act(player);

            if (IsAlive && player.IsAlive)
            {
                int burnDuration = 3;
                player.ApplyElementStatus(Element, burnDuration);
                Console.WriteLine($"🔥 {Name} vous enflamme ! ({burnDuration} tours)");
            }
        }
    }

    class IceElemental : Enemy
    {
        public IceElemental(int wave)
            : base("Élémentaire de Glace", 100 + wave * 20, 10 + wave * 2)
        {
            Element = ElementType.Ice;
            CurrentElementStatus = new ElementStatus(ElementType.Ice, int.MaxValue);
        }

        public override bool TakeDamage(int amount)
        {
            if (CombatContext.CurrentWeaponType == WeaponType.Sword ||
                CombatContext.CurrentWeaponType == WeaponType.Greatsword ||
                CombatContext.CurrentWeaponType == WeaponType.Spear)
            {
                amount = (int)(amount * 0.2);
            }

            return base.TakeDamage(amount);
        }

        public override void Act(Player player)
        {
            base.Act(player);

            if (IsAlive && player.IsAlive)
            {
                int iceDuration = 3;
                player.ApplyElementStatus(Element, iceDuration);
                Console.WriteLine($"❄️ {Name} vous recouvre de glace ! ({iceDuration} tours)");
            }
        }
    }

    class DragonBoss : Enemy
    {
        public DragonBoss(int wave) : base("Dragon", 700 + wave * 50, 70 + wave * 5)
        {
            IsBoss = true;
            XPValue = 100;
            GoldValue = 100;

            Attacks = new List<EnemyAttack>
            {
                new EnemyAttack("Souffle ardent", 80, 120, 0.5),
                new EnemyAttack("Griffure", 40, 60, 0.3),
                new EnemyAttack("Hurlement terrifiant", 0, 0, 0.2, player =>
                {
                    Console.WriteLine("😱 Vous êtes intimidé et perdez votre bonus d’attaque !");
                    player.DamageBuffTurns = 0;
                })
            };
        }
    }

    class WormBoss : Enemy
    {
        public WormBoss(int wave) : base("Ver Géant", 600 + wave * 40, 55 + wave * 4)
        {
            IsBoss = true;
            XPValue = 100;
            GoldValue = 100;

            Attacks = new List<EnemyAttack>
            {
                new EnemyAttack("Émergence violente", 70, 100, 0.6, p =>
                {
                    if (!p.IsDefending)
                        Console.WriteLine("💥 L'attaque vous prend par surprise !");
                    else
                        Console.WriteLine("🛡️ Vous bloquez partiellement l’émergence !");
                }),
                new EnemyAttack("Vibration souterraine", 20, 30, 0.4, p =>
                {
                    Console.WriteLine("🌪️ Vous perdez votre équilibre, défense désactivée.");
                    p.IsDefending = false;
                })
            };
        }
    }

    class SpectralShadowBoss : Enemy
    {
        private Random rng = new Random();
        private bool justUsedFearCry = false;

        public SpectralShadowBoss(int wave) : base("Ombre Spectrale", 650 + wave * 40, 60 + wave * 4)
        {
            IsBoss = true;
            XPValue = 100;
            GoldValue = 100;

            Attacks = new List<EnemyAttack>
            {
                new EnemyAttack("Griffure fantomatique", 50, 70, 0.4),
                new EnemyAttack("Cri d'effroi", 0, 0, 0.3, p =>
                {
                    Console.WriteLine("😨 Vous êtes paralysé par la peur et perdrez votre prochain tour !");
                    p.SkipNextTurn = true;
                    justUsedFearCry = true;
                }),
                new EnemyAttack("Drain d’âme", 40, 55, 0.3, p =>
                {
                    int healAmount = rng.Next(20, 31);
                    Health = Math.Min(MaxHealth, Health + healAmount);
                    Console.WriteLine($"💉 L'Ombre Spectrale vole votre énergie vitale et récupère {healAmount} PV !");
                })
            };
        }

        public override void Act(Player player)
        {
            if (StunTurns > 0)
            {
                Console.WriteLine($"{Name} est étourdi et ne peut pas attaquer !");
                StunTurns--;
                return;
            }

            // Si l'attaque précédente était "Cri d'effroi", on doit forcément attaquer (pas de repos)
            if (justUsedFearCry)
            {
                PerformFearCryFollowUp(player);
                justUsedFearCry = false;
                return;
            }

            // Gestion normale du boss avec repos et attaque
            if (isResting)
            {
                if (restTurnsRemaining > 0)
                {
                    if (restTurnsRemaining == 2 || restTurnsRemaining == 3)
                        Console.WriteLine($"😮 {Name} se repose.");
                    else
                        Console.WriteLine($"😮 {Name} se repose encore.");
                    restTurnsRemaining--;
                    return;
                }
                else
                {
                    isResting = false;
                    attackTurnsRemaining = new Random().Next(1, 6); // 1 à 5 attaques
                }
            }

            PerformRandomAttack(player);
            attackTurnsRemaining--;

            if (attackTurnsRemaining <= 0)
            {
                isResting = true;
                restTurnsRemaining = new Random().Next(1, 4); // Repos de 1 à 3 tours
            }
        }
        private void PerformFearCryFollowUp(Player player)
        {
            var normalAttacks = Attacks.Where(a => a.Name != "Cri d'effroi").ToList();
            if (normalAttacks.Count == 0)
            {
                Console.WriteLine($"{Name} hésite... et ne fait rien.");
                return;
            }

            var attack = normalAttacks[new Random().Next(normalAttacks.Count)];
            attack.Execute(player);
        }

        public override bool TakeDamage(int amount)
        {
            if (isResting)
            {
                return base.TakeDamage(amount);
            }

            if (rng.NextDouble() < 0.3)
            {
                Console.WriteLine("💨 L'Ombre Spectrale devient invisible et esquive votre attaque !");
                return false;
            }

            return base.TakeDamage(amount);
        }
    }

    class SorcererBoss : Enemy
    {
        public SorcererBoss(int wave)
            : base("Sorcier", 600 + wave * 40, 60 + wave * 4)
        {
            IsBoss = true;
            XPValue = 120;
            GoldValue = 120;

            Attacks = new List<EnemyAttack>
        {
            // Attaque de feu
            new EnemyAttack("Boule de feu", 70, 90, 0.4, player =>
            {
                Console.WriteLine("🔥 Le sorcier lance une boule de feu !");
                player.ApplyElementStatus(ElementType.Fire, 3);
            }),

            // Attaque de glace
            new EnemyAttack("Éclair de glace", 70, 90, 0.4, player =>
            {
                Console.WriteLine("❄️ Le sorcier projette un éclair glacé !");
                player.ApplyElementStatus(ElementType.Ice, 3);
            }),

            // Attaque neutre pour varier
            new EnemyAttack("Décharge magique", 50, 70, 0.2, player =>
            {
                Console.WriteLine("✨ Le sorcier libère une vague d’énergie pure !");
            })
        };
        }
    }

    class EnemyFactory
    {
        static Random random = new Random();

        public static Enemy CreateEnemy(int wave)
        {
            int type;

            if (wave < 10)
            {
                type = random.Next(3);
            }
            else
            {
                type = random.Next(5);
            }

            switch (type)
            {
                case 0: return new Enemy("Gobelin", 150 + wave * 10, 30 + wave * 2);
                case 1: return new Enemy("Orc", 200 + wave * 15, 40 + wave * 3);
                case 2: return new Enemy("Troll", 250 + wave * 20, 50 + wave * 4);
                case 3: return new FireElemental(wave);
                case 4: return new IceElemental(wave);
                default: return new Enemy("Gobelin", 150 + wave * 10, 30 + wave * 2);
            }
        }

        public static Enemy CreateBoss(int wave)
        {
            // Si c'est la vague 10, on force le Sorcier
            if (wave == 10)
            {
                var sorcerer = new SorcererBoss(wave);
                sorcerer.IsBoss = true;
                return sorcerer;
            }

            // Sinon, on pioche un boss au hasard
            List<Func<int, Enemy>> bossConstructors = new List<Func<int, Enemy>>
            {
                w => new DragonBoss(w),
                w => new WormBoss(w),
                w => new SpectralShadowBoss(w)
            };

            int index = random.Next(bossConstructors.Count);
            var chosenBoss = bossConstructors[index](wave);
            chosenBoss.IsBoss = true;

            return chosenBoss;
        }

    }

    static class LootManager
    {
        static Random random = new Random();

        public static void DropPotion(Player player, Enemy enemy, Random rng)
        {
            int baseChance = 30;
            if (player.LuckBuffTurns > 0)
                baseChance += 20;

            int roll = rng.Next(100);
            if (roll > baseChance) return;

            // Poids des potions
            int weightLuck = 50;
            int weightHeal = 30;
            int weightDamage = 20;
            int weightSuperHeal = 10;
            int weightXP = 30;
            int weightGold = 30;

            // Si potion de chance active, augmenter poids des potions utiles
            if (player.LuckBuffTurns > 0)
            {
                weightHeal = (int)(weightHeal * 1.5);       // 45
                weightDamage = (int)(weightDamage * 1.5);   // 30
                weightSuperHeal = (int)(weightSuperHeal * 1.5); // 15
                weightXP = (int)(weightXP * 1.5);           // 45
                weightGold = (int)(weightGold * 1.5);       // 45
            }

            int totalWeight = weightLuck + weightHeal + weightDamage + weightSuperHeal + weightXP + weightGold;
            int potionRoll = rng.Next(totalWeight);

            PotionType potion;
            if (potionRoll < weightLuck)
                potion = PotionType.Luck;
            else if (potionRoll < weightLuck + weightHeal)
                potion = PotionType.Heal;
            else if (potionRoll < weightLuck + weightHeal + weightDamage)
                potion = PotionType.Damage;
            else if (potionRoll < weightLuck + weightHeal + weightDamage + weightSuperHeal)
                potion = PotionType.SuperHeal;
            else if (potionRoll < weightLuck + weightHeal + weightDamage + weightSuperHeal + weightXP)
                potion = PotionType.XP;
            else
                potion = PotionType.Gold;

            player.Inventory[potion]++;
            Console.WriteLine($"\n🎉 {enemy.Name} a laissé tomber une potion de type {potion} !");
        }
        public static void DropElementStone(Player player, Enemy enemy, Random rng)
        {
            if (enemy.Element == ElementType.Fire || enemy.Element == ElementType.Ice)
            {
                string stoneName = enemy.Element == ElementType.Fire ? "Pierre enflammée" : "Pierre congelée";

                // Drop garanti
                player.ElementStones[stoneName]++;
                Console.WriteLine($"\n💎 {enemy.Name} a laissé tomber une {stoneName} !");

                // 50% de chance pour une deuxième pierre
                if (rng.NextDouble() < 0.5)
                {
                    player.ElementStones[stoneName]++;
                    Console.WriteLine($"💎 Chance ! Vous trouvez une deuxième {stoneName} !");
                }
            }
        }
    }

    class Merchant
    {
        private class ShopItem
        {
            public string Label;
            public int Price;
            public int Stock;
            public Action<Player> Action;

            public ShopItem(string label, int price, int stock, Action<Player> action)
            {
                Label = label;
                Price = price;
                Stock = stock;
                Action = action;
            }
        }

        private List<ShopItem> shopItems;
        private Random random = new Random();
        private SpecialAttackType? specialForSaleThisVisit = null;

        public Merchant(int waveNumber)
        {
            int healStock = 20 + ((waveNumber >= 15) ? ((waveNumber - 5) / 10) * 10 : 0);
            int damageStock = 5 + ((waveNumber >= 15) ? ((waveNumber - 5) / 10) * 5 : 0);
            int superHealStock = 5 + ((waveNumber >= 15) ? ((waveNumber - 5) / 10) * 5 : 0);

            shopItems = new List<ShopItem>
            {
                new ShopItem("Potion de soin (+100 PV)", 5, healStock, p => p.Inventory[PotionType.Heal]++),
                new ShopItem("Potion de dégâts (+20 dégâts x10 tours)", 10, damageStock, p => p.Inventory[PotionType.Damage]++),
                new ShopItem("Super potion de soin (+250 PV)", 20, superHealStock, p => p.Inventory[PotionType.SuperHeal]++),
                new ShopItem("Augmentation PV max permanente (+200 PV)", 100, 1, p => p.IncreaseMaxHealth(200)),
                new ShopItem("Pierre enflammée", 30, 1, p =>
                {
                    if (!p.ElementStones.ContainsKey("Pierre enflammée"))
                        p.ElementStones["Pierre enflammée"] = 0;
                    p.ElementStones["Pierre enflammée"]++;
                    Console.WriteLine("🔥 Vous avez acheté une Pierre enflammée !");
                }),
                new ShopItem("Pierre congelée", 30, 1, p =>
                {
                    if (!p.ElementStones.ContainsKey("Pierre congelée"))
                        p.ElementStones["Pierre congelée"] = 0;
                    p.ElementStones["Pierre congelée"]++;
                    Console.WriteLine("❄️ Vous avez acheté une Pierre congelée !");
                })
            };
        }


        public void ShowShop(Player player)
        {
            if (specialForSaleThisVisit == null)
                specialForSaleThisVisit = GetRandomSpecialAttackNotOwned(player);

            while (true)
            {
                Console.Clear();
                Console.WriteLine("👺 Bienvenue chez le marchand ! Voici ce que vous pouvez acheter :");

                // Générer toutes les offres disponibles
                List<(string label, int price, Action<Player> action, ShopItem stockRef)> offers = shopItems
                    .Select(item =>
                    {
                        string suffix = item.Stock > 0 ? $" (Stock: {item.Stock})" : " [Rupture de stock]";
                        return ($"{item.Label}{suffix}", item.Price, item.Action, item);
                    })
                    .ToList();

                if (specialForSaleThisVisit != null)
                {
                    SpecialAttack special = new SpecialAttack(specialForSaleThisVisit.Value);
                    string label = $"Capacité spéciale : {special.Name} ({special.Description})";
                    int price = 50;
                    var specialType = specialForSaleThisVisit.Value;
                    Action<Player> grant = p =>
                    {
                        if (p.SpecialAttacks.Any(sp => sp.Type == specialType))
                        {
                            Console.WriteLine("❌ Vous possédez déjà cette capacité.");
                            return;
                        }

                        p.SpecialAttacks.Add(new SpecialAttack(specialType));
                        Console.WriteLine($"✨ Nouvelle capacité apprise : {special.Name} !");
                        var next = GetRandomSpecialAttackNotOwned(p);
                        if (next == null)
                        {
                            Console.WriteLine("🧙‍♂️ Vous connaissez désormais toutes les capacités disponibles !");
                            specialForSaleThisVisit = null;
                        }
                        else
                        {
                            specialForSaleThisVisit = next;
                        }
                    };

                    offers.Add((label, price, grant, null));
                }

                for (int i = 0; i < offers.Count; i++)
                    Console.WriteLine($"[{i + 1}] {offers[i].label} - {offers[i].price} gold");

                Console.WriteLine("[0] Quitter");
                Console.WriteLine($"\nVous avez {player.Gold} pièces d'or.\n");
                Console.Write("Votre choix : ");

                if (int.TryParse(Console.ReadLine(), out int choice))
                {
                    if (choice == 0)
                    {
                        specialForSaleThisVisit = null;
                        return;
                    }

                    if (choice > 0 && choice <= offers.Count)
                    {
                        var selected = offers[choice - 1];
                        var stockItem = selected.stockRef;

                        // Si c’est un item avec du stock (exclut la capacité spéciale)
                        if (stockItem != null)
                        {
                            if (stockItem.Stock <= 0)
                            {
                                Console.WriteLine("❌ Cet article est en rupture de stock !");
                            }
                            else if (stockItem.Stock == 1 || stockItem.Label.StartsWith("Augmentation PV"))
                            {
                                // Achat unique obligatoire
                                if (player.Gold >= selected.price)
                                {
                                    player.Gold -= selected.price;
                                    selected.action(player);
                                    stockItem.Stock--;
                                    Console.WriteLine($"✅ Vous avez acheté : {stockItem.Label}");
                                }
                                else
                                {
                                    Console.WriteLine("❌ Vous n’avez pas assez d’or !");
                                }
                            }
                            else
                            {
                                // Achat 1 ou tout le stock
                                Console.WriteLine($"\nSouhaitez-vous :");
                                Console.WriteLine($"[0] Annuler l'achat");
                                Console.WriteLine($"[1] Acheter 1 pour {stockItem.Price} or");
                                Console.WriteLine($"[2] Acheter tout le stock ({stockItem.Stock}) pour {stockItem.Stock * stockItem.Price} or");
                                Console.Write("Votre choix : ");
                                var input = Console.ReadLine()?.Trim();

                                if (input == "1")
                                {
                                    if (player.Gold >= stockItem.Price)
                                    {
                                        player.Gold -= stockItem.Price;
                                        stockItem.Action(player);
                                        stockItem.Stock--;
                                        Console.WriteLine($"✅ Vous avez acheté 1 x {stockItem.Label}");
                                    }
                                    else
                                    {
                                        Console.WriteLine("❌ Pas assez d’or !");
                                    }
                                }
                                else if (input == "2")
                                {
                                    int total = stockItem.Stock * stockItem.Price;
                                    if (player.Gold >= total)
                                    {
                                        player.Gold -= total;
                                        for (int i = 0; i < stockItem.Stock; i++) stockItem.Action(player);
                                        Console.WriteLine($"✅ Vous avez acheté tout le stock de {stockItem.Label} ({stockItem.Stock}) !");
                                        stockItem.Stock = 0;
                                    }
                                    else
                                    {
                                        int maxQty = player.Gold / stockItem.Price;
                                        if (maxQty == 0)
                                        {
                                            Console.WriteLine("❌ Vous n'avez pas assez d’or !");
                                        }
                                        else
                                        {
                                            Console.WriteLine($"\n❌ Pas assez d’or pour tout acheter.");
                                            Console.WriteLine($"Souhaitez-vous acheter {maxQty} x {stockItem.Label} pour {maxQty * stockItem.Price} or ? (O/N)");
                                            string confirm = Console.ReadLine()?.Trim().ToLower();
                                            if (confirm == "o")
                                            {
                                                player.Gold -= maxQty * stockItem.Price;
                                                for (int i = 0; i < maxQty; i++) stockItem.Action(player);
                                                stockItem.Stock -= maxQty;
                                                Console.WriteLine($"✅ Vous avez acheté {maxQty} x {stockItem.Label}");
                                            }
                                            else
                                            {
                                                Console.WriteLine("❌ Achat annulé.");
                                            }
                                        }
                                    }
                                }
                                else if (input == "0" || string.IsNullOrEmpty(input))
                                {
                                    Console.WriteLine("❌ Achat annulé.");
                                }
                                else
                                {
                                    Console.WriteLine("❌ Choix invalide.");
                                }
                            }
                        }
                        else
                        {
                            // Achat de la capacité spéciale
                            if (player.Gold >= selected.price)
                            {
                                player.Gold -= selected.price;
                                selected.action(player);
                                Console.WriteLine($"✅ Vous avez acheté : {selected.label}");
                            }
                            else
                            {
                                Console.WriteLine("❌ Vous n'avez pas assez d'or !");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("❌ Choix invalide.");
                    }
                }
                else
                {
                    Console.WriteLine("❌ Entrée invalide.");
                }

                Console.WriteLine("Appuyez sur une touche pour continuer...");
                Console.ReadKey(true);
            }
        }

        private SpecialAttackType? GetRandomSpecialAttackNotOwned(Player player)
        {
            var all = Enum.GetValues(typeof(SpecialAttackType)).Cast<SpecialAttackType>();
            var notOwned = all.Where(sa => !player.SpecialAttacks.Any(sp => sp.Type == sa)).ToList();
            if (notOwned.Count == 0) return null;
            return notOwned[random.Next(notOwned.Count)];
        }
    }

    class Blacksmith
    {
        public static void VisitBlacksmith(Player player)
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("🔨 Bienvenue chez le forgeron !");
                Console.WriteLine($"💰 Or disponible : {player.Gold}");
                Console.WriteLine("Quelle arme souhaitez-vous améliorer ?\n");

                for (int i = 0; i < player.Weapons.Count; i++)
                {
                    var weapon = player.Weapons[i];
                    int currentDmg = weapon.BaseDamage + (weapon.Level * 5);
                    int nextDmg = weapon.BaseDamage + ((weapon.Level + 1) * 5);
                    int upgradeCost = 50 + (weapon.Level * 30);

                    string info = weapon.Level < Weapon.MaxLevel
                        ? $"→ {nextDmg} après amélioration"
                        : "(niveau max atteint)";

                    Console.WriteLine($"[{i + 1}] {weapon.Name} (Niv {weapon.Level}) - Dégâts : {currentDmg} {info} - 💰 {upgradeCost}");
                }

                Console.WriteLine("\n[0] Quitter le forgeron");
                Console.Write("\nVotre choix : ");
                string input = Console.ReadLine();          // ← LECTURE AU CLAVIER + Entrée

                if (input == "0")
                    break;

                if (int.TryParse(input, out int choice) &&
                    choice >= 1 && choice <= player.Weapons.Count)
                {
                    var weapon = player.Weapons[choice - 1];

                    if (weapon.Level >= Weapon.MaxLevel)
                    {
                        Console.WriteLine("⚠️ Cette arme est déjà au niveau maximum !");
                    }
                    else
                    {
                        int upgradeCost = 50 + (weapon.Level * 30);
                        if (player.Gold >= upgradeCost)
                        {
                            player.Gold -= upgradeCost;
                            weapon.Level++;
                            Console.WriteLine($"✅ {weapon.Name} améliorée au niveau {weapon.Level} !");
                        }
                        else
                        {
                            Console.WriteLine("❌ Pas assez d’or pour cette amélioration !");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("❌ Choix invalide.");
                }
                Console.ReadKey(true);
            }
        }
    }

    class Spell
    {
        public string Name { get; }
        public string Formula { get; }
        public string Description { get; }
        public int EssenceCost { get; }
        public Action<Player, Enemy> Effect { get; }

        public Spell(string name, string hardcodedFormulaOrNull, string description,
                     int cost, Action<Player, Enemy> effect)
        {
            Name = name;
            Formula = (hardcodedFormulaOrNull ?? GenerateFormula()).ToLower();
            Description = description;
            EssenceCost = cost;
            Effect = effect;
        }

        private static string GenerateFormula()
        {
            var rng = new Random();
            int len = rng.Next(5, 9);
            char[] ch = new char[len];
            for (int i = 0; i < len; i++)
                ch[i] = (char)rng.Next('a', 'z' + 1);
            return new string(ch);
        }
    }

    class DarkMage
    {
        private static Random rng = new();

        private static List<Spell> MasterSpells = new()
    {
        new Spell("Vol de vie interdit", null,
            "Inflige 60 dégâts et rend 100 PV — coût : 90 Essence",
            90,
            (pl, en) =>
            {
                en.TakeDamage(60);
                pl.Heal(100);
                Console.WriteLine("🩸 Vous drainez l'énergie vitale de l'ennemi !");
            }),

        new Spell("Mur d’obsidienne", null,
            "Réduit de 66 % les dégâts reçus pendant 3 tours — coût : 60 Essence",
            60,
            (pl, en) =>
            {
                pl.ObsidianShieldTurns = 4; // 3 tours de protection
                pl.IsDefending = false;
                Console.WriteLine("🛡️ Une barrière d'obsidienne vous protège !");
            }),

        new Spell("Brisure d’arme", null,
            "Désarme l'ennemi et réduit ses dégâts de 30 % — coût : 60 Essence",
            60,
            (pl, en) =>
            {
                en.AttackPower = (int)(en.AttackPower * 0.7);
                Console.WriteLine($"🗡️ {en.Name} est désarmé !");
            })
    };

        public void OfferSpell(Player p)
        {
            // Spells que le joueur ne connaît pas encore
            var unknown = MasterSpells
                .Where(sp => !p.LearnedSpells.Any(ls => ls.Formula == sp.Formula))
                .ToList();

            if (unknown.Count == 0)
            {
                Console.WriteLine("Le mage noir n'a plus rien à vous enseigner...");
                Console.ReadKey(true);
                return;
            }

            var spell = unknown[rng.Next(unknown.Count)];

            Console.Clear();
            Console.WriteLine("Vous sentez une présence sinistre dans l'air...\n"); Console.ReadKey(true);
            Console.WriteLine("Quelqu'un apparaît juste derrière vous..."); Console.ReadKey(true);
            Console.WriteLine("🌑 Un mage noir vous tend un grimoire :");
            Console.WriteLine($" Voulez-vous apprendre « {spell.Name} » ? {spell.Description}");
            Console.Write("Apprendre ce sort ? (O/N)\n ");

            if (Console.ReadKey(true).Key == ConsoleKey.O)
            {
                // Utilisation de la méthode utilitaire
                p.LearnSpell(spell);

                // On révèle la formule une seule fois
                Console.WriteLine($"Cette formule ne vous sera jamais répétée, retenez là !\n Formule secrète : {spell.Formula.ToUpper()}");
            }
            else
            {
                Console.WriteLine("Vous déclinez l'offre sinistre.");
            }

            Console.ReadKey(true);
        }
    }

    public class ElementStatus
    {
        public ElementType Type { get; set; }
        public int Duration { get; set; }
        public bool IsActive => Duration > 0;

        public ElementStatus(ElementType type, int duration)
        {
            Type = type;
            Duration = duration;
        }
    }

}