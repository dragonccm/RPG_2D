using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class EnemyAutoSetupTool : MonoBehaviour
{
    [Header("Kéo prefab enemy vào đây")]
    public GameObject enemyPrefab;

    [Header("Tùy chọn")]
    public bool autoSetupOnStart = false;

    [ContextMenu("Setup Enemy In Scene")]
    public void SetupEnemyInScene()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("Chưa gắn prefab enemy!");
            return;
        }

        // Tạo instance trong scene
        GameObject enemyInstance = Instantiate(enemyPrefab, transform.position, Quaternion.identity, null);
        enemyInstance.name = enemyPrefab.name + "_AutoSetup";

        // Kiểm tra & thêm các component cần thiết
        AddIfMissing<UnityEngine.AI.NavMeshAgent>(enemyInstance);
        AddIfMissing<Character>(enemyInstance);
        AddIfMissing<EnemyAIController>(enemyInstance);
        AddIfMissing<EnemyAnimationEvents>(enemyInstance);
        AddIfMissing<EnemyDeathHandler>(enemyInstance);
        AddIfMissing<AttackableCharacter>(enemyInstance);

        // Nếu là enemy tầm xa, thêm EnemyRangedAttack nếu chưa có
        if (enemyInstance.GetComponent<EnemyRanged>() != null)
        {
            AddIfMissing<EnemyRangedAttack>(enemyInstance);
        }

        // Thêm Collider nếu chưa có (ví dụ BoxCollider2D cho 2D)
        if (enemyInstance.GetComponent<Collider>() == null && enemyInstance.GetComponent<Collider2D>() == null)
        {
            if (enemyInstance.GetComponent<SpriteRenderer>() != null)
                enemyInstance.AddComponent<BoxCollider2D>();
            else
                enemyInstance.AddComponent<BoxCollider>();
        }

        // Thêm Animator nếu chưa có
        AddIfMissing<Animator>(enemyInstance);

        // Thêm Rigidbody nếu chưa có (ưu tiên 2D nếu có SpriteRenderer)
        if (enemyInstance.GetComponent<Rigidbody>() == null && enemyInstance.GetComponent<Rigidbody2D>() == null)
        {
            if (enemyInstance.GetComponent<SpriteRenderer>() != null)
                enemyInstance.AddComponent<Rigidbody2D>();
            else
                enemyInstance.AddComponent<Rigidbody>();
        }

        Debug.Log("Đã setup xong enemy instance: " + enemyInstance.name, enemyInstance);
    }

    private void AddIfMissing<T>(GameObject obj) where T : Component
    {
        if (obj.GetComponent<T>() == null)
        {
            obj.AddComponent<T>();
            Debug.Log("Tự động thêm component: " + typeof(T).Name, obj);
        }
    }

    // Tùy chọn: Tự động setup khi chạy scene
    private void Start()
    {
        if (autoSetupOnStart)
        {
            SetupEnemyInScene();
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(EnemyAutoSetupTool))]
public class EnemyAutoSetupToolEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EnemyAutoSetupTool tool = (EnemyAutoSetupTool)target;
        if (GUILayout.Button("Setup Enemy In Scene"))
        {
            tool.SetupEnemyInScene();
        }
    }
}
#endif 