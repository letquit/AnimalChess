using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 管理棋盘上棋子移动提示效果的单例类，负责显示和清除移动提示，
/// 并处理不同动物棋子的移动规则（如水域、草地穿越）。
/// </summary>
public class MoveHintEffect : MonoBehaviour
{
    /// <summary>
    /// 单例实例的访问属性，确保全局唯一实例。
    /// </summary>
    public static MoveHintEffect Get { get; private set; }

    /// <summary>
    /// 可移动位置提示框的预制体模板，用于动态创建提示效果。
    /// </summary>
    public GameObject moveHintPrefab;

    /// <summary>
    /// 当前场景中已创建的移动提示实例列表，用于后续清除操作。
    /// </summary>
    private List<GameObject> currentHints = new List<GameObject>();

    /// <summary>
    /// 定义允许通过水域的动物集合（牛、虎、蛇、狗）
    /// </summary>
    private static readonly HashSet<Animal> WaterAccessibleAnimals = new HashSet<Animal>
    {
        Animal.Ox, Animal.Tiger, Animal.Snake, Animal.Dog
    };

    /// <summary>
    /// 定义允许通过草地的所有动物集合（除象以外的所有动物）
    /// </summary>
    private static readonly HashSet<Animal> GrassAccessibleAnimals = new HashSet<Animal>
    {
        Animal.Rat, Animal.Ox, Animal.Tiger, Animal.Rabbit, Animal.Snake,
        Animal.Horse, Animal.Goat, Animal.Monkey, Animal.Chicken,
        Animal.Dog, Animal.Cat, Animal.Pig
    };

