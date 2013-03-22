namespace Com.Dianping.Cat.Message.Spi
{
    using Message;
    using System;

    public interface IMessageTree
    {
        String Domain { get; set; }


        String HostName { get; set; }


        String IpAddress { get; set; }


        IMessage Message { get; set; }


        String MessageId { get; set; }


        //String ParentMessageId { get; set; }


        //String RootMessageId { get; set; }


        //String SessionToken { get; set; }


        String ThreadGroupName { get; set; }


        String ThreadId { get; set; }


        String ThreadName { get; set; }
        IMessageTree Copy();
    }
}