using UnityEngine;
using UnityEngine.UI;
using UFileCSharpSDK;
using System;
using System.IO;
using System.Threading;
using System.Collections;
using System.Runtime.InteropServices;

public enum PhotoTypeEnum : int
{
    TAKE_CAMERA = 0,
    TAKE_PICK = 1,
}

public class PhotoControl : MonoBehaviour
{
    private string RoleID = "";//根据玩家roleid定义域名Key值//
    public static string UCloudBucket = "";//图片域名前缀
    public static string UFileEndStr = "?iopcmd=thumbnail&type=6&height=400&width=600|iopcmd=convert&dst=jpg&Q=50";//图片裁剪url后缀
    public static string UFileSmallEndStr = "?iopcmd=thumbnail&type=6&height=60&width=80";//图片裁剪url后缀
    public static string SavePathHead = Application.persistentDataPath + "/Pic";//图片保存位置
    public RawImage BigImage;//用于显示的image控件
    private RawImage SmallImage;//用于显示的聊天中的图片控件
    private Stream m_Stream;//图片上传的文件流
    private string FilePath = "";//图片本地路径
    private string cloudUrlStr = "";//图片的网络端url

    private bool isUpLoad = false;//正在上传标记

    public Texture2D CurSmallText = null;
    public int CurSmallHeight = 0;
    public int CurSmallWidth = 0;

#if UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidJavaObject GetMainActivity()
    {
        AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
      	AndroidJavaObject jo = jc.GetStatic<AndroidJavaObject>("currentActivity");
        return jo; 
    }
#endif
    //IOS打开照相机或者相册
    [DllImport("__Internal")]
    private static extern void _GetPhotoControl(int index);

    void Start()
    {
        DirectoryInfo di = new DirectoryInfo(SavePathHead);
        if (di.Exists)
        {
            di.Delete(true);
        }
        DirectoryInfo dir = new DirectoryInfo(SavePathHead);
        dir.Create();
    }


    void OnGUI()
    {
        if (GUI.Button(new Rect(100, 100, 100, 100), "Take Photo"))
        {
            OpenCamera();
        }

        if (GUI.Button(new Rect(300, 100, 100, 100), "Take Pick"))
        {
            OpenPick();
        }
    }

    /// <summary>
    /// 玩家没有选择图片或者照片取消回调接口
    /// </summary>
    public void CandleShowPhoto()
    {

    }

    /// <summary>
    /// 清除缓存数据
    /// </summary>
    public void CandleShowPhotoData()
    {
        if (BigImage != null)
        {
            BigImage.texture = null;
        }
        BigImage = null;
        m_Stream = null;
        cloudUrlStr = "";
    }

    /// <summary>
    /// 从Lua端调用的C#接口，传image控件和打开类型
    /// </summary>
    /// <param name="_bigImage">image控件</param>
    /// <param name="_typeIndex">打开照相机或者相册0/1</param>
    public void GetPhotoByType(RawImage _bigImage, int _typeIndex)
    {
        BigImage = _bigImage;
        if (_typeIndex == (int)PhotoTypeEnum.TAKE_CAMERA)
        {
            OpenCamera();
        }
        else if (_typeIndex == (int)PhotoTypeEnum.TAKE_PICK)
        {
            OpenPick();
        }
    }

    /// <summary>
    /// 从Lua端调用的C#接口，浏览大图
    /// </summary>
    /// <param name="_bigImage">image控件</param>
    /// <param name="_url">图片url buckey+key</param>
    public void ShowPhotoByCloudUrl(RawImage _smallImage, RawImage _bigImage, string _url)
    {
        SmallImage = _smallImage;
        BigImage = _bigImage;
        cloudUrlStr = _url;
        Thread loadThread = Loom.StartSingleThread(ShowUrlThread);
        Loom.StartSingleThread(ShowUrlBySmallImageThread);
    }

