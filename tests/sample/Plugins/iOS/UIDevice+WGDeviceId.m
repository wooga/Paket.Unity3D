//
//  WGDeviceId.m
//  WGDeviceId
//
//  Created by Raul Gigea on 1/15/13.
//  Copyright (c) 2013 wooga. All rights reserved.
//

#import "UIDevice+WGDeviceId.h"
#import "UIDevice+IdentifierAddition.h"
#import <CommonCrypto/CommonDigest.h>

#include <sys/socket.h> // Per msqr
#include <sys/sysctl.h>
#include <net/if.h>
#include <net/if_dl.h>

#define UD_WGDID_USER_DEFAULTS_KEY @"Wooga unique device id string"

@interface UIDevice(Private)

// Same as uniqueGlobalPerDeviceNameIdentifier but with custom prefix ( should be under < 9999 )
- (uint64_t) uniqueGlobalPerDeviceNameIdentifierWithPrefix:(int)prefix;
-(NSString*) WGDeviceIdString;

@end

#ifdef UNITY_VERSION

char* WGDeviceIdStringMono()
{
    NSString *idstring = [[UIDevice currentDevice] WGDeviceIdStringCached];
    char *charString = (char*)malloc(strlen([idstring UTF8String]) + 1);
    strcpy(charString, [idstring UTF8String]);
	return charString;
}

#endif

@implementation UIDevice (WGDeviceId)

-(NSString*) WGDeviceIdStringCached
{
    NSUserDefaults *defaults = [NSUserDefaults standardUserDefaults];
    NSString *deviceID = [defaults objectForKey:UD_WGDID_USER_DEFAULTS_KEY];
    
    if (deviceID == nil)
    {
        deviceID = [self WGDeviceIdStringDeprecated];
        [defaults setObject:deviceID forKey:UD_WGDID_USER_DEFAULTS_KEY];
        [defaults synchronize];
    }
    return deviceID;
}

-(NSString*) WGDeviceIdString
{
    return [self WGDeviceIdStringDeprecated];
}


-(NSString*) WGDeviceIdStringDeprecated
{
    return [NSString stringWithFormat:@"%qu", [self uniqueGlobalPerDeviceNameIdentifierDeprecated]];
}

-(NSString*) WGDeviceIdStringWithPrefix:(int)prefix
{
    return [NSString stringWithFormat:@"%qu", [self uniqueGlobalPerDeviceNameIdentifierWithPrefix:prefix]];
}

-(NSString*) oldTrackingID
{
    NSString *uniqueIdentifier = [NSString stringWithFormat:@"6666%@", [self uniqueGlobalDeviceIdentifier]];
    return uniqueIdentifier;
}

- (uint64_t) uniqueGlobalPerDeviceNameIdentifierDeprecated
{
    return [self uniqueGlobalPerDeviceNameIdentifierWithPrefix:6667];
}

- (uint64_t) uniqueGlobalPerDeviceNameIdentifier
{
    return [self uniqueGlobalPerDeviceNameIdentifierDeprecated];
}

- (uint64_t) uniqueGlobalPerDeviceNameIdentifierWithPrefix:(int)prefix
{
    NSString *deviceName = [[UIDevice currentDevice] name];
    NSString *macAddress, *macAndDeviceName;
    
    NSString *reqSysVer = @"7.0";
    NSString *currSysVer = [[UIDevice currentDevice] systemVersion];
    if ([currSysVer compare:reqSysVer options:NSNumericSearch] != NSOrderedAscending)
    {
        // Use identifier for vendor on iOS7 and above
        // increment prefix by 100 to have a different number space than in iOS6
        prefix += 100;
        macAndDeviceName = [[[UIDevice currentDevice] identifierForVendor] UUIDString];
    }
    else {
        macAddress = [[UIDevice currentDevice] macaddress];
        macAndDeviceName = [NSString stringWithFormat:@"%@:%@",macAddress, deviceName];
    }
    
    const char *value = [macAndDeviceName UTF8String];
    unsigned char outputBuffer[CC_MD5_DIGEST_LENGTH];
    CC_MD5(value, strlen(value), outputBuffer);
    
    uint64_t strippedMD548bit =
    (uint64_t) *(outputBuffer)  |
    (uint64_t) *(outputBuffer+1) << 8 |
    (uint64_t) *(outputBuffer+2) << 16 |
    (uint64_t) *(outputBuffer+3) << 24 |
    (uint64_t) *(outputBuffer+4) << 32 |
    (uint64_t) *(outputBuffer+5) << 40;
    
    // add prefix
    strippedMD548bit += (prefix * 1000000000000000LL);
    return strippedMD548bit;
}

@end
