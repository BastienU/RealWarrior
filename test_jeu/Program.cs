using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

class Program
{
    static void Main(string[] args)
    {
        InitializeGame();
    }

    public static void InitializeGame()
    {
        Inventory inventory = new Inventory();

        Console.WriteLine("Veuillez choisir le nom de votre héros.");
        string? heros_name = Console.ReadLine();

        if (string.IsNullOrEmpty(heros_name))
        {
            heros_name = "Héros";
        }

        Console.WriteLine();
        Console.WriteLine($"Bienvenue {heros_name} dans cette nouvelle aventure !");
        Console.WriteLine("Battez-vous pour la gloire et aller jusqu'au bout !");
        Console.WriteLine();
        Character player = new Character { Name = heros_name, Health = 500 };

        Console.WriteLine("Votre inventaire : ");
        Console.WriteLine("Vous commencez avec une épée équipée");
        player.Inventory.AddItem("Potion de soin");
        player.Inventory.AddItem("Potion de soin");
        player.Inventory.AddItem("Potion de chance");
        player.Inventory.AddItem("Potion de dégâts");
        Console.WriteLine();

        Combat combat = new Combat();
        combat.StartBattle(player, inventory);

        Console.WriteLine("Appuyez sur une touche pour quitter...");
        Console.ReadKey();
    }
}

#region Inventory
class Inventory
{
    private List<string> items = new List<string>();

    public void AddItem(string item)
    {
        items.Add(item);
        Console.WriteLine(item + " ajoutée à l'inventaire.");
    }

    public void ShowInventory()
    {
        Console.WriteLine("\n*** Inventaire ***");
        foreach (var item in items)
        {
            Console.WriteLine("- " + item);
        }
    }

    public bool UsePotion(Character player)
    {
        if (items.Contains("Potion de soin"))
        {
            items.Remove("Potion de soin");
            player.Heal(50); // Restaure la santé ici
            Console.WriteLine("Potion de soin utilisée! Santé restaurée de 50 points.");
            return true;
        }
        else
        {
            Console.WriteLine("Vous n'avez plus de potion de soin dans votre inventaire!");
            return false;
        }
    }

    public bool UseSuperPotion(Character player)
    {
        if (items.Contains("Super Potion de soin"))
        {
            items.Remove("Super Potion de soin");
            player.Heal(150); // Restaure la santé
            Console.WriteLine($"Super Potion de soin utilisée! Santé restaurée de 150 points.");
            return true;
        }
        else
        {
            Console.WriteLine("Vous n'avez plus de super potion de soin dans votre inventaire!");
            return false;
        }
    }

    public bool UseLuckPotion(Character player)
    {
        if (items.Contains("Potion de chance"))
        {
            items.Remove("Potion de chance");
            Console.WriteLine();
            Console.WriteLine("Potion de chance utilisée! Vos ennemis auront plus de chance de laisser une potion en mourant.");
            player.HasChancePotionActive = true; // Activer l'effet de la potion de chance
            return true;
        }
        return false;
    }

    public bool UseDamageBoostPotion(Character player)
    {
        if (items.Contains("Potion de dégâts"))
        {
            items.Remove("Potion de dégâts");
            player.IncreaseDamage(10, 10); // Augmente les dégâts de 10 pendant 10 tours.
            Console.WriteLine();
            Console.WriteLine("Potion de dégâts utilisée ! Vos dégâts sont augmentés temporairement.");
            return true;
        }
        return false;
    }

    public bool HasPotion()
    {
        return items.Contains("Potion de soin");
    }

    public bool HasSuperPotion()
    {
        return items.Contains("Super Potion de soin");
    }

    public bool HasLuckPotion()
    {
        return items.Contains("Potion de chance");
    }

    public bool HasDamageBoostPotion()
    {
        return items.Contains("Potion de dégâts");
    }

