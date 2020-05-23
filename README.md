# NitroMdlConv
A command line tool to convert extracted Nitro+ Sonicomi character models into Collada collection files, compatible with Blender.

Usage
-----
- Open command console and change directory to folder where you want to output the converted files
- Type full path of the executable or drop NitroMdlConv.exe onto console
- First add filename of Sonico`s base model (sonico_base.mdl) or drop it onto console
- Add additional body/clothing models
- Confirm with enter to start the converting process
- The console should read "File successfully converted" when done
- Load .dae files into Blender via import from Collada
- Load and link textures with the model (The converted model comes pre-mapped with the original textures)

Tested Jul'19 on Windows 10, German region settings and Blender 2.79
https://www.youtube.com/watch?v=vQdnsIkc8NA

> The tool cannot process raw (compressed and encrypted) game files and the repository does not include samples.
> Do not ask for any asset uploads. To extract models, use NIPA extractor (https://github.com/Wilhansen/nipa) on your own game installation.
