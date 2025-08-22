using System;
using System.Collections.Generic;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using PortalTweaks.Behaviors;
using UnityEngine;
using Random = UnityEngine.Random;

namespace PortalTweaks.Tweaks;

public static class Portal
{
    private static readonly Dictionary<string, ConfigEntry<string>> keyConfigs = new();
    private static PortalCharge? m_currentPortal;
    
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    private static class Register_Tweaks
    {
        private static void Postfix(ZNetScene __instance)
        {
            if (!__instance) return;
            foreach (var prefab in __instance.m_prefabs)
            {
                if (prefab.GetComponent<TeleportWorld>())
                {
                    if (prefab.GetComponent<PortalCharge>() == null)
                        prefab.AddComponent<PortalCharge>();

                    /* if (prefab.TryGetComponent(out WearNTear wearNTear))
                    {
                        ConfigEntry<float> health = PortalTweaksPlugin._Plugin.config(prefab.name, "Health", wearNTear.m_health, "Set health");
                        wearNTear.m_health = health.Value;
                        health.SettingChanged += (sender, args) => wearNTear.m_health = health.Value;
                        ConfigEntry<bool> ashImmune = PortalTweaksPlugin._Plugin.config(prefab.name, "Ash Damage Immune", wearNTear.m_ashDamageImmune, "Set immune to ash damage");
                        ConfigEntry<bool> ashResist = PortalTweaksPlugin._Plugin.config(prefab.name, "Ash Damage Resist", wearNTear.m_ashDamageResist, "Set resistant to ash damage");
                        wearNTear.m_ashDamageImmune = ashImmune.Value;
                        wearNTear.m_ashDamageResist = ashResist.Value;
                        ashImmune.SettingChanged += (sender, args) => wearNTear.m_ashDamageImmune = ashImmune.Value;
                        ashResist.SettingChanged += (sender, args) => wearNTear.m_ashDamageResist = ashResist.Value;
                    }

                    if (prefab.TryGetComponent(out Piece piece))
                    {
                        List<string> resources = new();
                        foreach (Piece.Requirement? req in piece.m_resources)
                        {
                            resources.Add($"{req.m_resItem.name}:{req.m_amount}");
                        }

                        ConfigEntry<string> requirements = PortalTweaksPlugin._Plugin.config(prefab.name, "Recipe", String.Join(",", resources), "[itemName]:[amount], ...");
                        List<Piece.Requirement> reqs = new();
                        foreach (string data in requirements.Value.Split(','))
                        {
                            string[] info = data.Split(':');
                            if (info.Length != 2) continue;
                            GameObject item = __instance.GetPrefab(info[0]);
                            if (!item || !item.TryGetComponent(out ItemDrop itemDrop)) continue;
                            reqs.Add(new Piece.Requirement()
                            {
                                m_resItem = itemDrop,
                                m_amount = int.TryParse(info[1], out int amount) ? amount : 1,
                                m_recover = true,
                                m_extraAmountOnlyOneIngredient = 1
                            });
                        }

                        piece.m_resources = reqs.ToArray();

                        requirements.SettingChanged += (sender, args) =>
                        {
                            List<Piece.Requirement> configRequirements = new();
                            foreach (string data in requirements.Value.Split(','))
                            {
                                string[] info = data.Split(':');
                                if (info.Length != 2) continue;
                                GameObject item = __instance.GetPrefab(info[0]);
                                if (!item || !item.TryGetComponent(out ItemDrop itemDrop)) continue;
                                configRequirements.Add(new Piece.Requirement()
                                {
                                    m_resItem = itemDrop,
                                    m_amount = int.TryParse(info[1], out int amount) ? amount : 1,
                                    m_recover = true,
                                    m_extraAmountOnlyOneIngredient = 1
                                });
                            }

                            piece.m_resources = configRequirements.ToArray();
                        };

                        ConfigEntry<Piece.PieceCategory> category = PortalTweaksPlugin._Plugin.config(prefab.name, "Category", piece.m_category, "Set category");
                        if (Enum.IsDefined(typeof(Piece.PieceCategory), category.Value))
                        {
                            piece.m_category = category.Value;
                        }

                        category.SettingChanged += (sender, args) =>
                        {
                            if (Enum.IsDefined(typeof(Piece.PieceCategory), category.Value))
                            {
                                piece.m_category = category.Value;
                            }
                        };
                    } */
                }

                /*if (prefab.TryGetComponent(out ItemDrop component))
                {
                    if (component.m_itemData.m_shared.m_teleportable) continue;
                    ConfigEntry<string> config = PortalTweaksPlugin._Plugin.config("Keys", prefab.name, "", "Set defeat key to allow teleportation");
                    keyConfigs[component.m_itemData.m_shared.m_name] = config;
                } */
            }
        }
    }

