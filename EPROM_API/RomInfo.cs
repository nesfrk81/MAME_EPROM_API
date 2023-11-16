namespace EPROM_API
{
    public class RomInfo
    {
        public string? GameName { get; set; }
        public string? GameYear { get; set; }
        public string? MameSetName { get; set; }

        public List<Rom> Roms { get; set; } = new List<Rom>();
    }

    public class Rom
    {
        public string? RomName { get; set; }
        public string? RomType { get; set; }
        public string? RomRegion { get; set; }
        public string? RomSize { get; set; }
    }
}