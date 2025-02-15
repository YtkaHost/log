using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;

    public class Program
    {
        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File("operations.log")
                .CreateLogger();

            try
            {
                var shop = new Shop();
                shop.Run();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred");
            }
            finally
            {
                Log.Information("Program work end");
            }
        }
    }

    public class Shop
    {
        private readonly ProductCatalog productCatalog;
        private readonly SudokuCaptcha captchaService;

        public Shop()
        {
            productCatalog = new ProductCatalog();
            captchaService = new SudokuCaptcha();
        }

        public void Run()
        {
            var product = SelectProduct();
            if (product == null) return;

            if (VerifyCaptcha())
            {
                CompletePurchase(product);
            }
            else
            {
                Log.Information("Purchase canceled for {ProductName}", product.Name);
                Console.WriteLine("Покупку скасовано!");
            }
        }

        private Product SelectProduct()
        {
            Console.WriteLine("Доступнi товари:");
            productCatalog.ListProducts();

            Console.Write("Введiть ID товару: ");
            string input = Console.ReadLine();
            int productId;

            try
            {
                productId = Convert.ToInt32(input);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка: {ex}");
                return null;
            }

            var product = productCatalog.GetProduct(productId);
            if (product != null)
            {
                Log.Information("Selected product {ProductName} (ID: {ProductId})", product.Name, product.Id);
            }
            else
            {
                Console.WriteLine("Товар не знайдено!");
            }
            return product;
        }

        private bool VerifyCaptcha()
        {
            var captcha = captchaService.GenerateNewCaptcha();
            captcha.PrintBoard();

            Console.WriteLine("Розв’яжiть судоку для пiдтвердження покупки!");
            Console.WriteLine("Вводьте координати та числа через пробiл (рядок стовпець число)");
            Console.WriteLine("Введiть 'ready' для завершення");

            while (true)
            {
                Console.Write("Введення: ");
                var input = Console.ReadLine()?.Trim().ToLower();

                if (input == "ready")
                {
                    if (captcha.IsSolvedCorrectly())
                    {
                        Log.Information("Captcha solved correctly");
                        return true;
                    }

                    Log.Warning("Incorrect captcha solution");
                    Console.WriteLine("Ви робот!");
                    return false;
                }

                if (!captcha.ProcessInput(input))
                {
                    Console.WriteLine("Неправильний ввiд!");
                }

                captcha.PrintBoard();
            }
        }

        private void CompletePurchase(Product product)
        {
            Log.Information("Purchase confirmed for {ProductName}", product.Name);
            Console.WriteLine($"Дякуємо за покупку {product.Name}!");
        }
    }

    public class ProductCatalog
    {
        private readonly List<Product> products = new()
        {
            new(1, "Смартфон", 52000),
            new(2, "Ноутбук", 100500),
            new(3, "Навушники", 5000)
        };

        public void ListProducts()
        {
            foreach (var product in products)
            {
                Console.WriteLine($"{product.Id}. {product.Name} - {product.Price} грн");
            }
        }

        public Product GetProduct(int id)
        {
            return products.FirstOrDefault(p => p.Id == id);
        }
    }

    public class Product
    {
        public int Id { get; }
        public string Name { get; }
        public double Price { get; }

        public Product(int Id, string Name, double Price)
        {
            this.Id = Id;
            this.Name = Name;
            this.Price = Price;
        }
    }

    public class SudokuCaptcha
    {
        private const int Holes = 1;
        private readonly Random random = new();

        public SudokuPuzzle GenerateNewCaptcha()
        {
            Log.Debug("Generating new captcha");
            var generator = new SudokuGenerator();
            var puzzle = generator.Generate(Holes);
            Log.Information("New captcha generated");
            return puzzle;
        }
    }

    public class SudokuPuzzle
    {
        private readonly int[,] solution;
        private readonly int[,] current;
        private readonly bool[,] editable;

        public SudokuPuzzle(int[,] solution, int[,] current, bool[,] editable)
        {
            this.solution = solution;
            this.current = current;
            this.editable = editable;
        }

        public void PrintBoard()
        {
            Console.WriteLine("\nПоточний стан судоку:");
            Console.WriteLine("+-------+-------+-------+");

            for (int i = 0; i < 9; i++)
            {
                if (i % 3 == 0 && i != 0)
                    Console.WriteLine("+-------+-------+-------+");

                for (int j = 0; j < 9; j++)
                {
                    if (j % 3 == 0)
                    {
                        Console.Write("| ");
                    }

                    if (current[i, j] == 0)
                    {
                        Console.Write(". ");
                    }
                    else
                    {
                        Console.Write(current[i, j].ToString() + " ");
                    }
                }
                Console.WriteLine("|");
            }
            Console.WriteLine("+-------+-------+-------+");
        }

        public bool ProcessInput(string input)
        {
            int row, col, num;

            var parts = input.Split();
            if (parts.Length != 3) return false;

            try
            {
                row = Convert.ToInt32(parts[0]);
                col = Convert.ToInt32(parts[1]);
                num = Convert.ToInt32(parts[2]);
            }
            catch (Exception ex)
            {
                Log.Error("Error: {ex}", ex);
                return false;
            }

            row--;
            col--;

            if (row < 0 || row >= 9 || col < 0 || col >= 9 || num < 1 || num > 9)
                return false;

            if (!editable[row, col])
            {
                Log.Warning("Attempt to modify protected cell {Row}:{Col}", ++row, ++col);
                return false;
            }

            current[row, col] = num;
            Log.Debug("Entered value {Num} at {Row}:{Col}", num, ++row, ++col);
            return true;
        }

        public bool IsSolvedCorrectly()
        {
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    if (current[i, j] != solution[i, j])
                        return false;
                }
            }
            return true;
        }
    }

    public class SudokuGenerator
    {
        private readonly Random random = new();

        public SudokuPuzzle Generate(int holes)
        {
            var board = new int[9, 9];
            FillDiagonal(board);
            Solve(board);

            int[,] solution = new int[9, 9];
            Array.Copy(board, solution, board.Length);

            var editable = new bool[9, 9];

            RemoveNumbers(board, editable, holes);
            return new SudokuPuzzle(solution, board, editable);
        }

        private void FillDiagonal(int[,] board)
        {
            for (int i = 0; i < 9; i += 3)
            {
                FillBox(board, i, i);
            }
        }

        private void FillBox(int[,] board, int row, int col)
        {
            var used = new bool[10];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    int num;
                    do
                    {
                        num = random.Next(1, 10);
                    } while (used[num]);

                    used[num] = true;
                    board[row + i, col + j] = num;
                }
            }
        }

        private bool Solve(int[,] board, int row = 0, int col = 0)
        {
            if (row > 8) return true;
            if (col > 8) return Solve(board, row + 1, 0);
            if (board[row, col] != 0) return Solve(board, row, col + 1);

            List<int> nums = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            for (int i = nums.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                int temp = nums[j];
                nums[j] = nums[i];
                nums[i] = temp;
            }

            foreach (var num in nums)
            {
                if (IsSafe(board, row, col, num))
                {
                    board[row, col] = num;
                    if (Solve(board, row, col + 1)) return true;
                    board[row, col] = 0;
                }
            }
            return false;
        }


        private bool IsSafe(int[,] board, int row, int col, int num)
        {
            for (int i = 0; i < 9; i++)
            {
                if (board[row, i] == num) return false;
                if (board[i, col] == num) return false;
            }

            int startRow = row / 3 * 3;
            int startCol = col / 3 * 3;

            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    if (board[startRow + i, startCol + j] == num)
                        return false;

            return true;
        }

        private void RemoveNumbers(int[,] board, bool[,] editable, int holes)
        {
            for (int i = 0; i < holes; i++)
            {
                int row, col;
                do
                {
                    row = random.Next(9);
                    col = random.Next(9);
                } while (board[row, col] == 0);

                editable[row, col] = true;
                board[row, col] = 0;
            }
        }
    }