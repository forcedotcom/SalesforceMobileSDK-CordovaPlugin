/*
 Copyright (c) 2015-present, salesforce.com, inc. All rights reserved.
 
 Redistribution and use of this software in source and binary forms, with or without modification,
 are permitted provided that the following conditions are met:
 * Redistributions of source code must retain the above copyright notice, this list of conditions
 and the following disclaimer.
 * Redistributions in binary form must reproduce the above copyright notice, this list of
 conditions and the following disclaimer in the documentation and/or other materials provided
 with the distribution.
 * Neither the name of salesforce.com, inc. nor the names of its contributors may be used to
 endorse or promote products derived from this software without specific prior written
 permission of salesforce.com, inc.
 
 THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR
 IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
 FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
 DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
 DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY
 WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

#import <Foundation/Foundation.h>

/** Class containing the perf data associated with a selector execution.
 */
@interface SFSDKInstrumentationPostExecutionData : NSObject

/** The name of the class associated with the selector.
 */
@property (nonatomic, copy) NSString *className;

/** The name of the original selector executed.
 */
@property (nonatomic, copy) NSString *selectorName;

/** Whether or not the selector is an instance method.
 */
@property (nonatomic, assign) BOOL isInstanceMethod;

/** The start date of the selector's execution.
 */
@property (nonatomic, strong) NSDate *executionStartDate;

/** The end date of the selector's execution.
 */
@property (nonatomic, strong) NSDate *executionEndDate;

/** The amount of execution time for the selector, in seconds.
 */
@property (nonatomic, assign) NSTimeInterval executionTime;

@end

typedef void(^SFMethodInterceptorInvocationCallback)(NSInvocation *invocation);
typedef void(^SFMethodInterceptorInvocationAfterCallback)(NSInvocation *invocation, SFSDKInstrumentationPostExecutionData *data);

/** This class provides a simple way to intercept an
 instance method or a class method and forward message
 to the original method if needed.
 */
@interface SFMethodInterceptor : NSObject

/** Class to intercept
*/
@property (nonatomic, strong) Class classToIntercept;

/** Selector to intercept
*/
@property (nonatomic) SEL selectorToIntercept;

/** YES if the `selectorToIntercept` is an instance
* method, NO if it's a class method.
*/
@property (nonatomic) BOOL instanceMethod;

// The various blocks of interceptions (each of them can be nil)
/** Block to be called before the target method. Can be nil.
 */
@property (nonatomic, copy) SFMethodInterceptorInvocationCallback targetBeforeBlock;
/** Block that replaces the target method. Can be nil.
 */
@property (nonatomic, copy) SFMethodInterceptorInvocationCallback targetReplaceBlock;
/** Block to be called after the target method. Can be nil.
 */
@property (nonatomic, copy) SFMethodInterceptorInvocationAfterCallback targetAfterBlock;

/** Set this property to YES to enable the interceptor.
*/
@property (nonatomic) BOOL enabled;

@end
