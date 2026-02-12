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

    public int LowStockThreshold => ServingsPerOrder / 4;
}

public static class IngredientEconomyDatabase
{
    public static readonly Dictionary<string, IngredientMetaData> Data = new()
    {
        // SalePricePerUse: 1인분 판매가, OrderAmountKg: 1회 주문 시 주문하는 양(kg), PricePerKg: 1kg 당 원가, GramsPerServing: 1인분 조리시 들어가는 양(g)
        { "떡", new IngredientMetaData { Name = "떡", SalePricePerUse = 1500, OrderAmountKg = 20, PricePerKg = 2500, GramsPerServing = 200 } },
        { "오뎅", new IngredientMetaData { Name = "오뎅", SalePricePerUse = 1500, OrderAmountKg = 20, PricePerKg = 2500, GramsPerServing = 200 } },
        { "파", new IngredientMetaData { Name = "파", SalePricePerUse = 0, OrderAmountKg = 3, PricePerKg = 1200, GramsPerServing = 50 } },
        { "양배추", new IngredientMetaData { Name = "양배추", SalePricePerUse = 0, OrderAmountKg = 12, PricePerKg = 600, GramsPerServing = 200 } },
        { "모짜렐라", new IngredientMetaData { Name = "모짜렐라", SalePricePerUse = 1500, OrderAmountKg = 2, PricePerKg = 14000, GramsPerServing = 50 } },
        { "우삼겹", new IngredientMetaData { Name = "우삼겹", SalePricePerUse = 3000, OrderAmountKg = 2, PricePerKg = 17000, GramsPerServing = 50 } },
        { "계란", new IngredientMetaData { Name = "계란", SalePricePerUse = 1500, OrderAmountKg = 4, PricePerKg = 10000, GramsPerServing = 100 } },
        { "일반당면", new IngredientMetaData { Name = "일반당면", SalePricePerUse = 1000, OrderAmountKg = 2, PricePerKg = 10000, GramsPerServing = 50 } },
        { "라면사리", new IngredientMetaData { Name = "라면사리", SalePricePerUse = 1000, OrderAmountKg = 4, PricePerKg = 3000, GramsPerServing = 100 } },
        { "곱창", new IngredientMetaData { Name = "곱창", SalePricePerUse = 5000, OrderAmountKg = 4, PricePerKg = 30000, GramsPerServing = 100 } },
        { "조랭이떡", new IngredientMetaData { Name = "조랭이떡", SalePricePerUse = 1500, OrderAmountKg = 4, PricePerKg = 2500, GramsPerServing = 100 } },
        { "고구마", new IngredientMetaData { Name = "고구마", SalePricePerUse = 2000, OrderAmountKg = 8, PricePerKg = 6000, GramsPerServing = 200 } },
        { "옥수수", new IngredientMetaData { Name = "옥수수", SalePricePerUse = 2500, OrderAmountKg = 4, PricePerKg = 8000, GramsPerServing = 100 } },
        { "소세지", new IngredientMetaData { Name = "소세지", SalePricePerUse = 1500, OrderAmountKg = 4, PricePerKg = 8000, GramsPerServing = 100 } },
        { "군자 소스", new IngredientMetaData { Name = "군자 소스", SalePricePerUse = 500, OrderAmountKg = 6, PricePerKg = 4000, GramsPerServing = 50 } },
        { "마라 소스", new IngredientMetaData { Name = "마라 소스", SalePricePerUse = 1500, OrderAmountKg = 2, PricePerKg = 16000, GramsPerServing = 50 } },
        { "로제 소스", new IngredientMetaData { Name = "로제 소스", SalePricePerUse = 1500, OrderAmountKg = 2, PricePerKg = 16000, GramsPerServing = 50 } },
        { "크림 소스", new IngredientMetaData { Name = "크림 소스", SalePricePerUse = 1000, OrderAmountKg = 2, PricePerKg = 16000, GramsPerServing = 50 } },
        { "간장 소스", new IngredientMetaData { Name = "간장 소스", SalePricePerUse = 1000, OrderAmountKg = 2, PricePerKg = 20000, GramsPerServing = 50 } },
        { "카레 소스", new IngredientMetaData { Name = "카레 소스", SalePricePerUse = 1000, OrderAmountKg = 2, PricePerKg = 16000, GramsPerServing = 50 } },
        { "짜장 소스", new IngredientMetaData { Name = "짜장 소스", SalePricePerUse = 1000, OrderAmountKg = 2, PricePerKg = 16000, GramsPerServing = 50 } },
    };
}
