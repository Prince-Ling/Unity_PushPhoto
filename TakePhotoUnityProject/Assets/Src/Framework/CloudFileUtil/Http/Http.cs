using System;
using System.Net;

namespace UFileCSharpSDK
{
    public static class HttpWebResponseExt {
        public static HttpWebResponse GetResponseNoException(this HttpWebRequest req) {
            try { 
                return (HttpWebResponse)req.GetResponse();
            }catch (WebException we) {
                var resp = we.Response as HttpWebResponse;
                if (null == resp) throw;
                return resp;
            }
        }
    };
}