using UnityEngine;

namespace PortalTweaks.Behaviors;

public class PortalCharge : MonoBehaviour
{
    private ZNetView? m_znv;
    private static readonly int m_key = "portalCharge".GetStableHashCode();
    private static readonly int m_timerKey = "portalChargeTimer".GetStableHashCode();
    private const float m_updateTime = 30f;
    public void Awake()
    {
        if (!ZNet.instance) return;
        m_znv = GetComponent<ZNetView>();
        if (m_znv == null) return;
        if (!m_znv.IsValid()) return;
        
        m_znv.Register<int>(nameof(RPC_AddCharge), RPC_AddCharge);
        m_znv.Register<int>(nameof(RPC_RemoveCharge), RPC_RemoveCharge);

        long lastDecay = m_znv.GetZDO().GetLong(m_timerKey);
        if (lastDecay == 0L)
        {
            m_znv.GetZDO().Set(m_timerKey, (long)ZNet.instance.GetTimeSeconds());
        }

        if (GetCurrentCharge() > 0 && PortalTweaksPlugin._Decays.Value is PortalTweaksPlugin.Toggle.On)
        {
            InvokeRepeating(nameof(UpdateCharge), m_updateTime, m_updateTime);
        }
    }

    public void UpdateCharge()
    {
        if (m_znv == null || !ZNet.instance) return;
        if (!m_znv.IsValid()) return;
        int floor = GetDecayAmount();
        if (floor == 0) return;
        if (!RemoveCharge(floor) || PortalTweaksPlugin._Decays.Value is PortalTweaksPlugin.Toggle.Off)
        {
            m_znv.CancelInvoke(nameof(UpdateCharge));
        }
    }

    private int GetDecaySeconds() => PortalTweaksPlugin._chargeDecay.Value * 60;

    private int GetDecayAmount()
    {
        if (m_znv == null || !ZNet.instance) return 0;
        if (!m_znv.IsValid()) return 0;
        long lastDecay = m_znv.GetZDO().GetLong(m_timerKey);
        long difference = (long)ZNet.instance.GetTimeSeconds() - lastDecay;
        long amount = difference / GetDecaySeconds();
        return Mathf.FloorToInt(amount);
    }

    public bool AddCharge(int amount)
    {
        if (m_znv == null) return false;
        if (!m_znv.IsValid()) return false;
        int currentCharge = GetCurrentCharge();
        if (currentCharge >= PortalTweaksPlugin._chargeMax.Value) return false;
        m_znv.ClaimOwnership();
        m_znv.InvokeRPC(nameof(RPC_AddCharge), amount);
        return true;
    }

    public int GetCurrentCharge()
    {
        if (m_znv == null) return PortalTweaksPlugin._cost.Value;
        return !m_znv.IsValid() ? PortalTweaksPlugin._cost.Value : m_znv.GetZDO().GetInt(m_key);
    }

    private void RPC_AddCharge(long sender, int amount)
    {
        if (m_znv == null || !ZNet.instance) return;
        if (!m_znv.IsValid()) return;
        int currentCharge = GetCurrentCharge();
        if (currentCharge >= PortalTweaksPlugin._chargeMax.Value) return;
        m_znv.GetZDO().Set(m_key, Mathf.Clamp(currentCharge + amount, 0, PortalTweaksPlugin._chargeMax.Value));
        ResetTimer();
        CancelInvoke(nameof(UpdateCharge));
        if (PortalTweaksPlugin._Decays.Value is PortalTweaksPlugin.Toggle.Off) return;
        InvokeRepeating(nameof(UpdateCharge), m_updateTime, m_updateTime);
    }

    public bool RemoveCharge(int amount)
    {
        if (amount == 0) return false;
        if (m_znv == null) return false;
        if (!m_znv.IsValid()) return false;
        int currentCharge = GetCurrentCharge();
        if (currentCharge == 0) return false;
        m_znv.InvokeRPC(nameof(RPC_RemoveCharge), currentCharge - amount < 0 ? currentCharge : amount);
        return true;
    }

    private void RPC_RemoveCharge(long sender, int amount)
    {
        if (m_znv == null || !ZNet.instance) return;
        if (!m_znv.IsValid() || m_znv.GetZDO() == null) return;
        int currentCharge = GetCurrentCharge();
        if (currentCharge - amount < 0)
        {
            m_znv.GetZDO().Set(m_key, 0);
        }
        else
        {
            m_znv.GetZDO().Set(m_key, currentCharge - amount);
        }
        ResetTimer();
    }

    private void ResetTimer()
    {
        if (m_znv == null || !ZNet.instance) return;
        if (!m_znv.IsValid() || m_znv.GetZDO() == null) return;
        m_znv.GetZDO().Set(m_timerKey, (long)ZNet.instance.GetTimeSeconds());
    }

    public bool CanTeleport() => GetCurrentCharge() - PortalTweaksPlugin._cost.Value >= 0 || (Player.m_localPlayer?.NoCostCheat() ?? false);

    public ItemDrop? GetChargeItem()
    {
        if (!ObjectDB.instance) return null;
        GameObject prefab = ObjectDB.instance.GetItemPrefab(PortalTweaksPlugin._chargeItem.Value);
        if (!prefab)
        {
            prefab = ObjectDB.instance.GetItemPrefab("GreydwarfEye");
            if (!prefab) return null;
        }

        return prefab.TryGetComponent(out ItemDrop component) ? component : null;
    }
}