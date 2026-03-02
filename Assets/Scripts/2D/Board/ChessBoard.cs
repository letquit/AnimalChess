using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// 棋盘类，用于管理棋盘上的每一个方格
/// 提供基于坐标访问方格的功能，并维护全局唯一实例
/// </summary>
public class ChessBoard : MonoBehaviour
{
    /// <summary>
    /// 单例实例，确保全局只有一个棋盘对象
    /// 使用线程锁保证多线程环境下的实例安全
    /// </summary>
    private static ChessBoard _instance;
    
    /// <summary>
    /// 获取棋盘单例实例的访问器
    /// 当设置新实例时会自动销毁重复创建的对象
    /// </summary>
    public static ChessBoard Get
    {
        get
        {
            lock (typeof(ChessBoard))
            {
                return _instance;
            }
        }
        private set
        {
            lock (typeof(ChessBoard))
            {
                if (_instance != null && value != null && _instance != value)
                {
                    Debug.LogError("尝试创建多个 ChessBoard 实例，已自动销毁重复实例");
                    Destroy(value.gameObject);
                    return;
                }
                _instance = value;
            }
        }
    }

    /// <summary>
    /// 存储所有棋盘方格的字典，键为 (x, y) 坐标元组
    /// 用于快速坐标定位方格对象
    /// </summary>
    private Dictionary<(int, int), Square> squareDict = new Dictionary<(int, int), Square>();

    /// <summary>
    /// 坐标索引器，通过二维坐标获取对应的棋盘方格
    /// </summary>
    /// <param name="x">方格的横向坐标（列索引）</param>
    /// <param name="y">方格的纵向坐标（行索引）</param>
    /// <returns>指定坐标的方格对象，若不存在则返回 null</returns>
    /// <exception cref="ArgumentOutOfRangeException">当坐标值为负数时抛出异常</exception>
    public Square this[int x, int y]
    {
        get
        {
            if (x < 0 || y < 0)
                throw new ArgumentOutOfRangeException("坐标不能为负数");
            
            squareDict.TryGetValue((x, y), out var square);
            return square;
        }
    }

    /// <summary>
    /// 位置对象索引器，通过Location对象获取对应的棋盘方格
    /// </summary>
    /// <param name="location">包含有效坐标的Location对象</param>
    /// <returns>调用坐标索引器返回对应的方格对象</returns>
    public Square this[Location location]
    {
        get
        {
            return this[location.x, location.y];
        }
    }

    /// <summary>
    /// Unity生命周期方法，在游戏对象初始化时调用
    /// 完成单例实例设置和方格字典初始化
    /// </summary>
    private void Awake()
    {
        // 设置静态实例为当前对象
        Get = this;

        // 初始化字典
        foreach (var square in squares)
        {
            var key = (square.location.x, square.location.y);
            if (squareDict.ContainsKey(key))
            {
                Debug.LogWarning($"坐标 ({key.Item1}, {key.Item2}) 存在重复方格");
                continue;
            }
            squareDict.Add(key, square);
        }
    }

    /// <summary>
    /// 存储所有棋盘方格的列表（保留原接口兼容性）
    /// 用于初始化时构建方格字典的数据源
    /// </summary>
    public List<Square> squares;
}
