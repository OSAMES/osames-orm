using OsamesMicroOrm;

namespace TestOsamesMicroOrm.TestDbEntities
{
    internal class TestUnmappedEntity : DatabaseEntityObject
    {
        public string Id { get; set; }
        public override void Copy<T>(T object_)
        {
            throw new System.NotImplementedException();
        }
    }
}
