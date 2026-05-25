using UnityEngine;

public class FlowStageDebugController : MonoBehaviour
{
    void Update()
    {
        GameManager manager = GameManager.Instance;
        if (manager == null || !manager.BalanceConfig.debug.enableFlowStageJump)
            return;

        KeyCode[] keys = manager.BalanceConfig.debug.flowStageKeys;
        if (keys == null)
            return;

        bool snapshotMode = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        FlowJumpMode mode = snapshotMode ? FlowJumpMode.FlowSnapshot : FlowJumpMode.TimeOnly;

        for (int i = 0; i < keys.Length; i++)
        {
            if (Input.GetKeyDown(keys[i]))
            {
                manager.DebugJumpToFlowStage(i, mode);
                break;
            }
        }
    }
}
