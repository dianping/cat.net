namespace Com.Dianping.Cat.Configuration
{
    /// <summary>
    ///   描述当前系统的情况
    /// </summary>
    public class Domain
    {
        private bool _mEnabled = true;

        /// <summary>
        ///   当前系统的标识
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///   当前系统的IP
        /// </summary>
        public string Ip { get; set; }

        /// <summary>
        ///   Cat日志是否开启，默认开启
        /// </summary>
        public bool Enabled
        {
            get { return _mEnabled; }
            set { _mEnabled = value; }
        }
    }
}