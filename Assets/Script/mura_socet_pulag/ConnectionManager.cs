using System.Collections.Generic;
using UnityEngine;

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
           // Debug.Log($"[Connect] 繋ぎ替え: {plugNode.owner?.gameObject.name ?? "Battery"} → {socketOwner.gameObject.name}");
            Recalculate();
            return;
        }
        connections[plugNode] = socketOwner;
       // Debug.Log($"[Connect] {plugNode.owner?.gameObject.name ?? "Battery"} → {socketOwner.gameObject.name}");
        Recalculate();
    }

    public void Disconnect(PowerNode plugNode)
    {
        if (!connections.ContainsKey(plugNode)) return;
        //Debug.Log($"[Disconnect] {plugNode.owner?.gameObject.name ?? "Battery"} 切断");
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
            node.SetPoweredBy(null);
            node.SetDepth(int.MaxValue); // バッテリーからの距離
        }

        // ① batteryから直接繋がっているmonitorを深さ1で有効化
        foreach (var pair in connections)
        {
            if (pair.Key.isBattery)
            {
                battery bat = pair.Key.GetComponentInParent<battery>();
                if (bat != null)
                {
                    pair.Value.SetPowered(true);
                    pair.Value.SetPoweredBy(bat);
                    pair.Value.SetDepth(1);
                }
            }
        }

        // ② 変化がなくなるまで伝播（深さも記録）
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
                    socketOwner.SetDepth(plugOwner.GetDepth() + 1);
                    changed = true;
                }
                else if (socketPowered && plugOwner != null && !plugOwner.IsPowered())
                {
                    plugOwner.SetPowered(true);
                    plugOwner.SetPoweredBy(socketOwner.GetPoweredBy());
                    plugOwner.SetDepth(socketOwner.GetDepth() + 1);
                    changed = true;
                }
            }
        }

        // ③ battery別に深さ順でソートして容量内だけONにする
        foreach (battery bat in allBatteries)
        {
            // このbatteryから電力をもらっているノードを収集
            List<PowerNode> poweredNodes = new List<PowerNode>();
            foreach (PowerNode node in allNodes)
                if (node.IsPowered() && !node.isBattery && node.GetPoweredBy() == bat)
                    poweredNodes.Add(node);

            //バッテリーに近い順（depth昇順）でソート
            poweredNodes.Sort((a, b) => a.GetDepth().CompareTo(b.GetDepth()));

            int count = poweredNodes.Count;

            if (count > bat.maxCharge)
            {
                // 容量超過分（遠い方から）をOFF
                for (int i = bat.maxCharge; i < count; i++)
                {
                    poweredNodes[i].SetPowered(false);
                    poweredNodes[i].SetPoweredBy(null);
                    //Debug.Log($"[Recalculate] {poweredNodes[i].gameObject.name} 容量超過でOFF (depth={poweredNodes[i].GetDepth()})");
                }
                count = bat.maxCharge;
            }

            bat.SetCharge(bat.maxCharge - count);
            //Debug.Log($"[Recalculate] {bat.gameObject.name}: {bat.currentCharge}/{bat.maxCharge} (接続数={count})");
        }
    }
}