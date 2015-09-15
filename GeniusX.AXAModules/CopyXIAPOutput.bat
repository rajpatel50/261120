SET SourceFolder=..\..\..\..\XIAP Framework\6_0_129\root
SET TargetFolder=..\

xcopy "%SourceFolder%\XIAP.Shell\Bin\debug\." "%TargetFolder%"\Lib\XIAP.Shell /i /d /y /r /e

xcopy "%SourceFolder%\XIAP\bin\debug\." "%TargetFolder%"\Lib\XIAP /i /d /y /r /e

xcopy "%SourceFolder%\XIAP.DesignStudio\Bin\Debug\Xiap.DesignStudio*.dll" "%TargetFolder%"\Lib\XIAP.DesignStudio /i /d /y /r /e

xcopy "%SourceFolder%\XIAP\Config\." "ClaimWakeUpService\Xiap.DataMigration.GeniusInterface.AXACS.Service\Config" /i /d /y /r /e

xcopy .\Config\Custom\*.* "ClaimWakeUpService\Xiap.DataMigration.GeniusInterface.AXACS.Service\Config\Custom\AXA" /i /d /y /r /e


