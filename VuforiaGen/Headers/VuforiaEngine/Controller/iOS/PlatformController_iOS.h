/*===============================================================================
Copyright (c) 2024 PTC Inc. and/or Its Subsidiary Companies. All Rights Reserved.

Vuforia is a trademark of PTC Inc., registered in the United States and other
countries.
===============================================================================*/

#ifndef _VU_PLATFORMCONTROLLER_IOS_H_
#define _VU_PLATFORMCONTROLLER_IOS_H_

/**
 * \file PlatformController_iOS.h
 * \brief iOS-specific functionality for the Vuforia Engine
 */

#include <VuforiaEngine/Core/Basic.h>
#include <VuforiaEngine/Core/System.h>

#ifdef __cplusplus
extern "C"
{
#endif

/** \addtogroup PlatformIOSControllerGroup iOS-specific Platform Controller
 * \ingroup PlatformControllerGroup
 * \{
 * iOS platform-specific platform functionality accessed via the PlatformController
 */

/// \brief ARKit-specific info for the platform-based Vuforia Fusion Provider
/**
 * \note The pointers contained in this data structure are owned by Vuforia Engine and should
 * be used with caution by the developer. For example do not release the session, do not pause
 * the session, do not reconfigure it, doing so will cause Vuforia Engine's handling of the information
 * from the provider to fail in undefined ways.
 *
 * Valid values for the pointers will be available only after Vuforia Engine has been started and
 * the Vuforia State contains camera frame data.
 *
 * The ARSession pointer will remain valid until Vuforia Engine is stopped, either by calling
 * \ref vuEngineStop explicitly or when an asynchronous life-cycle error is reported via the
 * \ref VuErrorHandler callback with error codes \ref VU_ENGINE_ERROR_INVALID_LICENSE and
 * \ref VU_ENGINE_ERROR_CAMERA_DEVICE_LOST.
 *
 * The ARFrame pointer will remain valid only for the duration of one Vuforia Engine frame.
 * The current ARFrame can, however, always be obtained directly from the ARSession,
 * using arSession.currentFrame.
 *
 * On receiving a \ref VuErrorHandler callback with either of the errors \ref VU_ENGINE_ERROR_INVALID_LICENSE
 * and \ref VU_ENGINE_ERROR_CAMERA_DEVICE_LOST, the pointers may already be invalid inside the callback.
 * The App must therefore not make use of the pointers inside the callback, and return the control to
 * Vuforia Engine without delay. The pointers can be re-requested after Vuforia Engine has been
 * (re-)started.
 *
 * Users are advised to always register for the \ref VuErrorHandler via the \ref VuErrorHandlerConfig
 * when using the Fusion Provider pointers and handle potential asynchronous invalidation of these pointers
 * appropriately.
 */
typedef struct VuPlatformARKitInfo
{
    /// \brief ARKit session, pointer of type "ARSession"
    /**
     * The caller needs to cast the arSession pointer to the appropriate type as follows:
     * ARSession* session = (__bridge ARSession*)info.arSession;
     */
    void* arSession;

    /// \brief ARKit frame, pointer of type "ARFrame"
    /**
     * The caller needs to cast the arFrame pointer to the appropriate type as follows:
     * ARFrame* frame = (__bridge ARFrame*)info.arFrame;
     *
     * Alternatively the frame can also be obtained directly from the ARSession,
     * using arSession.currentFrame;
     */
    void* arFrame;
} VuPlatformARKitInfo;

/// \brief Get information about the ARKit Fusion Provider Platform
/**
 * The information contained in the returned struct can be used to allow applications to interact with
 * the underlying ARKit session to gain access to functionality not currently available through the
 * Vuforia API. For example additional lighting information or plane boundaries.
 *
 * \note Call this function after Vuforia Engine has been started and the Vuforia State
 * contains a camera frame.
 *
 * \param controller Plaform controller retrieved from Engine (see \ref vuEngineGetPlatformController)
 * \param arkitInfo ARKit-specific info for the platform-based Vuforia Fusion Provider
 * \returns VU_FAILED if Vuforia is not running, is not using the ARKit Fusion Provider Platform,
 * or if the ARKit pointers are not ready to be retrieved yet, VU_SUCCESS otherwise
 */
VU_API VuResult VU_API_CALL vuPlatformControllerGetARKitInfo(const VuController* controller, VuPlatformARKitInfo* arkitInfo);

/// \brief Set ARKit platform fusion provider configuration
/**
 * This function is used to configure the ARKit session that will be used. An instance of the class
 * ARWorldTrackingConfiguration should be created and its parameters should be set as desired.
 * The pointer to said instance should be passed into this function. Vuforia Engine then inspects the
 * configuration values and takes a copy of the ones that are appropriate to use with Vuforia Engine
 *
 * \note Call this function before \ref vuPlatformControllerGetARKitInfo is called for the first time.
 *
 * \note Important to notice that the setting has no effect until \ref vuPlatformControllerGetARKitInfo is called.
 *
 * \note Currently Vuforia Engine only uses the AREnvironmentTexturing option of the
 * ARWorldTrackingConfiguration instance supplied to this call. All other configuration options
 * are managed by Vuforia Engine.
 *
 * \note The current configuration can be found by acquiring the ARSession by using
 * \ref vuPlatformControllerGetARKitInfo and querying the configuration from it.
 *
 * \param controller Platform controller retrieved from Engine (see \ref vuEngineGetPlatformController)
 * \param config Configuration pointer of type ARWorldTrackingConfiguration
 */
VU_API VuResult VU_API_CALL vuPlatformControllerSetARKitConfig(VuController* controller, const void* config);

/** \} */

#ifdef __cplusplus
}
#endif

#endif // _VU_PLATFORMCONTROLLER_IOS_H_
