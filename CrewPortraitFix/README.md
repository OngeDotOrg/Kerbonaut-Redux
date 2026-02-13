# Crew Portrait Fix

A tiny KSP mod that fixes the crew portrait camera position. (Custom mesh clip problems)

## What It Does

- Adjusts the camera angle/position for kerbal portraits

## Config Options

Add a `CREW_PORTRAIT_CAMERA` node to your config:

```
CREW_PORTRAIT_CAMERA
{
    cameraOffset = 0, 0.2, 0.2
    cameraFOV = 105.69
    nearClipPlane = 0.01
    cameraDistance = 1.14
    showOverlayKerbals = true
}
```

## Building

```bash
dotnet build src/CrewPortraitFix.csproj
```

Or just open in your IDE and hit build.

## Part of Kerbonaut Redux

This is a companion mod to Kerbonaut Redux but works standalone too.

## License

Free KSP mod by Onge.org. See LICENSE file.
