using UnityEngine;

/// <summary>
/// 选择核心管理类，用于管理棋子的选中状态
/// 提供全局唯一的选中状态管理功能，支持跨场景存在
/// </summary>
public class SelectCore : MonoBehaviour
{
    /// <summary>
    /// 线程安全的单例实例锁对象
    /// 用于确保多线程环境下单例初始化的原子性操作
    /// </summary>
    private static readonly object _instanceLock = new object();

    /// <summary>
    /// 单例实例存储对象
    /// 使用私有字段保证外部无法直接修改实例引用
    /// </summary>
    private static SelectCore _instance;

    /// <summary>
    /// 获取单例实例的线程安全访问器
    /// 在实例未初始化时返回null并触发错误日志
    /// </summary>
    /// <returns>返回已存在的SelectCore实例，若未初始化则返回null</returns>
    public static SelectCore Get
    {
        get
        {
            if (_instance == null)
            {
                Debug.LogError("SelectCore instance is not initialized!");
            }
            return _instance;
        }
    }

    /// <summary>
    /// 当前选中的棋子对象引用
    /// 通过序列化字段在Inspector中可视化调试
    /// </summary>
    [SerializeField]
    private Chessman _selection;

    /// <summary>
    /// 全局访问的当前选中棋子属性
    /// 提供线程安全的选中对象访问接口
    /// </summary>
    public static Chessman Selection => Get._selection;

    /// <summary>
    /// Unity生命周期方法：场景加载时初始化
    /// 确保全局唯一实例并保持跨场景存在
    /// </summary>
    private void Awake()
    {
        // 线程安全的单例初始化逻辑
        lock (_instanceLock)
        {
            // 处理重复实例情况
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("Duplicate SelectCore instance detected. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }

            // 初始化单例并设置跨场景保持
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    /// <summary>
    /// 尝试设置新的棋子选中状态
    /// 允许传入null值进行选中状态重置
    /// </summary>
    /// <param name="chessman">需要选中的Chessman对象，可为空</param>
    public static void TrySelect(Chessman chessman)
    {
        if (_instance == null)
        {
            Debug.LogError("SelectCore is not initialized. Cannot select chessman.");
            return;
        }

        _instance._selection = chessman;
    }

    /// <summary>
    /// 清除当前棋子选中状态
    /// 将触发全局选中状态重置操作
    /// </summary>
    public static void DropSelect()
    {
        if (_instance == null)
        {
            Debug.LogError("SelectCore is not initialized. Cannot drop selection.");
            return;
        }

        _instance._selection = null;
    }
}
