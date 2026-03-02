using System.Collections.Generic;
using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 棋子基类，管理棋子的基础属性和行为逻辑
/// </summary>
public class Chessman : MonoBehaviour
{
    /// <summary>
    /// 棋子在棋盘上的坐标位置
    /// </summary>
    public Location location;

    /// <summary>
    /// 棋子代表的动物类型（决定战斗力和移动特性）
    /// </summary>
    public Animal animal;

    /// <summary>
    /// 棋子所属阵营（红方/蓝方）
    /// </summary>
    public Camp camp;

    /// <summary>
    /// 记录动物原始战斗力值用于陷阱状态计算
    /// </summary>
    private int originalStrength;

    /// <summary>
    /// 返回棋子的字符串表示（用于调试输出）
    /// </summary>
    public override string ToString() => $"棋子坐标:{location} 动物类型:{animal} 阵营:{camp}";

    /// <summary>
    /// 获取所有存活的棋子对象
    /// </summary>
    /// <param name="camp">筛选阵营（默认中立阵营表示获取所有阵营棋子）</param>
    /// <returns>符合条件的棋子列表</returns>
    public static List<Chessman> All(Camp camp = Camp.Neutral)
    {
        List<Chessman> ret = new List<Chessman>();
        foreach (var chessman in FindObjectsOfType<Chessman>())
        {
            if (camp == Camp.Neutral || chessman.camp == camp)
                ret.Add(chessman);
        }
        return ret;
    }

    /// <summary>
    /// 清除所有棋子（用于游戏重置）
    /// </summary>
    public static void ClearAll()
    {
        var all = All();
        for (int i = all.Count - 1; i >= 0; i--)
            all[i].ExitFromBoard();
    }

    /// <summary>
    /// 根据坐标获取对应棋子
    /// </summary>
    /// <param name="location">目标坐标</param>
    /// <returns>坐标上的棋子对象或null</returns>
    public static Chessman GetChessman(Location location)
    {
        foreach (var chessman in All())
        {
            if (chessman.location.Equals(location))
                return chessman;
        }
        return null;
    }

    /// <summary>
    /// 获取棋子当前所在方块
    /// </summary>
    public Square Square => ChessBoard.Get[location];

    /// <summary>
    /// Unity初始化方法，绑定点击事件和初始移动
    /// </summary>
    public void Start()
    {
        if (camp == Camp.Neutral)
        {
            Debug.LogError("棋子阵营不能为中立。");
            return;
        }

        originalStrength = (int)animal;
        GetComponent<Button>().onClick.AddListener(OnChessmanClicked);
        MoveTo(location, false, true);
    }

