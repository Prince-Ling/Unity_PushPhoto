using System;
using System.Text;

namespace UFileCSharpSDK
{
	public class Config
	{
		public static string VERSION = @"1.0.2";

		/// <summary>
		/// UCloud管理服务器地址后缀
		/// </summary>
		public static string UCLOUD_PROXY_SUFFIX = @".ufile.ucloud.cn";

		/// <summary>
		/// UCloud提供的公钥
		/// </summary>
        public static string UCLOUD_PUBLIC_KEY = @"5ZSvzUQPCD1K+dELjcoQlkHQ/VsVUCNr86VdLmvLeGFgmpNbrQO6Tw==";

		/// <summary>
		/// UCloud提供的密钥
		/// </summary>
        public static string UCLOUD_PRIVATE_KEY = @"e2cfa3e916907f52781cbfbf71870ff7eba1db39";


		public static string GetUserAgent() 
		{
			return @"UCloudCSharp/" + VERSION;
		}
	}
}

