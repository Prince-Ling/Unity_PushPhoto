
//
//  GetPhotoManager.h
//  image2
//
//  Created by tengjiang on 16/4/26.
//  Copyright © 2016年 GAEA. All rights reserved.
//

#ifndef GetPhotoManager_h
#define GetPhotoManager_h

#import <UIKit/UIKit.h>
#import <Foundation/Foundation.h>

@interface GetPhotoManager : UIViewController<UIImagePickerControllerDelegate,UIActionSheetDelegate,UINavigationControllerDelegate>

- (void)GetPhotoChoose:(NSInteger)ChooseIndex;
- (NSData *) imageCompressForWidth:(UIImage *)sourceImage targetWidth:(CGFloat)defineWidth targetHeight:(CGFloat)defineHeight;
- (int)GetCameraPermission;
- (int)GEtPickPermission;
@end

#endif /* GetPhotoManager_h */
