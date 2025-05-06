using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 玩家管理器，用于管理游戏中的玩家相关逻辑
/// 包含单例模式实现、回合流程控制、移动次数统计和UI状态更新功能
/// 管理双方阵营(Camp.Blue/Camp.Red)的回合切换与行动力限制
/// </summary>
public class PlayerManager : MonoBehaviour
{
    /// <summary>
    /// 单例模式全局访问实例（通过静态属性Instance获取）
    /// </summary>
    private static PlayerManager instance = null;
    public static PlayerManager Instance
    {
        get
        {
            if (instance == null)
            {
                Debug.LogError("PlayerManager实例未初始化");
            }
            return instance;
        }
    }

    /// <summary>
    /// 当前正在行动的玩家阵营
    /// 默认从蓝色阵营开始游戏
    /// </summary>
    public Camp currentPlayer = Camp.Blue;

    /// <summary>
    /// 轮次阶段枚举类型
    /// FirstTurn: 首轮特殊阶段（移动次数限制为2次）
    /// Normal: 正常轮次阶段（移动次数限制为3次）
    /// </summary>
    private enum TurnPhase
    {
        FirstTurn,  // 首轮
        Normal      // 正常轮次
    }

    /// <summary>
    /// 当前轮次阶段状态
    /// 控制不同阶段的移动次数限制规则
    /// </summary>
    private TurnPhase currentPhase = TurnPhase.FirstTurn;

    /// <summary>
    /// 当前玩家已执行的移动次数
    /// 用于判断是否达到阶段移动上限
    /// </summary>
    private int moveCount = 0;

    /// <summary>
    /// 记录当前回合已移动过的棋子集合
    /// 防止同一棋子重复移动
    /// </summary>
    private List<Chessman> movedChessmenThisTurn;

    /// <summary>
    /// UI显示组件：当前行动方文本
    /// 显示当前玩家阵营名称
    /// </summary>
    public Text CurrentPlayerText;

    /// <summary>
    /// UI显示组件：剩余行动力文本
    /// 显示当前阶段剩余可移动次数
    /// </summary>
    public Text RemainActionPointsText;

    /// <summary>
    /// 缓存当前剩余行动力文本，避免无效更新
    /// </summary>
    private string cachedRemainActionText = string.Empty;

    /// <summary>
    /// 缓存当前玩家文本，避免无效更新
    /// </summary>
    private string cachedPlayerText = string.Empty;

    /// <summary>
    /// 初始化玩家管理器单例并配置初始状态
    /// 包含单例唯一性检查、对象持久化设置和UI初始化
    /// </summary>
    private void Awake()
    {
        // 单例唯一性检查
        if (instance == null)
        {
            instance = this;
            // 确保对象未被标记为 DontSaveInEditor
            if ((gameObject.hideFlags & HideFlags.DontSaveInEditor) != 0)
            {
                Debug.LogWarning("PlayerManager 不应被标记为 DontSaveInEditor");
                gameObject.hideFlags &= ~HideFlags.DontSaveInEditor;
            }
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    
        // 初始化集合
        movedChessmenThisTurn = new List<Chessman>();
    
        UpdateUI();
    }

    /// <summary>
    /// 回合结束处理方法
    /// 取消棋子选中状态并切换玩家回合
    /// 处理首轮到正常阶段的过渡逻辑
    /// </summary>
    public static void Tick()
    {
        if (Instance == null) return;

        SelectCore.DropSelect();

        // 获取当前阶段的移动次数上限
        int moveLimit = Instance.currentPhase == TurnPhase.FirstTurn ? 2 : 3;
        Instance.moveCount++;

        if (Instance.moveCount == moveLimit)
        {
            // 切换玩家并重置状态
            Instance.currentPlayer = (Instance.currentPlayer == Camp.Blue) 
                ? Camp.Red 
                : Camp.Blue;
            
            Instance.moveCount = 0;
            
            // 首轮结束后进入正常阶段
            if (Instance.currentPhase == TurnPhase.FirstTurn)
            {
                Instance.currentPhase = TurnPhase.Normal;
            }
            
            Instance.movedChessmenThisTurn.Clear();
        }

        Instance.UpdateUI();
    }

    /// <summary>
    /// 更新用户界面显示
    /// 同步刷新当前玩家标识和剩余行动力数值（仅当内容变化时更新）
    /// </summary>
    private void UpdateUI()
    {
        // 更新当前玩家文本
        string newPlayerText = $"当前行动方：{currentPlayer}";
        if (CurrentPlayerText != null && cachedPlayerText != newPlayerText)
        {
            CurrentPlayerText.text = newPlayerText;
            cachedPlayerText = newPlayerText;
        }

        // 计算剩余行动力
        int remainActions = currentPhase == TurnPhase.FirstTurn 
            ? 2 - moveCount 
            : 3 - moveCount;
        
        // 更新剩余行动力文本
        string newRemainText = $"剩余行动力：{remainActions}";
        if (RemainActionPointsText != null && cachedRemainActionText != newRemainText)
        {
            RemainActionPointsText.text = newRemainText;
            cachedRemainActionText = newRemainText;
        }
    }

    /// <summary>
    /// 检查指定棋子是否已在本回合移动过
    /// 用于防止同一棋子重复移动
    /// </summary>
    /// <param name="chessman">需要检查的棋子对象</param>
    /// <returns>若已移动返回true，否则返回false</returns>
    public static bool HasChessmanMovedThisTurn(Chessman chessman)
    {
        if (Instance == null || Instance.movedChessmenThisTurn == null || chessman == null)
        {
            return false;
        }
        return Instance.movedChessmenThisTurn.Contains(chessman);
    }

    /// <summary>
    /// 标记指定棋子为已移动状态
    /// 将棋子添加到本回合移动记录集合中
    /// </summary>
    /// <param name="chessman">需要标记的棋子对象</param>
    public static void MarkChessmanAsMoved(Chessman chessman)
    {
        if (Instance == null || Instance.movedChessmenThisTurn == null || chessman == null)
        {
            return;
        }
        Instance.movedChessmenThisTurn.Add(chessman);
    }
}
