
# 🐝 Karen

Windows Bootstrapper for LANraragi using MSYS2.  
Works on Windows 10 1903 and up. 
**64-bit OSes only!**  

* Start/Stop LRR from a Windows-based UI
* Show/hide log console on a click
* Options to set content folder and listen port

![scr](./screenshot.jpg)

## Building

Karen can be built as a normal csproj app, but requires a [LANraragi MSYS2 VFS](https://github.com/Difegue/LANraragi/blob/dev/tools/build/windows/create-dist.sh) added to its build output to really do anything.  
The VFS needs to be added to a `lanraragi` subfolder, ie: `Karen/bin/win-x64/publish/lanraragi`.

The MSI (Setup wixproj) can just be built as-is once this is done. 
