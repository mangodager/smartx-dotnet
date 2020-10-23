using System;
using System.Collections.Generic;
using System.Net;

namespace ETModel
{
	public static class NetworkHelper
	{
		public static IPEndPoint ToIPEndPoint(string host, int port)
		{

			return new IPEndPoint(IPAddress.Parse(host), port);
		}

		public static IPEndPoint ToIPEndPoint(string address)
		{
			int index = address.LastIndexOf(':');
			string host = address.Substring(0, index);
			string p = address.Substring(index + 1);
			int port = int.Parse(p);
			return ToIPEndPoint(host, port);
		}

		public static string DnsToIPEndPoint(string address)
		{
			try
			{
				int index = address.LastIndexOf(':');
				string host = address.Substring(0, index);
				string p = address.Substring(index + 1);
				int port = int.Parse(p);
				IPHostEntry iphost = Dns.GetHostEntry(host);
				List<IPAddress> list = new List<IPAddress>();
				for(int ii=0;ii<iphost.AddressList.Length;ii++)
                {
					if (iphost.AddressList[ii].AddressFamily==System.Net.Sockets.AddressFamily.InterNetwork)
					{
						list.Add(iphost.AddressList[ii]);
					}
                }
				int random = RandomHelper.Range(0, list.Count);
				return $"{list[random]}:{port}";
			}
			catch(Exception)
            {

            }
			return address;
		}

	}
}
