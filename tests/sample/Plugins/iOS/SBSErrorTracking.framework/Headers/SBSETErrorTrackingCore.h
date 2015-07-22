#if !__has_feature(objc_arc)
#error This file must be compiled with ARC. Either turn on ARC for the project or use -fobjc-arc flag
#endif

#import "SBSETPayloadBuilder.h"

#define DEFAULT_BREADCRUMB_BUFFER_SIZE 20

@class PLCrashReport;

@interface SBSETErrorTrackingCore : NSObject

@property NSString *sbsUserId;
@property NSString *sbsDeviceId;

- (instancetype)initWithSBSGameId:(NSString *)sbsGameId userId:(NSString *)userId;

- (void)start;

- (void)notifyErrorType:(NSString *)errorType message:(NSString *)message stackTraceJSONString:(NSString *)stackTraceJSONString metadata:(NSDictionary *)metadata inBackground:(BOOL)inBackground severity:(SBSETSeverity)severity;

- (void)notifyErrorType:(NSString *)errorType message:(NSString *)message metadata:(NSDictionary *)metadata severity:(SBSETSeverity)severity;
- (void)notifyCrashWithReport:(PLCrashReport *)crashReport;

- (void)addBreadcrumb:(NSString *)breadcrumb;
@property NSInteger breadcrumbBufferSize;

@end