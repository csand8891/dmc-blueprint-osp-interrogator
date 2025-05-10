namespace DmcBlueprint.Models
{
    public class RevisionEntry
    {
        public string? Identifier { get; set; }
        public DateTime SoftwareProductionDate { get; set; }
        public string? SalesOrderNumber { get; set; }
        public string? ProjectNumber { get; set; }
        public string? FullCommentLine { get; set; }
    }
}