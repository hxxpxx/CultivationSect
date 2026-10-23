// ===== AIDialogueNode.cs (ScriptableObject) =====
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "CultivationSectLeader/AI/AIDialogueNode")]
public class AIDialogueNode : ScriptableObject
{
    public string nodeId;
    [TextArea(3, 6)] public string aiText;              // AI说的话
    [TextArea(2, 4)] public string playerResponseOption; // 玩家回应选项
    public AIDialogueEffect effect;                      // 对话效果
    public List<AIDialogueNode> nextNodes;               // 后续节点
    public List<string> requiredFlags;                   // 触发条件
    public AIPersonality targetPersonality;
}

[System.Serializable]
public class AIDialogueEffect
{
    public int trustChange;                              // 信任变化
    public float corruptionChange;                       // 腐蚀变化
    public bool revealsSecret;                           // 是否透露秘密
    public bool offersDeal;                             // 是否提出交易
    public string flagToSet;                            // 设置全局flag
}