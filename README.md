# Opaquectrl

This is a extension of visual studio 2022,it prevents the intellisense popover from becoming transparent while holding down the control key.

## Why this extension was developed

If you want bind the ctrl key to navigate intellisense,when you holding down the 
ctrl key,intellisense becomes transparent,this is so annoying.

You can see the problem here   
[Allow disabling transparency on hold Ctrl](https://developercommunity.visualstudio.com/t/cannot-disable-transparency-on-hold-ctrl/1466213)

## How it works

I hooked the Setter of the property `VisualOpacity` of the class `UIElement`, anyone trying to set the transparency of intellisense is filtered out.
This is tricky and crude, but it works.

## How to use

Just build and install it

## Thanks for

  [Harmony](https://github.com/pardeike/Harmony)
