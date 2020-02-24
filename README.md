## MagazinBoost (Remod version)

Current version 1.6.3: [Download](https://code.remod.org/MagazinBoost.cs)

Magazin Booster can change the most important values for any projectile Weapon. It works with custom changeable rights for the available properties and will get applied after "OnItemCraftFinished" or "OnReloadWeapon."

Additionally it can change the default value's for maximum ammunition count and preloaded ammunition count for every weapon, which will be spawned on the server, either in boxes, by kits or also by give.

All by permission boosted weapons can only be used by players with the needed rights. The plugin checks (by default TRUE, can be changed) if a weapon has boosted values and will revert them to the server default, once a player with no appropriate rights has such a weapon in his belt. As of the ability, that those weapons can be marked with a custom skin (limited by the available skins), those skins will be removed on the same way like the properties. This can be changed by global setting.

This strategy will keep up your donations and prevents massproducing of such weapons.

Actually included weapons are:

- Every projectile weapon
- Every bow weapon

The plugin can change the following properties:

- Maximum ammunition per magazine
- Preloaded ammunition after being crafted
- Maximum condition
- Preloaded ammotype after being crafted
- Default skin for boosted weapons

### Permissions

These default permissions will be created on first launch, and can be changed by any name to your personal need before granting those rights to any groups:

- `magazinboost.canall` - All 
- `magazinboost.canmaxammo` - MaxAmmo
- `magazinboost.canpreload` - PreLoad
- `magazinboost.canmaxcondition` - MaxCondition 
- `magazinboost.canammotype` - AmmoType

The player will get only those settings applied to which type of permission he was granted for. With this you've got 4(5) stages to choose between in your donators/VIP chargements

### Configuration

The plugin will create a config-file 'MagazinBoost.json' in 'config' at first start:

```json
{
  "CheckRights": {
    "checkForRightsInBelt": true,
    "removeSkinIfNoRights": true
  },
  "Permissions": {
    "permissionAll": "magazinboost.canall",
    "permissionAmmoType": "magazinboost.canammotype",
    "permissionMaxAmmo": "magazinboost.canmaxammo",
    "permissionMaxCondition": "magazinboost.canmaxcondition",
    "permissionPreLoad": "magazinboost.canpreload"
  }
}
```

- `checkForRightsInBelt` - checks for pluginrights and will revert a custom property if the player has'nt this right
- `removeSkinIfNoRights` - removes any custom skin if it's not the server default skin or in case of the current is equal the setting of the specific weapon

The plugin will also create the default weapon stats/settings in the config file.

These settings of each weapon are NOT changeable and must not be changed:

- name
- displayname

These settings of each weapon are changeable and should be changed for any profit:

- `maxammo` - The maximum amount the magazine can load
- `preload` - The preloaded amount of ammo after being crafted
- `maxcondition` - The weapons maximum condition
- `ammotype` - The default preloaded ammo type
- `skinid` - The custom skin for all modded weapons by type
- `servermaxammo` - equal to maxammo, but a global setting for server
- `serverpreload` - equal to preload, but a global setting for server
- `serverammotype` - equal to ammotype, but a global setting for server
- `servermaxcondition` - equal to maxcondition, but a global setting for server
- `serveractive` - use yes/no (true/false) global settings for this weapon

Also available is a consolecommand to give changed weapons to players:

- mb.giveplayerUsage: mb.giveplayer playername|id weaponshortname (optional: skinid)

The options for each weapon given by this command are saved in:

- givemaxammo
- givepreload
- giveammotype
- givemaxcondition
- giveskinid

Possible ammo types are:

- "ammo.handmade.shell"
- "ammo.pistol"
- "ammo.pistol.fire"
- "ammo.pistol.hv"
- "ammo.rifle"
- "ammo.rifle.explosive"
- "ammo.rifle.incendiary"
- "ammo.rifle.hv"
- "ammo.rocket.basic"
- "ammo.rocket.fire"
- "ammo.rocket.hv"
- "ammo.rocket.smoke"
- "ammo.shotgun"
- "ammo.shotgun.slug"
- "arrow.hv"
- "arrow.wooden"