    /// <summary>
    /// 执行棋子移动逻辑，包含路径验证和战斗判定
    /// </summary>
    /// <param name="target">目标坐标</param>
    /// <param name="swapPlayers">移动后是否切换玩家回合</param>
    /// <param name="ignoreRestriction">是否忽略移动限制（如已移动过本回合的限制）</param>
    public void MoveTo(Location target, bool swapPlayers = true, bool ignoreRestriction = false)
    {
        try
        {
            // 检查移动限制
            if (!ignoreRestriction && PlayerManager.HasChessmanMovedThisTurn(this))
                return;

            // 路径合法性检查
            if (!ignoreRestriction)
            {
                Square currentSquare = Square;
                bool isHorizontal = location.y == target.y;
                bool isVertical = location.x == target.x;
                bool isStraightLine = isHorizontal || isVertical;
                bool isLongMove = !location.IsNear(target);

                // 处理长距离移动（跳跃逻辑）
                if (isStraightLine && isLongMove)
                {
                    PathType pathType = AnalyzePathType(location, target);
                    if (pathType == PathType.Invalid)
                    {
                        Debug.LogWarning("路径中有障碍或非法地形，无法跳跃");
                        return;
                    }

                    // 水域跳跃处理
                    // if (pathType == PathType.Water && IsJumpOverWaterAnimal(animal))
                    if (pathType == PathType.Water && TerrainRule.IsWaterJumper(animal))
                    {
                        if (currentSquare.type == SquareType.Water)
                        {
                            Debug.LogWarning("位于水域中的棋子不能进行跳河");
                            return;
                        }

                        List<Location> path;
                        if (!CanJumpOverWater(location, target, out path, this))
                        {
                            Debug.LogWarning("跳河失败");
                            return;
                        }

                        HandleJumpMove(target, path, swapPlayers);
                        return;
                    }

                    // 草地跳跃处理
                    if (pathType == PathType.Grass && IsJumpOverGrassAnimal(animal))
                    {
                        List<Location> path;
                        if (!CanJumpOverGrass(location, target, out path, this))
                        {
                            Debug.LogWarning("跳跃草地失败");
                            return;
                        }

                        HandleJumpMove(target, path, swapPlayers);
                        return;
                    }

                    Debug.LogWarning("不支持的跳跃类型");
                    return;
                }

                // 基础移动规则检查
                if (!location.IsNear(target))
                {
                    Debug.LogWarning("只能横向或纵向移动一个格子");
                    return;
                }

                Square square = ChessBoard.Get[target.x, target.y];

                // 目标位置地形校验
                // if (square.type == SquareType.Water && !CanEnterWater(animal))
                if (square.type == SquareType.Water && !TerrainRule.CanEnter(SquareType.Water, animal))
                {
                    Debug.LogWarning($"{animal} 不允许进入水域");
                    return;
                }

                // if (square.type == SquareType.Grass && !CanEnterGrass(animal)) 
                if (square.type == SquareType.Grass && !TerrainRule.CanEnter(SquareType.Grass, animal))
                {
                    Debug.LogWarning($"{animal} 不允许进入草地");
                    return;
                }

                // 同阵营棋子阻挡检查
                if (square.Chessman != null && square.Chessman.camp == camp)
                {
                    Debug.LogWarning("目标位置上有己方棋子，不能移动");
                    return;
                }
            }

            // 战斗判定处理
            Square squareTarget = ChessBoard.Get[target.x, target.y];
            if (squareTarget.Chessman != null && squareTarget.Chessman != this)
            {
                int strengthComparison = CompareAnimalStrength(this, squareTarget.Chessman);
                HandleCombat(squareTarget, strengthComparison);
            }

            // 陷阱状态更新
            UpdateTrapStatus(squareTarget);
            location = target;
            transform.DOMove(squareTarget.transform.position, 0.35f);

            // 移动记录和回合切换
            if (!ignoreRestriction)
            {
                PlayerManager.MarkChessmanAsMoved(this);
                if (swapPlayers) PlayerManager.Tick();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"移动棋子失败: {ex}");
        }
    }

    /// <summary>
    /// 执行跳跃移动的特殊处理
    /// </summary>
    /// <param name="target">目标坐标</param>
    /// <param name="path">跳跃路径上的所有坐标点</param>
    /// <param name="swapPlayers">是否切换玩家回合</param>
    private void HandleJumpMove(Location target, List<Location> path, bool swapPlayers)
    {
        var targetSquare = ChessBoard.Get[target];
        if (targetSquare.Chessman != null && targetSquare.Chessman.camp != camp)
        {
            int strengthComparison = CompareAnimalStrength(this, targetSquare.Chessman);
            HandleCombat(targetSquare, strengthComparison);
        }
        else if (targetSquare.Chessman != null && targetSquare.Chessman.camp == camp)
        {
            Debug.LogWarning("不能吃掉己方棋子");
            return;
        }

        // 清除路径上的敌方棋子
        foreach (var loc in path)
        {
            var square = ChessBoard.Get[loc];
            if (square.Chessman != null && square.Chessman.camp != camp)
                square.RemoveChessman();
        }

        // 执行位置更新和动画
        location = target;
        transform.DOMove(targetSquare.transform.position, 0.35f);
        PlayerManager.MarkChessmanAsMoved(this);
        if (swapPlayers) PlayerManager.Tick();
    }

