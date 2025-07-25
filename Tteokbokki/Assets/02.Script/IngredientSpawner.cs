using UnityEngine;

public class IngredientSpawner : MonoBehaviour
{
    public static IngredientSpawner Instance { get; private set; }

    [Header("재료 프리팹")]
    public GameObject tteokPrefab;
    public GameObject paFragmentPrefab;
    public GameObject ramenPrefab;

    [Header("파 관련 설정")]
    public int paSpawnCount = 6;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SpawnIngredient(string ingredientName)
    {
        switch (ingredientName)
        {
            case "떡":
                SpawnTteok();
                break;

            case "파":
                SpawnPa();
                break;

            case "라면사리":
                SpawnRamen();
                break;


            default:
                Debug.LogWarning($"'{ingredientName}'에 대한 스폰 로직이 없습니다.");
                break;
        }
    }

    private void SpawnRamen()
    {
        Vector3 spawnPos = new Vector3(Random.Range(0f, 3f), 2f, 0);
        Instantiate(ramenPrefab, spawnPos, Quaternion.identity);
    }

    private void SpawnTteok()
    {
        Vector3 spawnPos = new Vector3(Random.Range(0.6f, 3f), 2f, 0);
        Instantiate(tteokPrefab, spawnPos, Quaternion.identity);
    }

    private void SpawnPa()
    {
        Vector3 center = new Vector3(Random.Range(0.6f, 2f), 2f, 0);

        for (int i = 0; i < paSpawnCount; i++)
        {
            Vector3 offset = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(0f, 0.3f), 0f);
            Vector3 spawnPos = center + offset;
            Instantiate(paFragmentPrefab, spawnPos, Quaternion.identity);
        }
    }
}
