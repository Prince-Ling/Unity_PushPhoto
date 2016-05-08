using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;

namespace UFileCSharpSDK
{
	public class Utils
	{
		public enum RequestHeadType {
			HEAD_FIELD_CHECK
		};
		public static int bufferLen = 32 * 1024;
		public static int blockSize = 4 * 1024 * 1024;
		public static string GetMimeType(string file) {
			return MimeTypeMap.GetMimeType (Path.GetExtension (file));
		}
		public static void Copy(Stream dst, Stream src)
		{
			long l  =src.Position;
			byte[] buffer = new byte[bufferLen];
			while (true)
			{
				int n = src.Read(buffer, 0, bufferLen);
				if (n == 0) break;
				dst.Write(buffer, 0, n);
			}
			src.Seek (l, SeekOrigin.Begin);
		}
		public static long CopyNBit(Stream dst, Stream src, long numBytesToCopy)
		{
			long l  =src.Position;
			byte[] buffer = new byte[bufferLen];
			long numBytesWritten = 0;
			while (numBytesWritten < numBytesToCopy)
			{
				int len = bufferLen;
				if ((numBytesToCopy - numBytesWritten) < len)
				{
					len = (int)(numBytesToCopy - numBytesWritten);
				}
				int n = src.Read(buffer, 0, len);
				if (n == 0) break;
				dst.Write(buffer, 0, n);
				numBytesWritten += n;
			}
			src.Seek (l, SeekOrigin.Begin);
			return numBytesWritten;
		}
		public static string GetURL(string bucket, string key) 
		{
			return @"http://" + bucket + Config.UCLOUD_PROXY_SUFFIX + (key == string.Empty ? "" : (@"/" + key));
		}
		public static string GetMD5(string file, Int64 offset, Int64 size)
        {
            string md5String;
            FileStream fileStream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            long fileSize = fileStream.Length;
            fileStream.Seek(offset, SeekOrigin.Begin);

            System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            if (size != 0)
            {
                if (fileSize - offset < size)
                {
                    size = fileSize - offset;
                }
                byte[] buffer = new byte[size];
                fileStream.Read(buffer, 0, (int)size);
                MemoryStream ms = new MemoryStream(buffer);
                md5String = BitConverter.ToString(md5.ComputeHash(ms)).Replace("-", String.Empty);
            }
            else
            {
                md5String = BitConverter.ToString(md5.ComputeHash(fileStream)).Replace("-", String.Empty);
            }
            fileStream.Close();
            return md5String;
        }
		public static long GetContengLength(string file)
		{
			FileInfo fileInfo = new FileInfo (file);
			return fileInfo.Length;
		}

		public static void CopyFile(HttpWebRequest request, Stream file) 
		{
 			Stream rs = request.GetRequestStream();
			Utils.CopyNBit (rs, file, file.Length);
			rs.Close ();
		}

        public static void CopyFile(HttpWebRequest request, string file)
        {
            FileStream fileStream = File.OpenRead(file);
            Stream rs = request.GetRequestStream();
            Utils.CopyNBit(rs, fileStream, fileStream.Length);
            fileStream.Close ();
            rs.Close();
        }

        public static void SetHeaders(HttpWebRequest request, string file, string bucket, string key, string httpVerb)
		{
			request.UserAgent = Config.GetUserAgent ();
            if (file != string.Empty)
            {
                request.ContentType = Utils.GetMimeType(file);
            }
            else
            {
                request.ContentType = "image/jpeg";
            }

            request.Method = httpVerb;
			request.Headers.Add ("Authorization", Digest.SignRequst(request, RequestHeadType.HEAD_FIELD_CHECK, bucket, key));
		}
		public static string GetSHA1(byte[] data)
		{
			SHA1 sha = new SHA1CryptoServiceProvider ();
			return System.Text.Encoding.Default.GetString (sha.ComputeHash (data));
		}

	}
}