    [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.UseItem))]
    private static class TeleportWorld_UseItem_Patch
    {
        private static void Postfix(TeleportWorld __instance, Humanoid user, ItemDrop.ItemData item, ref bool __result)
        {
            if (!__instance.TryGetComponent(out PortalCharge component)) return;

            if (item.m_shared.m_name != component.GetChargeItem()?.m_itemData.m_shared.m_name) return;

            int stack = item.m_stack;
            int max = PortalTweaksPlugin._chargeMax.Value - component.GetCurrentCharge();

            if (stack > max)
            {
                if (!component.AddCharge(max)) return;
                if (user.GetInventory().RemoveItem(item, max))
                {
                    __result = true;
                    return;
                }
            }
            else
            {
                if (!component.AddCharge(stack)) return;
                if (user.GetInventory().RemoveItem(item))
                {
                    __result = true;
                    return;
                }
            }
            user.Message(MessageHud.MessageType.Center, FullyChargedMessage());

            // if (!component.AddCharge(1))
            // {
            //     user.Message(MessageHud.MessageType.Center, FullyChargedMessage());
            //     __result = true;
            //     return;
            // }
            // if (user.GetInventory().RemoveOneItem(item))
            // {
            //     __result = true;
            // }
        }
    }

    [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.GetHoverName))]
    private static class TeleportWorld_GetHoverName_Patch
    {
        private static void Postfix(TeleportWorld __instance, ref string __result)
        {
            if (__instance.TryGetComponent(out Piece component))
            {
                __result = component.m_name;
            }
        }
    }

    [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.GetHoverText))]
    private static class TeleportWorld_GetHoverText_Patch
    {
        private static void Postfix(TeleportWorld __instance, ref string __result)
        {
            if (!__instance) return;
            if (!__instance.m_nview.IsValid()) return;
            if (!__instance.TryGetComponent(out PortalCharge component)) return;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(__result);
            stringBuilder.Append("\n");
            stringBuilder.Append($"{component.GetChargeItem()?.m_itemData.m_shared.m_name}: ");
            stringBuilder.Append($"{component.GetCurrentCharge()} / {PortalTweaksPlugin._chargeMax.Value}\n");
            if (!PortalTweaksPlugin.m_isTargetPortalInstalled) 
                stringBuilder.Append($"[<color=yellow><b>L.Shift</b></color> + <color=yellow><b>$KEY_Use</b></color>] {Localization.instance.Localize("$portal_add_charge")}");
            __result = Localization.instance.Localize(stringBuilder.ToString());
        }
    }

    [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.Interact))]
    private static class TeleportWorld_Interact_Patch
    {
        private static bool Prefix(TeleportWorld __instance, Humanoid human, bool alt)
        {
            if (PortalTweaksPlugin.m_isTargetPortalInstalled) return true;
            if (!__instance.m_nview.IsValid()) return true;
            if (!alt) return true;
            if (!PrivateArea.CheckAccess(__instance.transform.position)) return true;
            if (!__instance.TryGetComponent(out PortalCharge component)) return false;
            string? itemName = component.GetChargeItem()?.m_itemData.m_shared.m_name;
            if (itemName.IsNullOrWhiteSpace()) return false;
            ItemDrop.ItemData? item = human.GetInventory().GetItem(itemName);
            if (item == null)
            {
                human.Message(MessageHud.MessageType.Center, RequiredItemMessage(component));
                return false;
            }
            if (component.AddCharge(1))
            {
                if (!human.GetInventory().RemoveOneItem(item)) return false;
                human.DoInteractAnimation(__instance.gameObject);
                return false;
            }
            human.Message(MessageHud.MessageType.Center, FullyChargedMessage());
            return false;
        }
    }

    [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.Teleport))]
    private static class TeleportWorld_Teleport_Patch
    {
        public static bool teleported = false;

        private static bool Prefix(TeleportWorld __instance)
        {
            teleported = false;
            if (!__instance.TryGetComponent(out PortalCharge component)) return true;
            if (!component.CanTeleport())
            {
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, RequiredItemMessage(component));
                return false;
            }

            if (__instance.TargetFound() && ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoBossPortals))
            {
                if (Player.m_localPlayer.m_nview.GetZDO().GetBool("EventFlag".GetStableHashCode()))
                {
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_blockedbyboss");
                    return false;
                }
            }
            return true;
        }

        private static void Postfix(TeleportWorld __instance, Player player)
        {
            if (!__instance.TryGetComponent(out PortalCharge component)) return;
            if (teleported)
            {
                component.RemoveCharge(PortalTweaksPlugin._cost.Value);
                TeleportTames(__instance, player);
            }
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.TeleportTo))]
    private static class Player_TeleportTo_Patch
    {
        private static void Postfix(Player __instance, Vector3 pos, Quaternion rot)
        {
            TeleportWorld_Teleport_Patch.teleported = true;
            if (!PortalTweaksPlugin.m_isTargetPortalInstalled) return;
            if (!__instance) return;
            if (m_currentPortal == null) return;
            if (!__instance.NoCostCheat())
                m_currentPortal.RemoveCharge(PortalTweaksPlugin._cost.Value);
            m_currentPortal = null;
            if (PortalTweaksPlugin._TeleportTames.Value is PortalTweaksPlugin.Toggle.Off) return;
            TeleportCharacters(GetTames(__instance), pos, rot);
        }
    }

    [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.HaveTarget))]
    private static class TeleportWorld_HaveTarget_Patch
    {
        private static void Postfix(TeleportWorld __instance, ref bool __result)
        {
            if (!__instance.TryGetComponent(out PortalCharge component)) return;
            __result &= component.CanTeleport();
        }
    }

    [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.TargetFound))]
    private static class TeleportWorld_TargetFound
    {
        private static void Postfix(TeleportWorld __instance, ref bool __result)
        {
            if (!__instance.TryGetComponent(out PortalCharge component)) return;
            __result &= component.CanTeleport();
        }
    }

    /* [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.IsTeleportable))]
    private static class Humanoid_IsTeleportable_Patch
    {
        private static void Postfix(Humanoid __instance, ref bool __result)
        {
            if (__result) return;
            if (PortalTweaksPlugin._TeleportAnything.Value is PortalTweaksPlugin.Toggle.On)
            {
                __result = true;
                return;
            }

            if (PortalTweaksPlugin._UseKeys.Value is PortalTweaksPlugin.Toggle.Off) return;
            __result = CanTeleport(__instance);
        }
    } */

    private static string RequiredItemMessage(PortalCharge component) => string.Format(Localization.instance.Localize("$portal_req_charge"), (PortalTweaksPlugin._cost.Value > 1 ? PortalTweaksPlugin._cost.Value.ToString() + " " : "") + Localization.instance.Localize(component.GetChargeItem()?.m_itemData.m_shared.m_name));

    private static string FullyChargedMessage() => Localization.instance.Localize("$portal_fully_charged");

    /* private static bool CanTeleport(Humanoid __instance)
    {
        foreach (var itemData in __instance.GetInventory().m_inventory)
        {
            if (itemData.m_shared.m_teleportable) continue;
            if (!keyConfigs.TryGetValue(itemData.m_shared.m_name, out ConfigEntry<string> config)) return false;
            if (config.Value.IsNullOrWhiteSpace()) return false;
            if (ZoneSystem.instance.GetGlobalKey(config.Value)) continue;
            if (__instance.HaveUniqueKey(config.Value)) continue;
            return false;
        }

        return true;
    } */

    private static void TeleportTames(TeleportWorld __instance, Player player)
    {
        if (PortalTweaksPlugin._TeleportTames.Value is PortalTweaksPlugin.Toggle.Off) return;
        if (!__instance.TargetFound()) return;
        if (ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoPortals)) return;
        ZDO zdo = ZDOMan.instance.GetZDO(__instance.m_nview.GetZDO().GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal));
        if (zdo == null) return;
        GetLocation(__instance, zdo, out Vector3 position, out Quaternion rotation);
        TeleportCharacters(GetTames(player), position, rotation);
    }

    private static void TeleportCharacters(List<Character> characters, Vector3 position, Quaternion rotation)
    {
        foreach (Character? character in characters)
        {
            if (character is Humanoid humanoid && !humanoid.GetInventory().IsTeleportable()) continue;
            Vector3 random = Random.insideUnitSphere * 10f;
            Vector3 location = position + new Vector3(random.x, 0f, random.z);
            TeleportTo(character, location, rotation);
        }
    }

    private static void GetLocation(TeleportWorld __instance, ZDO zdo, out Vector3 position, out Quaternion rotation)
    {
        position = zdo.GetPosition();
        rotation = zdo.GetRotation();
        Vector3 vector3 = rotation * Vector3.forward;
        Vector3 pos = position + vector3 * __instance.m_exitDistance + Vector3.up;
        position = pos;
    }

    private static List<Character> GetTames(Player player)
    {
        List<Character> m_characters = new();
        foreach (Character? character in Character.GetAllCharacters())
        {
            if (!character.TryGetComponent(out Tameable tameable)) continue;
            if (tameable.m_monsterAI == null) continue;
            GameObject follow = tameable.m_monsterAI.GetFollowTarget();
            if (!follow) continue;
            if (!follow.TryGetComponent(out Player component)) continue;
            if (component.GetHoverName() != player.GetHoverName()) continue;
            m_characters.Add(character);
        }

        return m_characters;
    }

    private static void TeleportTo(Character character, Vector3 pos, Quaternion rot)
    {
        if (!character.m_nview.IsOwner()) character.m_nview.ClaimOwnership();
        Transform transform = character.transform;
        pos.y = ZoneSystem.instance.GetSolidHeight(pos) + 0.5f;
        transform.position = pos;
        transform.rotation = rot;
        character.m_body.linearVelocity = Vector3.zero;
    }

    [HarmonyPatch(typeof(TeleportWorldTrigger), nameof(TeleportWorldTrigger.OnTriggerEnter))]
    private static class TeleportWorldTrigger_OnTriggerEnter_Patch
    {
        private static void Postfix(TeleportWorldTrigger __instance)
        {
            if (!PortalTweaksPlugin.m_isTargetPortalInstalled) return;
            if (!Minimap.instance || !Game.instance || !Player.m_localPlayer) return;
            if (!__instance.m_teleportWorld.TryGetComponent(out PortalCharge component)) return;
            if (component.CanTeleport())
            {
                m_currentPortal = component;
                return;
            }
            Minimap.instance.SetMapMode(Game.m_noMap ? Minimap.MapMode.None : Minimap.MapMode.Small);
            Player.m_localPlayer.Message(MessageHud.MessageType.Center, RequiredItemMessage(component));
        }
    }
}