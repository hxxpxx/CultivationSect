// ===== MakePillsSession.cs =====
// 炼丹小游戏的会话状态机：一炉的流转（待命 → 炼化中 → 炼成）。
using System;
using UnityEngine;

namespace CultivationSect.MakePills
{
    public enum SessionState { Idle, Running, Refined }

    public class MakePillsSession : MonoBehaviour
    {
        public SessionState State { get; private set; } = SessionState.Idle;
        public int round;                    // 炉次

        public event Action<SessionState> OnStateChanged;

        public void Begin()
        {
            round++;
            SetState(SessionState.Running);
        }

        public void EndRefined()
        {
            SetState(SessionState.Refined);
        }

        void SetState(SessionState s)
        {
            State = s;
            OnStateChanged?.Invoke(s);
        }
    }
}
