/*===============================================================================
Copyright (c) 2024 PTC Inc. and/or Its Subsidiary Companies. All Rights Reserved.

Vuforia is a trademark of PTC Inc., registered in the United States and other
countries.
===============================================================================*/

#ifndef _VU_PLATFORMCONTROLLER_UWP_H_
#define _VU_PLATFORMCONTROLLER_UWP_H_

/**
 * \file PlatformController_UWP.h
 * \brief UWP-specific functionality for the Vuforia Engine
 */

#include <VuforiaEngine/Core/Basic.h>
#include <VuforiaEngine/Core/System.h>

#ifdef __cplusplus
extern "C"
{
#endif

/** \addtogroup PlatformUWPControllerGroup UWP-specific Platform Controller
 * \ingroup PlatformControllerGroup
 * \{
 * UWP platform-specific functionality accessed via the PlatformController
 */

/// \brief Set the app-specific coordinate system for Windows Holographic applications
/**
 * \note Setting the coordinate system will reset device tracking and destroy all anchors
 *
 * \param controller Platform controller retrieved from Engine (see vuEngineGetPlatformController)
 * \param appSpecifiedCS Pointer of type "ABI::Windows::Perception::Spatial::ISpatialCoordinateSystem"
 * representing your Hololens app's coordinate system
 */
VU_API VuResult VU_API_CALL vuPlatformControllerSetHolographicAppCS(VuController* controller, void* appSpecifiedCS);


/// \brief Set the app-specific coordinate system for Windows Holographic applications
/**
 * \note Setting the coordinate system will reset device tracking and destroy all anchors
 *
 * \param controller Platform controller retrieved from Engine (see vuEngineGetPlatformController)
 * \param appSpecifiedCS Pointer to an "IUnknown" with underlying type "Windows::Perception::Spatial::ISpatialCoordinateSystem"
 * representing your Hololens app's coordinate system
 */
VU_API VuResult VU_API_CALL vuPlatformControllerSetHolographicAppCSFromIUnknown(VuController* controller, void* appSpecifiedCS);

/** \} */

#ifdef __cplusplus
}
#endif

#endif // _VU_PLATFORMCONTROLLER_UWP_H_
