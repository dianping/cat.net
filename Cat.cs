using System.Linq;

namespace Com.Dianping.Cat
{
    using Configuration;
    using Message;
    using Message.Internals;
    using Message.Spi;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;

    public class Cat
    {
        private static readonly Cat Instance = new Cat();

        private bool _mInitialized;

        private IMessageManager _mManager;

        private IMessageProducer _mProducer;

        private Cat()
        {
        }

        public static IMessageManager GetManager()
        {
            return Instance._mManager;
        }

        public static IMessageProducer GetProducer()
        {
            return Instance._mProducer;
        }

        public static void Initialize(string configFile)
        {
            if (Instance._mInitialized)
            {
                Logger.Warn("Cat can't initialize again with config file(%s), IGNORED!", configFile);
                return;
            }

            Logger.Info("Initializing Cat .Net Client ...");

            DefaultMessageManager manager = new DefaultMessageManager();
            ClientConfig clientConfig = LoadClientConfig(configFile);

            manager.InitializeClient(clientConfig);
            Instance._mProducer = new DefaultMessageProducer(manager);
            Instance._mManager = manager;
            Instance._mInitialized = true;

            Logger.Info("Cat .Net Client initialized.");
        }

        public static bool IsInitialized()
        {
            return Instance._mInitialized;
        }

        private static ClientConfig LoadClientConfig(string configFile)
        {
            ClientConfig config = new ClientConfig();

            if (File.Exists(configFile))
            {
                Logger.Info("Use config file({0}).", configFile);

                XmlDocument doc = new XmlDocument();

                doc.Load(configFile);

                XmlElement root = doc.DocumentElement;

                if (root != null)
                {
                    config.Domain = BuildDomain(root.GetElementsByTagName("domain"));

                    IEnumerable<Server> servers = BuildServers(root.GetElementsByTagName("servers"));

                    //NOTE: 只添加Enabled的
                    foreach (Server server in servers.Where(server => server.Enabled))
                    {
                        config.Servers.Add(server);
                        Logger.Info("CAT server configured: {0}:{1}", server.Ip, server.Port);
                    }
                }
            }
            else
            {
                Logger.Warn("Config file({0}) not found, using localhost:2280 instead.", configFile);

                config.Domain = BuildDomain(null);
                config.Servers.Add(new Server("localhost", 2280));
            }

            return config;
        }

        private static Domain BuildDomain(XmlNodeList nodes)
        {
            if (nodes == null || nodes.Count == 0)
            {
                return new Domain();
            }

            XmlElement node = (XmlElement) nodes[0];
            return new Domain
                       {
                           Id = GetStringProperty(node, "id", "Unknown"),
                           //Ip = GetStringProperty(node, "ip", null),
                           Enabled = GetBooleanProperty(node, "enabled", true)
                       };
        }

        private static IEnumerable<Server> BuildServers(XmlNodeList nodes)
        {
            List<Server> servers = new List<Server>();

            if (nodes != null && nodes.Count > 0)
            {
                XmlElement first = (XmlElement) nodes[0];
                XmlNodeList serverNodes = first.GetElementsByTagName("server");

                foreach (XmlNode node in serverNodes)
                {
                    XmlElement serverNode = (XmlElement) node;
                    string ip = GetStringProperty(serverNode, "ip", "localhost");
                    int port = GetIntProperty(serverNode, "port", 2280);
                    Server server = new Server(ip, port) {Enabled = GetBooleanProperty(serverNode, "enabled", true)};

                    servers.Add(server);
                }
            }

            if (servers.Count == 0)
            {
                Logger.Warn("No server configured, use localhost:2280 instead.");
                servers.Add(new Server("localhost", 2280));
            }

            return servers;
        }

        private static string GetStringProperty(XmlElement element, string name, string defaultValue)
        {
            if (element != null)
            {
                string value = element.GetAttribute(name);

                if (value.Length > 0)
                {
                    return value;
                }
            }

            return defaultValue;
        }

        private static bool GetBooleanProperty(XmlElement element, string name, bool defaultValue)
        {
            if (element != null)
            {
                string value = element.GetAttribute(name);

                if (value.Length > 0)
                {
                    return "true".Equals(value);
                }
            }

            return defaultValue;
        }

        private static int GetIntProperty(XmlElement element, string name, int defaultValue)
        {
            if (element != null)
            {
                string value = element.GetAttribute(name);

                if (value.Length > 0)
                {
                    int tmpRet;
                    if (int.TryParse(value, out tmpRet))
                        return tmpRet;
                }
            }

            return defaultValue;
        }
    }
}