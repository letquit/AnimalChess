using UnityEngine;
using System;

#region Location:坐标
/// <summary>
/// 表示棋盘上的坐标位置，包含行列信息及坐标操作方法。
/// 提供坐标有效性验证、相邻判断、向量转换等功能。
/// </summary>
[Serializable]
public struct Location
{
    /// <summary>
    /// 横坐标（列索引），有效范围0-18，表示棋盘横向位置。
    /// </summary>
    public int x; 

    /// <summary>
    /// 纵坐标（行索引），有效范围0-12，表示棋盘纵向位置。
    /// </summary>
    public int y; 

    /// <summary>
    /// 将当前坐标转换为Unity二维向量形式，便于进行向量运算。
    /// </summary>
    public Vector2Int Vector => new Vector2Int(x, y); 

    /// <summary>
    /// 返回坐标对象的字符串表示。
    /// </summary>
    /// <returns>格式为"(x,y)"的坐标字符串</returns>
    public override string ToString()
    {
        return $"({x},{y})";
    }

    /// <summary>
    /// 判断当前坐标与指定坐标是否为相邻坐标（曼哈顿距离为1）。
    /// </summary>
    /// <param name="other">需要比较的另一个坐标</param>
    /// <returns>当坐标相邻时返回true，否则返回false</returns>
    public bool IsNear(Location other)
    {
        int dx = Math.Abs(x - other.x);
        int dy = Math.Abs(y - other.y);
        return (dx + dy) == 1 && dx >= 0 && dy >= 0; // 曼哈顿距离为1
    }

    /// <summary>
    /// 静态方法：判断两个坐标是否相邻。
    /// </summary>
    /// <param name="a">第一个坐标</param>
    /// <param name="b">第二个坐标</param>
    /// <returns>当坐标相邻时返回true，否则返回false</returns>
    public static bool IsNear(Location a, Location b)
    {
        return a.IsNear(b);
    }

    /// <summary>
    /// 坐标合法性验证常量定义。
    /// X轴范围：0 ≤ x ≤ 18
    /// Y轴范围：0 ≤ y ≤ 12
    /// </summary>
    public const int Xmin = 0;
    public const int Xmax = 18;
    public const int Ymin = 0;
    public const int Ymax = 12;

    /// <summary>
    /// 验证当前坐标是否在合法范围内。
    /// </summary>
    /// <returns>坐标合法返回true，否则返回false</returns>
    public bool IsValid()
    {
        return IsValid(x, y);
    }

    /// <summary>
    /// 验证指定坐标值是否在合法范围内。
    /// </summary>
    /// <param name="x">待验证的横坐标</param>
    /// <param name="y">待验证的纵坐标</param>
    /// <returns>坐标合法返回true，否则返回false</returns>
    private static bool IsValid(int x, int y)
    {
        return x >= Xmin && x <= Xmax && y >= Ymin && y <= Ymax;
    }

    /// <summary>
    /// 使用指定坐标值初始化Location实例。
    /// </summary>
    /// <param name="x">横坐标值</param>
    /// <param name="y">纵坐标值</param>
    /// <exception cref="ArgumentException">当在编辑器环境下创建非法坐标时抛出异常</exception>
    public Location(int x, int y)
    {
        this.x = x;
        this.y = y;
#if UNITY_EDITOR
        if (!IsValid(x, y))
        {
            throw new ArgumentException($"尝试创建一个超出棋盘范围的坐标:({x},{y})");
        }
#endif
    }

    /// <summary>
    /// 重载等于运算符，判断两个坐标是否相等。
    /// </summary>
    /// <param name="a">第一个比较对象</param>
    /// <param name="b">第二个比较对象</param>
    /// <returns>坐标值相等时返回true</returns>
    public static bool operator ==(Location a, Location b)
    {
        return a.x == b.x && a.y == b.y;
    }

    /// <summary>
    /// 重载不等于运算符，判断两个坐标是否不等。
    /// </summary>
    /// <param name="a">第一个比较对象</param>
    /// <param name="b">第二个比较对象</param>
    /// <returns>坐标值不等时返回true</returns>
    public static bool operator !=(Location a, Location b)
    {
        return a.x != b.x || a.y != b.y;
    }

    /// <summary>
    /// 重写Equals方法，判断对象是否与当前坐标相等。
    /// </summary>
    /// <param name="obj">需要比较的对象</param>
    /// <returns>当对象为Location类型且坐标值相等时返回true</returns>
    public override bool Equals(object obj)
    {
        if (obj is Location)
        {
            Location other = (Location)obj;
            return x == other.x && y == other.y;
        }
        return false;
    }

    /// <summary>
    /// 重写GetHashCode方法，生成基于坐标值的哈希码。
    /// </summary>
    /// <returns>由x和y值组合生成的哈希码</returns>
    public override int GetHashCode()
    {
        return x.GetHashCode() ^ y.GetHashCode();
    }
}
#endregion

#region Camp
/// <summary>
/// 表示玩家阵营分类，用于区分不同势力单位。
/// 包含中立、蓝方、红方和无阵营四种状态。
/// </summary>
public enum Camp
{
    Neutral, Blue, Red, None
}
#endregion

#region Animal
/// <summary>
/// 动物等级枚举，定义游戏中各动物棋子的战斗力等级。
/// 数值越大等级越高，特殊单位猫为最高等级。
/// </summary>
public enum Animal
{
    Pig = 1,
    Dog = 2,
    Chicken = 3,
    Monkey = 4,
    Goat = 5,
    Horse = 6,
    Snake = 7,
    Dragon = 8,
    Rabbit = 9,
    Tiger = 10,
    Ox = 11,
    Rat = 12,
    Cat = 13 // 添加了猫，作为第13种动物，等级位于鼠之上
}
#endregion

#region SquareType
/// <summary>
/// 棋盘格地形类型定义，决定格子的功能特性。
/// 包含普通路径、水域、草地、宝藏点、基地和陷阱等类型。
/// </summary>
public enum SquareType
{
    Pattern, Water, Grass, Treasure, Home, Trap
}
#endregion
