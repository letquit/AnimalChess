using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 代表棋盘上的一个方格，包含位置、类型、阵营属性及交互逻辑。
/// 管理棋子放置、点击事件处理和移动交互逻辑，是棋盘系统的核心数据载体。
/// </summary>
public class Square : MonoBehaviour
{
    /// <summary>
    /// 方格的位置坐标（棋盘二维坐标系）。
    /// 通过Inspector或运行时初始化，用于标识棋子在棋盘上的唯一位置
    /// </summary>
    public Location location;

    /// <summary>
    /// 方格的地形类型（如普通、障碍等）。
    /// 影响棋子移动路径计算和可行走性判断
    /// </summary>
    public SquareType type;

    /// <summary>
    /// 方格所属的阵营（用于区分势力范围）。
    /// 决定棋子归属和敌我识别逻辑
    /// </summary>
    public Camp camp;

    /// <summary>
    /// 获取位于此方格上的棋子对象。
    /// 通过Location实时查询棋子管理器获取当前棋子
    /// </summary>
    public Chessman Chessman => Chessman.GetChessman(location);

    private Button _button; // 缓存按钮组件

    private void Start()
    {
        _button = GetComponent<Button>();
        if (_button != null)
        {
            _button.onClick.AddListener(OnSquareClicked);
        }
    }

    /// <summary>
    /// 返回方格的字符串表示形式，用于调试和日志输出。
    /// </summary>
    /// <returns>
    /// 包含位置、类型和阵营信息的格式化字符串
    /// 格式示例："棋盘方格坐标:(2,3),地形类型:普通,阵营:红方"
    /// </returns>
    public override string ToString()
    {
        return $"棋盘方格坐标:{location},地形类型:{type},阵营:{camp}";
    }

    /// <summary>
    /// 处理方格点击事件的核心逻辑，包含：
    /// 1. 当前是否有选中棋子的情况处理
    /// 2. 空方格移动指令校验与执行
    /// 3. 敌我棋子交互逻辑判断
    /// 4. 移动提示系统状态更新
    /// </summary>
    public void OnSquareClicked()
    {
        var selection = SelectCore.Selection;
        var chessman = Chessman; // 缓存结果

        bool hasChessman = chessman != null;
        bool isOwnChessman = hasChessman && chessman.camp == PlayerManager.Instance.currentPlayer;

        if (selection != null)
        {
            HandleSelectedState(selection, chessman, hasChessman, isOwnChessman);
        }
        else
        {
            HandleNoSelection(chessman, isOwnChessman);
        }
    }

    /// <summary>
    /// 处理已有棋子选中状态下的方格点击逻辑
    /// </summary>
    /// <param name="selection">当前选中的棋子对象</param>
    /// <param name="chessman">当前点击方格上的棋子对象</param>
    /// <param name="hasChessman">指示方格上是否存在棋子</param>
    /// <param name="isOwnChessman">指示是否为当前玩家阵营的棋子</param>
    private void HandleSelectedState(Chessman selection, Chessman chessman, bool hasChessman, bool isOwnChessman)
    {
        if (!hasChessman)
        {
            // 目标位置有效性校验与移动指令执行
            if (MoveHintEffect.Get.CanMoveTo(selection, location))
            {
                CommandCenter.GameOrder(selection, location);
            }
        }
        else if (isOwnChessman)
        {
            // 同阵营棋子重新选中逻辑
            SelectCore.TrySelect(chessman);
            MoveHintEffect.Get.ShowMoveHints(chessman);
        }
        else
        {
            // 敌方棋子攻击逻辑处理
            if (MoveHintEffect.Get.CanMoveTo(selection, location))
            {
                CommandCenter.GameOrder(selection, location);
            }
        }

        // 统一处理无效点击或移动失败的情况
        if (!MoveHintEffect.Get.CanMoveTo(selection, location))
        {
            SelectCore.DropSelect();
        }

        MoveHintEffect.Get.ClearMoveHints(); // 统一清除提示
    }

    /// <summary>
    /// 处理未选中任何棋子状态下的点击逻辑
    /// </summary>
    /// <param name="chessman">当前点击方格上的棋子对象</param>
    /// <param name="isOwnChessman">指示是否为当前玩家阵营的棋子</param>
    private void HandleNoSelection(Chessman chessman, bool isOwnChessman)
    {
        // 无选中状态下的棋子选中逻辑
        if (chessman != null && isOwnChessman)
        {
            SelectCore.TrySelect(chessman);
            MoveHintEffect.Get.ShowMoveHints(chessman);
        }
    }

    /// <summary>
    /// 清除当前方格上的棋子（如果存在），用于棋子移动或战斗后的状态更新。
    /// 调用棋子对象的退出棋盘逻辑，解除方格与棋子的关联
    /// </summary>
    public void RemoveChessman()
    {
        var chessman = Chessman;
        if (chessman != null)
        {
            chessman.ExitFromBoard();
        }
    }
    
    
    // [MenuItem("棋盘/初始化棋盘方格")]
    // public static void InitSquares()
    // {
    //     var squares = FindObjectsOfType<Square>();
    //     foreach (var square in squares)
    //     {
    //         string name = square.gameObject.name;
    //         string[] locationValues = name.Split(',');
    //         if (locationValues.Length >= 2 &&
    //             int.TryParse(locationValues[0], out int x) &&
    //             int.TryParse(locationValues[1], out int y))
    //         {
    //             square.location = new Location(x, y);
    //         }
    //         else
    //         {
    //             Debug.LogWarning($"无法解析方格坐标: {name}，请确保名称格式为 x,y");
    //         }
    //     }
    // }
}
