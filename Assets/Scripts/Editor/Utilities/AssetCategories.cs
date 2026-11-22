// AssetCategories: decides which sub-folder each generated asset goes into, so Assets/Generated stays
// organised (e.g. Prefabs/Doors, Materials/Glowing, Textures/Glyphs, Audio/Loops). Every generator
// builds its save/load paths through these functions, so moving an asset = editing one line here.
namespace LastTorch.EditorTools
{
    public static class AssetCategories
    {
        public static string Prefab(string name)
        {
            switch (name)
            {
                case "Player": return "Characters";
                case "Wall": case "Floor": return "Level";
                case "OilJar": case "Gem": case "Key_Sun": case "Key_Moon": case "Key_Serpent": case "HeartAltar": return "Collectibles";
                case "KeyDoor": case "SanctumDoor": case "EntranceDoor": case "ExitGate": return "Doors";
                case "SpikeTile": case "DartPlate": case "DartLauncher": case "Dart": case "Boulder": return "Traps";
                case "PickupBurst": case "DustBurst": case "WarningCircle": case "Debris": return "Effects";
                default: return "Decoration"; // Vine, GlyphPanel, Pillar, Rubble
            }
        }

        public static string Material(string name)
        {
            if (name.StartsWith("Particle_")) return "Particles";
            if (name.StartsWith("Explorer_")) return "Explorer";
            if (name.StartsWith("Key_") || name.StartsWith("Glyph_")) return "Glowing";
            switch (name)
            {
                case "Gem": case "Heart": case "Daylight": case "Socket_On": return "Glowing";
                case "Clay": case "Wood": case "Metal": case "Socket_Off": return "Props";
                default: return "Environment"; // Stone_*, Rock, Sand, Hole, Moss
            }
        }

        public static string Texture(string name)
        {
            if (name.StartsWith("glyph_")) return "Glyphs";
            if (name.StartsWith("ui_")) return "UI";
            if (name == "soft_particle") return "Particles";
            return "Environment"; // stone, moss, clay, wood
        }

        public static string Mesh(string name)
        {
            switch (name)
            {
                case "WallBlock": case "FloorTile": case "DoorSlab": case "Plate": case "LauncherBlock": case "PillarBase":
                case "Tablet": case "Temple_Walls": case "Temple_Floor": case "MenuWall": case "MenuFloor": case "Quad":
                    return "Level";
                default: return "Props"; // gem, crystal, rocks, jar, dart, altar, spike...
            }
        }

        public static string Audio(string name)
        {
            switch (name)
            {
                case "torch_loop": case "ambient_drone": case "boulder_roll": case "heart_hum": case "collapse_rumble": return "Loops";
                case "ui_click": case "ui_hover": return "UI";
                default: return "SFX";
            }
        }
    }
}
