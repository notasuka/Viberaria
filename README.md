# Viberaria
A Buttplug.io Mod for Terraria/tModLoader

## Features
* Vibration events
  * Death vibration
  * Damage vibration (on hit)
  * Health scaling vibration (damage% of max health)
  * Health/Mana potion vibration
  * Debuff vibration
  * Fishing vibration
* Configuration for each feature:
  * Enabled / Disabled
  * Vibration strength
  * Vibration duration
  * Several event's pattern can be configured.
* Works in Multiplayer (No need to be installed on the server)

## Installation
1) Install tModLoader. It can be installed for free on [Steam](https://store.steampowered.com/app/1281930/tModLoader/) or their website (https://tmodloader.net/)
2) Download the [latest release](https://github.com/notasuka/Viberaria/releases/) of Viberaria.
3) Open the `Mods` directory at `Documents\My Games\Terraria\tModLoader\Mods`.
4) Insert the `Viberaria.tmod` file into the `Mods` directory.
5) Launch `Intiface Central` and start the Intiface server (the big purple button).
6) Launch `tModLoader`.
7) Enable the mod and/or configure it (see the [Configuration](#configuration) section)
   1) Configure the Intiface IP address in the mod configuration.
8) Join a single- or multiplayer world 

## Configuration
Configuring the mod is fairly simple thanks to tModLoader. You can configure mods at any time in the main menu or in-game.  
In the Main Menu, go to:
- `Workshop -> Manage Mods -> Viberaria (cogwheel icon) (if the mod is enabled) -> Viberaria Config` or
- `Workshop -> Manage Mods -> Mod configuration (at the bottom) -> Viberaria -> Viberaria Config`  

In-game, go to:
- `Escape (menu) -> Mod Configuration -> Viberaria -> Viberaria Config`

## Troubleshooting
- I'm getting chat messages saying it can't connect to Intiface.
  - Ensure the Intiface Central app is open and that you clicked the 'Start Server' button (the big purple one).
  - Check if the IP address is correct
    - Go to the [mod config](#configuration).
    - Look for `Intiface Address`. It will be followed by the IP it uses to find the Intiface Server.
      - It should look like `Intiface Address: localhost:12345` or something like `Intiface Address: 192.168.x.x:12345`.
    - Click it to modify it. A button at the top will allow you to switch between `localhost` and a custom IP address.
      - Replace the array of numbers with the numbers of the IP address of the intiface server. To find what IP you should fill in, look at the following `troubleshooting` question.
    - Check if the Intiface Address now shows up as it should.
- Where can I find the Intiface Address?
  - After installing the Intiface Server on your device and opening it, you will find a large purple `Start Server` button at the top. This button is the main switch to turn on the server that connects your toys to your game. Start the server by clicking this `Start Server`.
  - After it starts, the `Status` will change from "Engine not running" to "Engine running, waiting for client". Below the status, you can find `Server Address`. It should be `ws://localhost:12345` or look something like `ws://x.x.x.x:12345`, where `x` is a number between 0 and 255. You can ignore `ws://` (`ws` stands for WebSocket).
  - This is the IP address you will need in the Viberaria [mod config](#configuration). If you're running the Intiface Central server on the same device as the one you're playing Terraria with, the address is usually `localhost`. If you're not running it on the same device but it still says `localhost`, look at the last step to find your device's IP address manually.
  - Your device's local IP address typically looks something like `192.168.x.x`. If it doesn't, you can still try the IP in the Viberaria config.
  - Otherwise, look up online how you can find your IP address for your device. It usually looks something like `192.168.x.x`.
    - On Android, you can go to your internet settings, click the cogwheel for advanced internet settings, and click `Show more`.
    - On Windows, open the Command Prompt (`cmd`) / Terminal or Windows Powershell, and run the command `ipconfig`. It shows a bunch of stuff, but you're looking for the `IPv4 address`.
    - On macOS, open the Terminal and run the command `ifconfig`, probably.
- Intiface Central crashes when it starts up or has errors in the `Log` tab.
  - Ensure your device's Bluetooth is on. If your device (PC, for example) doesn't have Bluetooth, you can still run this app on your phone (or another device with Bluetooth) and change the IP address in the Viberaria [mod config](#configuration) to match the IP address.
- My toy isn't vibrating when I think it should.
  - Check if the toy connects when you join the world. It should show in chat.
    - Ensure the mod is enabled in your mod list. If it is, `Viberaria` should be visible in the `Mod Configuration` of tModLoader.
    - Ensure `Viberaria Enabled` is enabled in the `Viberaria` mod configuration menu.
    - Ensure your device's bluetooth is on.
  - Check if the feature you want it to vibrate for is actually enabled. To get Damage vibrations, you must make sure `Damage Vibration Enabled` is turned on in the [mod config](#configuration).
  - Check if the toy is supposed to vibrate.
    - Go to the [mod config](#configuration), scroll to the bottom, and open the Debug subpage.
    - Click `Enabled` to turn on Debug mode. Make sure `Toy Strength Messages` is turned on.
    - Join a world, and trigger a vibration event (getting hurt, poisoned, dying, drinking potions, etc.). It should show in chat how strong the toy is supposed to vibrate.
  - If there is no vibration, but you know there should be, you can make a bug report on this GitHub following the [contributing](#contributing) steps.

## Contributing
Make a [bug report](https://github.com/notasuka/Viberaria/issues/new), or fork the repo and make a pull request after making the desired changes.