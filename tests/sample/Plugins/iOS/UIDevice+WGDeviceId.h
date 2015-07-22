//
//  WGDeviceId.h
//  WGDeviceId
//
//  Created by Raul Gigea on 1/15/13.
//  Copyright (c) 2013 wooga. All rights reserved.
//

#import <UIKit/UIKit.h>

#ifdef UNITY_VERSION

char* WGDeviceIdStringMono();

#endif

@interface UIDevice (WGDeviceId)

// This should be defined in pod dependency 'UIDeviceAdditions'
- (NSString *) macaddress;

/*
 * @method uniqueGlobalPerDeviceNameIdentifier
 * @description use this method when you need a unique global identifier to track a device
 * with multiple apps. as example a advertising network will use this method to track the device
 * from different apps.
 * ID used for live game backend & tracking, consisting of default 6667 prefix + decimal number of ( hast of MAC-address + DeviceName )
 */
- (uint64_t) uniqueGlobalPerDeviceNameIdentifier __attribute__ ((deprecated));
// NSString cast of uniqueGlobalPerDeviceNameIdentifier
-(NSString*) WGDeviceIdString __attribute__ ((deprecated)); // use the Cached version instead
-(NSString*) WGDeviceIdStringWithPrefix:(int)prefix __attribute__ ((deprecated));

-(NSString*) WGDeviceIdStringCached;


// DEPRECATED !
// ID used for tracking backend, consisting of 6666 prefix + MD5-ed hash of MAC-address ( without DeviceName )
-(NSString*) oldTrackingID;

@end