    public void LootPotion(Character enemy, Character player)
    {
        Random rand = new Random();
        int lootChance = rand.Next(1, 101);

        if (lootChance <= 20)
        {
            AddLoot("Potion de soin", player, enemy);
        }
        else if (lootChance <= 30)
        {
            AddLoot("Potion de chance", player, enemy);
            player.HasChancePotionActive = false;
        }
        else if (lootChance <= 35)
        {
            AddLoot("Potion de dégâts", player, enemy);
        }
        else if (lootChance <= 36)
        {
            AddLoot("Super Potion de soin", player, enemy);
        }
    }


    private void AddLoot(string loot, Character player, Character enemy)
    {
        player.Inventory.AddItem(loot);
    }
}
#endregion

#region Character
class Character
{
    public string? Name { get; set; }
    public int Health { get; set; }
    public int Level { get; set; } = 1;
    public int Experience { get; set; } = 0;
    public int BaseAttackDamage { get; set; } = 5;
    public bool IsTargeted { get; set; }
    public bool IsStunned { get; set; } // Indique si le personnage est actuellement étourdi
    public int StunCounter { get; set; } = 0; // Compteur de tours d'étourdissement
    public Weapon EquippedWeapon { get; set; }
    public int LuckPotionDuration { get; set; } = 0;
    public int SpecialAbilityCooldown { get; set; } = 0; // Pour la capacité spéciale
    public bool HasDamageBoostPotion { get; set; } = false; // Si la potion de dégâts est active
    public int DamageBoost { get; private set; } = 0;
    public int TotalDamage => BaseAttackDamage + DamageBoost; // Dégâts totaux (base + boost)
    public bool IsShielding { get; set; } = false; // Le joueur se protège avec son bouclier

    public Inventory Inventory { get; set; } = new Inventory(); // Inventaire du joueur

    public Character()
    {
        IsStunned = false;
        IsShielding = false;
    }
    public void TakeDamage(int amount)
    {
        if (IsShielding)
        {
            int reducedDamage = amount / 5; // Réduction de dégâts avec bouclier (ajustée à 50% pour plus de clarté)
            Health -= reducedDamage;
            Console.WriteLine($"{Name} a bloqué une partie des dégâts avec son bouclier! Dégâts subis: {reducedDamage} | Santé restante: {Health}");
            IsShielding = false; // Le bouclier ne dure que pendant un tour
        }
        else
        {
            Health -= amount;
            Console.WriteLine($"{Name} a subi {amount} points de dégâts. Santé restante: {Health}");
        }
    }

    public void Heal(int amount)
    {
        Health += amount;
        Console.WriteLine($"{Name} a récupéré {amount} points de santé. Santé actuelle: {Health}");
    }

    public void GainExperience(int amount)
    {
        Experience += amount;
        Console.WriteLine($"{Name} a gagné {amount} points d'expérience! Total: {Experience}");
        CheckLevelUp();
    }

    private void CheckLevelUp()
    {
        if (Experience >= Level * 100)
        {
            Level++;
            Health += 100; // Regain de PV
            BaseAttackDamage += 1; // Augmenter les dégâts de l'attaque de base
            Experience = 0;
            Console.WriteLine($"{Name} est monté au niveau {Level}! Santé: {Health}, Dégâts d'attaque: {BaseAttackDamage}");
        }
    }

    public void ApplyDamageBoost()
    {
        HasDamageBoostPotion = true;
        Console.WriteLine($"{Name} bénéficie d'un boost de dégâts de 10 points pendant les 10 prochains tours !");
    }

