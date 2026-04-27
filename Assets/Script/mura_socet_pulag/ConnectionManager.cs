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
        if (connections.TryGetValue(plugNode, out PowerNode existing))
        {
            if (existing == socketOwner) return;
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
        battery[] allBatteries = FindObjectsByType<battery>(FindObjectsSortMode.None);

        // 全ノードをリセット
        foreach (PowerNode node in allNodes)
        {
            node.SetPowered(false);
            node.SetPoweredBy(null); // どのbatteryから電力をもらっているか
        }

        // 各batteryから直接繋がっているmonitorを有効化
        foreach (var pair in connections)
        {
            if (pair.Key.isBattery)
            {
                battery bat = pair.Key.GetComponentInParent<battery>();
                pair.Value.SetPowered(true);
                pair.Value.SetPoweredBy(bat);
            }
        }

        // 変化がなくなるまで伝播
        bool changed = true;
        while (changed)
        {
            changed = false;
            foreach (var pair in connections)
            {
                PowerNode plugOwner = pair.Key.owner;
                PowerNode socketOwner = pair.Value;

                bool plugPowered = plugOwner != null && plugOwner.IsPowered();
                bool socketPowered = socketOwner.IsPowered();

                if (plugPowered && !socketPowered)
                {
                    socketOwner.SetPowered(true);
                    socketOwner.SetPoweredBy(plugOwner.GetPoweredBy());
                    changed = true;
                }
                else if (socketPowered && plugOwner != null && !plugOwner.IsPowered())
                {
                    plugOwner.SetPowered(true);
                    plugOwner.SetPoweredBy(socketOwner.GetPoweredBy());
                    changed = true;
                }
            }
        }

        // battery別に消費数をカウントして残量を更新
        foreach (battery bat in allBatteries)
        {
            int count = 0;
            foreach (PowerNode node in allNodes)
                if (node.IsPowered() && !node.isBattery && node.GetPoweredBy() == bat)
                    count++;

            bat.SetCharge(bat.maxCharge - count);
            Debug.Log($"[Recalculate] {bat.gameObject.name}: {bat.currentCharge}/{bat.maxCharge} (接続数={count})");
        }
    }
}