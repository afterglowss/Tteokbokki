using System.Collections.Generic;

public class IngredientMetaData
{
    public string Name;
    public int SalePricePerUse;  // 재료 하나당 받을 금액 (0원 가능)
    public int OrderAmountKg;
    public int PricePerKg;
    public int GramsPerServing;

    public int OrderCost => OrderAmountKg * PricePerKg;
    public int CostPerServing => PricePerKg * GramsPerServing / 1000;
    public int ProfitPerServing => SalePricePerUse - CostPerServing;
    public int ServingsPerOrder => (OrderAmountKg * 1000) / GramsPerServing;
}

public static class IngredientEconomyDatabase
{
    public static readonly Dictionary<string, IngredientMetaData> Data = new()
    {
        { "떡", new IngredientMetaData { Name = "떡", SalePricePerUse = 1000, OrderAmountKg = 20, PricePerKg = 2500, GramsPerServing = 200 } },
        { "오뎅", new IngredientMetaData { Name = "오뎅", SalePricePerUse = 1000, OrderAmountKg = 20, PricePerKg = 2500, GramsPerServing = 200 } },
        { "파", new IngredientMetaData { Name = "파", SalePricePerUse = 0, OrderAmountKg = 2, PricePerKg = 1200, GramsPerServing = 50 } },
        { "양배추", new IngredientMetaData { Name = "양배추", SalePricePerUse = 0, OrderAmountKg = 8, PricePerKg = 400, GramsPerServing = 200 } },
        { "체다치즈", new IngredientMetaData { Name = "체다치즈", SalePricePerUse = 1500, OrderAmountKg = 2, PricePerKg = 14000, GramsPerServing = 50 } },
        { "모짜렐라", new IngredientMetaData { Name = "모짜렐라", SalePricePerUse = 1500, OrderAmountKg = 2, PricePerKg = 14000, GramsPerServing = 50 } },
        { "중국당면", new IngredientMetaData { Name = "중국당면", SalePricePerUse = 2000, OrderAmountKg = 4, PricePerKg = 7000, GramsPerServing = 100 } },
        { "일반당면", new IngredientMetaData { Name = "일반당면", SalePricePerUse = 1000, OrderAmountKg = 2, PricePerKg = 10000, GramsPerServing = 50 } },
        { "라면사리", new IngredientMetaData { Name = "라면사리", SalePricePerUse = 1000, OrderAmountKg = 4, PricePerKg = 3000, GramsPerServing = 100 } },
        { "우삼겹", new IngredientMetaData { Name = "우삼겹", SalePricePerUse = 2500, OrderAmountKg = 2, PricePerKg = 17000, GramsPerServing = 50 } },
        { "계란", new IngredientMetaData { Name = "계란", SalePricePerUse = 1500, OrderAmountKg = 4, PricePerKg = 10000, GramsPerServing = 100 } },
        { "메추리알", new IngredientMetaData { Name = "메추리알", SalePricePerUse = 1500, OrderAmountKg = 2, PricePerKg = 8000, GramsPerServing = 50 } },
        { "분모자", new IngredientMetaData { Name = "분모자", SalePricePerUse = 3000, OrderAmountKg = 5, PricePerKg = 14000, GramsPerServing = 125 } },
        { "유부", new IngredientMetaData { Name = "유부", SalePricePerUse = 1500, OrderAmountKg = 4, PricePerKg = 12000, GramsPerServing = 100 } },
        { "곱창", new IngredientMetaData { Name = "곱창", SalePricePerUse = 4000, OrderAmountKg = 4, PricePerKg = 30000, GramsPerServing = 100 } },
        { "군자 소스", new IngredientMetaData { Name = "군자 소스", SalePricePerUse = 500, OrderAmountKg = 4, PricePerKg = 4000, GramsPerServing = 50 } },
        { "마라 소스", new IngredientMetaData { Name = "마라 소스", SalePricePerUse = 0, OrderAmountKg = 2, PricePerKg = 16000, GramsPerServing = 50 } },
        { "로제 크림", new IngredientMetaData { Name = "로제 크림", SalePricePerUse = 0, OrderAmountKg = 2, PricePerKg = 16000, GramsPerServing = 50 } },
    };
}