    public int Attack(List<Character> enemies, Character targetEnemy = null)
    {
        int damageDealt = EquippedWeapon.DealDamage() + BaseAttackDamage;

        if (HasDamageBoostPotion)
        {
            damageDealt += 10; // Augmente les dégâts de l'attaque de l'arme
            Console.WriteLine();
            Console.WriteLine($"{Name} attaque avec {EquippedWeapon.Name} et inflige {damageDealt} points de dégâts (bonus potion de dégâts) !");
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine($"{Name} attaque avec {EquippedWeapon.Name} et inflige {damageDealt} points de dégâts !");
        }

        // Si un ennemi spécifique est ciblé, appliquer les dégâts uniquement à cet ennemi
        if (targetEnemy != null)
        {
            targetEnemy.IsTargeted = true; // L'ennemi ciblé ne peut pas contre-attaquer ce tour
            if (targetEnemy.Health > 0)
            {
                targetEnemy.TakeDamage(damageDealt);  // Applique les dégâts uniquement à l'ennemi ciblé
            }
        }
        else
        {
            // Si aucun ennemi spécifique n'est ciblé (ou si c'est une autre arme), attaque tous les ennemis
            foreach (var enemy in enemies.Where(e => e.Health > 0))
            {
                enemy.TakeDamage(damageDealt);
            }
        }

        return damageDealt;
    }

    public void UseSpecialAbility(List<Character> enemies, Character player)
    {
        Random random = new Random();
        int damageTaken = random.Next(10, 25);

        if (SpecialAbilityCooldown > 0)
        {
            Console.WriteLine($"Votre capacité spéciale est en cours de recharge. Il vous reste {SpecialAbilityCooldown} tours avant de pouvoir l'utiliser.");
            return;
        }

        SpecialAbilityCooldown = 3; // Réinitialiser le cooldown à 3 tours

        // Compter les ennemis étourdis
        int stunnedCount = enemies.Count(enemy => enemy.IsStunned);

        // Augmenter les chances de toucher les ennemis en fonction du nombre d'ennemis étourdis
        // Plus d'ennemis étourdis = plus de chances de toucher
        float baseChanceToHit = 0.5f;  // 50% de chance de toucher par défaut
        float chanceToHit = baseChanceToHit + stunnedCount * 0.1f;  // Augmente de 10% par ennemi étourdi

        // S'assurer que la chance de toucher ne dépasse pas 100%
        chanceToHit = Math.Min(chanceToHit, 1.0f);

        Random rand = new Random();
        foreach (var enemy in enemies)
        {
            if (enemy.Health <= 0) continue;  // Si l'ennemi est déjà mort, on passe à l'ennemi suivant

            // Si l'ennemi est étourdi, il subit la capacité spéciale sans pouvoir attaquer
            if (enemy.IsStunned)
            {
                const int specialAbilityDamage = 50;
                enemy.Health -= specialAbilityDamage;
                Console.WriteLine($"{enemy.Name} est étourdi et a été automatiquement touché par la capacité spéciale, subissant {specialAbilityDamage} points de dégâts !");
                if (enemy.Health <= 0)
                {
                    Console.WriteLine($"{enemy.Name} a été vaincu !");
                }
                else
                {
                    Console.WriteLine($"{enemy.Name} est toujours vivant ! Santé restante : {enemy.Health}");
                }
                // L'ennemi ne peut pas attaquer après avoir été étourdi
                enemy.IsTargeted = true;
            }
            else
            {
                // Si l'ennemi n'est pas étourdi, on détermine si la capacité spéciale le touche
                if (rand.NextDouble() <= chanceToHit)
                {
                    const int specialAbilityDamage = 50;
                    enemy.Health -= specialAbilityDamage;
                    Console.WriteLine($"{enemy.Name} a été touché par la capacité spéciale et a subi {specialAbilityDamage} points de dégâts !");
                    if (enemy.Health <= 0)
                    {
                        Console.WriteLine($"{enemy.Name} a été vaincu !");
                    }
                    else
                    {
                        // Si l'ennemi est touché, il ne peut pas attaquer ce tour-ci
                        enemy.IsTargeted = true;
                        Console.WriteLine($"{enemy.Name} ne peut pas attaquer ! Santé restante : {enemy.Health}");
                    }
                }
                else
                {
                    // L'ennemi esquive, mais on ne doit afficher que le message de contre-attaque
                    if (rand.Next(2) == 0) // 50% chance d'esquiver et d'attaquer
                    {
                        Console.WriteLine($"{enemy.Name} esquive et contre-attaque !");
                        if (player != null && player.Health > 0)
                            player.TakeDamage(damageTaken);
                        else
                            Console.WriteLine("Aucune contre-attaque possible");
                    }
                    else
                    {
                        // Si l'ennemi esquive mais ne contre-attaque pas, afficher l'esquive
                        Console.WriteLine($"{enemy.Name} a esquivé l'attaque !");
                    }
                }
            }
        }
    }