    /// <summary>
    /// 从Lua端调用的C#接口，浏览大图
    /// </summary>
    /// <param name="_bigImage">image控件</param>
    /// <param name="_url">图片url buckey+key</param>
    public void ShowPhotoByCloudUrl2(RawImage _bigImage, string _url)
    {
        BigImage = _bigImage;
        cloudUrlStr = _url;
        Thread loadThread = Loom.StartSingleThread(ShowUrlThread);
        //Loom.StartSingleThread(ShowUrlBySmallImageThread);
    }

    public void UpLoadSuccessShowSmallImage(RawImage _smallImage)
    {
        if (CurSmallText != null)
        {
            _smallImage.texture = CurSmallText;
            _smallImage.rectTransform.sizeDelta = new Vector2(CurSmallWidth, CurSmallHeight);
            CurSmallText = null;
        }
    }

    /// <summary>
    /// 显示大图
    /// </summary>
    public void ShowUrlThread()
    {
        string[] buckeyAndKey = cloudUrlStr.Split('$');
        if (buckeyAndKey.Length == 2)
        {
            string filename = buckeyAndKey[1].Replace("/", "_");
            string filePath = SavePathHead + "/" + filename;
            if (System.IO.File.Exists(filePath))
            {
                Loom.DispatchToMainThread(() =>
                {
                    LoadPicByUrl(filePath);
                });
            }
            else
            {
                FileStream stream = new FileStream(filePath, FileMode.CreateNew);
                Proxy.GetFile(buckeyAndKey[0], buckeyAndKey[1] + UFileEndStr, stream);
                stream.Close();
                Loom.DispatchToMainThread(() =>
                {
                    LoadPicByUrl(filePath);
                });
            }
        }
    }

    /// <summary>
    /// 显示小图
    /// </summary>
    public void ShowUrlBySmallImageThread()
    {
        string[] buckeyAndKey = cloudUrlStr.Split('$');
        if (buckeyAndKey.Length == 2)
        {
            string filename = "small"+buckeyAndKey[1].Replace("/", "_");
            string filePath = SavePathHead + "/" + filename;
            if (System.IO.File.Exists(filePath))
            {
                Loom.DispatchToMainThread(() =>
                {
                    LoadPicByUrlBySmallImage(filePath);
                });
            }
            else
            {
                FileStream stream = new FileStream(filePath, FileMode.CreateNew);
                Proxy.GetFile(buckeyAndKey[0], buckeyAndKey[1] + UFileSmallEndStr, stream);
                stream.Close();
                Loom.DispatchToMainThread(() =>
                {
                    LoadPicByUrlBySmallImage(filePath);
                });
            }
        }
    }

    /// <summary>
    /// wwwload大图
    /// </summary>
    /// <param name="_url"></param>
    public void LoadPicByUrl(string _url)
    {
        string url = "";
#if UNITY_STANDALONE_WIN
        url = "file:///" + _url;
#else
        url = "file://" + _url;
#endif
        StartCoroutine(LoadTexture(url));
    }

    /// <summary>
    /// wwwload小图
    /// </summary>
    /// <param name="_url"></param>
    public void LoadPicByUrlBySmallImage(string _url)
    {
        string url = "";
#if UNITY_STANDALONE_WIN
        url = "file:///" + _url;
#else
        url = "file://" + _url;
#endif
        StartCoroutine(LoadTextureBySmallImage(url));
    }

    /// <summary>
    /// load大图携程
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    IEnumerator LoadTexture(string name)
    {
        string path = name;
        WWW www = new WWW(path);
        yield return www;
        if (www.isDone)
        {
            Texture2D txt = www.texture;

            int width = txt.width;
            int height = txt.height;
            int targetHeight = 400;
            int targetWidth = 600;     
            int newHeight = 0;
            int newWidth = 0;
            GetCutSize(targetHeight, targetWidth, height, width, ref newHeight, ref newWidth);
            if (BigImage != null)
            {
                BigImage.gameObject.SetActive(true);
                BigImage.rectTransform.sizeDelta = new Vector2(newWidth, newHeight);
                BigImage.texture = txt;
            }
            CloseDialogJuHua();
        }
    }

