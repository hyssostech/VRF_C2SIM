/*******************************************************************************
** Copyright (c) 2025 MaK Technologies, Inc.
** All rights reserved.
*******************************************************************************/

#pragma once

#include <vl/exerciseConnInitializer.h>
#include <vlutil/vlString.h>
#include <mtl/mtlEnvironment.h>
#include <matrix/vlVector.h>

class DtMtlEnvironment;

//DtRemoteControlInitializer derives from DtVrlApplicationInitializer and
//configures command line arguments for the remoteControl application.
//DtVrlApplicationInitializer contains
//default behavior for protocol specific parameters, while 
//DtRemoteControlInitializer contains behavior particular to the remoteControl
//application.
//
//parseCmdLine() parses the command line arguments specified in
//this class and DtVrlApplicationInitializer.  loadMtlFile() can be
//called (defined in DtVrlApplicationInitializer) with an MTL file name.
//This will parse the MTL file specified and set those variables registered
class DtRemoteControlInitializer : public DtVrlApplicationInitializer
{
public:
   DtRemoteControlInitializer(int argc, char* argv[], const DtString& defaultConfigFilePath);

   virtual ~DtRemoteControlInitializer() override;
   
   virtual void setSessionId(int);
   virtual int sessionId() const;

protected:
   //Register the MTL/Command Line parameters.  Non-virtual
   //since they are called from the constructor
   void initMtl();
   void initCmdLine();

protected:
   DtConfigVariable<int> mySessionId;
};