    public bool HasChancePotionActive { get; set; } = false; // Si la potion de chance est active

    public void IncreaseDamage(int bonus, int durationInTurns)
    {
        DamageBoost = bonus;
        Console.WriteLine();
        Console.WriteLine($"Les dégâts sont augmentés de {bonus} points pour {durationInTurns} tours.");
    }

    public void DecreaseDamageBoost()
    {
        if (DamageBoost > 0)
        {
            DamageBoost = 0;
            Console.WriteLine("Le bonus de dégâts a expiré.");
        }
    }


}
#endregion

#region Weapon
class Weapon
{
    public string Name { get; set; }
    public int MinDamage { get; set; }
    public int MaxDamage { get; set; }
    public int AttackDelay { get; set; }
    public string Type { get; set; }

    public Weapon(string name, int minDamage, int maxDamage, int attackDelay, string type)
    {
        Name = name;
        MinDamage = minDamage;
        MaxDamage = maxDamage;
        AttackDelay = attackDelay;
        Type = type;
    }

    public int DealDamage()
    {
        Random random = new Random();
        return random.Next(MinDamage, MaxDamage + 1);
    }
}
#endregion

#region Combat
class Combat
{
    private Random random = new Random();
    private List<Character> enemies;

    public void StartBattle(Character player, Inventory inventory)
    {
        Weapon sword = new Weapon("Épée", 5, 15, 1, "mélée");
        Weapon greatSword = new Weapon("Espadon", 12, 25, 2, "mélée");
        Weapon spear = new Weapon("Lance", 5, 10, 1, "mélée");

        List<Weapon> availableWeapons = new List<Weapon> { sword, greatSword, spear };

        player.EquippedWeapon = sword;

        int wave = 1;
        while (player.Health > 0)  // Continue tant que le joueur est en vie
        {
            Console.WriteLine($"\n--- Vague {wave} ---");
            enemies = GenerateEnemies(wave);

            while (player.Health > 0 && enemies.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine($"{player.Name} - Santé: {player.Health} | Niveau: {player.Level} | XP: {player.Experience}");

                Console.WriteLine("Choisissez une action :");
                Console.WriteLine("1. Attaquer");
                Console.WriteLine("2. Défendre");
                Console.WriteLine("3. Utiliser une potion");
                Console.WriteLine("4. Changer d'arme");
                Console.WriteLine("5. Afficher l'inventaire");
                Console.WriteLine("6. Utiliser une capacité spéciale");
                Console.WriteLine();

                var choice = Console.ReadKey(true).KeyChar;
                Console.WriteLine();

                bool actionValide = true;

                // Action choisie
                if (choice == '1')  // Attaquer
                {
                    if (player.EquippedWeapon.Name == "Lance")  // Si la lance est équipée
                    {
                        // Choisir un ennemi spécifique à attaquer
                        Console.WriteLine("Choisissez un ennemi à cibler :");
                        for (int i = 0; i < enemies.Count; i++)
                        {
                            if (enemies[i].Health > 0) // N'afficher que les ennemis vivants
                            {
                                Console.WriteLine($"{i + 1}. {enemies[i].Name} (Santé: {enemies[i].Health})");
                            }
                        }

                        // Lire la sélection de l'utilisateur
                        char enemyChoice = Console.ReadKey(true).KeyChar;
                        int enemyIndex = int.TryParse(enemyChoice.ToString(), out enemyIndex) ? enemyIndex - 1 : -1;

                        if (enemyIndex >= 0 && enemyIndex < enemies.Count && enemies[enemyIndex].Health > 0)
                        {
                            // Si l'ennemi sélectionné est valide et vivant, attaque cet ennemi
                            Character targetEnemy = enemies[enemyIndex];
                            player.Attack(enemies, targetEnemy);  // Attaque ciblée sur un seul ennemi
                        }
                        else
                        {
                            Console.WriteLine("Choix invalide ou ennemi déjà mort.");
                            actionValide = false;  // Si l'ennemi est invalide, on empêche l'action
                        }
                    }
                    else if (player.EquippedWeapon.Name == "Épée")
                    {
                        Random rand = new Random();
                        int enemiesToHit = rand.Next(1, Math.Min(4, enemies.Count + 1));  // Choisit entre 1 et 3 ennemis, ou moins si moins d'ennemis sont présents

                        List<Character> enemiesHit = new List<Character>();

                        for (int i = 0; i < enemiesToHit; i++)
                        {
                            // Choisir un ennemi aléatoire qui n'a pas encore été touché
                            Character enemy = enemies[rand.Next(enemies.Count)];
                            if (!enemiesHit.Contains(enemy) && enemy.Health > 0)
                            {
                                enemiesHit.Add(enemy);
                                int damage = rand.Next(5, 15);
                                Console.WriteLine($"{player.Name} attaque avec {player.EquippedWeapon.Name} et inflige {damage} points de dégâts.");
                                enemy.TakeDamage(damage);
                                //Console.WriteLine($"{enemy.Name} a été touché par l'épée et a subi {damage} points de dégâts.");
                            }
                        }
                    }
                    else if (player.EquippedWeapon.Name == "Espadon")
                    {
                        Random rand = new Random();

                        // Vérifier si tous les ennemis sont étourdis
                        bool allEnemiesStunned = enemies.All(e => e.IsStunned);

                        if (allEnemiesStunned)
                        {
                            Console.WriteLine($"Tous les ennemis sont étourdis ! L'attaque avec {player.EquippedWeapon.Name} touche obligatoirement tous les ennemis !");
                            foreach (var enemy in enemies)
                            {
                                if (enemy.Health > 0)
                                {
                                    int damage = rand.Next(12, 25);  // Dégâts élevés pour l'espadon
                                    Console.WriteLine($"{player.Name} attaque avec {player.EquippedWeapon.Name} et inflige {damage} points de dégâts.");
                                    enemy.TakeDamage(damage);
                                }
                            }
                        }
                        else
                        {
                            // Vérifier si au moins un ennemi a été bloqué
                            bool enemyBlocked = enemies.Any(e => e.IsStunned || player.IsShielding);

                            // Ajuster les chances d'interruption
                            double interruptionChance = enemyBlocked ? 0.2 : 0.5; // 20% si un ennemi est bloqué, sinon 50%

                            if (rand.NextDouble() < interruptionChance)
                            {
                                Console.WriteLine($"{player.Name} a été interrompu, les ennemis attaquent !");
                                // Ici, les ennemis peuvent attaquer normalement
                            }
                            else
                            {
                                foreach (var enemy in enemies)
                                {
                                    if (enemy.Health > 0)  // Vérifier que l'ennemi est en vie
                                    {
                                        int damage = rand.Next(12, 25);  // Dégâts élevés pour l'espadon
                                        Console.WriteLine($"{player.Name} attaque avec {player.EquippedWeapon.Name} et inflige {damage} points de dégâts.");
                                        enemy.TakeDamage(damage);
                                    }
                                }
                            }
                        }
                    }

                    else
                    {
                        // Si l'arme équipée n'est pas la lance, attaque tous les ennemis
                        player.Attack(enemies);  // Attaque normale (tous les ennemis)
                    }
                }
                else if (choice == '2')
                {
                    Console.WriteLine($"{player.Name} lève son bouclier.");
                }
                else if (choice == '3')
                {
                    UsePotion(player, player.Inventory);
                    actionValide = false;
                }
                else if (choice == '4')
                {
                    ChangeWeapon(player, availableWeapons);
                }
                else if (choice == '5')
                {
                    player.Inventory.ShowInventory();
                    //DisplayInventory(inventory);
                    actionValide = false;
                }
                else if (choice == '6')
                {
                    UseSpecialAbility(player, enemies);
                }
                else
                {
                    Console.WriteLine("Touche invalide! Veuillez choisir une action valide.");
                    actionValide = false;
                }

                if (player.SpecialAbilityCooldown > 0)
                {
                    if (choice == '1' || choice == '2')
                    { 
                        player.SpecialAbilityCooldown--;
                    }
                    
                    if (player.SpecialAbilityCooldown == 0)
                    {
                        Console.WriteLine();
                        Console.WriteLine("Votre capacité spéciale est de nouveau disponible !");
                    }

                    if (choice == '6')
                    {
                        continue;
                    }
                }

                // Partie où les ennemis attaquent
                if (actionValide && choice != '3' && choice != '5') // Dans le cas de la touche 3 et 5, l'ennemi n'attaque pas
                {
                    foreach (var enemy in enemies)
                    {
                        if (enemy.Health > 0 && !enemy.IsTargeted) // Ennemis non ciblés peuvent attaquer
                        {
                            if (enemy.IsStunned)
                            {
                                Console.WriteLine($"{enemy.Name} est encore étourdi et ne peut pas attaquer ce tour-ci.");

                                enemy.StunCounter++;
                                if (enemy.StunCounter >= 2)
                                {
                                    enemy.IsStunned = false;  // Libère l’ennemi de l’étourdissement après 2 tours
                                    enemy.StunCounter = 0;    // Réinitialise le compteur
                                }
                            }
                            else
                            {
                                int damageTaken = random.Next(10, 25);
                                Console.WriteLine();
                                if (choice == '2')
                                    player.IsShielding = true;
                                else
                                    player.IsShielding = false;

                                // Étourdit l'ennemi immédiatement
                                if (random.NextDouble() < 0.3 && player.IsShielding) // 30% de chances d'étourdir
                                {
                                    enemy.IsStunned = true;
                                    enemy.StunCounter = 1; // Commence le compteur à 1 pour que l'effet dure deux tours
                                    Console.WriteLine($"{enemy.Name} est étourdi par le bouclier de {player.Name} et ne pourra pas attaquer au prochain tour !");
                                }
                                else
                                {
                                    Console.WriteLine($"{enemy.Name} attaque et inflige {damageTaken} points de dégâts à {player.Name}!");
                                    player.TakeDamage(damageTaken);
                                    Console.WriteLine();
                                }
                            }
                        }
                    }
                }




                // Réinitialisation de l'état de tous les ennemis après chaque tour pour permettre aux ennemis de contre-attaquer dans le prochain tour
                foreach (var enemy in enemies)
                {
                    if (enemy.Health > 0)
                    {
                        enemy.IsTargeted = false;  // L'ennemi peut à nouveau attaquer dans le tour suivant
                    }
                }

                // Vérification de la fin de la bataille
                if (player.Health <= 0)
                {
                    Console.WriteLine($"{player.Name} a été vaincu !");
                    break;
                }

                // Gestion de la mort des ennemis et appel du loot
                var deadEnemies = enemies.Where(e => e.Health <= 0).ToList();
                foreach (var enemy in deadEnemies)
                {
                    inventory.LootPotion(enemy, player); // Appel à LootPotion si l'ennemi est mort
                    enemies.Remove(enemy); // Supprime l'ennemi mort de la liste principale
                }

                if (enemies.All(e => e.Health <= 0))
                {
                    Console.WriteLine("Tous les ennemis de la vague ont été vaincus!");
                    break;
                }
            }

            // Si tous les ennemis sont vaincus, passe à la vague suivante
            if (player.Health > 0)
            {
                wave++;
            }
            else
            {
                Console.WriteLine("Le jeu est terminé !");
                Console.WriteLine("Appuyez sur une touche pour continuer...");
                Console.ReadKey(true);  // Attend que l'utilisateur appuie sur une touche
                Console.WriteLine("Voulez-vous rejouer ?");
                Console.WriteLine("1 : Rejouer || 2 : Quitter");

                var choice = Console.ReadKey(true).KeyChar;

                switch (choice)
                {
                    case '1':
                        Console.Clear();
                        Program.InitializeGame();
                        break;

                    case '2':
                        Console.WriteLine("Merci d'avoir joué !");
                        Console.ReadKey(true);
                        Environment.Exit(0);
                        break;

                    default:
                        Console.WriteLine("Choix invalide. Veuillez réessayer.");
                        break;
                }

                break;  // Fin du jeu si le joueur meurt
            }
        }

    }




