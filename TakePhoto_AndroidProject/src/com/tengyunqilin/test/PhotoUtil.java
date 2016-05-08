package com.tengyunqilin.test;

import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.io.File;
import java.io.FileNotFoundException;
import java.io.FileOutputStream;
import java.io.IOException;

import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.os.Environment;
import android.util.Base64;
import android.util.Log;

public class PhotoUtil {
	public static void saveBitmap(Bitmap bitmap, String bitName) throws IOException {
		File file = new File(Environment.getExternalStorageDirectory().getPath() + "/" + bitName);
		if (file.exists()) {
			file.delete();
		}
		FileOutputStream out;
		try {
			out = new FileOutputStream(file);
			if (bitmap.compress(Bitmap.CompressFormat.PNG, 50, out)) {
				out.flush();
				out.close();
			}
		} catch (FileNotFoundException e) {
			e.printStackTrace();
		} catch (IOException e) {
			e.printStackTrace();
		}
	}

	// 质量压缩
	public static Bitmap compressImage(Bitmap image, int size) {
		ByteArrayOutputStream baos = new ByteArrayOutputStream();
		image.compress(Bitmap.CompressFormat.JPEG, size, baos);// 质量压缩方法，这里100表示不压缩，把压缩后的数据存放到baos中
//		int options = 100;
//		while (baos.toByteArray().length / 1024 > 50) { // 循环判断如果压缩后图片是否大于50kb,大于继续压缩
//			baos.reset();// 重置baos即清空baos
//			image.compress(Bitmap.CompressFormat.JPEG, options, baos);// 这里压缩options%，把压缩后的数据存放到baos中
//			options -= 10;// 每次都减少10
//		}
		ByteArrayInputStream isBm = new ByteArrayInputStream(baos.toByteArray());// 把压缩后的数据baos存放到ByteArrayInputStream中
		Bitmap bitmap = BitmapFactory.decodeStream(isBm, null, null);// 把ByteArrayInputStream数据生成图片
		return bitmap;
	}

	// 图片按比例大小压缩方法
	public static Bitmap getImageFromPath(String srcPath, int size, float height , float width) {
		Log.e("unity", "path:"+srcPath);
		BitmapFactory.Options newOpts = new BitmapFactory.Options();
		// 开始读入图片，此时把options.inJustDecodeBounds 设回true了
		newOpts.inJustDecodeBounds = true;
		Bitmap bitmap = BitmapFactory.decodeFile(srcPath, newOpts);// 此时返回bm为空
		newOpts.inJustDecodeBounds = false;
		int w = newOpts.outWidth;
		int h = newOpts.outHeight;
		Log.e("unity", "WandH:"+String.valueOf(w)+" "+String.valueOf(h));
		float hh = height;// 这里设置高度为500f
		float ww = width;// 这里设置宽度为750f
		// 缩放比。由于是固定比例缩放，只用高或者宽其中一个数据进行计算即可
		int be = 1;// be=1表示不缩放
		if (w > ww) {// 如果宽度大的话根据宽度固定大小缩放
			be = (int) (newOpts.outWidth / ww);
		} 
		if (w < h && h > hh) {// 如果高度高的话根据宽度固定大小缩放
			be = (int) (newOpts.outHeight / hh);
		}
		if (be <= 0)
			be = 1;
		newOpts.inSampleSize = be;// 设置缩放比例
		// 重新读入图片，注意此时已经把options.inJustDecodeBounds 设回false了
		bitmap = BitmapFactory.decodeFile(srcPath, newOpts);
		if(size == 100)
		{
			return bitmap;
		}
		else
		{
			return compressImage(bitmap, size);// 压缩好比例大小后再进行质量压缩
		}	
	}

	// 图片按比例大小压缩方法
	public static Bitmap compBitmap(Bitmap image, float height , float width) {
		ByteArrayOutputStream baos = new ByteArrayOutputStream();
		image.compress(Bitmap.CompressFormat.JPEG, 50, baos);
		ByteArrayInputStream isBm = new ByteArrayInputStream(baos.toByteArray());
		BitmapFactory.Options newOpts = new BitmapFactory.Options();
		// 开始读入图片，此时把options.inJustDecodeBounds 设回true了
		newOpts.inJustDecodeBounds = true;
		Bitmap bitmap = BitmapFactory.decodeStream(isBm, null, newOpts);
		newOpts.inJustDecodeBounds = false;
		int w = newOpts.outWidth;
		int h = newOpts.outHeight;
		float hh = height;// 这里设置高度为500f
		float ww = width;// 这里设置宽度为750f
		// 缩放比。由于是固定比例缩放，只用高或者宽其中一个数据进行计算即可
		int be = 1;// be=1表示不缩放
		if (w > ww) {// 如果宽度大的话根据宽度固定大小缩放
			be = (int) (newOpts.outWidth / ww);
		}
		if (w < h && h > hh) {// 如果高度高的话根据宽度固定大小缩放
			be = (int) (newOpts.outHeight / hh);
		}
		if (be <= 0)
			be = 1;
		newOpts.inSampleSize = be;// 设置缩放比例
		// 重新读入图片，注意此时已经把options.inJustDecodeBounds 设回false了
		isBm = new ByteArrayInputStream(baos.toByteArray());
		bitmap = BitmapFactory.decodeStream(isBm, null, newOpts);
		return bitmap;
		// return compressImage(bitmap, 50);// 压缩好比例大小后再进行质量压缩
	}

	public static String getBitmapStrBase64(Bitmap bitmap) {
		ByteArrayOutputStream baos = new ByteArrayOutputStream();
		bitmap.compress(Bitmap.CompressFormat.JPEG, 100, baos);
		byte[] bytes = baos.toByteArray();
		return Base64.encodeToString(bytes, 0);
	}
}
