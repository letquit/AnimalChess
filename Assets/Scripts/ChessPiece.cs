using UnityEngine;

/// <summary>
/// 棋子组件类，负责管理棋子的基本属性、位置和行为
/// </summary>
public class ChessPiece : MonoBehaviour
{
    [Header("Chess")] public ChessType chessType = ChessType.Movable;
    public string pieceName = "Chess";
    public int camp = 0; // 0=无, 1=红方, 2=蓝方

    [Header("GridPosition")] public Vector2Int gridPosition = Vector2Int.zero;


    public SpriteRenderer spriteRenderer;
    public GridSystem gridSystem;


    public float yOffset = 0.5f;

    private Vector3 targetWorldPos;

    /// <summary>
    /// 初始化方法，在对象创建时调用
    /// </summary>
    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// 启动方法，在对象开始运行时调用
    /// </summary>
    private void Start()
    {
        // 根据网格位置更新世界坐标
        UpdateWorldPosition();
    }

    /// <summary>
    /// 更新方法，每帧调用以同步棋子位置
    /// </summary>
    private void Update()
    {
        UpdateWorldPosition();
    }

    /// <summary>
    /// 根据当前网格位置更新棋子的世界坐标位置
    /// </summary>
    public void UpdateWorldPosition()
    {
        if (gridSystem == null)
        {
            gridSystem = FindObjectOfType<GridSystem>();
            if (gridSystem == null) return;
        }

        // 获取网格的世界坐标
        Vector2 worldPos = IsometricMath.GridToWorldWithOffset(gridPosition,
            gridSystem.tileWidth, gridSystem.tileHeight);

        // 应用Y轴偏移
        targetWorldPos = new Vector3(worldPos.x, worldPos.y + yOffset, 0);

        transform.position = targetWorldPos;

        // 设置排序层（确保棋子在棋盘上方）
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = Mathf.RoundToInt(-worldPos.y) + 10;
        }
    }

    /// <summary>
    /// 设置棋子的新网格位置
    /// </summary>
    /// <param name="newPos">新的网格位置</param>
    public void SetGridPosition(Vector2Int newPos)
    {
        gridPosition = newPos;
        UpdateWorldPosition();
    }

    /// <summary>
    /// 设置棋子类型
    /// </summary>
    /// <param name="type">要设置的棋子类型</param>
    public void SetChessType(ChessType type)
    {
        chessType = type;
    }

    /// <summary>
    /// 判断棋子是否可以移动
    /// </summary>
    /// <returns>如果棋子可移动则返回true，否则返回false</returns>
    public bool CanMove()
    {
        return chessType == ChessType.Movable || chessType == ChessType.Interactive;
    }

    /// <summary>
    /// 当棋子被点击时触发的方法
    /// </summary>
    public void OnPieceClicked()
    {
        Debug.Log($"[{pieceName}] Clicked! Type: {chessType}, Grid: {gridPosition}");

        if (!CanMove())
        {
            Debug.Log($"[{pieceName}] is Fixed and cannot move!");
        }
    }
}