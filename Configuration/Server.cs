namespace Com.Dianping.Cat.Configuration
{
    /// <summary>
    ///   描述记录当前系统日志的目标Cat服务器
    /// </summary>
    public class Server
    {
        private readonly string _mIp;

        private readonly int _mPort;

        public Server(string ip, int port)
        {
            _mIp = ip;
            _mPort = port;
            Enabled = true;
        }

        /// <summary>
        ///   Cat服务器IP
        /// </summary>
        public string Ip
        {
            get { return _mIp; }
        }

        /// <summary>
        ///   Cat服务器端口
        /// </summary>
        public int Port
        {
            get { return _mPort; }
        }

        /// <summary>
        ///   Cat服务器是否有效，默认有效
        /// </summary>
        public bool Enabled { get; set; }
    }
}