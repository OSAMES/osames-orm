using OsamesMicroOrm;

namespace TestOsamesMicroOrm.TestDbEntities
{
    [DatabaseMapping("")]
    internal class TestEmptyMappingEntity : DatabaseEntityObject
    {
        public override void Copy<T>(T object_)
        {
            throw new System.NotImplementedException();
        }
    }
}