    /// <summary>
    /// 处理两个棋子之间的战斗逻辑
    /// </summary>
    /// <param name="targetSquare">目标方块</param>
    /// <param name="strengthComparison">力量比较结果（正数表示攻击方强，负数表示防御方强，0表示同归于尽）</param>
    private void HandleCombat(Square targetSquare, int strengthComparison)
    {
        if (strengthComparison > 0)
        {
            Debug.Log($"[吃子] {animal} 吃掉了 {targetSquare.Chessman.animal}");
            targetSquare.RemoveChessman();
        }
        else if (strengthComparison < 0)
        {
            Debug.Log($"[吃子] {targetSquare.Chessman.animal} 吃掉了 {animal}");
            ExitFromBoard();
        }
        else
        {
            Debug.Log($"[同归于尽] {animal} 和 {targetSquare.Chessman.animal} 阵亡");
            targetSquare.RemoveChessman();
            ExitFromBoard();
        }
    }

    /// <summary>
    /// 分析两点之间的路径类型（水域/草地/无效）
    /// </summary>
    /// <param name="start">起点坐标</param>
    /// <param name="end">终点坐标</param>
    /// <returns>路径类型枚举</returns>
    private PathType AnalyzePathType(Location start, Location end)
    {
        int dx = end.x - start.x;
        int dy = end.y - start.y;

        if (!(dx == 0 || dy == 0)) return PathType.Invalid;

        int stepX = dx != 0 ? (dx > 0 ? 1 : -1) : 0;
        int stepY = dy != 0 ? (dy > 0 ? 1 : -1) : 0;

        bool? isWaterPath = null;

        for (int x = start.x + stepX, y = start.y + stepY; x != end.x || y != end.y; x += stepX, y += stepY)
        {
            var loc = new Location(x, y);
            var square = ChessBoard.Get[loc];
            if (square == null) return PathType.Invalid;

            if (square.type == SquareType.Water)
            {
                if (isWaterPath == null) isWaterPath = true;
                else if (!isWaterPath.Value) return PathType.Invalid;
            }
            else if (square.type == SquareType.Grass)
            {
                if (isWaterPath == null) isWaterPath = false;
                else if (isWaterPath.Value) return PathType.Invalid;
            }
            else
            {
                return PathType.Invalid;
            }
        }

        var endSquare = ChessBoard.Get[end.x, end.y];
        if (endSquare == null || endSquare.type == SquareType.Water || endSquare.type == SquareType.Grass)
            return PathType.Invalid;

        return isWaterPath.HasValue && isWaterPath.Value ? PathType.Water : PathType.Grass;
    }

    /// <summary>
    /// 路径类型枚举
    /// </summary>
    private enum PathType { Invalid, Water, Grass }

    /// <summary>
    /// 判断动物是否能跳跃草地
    /// </summary>
    private bool IsJumpOverGrassAnimal(Animal animal) => animal is Animal.Tiger or Animal.Dragon or Animal.Monkey;
    
    /// <summary>
    /// 判断动物是否能跳跃水域
    /// </summary>
    private bool IsJumpOverWaterAnimal(Animal animal) => animal is Animal.Tiger or Animal.Dragon;