    /// <summary>
    /// load小图携程
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    IEnumerator LoadTextureBySmallImage(string name)
    {
        string path = name;
        WWW www = new WWW(path);
        yield return www;
        if (www.isDone)
        {
            Texture2D txt = www.texture;
            if (SmallImage != null)
            {
                SmallImage.rectTransform.sizeDelta = new Vector2(txt.width, txt.height);
                SmallImage.texture = txt;
            }
        }
    }

    /// <summary>
    /// 上传图片
    /// </summary>
    /// <param name="roleId">玩家roleid</param>
    public void UpLoadPhoto(string roleId)
    {
        if (isUpLoad == true)
        {
            UpLoadAlreadyShowTips();
            return;
        }
        isUpLoad = true;
        RoleID = roleId;
        Thread loadThread = Loom.StartSingleThread(StartUpPicThread);
    }

    /// <summary>
    /// 上传图片线程
    /// </summary>
    public void StartUpPicThread()
    {
        Loom.DispatchToMainThread(() =>
        {
            OpenDialogJuHua();
        });

        string KeyTitle = RoleID;
        string KeyContent = DateTime.Now.ToFileTime().ToString();
        string KeyStr = KeyTitle + "/" + KeyContent + ".jpg";
        if (Proxy.PutFileV2(UCloudBucket, KeyStr, FilePath, m_Stream) == string.Empty)
        {
            //上传成功
            m_Stream = null;
            string KeyResult = UCloudBucket + "$" + KeyStr;
            Loom.DispatchToMainThread(() =>
            {
                UploadPhotoOKAndCloseDialog();
                SendImageMsgToServer(KeyResult);
            });
            isUpLoad = false;
        }
        else
        {
            //上传失败
            Loom.DispatchToMainThread(() =>
            {
                UploadPhotoFailAndShowTips();
            });
            isUpLoad = false;
        }
    }

    /// <summary>
    /// 打开相机
    /// </summary>
    public void OpenCamera()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        GetMainActivity().Call("TakePhotoChoose");
#elif UNITY_IPHONE && !UNITY_EDITOR
        _GetPhotoControl(0);
#endif
#if UNITY_EDITOR
        OpenWindowsPick();
#endif
    }

    /// <summary>
    /// 打开相册
    /// </summary>
    public void OpenPick()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        GetMainActivity().Call("TakePickChoose");
#elif UNITY_IPHONE && !UNITY_EDITOR
        _GetPhotoControl(1);
#endif
#if UNITY_EDITOR
        OpenWindowsPick();
