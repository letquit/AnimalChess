using UnityEngine;

/// <summary>
/// 管理选择框视觉效果的组件，负责根据选中对象的阵营动态切换选择框样式
/// 需要配合Camera组件使用，通过OnPostRender事件实现渲染后更新
/// </summary>
[RequireComponent(typeof(Camera))]
public class SelectEffect : MonoBehaviour
{
    /// <summary>
    /// 蓝方阵营选择框的预制体模板
    /// 用于在Inspector中拖拽赋值
    /// </summary>
    [SerializeField] private GameObject blueSelectionFramePrefab;

    /// <summary>
    /// 红方阵营选择框的预制体模板
    /// 用于在Inspector中拖拽赋值
    /// </summary>
    [SerializeField] private GameObject redSelectionFramePrefab;

    /// <summary>
    /// 当前激活的选择框实例的引用
    /// 用于动态更新位置和控制显隐
    /// </summary>
    private GameObject currentSelectionFrame;

    /// <summary>
    /// 记录当前显示的选择框所属阵营
    /// 用于检测阵营变化时触发样式切换
    /// </summary>
    private Camp currentCamp = Camp.None;

    /// <summary>
    /// 缓存当前选择框的Transform组件
    /// 避免频繁GetComponent调用提升性能
    /// </summary>
    private Transform currentFrameTransform;

    /// <summary>
    /// 初始化时校验必要字段的赋值情况
    /// 如果预制体未赋值则自动禁用组件并输出错误日志
    /// </summary>
    void Awake()
    {
        // 检查 Prefab 是否赋值
        if (blueSelectionFramePrefab == null || redSelectionFramePrefab == null)
        {
            Debug.LogError("SelectEffect: 必须为 blueSelectionFramePrefab 和 redSelectionFramePrefab 赋值");
            enabled = false;
            return;
        }
    }

    /// <summary>
    /// 渲染后事件处理，负责选择框的动态更新
    /// 实现以下核心逻辑：
    /// 1. 获取当前选中对象及其阵营信息
    /// 2. 根据选中状态控制显隐
    /// 3. 检测阵营变化时切换选择框样式
    /// 4. 持续更新选择框的位置和父级关系
    /// </summary>
    void OnPostRender()
    {
        var selection = SelectCore.Selection;
        if (selection == null || !selection.TryGetComponent(out RectTransform rectTransform))
        {
            HideSelectionFrame();
            return;
        }

        Camp selectedCamp = selection.camp;

        // 如果阵营未变化，仅更新位置和可见性
        if (currentCamp == selectedCamp)
        {
            if (currentFrameTransform != null)
            {
                currentFrameTransform.SetParent(rectTransform, false);
                currentFrameTransform.localPosition = Vector3.zero;
                currentFrameTransform.gameObject.SetActive(true);
            }
            return;
        }

        // 销毁旧实例
        DestroyCurrentSelectionFrame();

        // 创建新实例
        GameObject prefabToUse = selectedCamp == Camp.Red ? redSelectionFramePrefab : blueSelectionFramePrefab;
        currentSelectionFrame = Instantiate(prefabToUse, Vector3.zero, Quaternion.identity, rectTransform);
        currentSelectionFrame.name = "SelectionFrame";

        // 缓存 Transform
        currentFrameTransform = currentSelectionFrame.transform;
        currentFrameTransform.localPosition = Vector3.zero;

        // 更新阵营状态
        currentCamp = selectedCamp;
    }

    /// <summary>
    /// 销毁当前选择框实例并重置相关引用
    /// 用于处理阵营切换或取消选择时的资源清理
    /// </summary>
    private void DestroyCurrentSelectionFrame()
    {
        if (currentSelectionFrame != null)
        {
            Destroy(currentSelectionFrame);
            currentSelectionFrame = null;
            currentFrameTransform = null;
            currentCamp = Camp.None;
        }
    }

    /// <summary>
    /// 隐藏当前选择框的公共方法
    /// 通过销毁当前实例实现隐藏效果
    /// </summary>
    private void HideSelectionFrame()
    {
        DestroyCurrentSelectionFrame();
    }
}
