# PRINT INTO BLACK

Replaces the black ink in a black-and-white logo with a second image, called the print. White stays white. The saved PNG is the logo's exact pixel size. The print is scaled to cover that size without stretching, then centered and cropped.

Windows only. 64-bit. No install. The download is one self-contained exe.

## Run

Double-click PrintIntoBlack.exe.

1. Choose the black-and-white logo, or drop it on the first box.
2. Choose the print, or drop it on the second box.
3. When SAVE PNG turns white, click it and save the PNG.

## Build

Needs the .NET 8 SDK. From this folder:

```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true
```

The program is `PrintIntoBlack.exe`.
