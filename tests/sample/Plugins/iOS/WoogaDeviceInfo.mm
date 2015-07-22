// @see http://stackoverflow.com/questions/12588923/ios-get-proxy-settings
// @see http://stackoverflow.com/questions/1598109/iphone-programmatically-read-proxy-settings

#import <Foundation/Foundation.h>

#include <sys/socket.h>
#include <sys/sysctl.h>
#include <net/if.h>
#include <net/if_dl.h>

extern "C" {

	const char* WoogaDeviceInfoGetCFBundleVersion()
	{
    	NSString *version = [[NSBundle mainBundle] objectForInfoDictionaryKey:@"CFBundleShortVersionString"];
        return strdup([version UTF8String]);
	}

	const char* WoogaDeviceInfoGetBuildVersion()
	{
		NSDictionary *infoDictionary = [[NSBundle mainBundle] infoDictionary];
		NSString *buildVersion = infoDictionary[(NSString*)kCFBundleVersionKey];
		return strdup([buildVersion UTF8String]);
	}

	const char* WoogaDeviceInfoGetCFBundleIdentifier()
	{
    	NSString *version = [[NSBundle mainBundle] bundleIdentifier];
    	return strdup([version UTF8String]);
	}

	const char* WoogaDeviceInfoGetMacAddress()
	{
			int                 mib[6];
			size_t              len;
			char                *buf;
			unsigned char       *ptr;
			struct if_msghdr    *ifm;
			struct sockaddr_dl  *sdl;

			mib[0] = CTL_NET;
			mib[1] = AF_ROUTE;
			mib[2] = 0;
			mib[3] = AF_LINK;
			mib[4] = NET_RT_IFLIST;

			if ((mib[5] = if_nametoindex("en0")) == 0) {
					printf("Error: if_nametoindex error\n");
					return NULL;
			}

			if (sysctl(mib, 6, NULL, &len, NULL, 0) < 0) {
					printf("Error: sysctl, take 1\n");
					return NULL;
			}

			if ((buf = (char *)malloc(len)) == NULL) {
					printf("Could not allocate memory. error!\n");
					return NULL;
			}

			if (sysctl(mib, 6, buf, &len, NULL, 0) < 0) {
					printf("Error: sysctl, take 2");
					free(buf);
					return NULL;
			}

			ifm = (struct if_msghdr *)buf;
			sdl = (struct sockaddr_dl *)(ifm + 1);
			ptr = (unsigned char *)LLADDR(sdl);
			NSString *outstring = [NSString stringWithFormat:@"%02X:%02X:%02X:%02X:%02X:%02X",
														*ptr, *(ptr+1), *(ptr+2), *(ptr+3), *(ptr+4), *(ptr+5)];
			free(buf);

			return strdup([outstring UTF8String]);
	}

	const char* WoogaDeviceInfoGetDeviceName()
	{
			return strdup([[[UIDevice currentDevice] name] UTF8String]);
	}

	const char* WoogaDeviceInfoGetIdForVendor()
	{
			return strdup([[[[UIDevice currentDevice] identifierForVendor] UUIDString] UTF8String]);
	}

	const float WoogaDeviceInfoGetSystemVersion()
	{
			return [[[UIDevice currentDevice] systemVersion] floatValue];
	}

  char* WoogaDeviceInfoGetProxyHost()
  {
      const int bSize = 4096;
      CFDictionaryRef dicRef = CFNetworkCopySystemProxySettings();
      const CFStringRef proxyCFstr = (const CFStringRef)CFDictionaryGetValue(dicRef, (const void*)kCFNetworkProxiesHTTPProxy);

      if(proxyCFstr == nil) {
          char * s = new char[1];
          strcpy(s, "");
          return s;
      }


      char buffer[bSize];
      memset(buffer, 0, bSize);
      if (CFStringGetCString(proxyCFstr, buffer, bSize, kCFStringEncodingUTF8))
      {
          char* ret = (char*) malloc(bSize);
          strcpy(ret, buffer);
          return ret;
      }
      char * s = new char[1];
      strcpy(s, "");
      return s;
  }

  int WoogaDeviceInfoGetProxyPort()
  {
      CFDictionaryRef dicRef = CFNetworkCopySystemProxySettings();
      const CFNumberRef portCFnum = (const CFNumberRef)CFDictionaryGetValue(dicRef, (const void*)kCFNetworkProxiesHTTPPort);

      if(portCFnum == nil) {
          return -1;
      }

      SInt32 port;
      if (CFNumberGetValue(portCFnum, kCFNumberSInt32Type, &port))
      {
          return port;
      }
      return -1;
  }

}
