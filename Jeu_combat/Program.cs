// Jeu d'aventure/survie en console - version évoluée avec capacités spéciales
using System;
using System.Collections.Generic;
using System.Linq;

namespace JeuSurvieConsole
{
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
        Merchant merchant = new Merchant();

        public void Start()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            while (true)
            {
                player = new Player();
                waveNumber = 1;

                Console.Clear();
                Console.WriteLine("Bienvenue dans cette nouvelle aventure !");
                Console.WriteLine("Appuyez sur une touche pour commencer...");
                Console.ReadKey(true);

                while (player.IsAlive)
                {
                    // Juste après la vague 9, avant la vague 10 (boss), on visite le forgeron
                    if (waveNumber == 10)
                    {
                        Console.Clear();
                        Console.WriteLine("⚠️ Vous vous apprêtez à affronter un terrible boss !");
                        Console.WriteLine("Il serait sage d'améliorer vos armes pour sortir victorieux de ce combat !");
                        Console.WriteLine("\nAppuyez sur une touche pour aller voir le forgeron...");
                        Console.ReadKey(true);

                        Blacksmith.VisitBlacksmith(player);
                    }

                    if (waveNumber % 10 == 0)
                        currentEnemy = EnemyFactory.CreateBoss(waveNumber);
                    else
                        currentEnemy = EnemyFactory.CreateEnemy(waveNumber);

                    CombatLoop();

                    if (!player.IsAlive)
                        break;

                    // Apparition du marchand toutes les 5 vagues, sauf à la vague 10 (boss)
                    if (waveNumber % 5 == 0 && waveNumber != 10)
                    {
                        merchant.ShowShop(player);
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
                DrawUI();

                // Reset de la défense au début du tour du joueur
                player.IsDefending = false;

                bool playerActed = false;
                ConsoleKey key = Console.ReadKey(true).Key;

                switch (key)
                {
                    case ConsoleKey.D1:
                        player.Attack(currentEnemy);
                        player.ReduceSpecialCooldown();
                        playerActed = true;
                        break;

                    case ConsoleKey.D2:
                        player.Defend(currentEnemy);
                        playerActed = true;
                        break;

                    case ConsoleKey.D3:
                        player.ChangeWeapon();
                        break;

                    case ConsoleKey.D4:
                        player.ChoosePotion();
                        break;

                    case ConsoleKey.D5:
                        player.ShowInventory();
                        break;

                    case ConsoleKey.D6:
                        player.PassTurn();
                        playerActed = true;
                        break;

                    case ConsoleKey.D7:
                        playerActed = player.UseSpecialAttack(currentEnemy);
                        break;
                }

                if (playerActed && currentEnemy.IsAlive)
                {
                    currentEnemy.Act(player);
                    Console.ReadKey(true);
                }

                player.UpdateBuffs();

                if (!currentEnemy.IsAlive)
                {
                    int goldReward = random.Next(5, 31);
                    player.GainGold(goldReward);
                    Console.WriteLine($"💰 Vous récupérez {goldReward} pièces d’or !");

                    if (currentEnemy.XPValue > 0)
                    {
                        player.GainXP(currentEnemy.XPValue);
                    }

                    LootManager.DropPotion(player, currentEnemy, random);
                    Console.ReadKey(true);
                }

            }
        }

        void DrawUI()
        {
            Console.Clear();
            Console.WriteLine($"========== VAGUE {waveNumber} ==========");
            Console.WriteLine($"👤 Joueur : {player.Health}/{player.MaxHealth} PV | Arme : {player.CurrentWeapon.Name} | Atk: {player.TotalAttack()} | Or: {player.Gold} | XP: {player.XP}/{player.XPToNextLevel}");
            Console.WriteLine($"🧪 Buffs : {player.ListBuffs()} | Cooldown Spécial : {player.SpecialCooldown}/4\n");
            Console.WriteLine($"🦾 Ennemi : {currentEnemy.Name} - PV : {currentEnemy.Health}/{currentEnemy.MaxHealth}\n");
            Console.WriteLine("[1] Attaquer  [2] Se défendre  [3] Changer d'arme  [4] Potion  [5] Inventaire  [6] Passer  [7] Capacité spéciale\n");
        }
    }


    // Nouvelle classe SpecialAttack

