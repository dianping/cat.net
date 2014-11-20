using System;
using System.Diagnostics;
using System.Globalization;
using System.Management;
using System.Threading;
using System.Text;
using Com.Dianping.Cat.Util;

namespace Com.Dianping.Cat.Message.Spi.Internals
{
    public class StatusUpdateTask
    {
        private readonly IMessageStatistics _mStatistics;

        public StatusUpdateTask(IMessageStatistics mStatistics)
        {
            _mStatistics = mStatistics;
        }

        /// <summary>
        ///   获取系统内存大小
        /// </summary>
        /// <returns> 内存大小(单位M) </returns>
        private static int GetPhisicalMemory()
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(); //用于查询一些如系统信息的管理对象 
            searcher.Query = new SelectQuery("Win32_PhysicalMemory ", "", new[] { "Capacity" }); //设置查询条件 
            ManagementObjectCollection collection = searcher.Get(); //获取内存容量 
            ManagementObjectCollection.ManagementObjectEnumerator em = collection.GetEnumerator();

            long capacity = 0;
            while (em.MoveNext())
            {
                ManagementBaseObject baseObj = em.Current;
                if (baseObj.Properties["Capacity"].Value != null)
                {
                    try
                    {
                        capacity += long.Parse(baseObj.Properties["Capacity"].Value.ToString());
                    }
                    catch
                    {
                        return 0;
                    }
                }
            }
            return (int)(capacity / 1024 / 1024);
        }

