using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using Amazon;
using Amazon.Route53;


namespace AwsConsoleApp1
{
    class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                PrintUsage();
                return;
            }

            var firstParam = args[0].Split('=');
            if (firstParam.Length != 2)
            {
                PrintUsage();
                return;
            }

            if (firstParam[0] != "--zone")
            {
                PrintUsage();
                return;
            }

            string zoneId = firstParam[1];
            if (string.IsNullOrEmpty(zoneId))
            {
                PrintUsage();
                return;
            }

            Console.Write(GetServiceOutput(zoneId));
        }

        public static void PrintUsage()
        {
            Console.WriteLine();
            Console.WriteLine("parameter example) --zone=[zone_id]");
            Console.WriteLine();
        }

        public static string GetServiceOutput(string hostedZoneId)
        {
            StringBuilder sb = new StringBuilder(1024);
            using (StringWriter sr = new StringWriter(sb))
            {
                AmazonRoute53 r53 = AWSClientFactory.CreateAmazonRoute53Client();
                var response = r53.ListResourceRecordSets(new Amazon.Route53.Model.ListResourceRecordSetsRequest
                {
                    HostedZoneId = hostedZoneId
                });

                var result = response.ListResourceRecordSetsResult;
                foreach (var record in result.ResourceRecordSets)
                {
                    if (record.Type == "NS")
                    {
                        // dns name 의 최대 길이를 구한다.
                        int maxLen = 0;
                        foreach (var rr in record.ResourceRecords)
                        {
                            maxLen = rr.Value.Length > maxLen ? rr.Value.Length : maxLen;
                        }

                        int nsWidth = maxLen + 5;
                        foreach (var rr in record.ResourceRecords)
                        {
                            // 너비를 동일하게 출력. 왼쪽 정렬
                            string format = string.Format("{{0,-{0}}}", nsWidth);
                            sr.Write(string.Format(format, rr.Value));

                            var host = Dns.GetHostEntry(rr.Value);
                            foreach (var ip in host.AddressList)
                            {
                                // ip는 왼쪽 정렬, 너비를 동일하게 출력.
                                string strIp = string.Format("{0,-18}", ip.ToString());
                                sr.Write(strIp);
                                sr.Write(", ");

                                // ping 출력
                                var ping = new Ping();
                                var reply = ping.Send(ip);
                                sr.Write("ping: {0} msec", reply.RoundtripTime);
                            }
                            sr.WriteLine();
                        }
                    }
                }
            }
            return sb.ToString();
        }
    }
}