#endif
    }

    /// <summary>
    /// Window打开
    /// </summary>
    public void OpenWindowsPick()
    {
        OpenFileName ofn = new OpenFileName();
        ofn.structSize = Marshal.SizeOf(ofn);
        ofn.file = new string(new char[256]);
        ofn.maxFile = ofn.file.Length;
        ofn.fileTitle = new string(new char[64]);
        ofn.maxFileTitle = ofn.fileTitle.Length;
        ofn.initialDir = UnityEngine.Application.dataPath;//默认路径
        ofn.title = "Open Project";
        ofn.filter = "图片文件(*.jpg*.png)\0*.jpg;*.png";
        ofn.defExt = "JPG";//显示文件的类型
        ofn.flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000200 | 0x00000008;//OFN_EXPLORER|OFN_FILEMUSTEXIST|OFN_PATHMUSTEXIST| OFN_ALLOWMULTISELECT|OFN_NOCHANGEDIR
        if (WindowDll.GetOpenFileName(ofn))
        {
            FilePath = ofn.file;
            LoadPicByUrl(FilePath);
            return;
        }
        CandleShowPhoto();
    }

    /// <summary>
    /// UI操作 打开菊花
    /// </summary>
    public void OpenDialogJuHua()
    {

    }

    /// <summary>
    /// UI操作 关闭菊花
    /// </summary>
    public void CloseDialogJuHua()
    {

    }

    /// <summary>
    /// UI操作 上传成功关闭界面
    /// </summary>
    public void UploadPhotoOKAndCloseDialog()
    {

    }

    /// <summary>
    /// UI操作 上传失败显示Tips
    /// </summary>
    public void UploadPhotoFailAndShowTips()
    {

    }

    /// <summary>
    /// UI操作 发送
    /// </summary>
    public void SendImageMsgToServer(string url)
    {

    }

    public void UpLoadAlreadyShowTips()
    {

    }
    
    /// <summary>
    /// 从Android/IOS获取图片的base64串并显示图片
    /// </summary>
    /// <param name="base64">图片的base64串</param>
    void ShowPhoto(string base64)
    {
        int targetHeight = 400;
        int targetWidth = 600;
        byte[] inputBytes = System.Convert.FromBase64String(base64);
        base64 = "";
        Texture2D text = BttetoPicByByte(inputBytes);
        int curHeight = text.height;
        int curWidth = text.width;
        int newHeight = 0;
        int newWidth = 0;
        GetCutSize(targetHeight, targetWidth, curHeight, curWidth, ref newHeight, ref newWidth);

        if (BigImage != null)
        {
            BigImage.gameObject.SetActive(true);
            BigImage.rectTransform.sizeDelta = new Vector2(newWidth, newHeight);
            BigImage.texture = text;
            m_Stream = new MemoryStream(inputBytes);
            inputBytes = null;
        }
        CloseDialogJuHua();
    }

    /// <summary>
    /// 保存小图用于发送显示
    /// </summary>
    /// <param name="base64"></param>
    void SaveMiniPhoto(string base64)
    {
        int targetHeight = 60;
        int targetWidth = 80;
        byte[] inputBytes = System.Convert.FromBase64String(base64);
        base64 = "";
        Texture2D text = BttetoPicByByte(inputBytes);
        int curHeight = text.height;
        int curWidth = text.width;
        int newHeight = 0;
        int newWidth = 0;
        GetCutSize(targetHeight, targetWidth, curHeight, curWidth, ref newHeight, ref newWidth);
        CurSmallText = text;
        CurSmallHeight = newHeight;
        CurSmallWidth = newWidth;
    }

    /// <summary>
    /// 工具：将string转成图片
    /// </summary>
    /// <param name="base64"></param>
    /// <returns></returns>
    Texture2D BttetoPic(string base64)
    {
        Texture2D pic = new Texture2D(600, 400);
        //将base64转码为byte[]  
        byte[] data = System.Convert.FromBase64String(base64);
        //加载byte[]图片 
        pic.LoadImage(data);
        return pic;
    }

    /// <summary>
    /// 工具：将bytes转成图片
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    Texture2D BttetoPicByByte(byte[] bytes)
    {
        Texture2D pic = new Texture2D(600, 400);
        //加载byte[]图片 
        pic.LoadImage(bytes);
        return pic;
    }

    /// <summary>
    /// 工具：返回给定高宽内的图片高度和宽度 比如给定400*600 肯定返回一个400*600之内的照片
    /// </summary>
    /// <param name="targetHeight"></param>
    /// <param name="targetWidth"></param>
    /// <param name="curHeight"></param>
    /// <param name="curWidth"></param>
    /// <param name="newHeight"></param>
    /// <param name="newWidth"></param>
    public static void GetCutSize(int targetHeight, int targetWidth, int curHeight, int curWidth, ref int newHeight, ref int newWidth)
    {
        int TargetHeight = targetHeight;
        int TargetWidth = targetWidth;
        int width = curWidth;
        int height = curHeight;
        float bili = 1f;
        if (width > TargetWidth)
        {
            bili = (float)TargetWidth / (float)width;
        }
        newHeight = (int)((float)height * bili);
        if (newHeight > TargetHeight)
        {
            bili = (float)TargetHeight / (float)height;
        }

        newHeight = (int)((float)height * bili);
        newWidth = (int)((float)width * bili);
    }
}