        public void Run(object o)
        {
            while (true)
            {
                var sb = new StringBuilder();
                sb.AppendLine("StatusUpdateTask:");
                sb.AppendLine("Bytes=" + _mStatistics.Bytes);
                sb.AppendLine("Overflowed=" + _mStatistics.Overflowed);
                sb.AppendLine("Produced=" + _mStatistics.Produced);

                //机器的CPU内核数
                var processorCount = Environment.ProcessorCount;
                sb.AppendLine("机器的CPU内核数=" + processorCount);

                //获取当前进程
                Process currentProcess = Process.GetCurrentProcess();
                //var processId = CurrentProcess.Id;//PID
                //获取当前进程CPU利用率
                var tmpRate =
                    Math.Round(
                        currentProcess.UserProcessorTime.TotalMilliseconds * 100 /
                        currentProcess.TotalProcessorTime.TotalMilliseconds, 2);
                var cpuUsage = tmpRate + "%"; //CPU
                //获取当前进程内存占用
                var memoryUsage = (currentProcess.WorkingSet64 / 1024 / 1024).ToString(CultureInfo.InvariantCulture) + "M (" +
                                  (currentProcess.WorkingSet64 / 1024).ToString(CultureInfo.InvariantCulture) + "KB)";
                //占用内存
                //获取当前进程的线程数
                var threadCount = currentProcess.Threads.Count; //线程

                //获取当前进程的线程池大小
                int maxThread, maxThread2;
                ThreadPool.GetMaxThreads(out maxThread, out maxThread2); //线程
                int avThread, avThread2;
                ThreadPool.GetAvailableThreads(out avThread, out avThread2);
                int mThread, mThread2;
                ThreadPool.GetMinThreads(out mThread, out mThread2);

                var threadPoolInfo = string.Format("线程池大小={0}/{1}, 线程池可用线程数={2}/{3}, 线程池最小线程数={4}/{5}", maxThread2,
                                                   maxThread, avThread2, avThread, mThread2,
                                                   mThread);

                sb.AppendLine("当前进程CPU利用率=" + cpuUsage);
                sb.AppendLine("当前进程内存占用=" + memoryUsage);
                sb.AppendLine("当前进程的线程数=" + threadCount);
                sb.AppendLine(threadPoolInfo);

                var bestThreadInfo = string.Empty;
                //最佳线程数为
                var bestThreadCountTmp = currentProcess.TotalProcessorTime.TotalMilliseconds /
                                         (currentProcess.UserProcessorTime.TotalMilliseconds / threadCount);
                bestThreadInfo += "最佳线程数=" + bestThreadCountTmp + ",";

                //如果需要增加线程
                if (Math.Abs(bestThreadCountTmp - maxThread) > 1)
                {
                    if (tmpRate < 90 && bestThreadCountTmp > 400)
                    {
                        int bestThreadCount = (int)Math.Floor(bestThreadCountTmp);
                        //ThreadPool.SetMinThreads(bestThreadCount, bestThreadCount);
                        ThreadPool.SetMaxThreads(bestThreadCount, bestThreadCount);
                        bestThreadInfo += "重设线程池的MaxThreads=" + bestThreadCount + ",";
                    }
                }
                sb.AppendLine(bestThreadInfo);

                var phisicalMemory = GetPhisicalMemory();
                sb.AppendLine("MemoryCapacity=" + phisicalMemory + "M");

                PerformanceCounter ramCounter = new PerformanceCounter("Memory", "Available MBytes");
                double available = ramCounter.NextValue();

                ////获取内存可用大小
                //ManagementClass cimobject2 = new ManagementClass("Win32_PerfFormattedData_PerfOS_Memory");
                //ManagementObjectCollection moc2 = cimobject2.GetInstances();
                //foreach (ManagementObject mo2 in moc2)
                //{
                //    available += int.Parse(mo2.Properties["AvailableMBytes"].Value.ToString();
                //}
                //moc2.Dispose();
                //cimobject2.Dispose();

                sb.AppendLine("MemoryAvailable=" + available + "M");
                sb.AppendLine("MemoryUsed=" + (phisicalMemory - available) + "M，" +
                              Math.Round(((phisicalMemory - available) / phisicalMemory * 100), 2) + "％");

                PerformanceCounter cpuCounter = new PerformanceCounter
                                                    {
                                                        CategoryName = "Processor",
                                                        CounterName = "% Processor Time",
                                                        InstanceName = "_Total"
                                                    };
                sb.AppendLine("总CPU利用率=" + cpuCounter.NextValue() + "%");

                //// Get the WMI class  
                //ManagementClass cc = new ManagementClass(new ManagementPath("Win32_Processor"));     
                //// Get the properties in the class     
                //ManagementObjectCollection moc = cc.GetInstances();    
                //foreach (ManagementObject mo in moc)
                //{
                //    PropertyDataCollection properties = mo.Properties; //获取内核数代码    
                //    Console.WriteLine("物理内核数:" + properties["NumberOfCores"].Value);
                //    Console.WriteLine("逻辑内核数:" + properties["NumberOfLogicalProcessors"].Value);
                //    //其他属性获取代码    
                //    foreach (PropertyData property in properties)
                //    {
                //        Console.WriteLine(property.Name + ":" + property.Value);
                //    }
                //}
                //moc.Dispose();
                //cc.Dispose();

                Logger.Info(sb.ToString());

                var sb2 = new StringBuilder();
                sb2.Append("Bytes=" + _mStatistics.Bytes);
                sb2.Append("&Overflowed=" + _mStatistics.Overflowed);
                sb2.Append("&Produced=" + _mStatistics.Produced);

                sb2.AppendLine("&机器的CPU内核数=" + processorCount);
                sb2.AppendLine("&当前进程CPU利用率=" + cpuUsage);
                sb2.AppendLine("&当前进程内存占用=" + memoryUsage);
                sb2.AppendLine("&当前进程的线程数=" + threadCount);
                sb2.AppendLine("&" + threadPoolInfo);
                sb2.AppendLine("&" + bestThreadInfo);
                sb2.AppendLine("&MemoryCapacity=" + phisicalMemory + "M");
                sb2.AppendLine("&MemoryAvailable=" + available + "M");
                sb2.AppendLine("&MemoryUsed=" + (phisicalMemory - available) + "M，" +
                               Math.Round(((phisicalMemory - available) / phisicalMemory * 100), 2) + "％");
                sb2.AppendLine("&总CPU利用率=" + cpuCounter.NextValue() + "%");

                Cat.GetProducer().LogHeartbeat("Cat", "Heartbeat", "0", sb2.ToString());
                Cat.GetProducer().LogEvent("Cat", "Heartbeat" + DateTime.Now.Minute, "0", sb2.ToString());

                _mStatistics.Bytes = 0;
                _mStatistics.Overflowed = 0;
                _mStatistics.Produced = 0;

                Thread.Sleep(60000);
            }
        }
    }
}