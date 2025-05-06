using UnityEngine;

public class CommandCenter : MonoBehaviour
{
    /// <summary>
    /// 向棋子发出移动指令
    /// </summary>
    /// <param name="chessman">目标棋子</param>
    /// <param name="target">目标位置坐标</param>
    /// <param name="ignorePlayerColor">是否忽略玩家阵营限制（默认false）</param>
    /// <param name="swapPlayersAfterMovement">移动后是否切换玩家回合（默认true）</param>
    /// <returns>无返回值</returns>
    public static void GameOrder(Chessman chessman, Location target, bool ignorePlayerColor = false, bool swapPlayersAfterMovement = true)
    {
        // 参数合法性检查
        // 验证目标棋子是否为空引用
        if (chessman == null)
        {
            Debug.LogWarning("尝试对空棋子下达命令");
            return;
        }

        // 缓存 PlayerManager 实例
        // 避免重复调用 Instance 属性访问器
        PlayerManager playerManager = PlayerManager.Instance;

        // 玩家切换前置条件检查
        // 验证是否需要切换玩家及PlayerManager初始化状态
        if (swapPlayersAfterMovement && playerManager == null)
        {
            Debug.LogWarning("无法切换玩家回合，PlayerManager未初始化");
            return;
        }

        // 目标位置有效性验证
        // 检查坐标是否超出棋盘范围
        if (!target.IsValid())
        {
            Debug.LogWarning("目标位置无效");
            return;
        }

        // 阵营限制条件检查
        // 当不忽略阵营时执行以下验证逻辑
        if (!ignorePlayerColor)
        {
            // 玩家对象初始化验证
            // 确保当前玩家控制器和玩家对象已初始化
            if (playerManager == null || playerManager.currentPlayer == null)
            {
                Debug.LogWarning("当前玩家未初始化");
                return;
            }

            // 阵营匹配验证
            // 确保棋子阵营与当前玩家一致
            if (chessman.camp != playerManager.currentPlayer)
            {
                return;
            }
        }

        // 执行棋子移动操作
        // 调用棋子对象的移动方法并传递切换玩家标志
        chessman.MoveTo(target, swapPlayersAfterMovement);
    }
}
