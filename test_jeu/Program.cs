﻿using System;
using System.Collections.Generic;
using System.Linq;

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
        string heros_name = Console.ReadLine();

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
            player.LuckPotionDuration = 3;
            return true;
        }
        return false;
    }

    public bool UseDamageBoostPotion(Character player)
    {
        if (items.Contains("Potion de dégâts"))
        {
            items.Remove("Potion de dégâts");
            player.ApplyDamageBoost();
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

        // Bonus de chance si la potion est active
        if (player.HasChancePotionActive)
        {
            lootChance += 50;
        }

        if (lootChance <= 20)
        {
            AddLoot("Potion de soin", player, enemy);
        }
        else if (lootChance <= 30)
        {
            AddLoot("Potion de chance", player, enemy);
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
    public string Name { get; set; }
    public int Health { get; set; }
    public int Level { get; set; }
    public int MinDamage { get; set; }
    public int MaxDamage { get; set; }
    public int Experience { get; set; } = 0;
    public int ExperienceReward { get; set; }
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
    public bool IsBoss { get; set; } = false;
    public Func<Character, bool> SpecialBehavior { get; set; } // Défini la capacité spéciale du boss
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
            int reducedDamage = amount / 5; // Réduction de dégâts avec bouclier
            Health -= reducedDamage;
            Console.WriteLine($"{Name} a bloqué une partie des dégâts avec son bouclier! Dégâts subis: {reducedDamage} | Santé restante: {Health}");
            IsShielding = false; // Le bouclier ne dure que pendant un tour
        }
        else
        {
            Health -= amount;
            Console.WriteLine();
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
        int ActualLevel = 0;
        while (Experience >= Level * 100) // Boucle pour gérer plusieurs montées de niveau en cas de grand gain
        {
            Experience -= Level * 100; // Soustraire le seuil
            Level++;
            Health += 50; // Regain de santé
            BaseAttackDamage += 1; // Augmenter l'attaque de base
            Console.WriteLine();
            if (Level != ActualLevel)
                Console.WriteLine($"{Name} est monté au niveau {Level} !");
            ActualLevel = Level;
            Console.WriteLine($"Vous gagnez 50 points de vie et votre attaque de base augmente de 1 !");
            Console.WriteLine($"Santé: {Health}, Attaque de base: {BaseAttackDamage}");
            Console.WriteLine();
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
                    //player.GainExperience(enemy.ExperienceReward);
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
                        //player.GainExperience(enemy.ExperienceReward);
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
        if (HasDamageBoostPotion == true)
        {
            DamageBoost = bonus;
            Console.WriteLine();
            Console.WriteLine($"Les dégâts sont augmentés de {bonus} points pour {durationInTurns} tours.");
            DecreaseDamageBoost();
        }
    }

    public void DecreaseDamageBoost()
    {
        if (DamageBoost == 0)
            HasDamageBoostPotion = false;
    }

    public void HandleLuckPotionEffects(Character player)
    {
        if (player.HasChancePotionActive)
        {
            player.LuckPotionDuration--; // Réduire la durée de la potion
            if (player.LuckPotionDuration <= 0)
            {
                player.HasChancePotionActive = false; // Désactiver l'effet
                Console.WriteLine("L'effet de la Potion de chance s'est dissipé.");
            }
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
    public string Type { get; set; }

    public Weapon(string name, int minDamage, int maxDamage, string type)
    {
        Name = name;
        MinDamage = minDamage;
        MaxDamage = maxDamage;
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

    /// TODO Revoir toute la méthode StarBattle pour traiter les points suivants : 
    /// Méthode Attack et TakeDamage faisant sensiblement la même chose.
    /// Clarifier le code et recréer les méthodes de manières plus propres, plus lisibles et plus pratique à entretenir.
    /// Si possible, créer une méthode pour chaque action de sorte que la méthode StartBattle ne fasse qu'appeler chacune de ces méthodes.

    public void StartBattle(Character player, Inventory inventory)
    {
        Weapon sword = new Weapon("Épée", 5, 15, "mélée");
        Weapon greatSword = new Weapon("Espadon", 12, 25, "mélée");
        Weapon spear = new Weapon("Lance", 5, 10, "mélée");

        List<Weapon> availableWeapons = new List<Weapon> { sword, greatSword, spear };

        player.EquippedWeapon = sword;

        int wave = 1;
        while (player.Health > 0)  // Continue tant que le joueur est en vie
        {
            Console.WriteLine($"\n--- Vague {wave} ---");
            enemies = GenerateEnemies(wave);

            while (player.Health > 0 && enemies.Count > 0)
            {
                //Print the action menu
                DisplayMenu(player);

                var choice = Console.ReadKey(true).KeyChar;
                Console.WriteLine();

                bool actionValide = true;

                // Gère l'action choisie par le joueur
                ChooseAction(player, availableWeapons, choice);

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
                        Console.WriteLine();
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
                    Console.WriteLine($"{player.Name} a été vaincu, l'aventure s'arrête ici !");
                    Console.WriteLine();
                    Console.WriteLine($"Félicitations ! Vous êtes allé jusqu'à la vague {wave}.");
                    Console.WriteLine();
                    break;
                }

                // Gestion de la mort des ennemis et appel du loot
                var deadEnemies = enemies.Where(e => e.Health <= 0).ToList();
                foreach (var enemy in deadEnemies)
                {
                    inventory.LootPotion(enemy, player); // Appel à LootPotion si l'ennemi est mort
                    player.GainExperience(enemy.ExperienceReward);
                    enemies.Remove(enemy); // Supprime l'ennemi mort de la liste principale
                }

                if (enemies.All(e => e.Health <= 0))
                {
                    player.HandleLuckPotionEffects(player);
                    Console.WriteLine("Tous les ennemis de la vague ont été vaincus!");
                    Console.WriteLine();
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

    /// TODO Revoir complètement la génération de vague pour les traiter une à une plutôt qu'utiliser une boucle qui génère des ennemis selon un pattern.
    private Character GenerateBoss(int wave)
    {
        List<Character> bosses = new List<Character>
    {
        new Character
        {
            Name = "Dragon",
            Health = 300,
            Level = wave,
            MinDamage = 100,
            MaxDamage = 200,
            ExperienceReward = 500,
            IsBoss = true,
            SpecialBehavior = (player) =>
            {
                Console.WriteLine("Le Dragon utilise son souffle de feu, ignorant les défenses !");
                return true; // Ignore les défenses du joueur
            }
        },
        new Character
        {
            Name = "Serpent Géant",
            Health = 300,
            Level = wave,
            MinDamage = 50,
            MaxDamage = 100,
            ExperienceReward = 500,
            IsBoss = true,
            SpecialBehavior = (player) =>
            {
                Console.WriteLine("Le Serpent se cache dans le sol pour esquiver une attaque !");
                return false; // Évite une attaque du joueur
            }
        }
    };

        Random rand = new Random();
        return bosses[rand.Next(bosses.Count)];
    }

    private List<Character> GenerateEnemies(int wave)
    {
        List<Character> enemies = new List<Character>();

        // Boss à chaque 10e vague
        if (wave % 10 == 0)
        {
            var boss = GenerateBoss(wave);  // Générer un boss spécifique
            enemies.Add(boss);
        }
        else
        {
            int numGoblin = 0;
            int numTroll = 0;

            // Générer les Gobelins et les Trolls de manière alternée
            for (int i = 1; i <= wave; i++)
            {
                if (i % 2 != 0) // Vagues impaires = Gobelins
                {
                    numGoblin++;
                    if (wave == 2)
                        numGoblin--;
                }
                else // Vagues paires = Trolls
                {
                    numTroll++;
                    numGoblin--;
                }
            }

            // Ajouter les Gobelins
            for (int i = 0; i < numGoblin; i++)
            {
                var goblin = CreateGoblin(i + 1);
                enemies.Add(goblin);
            }

            // Ajouter les Trolls
            for (int i = 0; i < numTroll; i++)
            {
                var troll = CreateTroll(i + 1);
                enemies.Add(troll);
            }
        }

        return enemies;
    }


    private Character CreateGoblin(int count)
    {
        return new Character
        {
            Name = $"Gobelin {count}",
            Health = 50,
            MinDamage = 5,
            MaxDamage = 10,
            ExperienceReward = 50
        };
    }

    private Character CreateTroll(int count)
    {
        return new Character
        {
            Name = $"Troll {count}",
            Health = 100,
            MinDamage = 10,
            MaxDamage = 20,
            ExperienceReward = 100
        };
    }

    private void DisplayMenu(Character player)
    {
        Console.WriteLine();
        Console.WriteLine($"{player.Name} - Santé: {player.Health} | Niveau: {player.Level} | XP: {player.Experience}");

        Console.WriteLine("Choisissez une action :");
        Console.WriteLine("1. Attaquer");
        Console.WriteLine("2. Lever le bouclier");
        Console.WriteLine("3. Utiliser une potion");
        Console.WriteLine("4. Changer d'arme");
        Console.WriteLine("5. Afficher l'inventaire");
        Console.WriteLine("6. Utiliser la capacité spéciale");
        Console.WriteLine();
    }

    private void ChooseAction(Character player, List<Weapon> availableWeapons, char choice)
    {        
        Console.WriteLine();
        bool actionValide = true;

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
    }

    #region Méthode inutilisée
    //public void PerformAttack(Character player, List<Character> enemies)
    //{
    //    // Si un ennemi spécifique a été ciblé, on l'attaque
    //    if (player.EquippedWeapon.Name == "Lance")
    //    {
    //        // Demander à choisir un ennemi (cela a été fait dans StartBattle)
    //        // Nous avons déjà géré ce cas dans la partie StartBattle ci-dessus
    //    }
    //    else
    //    {
    //        // Attaque tous les ennemis si ce n'est pas la lance
    //        foreach (var enemy in enemies)
    //        {
    //            if (enemy.Health > 0)
    //            {
    //                int damageDealt = player.Attack(enemies);
    //                Console.WriteLine($"{enemy.Name} a reçu {damageDealt} points de dégâts.");
    //            }
    //        }
    //    }
    //} 
    #endregion


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