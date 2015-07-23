#if !__has_feature(objc_arc)
#error This file must be compiled with ARC. Either turn on ARC for the project or use -fobjc-arc flag
#endif

#import "SBSETPayloadBuilder.h"

void InitErrorTracking(char *sbsGameId, char *userId);

void SetSBSUserId(char *userId);

void SetSBSDeviceId(char *sbsDeviceId);

void NotifyError(char *errorType, char *errorMessage, char *stackTrace, char *metadata, bool inBackground, SBSETSeverity severity);

void AddBreadcrumb(char *breadcrumb);

void SetBreadcrumbBufferSize(int bufferSize);
