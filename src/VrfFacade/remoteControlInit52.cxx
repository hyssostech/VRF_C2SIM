/*******************************************************************************
** Copyright (c) 2025 MaK Technologies, Inc.
** All rights reserved.
*******************************************************************************/

#include "remoteControlInit52.h"  // 5.2d sample file, renamed for the VRF_API_52 build axis

#include <matrix/vlVector.h>
#include <matrix/geodeticCoord.h>

//Constructor -- Initialize the base class (will initialize protocol-specific
//parameters), command line arguments and mtl parameters
DtRemoteControlInitializer::DtRemoteControlInitializer(int argc, char* argv[], const DtString& defaultConfigFilePath)
: DtVrlApplicationInitializer(argc, argv, "remoteControl", defaultConfigFilePath)
, mySessionId(exerciseConnectionConfig()->settings(), "sess&ionId", 1, "Session id of the simulation")
{
   initMtl();
   initCmdLine();
}

DtRemoteControlInitializer::~DtRemoteControlInitializer()
{
}

//Registers the MTL parameters
void DtRemoteControlInitializer::initMtl()
{
}

//Initializes the command line parameters
void DtRemoteControlInitializer::initCmdLine()
{
}

//--------------------------------------------------------------
//   ACCESSORS/MODIFIERS
//   Provide a way to inspect/modify the data
//--------------------------------------------------------------

void DtRemoteControlInitializer::setSessionId(int i)
{
   mySessionId.setValue(i);
}

int DtRemoteControlInitializer::sessionId() const
{
   return mySessionId.value();
}
