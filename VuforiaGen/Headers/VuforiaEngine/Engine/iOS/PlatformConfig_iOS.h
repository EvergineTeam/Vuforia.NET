/*===============================================================================
Copyright (c) 2024 PTC Inc. and/or Its Subsidiary Companies. All Rights Reserved.

Confidential and Proprietary - Protected under copyright and other laws.
Vuforia is a trademark of PTC Inc., registered in the United States and other
countries.
===============================================================================*/

#ifndef _VU_PLATFORMCONFIG_IOS_H_
#define _VU_PLATFORMCONFIG_IOS_H_

/**
 * \file PlatformConfig_iOS.h
 * \brief iOS-specific configuration for the Vuforia Engine
 */

#include <VuforiaEngine/Engine/Engine.h>

#ifdef __cplusplus
extern "C"
{
#endif

/** \addtogroup PlatformiOSEngineConfigGroup iOS-specific Engine Configuration
 * \ingroup EngineConfigGroup
 * \{
 */

/// \brief iOS-specific configuration error code type for errors occurring when creating a Vuforia Engine instance
/**
 * \note The error code is reported via the \p errorCode parameter of the vuEngineCreate() function if an error
 * related to applying iOS-specific configuration occurs while initializing the new Engine instance.
 */
VU_ENUM(VuPlatformiOSConfigError)
{
    VU_ENGINE_CREATION_ERROR_PLATFORM_IOS_CONFIG_INITIALIZATION_ERROR = 0x550, ///< An error occurred during initialization of the platform
    VU_ENGINE_CREATION_ERROR_PLATFORM_IOS_CONFIG_INVALID_APP_GROUP =
        0x551 ///< Invalid app group - please see VuPlatformiOSConfig documentation
};

/// \brief iOS-specific platform configuration data structure
typedef struct VuPlatformiOSConfig
{
    ///\brief The view orientation to initialize Engine with. The value is a pointer to a UIInterfaceOrientation instance
    /**
     * It is strongly recommended to provide this value during Engine creation, if it is not provided Engine will use a default value
     * until \ref vuPlatformControllerSetViewOrientation is called with the actual value.
     *
     * \see vuPlatformControllerSetViewOrientation
     * \see vuPlatformControllerConvertPlatformViewOrientation
     */
    const void* interfaceOrientation;

    /// \brief App group identifier
    /**
     * This is required for the app to be able to access the app group's shared storage location. Supplying a nullptr here (default) will
     * disable this functionality. Any provided value must match the one in the .entitlements file of the app, and be a valid identifier as
     * per https://developer.apple.com/documentation/bundleresources/entitlements/com_apple_security_application-groups?language=objc
     *
     * \note The provided string is copied, and can be freed, after the Engine instance has been created.
     */
    const char* appGroup;
} VuPlatformiOSConfig;

/// \brief Default iOS-specific configuration
VU_API VuPlatformiOSConfig VU_API_CALL vuPlatformiOSConfigDefault();

/// \brief Add an iOS-specific configuration to the engine config
VU_API VuResult VU_API_CALL vuEngineConfigSetAddPlatformiOSConfig(VuEngineConfigSet* configSet, const VuPlatformiOSConfig* config);

/** \} */

#ifdef __cplusplus
}
#endif

#endif // _VU_PLATFORMCONFIG_IOS_H_
