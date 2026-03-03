using UnityEngine;

[System.Serializable]
public class TrapConfig
{
    public Vector2Int position;     // 陷阱位置
    public int camp;                // 阵营：0=无, 1=红方, 2=蓝方
}