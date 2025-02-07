using System;

[Flags]
public enum SelectionLayers
{
    Terrain = 1 << 0,
    Selector = 1 << 1,
    Units = 1 << 2,
    Buildings = 1 << 3,
}
