using OsamesMicroOrm;

namespace TestOsamesMicroOrm.TestDbEntities
{
    [DatabaseMapping("wrong mapping")]
    internal class TestWrongMappingEntity : DatabaseEntityObject
    {
        public override void Copy<T>(T object_)
        {
            throw new System.NotImplementedException();
        }
    }
}
