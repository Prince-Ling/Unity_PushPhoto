using System;
using System.Net;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace UFileCSharpSDK
{
	public class Digest
	{
		private static string CanonicalizedUCloudHeaders(WebHeaderCollection header) 
		{
			string canoncializedUCloudHeaders = string.Empty;
			SortedDictionary<string, string> headerMap = new SortedDictionary<string, string> ();
			for (int i = 0; i < header.Count; ++i) {
				string headerKey = header.GetKey (i);
				if (headerKey.ToLower().StartsWith ("x-ucloud-")) {
					foreach (string value in header.GetValues(i)) {
						if (headerMap.ContainsKey (headerKey)) {
							headerMap [headerKey] += value;
							headerMap [headerKey] += @",";
						} else {
							headerMap.Add (headerKey, value);
						}
					}
				}
			}
			foreach (KeyValuePair<string, string> item in headerMap) {
				canoncializedUCloudHeaders += (item.Key + @":" + item.Value + @"\n");
			}
			return canoncializedUCloudHeaders;
		}
		private static string CanonicalizedResource(string bucket, string key)
		{
			return "/" + bucket + "/" + key;
		}
		public static string SignRequst (HttpWebRequest request, Utils.RequestHeadType type, string bucket, string key) 
		{
			string Authorization = string.Empty;
			string StringToSign = string.Empty;
			switch (type) {
			case Utils.RequestHeadType.HEAD_FIELD_CHECK:
				Authorization += "UCloud ";
				Authorization += Config.UCLOUD_PUBLIC_KEY;
				Authorization += ":";
				StringToSign = request.Method + "\n" + request.Headers.Get ("Content-MD5") + "\n";
				StringToSign += request.ContentType;
				StringToSign += "\n";
				/*
				StringToSign += DateTime.Now.ToString ();
				*/
				StringToSign += "\n";
				StringToSign += CanonicalizedUCloudHeaders (request.Headers);
				StringToSign += CanonicalizedResource (bucket, key); 
					break;
				default:
					break;
			}
			HMACSHA1 hmac = new HMACSHA1 (Encoding.ASCII.GetBytes (Config.UCLOUD_PRIVATE_KEY));
			Byte[] hashValue = hmac.ComputeHash(Encoding.UTF8.GetBytes(StringToSign));
			string Signature = Convert.ToBase64String (hashValue);
			return Authorization + Signature;
		}
	}
}
