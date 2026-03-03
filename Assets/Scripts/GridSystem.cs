using UnityEngine;

/// <summary>
/// 网格系统管理器，负责创建和管理等距网格地图
/// </summary>
public class GridSystem : MonoBehaviour
{
    /// <summary>
    /// 瓷砖预制体对象
    /// </summary>
    public GameObject tilePrefab;

    /// <summary>
    /// 瓷砖宽度
    /// </summary>
    public float tileWidth = 1.58f;

    /// <summary>
    /// 瓷砖高度
    /// </summary>
    public float tileHeight = 1.78f;

    /// <summary>
    /// 网格地图二维数组
    /// </summary>
    private GameObject[,] gridMap;

    /// <summary>
    /// 地图宽度（格子数量）
    /// </summary>
    public int mapWidth = 13;

    /// <summary>
    /// 地图高度（格子数量）
    /// </summary>
    public int mapHeight = 19;

    /// <summary>
    /// 棋子管理器引用
    /// </summary>
    public ChessManager chessManager;

    /// <summary>
    /// 初始化方法，在游戏对象启动时调用
    /// </summary>
    private void Start()
    {
        GenerateGrid();

        if (chessManager == null)
            chessManager = FindObjectOfType<ChessManager>();
    }

    /// <summary>
    /// 生成网格地图，实例化所有瓷砖并设置其位置和属性
    /// </summary>
    private void GenerateGrid()
    {
        gridMap = new GameObject[mapWidth, mapHeight];

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                Vector2 worldPos = IsometricMath.GridToWorldWithOffset(new Vector2Int(x, y), tileWidth, tileHeight);

                GameObject tile = Instantiate(tilePrefab, new Vector3(worldPos.x, worldPos.y, 0), Quaternion.identity);
                tile.transform.SetParent(transform);
                tile.name = $"Tile_{x}_{y}";
                gridMap[x, y] = tile;

                SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sortingOrder = Mathf.RoundToInt(-worldPos.y);
                }
            }
        }
    }

    /// <summary>
    /// 检查给定的网格坐标是否在有效范围内
    /// </summary>
    /// <param name="gridPos">要检查的网格坐标</param>
    /// <returns>如果坐标在有效范围内则返回true，否则返回false</returns>
    public bool IsValidGrid(Vector2Int gridPos)
    {
        return gridPos.x >= 0 && gridPos.x < mapWidth &&
               gridPos.y >= 0 && gridPos.y < mapHeight;
    }

    /// <summary>
    /// 获取指定网格坐标处的瓷砖对象
    /// </summary>
    /// <param name="gridPos">网格坐标</param>
    /// <returns>如果坐标有效则返回对应的瓷砖对象，否则返回null</returns>
    public GameObject GetTileAt(Vector2Int gridPos)
    {
        if (IsValidGrid(gridPos))
        {
            return gridMap[gridPos.x, gridPos.y];
        }

        return null;
    }

    /// <summary>
    /// 更新方法，处理鼠标点击事件并检测点击的棋子或格子
    /// </summary>
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 adjustedMousePos = mouseWorldPos - (Vector2)transform.position;

            Vector2Int gridPos = IsometricMath.WorldToGridWithOffset(adjustedMousePos, tileWidth, tileHeight);

            // 检查是否点击了棋子
            if (chessManager != null)
            {
                ChessPiece clickedPiece = chessManager.GetChessAtGrid(gridPos);
                if (clickedPiece != null)
                {
                    clickedPiece.OnPieceClicked();
                    return; // 点击棋子后不处理格子逻辑
                }
            }

            // 点击格子
            if (IsValidGrid(gridPos))
            {
                Debug.Log($"Clicked Grid: {gridPos.x},{gridPos.y}");
            }
            else
            {
                Debug.Log($"Clicked outside grid: {gridPos}");
            }
        }
    }
}