    private List<Character> GenerateEnemies(int wave)
    {
        List<Character> enemies = new List<Character>();
        for (int i = 0; i < wave; i++)
        {
            Character enemy = new Character { Name = $"Ennemi {i + 1}", Health = 100 + (i * 20), Level = wave };
            enemies.Add(enemy);
        }
        return enemies;
    }

    public void PerformAttack(Character player, List<Character> enemies)
    {
        // Si un ennemi spécifique a été ciblé, on l'attaque
        if (player.EquippedWeapon.Name == "Lance")
        {
            // Demander à choisir un ennemi (cela a été fait dans StartBattle)
            // Nous avons déjà géré ce cas dans la partie StartBattle ci-dessus
        }
        else
        {
            // Attaque tous les ennemis si ce n'est pas la lance
            foreach (var enemy in enemies)
            {
                if (enemy.Health > 0)
                {
                    int damageDealt = player.Attack(enemies);
                    Console.WriteLine($"{enemy.Name} a reçu {damageDealt} points de dégâts.");
                }
            }
        }
    }


    // Méthode affichant le menu de choix de potion à utiliser.
    private void UsePotion(Character player, Inventory inventory)
    {
        Console.WriteLine("Quel type de potion souhaitez-vous utiliser?");
        Console.WriteLine("1. Potion de soin");
        Console.WriteLine("2. Super Potion de soin");
        Console.WriteLine("3. Potion de chance");
        Console.WriteLine("4. Potion de dégâts");

        var choice = Console.ReadKey(true).KeyChar;

        switch (choice)
        {
            case '1':
                Console.WriteLine();
                inventory.UsePotion(player);
                break;
            case '2':
                Console.WriteLine();
                inventory.UseSuperPotion(player);
                break;
            case '3':
                if (!inventory.UseLuckPotion(player))
                {
                    Console.WriteLine();
                    Console.WriteLine("Vous n'avez pas de potion de chance.");
                }
                break;
            case '4':
                if (!inventory.UseDamageBoostPotion(player))
                {
                    Console.WriteLine();
                    Console.WriteLine("Vous n'avez pas de potion de dégâts.");
                }
                break;
            default:
                Console.WriteLine();
                Console.WriteLine("Choix invalide.");
                Console.WriteLine();
                break;
        }
    }

    private void ChangeWeapon(Character player, List<Weapon> availableWeapons)
    {
        Console.WriteLine("Choisissez une arme :");
        for (int i = 0; i < availableWeapons.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {availableWeapons[i].Name} (Dégâts : {availableWeapons[i].MinDamage}-{availableWeapons[i].MaxDamage})");
        }

        var weaponChoice = Console.ReadKey(true).KeyChar;
        int weaponIndex = int.TryParse(weaponChoice.ToString(), out weaponIndex) ? weaponIndex - 1 : -1;

        if (weaponIndex >= 0 && weaponIndex < availableWeapons.Count)
        {
            player.EquippedWeapon = availableWeapons[weaponIndex];
            Console.WriteLine();
            Console.WriteLine($"{player.Name} a équipé {player.EquippedWeapon.Name} !");
            Console.WriteLine();
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine("Choix invalide.");
            Console.WriteLine();
        }
    }

    private void UseSpecialAbility(Character player, List<Character> enemies)
    {
        player.UseSpecialAbility(enemies, player);
    }
}
#endregion