    enum SpecialAttackType
    {
        DoubleStrike,
        BerserkerSmash,
        StunningBlow
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
                    Description = "Inflige des dégâts et étourdit l'ennemi pendant 3 tours.";
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
            Console.WriteLine($"Coup Étourdisant ! {enemy.Name} reçoit {dmg} dégâts et est étourdi pendant 3 tours !");
            enemy.TakeDamage(dmg);
            enemy.StunTurns = 3;
        }
    }



    class Player
    {
        private int health;  // champ privé pour Health
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
        public int DamageBuffTurns = 0;
        public int LuckBuffTurns = 0;
        public bool IsDefending = false;
        public int Gold = 0;
        public List<SpecialAttack> SpecialAttacks = new List<SpecialAttack>();
        public int SpecialCooldown = 0;

        public Player()
        {
            Weapons.Add(new Weapon("Épée", 50));
            Weapons.Add(new Weapon("Espadon", 65));
            Weapons.Add(new Weapon("Lance", 45));
            CurrentWeapon = Weapons[0];

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

        public void Attack(Enemy enemy)
        {
            int dmg = TotalAttack();
            Console.WriteLine($"Vous attaquez {enemy.Name} avec votre {CurrentWeapon.Name} et infligez {dmg} dégâts !");
            enemy.TakeDamage(dmg);
        }

        public bool UseSpecialAttack(Enemy enemy)
        {
            if (SpecialAttacks.Count == 0)
            {
                Console.WriteLine("❌ Vous ne possédez aucune capacité spéciale.");
                return false;
            }

            if (SpecialCooldown > 0)
            {
                Console.WriteLine($"⏳ Capacité spéciale indisponible. (Recharge : {SpecialCooldown} attaques restantes)");
                return false;
            }

            if (SpecialAttacks.Count == 1)
            {
                SpecialAttacks[0].Execute(this, enemy);
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
                }
                else
                {
                    Console.WriteLine("Choix invalide. Tour perdu...");
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
            if (roll < chance)
            {
                enemy.StunTurns = 2;
                Console.WriteLine($"✨ Vous avez étourdi {enemy.Name} !");
            }
        }

        public void ChangeWeapon()
        {
            Console.Clear();
            Console.WriteLine("Choisissez une arme :");
            for (int i = 0; i < Weapons.Count; i++)
                Console.WriteLine($"[{i}] {Weapons[i].Name} (Dégâts : {Weapons[i].BaseDamage})");

            if (int.TryParse(Console.ReadKey(true).KeyChar.ToString(), out int index) && index >= 0 && index < Weapons.Count)
                CurrentWeapon = Weapons[index];
        }

        public void ChoosePotion()
        {
            Console.Clear();
            Console.WriteLine("Quelle potion utiliser ?");
            int i = 1;
            Dictionary<int, PotionType> mapping = new Dictionary<int, PotionType>();
            foreach (var kv in Inventory)
            {
                Console.WriteLine($"[{i}] {kv.Key} : {kv.Value}");
                mapping[i] = kv.Key;
                i++;
            }
            var key = Console.ReadKey(true).KeyChar.ToString();
            if (int.TryParse(key, out int choice) && mapping.ContainsKey(choice))
                UsePotion(mapping[choice]);
            else
                Console.WriteLine("Choix invalide.");
            Console.ReadKey(true);
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
                }
            }
            else Console.WriteLine("Potion indisponible.");
        }

        public void ShowInventory()
        {
            Console.Clear();
            Console.WriteLine("Inventaire :");
            foreach (var kv in Inventory)
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

            return buffs.Count > 0 ? string.Join(", ", buffs) : "Aucun";
        }

        public void UpdateBuffs()
        {
            if (DamageBuffTurns > 0) DamageBuffTurns--;
            if (LuckBuffTurns > 0) LuckBuffTurns--;
            if (SpecialAttackDamageBuffTurns > 0) SpecialAttackDamageBuffTurns--;

            IsDefending = false;
        }

        public void TakeDamage(int amount)
        {
            if (IsDefending)
            {
                int reduced = amount / 2;
                Console.WriteLine("🛡️ Vous bloquez avec votre bouclier ! Dégâts réduits de moitié !");
                amount = reduced;
            }

            Health -= amount;
            if (Health < 0) Health = 0;

            Console.WriteLine($"💥 Vous subissez {amount} dégâts ! PV restants : {Health}");

            IsDefending = false;
        }


        public void PassTurn()
        {
            Console.WriteLine("Vous passez votre tour.");
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

    }


    class Weapon
    {
        public string Name;
        public int BaseDamage;
        public int Level;
        public static int MaxLevel = 5;
        public Weapon(string name, int baseDamage)
        {
            Name = name;
            BaseDamage = baseDamage;
            Level = 1;
        }
    }

    enum PotionType { Heal, SuperHeal, Luck, Damage }

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
        public int PhaseTurnCounter { get; set; } = 0;
        public int ActivePhaseTurns { get; set; } = 3;
        public int RestPhaseTurns { get; set; } = 1;
        public List<EnemyAttack> Attacks { get; set; } = new();


        // Ajout d’un compteur d’étourdissement
        public int StunTurns = 0;

        public Enemy(string name, int hp, int atk)
        {
            Name = name;
            MaxHealth = hp;
            Health = hp;
            AttackPower = atk;

            // Génération des récompenses
            GenerateLoot();
        }

        public virtual void Attack(Player player)
        {
            if (StunTurns > 0)
            {
                Console.WriteLine($"{Name} est étourdi et ne peut pas attaquer !");
                StunTurns--;
                return;
            }

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
            else if (lowerName.Contains("orc"))
            {
                XPValue = random.Next(12, 21);
                GoldValue = random.Next(10, 25);
            }
            else
            {
                XPValue = random.Next(8, 16);
                GoldValue = random.Next(5, 30);
            }
        }

        public virtual void TakeDamage(int amount)
        {
            Health -= amount;
            Console.WriteLine($"{Name} subit {amount} dégâts !");
        }

        public void Act(Player player)
        {
            if (IsBoss)
            {
                PhaseTurnCounter++;

                if (PhaseTurnCounter <= ActivePhaseTurns)
                {
                    PerformRandomAttack(player);
                }
                else if (PhaseTurnCounter <= ActivePhaseTurns + RestPhaseTurns)
                {
                    Console.WriteLine($"😮 {Name} semble reprendre son souffle... Il ne fait rien ce tour.");
                }
                else
                {
                    PhaseTurnCounter = 0;
                }
            }
            else
            {
                Attack(player);
            }
        }

        private void PerformRandomAttack(Player player)
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

    class EnemyFactory
    {
        static Random random = new Random();

        public static Enemy CreateEnemy(int wave)
        {
            int type = random.Next(3);
            switch (type)
            {
                case 0: return new Enemy("Gobelin", 150 + wave * 10, 30 + wave * 2);
                case 1: return new Enemy("Orc", 200 + wave * 15, 40 + wave * 3);
                case 2: return new Enemy("Troll", 250 + wave * 20, 50 + wave * 4);
                default: return new Enemy("Gobelin", 150 + wave * 10, 30 + wave * 2);
            }
        }

        public static Enemy CreateBoss(int wave)
        {
            List<Func<int, Enemy>> bossConstructors = new List<Func<int, Enemy>>
            {
                w => new DragonBoss(w),
                w => new WormBoss(w)
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
                baseChance += 20; // +20% chance de drop avec potion chance

            int roll = rng.Next(100);
            if (roll > baseChance) return; // pas de potion drop

            // Poids des potions selon la demande (Chance > Heal > Damage > SuperHeal)
            int chanceTotal = 100;
            int weightChance = 40;
            int weightHeal = 30;
            int weightDamage = 20;
            int weightSuperHeal = 10;

            int potionRoll = rng.Next(chanceTotal);
            PotionType potion;
            if (potionRoll < weightChance)
                potion = PotionType.Luck;
            else if (potionRoll < weightChance + weightHeal)
                potion = PotionType.Heal;
            else if (potionRoll < weightChance + weightHeal + weightDamage)
                potion = PotionType.Damage;
            else
                potion = PotionType.SuperHeal;

            player.Inventory[potion]++;
            Console.WriteLine($"\n🎉 {enemy.Name} a laissé tomber une potion de type {potion} !");
            Console.WriteLine("Appuyez sur une touche pour continuer...");
            Console.ReadKey(true);
        }
    }

    class Merchant
    {
        private Dictionary<string, (int price, Action<Player>)> shopItems;
        private Random random = new Random();

        // Capacité proposée pour la session actuelle
        private SpecialAttackType? specialForSaleThisVisit = null;

        public Merchant()
        {
            shopItems = new Dictionary<string, (int, Action<Player>)>
        {
            { "Potion de soin (+100 PV)", (5, p => p.Inventory[PotionType.Heal]++) },
            { "Potion de dégâts (+20 dégâts x10 tours)", (10, p => p.Inventory[PotionType.Damage]++) },
            { "Super potion de soin (+250 PV)", (20, p => p.Inventory[PotionType.SuperHeal]++) },
            { "Augmentation PV max permanente (+200 PV)", (100, p => p.IncreaseMaxHealth(200)) },
        };
        }

        public void ShowShop(Player player)
        {
            // Choisir une capacité spéciale une seule fois par visite
            if (specialForSaleThisVisit == null)
                specialForSaleThisVisit = GetRandomSpecialAttackNotOwned(player);

            while (true)
            {
                Console.Clear();
                Console.WriteLine("👺 Bienvenue chez le marchand ! Voici ce que vous pouvez acheter :");

                List<(string label, int price, Action<Player> action)> offers = shopItems
                    .Select(kv => (kv.Key, kv.Value.price, kv.Value.Item2))
                    .ToList();

                if (specialForSaleThisVisit != null)
                {
                    SpecialAttack special = new SpecialAttack(specialForSaleThisVisit.Value);
                    string label = $"Capacité spéciale : {special.Name} ({special.Description})";
                    int price = 50;
                    var specialType = specialForSaleThisVisit.Value;
                    Action<Player> grant = p =>
                    {
                        // Vérifie que le joueur ne possède pas déjà cette capacité
                        if (p.SpecialAttacks.Any(sp => sp.Type == specialType))
                        {
                            Console.WriteLine("❌ Vous possédez déjà cette capacité.");
                            return;
                        }

                        // Ajoute la capacité
                        p.SpecialAttacks.Add(new SpecialAttack(specialType));
                        Console.WriteLine($"✨ Nouvelle capacité apprise : {special.Name} !");

                        // On propose une nouvelle capacité s'il en reste
                        var next = GetRandomSpecialAttackNotOwned(p);
                        if (next == null)
                        {
                            Console.WriteLine("🧙‍♂️ Vous connaissez désormais toutes les capacités disponibles !");
                            specialForSaleThisVisit = null; // Plus rien à vendre
                        }
                        else
                        {
                            specialForSaleThisVisit = next; // Nouvelle capacité aléatoire
                        }
                    };
                    offers.Add((label, price, grant));
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
                        // On quitte le marchand, donc on réinitialisera la capacité à la prochaine visite
                        specialForSaleThisVisit = null;
                        return;
                    }

                    if (choice > 0 && choice <= offers.Count)
                    {
                        var selected = offers[choice - 1];
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
                    int currentDamage = weapon.BaseDamage + (weapon.Level * 5);
                    int nextDamage = weapon.BaseDamage + ((weapon.Level + 1) * 5);
                    int upgradeCost = 50 + (weapon.Level * 30);

                    string upgradeInfo = weapon.Level < Weapon.MaxLevel
                        ? $"→ {nextDamage} après amélioration"
                        : "(niveau max atteint)";

                    Console.WriteLine($"[{i + 1}] {weapon.Name} (Niv {weapon.Level}) - Dégâts : {currentDamage} {upgradeInfo} - 💰 {upgradeCost}");
                }

                Console.WriteLine("\n[0] Quitter le forgeron");

                Console.Write("\nVotre choix : ");
                var input = Console.ReadKey(true).KeyChar;

                if (input == '0')
                    break;

                if (int.TryParse(input.ToString(), out int choice) && choice >= 1 && choice <= player.Weapons.Count)
                {
                    var weapon = player.Weapons[choice - 1];

                    if (weapon.Level >= Weapon.MaxLevel)
                    {
                        Console.WriteLine("⚠️ Cette arme est déjà au niveau maximum !");
                        Console.WriteLine("Appuyez sur une touche pour continuer...");
                        Console.ReadKey(true);
                        continue;
                    }

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
                else
                {
                    Console.WriteLine("❌ Choix invalide.");
                }

                Console.WriteLine("Appuyez sur une touche pour continuer...");
                Console.ReadKey(true);
            }
        }
    }

}