    /// <summary>
    /// 初始化单例实例，确保场景中仅存在一个MoveHintEffect实例。
    /// 如果已存在实例则销毁自身，否则设置为全局唯一实例并保持不随场景加载销毁。
    /// </summary>
    private void Awake()
    {
        if (Get != null)
        {
            Destroy(gameObject);
            return;
        }

        Get = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 为指定棋子生成可移动位置的提示框。
    /// </summary>
    /// <param name="chessman">需要显示移动提示的目标棋子</param>
    /// <remarks>
    /// 1. 首先清除已有提示
    /// 2. 校验棋子有效性及选择状态
    /// 3. 检查棋子是否已移动过
    /// 4. 遍历棋盘所有格子，筛选可移动位置并创建提示框
    /// </remarks>
    public void ShowMoveHints(Chessman chessman)
    {
        ClearMoveHints(); // 清除旧提示

        if (chessman == null || SelectCore.Selection != chessman)
        {
            return;
        }

        // 新增检查：如果该棋子已在本回合中移动过，则不显示任何提示
        if (PlayerManager.HasChessmanMovedThisTurn(chessman))
        {
            Debug.Log($"{chessman.animal} 已移动过，不显示移动提示");
            return;
        }

        ChessBoard board = ChessBoard.Get;

        foreach (var square in board.squares)
        {
            if (CanMoveTo(chessman, square.location))
            {
                var hintInstance = Instantiate(moveHintPrefab, transform);
                hintInstance.SetActive(true);

                // 获取对应 Square 的 RectTransform
                if (square.TryGetComponent(out RectTransform rectTransform))
                {
                    hintInstance.transform.SetParent(rectTransform, false);
                    hintInstance.transform.localPosition = Vector3.zero;
                }

                currentHints.Add(hintInstance);
            }
        }
    }

    /// <summary>
    /// 判断棋子是否可以直线移动或跳跃到指定坐标位置。
    /// 包含地形限制、阻挡规则和特殊跳跃规则的综合判断。
    /// </summary>
    /// <param name="chessman">移动主体棋子</param>
    /// <param name="target">目标坐标位置</param>
    /// <returns>是否允许移动到目标位置</returns>
    /// <remarks>
    /// 核心移动规则优先级：
    /// 1. 先判断是否为长距离直线移动（需要跳跃）
    /// 2. 再依次尝试草地跳跃和水域跳跃规则
    /// 3. 最后执行普通移动规则判断
    /// </remarks>
    public bool CanMoveTo(Chessman chessman, Location target)
    {
        Location current = chessman.location;

        // 基础条件：不能原地踏步
        if (current == target)
        {
            return false;
        }

        // 计算移动特征
        bool isStraightLine = IsStraightLine(current, target);
        bool isLongMove = !current.IsNear(target);
        bool isTigerOrDragon = chessman.animal == Animal.Tiger || chessman.animal == Animal.Dragon;
        bool isMonkey = chessman.animal == Animal.Monkey;

        if (isStraightLine && isLongMove)
        {
            // 1. 尝试草地跳跃
            if ((isTigerOrDragon || isMonkey) && 
                CanJumpOverGrass(current, target, out _, chessman))
            {
                return true;
            }

            // 2. 尝试水域跳跃（仅Tiger/Dragon）
            if (isTigerOrDragon && 
                CanJumpOverWater(current, target, out _, chessman))
            {
                return true;
            }

            // 3. 非法跳跃类型
            return false;
        }

        // 普通移动逻辑：必须是相邻且直线移动
        if (!current.IsNear(target) || !isStraightLine)
        {
            return false;
        }

        ChessBoard board = ChessBoard.Get;
        Square square = board[target.x, target.y];

        // 水域限制规则
        // if (square.type == SquareType.Water && !IsWaterAccessible(chessman.animal))
        if (square.type == SquareType.Water && !TerrainRule.CanEnter(SquareType.Water, chessman.animal))
        {
            return false;
        }

        // 草地限制规则
        // if (square.type == SquareType.Grass && !IsGrassAccessible(chessman.animal))
        if (square.type == SquareType.Grass && !TerrainRule.CanEnter(SquareType.Grass, chessman.animal))
        {
            return false;
        }

        // 非跳跃时检查目标格是否有棋子阻挡
        if (square.Chessman != null && square.Chessman.camp == chessman.camp)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 判断两个坐标点是否位于同一行或同一列（构成直线移动路径）。
    /// </summary>
    /// <param name="a">第一个坐标点</param>
    /// <param name="b">第二个坐标点</param>
    /// <returns>是否构成直线移动路径</returns>
    private bool IsStraightLine(Location a, Location b)
    {
        return a.x == b.x || a.y == b.y;
    }

    /// <summary>
    /// 检查棋子是否可以进行跨水域跳跃，并收集路径信息。
    /// 要求：起点非水域，路径全水域，终点非水域，且路径中最多一个敌方棋子。
    /// </summary>
    /// <param name="start">跳跃起始坐标</param>
    /// <param name="end">跳跃终止坐标</param>
    /// <param name="path">输出参数，记录有效跳跃路径上的所有坐标点</param>
    /// <param name="chessman">执行跳跃的棋子</param>
    /// <returns>是否满足水域跳跃条件</returns>
    /// <remarks>
    /// 遍历跳跃路径上的所有中间格子，验证：
    /// - 必须全部是水域
    /// - 最多允许一个敌方棋子
    /// - 不允许存在己方棋子
    /// </remarks>
    public bool CanJumpOverWater(Location start, Location end, out List<Location> path, Chessman chessman)
    {
        path = new List<Location>();
        ChessBoard board = ChessBoard.Get;
        
        Square currentSquare = board[chessman.location];
        
        // 新增判断：如果当前棋子所在格子是 Water，则不允许跳河
        if (currentSquare.type == SquareType.Water)
        {
            return false;
        }

        int dx = end.x - start.x;
        int dy = end.y - start.y;

        // 必须是直线移动
        if (!(dx == 0 || dy == 0))
        {
            return false;
        }

        int stepX = dx != 0 ? (dx > 0 ? 1 : -1) : 0;
        int stepY = dy != 0 ? (dy > 0 ? 1 : -1) : 0;

        int alliedCount = 0;       // 己方棋子计数
        int totalChessmanCount = 0; // 总棋子数量

        for (int x = start.x + stepX, y = start.y + stepY;
             x != end.x || y != end.y;
             x += stepX, y += stepY)
        {
            var loc = new Location(x, y);
            var square = board[loc];

            // 中间格子必须存在且是 Water
            if (square == null || square.type != SquareType.Water)
            {
                path.Clear();
                return false;
            }

            path.Add(loc);

            if (square.Chessman != null)
            {
                totalChessmanCount++;

                if (square.Chessman.camp == chessman.camp)
                {
                    alliedCount++;
                }
            }
        }

        if (alliedCount > 0 || totalChessmanCount >= 2)
        {
            path.Clear();
            return false;
        }

        // 终点必须是非水域
        var endSquare = board[end.x, end.y];
        if (endSquare == null || endSquare.type == SquareType.Water)
        {
            path.Clear();
            return false;
        }

        return true;
    }

    /// <summary>
    /// 检查棋子是否可以进行跨草地跳跃，并收集路径信息。
    /// 要求：路径全草地，终点非草地，路径中最多一个敌方棋子且无己方棋子。
    /// </summary>
    /// <param name="start">跳跃起始坐标</param>
    /// <param name="end">跳跃终止坐标</param>
    /// <param name="path">输出参数，记录有效跳跃路径上的所有坐标点</param>
    /// <param name="chessman">执行跳跃的棋子</param>
    /// <returns>是否满足草地跳跃条件</returns>
    /// <remarks>
    /// 遍历跳跃路径上的所有中间格子，验证：
    /// - 必须全部是草地
    /// - 不允许存在己方棋子
    /// - 最多允许一个敌方棋子
    /// </remarks>
    public bool CanJumpOverGrass(Location start, Location end, out List<Location> path, Chessman chessman)
    {
        path = new List<Location>();
        ChessBoard board = ChessBoard.Get;

        int dx = end.x - start.x;
        int dy = end.y - start.y;

        // 必须是直线移动
        if (!(dx == 0 || dy == 0))
        {
            return false;
        }

        int stepX = dx != 0 ? (dx > 0 ? 1 : -1) : 0;
        int stepY = dy != 0 ? (dy > 0 ? 1 : -1) : 0;

        int enemyCount = 0;       // 敌方棋子计数

        for (int x = start.x + stepX, y = start.y + stepY;
             x != end.x || y != end.y;
             x += stepX, y += stepY)
        {
            var loc = new Location(x, y);
            var square = board[loc];

            // 中间格子必须存在且是 Grass
            if (square == null || square.type != SquareType.Grass)
            {
                path.Clear();
                return false;
            }

            path.Add(loc);

            if (square.Chessman != null)
            {
                if (square.Chessman.camp == chessman.camp)
                {
                    // 己方棋子阻挡
                    path.Clear();
                    return false;
                }
                else
                {
                    // 敌方棋子，统计数量
                    enemyCount++;
                }
            }
        }

        if (enemyCount > 1)
        {
            path.Clear();
            return false;
        }

        // 终点必须是合法非草地方格
        var endSquare = board[end.x, end.y];
        if (endSquare == null || endSquare.type == SquareType.Grass)
        {
            path.Clear();
            return false;
        }

        return true;
    }

    // /// <summary>
    // /// 检查指定动物是否允许进入水域格子。
    // /// </summary>
    // /// <param name="animal">动物类型</param>
    // /// <returns>是否允许进入水域</returns>
    // private bool IsWaterAccessible(Animal animal)
    // {
    //     return WaterAccessibleAnimals.Contains(animal);
    // }
    //
    // /// <summary>
    // /// 检查指定动物是否允许进入草地格子。
    // /// </summary>
    // /// <param name="animal">动物类型</param>
    // /// <returns>是否允许进入草地</returns>
    // private bool IsGrassAccessible(Animal animal)
    // {
    //     return GrassAccessibleAnimals.Contains(animal);
    // }

    /// <summary>
    /// 销毁所有当前显示的移动提示框并清空实例列表。
    /// </summary>
    public void ClearMoveHints()
    {
        foreach (var hint in currentHints)
        {
            Destroy(hint);
        }
        currentHints.Clear();
    }
}
