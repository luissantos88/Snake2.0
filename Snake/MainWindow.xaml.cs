using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;


namespace Snake
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        private readonly Dictionary<GridValue, ImageSource> gridValToImage = new()
        {
            { GridValue.Empty, Images.Empty },
            { GridValue.Snake, Images.Body},
            { GridValue.Food, Images.Food},
            { GridValue.BadFood, Images.BadFood},
            { GridValue.DrunkFood, Images.DrunkFood},
        };
        private readonly Dictionary<Direction, int> dirToRotation = new()
        {
            { Direction.Up, 0 },
            { Direction.Right, 90 },
            { Direction.Down, 180 },
            { Direction.Left, 270 }
        };
        private readonly int rows = 20, cols = 20;
        private readonly Image[,] gridImages;
        private GameState gameState;
        private bool gameRunning;
        int score;
        int highScore;


        public MainWindow()
        {

            InitializeComponent();
            ReadHighScore();
            gridImages = SetupGrid();
            gameState = new GameState(rows, cols);
        }

        private async Task RunGame()
        {
            GameState.oppositeDirection = false;
            Draw();
            await ShowCountDown();
            Overlay.Visibility = Visibility.Hidden;
            PlayMusic();
            await GameLoop();
            await ShowGameOver();
            gameState = new GameState(rows, cols);

        }

        private async void Window_PreviewKeyDown(object sender, KeyEventArgs e) // and user press any key this method is called
        {
            if (Overlay.Visibility == Visibility.Visible)
            {
                e.Handled = true; // prevents Window_KeyDown to be called
            }

            if (!gameRunning)
            {
                gameRunning = true;
                await RunGame();
                gameRunning = false;
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e) // give input to keyboard keys
        {
            if (gameState.GameOver)
            {
                return;
            }

            if (GameState.oppositeDirection)
            {
                switch (e.Key)
                {
                    case Key.Left:
                        gameState.ChangeDirection(Direction.Right);
                        break;
                    case Key.Right:
                        gameState.ChangeDirection(Direction.Left);
                        break;
                    case Key.Up:
                        gameState.ChangeDirection(Direction.Down);
                        break;
                    case Key.Down:
                        gameState.ChangeDirection(Direction.Up);
                        break;
                }
            }

            if (!GameState.oppositeDirection)
            {
                switch (e.Key)
                {
                    case Key.Left:
                        gameState.ChangeDirection(Direction.Left);
                        break;
                    case Key.Right:
                        gameState.ChangeDirection(Direction.Right);
                        break;
                    case Key.Up:
                        gameState.ChangeDirection(Direction.Up);
                        break;
                    case Key.Down:
                        gameState.ChangeDirection(Direction.Down);
                        break;
                }
            }
        }

        private async Task GameLoop()
        {
            while (!gameState.GameOver)
            {
                await Task.Delay(100);
                gameState.MoveAsync();
                Draw();
            }
        }

        private Image[,] SetupGrid()
        {
            Image[,] images = new Image[rows, cols];
            GameGrid.Rows = rows;
            GameGrid.Columns = cols;
            GameGrid.Width = GameGrid.Height * (cols / (double)rows);

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    Image image = new Image
                    {
                        Source = Images.Empty,
                        RenderTransformOrigin = new Point(0.5, 0.5)
                    };

                    images[r, c] = image;
                    GameGrid.Children.Add(image);
                }
            }
            return images;
        }
        private void Draw()
        {
            DrawGrid();
            DrawSnakeHead();
            ScoreAndHighScore();
        }

        private void ReadHighScore()
        {
            highScore = 0; // Initialize highScore to a default value in case the file doesn't exist or doesn't contain a valid integer.

            if (!File.Exists("highScores.txt"))
            {
                TextWriter textWriter = new StreamWriter("highScores.txt");
                textWriter.Write(highScore);
                textWriter.Close();
            }

            string line;
            TextReader textReader = new StreamReader("highScores.txt");
            line = textReader.ReadLine();
            highScore = int.Parse(line);
            textReader.Close();
            HighScoreText.Text = "HighScore " + highScore;
        }
        private void WriteHighScore()
        {

            if (score > highScore)
            {
                highScore = score;

                HighScoreText.Text = "HighScore " + highScore;
                HighScoreText.Foreground = new SolidColorBrush(Colors.Gold);
                TextWriter textWriter = new StreamWriter("highScores.txt");
                textWriter.WriteLine(highScore);
                textWriter.Close();
            }
        }
        private void ScoreAndHighScore()
        {
            score = gameState.Score;
            ScoreText.Text = $"Score " + score;
        }
        private void DrawGrid() // draws the images to every positon of the grid
        {
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    GridValue gridVal = gameState.Grid[r, c];
                    gridImages[r, c].Source = gridValToImage[gridVal];
                    gridImages[r, c].RenderTransform = Transform.Identity; // image rotate is only the head
                }
            }
        }
        private void DrawSnakeHead()
        {
            Position headPos = gameState.HeadPosition(gameState.GetSnakePositions());
            Image image = gridImages[headPos.Row, headPos.Col];
            image.Source = Images.Head;

            int rotation = dirToRotation[gameState.Dir];
            image.RenderTransform = new RotateTransform(rotation);
        }
        private async Task DrawDeadSnake()
        {
            List<Position> positions = new List<Position>(gameState.SnakePositons());

            for (int i = 0; i < positions.Count; i++)
            {
                Position pos = positions[i];
                ImageSource source = (i == 0) ? Images.DeadHead : Images.DeadBody;
                gridImages[pos.Row, pos.Col].Source = source;
                await Task.Delay(50);
            }
        }
        private async Task ShowCountDown()
        {
            for (int i = 3; i >= 1; i--)
            {
                OverlayText.Text = i.ToString();
                await Task.Delay(500);
            }
        }

        private async Task ShowGameOver()
        {
            WriteHighScore();
            await DrawDeadSnake();
            await Task.Delay(1000);
            Overlay.Visibility = Visibility.Visible;
            OverlayText.Text = "PRESS ANY KEY TO START";
            HighScoreText.Foreground = new SolidColorBrush(Colors.White);
        }
        public static void PlayMusic() // play snake theme music
        {
            SoundPlayer musicPlayer = new SoundPlayer(Properties.Resource1.snakeMusic);
            musicPlayer.PlayLooping();
        }
        public static void StopPlayMusic() // stops snake theme music
        {
            SoundPlayer musicPlayer = new SoundPlayer(Properties.Resource1.snakeMusic);
            musicPlayer.Stop();
        }
        public static void PlaySnakeDead() // play snake dead sound
        {
            SoundPlayer musicPlayer = new SoundPlayer(Properties.Resource1.snakeDead);
            musicPlayer.Play();
        }
    }
}