# NikkeModManager

NikkeModManager is a tool built with Windows Forms and [Monogame](https://www.monogame.net/) for managing mods for the game [Goddess of Victory: Nikke](https://nikke-en.com/). It can read in, preview, and patch in asset bundles on demand for a strightforward modding experience. 

The mod manager is currently in beta, expect bugs and crashes. It is highly recommended to backup your /Nikke/eb folder before running the application as a way to restore base game data without having to redownload it.

Currently only supports Windows PCs, support may be extended to Android in the future.

## Features

* Automatically reads in compatible Asset Bundles files and displays them sorted by Nikke, Skin, and Pose
* Previews selected bundle using an embedded Monogame window
* Choose exactly which bundle files to install from all available mods
* Swap back to default Nikke bundles at any time
* One button patching process, including renaming mod files

## Requirements

[.NET Desktop Runtime 6.0](https://dotnet.microsoft.com/download/dotnet/6.0)

## Usage

1. **Backup your \NIKKE\eb Folder**

Either by zipping it up in place or copying the directory elsewhere, if anything unexpected happens and you need to restore the base game asset bundles this will save you from having to redownload them.

If you currently have mods installed it's recommended to remove them before running the application, otherwise the manager will treat those mods as base game files (which is fine if you never want to uninstall those mods).

2. Download the latest release from [GitHub](https://github.com/) (insert link here)
3. Move downloaded NikkeModManager.zip to desired location and unzip
4. Move all mods into the "mods" folder. A mod is any zip file or normal folder containing asset bundles (files named like 7d980edbbdaf245024fe1bbfa3ea23ec). Asset bundles directly in the mods folder will not be loaded, they must be contained in a folder or zip file for organizational purposes.
5. Run NikkeModManager.exe. The first time the application is run will take a few minutes as it backs up existing nikke files, caches mod data, and builds a filename map.
6. Double click a character in the tree on the left to see all available bundle files organized by skin and pose, or hit the Expand All button to see all characters. Click a bundle file to see it previewed in the right pane.
7. Double click a bundle file to mark it for installation. Dark green files are currently installed, light green files will be installed with the next patch, and pink files either are not installed or will be uninstalled with the next patch.
8. Click the Patch button to begin the patching process. A dialog will open confirming the number of mods to patch, and once the patching process has completed another box will open stating the number of successfully patched files.

### Notes

* The manager will not load asset bundles that are not bundled for Windows
* If a mod file fails to patch a box should open telling you why
* Patching while the game is currently running is unsupported. You can try to do it anyway but there's no guarantee it will work.
* All errors or warning should be logged to the console that opens alongside the game, if anything behaves unexpectedly try looking there and see if an error was thrown.

## FAQ

### A Nikke is missing or has a misspelled name

Check the \_nikke_data.csv file, if the nikke's name doesn't show up or has an x by its name then I missed it or didn't realize it was a character. If you know the characters id you can fix it yourself or post an issue here and it will be fixed in the next release.

### The mod manager didn't load one of my mod files

The primary reasons why an asset bundle is skipped is because
1. The asset bundle wasn't built for Windows
2. The asset bundle is missing either the atlas file, texture file, or skeleton file
3. The bundle's associated character id is marked to be skipped in \_nikke_data.csv

The console should state the exact reason why any asset bundles are skipped

### Some of the Nikkes look like creatures from my nightmares / Some Nikkes look weird

I am currently unsure why this happens, it's most likely caused by some incompatibility with the skin and the spine renderer that monogame uses. 
Hopefully it will be fixed in the near future.

### The application is conistently crashing / not working

Post an issue to this repo explaining what's going wrong, what you're doing when it goes wrong, and attach the log.txt file. Or message me on Discord.

## Mod Authors

In order to make it easier to keep track of mods the mod manager will look for a manifest.json file somewhere inside the mod folder/zip file. 
It will use the information within the manifest to populate the fields in the right panel when previewing a bundle file.
A template manifest.json is below.

```
{
	"Author": "author username",
	"Link": "link to authors patron/discord/whatever",
	"GameVersion": "107.6.6",
	"ModVersion": "1.0",
	"Data":{
		"CustomField": "CustomValue"
	}
}
```

A manifest file is not required, but can make it easier for users to keep track of where their mods came from.

## Credits

Thanks to Perfare's [Asset Studio](https://github.com/Perfare/AssetStudio) and FZFalzar's [Nikke Tools](https://github.com/FZFalzar/NikkeTools) which make decrytping and parsing Nikke asset bundles possible.

