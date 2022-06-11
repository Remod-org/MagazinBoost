using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("MagazinBoost", "Fujikura/RFC1920", "1.6.4", ResourceId = 1962)]
    [Description("Can change magazines, ammo and condition for most projectile weapons")]
    internal class MagazinBoost : RustPlugin
    {
        #region Config
        private ConfigData configData;

        protected override void LoadDefaultConfig()
        {
            Puts("Creating new config file.");
            configData = new ConfigData
            {
                Version = Version
            };
            GetWeapons();
            SaveConfig(configData);
        }

        private void LoadVariables()
        {
            configData = Config.ReadObject<ConfigData>();
        }

        private void SaveConfig(ConfigData config)
        {
            Config.WriteObject(config, true);
        }

        private class ConfigData
        {
            public ConfigRights CheckRights = new ConfigRights();
            public ConfigPermissions Permissions = new ConfigPermissions();
            public Dictionary<string, WeaponStats> Weapons = new Dictionary<string, WeaponStats>();
            public bool Debug;
            public VersionNumber Version;
        }

        private class ConfigRights
        {
            public bool checkPermission = true;
            public bool removeSkinIfNoRights = true;
        }

        private class ConfigPermissions
        {
            public string permissionAll = "magazinboost.canall";
            public string permissionMaxAmmo = "magazinboost.canmaxammo";
            public string permissionPreLoad = "magazinboost.canpreload";
            public string permissionMaxCondition = "magazinboost.canmaxcondition";
            public string permissionAmmoType = "magazinboost.canammotype";
        }

        private class WeaponStats
        {
            public string displayname;
            public int maxammo;
            public int preload;
            public float maxcondition;
            public string ammotype;
            public int skinid;
            public bool settingactive;
            public int servermaxammo;
            public int serverpreload;
            public string serverammotype;
            public float servermaxcondition;
            public bool serveractive;
            public int givemaxammo;
            public int givepreload;
            public string giveammotype;
            public float givemaxcondition;
        }
        #endregion Config

        #region Oxide_hooks
        private void Init()
        {
            LoadVariables();
            GetWeapons();
            permission.RegisterPermission(configData.Permissions.permissionAll, this);
            permission.RegisterPermission(configData.Permissions.permissionMaxAmmo, this);
            permission.RegisterPermission(configData.Permissions.permissionPreLoad, this);
            permission.RegisterPermission(configData.Permissions.permissionMaxCondition, this);
            permission.RegisterPermission(configData.Permissions.permissionAmmoType, this);
        }

        private object OnWeaponReload(BaseProjectile projectile, BasePlayer player)
        {
            if (configData.Debug) Puts("OnReloadWeapon called!");
            Item item = projectile.GetItem();
            WeaponStats weaponStats = new WeaponStats();

            if (configData.Weapons.ContainsKey(item.info.shortname))
            {
                if (configData.Debug) Puts("OnReloadWeapon: Adding weapon ");
                weaponStats = configData.Weapons[item.info.shortname];
            }
            if (!weaponStats.settingactive)
            {
                if (configData.Debug) Puts($"OnReloadWeapon: Weapon type {item.info.shortname} inactive.");
                return null;
            }

            double maxammo = HasExtender(projectile) ? Math.Ceiling(weaponStats.maxammo * 1.25) : weaponStats.maxammo;
            if (HasRight(player, "maxammo") || HasRight(player, "all"))
            {
                if (configData.Debug) Puts($"OnReloadWeapon: Weapon type {item.info.shortname} active - giving maxammo.");
                projectile.primaryMagazine.capacity = (int)maxammo;
                projectile.SendNetworkUpdate();
            }
            return null;
        }

        private bool HasExtender(BaseProjectile projectile)
        {
            foreach (BaseEntity attachment in projectile.children)
            {
                if (attachment.name.Contains("extendedmags.entity.prefab")) return true;
            }
            return false;
        }

        private void OnItemCraftFinished(ItemCraftTask task, Item item)
        {
            if (configData.Debug) Puts("OnItemCraftFinished called!");
            if (!(item.GetHeldEntity() is BaseProjectile)) return;
            if (!HasAnyRight(task.owner)) return;
            WeaponStats weaponStats = new WeaponStats();
            if (configData.Weapons.ContainsKey(item.info.shortname))
            {
                weaponStats = configData.Weapons[item.info.shortname];
            }
            if (!weaponStats.settingactive) return;

            BaseProjectile bp = item.GetHeldEntity() as BaseProjectile;
            if (bp != null)
            {
                if (HasRight(task.owner, "maxammo") || HasRight(task.owner, "all"))
                {
                    bp.primaryMagazine.capacity = weaponStats.maxammo;
                }
                if (HasRight(task.owner, "preload") || HasRight(task.owner, "all"))
                {
                    bp.primaryMagazine.contents = weaponStats.preload;
                }
                if (HasRight(task.owner, "ammotype") || HasRight(task.owner, "all"))
                {
                    ItemDefinition ammo = ItemManager.FindItemDefinition(weaponStats.ammotype);
                    if (ammo != null) bp.primaryMagazine.ammoType = ammo;
                }
                if (HasRight(task.owner, "maxcondition") || HasRight(task.owner, "all"))
                {
                    item._maxCondition = Convert.ToSingle(weaponStats.maxcondition);
                    item._condition = Convert.ToSingle(weaponStats.maxcondition);
                }
                if (weaponStats.skinid > 0)
                {
                    item.skin = Convert.ToUInt64(weaponStats.skinid);
                    item.GetHeldEntity().skinID = Convert.ToUInt64(weaponStats.skinid);
                }
            }
        }

        private void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            if (!configData.CheckRights.checkPermission) return;
            if (item.GetHeldEntity() is BaseProjectile && container.HasFlag(ItemContainer.Flag.Belt))
            {
                WeaponStats weaponStats = null;
                WeaponStats checkStats;
                if (configData.Weapons.TryGetValue(item.info.shortname, out checkStats))
                {
                    weaponStats = checkStats;
                    if (!weaponStats.settingactive) return;
                }
                else
                {
                    return;
                }
                BaseProjectile bp = item.GetHeldEntity() as BaseProjectile;
                if (bp != null)
                {
                    if (bp.primaryMagazine.capacity > item.info.GetComponent<ItemModEntity>().entityPrefab.Get().GetComponent<BaseProjectile>().primaryMagazine.definition.builtInSize && !(HasRight(container.playerOwner, "maxammo") || HasRight(container.playerOwner, "all")))
                    {
                        bp.primaryMagazine.capacity = item.info.GetComponent<ItemModEntity>().entityPrefab.Get().GetComponent<BaseProjectile>().primaryMagazine.definition.builtInSize;
                        if (bp.primaryMagazine.contents > bp.primaryMagazine.capacity)
                        {
                            bp.primaryMagazine.contents = bp.primaryMagazine.capacity;
                        }
                    }
                    if (item.maxCondition > item.info.condition.max && !(HasRight(container.playerOwner, "maxcondition") || HasRight(container.playerOwner, "all")))
                    {
                        float newCon = item.condition * (item.info.condition.max / item.maxCondition);
                        item._maxCondition = Convert.ToSingle(item.info.condition.max);
                        item._condition = Convert.ToSingle(newCon);
                    }
                    if (configData.CheckRights.removeSkinIfNoRights && !HasAnyRight(container.playerOwner) && item.GetHeldEntity().skinID == Convert.ToUInt64(weaponStats.skinid) && item.GetHeldEntity().skinID != 0uL)
                    {
                        item.skin = 0uL;
                        item.GetHeldEntity().skinID = 0uL;
                    }
                }
            }
        }
        #endregion

        #region Main
        private void GetWeapons()
        {
            IEnumerable<ItemDefinition> weapons = ItemManager.GetItemDefinitions().Where(p => p.category == ItemCategory.Weapon && p.GetComponent<ItemModEntity>() != null);

            if (configData.Weapons.Count == 0)
            {
                int counter = 0;
                foreach (ItemDefinition weapon in weapons)
                {
                    if (configData.Debug) Puts($"Processing new weapon {weapon.shortname}");
                    if (weapon.GetComponent<ItemModEntity>().entityPrefab.Get().GetComponent<BaseProjectile>() == null) continue;

                    WeaponStats weaponStats = new WeaponStats()
                    {
                        displayname = weapon.displayName.english,
                        maxammo = weapon.GetComponent<ItemModEntity>().entityPrefab.Get().GetComponent<BaseProjectile>().primaryMagazine.definition.builtInSize,
                        preload = weapon.GetComponent<ItemModEntity>().entityPrefab.Get().GetComponent<BaseProjectile>().primaryMagazine.contents,
                        maxcondition = weapon.condition.max,
                        ammotype = weapon.GetComponent<ItemModEntity>().entityPrefab.Get().GetComponent<BaseProjectile>().primaryMagazine.ammoType.shortname,
                        skinid = 0,
                        settingactive = true,
                        servermaxammo = weapon.GetComponent<ItemModEntity>().entityPrefab.Get().GetComponent<BaseProjectile>().primaryMagazine.definition.builtInSize,
                        serverpreload = weapon.GetComponent<ItemModEntity>().entityPrefab.Get().GetComponent<BaseProjectile>().primaryMagazine.contents,
                        serverammotype = weapon.GetComponent<ItemModEntity>().entityPrefab.Get().GetComponent<BaseProjectile>().primaryMagazine.ammoType.shortname,
                        servermaxcondition = weapon.condition.max,
                        serveractive = false,
                        givemaxammo = weapon.GetComponent<ItemModEntity>().entityPrefab.Get().GetComponent<BaseProjectile>().primaryMagazine.definition.builtInSize,
                        givepreload = weapon.GetComponent<ItemModEntity>().entityPrefab.Get().GetComponent<BaseProjectile>().primaryMagazine.contents,
                        giveammotype = weapon.GetComponent<ItemModEntity>().entityPrefab.Get().GetComponent<BaseProjectile>().primaryMagazine.ammoType.shortname,
                        givemaxcondition = weapon.condition.max
                    };
                    configData.Weapons.Add(weapon.shortname, weaponStats);
                    counter++;
                }
                if (configData.Debug) Puts($"Created initial weaponlist with '{counter}' projectile weapons.");
                SaveConfig(configData);
            }
            else
            {
                if (configData.Weapons.Count > 0)
                {
                    int countLoadedServerStats = 0;
                    foreach (ItemDefinition weapon in weapons)
                    {
                        if (!GameManifest.guidToPath.ContainsKey(weapon.GetComponent<ItemModEntity>().entityPrefab.guid) || weapon.GetComponent<ItemModEntity>().entityPrefab.Get().GetComponent<BaseProjectile>() == null) continue;
                        if (configData.Weapons.ContainsKey(weapon.shortname))
                        {
                            if (configData.Debug) Puts($"Processing existing weapon {weapon.shortname}");
                            WeaponStats serverDefaults = configData.Weapons[weapon.shortname];
                            if (serverDefaults.givemaxammo == 0)
                            {
                                serverDefaults.givemaxammo = serverDefaults.servermaxammo;
                                serverDefaults.givepreload = serverDefaults.serverpreload;
                                serverDefaults.giveammotype = serverDefaults.serverammotype;
                                serverDefaults.givemaxcondition = serverDefaults.servermaxcondition;
                                serverDefaults.skinid = 0;
                            }

                            if (serverDefaults.serveractive)
                            {
                                ItemDefinition weaponDef = ItemManager.FindItemDefinition(weapon.shortname);
                                weaponDef.GetComponent<ItemModEntity>().entityPrefab.Get().GetComponent<BaseProjectile>().primaryMagazine.definition.builtInSize = serverDefaults.servermaxammo;
                                weaponDef.GetComponent<ItemModEntity>().entityPrefab.Get().GetComponent<BaseProjectile>().primaryMagazine.contents = serverDefaults.serverpreload;
                                ItemDefinition ammo = ItemManager.FindItemDefinition(serverDefaults.serverammotype);
                                if (ammo != null)
                                    weaponDef.GetComponent<ItemModEntity>().entityPrefab.Get().GetComponent<BaseProjectile>().primaryMagazine.ammoType = ammo;
                                weaponDef.condition.max = Convert.ToSingle(serverDefaults.servermaxcondition);
                                countLoadedServerStats++;
                            }
                            continue;
                        }
                        WeaponStats weaponStats = new WeaponStats()
                        {
                            displayname = weapon.displayName.english,
                            maxammo = weapon.GetComponent<ItemModEntity>().entityPrefab.Get().GetComponent<BaseProjectile>().primaryMagazine.definition.builtInSize,
                            preload = weapon.GetComponent<ItemModEntity>().entityPrefab.Get().GetComponent<BaseProjectile>().primaryMagazine.contents,
                            maxcondition = weapon.condition.max,
                            ammotype = weapon.GetComponent<ItemModEntity>().entityPrefab.Get().GetComponent<BaseProjectile>().primaryMagazine.ammoType.shortname,
                            skinid = 0,
                            settingactive = true,
                            servermaxammo = weapon.GetComponent<ItemModEntity>().entityPrefab.Get().GetComponent<BaseProjectile>().primaryMagazine.definition.builtInSize,
                            serverpreload = weapon.GetComponent<ItemModEntity>().entityPrefab.Get().GetComponent<BaseProjectile>().primaryMagazine.contents,
                            serverammotype = weapon.GetComponent<ItemModEntity>().entityPrefab.Get().GetComponent<BaseProjectile>().primaryMagazine.ammoType.shortname,
                            servermaxcondition = weapon.condition.max,
                            serveractive = false,
                            givemaxammo = weapon.GetComponent<ItemModEntity>().entityPrefab.Get().GetComponent<BaseProjectile>().primaryMagazine.definition.builtInSize,
                            givepreload = weapon.GetComponent<ItemModEntity>().entityPrefab.Get().GetComponent<BaseProjectile>().primaryMagazine.contents,
                            giveammotype = weapon.GetComponent<ItemModEntity>().entityPrefab.Get().GetComponent<BaseProjectile>().primaryMagazine.ammoType.shortname,
                            givemaxcondition = weapon.condition.max
                        };
                        configData.Weapons.Add(weapon.shortname, weaponStats);
                        if (configData.Debug) Puts($"Added NEW weapon '{weapon.displayName.english} ({weapon.shortname})' to weapons list");
                    }
                    if (countLoadedServerStats > 0 && configData.Debug)
                    {
                        Puts($"Changed server default values for '{countLoadedServerStats}' weapons");
                    }
                    SaveConfig(configData);
                }
            }
        }

        [ConsoleCommand("mb.giveplayer")]
        private void BoostGive(ConsoleSystem.Arg arg)
        {
            if (arg.Connection?.authLevel < 2) return;
            if (arg.Args == null || arg.Args.Length < 2)
            {
                SendReply(arg, "Usage: magazinboost.give playername|id weaponshortname (optional: skinid)");
                return;
            }

            ulong skinid = 0;
            if (arg.Args.Length > 2)
            {
                if (!ulong.TryParse(arg.Args[2], out skinid))
                {
                    SendReply(arg, "Skin has to be a number");
                    return;
                }
                if (arg.Args[2].Length != 9)
                {
                    SendReply(arg, "Skin has to be a 9-digit number");
                    return;
                }
            }

            BasePlayer target = BasePlayer.Find(arg.Args[0]);
            if (target == null)
            {
                SendReply(arg, $"Player '{arg.Args[0]}' not found");
                return;
            }

            WeaponStats weaponStats = null;
            WeaponStats checkStats;
            if (configData.Weapons.TryGetValue(arg.Args[1], out checkStats))
            {
                weaponStats = checkStats;// as Dictionary <string, object>;
            }
            else
            {
                SendReply(arg, "Weapon '{arg.Args[0]}' not included/supported");
                return;
            }

            Item item = ItemManager.Create(ItemManager.FindItemDefinition(arg.Args[1]), 1, skinid);
            if (item == null)
            {
                SendReply(arg, "Weapon not created for unknown reason");
                return;
            }

            BaseProjectile bp = item.GetHeldEntity() as BaseProjectile;
            if (bp != null)
            {
                bp.primaryMagazine.capacity = weaponStats.givemaxammo;
                bp.primaryMagazine.contents = weaponStats.givepreload;
                ItemDefinition ammo = ItemManager.FindItemDefinition(weaponStats.giveammotype);
                if (ammo != null) bp.primaryMagazine.ammoType = ammo;
                item._maxCondition = Convert.ToSingle(weaponStats.givemaxcondition);
                item._condition = Convert.ToSingle(weaponStats.givemaxcondition);

                if (skinid == 0 && Convert.ToUInt64(weaponStats.skinid) > 0)
                    skinid = Convert.ToUInt64(weaponStats.skinid);

                if (skinid > 0)
                {
                    item.skin = Convert.ToUInt64(weaponStats.skinid);
                    item.GetHeldEntity().skinID = Convert.ToUInt64(weaponStats.skinid);
                }
                target.GiveItem(item);
                SendReply(arg, $"Weapon '{arg.Args[1]}' given to Player '{target.displayName}'");
            }
        }
        #endregion

        #region Helpers
        private bool HasAnyRight(BasePlayer player)
        {
            if (permission.UserHasPermission(player.UserIDString, configData.Permissions.permissionAll)) return true;
            if (permission.UserHasPermission(player.UserIDString, configData.Permissions.permissionMaxAmmo)) return true;
            if (permission.UserHasPermission(player.UserIDString, configData.Permissions.permissionPreLoad)) return true;
            if (permission.UserHasPermission(player.UserIDString, configData.Permissions.permissionMaxCondition)) return true;
            if (permission.UserHasPermission(player.UserIDString, configData.Permissions.permissionAmmoType)) return true;
            return false;
        }

        private bool HasRight(BasePlayer player, string perm)
        {
            bool right = false;
            switch (perm)
            {
                case "all":
                    if (permission.UserHasPermission(player.UserIDString, configData.Permissions.permissionAll)) { right = true; }
                    break;
                case "maxammo":
                    if (permission.UserHasPermission(player.UserIDString, configData.Permissions.permissionMaxAmmo)) { right = true; }
                    break;
                case "preload":
                    if (permission.UserHasPermission(player.UserIDString, configData.Permissions.permissionPreLoad)) {right = true;}
                    break;
                case "maxcondition":
                    if (permission.UserHasPermission(player.UserIDString, configData.Permissions.permissionMaxCondition)) { right = true; }
                    break;
                case "ammotype":
                    if (permission.UserHasPermission(player.UserIDString, configData.Permissions.permissionAmmoType)) { right = true; }
                    break;
            }
            return right;
        }
        //static List<string> GetPlayerAmmo(BasePlayer player, string type=null)
        private bool GetPlayerAmmo(BasePlayer player, string type=null)
        {
            foreach (Item item in player.inventory.containerMain.itemList)
            {
                if (item?.info.shortname.Contains(type) == true)
                {
                    if (configData.Debug) Puts($"Name: {item.info.shortname}, Amount: {item.amount}");
                    return true;
                    //int amt = item.amount();
                }
            }
            return false;
        }
        #endregion
    }
}
