package com.tengyunqilin.test;

import java.io.File;
import java.io.IOException;
import java.text.SimpleDateFormat;
import java.util.Date;

import com.unity3d.player.UnityPlayer;
import com.unity3d.player.UnityPlayerActivity;

import android.Manifest;
import android.content.ContentResolver;
import android.content.Intent;
import android.database.Cursor;
import android.graphics.Bitmap;
import android.net.Uri;
import android.os.Bundle;
import android.os.Environment;
import android.provider.MediaStore;
import android.util.Log;

public class MainActivity extends UnityPlayerActivity {
	// public class UnityTestActivity extends Activity {

	public static final int PIC_TAKE_PHOTO = 1;
	// 相册
	public static final int PIC_TAKE_PICK = 2;

	private static final String IMAGE_TYPE = "image/*";
	private String PhotoPath = "";

	@Override
	public void onCreate(Bundle savedInstanceState) {
		super.onCreate(savedInstanceState);

	}

	public void TakePhotoChoose() {
		int hasCameraPermission = checkCallingOrSelfPermission(Manifest.permission.CAMERA);

		Intent intent = new Intent(MediaStore.ACTION_IMAGE_CAPTURE);
		SimpleDateFormat format = new SimpleDateFormat("yyyyMMddHHmmss");
		File imageFile = new File(Environment.getExternalStorageDirectory().getPath() + "/DCIM/Camera/"
				+ format.format(new Date()) + ".jpg");
		intent.putExtra(MediaStore.EXTRA_OUTPUT, Uri.fromFile(imageFile));
		PhotoPath = imageFile.getPath();
		startActivityForResult(intent, PIC_TAKE_PHOTO);
	}

	public void TakePickChoose() {
		Intent intent = new Intent(Intent.ACTION_PICK, null);
		intent.setDataAndType(MediaStore.Images.Media.EXTERNAL_CONTENT_URI, IMAGE_TYPE);
		startActivityForResult(intent, PIC_TAKE_PICK);
	}

	/**
	 * 生命周期onActivityForResult接口
	 * 
	 * @param context
	 * @param requestCode
	 * @param resultCode
	 * @param data
	 */
	@Override
	public void onActivityResult(int requestCode, int resultCode, Intent data) {
		super.onActivityResult(requestCode, resultCode, data);
		if (resultCode == 0) {
			// 取消接口
			UnityPlayer.UnitySendMessage("GameMain", "CandleShowPhoto", "");
			return;
		}
		if (requestCode == PIC_TAKE_PHOTO) {
			// 拍照
			Log.e("Unity", "GetPhotoPath");
			UnityPlayer.UnitySendMessage("GameMain", "GetPath", PhotoPath);
			Bitmap bm = PhotoUtil.getImageFromPath(PhotoPath, 40, 500, 750);
			Bitmap bmSmall = PhotoUtil.getImageFromPath(PhotoPath, 40, 60, 80);
			UnityPlayer.UnitySendMessage("GameMain", "ShowPhoto", PhotoUtil.getBitmapStrBase64(bm));
			UnityPlayer.UnitySendMessage("GameMain", "SaveMiniPhoto", PhotoUtil.getBitmapStrBase64(bmSmall));
		}
		if (requestCode == PIC_TAKE_PICK) {
			// 读取相册缩放图片
			Bitmap bm = null;
			ContentResolver resolver = getContentResolver();
			Uri originalUri = data.getData();
			Cursor cursor = getContentResolver().query(originalUri, null, null, null, null);
			if (cursor != null && cursor.moveToFirst()) {
				String path = cursor.getString(cursor.getColumnIndexOrThrow(MediaStore.Images.Media.DATA));
				bm = PhotoUtil.getImageFromPath(path, 40, 500, 750);
				Bitmap bmSmall = PhotoUtil.getImageFromPath(path, 40, 60, 80);
				UnityPlayer.UnitySendMessage("GameMain", "ShowPhoto", PhotoUtil.getBitmapStrBase64(bm));
				UnityPlayer.UnitySendMessage("GameMain", "SaveMiniPhoto", PhotoUtil.getBitmapStrBase64(bmSmall));
			} else {
				try {
					bm = MediaStore.Images.Media.getBitmap(resolver, originalUri);
				} catch (IOException e) {
					e.printStackTrace();
				}
				if (bm != null) {
					Thread thread = new YaSuoPicThread(bm);
					thread.start();
				}
			}
		}
	}

}