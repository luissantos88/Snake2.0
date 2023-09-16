using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Snake
{
    public class GameState
    {
        public int Rows { get; }
        public int Cols { get; }
        public GridValue[,] Grid { get; }
        public Direction Dir { get; private set; }
        public int Score { get; private set; }
        public bool GameOver { get; private set; }

        private readonly LinkedList<Direction> dirChanges = new LinkedList<Direction>();
        private readonly LinkedList<Position> snakePositions = new LinkedList<Position>();
        private readonly Random random = new Random();
        volatile bool createFood = false;
        volatile bool goodFood = true;
        volatile bool badFoodBool = true;
        volatile bool badFoodEaten = false;
        volatile bool drunkFoodBool = true;
        volatile bool drunkFoodEaten = false;
        volatile public static bool oppositeDirection = false;

        public GameState(int rows, int cols)
        {
            Rows = rows;
            Cols = cols;
            Grid = new GridValue[rows, cols];
            Dir = Direction.Right;
            AddSnake();
            AddFood();
        }

        private void AddSnake()
        {
            int r = Rows / 2; // create snake middle of the grid

            for (int c = 1; c <= 3; c++)
            {
                Grid[r, c] = GridValue.Snake;
                snakePositions.AddFirst(new Position(r, c));
            }
        }

        private IEnumerable<Position> EmptyPositions() // returns all empty positions on the grid
        {
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    if (Grid[r, c] == GridValue.Empty)
                    {
                        yield return new Position(r, c);
                    }
                }
            }
        }

        private void AddFood() // creates random food
        {
            List<Position> empty = new List<Position>(EmptyPositions());

            if (empty.Count == 0) // if someone wins the game (no more empty postions)
            {
                return;
            }

            Position pos = empty[random.Next(empty.Count)];

            Random randomNumber = new Random();
            int num = randomNumber.Next(1, 9);

            while (!createFood && goodFood && badFoodBool && drunkFoodBool)
            {
                oppositeDirection = false;
                drunkFoodEaten = true;
                createFood = true;
                badFoodBool = false;
                drunkFoodBool = false;

                switch (num)
                {
                    case <= 6:
                        Grid[pos.Row, pos.Col] = GridValue.Food;
                        break;
                    case 7:
                        Grid[pos.Row, pos.Col] = GridValue.BadFood;
                        Task badFoodTask = badFoodtimerAsync(pos);
                        break;
                    case 8:
                        Grid[pos.Row, pos.Col] = GridValue.DrunkFood;
                        Task drunkFoodTask = oppositeMoveTimer(pos);
                        break;
                }
            }
        }

        public LinkedList<Position> GetSnakePositions() // returns snake positions
        {
            return snakePositions;
        }

        public Position HeadPosition(LinkedList<Position> snakePositions) // returns the snake head postion
        {
            return snakePositions.First.Value;
        }

        public Position TailPositon() // returns the tail of the snake
        {
            if (snakePositions == null)
            {
            }
            return snakePositions.Last.Value;
        }

        public IEnumerable<Position> SnakePositons()
        {
            return snakePositions;
        }

        private void AddHead(Position pos) // give new position to head of the snake
        {
            snakePositions.AddFirst(pos);
            Grid[pos.Row, pos.Col] = GridValue.Snake;
        }
        private void RemoveTail() // remove the tail from the last position
        {
            Position tail = snakePositions.Last.Value;
            Grid[tail.Row, tail.Col] = GridValue.Empty;
            snakePositions.RemoveLast();
        }

        private Direction GetLastDirection() // gets snake last direction
        {
            if (dirChanges.Count == 0)
            {
                return Dir;
            }

            return dirChanges.Last.Value;
        }
        private bool CanChangeDirection(Direction newDir) // checks if snake can change direction
        {
            if (dirChanges.Count == 2)
            {
                return false;
            }

            Direction lastDir = GetLastDirection();
            return newDir != lastDir && newDir != lastDir.Opposite();
        }

        public void ChangeDirection(Direction dir) // change snake direction
        {
            if (CanChangeDirection(dir))
            {
                dirChanges.AddLast(dir);
            }
        }
        private bool OutsideGrid(Position pos) // bool to check grid limits
        {
            return pos.Row < 0 || pos.Row >= Rows || pos.Col < 0 || pos.Col >= Cols;
        }

        private GridValue WillHit(Position newHeadPos) // check hit situations of snake
        {
            if (OutsideGrid(newHeadPos)) // if snake hit grid limits
            {
                return GridValue.Outside;
            }

            if (newHeadPos == TailPositon()) // if next position of head is tail then snake moves without hit
            {
                return GridValue.Empty;
            }

            return Grid[newHeadPos.Row, newHeadPos.Col];
        }

        public async Task MoveAsync() // move snake
        {
            if (dirChanges.Count > 0)
            {
                Dir = dirChanges.First.Value;
                dirChanges.RemoveFirst();
            }

            Position newHeadPos = HeadPosition(GetSnakePositions()).Translate(Dir);
            GridValue hit = WillHit(newHeadPos);

            if (hit == GridValue.Outside || hit == GridValue.Snake)  // if snake moves to grid limts or snake body GAME OVER           
            {
                MainWindow.StopPlayMusic();
                GameOver = true;
                MainWindow.PlaySnakeDead();
            }
            else if (hit == GridValue.Empty) // if snake moves to empty postision do nothing just moves
            {
                RemoveTail();
                AddHead(newHeadPos);
            }
            else if (hit == GridValue.Food) // if snake moves to food generates another random food
            {
                goodFood = true;
                badFoodBool = true;
                drunkFoodBool = true;
                Score++;
                AddHead(newHeadPos);
                createFood = false;
                AddFood();
            }
            else if (hit == GridValue.BadFood) // if snake moves to badFood score 1- and losses 1 bodypart
            {
                Score--;
                badFoodEaten = true;
                drunkFoodEaten = true;
                goodFood = true;
                drunkFoodBool = true;
                badFoodBool = true;
                RemoveTail();
                RemoveTail();
                AddHead(newHeadPos);
                createFood = false;
                AddFood();
            }
            else if (hit == GridValue.DrunkFood) // if snake moves to drunkfood score 2+ and direction is inverted for 5 seconds
            {
                Score += 2;
                drunkFoodEaten = true;
                oppositeDirection = true;
                createFood = true;
                badFoodBool = false;
                drunkFoodBool = false;
                goodFood = false;

                AddHead(newHeadPos);

                int timer = 0;

                while (timer < 5)
                {
                    timer++;
                    await Task.Delay(1000);
                }

                badFoodBool = true;
                badFoodEaten = false;
                drunkFoodBool = true;
                goodFood = true;
                drunkFoodEaten = false;
                oppositeDirection = false;
                createFood = false;
                AddFood();
            }
        }
        public async Task badFoodtimerAsync(Position pos) // creates a 5 seconds timer for the badfood 
        {
            badFoodEaten = false;
            createFood = true;
            goodFood = false;
            badFoodBool = false;
            drunkFoodBool = false;
            Position newHeadPos = HeadPosition(GetSnakePositions()).Translate(Dir);
            GridValue hit = WillHit(newHeadPos);
            int timer = 0;

            while (timer < 5)
            {
                timer++;
                await Task.Delay(1000);
            }

            if (!badFoodEaten)
            {
                Grid[pos.Row, pos.Col] = GridValue.Empty;
                createFood = false;
                goodFood = true;
                badFoodBool = true;
                drunkFoodBool = true;
                badFoodEaten = true;
                AddFood();
            }
        }
        public async Task oppositeMoveTimer(Position pos) // creates a 5 seconds timer for the drunkFood
        {
            drunkFoodEaten = false;
            goodFood = false;
            drunkFoodBool = false;
            badFoodBool = false;
            Position newHeadPos = HeadPosition(GetSnakePositions()).Translate(Dir);
            GridValue hit = WillHit(newHeadPos);
            createFood = true;
            int timer = 0;

            while (timer < 5)
            {
                timer++;
                await Task.Delay(1000);
            }

            if (!drunkFoodEaten)
            {
                Grid[pos.Row, pos.Col] = GridValue.Empty;
                createFood = false;
                goodFood = true;
                drunkFoodBool = true;
                badFoodBool = true;
                drunkFoodEaten = true;
                AddFood();
            }
        }
    }
}