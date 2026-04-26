using UnityEngine;
using System.Collections.Generic;

public class ConnectionManager : MonoBehaviour
{
    public static ConnectionManager Instance { get; private set; }
    private Dictionary<PowerNode, PowerNode> connections = new Dictionary<PowerNode, PowerNode>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void Connect(PowerNode plugNode, PowerNode socketOwner)
    {
        // 既に同じ接続が記録されていればスキップ
        if (connections.TryGetValue(plugNode, out PowerNode existing))
        {
            if (existing == socketOwner) return; // 同じ接続なのでスキップ
                                                 // 別のsocketに繋ぎ替えの場合は更新
            connections[plugNode] = socketOwner;
            Debug.Log($"[Connect] 繋ぎ替え: {plugNode.owner?.gameObject.name ?? "Battery"} → {socketOwner.gameObject.name}");
            Recalculate();
            return;
        }

        connections[plugNode] = socketOwner;
        Debug.Log($"[Connect] {plugNode.owner?.gameObject.name ?? "Battery"} → {socketOwner.gameObject.name}");
        Recalculate();
    }

    public void Disconnect(PowerNode plugNode)
    {
        if (!connections.ContainsKey(plugNode)) return;
        Debug.Log($"[Disconnect] {plugNode.owner?.gameObject.name ?? "Battery"} 切断");
        connections.Remove(plugNode);
        Recalculate();
    }

    void Recalculate()
    {
        PowerNode[] allNodes = FindObjectsByType<PowerNode>(FindObjectsSortMode.None);
        foreach (PowerNode node in allNodes)
            node.SetPowered(false);

        // batteryに直接繋がっているmonitorを有効化
        foreach (var pair in connections)
            if (pair.Key.isBattery)
                pair.Value.SetPowered(true);

        // 変化がなくなるまで繰り返す
        bool changed = true;
        while (changed)
        {
            changed = false;
            foreach (var pair in connections)
            {
                PowerNode plugOwner = pair.Key.owner;   // plugが属するmonitor
                PowerNode socketOwner = pair.Value;      // socketが属するmonitor

                // plugOwnerまたはsocketOwnerどちらかが電力ありなら両方有効化
                bool plugPowered = plugOwner != null && plugOwner.IsPowered();
                bool socketPowered = socketOwner.IsPowered();

                if (plugPowered && !socketPowered)
                {
                    socketOwner.SetPowered(true);
                    changed = true;
                }
                else if (socketPowered && plugOwner != null && !plugOwner.IsPowered())
                {
                    plugOwner.SetPowered(true);
                    changed = true;
                }
            }
        }

        // battery残量更新
        int poweredCount = 0;
        foreach (PowerNode node in allNodes)
            if (node.IsPowered() && !node.isBattery) poweredCount++;

        foreach (battery bat in FindObjectsByType<battery>(FindObjectsSortMode.None ))
            bat.SetCharge(bat.maxCharge - poweredCount);

        Debug.Log($"[Recalculate] poweredCount={poweredCount}");
    }
}