using System.Collections.Generic;

public class IngredientMetaData
{
    public string Name;
    public int SalePricePerUse;  // ��� �ϳ��� ���� �ݾ� (0�� ����)
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
        { "��", new IngredientMetaData { Name = "��", SalePricePerUse = 1000, OrderAmountKg = 20, PricePerKg = 2500, GramsPerServing = 200 } },
        { "����", new IngredientMetaData { Name = "����", SalePricePerUse = 1000, OrderAmountKg = 20, PricePerKg = 2500, GramsPerServing = 200 } },
        { "��", new IngredientMetaData { Name = "��", SalePricePerUse = 0, OrderAmountKg = 2, PricePerKg = 1200, GramsPerServing = 50 } },
        { "�����", new IngredientMetaData { Name = "�����", SalePricePerUse = 0, OrderAmountKg = 8, PricePerKg = 400, GramsPerServing = 200 } },
        { "ü��ġ��", new IngredientMetaData { Name = "ü��ġ��", SalePricePerUse = 1500, OrderAmountKg = 2, PricePerKg = 14000, GramsPerServing = 50 } },
        { "��¥����", new IngredientMetaData { Name = "��¥����", SalePricePerUse = 1500, OrderAmountKg = 2, PricePerKg = 14000, GramsPerServing = 50 } },
        { "�߱����", new IngredientMetaData { Name = "�߱����", SalePricePerUse = 2000, OrderAmountKg = 4, PricePerKg = 7000, GramsPerServing = 100 } },
        { "�Ϲݴ��", new IngredientMetaData { Name = "�Ϲݴ��", SalePricePerUse = 1000, OrderAmountKg = 2, PricePerKg = 10000, GramsPerServing = 50 } },
        { "���縮", new IngredientMetaData { Name = "���縮", SalePricePerUse = 1000, OrderAmountKg = 4, PricePerKg = 3000, GramsPerServing = 100 } },
        { "����", new IngredientMetaData { Name = "����", SalePricePerUse = 2500, OrderAmountKg = 2, PricePerKg = 17000, GramsPerServing = 50 } },
        { "���", new IngredientMetaData { Name = "���", SalePricePerUse = 1500, OrderAmountKg = 4, PricePerKg = 10000, GramsPerServing = 100 } },
        { "���߸���", new IngredientMetaData { Name = "���߸���", SalePricePerUse = 1500, OrderAmountKg = 2, PricePerKg = 8000, GramsPerServing = 50 } },
        { "�и���", new IngredientMetaData { Name = "�и���", SalePricePerUse = 3000, OrderAmountKg = 5, PricePerKg = 14000, GramsPerServing = 125 } },
        { "����", new IngredientMetaData { Name = "����", SalePricePerUse = 1500, OrderAmountKg = 4, PricePerKg = 12000, GramsPerServing = 100 } },
        { "��â", new IngredientMetaData { Name = "��â", SalePricePerUse = 4000, OrderAmountKg = 4, PricePerKg = 30000, GramsPerServing = 100 } },
        { "���� �ҽ�", new IngredientMetaData { Name = "���� �ҽ�", SalePricePerUse = 500, OrderAmountKg = 4, PricePerKg = 4000, GramsPerServing = 50 } },
        { "���� �ҽ�", new IngredientMetaData { Name = "���� �ҽ�", SalePricePerUse = 0, OrderAmountKg = 2, PricePerKg = 16000, GramsPerServing = 50 } },
        { "���� ũ��", new IngredientMetaData { Name = "���� ũ��", SalePricePerUse = 0, OrderAmountKg = 2, PricePerKg = 16000, GramsPerServing = 50 } },
    };
}
