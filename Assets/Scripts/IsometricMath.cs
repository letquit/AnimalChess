using UnityEngine;

/// <summary>
/// 提供等距投影数学转换功能的静态类
/// </summary>
public static class IsometricMath
{
    /// <summary>
    /// 将网格坐标转换为世界坐标
    /// </summary>
    /// <param name="gridPos">网格坐标位置</param>
    /// <param name="tileWidth">瓦片宽度</param>
    /// <param name="tileHeight">瓦片高度</param>
    /// <returns>对应的世界坐标位置</returns>
    public static Vector2 GridToWorld(Vector2Int gridPos, float tileWidth, float tileHeight)
    {
        float x = (gridPos.x * 0.5f * tileWidth) + (gridPos.y * -0.5f * tileWidth);
        float y = (gridPos.x * 0.25f * tileHeight) + (gridPos.y * 0.25f * tileHeight);
        
        // Unity Y轴向上，等距图通常Y轴向下，翻转Y
        return new Vector2(x, -y);
    }

    /// <summary>
    /// 将世界坐标转换为网格坐标（矩阵逆运算）
    /// </summary>
    /// <param name="worldPos">世界坐标位置</param>
    /// <param name="tileWidth">瓦片宽度</param>
    /// <param name="tileHeight">瓦片高度</param>
    /// <returns>对应的网格坐标位置</returns>
    public static Vector2Int WorldToGrid(Vector2 worldPos, float tileWidth, float tileHeight)
    {
        float a = 0.5f * tileWidth;
        float b = -0.5f * tileWidth;
        float c = -0.25f * tileHeight;
        float d = -0.25f * tileHeight;
        
        float det = (a * d) - (b * c);
        
        // 逆矩阵计算
        float gridX = (d * worldPos.x - b * worldPos.y) / det;
        float gridY = (-c * worldPos.x + a * worldPos.y) / det;

        return new Vector2Int(Mathf.RoundToInt(gridX), Mathf.RoundToInt(gridY));
    }
    
    /// <summary>
    /// 将网格坐标转换为带原点偏移的世界坐标
    /// </summary>
    /// <param name="gridPos">网格坐标位置</param>
    /// <param name="tileWidth">瓦片宽度</param>
    /// <param name="tileHeight">瓦片高度</param>
    /// <returns>带有原点偏移的世界坐标位置</returns>
    public static Vector2 GridToWorldWithOffset(Vector2Int gridPos, float tileWidth, float tileHeight)
    {
        Vector2 pos = GridToWorld(gridPos, tileWidth, tileHeight);
        
        float offsetX = 0.25f * tileWidth;
        float offsetY = -0.125f * tileHeight;
        
        return pos + new Vector2(offsetX, offsetY);
    }
    
    /// <summary>
    /// 将带原点偏移的世界坐标转换为网格坐标
    /// </summary>
    /// <param name="worldPos">带偏移的世界坐标位置</param>
    /// <param name="tileWidth">瓦片宽度</param>
    /// <param name="tileHeight">瓦片高度</param>
    /// <returns>对应的网格坐标位置</returns>
    public static Vector2Int WorldToGridWithOffset(Vector2 worldPos, float tileWidth, float tileHeight)
    {
        float offsetX = 0.25f * tileWidth;
        float offsetY = -0.125f * tileHeight;
        
        Vector2 adjustedPos = worldPos - new Vector2(offsetX, offsetY);
        
        return WorldToGrid(adjustedPos, tileWidth, tileHeight);
    }
}
