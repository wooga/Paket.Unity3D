#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#import <AdSupport/AdSupport.h>

extern "C" {

		const char* WGTrackingUnityGetAppleAdIdentifier()
		{
	    	NSString *adidStr = @"";

				//if on ios6 we set the adid
				if([[[UIDevice currentDevice] systemVersion] floatValue] >= 6.0)
				{
					NSUUID *adid = [ASIdentifierManager sharedManager].advertisingIdentifier;
					adidStr = [adid UUIDString];
				}

	      return strdup([adidStr UTF8String]);
		}

}
