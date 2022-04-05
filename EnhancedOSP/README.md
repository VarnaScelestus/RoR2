# Enhanced OSP (One Shot Protection)

A simple mod that allows you to:

- Set the invincibility window granted by OSP. (default setting is 0.5s, Vanilla is 0.1s)
- Set the threshold of missing HP required before a player loses OSP. (default is 0.1, or 10% missing HP, same as Vanilla)
- Set whether sources of maxHP reduction such as Shaped Glass negatively impact OSP. (default is false, Vanilla is true)
- Set whether sources of Shield such as Personal Shield Generator or Overloading Elite affix negatively impact OSP. Note: Trancendence and Perfected Elite Affix behavior unaffected to avoid godmode issues. (default is false, Vanilla is true)
- Set whether or not sources of MaxHP reduction (Shaped Glass, etc) are represented on the HUD via a pointless 'glass' effect that takes up space and makes the bar harder to read during gameplay. (default is false, Vanilla is true)

You can reach me (Varna) in the [RoR2 modding discord](https://discord.gg/5MbXZvd) with any feedback!

## Credits

[ThinkInvis](https://github.com/ThinkInvis/RoR2-TinkersSatchel) - For the location of the curse fraction setting + code I based my settings on.

[RoR2 modding discord](https://discord.gg/5MbXZvd) - Because IL is never without massive headaches.

## Changelog

1.3.0 - Fixed the HUD not updating the OSP fraction properly with the new settings, and added a setting to disable maxHP reduction's vanilla HUD "improvements".

1.2.0 - Added setting to control whether Shield affects OSP.

1.1.0 - Added two settings - one to control OSP threshold, and one to control whether maxHP reduction affects OSP (default false).

1.0.1 - Updated to work with SotV expansion. Removed R2API dependency.

1.0.0 - Initial release