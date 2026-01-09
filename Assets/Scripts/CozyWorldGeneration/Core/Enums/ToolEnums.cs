namespace CozyWorldGeneration.Core.Enums
{
    // Refactor TileType to be configuration and not 
    public enum TileType
    {
        Empty,
        Corner,
        Edge,
        InnerCorner,
        Diagonal,
        Fill
    }

    // This is mostly for grass but we'll see
    public enum TileState
    {
        Normal,
        Dug,
        Tilled,
        Ramp,
        Stairs,
        Waterlogged
    }

    public enum PaintMode
    {
        Terrain,
        Fluid
    }
}