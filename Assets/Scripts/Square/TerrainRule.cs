using System.Collections.Generic;
using System;

/// <summary>
/// 地形规则管理器：统一管理所有地形访问规则和跳跃规则
/// 通过静态方法提供全局访问接口，避免重复代码
/// 职责包含：
/// 1. 地形访问权限验证
/// 2. 跳跃能力判定
/// 3. 路径合法性校验
/// 4. 陷阱规则处理
/// </summary>
public static class TerrainRule
{
    #region 地形访问规则
    
    // 各地形允许的动物集合
    private static readonly Dictionary<SquareType, HashSet<Animal>> AccessRules = new Dictionary<SquareType, HashSet<Animal>>
    {
        {
            SquareType.Water, 
            new HashSet<Animal> { Animal.Ox, Animal.Tiger, Animal.Snake, Animal.Dog }
        },
        {
            SquareType.Grass, 
            new HashSet<Animal>(Enum.GetValues(typeof(Animal)) as Animal[]) // 全部允许
        }
    };

    /// <summary>
    /// 判断动物是否可以进入指定地形
    /// </summary>
    /// <param name="terrainType">目标地形类型</param>
    /// <param name="animal">待检测动物</param>
    /// <returns>布尔值表示是否允许进入</returns>
    public static bool CanEnter(SquareType terrainType, Animal animal)
    {
        if (terrainType == SquareType.Grass) return true; // 所有动物都可以进入草地
        
        if (AccessRules.TryGetValue(terrainType, out var allowedAnimals))
        {
            return allowedAnimals.Contains(animal);
        }
        return true; // 默认允许进入普通地形
    }

    #endregion

    #region 跳跃规则

    /// <summary>
    /// 可进行水域跳跃的动物判定
    /// </summary>
    /// <param name="animal">待检测动物</param>
    /// <returns>布尔值表示是否具备水域跳跃能力</returns>
    public static bool IsWaterJumper(Animal animal) => 
        animal is Animal.Tiger or Animal.Dragon;

    /// <summary>
    /// 可进行草地跳跃的动物判定
    /// </summary>
    /// <param name="animal">待检测动物</param>
    /// <returns>布尔值表示是否具备草地跳跃能力</returns>
    public static bool IsGrassJumper(Animal animal) => 
        animal is Animal.Tiger or Animal.Dragon or Animal.Monkey;

    /// <summary>
    /// 检查跳跃路径是否符合指定地形要求
    /// 验证规则：
    /// 1. 必须直线移动
    /// 2. 路径地形必须完全匹配目标地形
    /// 3. 路径阻挡规则（己方阻挡/敌方数量限制）
    /// 4. 终点有效性验证
    /// </summary>
    /// <param name="terrainType">目标地形类型</param>
    /// <param name="start">起点坐标</param>
    /// <param name="end">终点坐标</param>
    /// <param name="chessman">移动的棋子对象</param>
    /// <param name="path">输出参数：合法路径的位置列表</param>
    /// <returns>布尔值表示路径是否合法</returns>
    public static bool ValidateJumpPath(SquareType terrainType, Location start, Location end, Chessman chessman, out List<Location> path)
    {
        path = new List<Location>();
        var board = ChessBoard.Get;
        
        // 获取路径步长
        int dx = end.x - start.x;
        int dy = end.y - start.y;
        
        // 必须是直线移动
        if (!(dx == 0 || dy == 0)) return false;
        
        int stepX = dx != 0 ? (dx > 0 ? 1 : -1) : 0;
        int stepY = dy != 0 ? (dy > 0 ? 1 : -1) : 0;
        int enemyCount = 0;
        int totalChessmanCount = 0;

        // 遍历路径中的所有位置
        for (int x = start.x + stepX, y = start.y + stepY; 
             x != end.x || y != end.y; 
             x += stepX, y += stepY)
        {
            var loc = new Location(x, y);
            var square = board[loc];
            
            // 检查地形是否匹配
            if (square?.type != terrainType)
            {
                path.Clear();
                return false;
            }
            
            path.Add(loc);

            if (square.Chessman != null)
            {
                totalChessmanCount++;
                
                // 己方阻挡
                if (square.Chessman.camp == chessman.camp)
                {
                    path.Clear();
                    return false;
                }
                
                // 统计敌方数量
                enemyCount++;
            }
        }

        // 路径有效性验证
        if (terrainType == SquareType.Water && 
            (enemyCount > 1 || totalChessmanCount >= 2))
        {
            path.Clear();
            return false;
        }

        if (terrainType == SquareType.Grass && enemyCount > 1)
        {
            path.Clear();
            return false;
        }

        // 检查终点有效性
        var endSquare = board[end.x, end.y];
        if (endSquare == null || 
            endSquare.type == terrainType || 
            endSquare.type == SquareType.Trap)
        {
            path.Clear();
            return false;
        }

        return true;
    }

    #endregion

    #region 陷阱规则

    /// <summary>
    /// 判断棋子是否处于敌方陷阱
    /// 包含两种情况：
    /// 1. 中立陷阱（对所有棋子有效）
    /// 2. 敌方阵营陷阱
    /// </summary>
    /// <param name="chessman">待检测棋子</param>
    /// <returns>布尔值表示是否处于敌方陷阱</returns>
    public static bool IsInTrap(Chessman chessman)
    {
        var square = ChessBoard.Get[chessman.location];
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
    /// 处于陷阱时战斗力归零，否则返回动物等级数值
    /// </summary>
    /// <param name="chessman">待计算战斗力的棋子</param>
    /// <returns>整数表示有效战斗力值</returns>
    public static int CalculateEffectiveStrength(Chessman chessman) => 
        IsInTrap(chessman) ? 0 : (int)chessman.animal;

    #endregion
}
