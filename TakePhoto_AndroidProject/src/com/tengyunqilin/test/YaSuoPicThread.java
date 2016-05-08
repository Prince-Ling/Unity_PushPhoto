package com.tengyunqilin.test;

import java.io.ByteArrayOutputStream;
import java.io.IOException;

import com.unity3d.player.UnityPlayer;

import android.graphics.Bitmap;
import android.media.MediaPlayer;
import android.util.Log;

public class YaSuoPicThread extends Thread {

	private Bitmap mBitmap;
	private Bitmap mSmallBitmap;
	
	public YaSuoPicThread(Bitmap _bitmap) {
		this.mBitmap = _bitmap;
	}

	@Override
	public void run() {
		this.mBitmap = PhotoUtil.compBitmap(this.mBitmap, 500, 750);
		mSmallBitmap = PhotoUtil.compBitmap(this.mBitmap, 60, 80);
		UnityPlayer.UnitySendMessage("GameMain", "ShowPhoto", PhotoUtil.getBitmapStrBase64(this.mBitmap));
		UnityPlayer.UnitySendMessage("GameMain", "SaveMiniPhoto", PhotoUtil.getBitmapStrBase64(mSmallBitmap));
	}

}
