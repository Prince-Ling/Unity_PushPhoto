using System;
using System.Net;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace UFileCSharpSDK
{

	public class Proxy
	{
		public static void GetFile(string bucket, string key, Stream stream)
		{
            HttpWebRequest request = null;
            HttpWebResponse response = null;
            try
            {
                request = (HttpWebRequest)WebRequest.Create(Utils.GetURL(bucket, key));
                request.KeepAlive = false;
                Utils.SetHeaders(request, string.Empty, bucket, key, "GET");

                response = HttpWebResponseExt.GetResponseNoException(request);
                Stream body = response.GetResponseStream();
                if (response.StatusCode != HttpStatusCode.OK)
                {
                }

                int bytesRead;
                byte[] buffer = new byte[1024];
                while ((bytesRead = body.Read(buffer, 0, buffer.Length)) != 0)
                    stream.Write(buffer, 0, bytesRead);
            }
            catch (Exception e)
            {

            }
            finally {
                if (request != null) request.Abort();
                if (response != null) response.Close();
            }
			return;
		}

        [Obsolete("use method DeleteFileV2")]
		public static string DeleteFile(string bucket, string key)
		{
			string strResult = string.Empty;
            HttpWebRequest request = null;
            HttpWebResponse response = null;
            try
            {
                request = (HttpWebRequest)WebRequest.Create(Utils.GetURL(bucket, key));
                request.KeepAlive = false;
                Utils.SetHeaders(request, string.Empty, bucket, key, "DELETE");

                response = HttpWebResponseExt.GetResponseNoException(request);
                Stream body = response.GetResponseStream();
                if (response.StatusCode != HttpStatusCode.NoContent)
                {

                }
            }
            catch (Exception e)
            {
            }
            finally
            {
                if (request != null) request.Abort();
                if (response != null) response.Close();
            }
			return strResult;
		}

        public static void DeleteFileV2(string bucket, string key) {
            DeleteFile(bucket, key);
        }

        [Obsolete("use method PutFileV2")]
		public static string PutFile(string bucket, string key, string file, Stream fileStream) 
		{
			string strResult = string.Empty;
            HttpWebRequest request = null;
            HttpWebResponse response = null;
            try
            {
                request = (HttpWebRequest)WebRequest.Create(Utils.GetURL(bucket, key));
                request.KeepAlive = false;
                Utils.SetHeaders(request, file, bucket, key, "PUT");
                if (fileStream != null)
                {
                    Utils.CopyFile(request, fileStream);
                }
                else if (file != String.Empty)
                {
                    Utils.CopyFile(request, file);
                }
                else
                {
                    strResult = "no resource";
                }
                
                response = HttpWebResponseExt.GetResponseNoException(request);
                Stream body = response.GetResponseStream();
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    strResult = "some error";
                }
            }
            catch (Exception e)
            {
                strResult = "some error";
            }
            finally
            {
                if (request != null) request.Abort();
                if (response != null) response.Close();
            }
			return strResult;
		}

        public static string PutFileV2(string bucket, string key, string file, Stream fileStream) {
            return PutFile(bucket, key, file, fileStream);
        }


        //NOTE: concurrently upload is NOT supported
        public class MultiUploader {

            public enum PROCESS_TYPE{
                MINIT = 0,
                MUPLOAD,
                MFINISH,
                MCANCEL
            };

            public MultiUploader(string bucket, string key, string file) {
                m_bucket = bucket;
                m_key = key;
                m_filename = file;
                m_part_number = 0;
                m_etags = new List<string>();
                try
                {
                    m_file = File.OpenRead(file);
                }catch(Exception e){

                }
            }

            ~MultiUploader() {
                m_file.Close();
            }
            public void MInit() {

                HttpWebRequest request = null;
                HttpWebResponse response = null;
                try
                {
                    string url = URL(PROCESS_TYPE.MINIT);
                    request = (HttpWebRequest)WebRequest.Create(url);
                    request.Method = "POST";
                    request.KeepAlive = false;
                    Utils.SetHeaders(request, "", m_bucket, m_key, "POST");
                    response = HttpWebResponseExt.GetResponseNoException(request);
                    Stream body = response.GetResponseStream();

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                    }

                    //parsing to get the uploadid & blksize


                }
                catch (Exception e)
                {

                }
                finally
                {
                    if (request != null) request.Abort();
                    if (response != null) response.Close();
                }
            }

            //part == -1 means the caller wants to upload all the parts one after after another
            //otherwise,MUpload will only upload one indicated part(for example,you need to upload
            //the same part when this part upload failed before)
            public void MUpload(long part = -1) {

                long total_parts_upload = -1;
                if (part == -1)
                {
                    m_part_number = 0;
                }
                else {
                    m_part_number = part;
                    total_parts_upload = 1;
                }

                HttpWebRequest request = null;
                HttpWebResponse response = null;
                try
                {

                    long parts_uploaded = 0;
                    bool finished = false;
                    while (true)
                    {

                        if (finished || (total_parts_upload != -1 && parts_uploaded >= total_parts_upload)) break;

                        string url = URL(PROCESS_TYPE.MUPLOAD);
                        request = (HttpWebRequest)WebRequest.Create(url);
                        request.Method = "PUT";
                        request.KeepAlive = false;

                        Utils.SetHeaders(request, m_filename, m_bucket, m_key, "PUT");
                        m_file.Seek(m_part_number * m_blk_size, 0);

                        MemoryStream ms = new MemoryStream();
                        long n = Utils.CopyNBit(ms, m_file, m_blk_size);
                        if (n < m_blk_size)
                        {
                            finished = true;
                            if (n == 0) break;
                        }
                        ms.Position = 0;
                        //set content-length
                        request.ContentLength = n;
                        Utils.CopyNBit(request.GetRequestStream(), ms, n);

                        response = HttpWebResponseExt.GetResponseNoException(request);
                        Stream body = response.GetResponseStream();

                        if (response.StatusCode != HttpStatusCode.OK)
                        {
                        }
                        else
                        {
                            string etag = response.GetResponseHeader("ETag");
                            m_etags.Add(etag);
                        }
                        response.Close();

                        m_part_number += 1;
                        parts_uploaded += 1;
                    }
                }
                catch (Exception e)
                {

                }
                finally
                {
                    if (request != null) request.Abort();
                    if (response != null) response.Close();
                }
            }

            public void MFinish() {

                HttpWebRequest request = null;
                HttpWebResponse response = null;
                try
                {
                    string url = URL(PROCESS_TYPE.MFINISH);
                    request = (HttpWebRequest)WebRequest.Create(url);
                    request.Method = "POST";
                    request.KeepAlive = false;
                    Utils.SetHeaders(request, "", m_bucket, m_key, "POST");

                    MemoryStream ms = new MemoryStream();
                    StreamWriter sw = new StreamWriter(ms);
                    for (int idx = 0; idx < m_etags.Count; ++idx)
                    {
                        sw.Write(m_etags[idx] + ",");
                    }
                    sw.Flush();
                    ms.Position = 0;
                    Utils.Copy(request.GetRequestStream(), ms);

                    response = HttpWebResponseExt.GetResponseNoException(request);
                    Stream body = response.GetResponseStream();
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                    }
                }
                catch (Exception e)
                {

                }
                finally {
                    if (request != null) request.Abort();
                    if (response != null) response.Close();
                }
            }

            public bool IfLastPart() {
                FileInfo fi = new FileInfo(m_filename);
                long filesize = fi.Length;
                long last_part = filesize / m_blk_size;
                if (filesize % m_blk_size == 0) --last_part;
                return last_part == m_part_number;
            }

            private string URL(PROCESS_TYPE type) { 
                switch(type) {
                    case PROCESS_TYPE.MINIT:
                        return string.Format("http://{0}{1}/{2}?uploads", m_bucket, Config.UCLOUD_PROXY_SUFFIX, m_key);
                    case PROCESS_TYPE.MUPLOAD:
                        return string.Format("http://{0}{1}/{2}?uploadId={3}&partNumber={4}", m_bucket, Config.UCLOUD_PROXY_SUFFIX, m_key, m_uploadid, m_part_number);
                    case PROCESS_TYPE.MFINISH:
                        return string.Format("http://{0}{1}/{2}?uploadId={3}", m_bucket, Config.UCLOUD_PROXY_SUFFIX, m_key, m_uploadid);
                    case PROCESS_TYPE.MCANCEL: return "";
                        return string.Format("http://{0}{1}/{2}?uploadId={3}", m_bucket, Config.UCLOUD_PROXY_SUFFIX, m_key, m_uploadid);
                    default:
                        throw new Exception("invalid url type for multiuploader");
                }
            }

            private string m_bucket;
            private string m_key;
            private string m_filename;
            private string m_uploadid;
            private long m_blk_size;
            private long m_part_number;
            private FileStream m_file;
            private List<string> m_etags;
        };
	}
}