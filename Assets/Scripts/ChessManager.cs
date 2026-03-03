using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 棋子管理器，负责棋子的生成、移除、移动和查询操作
/// </summary>
public class ChessManager : MonoBehaviour
{
    [Header("Chess")] public GameObject chessPrefab;
    public List<ChessPiece> allChessPieces = new List<ChessPiece>();

    [Header("Trap Settings")]
    public GameObject trapPrefab;
    public List<TrapConfig> trapConfigs = new List<TrapConfig>();
    public Sprite trapSpriteRed;
    public Sprite trapSpriteBlue;
    
    private GridSystem gridSystem;
    private Dictionary<Vector2Int, ChessPiece> gridChessMap = new Dictionary<Vector2Int, ChessPiece>();
    
    private List<ChessPiece> spawnedPieces = new List<ChessPiece>();

    private void Start()
    {
        gridSystem = FindAnyObjectByType<GridSystem>();
        
        SpawnInitialPieces();
        
        
        SpawnTraps();
    }
    
    private void SpawnTraps()
    {
        if (trapPrefab == null)
        {
            Debug.LogWarning("Trap Prefab not assigned, skipping trap generation");
            return;
        }
        
        if (gridSystem == null)
        {
            Debug.LogError("GridSystem not found!");
            return;
        }
        
        foreach (TrapConfig config in trapConfigs)
        {
            GameObject trapObj = Instantiate(trapPrefab, transform);
            trapObj.name = $"Trap_{config.position.x}_{config.position.y}_Camp{config.camp}";
            
            ChessPiece trapPiece = trapObj.GetComponent<ChessPiece>();
            if (trapPiece == null)
            {
                trapPiece = trapObj.AddComponent<ChessPiece>();
            }
            
            // 设置属性
            trapPiece.chessType = ChessType.Fixed;
            trapPiece.camp = config.camp;
            trapPiece.gridPosition = config.position;
            trapPiece.yOffset = 0.3f;
            trapPiece.gridSystem = gridSystem;
            
            // 根据阵营切换Sprite
            SpriteRenderer sr = trapObj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                if (config.camp == 1 && trapSpriteRed != null)
                {
                    sr.sprite = trapSpriteRed;
                }
                else if (config.camp == 2 && trapSpriteBlue != null)
                {
                    sr.sprite = trapSpriteBlue;
                }
                
                // 设置排序层
                Vector2 worldPos = IsometricMath.GridToWorldWithOffset(config.position, 
                    gridSystem.tileWidth, gridSystem.tileHeight);
                sr.sortingOrder = Mathf.RoundToInt(-worldPos.y) + 10;
            }
            
            trapPiece.UpdateWorldPosition();
            
            spawnedPieces.Add(trapPiece);
            gridChessMap[config.position] = trapPiece;
            
            string campName = config.camp == 1 ? "Red" : config.camp == 2 ? "Blue" : "None";
            Debug.Log($"Spawned Trap at Grid {config.position}, Camp: {campName}");
        }
    }
    
    /// <summary>
    /// 实例化Inspector中配置的初始棋子
    /// </summary>
    private void SpawnInitialPieces()
    {
        if (gridSystem == null)
        {
            Debug.LogError("GridSystem not found!");
            return;
        }
        
        foreach (ChessPiece pieceTemplate in allChessPieces)
        {
            if (pieceTemplate == null) continue;
            
            // 实例化棋子
            GameObject chessObj = Instantiate(pieceTemplate.gameObject, transform);
            chessObj.name = $"Chess_{pieceTemplate.pieceName}";
            
            // 获取新实例的ChessPiece组件
            ChessPiece spawnedPiece = chessObj.GetComponent<ChessPiece>();
            
            // 设置GridSystem引用
            spawnedPiece.gridSystem = gridSystem;
            
            // 初始化位置
            spawnedPiece.UpdateWorldPosition();
            
            // 添加到实例化后的棋子列表
            spawnedPieces.Add(spawnedPiece);
            gridChessMap[spawnedPiece.gridPosition] = spawnedPiece;
            
            Debug.Log($"Spawned Chess: {spawnedPiece.pieceName} at Grid {spawnedPiece.gridPosition}, Type: {spawnedPiece.chessType}");
        }
    }

    /// <summary>
    /// 生成棋子
    /// </summary>
    /// <param name="gridPos">网格位置</param>
    /// <param name="name">棋子名称</param>
    /// <param name="type">棋子类型，默认为可移动类型</param>
    /// <param name="camp">阵营，默认为0</param>
    /// <returns>生成的棋子组件</returns>
    public ChessPiece SpawnChessPiece(Vector2Int gridPos, string name,
        ChessType type = ChessType.Movable, int camp = 0)
    {
        if (chessPrefab == null)
        {
            Debug.LogError("Chess Prefab is not assigned!");
            return null;
        }

        if (gridSystem == null)
        {
            gridSystem = FindAnyObjectByType<GridSystem>();
        }

        // 实例化棋子
        GameObject chessObj = Instantiate(chessPrefab, transform);
        chessObj.name = $"Chess_{name}";

        // 获取组件
        ChessPiece piece = chessObj.GetComponent<ChessPiece>();
        if (piece == null)
        {
            piece = chessObj.AddComponent<ChessPiece>();
        }

        // 设置属性
        piece.pieceName = name;
        piece.chessType = type;
        piece.camp = camp;
        piece.gridPosition = gridPos;
        piece.gridSystem = gridSystem;

        // 初始化位置
        piece.UpdateWorldPosition();

        // 添加到列表
        allChessPieces.Add(piece);
        gridChessMap[gridPos] = piece;

        Debug.Log($"Spawned Chess: {name} at Grid {gridPos}, Type: {type}");

        return piece;
    }

    /// <summary>
    /// 移除棋子
    /// </summary>
    /// <param name="piece">要移除的棋子</param>
    public void RemoveChessPiece(ChessPiece piece)
    {
        if (piece != null)
        {
            gridChessMap.Remove(piece.gridPosition);
            allChessPieces.Remove(piece);
            Destroy(piece.gameObject);
        }
    }

    /// <summary>
    /// 获取指定位置的棋子
    /// </summary>
    /// <param name="gridPos">网格位置</param>
    /// <returns>位于指定位置的棋子，如果不存在则返回null</returns>
    public ChessPiece GetChessAtGrid(Vector2Int gridPos)
    {
        if (gridChessMap.ContainsKey(gridPos))
        {
            return gridChessMap[gridPos];
        }

        return null;
    }

    /// <summary>
    /// 检查位置是否有棋子
    /// </summary>
    /// <param name="gridPos">网格位置</param>
    /// <returns>如果位置有棋子返回true，否则返回false</returns>
    public bool HasChessAtGrid(Vector2Int gridPos)
    {
        return gridChessMap.ContainsKey(gridPos);
    }

    /// <summary>
    /// 移动棋子
    /// </summary>
    /// <param name="piece">要移动的棋子</param>
    /// <param name="newGridPos">新的网格位置</param>
    /// <returns>移动成功返回true，失败返回false</returns>
    public bool MoveChessPiece(ChessPiece piece, Vector2Int newGridPos)
    {
        if (piece == null || !piece.CanMove())
        {
            return false;
        }

        if (HasChessAtGrid(newGridPos))
        {
            Debug.Log("Target grid already has a chess piece!");
            return false;
        }

        // 移除旧位置映射
        gridChessMap.Remove(piece.gridPosition);

        // 更新位置
        piece.SetGridPosition(newGridPos);

        // 添加新位置映射
        gridChessMap[newGridPos] = piece;

        return true;
    }
}