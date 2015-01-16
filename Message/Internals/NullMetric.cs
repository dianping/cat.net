
namespace Com.Dianping.Cat.Message.Internals
{
    public class NullMetric : AbstractMessage, IMetric
    {
        public NullMetric()
            : base(null, null)
        {
        }
    }
}