    /// <summary>
    /// 验证是否可以执行草地跳跃
    /// </summary>
    /// <param name="start">起点坐标</param>
    /// <param name="end">终点坐标</param>
    /// <param name="path">输出路径坐标列表</param>
    /// <param name="chessman">跳跃的棋子对象</param>
    /// <returns>是否可以通过草地跳跃</returns>
    private bool CanJumpOverGrass(Location start, Location end, out List<Location> path, Chessman chessman)
    {
        path = new List<Location>();
        int dx = end.x - start.x, stepX = dx != 0 ? (dx > 0 ? 1 : -1) : 0;
        int dy = end.y - start.y, stepY = dy != 0 ? (dy > 0 ? 1 : -1) : 0;
        int enemyCount = 0;

        for (int x = start.x + stepX, y = start.y + stepY; x != end.x || y != end.y; x += stepX, y += stepY)
        {
            var loc = new Location(x, y);
            var square = ChessBoard.Get[loc];

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
                    path.Clear();
                    return false;
                }
                else
                {
                    enemyCount++;
                }
            }
        }

        if (enemyCount > 1)
        {
            path.Clear();
            return false;
        }

        return true;
    }

    /// <summary>
    /// 验证是否可以执行水域跳跃
    /// </summary>
    /// <param name="start">起点坐标</param>
    /// <param name="end">终点坐标</param>
    /// <param name="path">输出路径坐标列表</param>
    /// <param name="chessman">跳跃的棋子对象</param>
    /// <returns>是否可以通过水域跳跃</returns>
    public bool CanJumpOverWater(Location start, Location end, out List<Location> path, Chessman chessman)
    {
        path = new List<Location>();
        int dx = end.x - start.x, stepX = dx != 0 ? (dx > 0 ? 1 : -1) : 0;
        int dy = end.y - start.y, stepY = dy != 0 ? (dy > 0 ? 1 : -1) : 0;
        int enemyCount = 0, totalChessmanCount = 0;

        for (int x = start.x + stepX, y = start.y + stepY; x != end.x || y != end.y; x += stepX, y += stepY)
        {
            var loc = new Location(x, y);
            var square = ChessBoard.Get[loc];

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
                    path.Clear();
                    return false;
                }
                else
                {
                    enemyCount++;
                }
            }
        }

        if (enemyCount > 1 || totalChessmanCount >= 2)
        {
            path.Clear();
            return false;
        }

        return true;
    }

    // /// <summary>
    // /// 判断动物是否可以进入水域
    // /// </summary>
    // private bool CanEnterWater(Animal animal) => animal is Animal.Ox or Animal.Tiger or Animal.Snake or Animal.Dog;
    
    // /// <summary>
    // /// 判断动物是否可以进入草地（所有动物都可以）
    // /// </summary>
    // private bool CanEnterGrass(Animal animal) => true;

    /// <summary>
    /// 比较两个棋子的有效战斗力
    /// </summary>
    /// <param name="attacker">攻击方棋子</param>
    /// <param name="defender">防御方棋子</param>
    /// <returns>战斗力差值</returns>
    private int CompareAnimalStrength(Chessman attacker, Chessman defender)
    {
        int strengthA = CalculateEffectiveStrength(attacker);
        int strengthB = CalculateEffectiveStrength(defender);
        return strengthA - strengthB;
    }

    /// <summary>
    /// 判断棋子是否处于敌方陷阱中
    /// </summary>
    public static bool IsInTrap(Chessman chessman)
    {
        Square square = ChessBoard.Get[chessman.location];
        if (square.type != SquareType.Trap) return false;

        return square.camp switch
        {
            Camp.Neutral => true,
            Camp.Blue => chessman.camp == Camp.Red,
            Camp.Red => chessman.camp == Camp.Blue,
            _ => false,
        };
    }

    /// <summary>
    /// 计算棋子的有效战斗力（考虑陷阱影响）
    /// </summary>
    private int CalculateEffectiveStrength(Chessman chessman) => IsInTrap(chessman) ? 0 : (int)chessman.animal;

    /// <summary>
    /// 更新棋子的陷阱状态并输出日志
    /// </summary>
    private void UpdateTrapStatus(Square squareTarget)
    {
        bool isTargetInTrap = squareTarget.type == SquareType.Trap && (
            squareTarget.camp == Camp.Neutral ||
            (squareTarget.camp == Camp.Blue && camp == Camp.Red) ||
            (squareTarget.camp == Camp.Red && camp == Camp.Blue));

        if (isTargetInTrap)
            Debug.Log($"[陷入陷阱] {animal} 攻击力归零");
        else if (IsInTrap(this))
            Debug.Log($"[脱离陷阱] {animal} 恢复战斗力");
    }

    /// <summary>
    /// 棋子点击事件处理
    /// </summary>
    private void OnChessmanClicked() => Square.OnSquareClicked();

    /// <summary>
    /// 从棋盘移除棋子（销毁游戏对象）
    /// </summary>
    public void ExitFromBoard() => Destroy(gameObject);
}
