/*===============================================================================
Copyright (c) 2024 PTC Inc. and/or Its Subsidiary Companies. All Rights Reserved.

Confidential and Proprietary - Protected under copyright and other laws.
Vuforia is a trademark of PTC Inc., registered in the United States and other
countries.
===============================================================================*/

#ifndef _VU_PLATFORMCONFIG_UWP_H_
#define _VU_PLATFORMCONFIG_UWP_H_

/**
 * \file PlatformConfig_UWP.h
 * \brief UWP-specific configuration for the Vuforia Engine
 */

#include <VuforiaEngine/Engine/Engine.h>

#ifdef __cplusplus
extern "C"
{
#endif

/** \addtogroup PlatformUWPEngineConfigGroup UWP-specific Engine Configuration
 * \ingroup EngineConfigGroup
 * \{
 */

/// \brief UWP-specific configuration error code type for errors occurring when creating a Vuforia Engine instance
/**
 * \note The error code is reported via the \p errorCode parameter of the vuEngineCreate() function if an error
 * related to applying UWP-specific configuration occurs while initializing the new Engine instance.
 */
VU_ENUM(VuPlatformUWPConfigError)
{
    VU_ENGINE_CREATION_ERROR_PLATFORM_UWP_CONFIG_INITIALIZATION_ERROR = 0x530, ///< An error occurred during initialization of the platform
};

/// \brief UWP-specific platform configuration data structure
typedef struct VuPlatformUWPConfig
{
    ///\brief The view orientation to initialize Engine with.
    ///       The value is a pointer to a Windows::Graphics::Display::DisplayOrientations instance
    /**
     * It is strongly recommended to provide this value during Engine creation, if it is not provided Engine will use a default value
     * until \ref vuPlatformControllerSetViewOrientation is called with the actual value.
     *
     * \see vuPlatformControllerSetViewOrientation
     * \see vuPlatformControllerConvertPlatformViewOrientation
     */
    const void* displayOrientation;

} VuPlatformUWPConfig;

/// \brief Default UWP-specific configuration
VU_API VuPlatformUWPConfig VU_API_CALL vuPlatformUWPConfigDefault();

/// \brief Add a UWP-specific configuration to the Engine config
VU_API VuResult VU_API_CALL vuEngineConfigSetAddPlatformUWPConfig(VuEngineConfigSet* configSet, const VuPlatformUWPConfig* config);

/** \} */

#ifdef __cplusplus
}
#endif

#endif // _VU_PLATFORMCONFIG_UWP_H_
