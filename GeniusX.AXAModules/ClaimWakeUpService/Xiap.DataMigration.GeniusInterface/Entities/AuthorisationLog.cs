namespace Xiap.DataMigration.GeniusInterface.AXACS.Entities
{
    using Metadata.Data.Enums;

    public class AuthorisationLog
    {
        public short AmountType { get; set; }
        public short AuthorisationResult { get; set; }
        public long ActionedByUserID { get; set; }
    }
}
