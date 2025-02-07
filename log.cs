using System;
using System.Diagnostics;
using Serilog;


class Program
{
    static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.Console()
                    .WriteTo.File("operation.log")
                    .CreateLogger();
        Log.Information("On");

        Magazin userOne = new Magazin("1", 4, 1500, 2500, "Bomb");
        Magazin userTwo = new Magazin("2", 5, 500, 1000, "USB hub");

        Info(userOne.ID, userOne.how, userOne.price, userOne.money, userOne.item);
        Check(userOne.ID, userOne.how, userOne.price, userOne.money, userOne.item);

        Info(userTwo.ID, userTwo.how, userTwo.price, userTwo.money, userTwo.item);
        Check(userTwo.ID, userTwo.how, userTwo.price, userTwo.money, userTwo.item);

        Log.Information("Off");
    }

    static void Info(string ID, double how, double price, double money, string item)
    {
        Log.Information("-----------------------------------------------------------------------------------------------------");
        Log.Information($"Order: ID - {ID}\tWhat - {item}\tHow - {how}\tPrice - {price}\tMoney - {money}");
    }
    static void Check(string ID, double how, double price, double money, string item)
    {
        Log.Information(" ");
        Log.Information("Order: {ID}", ID);
        if (how > 0)
        {
            Log.Information("Order bigger than zero: {how}", how);
        }
        else
        {
            Log.Error("Error less than zero");
        }
        if(money > price * how)
        {
            Log.Information("Order paid");
        }
        else
        {
            Log.Error("No money user");
        }
        Log.Information("-----------------------------------------------------------------------------------------------------");
    }
}

class Magazin


{
    public string ID { get; set; }
    public double how { get; set; }
    public double price { get; set; }
    public double money { get; set; }
    public string item { get; set; }

    public Magazin (string ID, double how, double price, double money, string item)
    {
        this.ID = ID;
        this.how = how;
        this.price = price;
        this.money = money;
        this.item = item;
    }

}
