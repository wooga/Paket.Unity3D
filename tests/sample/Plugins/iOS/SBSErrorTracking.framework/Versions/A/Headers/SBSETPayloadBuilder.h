#if !__has_feature(objc_arc)
#error This file must be compiled with ARC. Either turn on ARC for the project or use -fobjc-arc flag
#endif

#define SBS_INFO_KEY @"sbsInfo"
#define SBS_GAME_ID_KEY @"gameId"
#define SBS_SYSTEM_KEY @"system"
#define SBS_DEVICE_ID_KEY @"deviceId"
#define SBS_USER_ID_KEY @"userId"

#define USER_ID_KEY @"userId"

#define DEVICE_KEY @"device"
#define DEVICE_MODEL_KEY @"model"
#define DEVICE_OS_VERSION_KEY @"osVersion"
#define DEVICE_LOCALE_KEY @"locale"
#define DEVICE_SCREEN_RESOLUTION_KEY @"screenResolution"
#define DEVICE_OS_NAME_KEY @"osName"

#define NOTIFIER_VERSION_KEY @"notifierVersion"

#define DEVICE_PHYSICAL_RAM_SIZE_KEY @"physicalRamSize"
#define DEVICE_JAILBROKEN_KEY @"jailbroken"
#define DEVICE_IS_RETINA_KEY @"isRetina"
#define DEVICE_BATTERY_LEVEL_KEY @"batteryLevel"
#define DEVICE_BATTERY_STATE_KEY @"batteryState"
#define DEVICE_ORIENTATION_KEY @"orientation"

#define APP_KEY @"app"
#define APP_VERSION_KEY @"version"
#define APP_TECHNICAL_VERSION_KEY @"technicalVersion"
#define APP_BUNDLE_ID_KEY @"bundleId"

#define CF_BUNDLE_VERSION @"CFBundleVersion"
#define CF_BUNDLE_SHORT_VERSION_STRING @"CFBundleShortVersionString"
#define CF_BUNDLE_IDENTIFIER @"CFBundleIdentifier"

#define EVENTS_KEY @"events"

#define CREATED_AT_KEY @"createdAt"

#define ERROR_TYPE_KEY @"errorType"
#define ERROR_SEVERITY_KEY @"severity"
#define ERROR_MESSAGE_KEY @"message"
#define ERROR_STACKTRACE_RAW_KEY @"stacktraceRaw"
#define ERROR_STACKTRACE_KEY @"stacktrace"
#define ERROR_BREADCRUMBS_KEY @"breadcrumbs"
#define ERROR_METADATA_KEY @"metaData"

#define APP_STATE_KEY @"appState"
#define APP_STATE_MEMORY_USAGE_KEY @"memoryUsage"
#define APP_STATE_DURATION_KEY @"duration"
#define APP_STATE_LOW_MEMORY_KEY @"lowMemory"
#define APP_STATE_IN_FOREGROUND_KEY @"inForeground"

typedef NS_ENUM(NSUInteger, SBSETSeverity) {
    SBSETSeverityFatal = 1,
    SBSETSeverityError = 2,
    SBSETSeverityWarning = 3,
};

@interface SBSETPayloadBuilder : NSObject

- (NSDictionary *)buildPayloadForStartRequest;

- (NSDictionary *) buildPayloadForError;

@property NSString *sbsGameId;
@property NSString *sbsDeviceId;
@property NSString *sbsUserId;
@property NSString *userId;
@property NSString *errorType;
@property NSString *errorMessage;
@property NSArray *errorStackTrace;
@property NSString *errorRawStackTrace;
@property NSArray *errorBreadcrumbs;
@property NSDictionary *errorMetadata;
@property NSDate *creationDate;
@property NSString *osVersion;
@property NSString *appVersion;
@property SBSETSeverity severity;

@end