using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.ComponentModel;
using System.Windows.Controls.Primitives;

namespace MainMenu
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    
    public partial class MainWindow : Window

    {
        private List<Enemy> availableMonsters;
        private Random random = new Random();
        private Hero hero;
        private Enemy enemy;
        private bool isAutoBattleRunning = false;
        private DispatcherTimer playerAttackTimer;
        private DispatcherTimer enemyAttackTimer;
        private PlayerState playerState;
        private Dictionary<string, int> animationFrames = new Dictionary<string, int>();
        private Dictionary<string, int> enemyanimationFrames = new Dictionary<string, int>();
        private string currentAnimation = ""; // Текущая анимация
        private int currentFrame = 0;
        private DispatcherTimer animationTimer;

        public MainWindow()
        {
            
            InitializeComponent();
            
            hero = new Hero { Health = 100, Attack = 13, Defense = 1, MaxHealth = 100 };

            playerAttackTimer = new DispatcherTimer();
            playerAttackTimer.Interval = TimeSpan.FromMilliseconds(10);
            playerAttackTimer.Tick += PlayerAttackTimer_Tick;

            enemyAttackTimer = new DispatcherTimer();
            enemyAttackTimer.Interval = TimeSpan.FromMilliseconds(10);
            enemyAttackTimer.Tick += EnemyAttackTimer_Tick;

            // Настройка таймера анимации
            animationTimer = new DispatcherTimer();
            animationTimer.Interval = TimeSpan.FromMilliseconds(200); // Интервал в 200мс между кадрами
            animationTimer.Tick += AnimationTimer_Tick;
            animationTimer.Start();

            // Инициализация числа кадров для каждой анимации
            animationFrames["idle"] = 11;
            animationFrames["walk"] = 8;
            animationFrames["attack"] = 6;
            animationFrames["death"] = 9; // Допустим, что у нас 10 кадров для анимации смерти героя.

            InitSpritesCache(); // Инициализировать кеш спрайтов

            // Установка начальной анимации
            SetAnimation("idle");

            playerHealthProgressBar.DataContext = hero;

        }
        private void UpdateMonsterCountDisplay()
        {
            monsterCountTextBlock1.Text = $"Очки исследования: {defeatedMonsterCount}";
        }
        private Dictionary<string, List<BitmapImage>> spritesCache = new Dictionary<string, List<BitmapImage>>();
        private Dictionary<string, List<BitmapImage>> monsterSpritesCache = new Dictionary<string, List<BitmapImage>>();

        private void InitMonsterSpritesCache(Enemy enemy)
        {
            string[] monsterAnimations = { "idle", "attack" };
            string basePath = $"pack://application:,,,/images/monsters/{enemy.AnimationPath}";

            foreach (var animFramesPair in enemy.AnimationFrames)
            {
                string anim = animFramesPair.Key;
                int frameCount = animFramesPair.Value;
                monsterSpritesCache[anim] = new List<BitmapImage>(frameCount);

                for (int frame = 0; frame < frameCount; frame++)
                {
                    string imageName = $"{anim}_{frame + 1}.gif";
                    Uri imageUrl = new Uri($"{basePath}{imageName}", UriKind.Absolute);
                    monsterSpritesCache[anim].Add(new BitmapImage(imageUrl));
                }
            }
        }

        private void RunMonsterSprite(string animationName, int frameIndex)
        {
            if (monsterSpritesCache.TryGetValue(animationName, out var framesList))
            {
                if (frameIndex >= 0 && frameIndex < framesList.Count)
                {
                    BitmapImage bitmapImage = framesList[frameIndex];
                    enemyRect.Source = bitmapImage; // "enemyRect" это ваш элемент Image для монстра
                }
            }

        }

        private void InitSpritesCache()
        {
            string[] animations = { "idle", "walk", "attack", "death" };
            string basePath = "pack://application:,,,/images/";

            foreach (var anim in animations)
            {
                int frameCount = animationFrames[anim];
                spritesCache[anim] = new List<BitmapImage>(frameCount);

                for (int frame = 0; frame < frameCount; frame++)
                {
                    string imageName = $"{anim}_{frame + 1}.gif";
                    Uri imageUrl = new Uri($"{basePath}{imageName}", UriKind.Absolute);
                    spritesCache[anim].Add(new BitmapImage(imageUrl));
                }
            }
        }
        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            // Условие свитча должно быть одно, не дублировать для разных случаев
            switch (currentAnimation)
            {
                case "idle":
                case "walk": // Ходьба и покой используют один и тот же код
                case "attack":
                    runSprite(currentAnimation, currentFrame);
                    RunMonsterSprite(currentAnimation, currentFrame);
                    currentFrame++;
                    if (currentFrame >= spritesCache[currentAnimation].Count)
                    {
                        currentFrame = 0;
                        if (currentAnimation == "attack")
                        {
                            currentAnimation = "idle";
                        }
                    }
                    break;
                case "death":
                    runSprite(currentAnimation, currentFrame);
                    currentFrame++;
                    if (currentFrame >= spritesCache[currentAnimation].Count)
                    {
                        animationTimer.Stop(); // Закончить анимацию смерти и остановить таймер.
                    }
                    break;
            }

        }
        private void SetAnimation(string animationName)
        {
            if (currentAnimation != animationName)
            {
                currentFrame = 0; // Обеспечиваем начало анимации с первого кадра
                currentAnimation = animationName;
                // Можно добавить здесь специфические настройки для каждой анимации
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            MainMenu.Visibility = Visibility.Collapsed;
            LevelSelectPanel.Visibility = Visibility.Visible;
            UpdateMonsterCountDisplay();
            // Запуск автобоя запрятан внутри LoadLevel
        }
        private void LevelButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;


            if (button.Content.ToString() == "Уровень 2" && defeatedMonsterCount < 0)
            {
                // Если это второй уровень и герой не победил 3-х монстров, покажем сообщение
                MessageBox.Show("Вы не прошли 1 уровень!");
                return; // Возвращаем управление и не позволяем начать уровень
            }

            // Скрываем выбор уровня
            LevelSelectPanel.Visibility = Visibility.Collapsed;

            // Загружаем уровень и показываем интерфейс
            LoadLevel(button.Content.ToString());
            myCanvas.Visibility = Visibility.Visible;
            StartAutoBattle();
        }

        private void LoadLevel(string levelName)
        {
            
            LevelData levelData = GetLevelData(levelName); // ЭТО ДОЛЖНО БЫТЬ ПЕРВЫМ!!!!!!!!!!!!!!!!!
            if (levelData.LevelName == "Уровень 2" && levelData.Monsters.Count >= 4)
            {
                // Выбрать случайные 4 монстра из списка
                availableMonsters = levelData.Monsters.OrderBy(x => random.Next()).Take(4).ToList();
                if (levelData.LevelName == "Уровень 2" && levelData.Monsters.Count >= 4)
                {
                    // Выбрать случайные 4 монстра из списка
                    availableMonsters = levelData.Monsters.OrderBy(x => random.Next()).Take(4).ToList();
                }
            }
            else
            {
                // Во всех остальных случаях просто используйте все монстры из списка
                availableMonsters = levelData.Monsters.ToList();
            }
            if (availableMonsters.Any())
            {
                LoadNextMonster(); // Загружаем нового монстра
                enemyTextBlock.Text = enemy.Name;
                UpdateUI(); // Обновление интерфейса
            }
            else
            {
                // Обработка случая, когда монстры недоступны
                MessageBox.Show("Уровень пройден."); // Или другая логика обработки
            }
            Enemy monsterToFight = levelData.Monsters.FirstOrDefault(); // Получаем первого монстра из списка
            availableMonsters = levelData.Monsters.ToList(); // Создаем копию списка монстров
            if (monsterToFight != null)
            {
                enemy = new Enemy
                {
                    Health = monsterToFight.Health,
                    Attack = monsterToFight.Attack,
                    Defense = monsterToFight.Defense,
                    MaxHealth = monsterToFight.MaxHealth
                };
                LoadNextMonster(); // Загружаем первого монстра
                enemyTextBlock.Text = monsterToFight.Name; // Предполагается, что у вас есть TextBlock с именем enemyNameTextBlock в XAML
                UpdateUI(); // Обновить интерфейс сразу после создания врага
            }

            // Получение информации о выбранном уровне, например, из файла или структуры данных;

            string relativePath = $"images/{levelData.BackgroundImage}";
            BitmapImage backgroundImage = new BitmapImage(new Uri(relativePath, UriKind.Relative));

            // Создать кисть с изображением
            ImageBrush backgroundBrush = new ImageBrush
            {
                ImageSource = backgroundImage,
                Stretch = Stretch.Fill // или используйте другой Stretch, который соответствует вашему дизайну
            };

            // Применить кисть к фону myCanvas или другому элементу, который должен отображать фон
            myCanvas.Background = backgroundBrush;

            // Произведите дальнейшую обработку информации о уровне, чтобы настроить его элементы и параметры

            // ...

            // Произведите дальнейшую обработку информации о уровне, чтобы настроить его элементы и параметры
            // Ниже приведен простой пример, показывающий, как вы можете использовать данные уровня
            Console.WriteLine($"Загружен уровень: {levelData.LevelName}");
            if (levelData.LevelName == "Уровень 2")
            {
                // Выбрать первые 4 монстра из списка
                availableMonsters = levelData.Monsters.Take(4).ToList();
            }

        }

        

        private LevelData GetLevelData(string levelName)
        {
            /// Ваш код для получения информации о выбранном уровне
            var levelDataList = new List<LevelData>
            
            {
    new LevelData
    {
        LevelName = "Уровень 1",
        Monsters = new List<Enemy>
        {

            new Enemy { Name = "Гоблин", Health = 100, Attack = 15, Defense = -14, MaxHealth = 100, AnimationPath = "goblin/", AnimationFrames = new Dictionary<string, int>
    {
        { "idle", 8 },
        { "attack", 8 }
    } },
            new Enemy { Name = "Тролль", Health = 100, Attack = 10, Defense = -9, MaxHealth = 100, AnimationPath = "troll/", AnimationFrames = new Dictionary<string, int>
    {
        { "idle", 6 },
        { "attack", 14 }
    } }
        },
        BackgroundImage = "уровень1.png" // Путь к фоновой картинке для уровня 1
    },
    new LevelData
    {
        LevelName = "Уровень 2",
        Monsters = new List<Enemy>
        {
            new Enemy { Name = "Страшилище", Health = 50, Attack = 5, Defense = 0, MaxHealth = 50, AnimationPath = "guard/", AnimationFrames = new Dictionary<string, int>
    {
        { "idle", 7 },
        { "attack", 11 }
    } },
            new Enemy { Name = "Сессия", Health = 100, Attack = 20, Defense = 10, MaxHealth = 100 },
            new Enemy { Name = "НГ", Health = 100, Attack = 11, Defense = 10, MaxHealth = 100 },
            new Enemy { Name = "123", Health = 100, Attack = 50, Defense = -10, MaxHealth = 100 }

        },
        BackgroundImage = "уровень2.jpg" // Путь к фоновой картинке для уровня 2
    },
    // Другие уровни и их монстры
};

            return levelDataList.FirstOrDefault(level => level.LevelName == levelName);
        }


        private void StartAutoBattle()
        {
            playerAttackTimer.Start();
            enemyAttackTimer.Start();
        }

        private void PlayerAttackTimer_Tick(object sender, EventArgs e)
        {
            playerAttackProgressBar.Value += 1; // Увеличение значения ProgressBar игрока

            if (playerAttackProgressBar.Value >= playerAttackProgressBar.Maximum)
            {
                // ProgressBar игрока полностью заполнен, выполнить атаку игрока
                PerformPlayerAttack();
            }
            else
            {
                // Обновить состояние игрока и проиграть анимацию ожидания атаки
                playerState = PlayerState.Idle;
            }
        }

        private void runSprite(string animationName, int frameIndex)
        {
            if (spritesCache.TryGetValue(animationName, out var framesList))
            {
                if (frameIndex >= 0 && frameIndex < framesList.Count)
                {
                    BitmapImage bitmapImage = framesList[frameIndex];
                    player.Source = bitmapImage; // "player" это ваш элемент Image на канвасе
                }
            }
        }


        private void PerformPlayerAttack()
        {
            // Выполнение атаки игрока
            SetAnimation("attack"); // это запустит анимацию атаки

            // Сброс значения ProgressBar игрока
            playerAttackProgressBar.Value = 0;

            // Атака героя по врагу
            hero.AttackEnemy(enemy);

            // Обновление интерфейса после атаки
            UpdateUI();

            // Проверка условия победы или поражения
            if (hero.Health <= 0)
            {
                // Герой проиграл defeatedMonsterCount++; // Увеличиваем счетчик побежденных монстров

                // MessageBox.Show("Вы проиграли!");
                enemyAttackTimer.Stop();
                playerAttackTimer.Stop();
                SetAnimation("death");

            }
            else if (enemy.Health <= 0)
            {
                defeatedMonsterCount++; // Увеличиваем счетчик побежденных монстров
                monsterCountTextBlock.Text = $"Очки исследования: {defeatedMonsterCount}"; // обновляем текст на интерфейсе пользователя
                                                                              // Герой победил
                ShowCenteredPopup("Монстр побежден!", 1.4);
                enemyAttackTimer.Stop();
                playerAttackTimer.Stop();


                availableMonsters.Remove(enemy);
                if (availableMonsters.Any())
                {
                    LoadNextMonster(); // Загрузить следующего монстра
                    StartAutoBattle(); // Начать новый раунд автобоя
                }
                else
                {
                    MessageBox.Show("Все монстры побеждены!");
                }
            }

        }
        private void ShowCenteredPopup(string message, double duration)
        {
            // Создание TextBlock для отображения сообщения
            TextBlock messageTextBlock = new TextBlock
            {
                Text = message,
                Background = Brushes.White,
                Foreground = Brushes.Black,
                Padding = new Thickness(10)
            };

            // Создание Popup
            Popup popup = new Popup
            {
                Child = messageTextBlock,
                PlacementTarget = this, // Окно, в котором должен отобразиться Popup
                Placement = PlacementMode.Center // Размещение Popup в центре PlacementTarget
            };

            // Отображение Popup
            popup.IsOpen = true;

            // Таймер для закрытия Popup через заданное время
            DispatcherTimer closeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(duration)
            };
            closeTimer.Tick += (sender, args) =>
            {
                closeTimer.Stop();
                popup.IsOpen = false; // Закрытие Popup
            };
            closeTimer.Start();
        }

        private void LoadNextMonster()
        {
            if (availableMonsters.Count > 0)
            {
                int index = random.Next(availableMonsters.Count); // Выбираем случайный индекс
                Enemy nextMonster = availableMonsters[index];

                enemy = new Enemy
                {
                    // Загрузка данных следующего монстра
                    Health = nextMonster.Health,
                    Attack = nextMonster.Attack,
                    Defense = nextMonster.Defense,
                    MaxHealth = nextMonster.MaxHealth,
                    Name = nextMonster.Name
                };
                InitMonsterSpritesCache(nextMonster); // инициализация кеша с анимациями для выбранного монстра
                enemyTextBlock.Text = nextMonster.Name;
                UpdateUI();
            }
        }

        private void EnemyAttackTimer_Tick(object sender, EventArgs e)
        {
            enemyAttackProgressBar.Value += 1; // Увеличение значения ProgressBar монстра

            if (enemyAttackProgressBar.Value >= enemyAttackProgressBar.Maximum)
            {
                // ProgressBar монстра полностью заполнен, выполнить атаку монстра
                PerformEnemyAttack();
            }
        }

        private void PerformEnemyAttack()
        {
            // Выполнение атаки монстра

            // Сброс значения ProgressBar монстра
            enemyAttackProgressBar.Value = 0;

            // Атака врага по герою
            enemy.AttackHero(hero);

            // Обновление интерфейса после атаки
            UpdateUI();

            // Проверка условия победы или поражения
            if (hero.Health <= 0)
            {
                //defeatedMonsterCount = 0; // Сбросить счётчик побеждённых монстров
                monsterCountTextBlock.Text = $"Очки исследования: {defeatedMonsterCount}";
                MessageBox.Show("Вы проиграли!");
                enemyAttackTimer.Stop();
                playerAttackTimer.Stop();
            }
            else if (enemy.Health <= 0)
            {
                // Герой победил
                enemyAttackTimer.Stop();
                playerAttackTimer.Stop();
            }

        }
        private int defeatedMonsterCount = 0; // переменная для счётчика побеждённых монстров

        private void OptionsButton_Click(object sender, RoutedEventArgs e)
        {
            // Скрыть основное меню
            MainMenu.Visibility = Visibility.Collapsed; // Скрыть весь контейнер меню

            // Показать элементы интерфейса настроек
            SettingsPanel.Visibility = Visibility.Visible;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Сохранить настройки здесь
            // Пример записи имени игрока и громкости:
            double soundVolume = SoundVolumeSlider.Value;

            // Здесь может быть код для сохранения этих значений, например, в файл или базу данных

            // После сохранения скрыть панель настроек и показать главное меню
            // Код для сохранения настроек...

            // После сохранения возвращаемся к главному меню
            SettingsPanel.Visibility = Visibility.Collapsed;
            MainMenu.Visibility = Visibility.Visible; // Показать весь контейнер меню
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Обработка отмены настроек:
            // Возвращаемся к главному меню без сохранения изменений
            SettingsPanel.Visibility = Visibility.Collapsed;
            MainMenu.Visibility = Visibility.Visible; // Показать весь контейнер меню

            // Можно опционально сбросить изменения элементов управления
            // к их первоначальным значениям, если это необходимо.
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsPanel.Visibility = Visibility.Collapsed;
            MainMenu.Visibility = Visibility.Visible; // Показать весь контейнер меню
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Close(); // Закрыть окно игры
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string url = "https://discord.gg/RAkkWdjc3n"; // Замените ссылку на необходимую
            var psi = new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };

            // Запускаем процесс
            using (Process process = new Process { StartInfo = psi })
            {
                process.Start();
            }
        }
        private void IncreaseAttackButton_Click(object sender, RoutedEventArgs e)
        {
            monsterCountTextBlock.Text = $"Очки исследования: {defeatedMonsterCount}";

            if (defeatedMonsterCount > 0)
            {
                hero.Attack = hero.Attack + 10;
                defeatedMonsterCount--;
                UpdateMonsterCountDisplay();

            }
            else
            {
                MessageBox.Show("Недостаточно очков");

            }
        }

        private void IncreaseDefenseButton_Click(object sender, RoutedEventArgs e)
        {
            monsterCountTextBlock.Text = $"Очки исследования: {defeatedMonsterCount}";
           
            if (defeatedMonsterCount > 0 && hero.Defense < 50)
            {

                hero.Defense = hero.Defense + 7;
                defeatedMonsterCount--;
                UpdateMonsterCountDisplay();
                

            }

            else
            {
                MessageBox.Show("Недостаточно очков");
               
            }





        }
        private void Button_Click2(object sender, RoutedEventArgs e)
        {
            string url = "https://youtu.be/dQw4w9WgXcQ?si=A-Elibb8UXGrRRxb"; // Замените ссылку на необходимую
            var psi = new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };

            // Запускаем процесс
            using (Process process = new Process { StartInfo = psi })
            {
                process.Start();
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            // Останавливаем и сбрасываем таймеры для автобоя
            ResetCombatTimers();

            // Сбрасываем анимацию игрока
            ResetPlayerAnimation();

            // Сброс прогресс-баров здоровья к максимальным значениям (предполагаем, что это начальные значения)
            playerHealthProgressBar.Value = playerHealthProgressBar.Maximum;
            enemyHealthProgressBar.Value = enemyHealthProgressBar.Maximum;

            // Сброс состояния игрока и врага к начальным значениям
            hero.Health = (int)playerHealthProgressBar.Maximum;
            enemy.Health = (int)enemyHealthProgressBar.Maximum;

            // Обновление UI
            UpdateUI();

            // Возвращение к основному интерфейсу
            myCanvas.Visibility = Visibility.Collapsed;
            MainMenu.Visibility = Visibility.Visible; // Показываем весь контейнер меню

            // Сбрасываем флаг автобоя
            isAutoBattleRunning = false;

        }
        private void ResetCombatTimers()
        {
            if (playerAttackTimer.IsEnabled)
                playerAttackTimer.Stop();
            if (enemyAttackTimer.IsEnabled)
                enemyAttackTimer.Stop();

            // Сброс прогресс-баров атаки к начальным значениям
            playerAttackProgressBar.Value = 0;
            enemyAttackProgressBar.Value = 0;
        }
        private void ResetPlayerAnimation()
        {
            // Сброс анимации игрока к начальной позиции
            SetAnimation("idle"); // Сброс анимации на начальную позицию
            currentFrame = 0; // Обнуляем текущий кадр
            if (animationTimer.IsEnabled)
                animationTimer.Stop(); // Если таймер анимации работает, останавливаем его
            animationTimer.Start(); // Перезапускаем таймер анимации
        }

        public class LevelData
        {
            public string LevelName { get; set; }
            public List<Enemy> Monsters { get; set; } // Теперь это список объектов Enemy
            public string BackgroundImage { get; set; } // Путь к фоновой картинке

            public LevelData()
            {
                Monsters = new List<Enemy>(); // Инициализируем список объектов Enemy
            }
        }

        public class Hero
        {
            public int Health { get; set; }
            public int Attack { get; set; }
            public int Defense { get; set; }
            public int MaxHealth { get; set; } // Максимальное здоровье врага

            public void AttackEnemy(Enemy enemy)
            {
                // Рассчитываем урон героя по врагу
                int damage = Attack - enemy.Defense;
                if (damage > 0)
                {
                    enemy.Health -= damage;
                }
            }
        }

        public class Enemy
        {
            public string Name { get; set; }
            public int Health { get; set; }
            public int Attack { get; set; }
            public int Defense { get; set; }
            public int MaxHealth { get; set; } // Максимальное здоровье врага
            public string AnimationPath { get; set; } // Допустим, это относительный путь к папке с анимациями
            public Dictionary<string, int> AnimationFrames { get; set; } // Количество кадров для анимации

            public Enemy()
            {
                AnimationFrames = new Dictionary<string, int>();
            }


            public void AttackHero(Hero hero)
            {
                // Рассчитываем урон врага по герою
                int damage = Attack - hero.Defense;
                if (damage > 0)
                {
                    hero.Health -= damage;
                }
            }
        }

        private void UpdateUI()
        {
            // Обновление ProgressBar и других элементов интерфейса
            playerHealthProgressBar.Value = hero.Health;
            enemyHealthProgressBar.Value = enemy.Health;
        }

        public enum PlayerState
        {
            Idle,
            Attack,
            TakeDamage
        }

        private void ReturnToMainMenu()
        {
            // Очистка данных и возврат в главное меню
            hero.Health = 100;
            enemy.Health = 80;
            MainMenu.Visibility = Visibility.Visible;
            myCanvas.Visibility = Visibility.Collapsed;
        }


        private void Back_Click2(object sender, RoutedEventArgs e)
        {
            LevelSelectPanel.Visibility = Visibility.Collapsed;
            MainMenu.Visibility = Visibility.Visible; // Показать весь контейнер меню
            enemyAttackTimer.Stop();
            playerAttackTimer.Stop();
            isAutoBattleRunning = false;
            enemyAttackProgressBar.Value = 0;
            playerAttackProgressBar.Value = 0;
        }
        public class Animation
        {
            public Dictionary<string, List<BitmapImage>> Frames { get; private set; }
            public string CurrentAnimation { get; private set; }
            public int CurrentFrameIndex { get; private set; }
            public Image AnimationTarget { get; private set; }

            public Animation(Image target)
            {
                Frames = new Dictionary<string, List<BitmapImage>>();
                AnimationTarget = target;
            }

            public void AddFrames(string animationName, List<BitmapImage> frames)
            {
                Frames[animationName] = frames;
            }

            public void SetAnimation(string animationName)
            {
                if (Frames.ContainsKey(animationName))
                {
                    CurrentAnimation = animationName;
                    CurrentFrameIndex = 0;
                    AnimationTarget.Source = Frames[CurrentAnimation][CurrentFrameIndex];
                }
                else
                {
                    throw new ArgumentException($"Animation {animationName} does not exist.");
                }
            }

            public void UpdateFrame()
            {
                if (String.IsNullOrEmpty(CurrentAnimation))
                    return;

                CurrentFrameIndex = (CurrentFrameIndex + 1) % Frames[CurrentAnimation].Count;
                AnimationTarget.Source = Frames[CurrentAnimation][CurrentFrameIndex];
            }
        }
    }
}